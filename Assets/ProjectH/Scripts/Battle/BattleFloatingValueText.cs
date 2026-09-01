using System.Collections; // 코루틴 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 체력 변화 텍스트 방지
    public sealed class BattleFloatingValueText : MonoBehaviour // 피해 회복 숫자 디버그 텍스트
    {
        [SerializeField] private Text valueText; // 체력 변화 숫자 텍스트
        [SerializeField, Min(0.05f)] private float visibleSeconds = 0.75f; // 체력 변화 숫자 표시 시간
        private Coroutine hideRoutine; // 숨김 코루틴 참조

        public void Configure(Text targetText) // 에디터 텍스트 참조 설정
        {
            valueText = targetText; // 체력 변화 텍스트 연결

            if (valueText != null) // 체력 변화 텍스트 확인
            {
                valueText.enabled = false; // 초기 체력 변화 텍스트 숨김
            }
        }

        public void ShowDamage(int amount) // 피해 숫자 표시
        {
            ShowValue($"-{Mathf.Max(0, amount)}", new Color(1f, 0.35f, 0.30f, 1f)); // 붉은 피해 숫자 표시
        }

        public void ShowHealing(int amount) // 회복 숫자 표시
        {
            ShowValue($"+{Mathf.Max(0, amount)}", new Color(0.38f, 1f, 0.52f, 1f)); // 초록 회복 숫자 표시
        }

        private void ShowValue(string value, Color color) // 체력 변화 숫자 공통 표시
        {
            if (valueText == null) // 체력 변화 텍스트 확인
            {
                return; // 체력 변화 표시 중단
            }

            if (hideRoutine != null) // 기존 숨김 코루틴 확인
            {
                StopCoroutine(hideRoutine); // 기존 숨김 코루틴 중단
            }

            valueText.text = value; // 체력 변화 숫자 적용
            valueText.color = color; // 체력 변화 숫자 색상 적용
            valueText.enabled = true; // 체력 변화 숫자 표시
            hideRoutine = StartCoroutine(HideAfterDelay()); // 체력 변화 숫자 자동 숨김 시작
        }

        private IEnumerator HideAfterDelay() // 체력 변화 숫자 자동 숨김
        {
            yield return new WaitForSeconds(visibleSeconds); // 체력 변화 숫자 표시 시간 대기

            if (valueText != null) // 체력 변화 텍스트 존재 확인
            {
                valueText.enabled = false; // 체력 변화 숫자 숨김
            }

            hideRoutine = null; // 숨김 코루틴 참조 초기화
        }
    }
}
