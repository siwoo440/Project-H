using ProjectH.Battle.Rhythm; // 원형 스프라이트 기능
using ProjectH.Data; // 아이템·장비 데이터 기능
using ProjectH.SaveSystem; // 강화 주문서 해석 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class ItemIconView // 아이템 아이콘 (Day61 신규 — 정식 아이콘이 없으면 등급 테두리 + 유형 색 원 + 글자로 표시)
    {
        public static Image Create(Transform parent, string name) // 아이콘 틀 생성 (등급 테두리 + 안쪽 칸 + 원 + 글자)
        {
            Image frame = RuntimeUiKit.CreateImage(parent, name, new Color(0.35f, 0.36f, 0.40f, 1f)); // 등급 테두리
            frame.raycastTarget = false; // 입력 통과
            Image inner = RuntimeUiKit.CreateImage(frame.transform, "Inner", new Color(0.10f, 0.10f, 0.13f, 1f)); // 어두운 안쪽 칸
            inner.raycastTarget = false; // 입력 통과
            RuntimeUiKit.Stretch(inner.rectTransform, 4f); // 테두리 두께 4px
            Image disc = RuntimeUiKit.CreateImage(frame.transform, "Disc", Color.white); // 유형 색 원
            disc.sprite = RhythmCircleSpriteFactory.GetDiscSprite(); // 채움 원
            disc.raycastTarget = false; // 입력 통과
            RuntimeUiKit.Stretch(disc.rectTransform, 12f); // 안쪽 배치
            Image art = RuntimeUiKit.CreateImage(frame.transform, "Art", Color.white); // 정식 아이콘 자리
            art.raycastTarget = false; // 입력 통과
            art.preserveAspect = true; // 비율 유지
            RuntimeUiKit.Stretch(art.rectTransform, 8f); // 안쪽 배치
            Text symbol = RuntimeUiKit.CreateText(frame.transform, "Symbol", string.Empty, 30, Color.white).BestFit(10).Outlined(new Color(0f, 0f, 0f, 0.7f), new Vector2(1f, -1f)); // 유형 글자
            RuntimeUiKit.Stretch(symbol.rectTransform, 14f); // 안쪽 배치
            return frame; // 아이콘 반환
        }

        public static void Apply(Image frame, ItemData item, DataManager dataManager) // 아이콘에 아이템 적용 (null이면 빈 칸)
        {
            Image disc = frame.transform.Find("Disc").GetComponent<Image>(); // 원
            Image art = frame.transform.Find("Art").GetComponent<Image>(); // 정식 아이콘
            Text symbol = frame.transform.Find("Symbol").GetComponent<Text>(); // 글자

            if (item == null) // 빈 칸 확인
            {
                frame.color = new Color(0.30f, 0.30f, 0.34f, 0.8f); // 흐린 테두리
                disc.color = new Color(1f, 1f, 1f, 0.05f); // 흐린 원
                art.enabled = false; // 아이콘 숨김
                symbol.text = "+"; // 빈 칸 표시
                symbol.color = new Color(1f, 1f, 1f, 0.35f); // 흐린 글자
                return; // 처리 종료
            }

            frame.color = CharacterEquipmentScreenController.GetGradeColor(item.Grade); // 등급 테두리 색
            art.enabled = item.Icon != null; // 정식 아이콘 유무
            art.sprite = item.Icon; // 정식 아이콘 적용
            Color color = GetTypeColor(item); // 유형 색
            disc.color = art.enabled ? new Color(color.r, color.g, color.b, 0.15f) : new Color(color.r, color.g, color.b, 0.55f); // 정식 아이콘이면 흐리게
            symbol.text = art.enabled ? string.Empty : GetSymbol(item, dataManager); // 임시 글자
            symbol.color = Color.Lerp(color, Color.white, 0.7f); // 밝은 글자
        }

        public static string GetSymbol(ItemData item, DataManager dataManager) // 유형 글자 (정식 아이콘 전 임시)
        {
            if (EquipmentUpgradeCatalog.TryParseScroll(item.Id, out _, out ScrollGrade grade)) return grade.ToString(); // 주문서 등급 C·B·A
            if (item.Id.StartsWith("IT_RUNE_", System.StringComparison.Ordinal)) return "룬"; // 룬 조각·상자

            switch (item.Type) // 유형 분기
            {
                case ItemType.Equipment: // 장비
                    EquipmentData equipment = dataManager == null ? null : dataManager.GetEquipment(item.Id); // 장비 원본
                    return equipment == null ? "장" : EquipmentSlotInfo.GetLabel(equipment.Slot).Substring(0, 1); // 부위 첫 글자 (무·방·투·장·신)
                case ItemType.Consumable: return "약"; // 소비
                case ItemType.Material: return "재"; // 재료
                case ItemType.Gift: return "선"; // 선물
                case ItemType.Quest: return "!"; // 퀘스트
                default: return "?"; // 기타
            }
        }

        private static Color GetTypeColor(ItemData item) // 유형 색
        {
            if (EquipmentUpgradeCatalog.TryParseScroll(item.Id, out bool weapon, out _)) return weapon ? new Color(0.95f, 0.45f, 0.30f, 1f) : new Color(0.40f, 0.65f, 0.95f, 1f); // 무기 주문서 주황 · 방어구 주문서 파랑
            if (item.Id.StartsWith("IT_RUNE_", System.StringComparison.Ordinal)) return new Color(0.70f, 0.50f, 1f, 1f); // 룬 보라

            switch (item.Type) // 유형 분기
            {
                case ItemType.Equipment: return new Color(0.80f, 0.82f, 0.88f, 1f); // 장비 은색
                case ItemType.Consumable: return new Color(0.40f, 0.90f, 0.50f, 1f); // 소비 초록
                case ItemType.Material: return new Color(0.80f, 0.62f, 0.40f, 1f); // 재료 갈색
                case ItemType.Gift: return new Color(1f, 0.55f, 0.75f, 1f); // 선물 분홍
                default: return new Color(1f, 0.85f, 0.35f, 1f); // 기타 금색
            }
        }
    }
}
