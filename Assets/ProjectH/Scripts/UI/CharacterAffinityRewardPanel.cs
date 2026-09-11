using System; // 콜백 델리게이트 기능
using System.Collections.Generic; // 목록 자료형
using ProjectH.Core; // 전역 게임 관리자 기능
using ProjectH.Dialogue; // 개인 이벤트 상태 기능 (Day58 추가)
using ProjectH.SaveSystem; // 호감도 보상·이벤트 조건 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 중복 패널 방지
    public sealed class CharacterAffinityRewardPanel : MonoBehaviour // 캐릭터 창 호감도 단계 보상 패널 (Day56 신규, 최적화로 캐릭터 화면 컨트롤러에서 분리)
    {
        private static readonly Color TitleColor = new Color(0.36f, 0.20f, 0.30f, 1f); // 제목 색상
        private static readonly Color ClaimableColor = new Color(1f, 0.86f, 0.46f, 1f); // 받기 버튼 금색
        private static readonly Color ClaimedColor = new Color(0.80f, 0.92f, 0.80f, 1f); // 받음 버튼 연두색
        private static readonly Color LockedColor = new Color(0.84f, 0.84f, 0.86f, 1f); // 잠김 버튼 회색

        private readonly List<Button> tierButtons = new List<Button>(); // 단계별 보상 버튼
        private readonly List<AffinityTier> tierOrder = new List<AffinityTier>(); // 버튼 순서별 단계
        private const int EventRowCount = 2; // 개인 이벤트 표시 줄 수 (Day58 추가, 현재 캐릭터당 2화)
        private readonly List<string> reasonBuffer = new List<string>(); // 이벤트 미충족 사유 재사용 버퍼
        private readonly List<Text> eventRowTexts = new List<Text>(); // 개인 이벤트 줄 문구 (Day58 추가)
        private readonly List<Button> eventRowButtons = new List<Button>(); // 개인 이벤트 보기 버튼 (Day58 추가)
        private readonly List<CharacterEventDefinition> eventRowDefinitions = new List<CharacterEventDefinition>(); // 줄별 이벤트 (Day58 추가)
        private Text titleText; // 패널 제목
        private Text bonusText; // 적용 중 전투 보너스 문구
        private Text eventText; // 개인 이벤트 제목 문구 (Day58부터 목록은 줄별 버튼으로 표시)
        private Text resultText; // 보상 수령 결과 문구
        private string boundCharacterId = string.Empty; // 현재 표시 캐릭터 ID
        private Action onClaimed; // 수령 후 화면 갱신 콜백
        private Action<CharacterEventDefinition> onPlayEvent; // 개인 이벤트 보기 콜백 (Day58 추가)

        public bool IsOpen => gameObject.activeSelf; // 패널 열림 여부 반환

        public static CharacterAffinityRewardPanel Create(Transform parent, Action claimedCallback, Action<CharacterEventDefinition> playEventCallback) // 패널 생성 (Day58 개인 이벤트 보기 콜백 추가)
        {
            Image panel = RuntimeUiKit.CreateImage(parent, "AffinityRewardPanel", new Color(0.98f, 0.97f, 0.95f, 0.98f)); // 패널 배경 생성
            RuntimeUiKit.SetRect(panel.rectTransform, new Vector2(0.18f, 0.10f), new Vector2(0.82f, 0.90f)); // 화면 중앙 배치
            panel.gameObject.AddComponent<Outline>().effectColor = new Color(0.75f, 0.55f, 0.30f, 0.8f); // 금색 외곽선 적용
            CharacterAffinityRewardPanel view = panel.gameObject.AddComponent<CharacterAffinityRewardPanel>(); // 패널 컴포넌트 추가
            view.onClaimed = claimedCallback; // 수령 콜백 저장
            view.onPlayEvent = playEventCallback; // 개인 이벤트 보기 콜백 저장 (Day58 추가)
            view.Build(); // 구성 요소 생성
            panel.gameObject.SetActive(false); // 초기 숨김
            return view; // 패널 반환
        }

        private void Build() // 패널 구성 요소 생성
        {
            titleText = RuntimeUiKit.CreateText(transform, "Title", "호감도 보상", 22, TitleColor, FontStyle.Bold, TextAnchor.MiddleLeft); // 제목 생성
            RuntimeUiKit.SetRect(titleText.rectTransform, new Vector2(0.04f, 0.88f), new Vector2(0.80f, 0.97f)); // 제목 배치
            Button closeButton = CreateButton("CloseButton", "닫기", new Color(0.88f, 0.88f, 0.90f, 1f)); // 닫기 버튼 생성
            RuntimeUiKit.SetRect((RectTransform)closeButton.transform, new Vector2(0.84f, 0.89f), new Vector2(0.97f, 0.97f)); // 닫기 버튼 배치
            closeButton.onClick.AddListener(Toggle); // 닫기 이벤트 연결

            for (int index = 0; index < AffinityRewardCatalog.All.Count; index++) // 단계 보상 순회
            {
                AffinityTier tier = AffinityRewardCatalog.All[index].Tier; // 단계 조회
                Button tierButton = CreateButton($"TierReward_{tier}", string.Empty, LockedColor); // 단계 보상 버튼 생성
                float top = 0.86f - (index * 0.135f); // 버튼 세로 위치 계산
                RuntimeUiKit.SetRect((RectTransform)tierButton.transform, new Vector2(0.04f, top - 0.12f), new Vector2(0.96f, top)); // 버튼 배치
                tierButton.onClick.AddListener(() => Claim(tier)); // 수령 이벤트 연결
                tierButtons.Add(tierButton); // 버튼 목록 등록
                tierOrder.Add(tier); // 단계 순서 등록
            }

            bonusText = RuntimeUiKit.CreateText(transform, "BonusText", string.Empty, 15, new Color(0.20f, 0.35f, 0.55f, 1f), FontStyle.Bold, TextAnchor.MiddleLeft); // 전투 보너스 문구 생성
            RuntimeUiKit.SetRect(bonusText.rectTransform, new Vector2(0.04f, 0.26f), new Vector2(0.96f, 0.31f)); // 보너스 문구 배치
            eventText = RuntimeUiKit.CreateText(transform, "EventText", "개인 이벤트", 15, new Color(0.18f, 0.18f, 0.20f, 1f), FontStyle.Bold, TextAnchor.MiddleLeft); // 개인 이벤트 제목 생성
            RuntimeUiKit.SetRect(eventText.rectTransform, new Vector2(0.04f, 0.215f), new Vector2(0.96f, 0.255f)); // 개인 이벤트 제목 배치

            for (int index = 0; index < EventRowCount; index++) // 개인 이벤트 줄 생성 (Day58 추가)
            {
                int row = index; // 클릭용 줄 번호 복사
                float top = 0.21f - (index * 0.066f); // 줄 세로 위치
                Text rowText = RuntimeUiKit.CreateText(transform, $"EventRow_{index}", string.Empty, 15, new Color(0.18f, 0.18f, 0.20f, 1f), FontStyle.Normal, TextAnchor.MiddleLeft).Wrap(); // 줄 문구 생성
                rowText.supportRichText = true; // 상태 색 표시
                RuntimeUiKit.SetRect(rowText.rectTransform, new Vector2(0.04f, top - 0.058f), new Vector2(0.77f, top)); // 줄 문구 배치
                Button rowButton = CreateButton($"EventPlay_{index}", "보기", LockedColor); // 보기 버튼 생성
                RuntimeUiKit.SetRect((RectTransform)rowButton.transform, new Vector2(0.79f, top - 0.056f), new Vector2(0.96f, top - 0.002f)); // 보기 버튼 배치
                rowButton.onClick.AddListener(() => PlayEventRow(row)); // 보기 연결
                eventRowTexts.Add(rowText); // 줄 문구 등록
                eventRowButtons.Add(rowButton); // 버튼 등록
                eventRowDefinitions.Add(null); // 줄 이벤트 자리 확보
            }
            resultText = RuntimeUiKit.CreateText(transform, "ResultText", string.Empty, 15, new Color(0.55f, 0.35f, 0.10f, 1f)); // 수령 결과 문구 생성
            RuntimeUiKit.SetRect(resultText.rectTransform, new Vector2(0.04f, 0.01f), new Vector2(0.96f, 0.07f)); // 결과 문구 배치
        }

        private Button CreateButton(string name, string label, Color color) // 캐릭터 화면 스타일 버튼 생성 (색 전환 대상 미연결·줄바꿈 라벨)
        {
            Button button = RuntimeUiKit.CreateButton(transform, name, color, false); // 버튼 생성
            Text text = RuntimeUiKit.CreateText(button.transform, "Text", label, 16, new Color(0.10f, 0.10f, 0.10f, 1f)).Wrap(); // 줄바꿈 라벨 생성
            RuntimeUiKit.Stretch(text.rectTransform, 7f); // 라벨 확장
            return button; // 버튼 반환
        }

        public void Toggle() // 패널 열기·닫기
        {
            bool open = !gameObject.activeSelf; // 전환 후 상태 결정
            gameObject.SetActive(open); // 패널 표시 전환
            transform.SetAsLastSibling(); // 최상단 렌더링
            resultText.text = string.Empty; // 결과 문구 초기화

            if (open) // 열림 확인
            {
                onClaimed?.Invoke(); // 최신 상태 반영을 위해 화면 갱신 요청
            }
        }

        public void Refresh(SaveData saveData, CharacterSaveData characterSave, string displayName) // 선택 캐릭터 기준 패널 갱신
        {
            if (!IsOpen || saveData == null || characterSave == null) // 표시 및 데이터 확인
            {
                return; // 닫혀 있으면 갱신 생략
            }

            boundCharacterId = characterSave.CharacterId; // 표시 캐릭터 저장
            int affinity = AffinityService.GetAffinity(saveData, boundCharacterId); // 현재 호감도 조회
            AffinityTier current = AffinityService.GetAffinityTier(saveData, boundCharacterId); // 현재 단계 조회
            titleText.text = $"{displayName} · 호감도 보상   ({AffinityService.GetTierLabel(current)} {affinity}/{CharacterSaveData.MaxAffinity})"; // 제목 적용

            for (int index = 0; index < tierButtons.Count; index++) // 단계 버튼 순회
            {
                AffinityTier tier = tierOrder[index]; // 단계 조회
                AffinityTierReward reward = AffinityRewardCatalog.Get(tier); // 보상 조회
                AffinityRewardState state = AffinityRewardService.GetState(saveData, boundCharacterId, tier); // 상태 조회
                string stateLabel = state == AffinityRewardState.Claimable ? "[받기]" : state == AffinityRewardState.Claimed ? "[받음]" : "[잠김]"; // 상태 문구 결정
                SetButtonLabel(tierButtons[index], $"{stateLabel}  {AffinityService.GetTierLabel(tier)} ({AffinityService.GetTierThreshold(tier)})   {reward.Summary}"); // 버튼 문구 적용
                tierButtons[index].interactable = state == AffinityRewardState.Claimable; // 받기 상태만 입력 허용
                tierButtons[index].GetComponent<Image>().color = state == AffinityRewardState.Claimable ? ClaimableColor : state == AffinityRewardState.Claimed ? ClaimedColor : LockedColor; // 상태별 색상 적용
            }

            bonusText.text = $"적용 중 전투 보너스 : {AffinityRewardCatalog.GetClaimedBonus(characterSave).Summary}"; // 누적 보너스 표시
            RefreshEventRows(saveData, boundCharacterId); // 개인 이벤트 줄 표시 (Day58 보기 버튼으로 변경)
        }

        private void RefreshEventRows(SaveData saveData, string characterId) // 개인 이벤트 줄·보기 버튼 갱신 (Day58 — 잠김 / 보기 / 다시보기)
        {
            List<CharacterEventDefinition> events = CharacterEventCatalog.GetForCharacter(characterId); // 캐릭터 개인 이벤트 조회
            eventText.text = events.Count == 0 ? "개인 이벤트 · 준비 중" : "개인 이벤트"; // 제목 적용

            for (int index = 0; index < eventRowTexts.Count; index++) // 줄 순회
            {
                CharacterEventDefinition item = index < events.Count ? events[index] : null; // 줄 이벤트
                eventRowDefinitions[index] = item; // 줄 이벤트 저장
                eventRowTexts[index].gameObject.SetActive(item != null); // 이벤트 없으면 줄 숨김
                eventRowButtons[index].gameObject.SetActive(item != null); // 이벤트 없으면 버튼 숨김

                if (item == null) // 이벤트 확인
                {
                    continue; // 다음 줄
                }

                CharacterEventState state = DialogueService.GetEventState(saveData, item, reasonBuffer); // 이벤트 상태·사유 조회
                string stateText = state == CharacterEventState.Completed ? "<color=#2E6FB0>[완료]</color>" : state == CharacterEventState.Available ? "<color=#2E8B57>[해금]</color>" : $"<color=#8A8A8A>[잠김] {string.Join(" · ", reasonBuffer)}</color>"; // 상태 문구
                eventRowTexts[index].text = $"{item.Episode}화 「{item.Title}」   {stateText}"; // 줄 문구 적용
                SetButtonLabel(eventRowButtons[index], state == CharacterEventState.Completed ? "다시보기" : state == CharacterEventState.Available ? "보기" : "잠김"); // 버튼 문구
                eventRowButtons[index].interactable = state != CharacterEventState.Locked; // 잠김은 입력 막음
                eventRowButtons[index].GetComponent<Image>().color = state == CharacterEventState.Available ? ClaimableColor : state == CharacterEventState.Completed ? ClaimedColor : LockedColor; // 상태별 색상
            }
        }

        private void PlayEventRow(int row) // 개인 이벤트 보기 버튼 처리 (Day58 추가)
        {
            CharacterEventDefinition item = row >= 0 && row < eventRowDefinitions.Count ? eventRowDefinitions[row] : null; // 줄 이벤트 조회
            if (item != null) onPlayEvent?.Invoke(item); // 대화 화면 열기 요청
        }

        public void ShowResult(string message) // 결과 문구 표시 (Day58 추가, 개인 이벤트 완료 안내)
        {
            resultText.text = message ?? string.Empty; // 결과 문구 적용
        }

        private void Claim(AffinityTier tier) // 단계 보상 받기
        {
            SaveManager saveManager = GameManager.Instance == null ? null : GameManager.Instance.Save; // 저장 관리자 조회
            SaveData saveData = saveManager == null ? null : saveManager.CurrentSave; // 저장 데이터 조회

            if (saveData == null || string.IsNullOrEmpty(boundCharacterId)) // 데이터 및 캐릭터 확인
            {
                resultText.text = "저장 데이터를 찾을 수 없습니다."; // 오류 안내
                return; // 수령 중단
            }

            bool claimed = AffinityRewardService.TryClaim(saveData, GameManager.Instance.Data, boundCharacterId, tier, out string message); // 보상 수령 시도
            bool saved = claimed && saveManager.SaveCurrent(); // 성공 시 즉시 저장
            resultText.text = claimed && !saved ? $"{message} (저장 실패)" : message; // 결과 문구 표시
            onClaimed?.Invoke(); // 능력치·버튼 상태 갱신 요청 (전투 보너스 즉시 반영)
        }

        private static void SetButtonLabel(Button button, string label) // 버튼 문구 변경
        {
            Text text = button.GetComponentInChildren<Text>(true); // 버튼 문구 조회

            if (text != null) // 문구 존재 확인
            {
                text.text = label; // 문구 적용
            }
        }
    }
}
