using ProjectH.UI; // 그림 불러오기·스탠딩 조회 기능
using UnityEngine; // 스프라이트·벡터 기능
using UnityEngine.UI; // Image·Text 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleUnitArt // 전투 유닛 그림 (Day78 신규 — Resources/BattleUnits/{캐릭터ID·몬스터ID}.png의 SD 그림 → 아군은 스탠딩 → 색 상자)
    {
        public const string Folder = "BattleUnits/"; // SD 그림 Resources 경로
        public const string BossPrefix = "MON_BOSS_"; // 보스 몬스터 ID 접두어
        public const float BossScale = 1.5f; // 보스 그림 확대 배율 (일반 몬스터의 몇 배로 보일지)
        public static readonly Vector2 SpriteAnchorMin = new Vector2(-0.10f, 0.20f); // 그림을 쓸 때 바디 영역 왼쪽 아래 (체력바 바로 위)
        public static readonly Vector2 SpriteAnchorMax = new Vector2(1.10f, 0.86f); // 그림을 쓸 때 바디 영역 오른쪽 위 (역할 글자 아래)
        public static readonly Vector2 SpritePivot = new Vector2(0.5f, 0f); // 발 쪽 기준 (그림이 줄어들거나 커져도 발이 바닥에 붙는다)
        public static readonly Color HitTint = new Color(1f, 0.42f, 0.42f, 1f); // 그림 유닛의 피격 색 (흰색 번쩍임은 그림에서 보이지 않는다)

        public static Sprite GetSd(string unitId) // SD 그림 조회 (없으면 null)
        {
            return string.IsNullOrEmpty(unitId) ? null : RuntimeSpriteLoader.Load(Folder + unitId); // 캐릭터·몬스터 공용
        }

        public static Sprite GetAlly(string characterId) // 아군 그림 : SD → 기본 표정 스탠딩 → null(색 상자)
        {
            Sprite sd = GetSd(characterId); // SD 그림
            if (sd != null) return sd; // SD 우선
            if (string.IsNullOrEmpty(characterId)) return null; // 빈 ID
            Sprite standing = DialogueArtFactory.GetStanding(characterId, null, out bool placeholder); // 스탠딩 전신
            return placeholder ? null : standing; // 정식 스탠딩만 사용 (실루엣이면 색 상자가 낫다)
        }

        public static Sprite GetEnemy(string monsterId) // 적군 그림 : SD → null(색 상자)
        {
            return GetSd(monsterId); // 몬스터는 SD 그림만
        }

        public static bool IsBoss(string monsterId) // 보스 몬스터인지 (그림을 크게 그린다)
        {
            return !string.IsNullOrEmpty(monsterId) && monsterId.StartsWith(BossPrefix, System.StringComparison.Ordinal); // ID 접두어로 판정
        }

        public static void GetSpriteAnchors(float scale, out Vector2 min, out Vector2 max) // 그림 영역 (발 위치와 가로 중심은 그대로 두고 배율만큼 키운다)
        {
            float halfWidth = (SpriteAnchorMax.x - SpriteAnchorMin.x) * 0.5f * scale; // 반폭
            float centerX = (SpriteAnchorMin.x + SpriteAnchorMax.x) * 0.5f; // 가로 중심
            min = new Vector2(centerX - halfWidth, SpriteAnchorMin.y); // 왼쪽 아래 (발 높이 고정)
            max = new Vector2(centerX + halfWidth, SpriteAnchorMin.y + ((SpriteAnchorMax.y - SpriteAnchorMin.y) * scale)); // 오른쪽 위
        }

        public static bool ApplyTo(Image body, Sprite sprite, float scale = 1f) // 바디에 그림 적용 (그림이 없으면 false — 색 상자를 그대로 둔다)
        {
            if (body == null || sprite == null) return false; // 대상·그림 없음
            body.sprite = sprite; // 그림 적용
            body.color = Color.white; // 원래 색 그대로
            body.preserveAspect = true; // 비율 유지
            GetSpriteAnchors(scale, out Vector2 min, out Vector2 max); // 그림 영역
            RectTransform rect = body.rectTransform; // 바디 영역
            rect.pivot = SpritePivot; // 발 쪽 기준
            rect.anchorMin = min; // 왼쪽 아래
            rect.anchorMax = max; // 오른쪽 위
            rect.offsetMin = Vector2.zero; // 여백 없음
            rect.offsetMax = Vector2.zero; // 여백 없음
            return true; // 적용함
        }

        public static void PlaceNameAbove(Text label, Image body) // 이름을 그림 머리 위 작은 글자로 옮김 (색 상자 안에 있던 이름)
        {
            if (label == null || body == null) return; // 대상 없음
            RectTransform rect = label.rectTransform; // 이름 영역
            rect.anchorMin = new Vector2(0f, 1f); // 그림 위쪽에 붙임
            rect.anchorMax = new Vector2(1f, 1f); // 가로 전체
            rect.pivot = new Vector2(0.5f, 0f); // 아래 기준
            rect.sizeDelta = new Vector2(0f, 26f); // 한 줄 높이
            rect.anchoredPosition = Vector2.zero; // 머리 바로 위
            label.alignment = TextAnchor.LowerCenter; // 아래 가운데 정렬
            label.fontSize = 20; // 작은 글자
            label.resizeTextForBestFit = true; // 긴 이름은 줄여서
            label.resizeTextMinSize = 12; // 최소 크기
            label.resizeTextMaxSize = 20; // 최대 크기
            label.horizontalOverflow = HorizontalWrapMode.Wrap; // 영역 안에서
            label.verticalOverflow = VerticalWrapMode.Truncate; // 한 줄
            label.color = Color.white; // 흰 글자

            if (label.GetComponent<Outline>() == null) // 외곽선 확인
            {
                Outline outline = label.gameObject.AddComponent<Outline>(); // 배경 위에서 읽히도록 외곽선
                outline.effectColor = new Color(0f, 0f, 0f, 0.85f); // 검은 외곽선
                outline.effectDistance = new Vector2(1.5f, -1.5f); // 두께
            }
        }
    }
}
