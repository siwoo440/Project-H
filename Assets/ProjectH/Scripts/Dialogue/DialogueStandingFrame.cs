using UnityEngine; // 벡터 기능

namespace ProjectH.Dialogue // 프로젝트 대화 영역
{
    public static class DialogueStandingFrame // 대화 화면 스탠딩 구도 (Day77 신규 — 배율 하나로 캐릭터 크기를 정한다)
    {
        public const float Scale = 1.4f; // 확대 배율 — 이 숫자만 바꾸면 구도가 바뀐다 (1 = 전신 · 1.4 = 허벅지까지 · 1.7 = 허리까지)
        public const float SideTop = 0.95f; // 좌우 자리의 그림 위쪽 높이 (화면 높이 비율)
        public const float CenterTop = 0.97f; // 가운데 자리의 그림 위쪽 높이 (1인 대화)
        public const float HalfWidth = 0.40f; // 영역 반폭 (그림은 비율을 지켜 가운데에 그려지므로 넉넉하게 잡는다)
        public static readonly Vector2 Pivot = new Vector2(0.5f, 1f); // 위쪽 가운데 기준 (듣는 쪽을 줄여도 머리 위치가 흔들리지 않는다)

        public static float GetCenterX(DialogueStageSlot slot) // 자리별 가로 중심
        {
            return slot == DialogueStageSlot.Left ? 0.23f : slot == DialogueStageSlot.Right ? 0.77f : 0.5f; // 왼쪽 · 오른쪽 · 가운데
        }

        public static float GetTop(DialogueStageSlot slot) // 자리별 그림 위쪽 높이
        {
            return slot == DialogueStageSlot.Center ? CenterTop : SideTop; // 가운데는 조금 더 위
        }

        public static void GetAnchors(DialogueStageSlot slot, out Vector2 min, out Vector2 max) // 자리별 영역 (위쪽은 고정하고 배율만큼 아래로 키운다)
        {
            float top = GetTop(slot); // 그림 위쪽
            float centerX = GetCenterX(slot); // 가로 중심
            min = new Vector2(centerX - HalfWidth, top - (top * Scale)); // 왼쪽 아래 (배율이 1보다 크면 화면 아래로 내려가 대사창 뒤에 가려진다)
            max = new Vector2(centerX + HalfWidth, top); // 오른쪽 위
        }

        public static void GrowDown(Vector2 baseMin, Vector2 baseMax, out Vector2 min, out Vector2 max) // 전신이 들어가던 영역을 위쪽은 그대로 두고 배율만큼 아래로 키움 (상점 · 대장간 NPC가 대화 화면과 같은 배율을 쓴다)
        {
            min = new Vector2(baseMin.x, baseMax.y - ((baseMax.y - baseMin.y) * Scale)); // 아래쪽만 내려감
            max = baseMax; // 위쪽 고정
        }
    }
}
