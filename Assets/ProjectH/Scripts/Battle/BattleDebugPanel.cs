using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 전투 디버그 패널 방지
    public sealed class BattleDebugPanel : MonoBehaviour // 전투 개발 UI 표시 컨트롤러
    {
        [SerializeField] private GameObject panelRoot; // 전투 디버그 패널 루트
        [SerializeField] private Button toggleButton; // 전투 디버그 표시 버튼
        [SerializeField] private Text toggleText; // 전투 디버그 버튼 텍스트
        private bool buttonBound; // 디버그 버튼 연결 상태
        public bool IsVisible => panelRoot != null && panelRoot.activeSelf; // 현재 디버그 패널 표시 상태

        public void Configure(GameObject root, Button toggleTarget, Text toggleLabel) // 전투 디버그 UI 참조 설정
        {
            panelRoot = root; // 전투 디버그 패널 연결
            toggleButton = toggleTarget; // 전투 디버그 버튼 연결
            toggleText = toggleLabel; // 전투 디버그 버튼 문구 연결
        }

        private void Start() // 전투 디버그 패널 시작
        {
            BindButton(); // 전투 디버그 버튼 이벤트 연결
            SetVisible(false); // 시작 시 전투 디버그 UI 숨김
        }

        public void Toggle() // 전투 디버그 UI 표시 전환
        {
            SetVisible(!IsVisible); // 현재 디버그 표시 상태 반전
        }

        public void SetVisible(bool visible) // 전투 디버그 UI 표시 상태 설정
        {
            if (panelRoot != null) // 전투 디버그 패널 확인
            {
                panelRoot.SetActive(visible); // 전투 디버그 패널 표시 상태 적용
            }

            if (toggleText != null) // 전투 디버그 버튼 텍스트 확인
            {
                toggleText.text = visible ? "DEBUG ON" : "DEBUG"; // 디버그 표시 상태 문구 적용
            }

            SetWorldDebugVisible(visible); // 전장 Runtime 디버그 정보 표시 상태 적용
        }

        private void BindButton() // 전투 디버그 버튼 이벤트 연결
        {
            if (buttonBound || toggleButton == null) // 기존 연결 및 버튼 존재 확인
            {
                return; // 중복 디버그 버튼 연결 중단
            }

            toggleButton.onClick.AddListener(Toggle); // 전투 디버그 표시 전환 이벤트 연결
            buttonBound = true; // 전투 디버그 버튼 연결 완료 기록
        }

        private static void SetWorldDebugVisible(bool visible) // 전장 Runtime 디버그 정보 표시 설정
        {
            BattleUnitView[] allyViews = Object.FindObjectsByType<BattleUnitView>(FindObjectsInactive.Include, FindObjectsSortMode.None); // 전체 아군 View 조회

            for (int index = 0; index < allyViews.Length; index++) // 아군 View 순회
            {
                BattleUnitView view = allyViews[index]; // 현재 아군 View 조회

                if (view != null) // 아군 View 존재 확인
                {
                    view.SetDebugInfoVisible(visible); // 아군 Runtime 디버그 표시 적용
                }
            }

            BattleEnemyView[] enemyViews = Object.FindObjectsByType<BattleEnemyView>(FindObjectsInactive.Include, FindObjectsSortMode.None); // 전체 적군 View 조회

            for (int index = 0; index < enemyViews.Length; index++) // 적군 View 순회
            {
                BattleEnemyView view = enemyViews[index]; // 현재 적군 View 조회

                if (view != null) // 적군 View 존재 확인
                {
                    view.SetDebugInfoVisible(visible); // 적군 Runtime 디버그 표시 적용
                }
            }
        }

        private void OnDestroy() // 전투 디버그 패널 제거 처리
        {
            if (buttonBound && toggleButton != null) // 디버그 버튼 연결 여부 확인
            {
                toggleButton.onClick.RemoveListener(Toggle); // 디버그 버튼 이벤트 해제
            }

            buttonBound = false; // 디버그 버튼 연결 상태 초기화
        }
    }
}
