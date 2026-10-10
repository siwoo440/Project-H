using ProjectH.Battle.Rhythm; // 원형 스프라이트 기능
using ProjectH.SaveSystem; // 룬 정의 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class RuneIconView // 룬 아이콘 (Day60 신규 — 정식 아이콘 전까지 원형 발광 + 룬 글자로 표시)
    {
        public static Color GetColor(RuneKind kind) // 룬 분류 색
        {
            switch (RuneCatalog.Get(kind).Category) // 분류 분기
            {
                case RuneCategory.Attack: return new Color(1f, 0.38f, 0.36f, 1f); // 공격 붉은색
                case RuneCategory.Defense: return new Color(0.38f, 0.68f, 1f, 1f); // 방어 푸른색
                case RuneCategory.Recovery: return new Color(0.42f, 0.95f, 0.55f, 1f); // 회복 초록색
                default: return new Color(1f, 0.45f, 0.80f, 1f); // 특수 분홍색
            }
        }

        public static Image Create(Transform parent, string name) // 아이콘 틀 생성 (어두운 칸 + 발광 원 + 글자)
        {
            Image frame = RuntimeUiKit.CreateImage(parent, name, new Color(0.10f, 0.10f, 0.14f, 1f)); // 어두운 칸
            if (UiSkin.Apply(frame, UiSkin.Slot, 3f)) frame.color = Color.white; // 정식 칸 (Day85 — 아이템 칸과 같은 은테)
            frame.raycastTarget = false; // 입력 통과
            Image glow = RuntimeUiKit.CreateImage(frame.transform, "Glow", Color.white); // 발광 원
            glow.sprite = RhythmCircleSpriteFactory.GetDiscSprite(); // 채움 원
            glow.raycastTarget = false; // 입력 통과
            RuntimeUiKit.Stretch(glow.rectTransform, 10f); // 안쪽 배치
            Image ring = RuntimeUiKit.CreateImage(frame.transform, "Ring", Color.white); // 테두리 링
            ring.sprite = RhythmCircleSpriteFactory.GetRingSprite(); // 링 스프라이트
            ring.raycastTarget = false; // 입력 통과
            RuntimeUiKit.Stretch(ring.rectTransform, 8f); // 안쪽 배치
            Image art = RuntimeUiKit.CreateImage(frame.transform, "Art", Color.white); // 정식 룬 그림 자리 (Day82 추가)
            art.raycastTarget = false; // 입력 통과
            art.preserveAspect = true; // 비율 유지
            art.enabled = false; // 그림이 정해질 때까지 숨김
            RuntimeUiKit.Stretch(art.rectTransform, 4f); // 안쪽 배치
            Text symbol = RuntimeUiKit.CreateText(frame.transform, "Symbol", string.Empty, 24, Color.white).BestFit(10).Outlined(new Color(0f, 0f, 0f, 0.6f), new Vector2(1f, -1f)); // 룬 글자
            RuntimeUiKit.Stretch(symbol.rectTransform, 12f); // 안쪽 배치
            return frame; // 아이콘 반환
        }

        public static void Apply(Image frame, RuneInstanceSaveData rune) // 아이콘에 룬 적용 (null이면 빈 칸)
        {
            Image glow = frame.transform.Find("Glow").GetComponent<Image>(); // 발광 원
            Image ring = frame.transform.Find("Ring").GetComponent<Image>(); // 링
            Text symbol = frame.transform.Find("Symbol").GetComponent<Text>(); // 글자
            Image art = frame.transform.Find("Art").GetComponent<Image>(); // 정식 룬 그림 (Day82)
            Sprite icon = rune == null ? null : UiIcon.GetRune(rune.Kind); // 룬 그림 (없으면 null)
            art.sprite = icon; // 그림 적용
            art.enabled = icon != null; // 그림 유무

            if (rune == null) // 빈 칸 확인
            {
                glow.color = new Color(1f, 1f, 1f, 0.04f); // 흐린 원
                ring.color = new Color(1f, 1f, 1f, 0.15f); // 흐린 링
                symbol.text = string.Empty; // 글자 없음
                return; // 처리 종료
            }

            Color color = GetColor(rune.Kind); // 분류 색
            glow.color = new Color(color.r, color.g, color.b, 0.18f + (0.12f * rune.Grade)); // 등급이 높을수록 밝게
            ring.color = color; // 링 색
            symbol.text = RuneCatalog.Get(rune.Kind).Symbol; // 룬 글자
            symbol.color = Color.Lerp(color, Color.white, 0.55f); // 밝은 글자
            if (icon == null) return; // 그림 없음 — 발광 원과 글자 그대로
            ring.color = new Color(color.r, color.g, color.b, 0f); // 그림이 있으면 링 숨김 (Day82)
            symbol.text = string.Empty; // 글자 숨김 (발광 원은 등급 표시로 그림 뒤에 남긴다)
        }
    }
}
