using UnityEngine; // Unity UI 좌표 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleDay17HudLayout // 17일차 프로필·스킬 블록 HUD 분할 배치 기능
    {
        public const int ProfileCardCount = 4; // 프로필 카드 개수
        public const float ProfileAreaMaxX = 0.425f; // 프로필 왼쪽 영역 최대 X
        public const float SkillAreaMinX = 0.445f; // 스킬 블록 오른쪽 영역 최소 X
        private const float ProfileStartX = 0.020f; // 첫 프로필 카드 시작 X
        private const float ProfileCardWidth = 0.092f; // 축소 프로필 카드 너비
        private const float ProfileCardGap = 0.008f; // 프로필 카드 사이 간격
        private const float ProfileMinY = 0.015f; // 프로필 카드 하단 Y
        private const float ProfileMaxY = 0.225f; // 프로필 카드 상단 Y
        public static readonly Rect SkillPanelAnchorRect = new Rect(SkillAreaMinX, 0.020f, 0.535f, 0.195f); // 스킬 블록 오른쪽 하단 영역

        public static Rect GetProfileCardAnchorRect(int index) // 프로필 카드 인덱스별 앵커 Rect 계산
        {
            int safeIndex = Mathf.Clamp(index, 0, ProfileCardCount - 1); // 프로필 카드 인덱스 범위 보정
            float minX = ProfileStartX + (safeIndex * (ProfileCardWidth + ProfileCardGap)); // 프로필 카드 최소 X 계산
            return new Rect(minX, ProfileMinY, ProfileCardWidth, ProfileMaxY - ProfileMinY); // 프로필 카드 앵커 Rect 반환
        }

        public static void ApplyProfileCard(RectTransform rect, int index) // 프로필 카드 왼쪽 하단 배치 적용
        {
            if (rect == null) // 프로필 카드 RectTransform 확인
            {
                return; // 프로필 카드 배치 중단
            }

            ApplyAnchorRect(rect, GetProfileCardAnchorRect(index)); // 인덱스별 프로필 카드 Rect 적용
        }

        public static void ApplySkillPanel(RectTransform rect) // 스킬 블록 패널 오른쪽 하단 배치 적용
        {
            if (rect == null) // 스킬 블록 패널 RectTransform 확인
            {
                return; // 스킬 블록 패널 배치 중단
            }

            ApplyAnchorRect(rect, SkillPanelAnchorRect); // 스킬 블록 오른쪽 하단 Rect 적용
        }

        private static void ApplyAnchorRect(RectTransform rect, Rect anchorRect) // 정규화 앵커 Rect 적용
        {
            rect.anchorMin = new Vector2(anchorRect.xMin, anchorRect.yMin); // 최소 앵커 적용
            rect.anchorMax = new Vector2(anchorRect.xMax, anchorRect.yMax); // 최대 앵커 적용
            rect.offsetMin = Vector2.zero; // 최소 오프셋 초기화
            rect.offsetMax = Vector2.zero; // 최대 오프셋 초기화
        }
    }
}
