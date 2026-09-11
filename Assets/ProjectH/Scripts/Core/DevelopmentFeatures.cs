using UnityEngine; // Unity 기본 기능

namespace ProjectH.Core // 프로젝트 핵심 영역
{
    public static class DevelopmentFeatures // 개발·테스트 전용 기능 표시 여부 (최적화 — 출시 빌드에서 디버그 버튼 숨김)
    {
        public static bool Enabled => Debug.isDebugBuild; // 에디터와 Development Build에서만 true

        public static void HideInRelease(GameObject target) // 출시 빌드에서 대상 숨김 (씬 참조 유지를 위해 삭제 대신 비활성화)
        {
            if (!Enabled && target != null) // 출시 빌드 및 대상 확인
            {
                target.SetActive(false); // 개발 전용 UI 숨김
            }
        }

        public static void HideInRelease(Component target) // 컴포넌트 기준 숨김 편의 함수
        {
            if (target != null) // 대상 확인
            {
                HideInRelease(target.gameObject); // 소속 객체 숨김
            }
        }
    }
}
