using UnityEngine; // PlayerPrefs·수학 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class DialogueSettings // 대화 글자·자동 넘김 속도 (Day62 신규 — 대화 화면 ⚙, 전체 설정 화면은 Day72)
    {
        private const string TextSpeedKey = "ProjectH.Dialogue.TextSpeed"; // 글자 속도 저장 키
        private const string AutoSpeedKey = "ProjectH.Dialogue.AutoSpeed"; // 자동 넘김 속도 저장 키
        public static readonly string[] SpeedLabels = { "느림", "보통", "빠름" }; // 단계 이름
        private static readonly float[] CharsPerSecond = { 25f, 45f, 90f }; // 글자 속도 단계별 초당 글자
        private static readonly float[] AutoBaseDelay = { 1.8f, 1.0f, 0.5f }; // 자동 넘김 기본 대기 (초)
        private static readonly float[] AutoPerChar = { 0.05f, 0.035f, 0.02f }; // 자동 넘김 글자당 추가 대기 (초)

        public static int TextSpeed { get => Load(TextSpeedKey); set => Store(TextSpeedKey, value); } // 글자 속도 단계 0~2
        public static int AutoSpeed { get => Load(AutoSpeedKey); set => Store(AutoSpeedKey, value); } // 자동 넘김 단계 0~2

        public static float GetCharsPerSecond(int level) => CharsPerSecond[Clamp(level)]; // 단계별 글자 속도
        public static float GetAutoDelay(int level, int textLength) => AutoBaseDelay[Clamp(level)] + (AutoPerChar[Clamp(level)] * Mathf.Max(0, textLength)); // 대사 길이별 자동 대기

        private static int Clamp(int level) => Mathf.Clamp(level, 0, SpeedLabels.Length - 1); // 범위 보정

        private static int Load(string key) // 저장값 읽기 (기본 보통)
        {
            try // 저장소 접근 실패 대비
            {
                return Clamp(PlayerPrefs.GetInt(key, 1)); // 저장값
            }
            catch (UnityException) // 접근 불가 (메인 스레드 외 등)
            {
                return 1; // 보통
            }
        }

        private static void Store(string key, int value) // 저장값 쓰기
        {
            PlayerPrefs.SetInt(key, Clamp(value)); // 값 저장
            PlayerPrefs.Save(); // 즉시 기록
        }
    }
}
