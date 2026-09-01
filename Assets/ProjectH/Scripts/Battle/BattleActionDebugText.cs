using System.Collections; // 코루틴 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 디버그 텍스트 방지
    public sealed class BattleActionDebugText : MonoBehaviour // 전투 행동 디버그 텍스트
    {
        [SerializeField] private Text actionText; // 행동 표시 텍스트
        [SerializeField, Min(0.05f)] private float visibleSeconds = 0.65f; // 행동 텍스트 표시 시간
        private Coroutine hideRoutine; // 숨김 코루틴 참조

        public void Configure(Text targetText) // 에디터 텍스트 참조 설정
        {
            actionText = targetText; // 행동 텍스트 연결

            if (actionText != null) // 행동 텍스트 확인
            {
                actionText.enabled = false; // 초기 행동 텍스트 숨김
            }
        }

        public void Show(BattleActionKind actionKind) // 전투 행동 텍스트 표시
        {
            if (actionText == null) // 행동 텍스트 참조 확인
            {
                return; // 행동 텍스트 표시 중단
            }

            if (hideRoutine != null) // 기존 숨김 코루틴 확인
            {
                StopCoroutine(hideRoutine); // 기존 숨김 코루틴 중단
            }

            actionText.text = GetLabel(actionKind); // 행동 라벨 적용
            actionText.color = GetColor(actionKind); // 행동 라벨 색상 적용
            actionText.enabled = true; // 행동 텍스트 표시
            hideRoutine = StartCoroutine(HideAfterDelay()); // 행동 텍스트 자동 숨김 시작
        }

        public static string GetLabel(BattleActionKind actionKind) // 행동 종류별 디버그 라벨 반환
        {
            switch (actionKind) // 행동 종류 분기
            {
                case BattleActionKind.Skill: // 스킬 행동 처리
                    return "스킬!"; // 스킬 라벨 반환
                case BattleActionKind.Ultimate: // 궁극기 행동 처리
                    return "궁극기!"; // 궁극기 라벨 반환
                default: // 기본 공격 행동 처리
                    return "공격!"; // 기본 공격 라벨 반환
            }
        }

        private IEnumerator HideAfterDelay() // 행동 텍스트 자동 숨김
        {
            yield return new WaitForSeconds(visibleSeconds); // 행동 텍스트 표시 시간 대기

            if (actionText != null) // 행동 텍스트 존재 확인
            {
                actionText.enabled = false; // 행동 텍스트 숨김
            }

            hideRoutine = null; // 숨김 코루틴 참조 초기화
        }

        private static Color GetColor(BattleActionKind actionKind) // 행동 종류별 디버그 색상 반환
        {
            switch (actionKind) // 행동 종류 분기
            {
                case BattleActionKind.Skill: // 스킬 행동 처리
                    return new Color(0.38f, 0.88f, 1f, 1f); // 스킬 청색 반환
                case BattleActionKind.Ultimate: // 궁극기 행동 처리
                    return new Color(1f, 0.58f, 0.92f, 1f); // 궁극기 분홍색 반환
                default: // 기본 공격 행동 처리
                    return new Color(1f, 0.90f, 0.32f, 1f); // 기본 공격 황색 반환
            }
        }
    }
}
