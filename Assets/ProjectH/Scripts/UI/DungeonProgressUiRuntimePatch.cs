using System; // 문자열 비교 기능
using System.Collections; // 코루틴 기능
using ProjectH.Battle; // 던전 진행 상태 기능
using ProjectH.Core; // 게임 관리자 및 씬 기능
using ProjectH.SaveSystem; // 저장 데이터 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // Unity 씬 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 중복 진행 UI 패치 방지
    public sealed class DungeonProgressUiRuntimePatch : MonoBehaviour // 던전 진행 상태 런타임 UI 연동
    {
        private static readonly Color AvailableCardColor = new Color(0.15f, 0.22f, 0.32f, 1f); // 진입 가능 카드 색상
        private static readonly Color SelectedCardColor = new Color(0.23f, 0.33f, 0.46f, 1f); // 선택 카드 색상
        private static readonly Color ClearedCardColor = new Color(0.16f, 0.34f, 0.28f, 1f); // 클리어 카드 색상
        private static readonly Color LockedCardColor = new Color(0.22f, 0.24f, 0.28f, 1f); // 잠금 카드 색상
        private static readonly Color AvailableTextColor = new Color(0.94f, 0.72f, 0.22f, 1f); // 진입 가능 상태 글자 색상
        private static readonly Color ClearedTextColor = new Color(0.46f, 0.88f, 0.64f, 1f); // 클리어 상태 글자 색상
        private static readonly Color LockedTextColor = new Color(0.62f, 0.64f, 0.68f, 1f); // 잠금 상태 글자 색상
        private DungeonSelectScreenController screenController; // 던전 선택 화면 컨트롤러 참조

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // 첫 씬 로드 전 이벤트 구독 지정
        private static void RegisterSceneLoadedHandler() // 씬 로드 이벤트 구독
        {
            SceneRuntimePatch.Register(GameScenes.DungeonSelect, HandleSceneLoaded); // DungeonSelect 씬 로드 시 주입 등록 (최적화 — 공통 등록기 사용)
        }

        private static void HandleSceneLoaded(Scene scene) // 씬 로드 완료 처리
        {
            if (UnityEngine.Object.FindFirstObjectByType<DungeonProgressUiRuntimePatch>() != null) // 기존 패치 존재 확인
            {
                return; // 중복 패치 설치 중단
            }

            new GameObject("DungeonProgressUiRuntimePatch", typeof(DungeonProgressUiRuntimePatch)); // 런타임 진행 UI 패치 객체 생성
        }

        private void Awake() // 진행 UI 패치 초기화
        {
            DungeonSelectionRuntimeState.SetUnlockEvaluator(IsDungeonUnlocked); // 선택 시스템 해금 검사 연결
            DungeonSelectionRuntimeState.SelectionChanged += HandleSelectionChanged; // 던전 선택 변경 이벤트 구독
        }

        private IEnumerator Start() // 런타임 화면 생성 이후 초기 갱신
        {
            yield return null; // 기존 던전 선택 화면 한 프레임 생성 대기
            screenController = UnityEngine.Object.FindFirstObjectByType<DungeonSelectScreenController>(); // 던전 선택 화면 컨트롤러 조회
            RefreshProgressUi(); // 초기 던전 진행 UI 갱신
        }

        private void OnDestroy() // 진행 UI 패치 해제
        {
            DungeonSelectionRuntimeState.SelectionChanged -= HandleSelectionChanged; // 던전 선택 변경 이벤트 구독 해제
            DungeonSelectionRuntimeState.SetUnlockEvaluator(null); // 던전 해금 검사 연결 해제
        }

        private void HandleSelectionChanged(string _) // 던전 선택 변경 처리
        {
            StartCoroutine(RefreshAfterFrame()); // 기존 화면 갱신 이후 진행 UI 재적용
        }

        private IEnumerator RefreshAfterFrame() // 다음 프레임 진행 UI 갱신
        {
            yield return null; // 기존 선택 화면 상태 갱신 대기
            RefreshProgressUi(); // 진행 UI 상태 재적용
        }

        private bool IsDungeonUnlocked(string dungeonId) // 현재 저장 기준 던전 해금 여부 확인
        {
            SaveData saveData = GetCurrentSave(); // 현재 저장 데이터 조회
            return saveData != null && DungeonProgressionPolicy.IsUnlocked(saveData, dungeonId); // 저장 존재 및 순차 해금 결과 반환
        }

        private void RefreshProgressUi() // 던전 카드 및 상세 상태 갱신
        {
            if (screenController == null) // 화면 컨트롤러 참조 확인
            {
                screenController = UnityEngine.Object.FindFirstObjectByType<DungeonSelectScreenController>(); // 화면 컨트롤러 재조회
            }

            if (screenController == null) // 화면 컨트롤러 존재 확인
            {
                return; // 화면 미생성 상태 갱신 중단
            }

            SaveData saveData = GetCurrentSave(); // 현재 저장 데이터 조회

            for (int index = 0; index < DungeonSelectionRuntimeState.SupportedDungeonIds.Count; index++) // 지원 던전 ID 순회
            {
                string dungeonId = DungeonSelectionRuntimeState.SupportedDungeonIds[index]; // 현재 던전 ID 조회
                RefreshDungeonCard(saveData, dungeonId); // 현재 던전 카드 진행 상태 갱신
            }

            RefreshEnterButton(saveData); // 전투 진입 버튼 해금 상태 갱신
            RefreshDetailStatus(saveData); // 상세 상태 진행 문구 갱신
        }

        private void RefreshDungeonCard(SaveData saveData, string dungeonId) // 단일 던전 카드 진행 상태 갱신
        {
            Button cardButton = FindButton($"DungeonCard_{dungeonId}"); // 현재 던전 카드 버튼 조회

            if (cardButton == null) // 카드 버튼 존재 확인
            {
                return; // 누락 카드 갱신 중단
            }

            bool hasDungeonData = HasDungeonData(dungeonId); // 던전 정적 데이터 존재 여부 확인
            DungeonProgressState state = DungeonProgressionPolicy.GetState(saveData, dungeonId); // 저장 기준 던전 진행 상태 계산
            bool selectable = saveData != null && hasDungeonData && state != DungeonProgressState.Locked; // 카드 선택 가능 여부 계산
            bool selected = string.Equals(DungeonSelectionRuntimeState.SelectedDungeonId, dungeonId, StringComparison.Ordinal); // 현재 선택 카드 여부 확인
            cardButton.interactable = selectable; // 카드 버튼 잠금 상태 적용
            Image cardImage = cardButton.GetComponent<Image>(); // 카드 배경 이미지 조회

            if (cardImage != null) // 카드 배경 이미지 존재 확인
            {
                cardImage.color = ResolveCardColor(state, selected); // 진행 및 선택 상태 카드 색상 적용
            }

            Transform idTransform = cardButton.transform.Find("Id"); // 카드 ID 텍스트 Transform 조회
            Text idText = idTransform == null ? null : idTransform.GetComponent<Text>(); // 카드 ID 텍스트 조회

            if (idText != null) // 카드 ID 텍스트 존재 확인
            {
                string starsSuffix = state == DungeonProgressState.Cleared ? $" · {BuildStarsText(DungeonProgressSaveAdapter.GetBestStars(saveData, dungeonId))}" : string.Empty; // 클리어 상태 최고 별점 문구 계산 (Day47)
                idText.text = $"{dungeonId} · {DungeonProgressionPolicy.GetStatusLabel(state)}{starsSuffix}"; // 카드 진행 상태 문구 적용
                idText.color = ResolveStatusTextColor(state); // 카드 진행 상태 글자 색상 적용
            }
        }

        private void RefreshEnterButton(SaveData saveData) // 전투 진입 버튼 상태 갱신
        {
            Button enterButton = FindButton("EnterButton"); // 전투 진입 버튼 조회

            if (enterButton == null) // 전투 진입 버튼 존재 확인
            {
                return; // 진입 버튼 갱신 중단
            }

            string selectedDungeonId = DungeonSelectionRuntimeState.SelectedDungeonId; // 현재 선택 던전 ID 조회
            bool canEnter = saveData != null && HasDungeonData(selectedDungeonId) && DungeonProgressionPolicy.IsUnlocked(saveData, selectedDungeonId); // 저장 및 해금 기반 진입 가능 여부 계산
            enterButton.interactable = canEnter; // 전투 진입 버튼 상태 적용
        }

        private void RefreshDetailStatus(SaveData saveData) // 던전 상세 진행 상태 문구 갱신
        {
            Text statusText = FindText("Status"); // 상세 상태 텍스트 조회

            if (statusText == null) // 상세 상태 텍스트 존재 확인
            {
                return; // 상세 상태 갱신 중단
            }

            string selectedDungeonId = DungeonSelectionRuntimeState.SelectedDungeonId; // 현재 선택 던전 ID 조회

            if (string.IsNullOrWhiteSpace(selectedDungeonId)) // 던전 미선택 여부 확인
            {
                return; // 기존 미선택 안내 문구 유지
            }

            DungeonProgressState state = DungeonProgressionPolicy.GetState(saveData, selectedDungeonId); // 선택 던전 진행 상태 계산
            string statusLabel = DungeonProgressionPolicy.GetStatusLabel(state); // 선택 던전 상태 라벨 조회
            string suffix = BuildDetailSuffix(saveData, selectedDungeonId, state); // 상태별 상세 보조 문구 계산 (Day47 최고 별점 포함)
            statusText.text = $"{selectedDungeonId} · {statusLabel} · {suffix}"; // 선택 던전 상세 진행 문구 적용
        }

        private static string BuildDetailSuffix(SaveData saveData, string dungeonId, DungeonProgressState state) // 상세 상태 보조 문구 계산 (Day47)
        {
            if (state == DungeonProgressState.Locked) // 잠금 상태 확인
            {
                return DungeonProgressionPolicy.GetLockReason(dungeonId); // 잠금 사유 문구 반환
            }

            if (state == DungeonProgressState.Cleared) // 클리어 상태 확인
            {
                return $"최고 기록 {BuildStarsText(DungeonProgressSaveAdapter.GetBestStars(saveData, dungeonId))}"; // 최고 별점 문구 반환
            }

            return "출격 준비 완료"; // 진입 가능 상태 기본 문구 반환
        }

        private static string BuildStarsText(int stars) // 별점 표시 문구 생성 (Day47)
        {
            int clampedStars = Mathf.Clamp(stars, 0, DungeonProgressSaveAdapter.MaxStars); // 별점 범위 보정
            return new string('★', clampedStars) + new string('☆', DungeonProgressSaveAdapter.MaxStars - clampedStars); // 채움 및 빈 별 문구 반환
        }

        private Button FindButton(string objectName) // 화면 하위 버튼 이름 조회
        {
            Button[] buttons = screenController.GetComponentsInChildren<Button>(true); // 화면 하위 버튼 목록 조회

            for (int index = 0; index < buttons.Length; index++) // 화면 버튼 순회
            {
                Button button = buttons[index]; // 현재 버튼 조회

                if (button != null && string.Equals(button.gameObject.name, objectName, StringComparison.Ordinal)) // 버튼 이름 일치 여부 확인
                {
                    return button; // 일치 버튼 반환
                }
            }

            return null; // 버튼 조회 실패 반환
        }

        private Text FindText(string objectName) // 화면 하위 텍스트 이름 조회
        {
            Text[] texts = screenController.GetComponentsInChildren<Text>(true); // 화면 하위 텍스트 목록 조회

            for (int index = 0; index < texts.Length; index++) // 화면 텍스트 순회
            {
                Text text = texts[index]; // 현재 텍스트 조회

                if (text != null && string.Equals(text.gameObject.name, objectName, StringComparison.Ordinal)) // 텍스트 이름 일치 여부 확인
                {
                    return text; // 일치 텍스트 반환
                }
            }

            return null; // 텍스트 조회 실패 반환
        }

        private static SaveData GetCurrentSave() // 현재 저장 데이터 조회
        {
            return GameManager.Instance == null || GameManager.Instance.Save == null ? null : GameManager.Instance.Save.CurrentSave; // 게임 관리자 기반 현재 저장 반환
        }

        private static bool HasDungeonData(string dungeonId) // 던전 정적 데이터 존재 여부 확인
        {
            return !string.IsNullOrWhiteSpace(dungeonId) && GameManager.Instance != null && GameManager.Instance.Data != null && GameManager.Instance.Data.GetDungeon(dungeonId) != null; // 데이터 관리자 기반 던전 존재 결과 반환
        }

        private static Color ResolveCardColor(DungeonProgressState state, bool selected) // 던전 카드 상태 색상 반환
        {
            if (selected) // 현재 선택 카드 여부 확인
            {
                return SelectedCardColor; // 선택 카드 색상 반환
            }

            switch (state) // 던전 진행 상태 분기
            {
                case DungeonProgressState.Cleared: // 클리어 상태 처리
                    return ClearedCardColor; // 클리어 카드 색상 반환
                case DungeonProgressState.Available: // 진입 가능 상태 처리
                    return AvailableCardColor; // 진입 가능 카드 색상 반환
                default: // 잠금 상태 처리
                    return LockedCardColor; // 잠금 카드 색상 반환
            }
        }

        private static Color ResolveStatusTextColor(DungeonProgressState state) // 던전 상태 글자 색상 반환
        {
            switch (state) // 던전 진행 상태 분기
            {
                case DungeonProgressState.Cleared: // 클리어 상태 처리
                    return ClearedTextColor; // 클리어 글자 색상 반환
                case DungeonProgressState.Available: // 진입 가능 상태 처리
                    return AvailableTextColor; // 진입 가능 글자 색상 반환
                default: // 잠금 상태 처리
                    return LockedTextColor; // 잠금 글자 색상 반환
            }
        }
    }
}
