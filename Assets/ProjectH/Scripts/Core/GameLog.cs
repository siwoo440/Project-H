using System.Diagnostics; // 조건부 컴파일 속성

namespace ProjectH.Core // 프로젝트 핵심 영역
{
    public static class GameLog // 로그 창구 (Day74 신규 — 안내 로그는 출시 빌드에서 아예 빠진다)
    {
        [Conditional("UNITY_EDITOR")] // 에디터에서만 컴파일
        [Conditional("DEVELOPMENT_BUILD")] // 개발 빌드에서만 컴파일
        public static void Info(string message) // 진행 안내 (출시 빌드에서는 호출도, 문자열 조립도 사라진다)
        {
            UnityEngine.Debug.Log(message); // 콘솔 출력
        }

        public static void Warn(string message) // 경고 (출시 빌드에도 남긴다 — 원인 추적에 필요)
        {
            UnityEngine.Debug.LogWarning(message); // 콘솔 출력
        }

        public static void Error(string message) // 오류 (출시 빌드에도 남긴다)
        {
            UnityEngine.Debug.LogError(message); // 콘솔 출력
        }
    }
}
