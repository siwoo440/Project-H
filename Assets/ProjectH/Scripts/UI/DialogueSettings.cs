using ProjectH.Core; // 통합 환경 설정 기능
using UnityEngine; // 수학 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class DialogueSettings // 대화 글자·자동 넘김 속도 (Day62 신규 → Day72 통합 설정(GameSettings)을 그대로 따른다)
    {
        public const float BaseCharsPerSecond = 45f; // 대사 속도 ×1.00일 때 초당 글자 수
        public const float BasePerCharDelay = 0.035f; // 자동 넘김에서 글자 하나당 더 기다리는 시간 (초)

        public static float GetCharsPerSecond() // 지금 설정에 따른 초당 글자 수
        {
            return BaseCharsPerSecond * Mathf.Max(0.1f, GameSettings.DialogueSpeed); // 대사 속도 배수 적용
        }

        public static float GetAutoDelay(int textLength) // 대사 길이에 따른 자동 넘김 대기 시간
        {
            float perChar = BasePerCharDelay / Mathf.Max(0.1f, GameSettings.DialogueSpeed); // 빠를수록 글자당 대기도 짧아짐
            return GameSettings.AutoAdvanceSeconds + (perChar * Mathf.Max(0, textLength)); // 기본 대기 + 길이 보정
        }

        public static string GetSpeedText() // 대화 화면에 보여 줄 현재 속도 문구
        {
            return $"대사 속도 ×{GameSettings.DialogueSpeed:0.00} · 자동 넘김 {GameSettings.AutoAdvanceSeconds:0.0}초"; // 문구 반환
        }
    }
}
