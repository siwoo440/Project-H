using System.Collections.Generic; // 목록 자료형
using ProjectH.Core; // 프로젝트 핵심 기능
using ProjectH.SaveSystem; // 저장 데이터 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 전투 컨트롤러 방지
    public sealed class BattleScreenController : MonoBehaviour // 전투 화면 배치 컨트롤러
    {
        [SerializeField] private BattleFormationAnchors formationAnchors; // 전투 진형 앵커
        [SerializeField] private Transform allyRoot; // 생성 아군 루트
        [SerializeField] private BattleUnitView unitTemplate; // 아군 전투 유닛 템플릿
        [SerializeField] private BattleHudCardView[] hudCards; // 하단 4인 HUD 카드
        [SerializeField] private Text waveText; // 웨이브 표시 텍스트
        [SerializeField] private Text timeText; // 전투 시간 표시 텍스트
        [SerializeField] private Text statusText; // 개발 상태 표시 텍스트
        [SerializeField] private Text autoButtonText; // AUTO 버튼 텍스트
        [SerializeField] private GameObject menuPanel; // 전투 메뉴 패널
        [SerializeField] private Button menuButton; // 전투 메뉴 버튼
        [SerializeField] private Button autoButton; // AUTO 버튼
        [SerializeField] private Button returnDungeonButton; // 던전 선택 복귀 버튼
        [SerializeField] private Button closeMenuButton; // 메뉴 닫기 버튼
        private readonly List<BattleUnitView> spawnedUnits = new List<BattleUnitView>(); // 생성된 아군 유닛 목록
        private BattlePartyRuntime partyRuntime; // 현재 전투 파티 런타임
        private float elapsedSeconds; // 전투 경과 시간
        private bool initialized; // 전투 초기화 완료 상태
        private bool isAutoEnabled = true; // AUTO 표시 상태
        private bool buttonsBound; // 버튼 이벤트 연결 여부
        private bool isTransitioning; // 씬 전환 잠금 상태

        public void Configure(BattleFormationAnchors formation, Transform allies, BattleUnitView template, BattleHudCardView[] cards, Text wave, Text time, Text status, Text autoLabel, GameObject menu, Button menuTarget, Button autoTarget, Button returnTarget, Button closeTarget) // 에디터 참조 설정
        {
            formationAnchors = formation; // 진형 앵커 연결
            allyRoot = allies; // 아군 루트 연결
            unitTemplate = template; // 전투 유닛 템플릿 연결
            hudCards = cards; // HUD 카드 연결
            waveText = wave; // 웨이브 텍스트 연결
            timeText = time; // 전투 시간 텍스트 연결
            statusText = status; // 상태 텍스트 연결
            autoButtonText = autoLabel; // AUTO 텍스트 연결
            menuPanel = menu; // 전투 메뉴 패널 연결
            menuButton = menuTarget; // 전투 메뉴 버튼 연결
            autoButton = autoTarget; // AUTO 버튼 연결
            returnDungeonButton = returnTarget; // 던전 복귀 버튼 연결
            closeMenuButton = closeTarget; // 메뉴 닫기 버튼 연결
        }

        private void Start() // 전투 화면 시작
        {
            BindButtons(); // 버튼 이벤트 연결
            InitializeBattle(); // 전투 배치 초기화
        }

        private void Update() // 전투 화면 시간 갱신
        {
            if (!initialized || isTransitioning) // 전투 진행 가능 상태 확인
            {
                return; // 시간 갱신 중단
            }

            elapsedSeconds += Time.deltaTime; // 전투 경과 시간 누적
            RefreshTime(); // 전투 시간 표시 갱신
        }

        private void OnDestroy() // 전투 화면 종료
        {
            ClearSpawnedUnits(); // 생성 아군 유닛 정리
        }

        public void ToggleMenu() // 전투 메뉴 표시 전환
        {
            if (menuPanel == null || isTransitioning) // 메뉴 상태 확인
            {
                return; // 메뉴 전환 중단
            }

            menuPanel.SetActive(!menuPanel.activeSelf); // 메뉴 표시 상태 전환
        }

        public void ToggleAutoPreview() // AUTO 표시 상태 전환
        {
            if (isTransitioning) // 씬 전환 상태 확인
            {
                return; // AUTO 입력 중단
            }

            isAutoEnabled = !isAutoEnabled; // AUTO 표시 상태 반전
            RefreshAutoLabel(); // AUTO 버튼 표시 갱신
            SetText(statusText, isAutoEnabled ? "AUTO ON · 실제 자동 공격은 12일차부터 연결됩니다." : "AUTO OFF · 실제 전투 제어는 이후 단계에서 연결됩니다."); // AUTO 안내 표시
        }

        public void ReturnToDungeonSelect() // 던전 선택 화면 복귀
        {
            if (isTransitioning) // 기존 씬 전환 확인
            {
                return; // 중복 씬 전환 차단
            }

            if (GameManager.Instance == null || GameManager.Instance.Scenes == null) // 씬 로더 확인
            {
                SetText(statusText, "SceneLoader를 찾을 수 없습니다."); // 씬 로더 오류 표시
                return; // 던전 복귀 중단
            }

            isTransitioning = true; // 씬 전환 잠금 활성화
            SetInteraction(false); // 전투 UI 입력 잠금
            GameManager.Instance.Scenes.LoadScene(GameScenes.DungeonSelect); // 던전 선택 씬 이동
        }

        private void InitializeBattle() // 전투 배치 초기화
        {
            initialized = false; // 전투 초기화 상태 초기화
            elapsedSeconds = 0f; // 경과 시간 초기화
            menuPanel?.SetActive(false); // 시작 시 전투 메뉴 숨김
            SetText(waveText, "WAVE 1 / 3"); // 초기 웨이브 표시
            RefreshTime(); // 초기 시간 표시
            RefreshAutoLabel(); // 초기 AUTO 상태 표시
            HideAllHudCards(); // HUD 카드 초기 숨김
            ClearSpawnedUnits(); // 기존 생성 유닛 정리

            if (GameManager.Instance == null) // 게임 관리자 확인
            {
                FailInitialization("Bootstrap 씬부터 실행해 주세요."); // Bootstrap 실행 안내
                return; // 전투 초기화 중단
            }

            if (GameManager.Instance.Data == null || GameManager.Instance.Save == null) // 데이터 및 저장 관리자 확인
            {
                FailInitialization("DataManager 또는 SaveManager를 찾을 수 없습니다."); // 관리자 누락 안내
                return; // 전투 초기화 중단
            }

            SaveData saveData = GameManager.Instance.Save.CurrentSave; // 현재 저장 데이터 조회

            if (saveData == null) // 저장 데이터 확인
            {
                FailInitialization("전투에 사용할 진행 데이터가 없습니다."); // 저장 없음 안내
                return; // 전투 초기화 중단
            }

            if (!BattlePartyRuntime.TryCreate(GameManager.Instance.Data, saveData, out partyRuntime, out string runtimeError)) // 전투 파티 런타임 생성
            {
                FailInitialization(runtimeError); // 전투 파티 생성 오류 표시
                return; // 전투 초기화 중단
            }

            if (!BattleDeploymentPlan.TryCreate(partyRuntime, formationAnchors, out BattleDeploymentPlan plan, out string planError)) // 전장 배치 계획 생성
            {
                FailInitialization(planError); // 전장 배치 오류 표시
                return; // 전투 초기화 중단
            }

            if (unitTemplate == null || allyRoot == null) // 유닛 템플릿 및 생성 루트 확인
            {
                FailInitialization("Battle unit template or ally root is missing."); // 유닛 생성 구조 오류 표시
                return; // 전투 초기화 중단
            }

            if (hudCards == null || hudCards.Length < plan.Count) // HUD 카드 개수 확인
            {
                FailInitialization($"Not enough battle HUD cards. Required={plan.Count}."); // HUD 부족 오류 표시
                return; // 전투 초기화 중단
            }

            Camera targetCamera = Camera.main; // 메인 카메라 조회

            for (int index = 0; index < plan.Count; index++) // 배치 계획 순회
            {
                BattleDeploymentEntry entry = plan[index]; // 현재 배치 항목 조회
                BattleUnitView unit = Instantiate(unitTemplate, allyRoot); // 아군 전투 유닛 복제
                unit.gameObject.name = entry.Stats.RuntimeId; // 전투 유닛 객체 이름 설정
                unit.transform.position = entry.Anchor.position; // 전장 슬롯 위치 적용
                unit.transform.rotation = entry.Anchor.rotation; // 전장 슬롯 회전 적용
                unit.gameObject.SetActive(true); // 아군 전투 유닛 표시
                unit.SetWorldCamera(targetCamera); // 월드 UI 카메라 연결
                unit.Bind(entry.Stats); // 전투 스탯 연결
                spawnedUnits.Add(unit); // 생성 유닛 목록 등록
                hudCards[index].Bind(entry.Stats); // 하단 HUD 카드 연결
            }

            initialized = true; // 전투 초기화 완료 기록
            SetInteraction(true); // 전투 UI 입력 활성화
            SetText(statusText, $"파티 {plan.Count}명 배치 완료 · 기본 공격/타겟팅은 12일차 연결"); // 전투 배치 완료 표시
        }

        private void BindButtons() // 전투 UI 버튼 이벤트 연결
        {
            if (buttonsBound) // 기존 버튼 연결 확인
            {
                return; // 중복 이벤트 연결 중단
            }

            buttonsBound = true; // 버튼 연결 완료 기록
            menuButton?.onClick.AddListener(ToggleMenu); // 전투 메뉴 이벤트 연결
            autoButton?.onClick.AddListener(ToggleAutoPreview); // AUTO 이벤트 연결
            returnDungeonButton?.onClick.AddListener(ReturnToDungeonSelect); // 던전 복귀 이벤트 연결
            closeMenuButton?.onClick.AddListener(ToggleMenu); // 메뉴 닫기 이벤트 연결
        }

        private void RefreshTime() // 전투 경과 시간 표시
        {
            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(elapsedSeconds)); // 경과 시간 정수 변환
            int minutes = totalSeconds / 60; // 경과 분 계산
            int seconds = totalSeconds % 60; // 경과 초 계산
            SetText(timeText, $"{minutes:00}:{seconds:00}"); // 전투 시간 표시
        }

        private void RefreshAutoLabel() // AUTO 버튼 표시 갱신
        {
            SetText(autoButtonText, isAutoEnabled ? "AUTO ON" : "AUTO OFF"); // AUTO 활성 상태 표시
        }

        private void HideAllHudCards() // 전체 HUD 카드 숨김
        {
            if (hudCards == null) // HUD 카드 배열 확인
            {
                return; // HUD 숨김 중단
            }

            foreach (BattleHudCardView card in hudCards) // HUD 카드 순회
            {
                if (card != null) // HUD 카드 존재 확인
                {
                    card.SetVisible(false); // HUD 카드 숨김
                }
            }
        }

        private void FailInitialization(string message) // 전투 초기화 실패 처리
        {
            initialized = false; // 전투 초기화 실패 기록
            SetInteraction(false); // 전투 UI 입력 제한
            SetText(statusText, message); // 전투 초기화 오류 표시
            Debug.LogError($"[Project H][BATTLE] {message}"); // 전투 초기화 오류 로그
        }

        private void SetInteraction(bool enabled) // 전투 UI 입력 상태 설정
        {
            if (menuButton != null) // 메뉴 버튼 확인
            {
                menuButton.interactable = enabled; // 메뉴 버튼 상태 적용
            }

            if (autoButton != null) // AUTO 버튼 확인
            {
                autoButton.interactable = enabled; // AUTO 버튼 상태 적용
            }

            if (returnDungeonButton != null) // 던전 복귀 버튼 확인
            {
                returnDungeonButton.interactable = enabled; // 던전 복귀 버튼 상태 적용
            }

            if (closeMenuButton != null) // 메뉴 닫기 버튼 확인
            {
                closeMenuButton.interactable = enabled; // 메뉴 닫기 버튼 상태 적용
            }
        }

        private void ClearSpawnedUnits() // 생성 아군 유닛 정리
        {
            foreach (BattleUnitView unit in spawnedUnits) // 생성 유닛 목록 순회
            {
                if (unit != null) // 생성 유닛 존재 확인
                {
                    Destroy(unit.gameObject); // 생성 유닛 제거
                }
            }

            spawnedUnits.Clear(); // 생성 유닛 목록 초기화
        }

        private static void SetText(Text target, string value) // 텍스트 안전 설정
        {
            if (target != null) // 텍스트 참조 확인
            {
                target.text = value; // 텍스트 값 적용
            }
        }
    }
}
