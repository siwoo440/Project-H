using UnityEngine; // Unity UI 좌표 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleTopHudLayout // 전투 상단 HUD 공통 배치 기능
    {
        private static readonly Vector2 TimePanelAnchorMin = new Vector2(0.455f, 0.92f); // TimePanel 상단 중앙 최소 앵커
        private static readonly Vector2 TimePanelAnchorMax = new Vector2(0.545f, 0.985f); // TimePanel 상단 중앙 최대 앵커

        public static void ApplyTimePanel(RectTransform rect) // TimePanel 상단 중앙 배치 적용
        {
            if (rect == null) // TimePanel RectTransform 확인
            {
                return; // TimePanel 배치 중단
            }

            rect.anchorMin = TimePanelAnchorMin; // TimePanel 최소 앵커 적용
            rect.anchorMax = TimePanelAnchorMax; // TimePanel 최대 앵커 적용
            rect.offsetMin = Vector2.zero; // TimePanel 최소 오프셋 초기화
            rect.offsetMax = Vector2.zero; // TimePanel 최대 오프셋 초기화
        }
    }
}
