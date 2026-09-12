using System.Collections.Generic; // 목록 자료형
using ProjectH.Data; // 속성 기능

namespace ProjectH.Village // 프로젝트 마을 영역
{
    public enum GuildQuestKind // 길드 의뢰 종류 (Day67 신규)
    {
        ClearDungeon = 0, // 지정 던전 클리어
        WinBattles = 1, // 전투 승리 횟수
        ClearRift = 2, // 검은 균열 막기
        WinWithElement = 3 // 지정 속성 동료를 넣고 승리
    }

    public sealed class GuildQuestDefinition // 길드 의뢰 하나 (Day67 신규 — 수치는 이 파일에서 조정)
    {
        public string Id { get; } // 의뢰 ID
        public GuildQuestKind Kind { get; } // 종류
        public string Target { get; } // 대상 (던전 ID 등)
        public ElementType Element { get; } // 대상 속성 (WinWithElement 전용)
        public int Required { get; } // 목표 수
        public int RewardGold { get; } // 보상 골드
        public string RewardItemId { get; } // 보상 아이템 (없으면 빈 값)
        public int RewardItemCount { get; } // 보상 아이템 수량
        public int RewardBondResource { get; } // 보상 결속 자원

        public GuildQuestDefinition(string id, GuildQuestKind kind, string target, int required, int rewardGold, string rewardItemId = "", int rewardItemCount = 0, int rewardBondResource = 0, ElementType element = ElementType.None) // 의뢰 생성
        {
            Id = id; // ID 저장
            Kind = kind; // 종류 저장
            Target = target ?? string.Empty; // 대상 저장
            Required = required < 1 ? 1 : required; // 목표 저장
            RewardGold = rewardGold < 0 ? 0 : rewardGold; // 골드 저장
            RewardItemId = rewardItemId ?? string.Empty; // 아이템 저장
            RewardItemCount = rewardItemCount < 0 ? 0 : rewardItemCount; // 수량 저장
            RewardBondResource = rewardBondResource < 0 ? 0 : rewardBondResource; // 결속 자원 저장
            Element = element; // 대상 속성 저장 (Day68 수정 — 저장하지 않아 속성 의뢰가 진행되지 않았음)
        }

        public string RewardText // 보상 문구
        {
            get
            {
                string text = $"{RewardGold}G"; // 골드
                if (!string.IsNullOrEmpty(RewardItemId) && RewardItemCount > 0) text += $" · {GuildQuestCatalog.GetItemLabel(RewardItemId)} {RewardItemCount}"; // 아이템
                if (RewardBondResource > 0) text += $" · 결속 자원 {RewardBondResource}"; // 결속 자원
                return text; // 문구 반환
            }
        }
    }

    public static class GuildQuestCatalog // 길드 의뢰 목록 (Day67 신규 — 하루 3개, 날짜가 바뀌면 새 의뢰)
    {
        public const int DailyQuestCount = 3; // 하루 의뢰 수
        public const int BonusGold = 400; // 3개 모두 완료 보너스 골드
        public const int BonusBondResource = 1; // 3개 모두 완료 보너스 결속 자원

        private static readonly GuildQuestDefinition[] Definitions = // 의뢰 후보 (조건에 맞는 것만 오늘의 의뢰로 뽑힘)
        {
            new GuildQuestDefinition("GQ_FOREST", GuildQuestKind.ClearDungeon, "DG001", 1, 200, "IT_POTION_SMALL", 2), // 숲 정리
            new GuildQuestDefinition("GQ_FOREST_RUINS", GuildQuestKind.ClearDungeon, "DG002", 1, 260, "IT_MATERIAL_001", 3), // 폐허 조사
            new GuildQuestDefinition("GQ_SWAMP", GuildQuestKind.ClearDungeon, "DG003", 1, 320, "IT_RUNE_SHARD", 2), // 회랑 봉쇄
            new GuildQuestDefinition("GQ_SWAMP_DEEP", GuildQuestKind.ClearDungeon, "DG009", 1, 360, "IT_SCROLL_ARMOR_C", 1), // 독안개 정화
            new GuildQuestDefinition("GQ_NOIR", GuildQuestKind.ClearDungeon, "DG005", 1, 360, "IT_RUNE_SHARD", 3), // 서고 경비
            new GuildQuestDefinition("GQ_SILVARAN", GuildQuestKind.ClearDungeon, "DG006", 1, 400, "IT_MATERIAL_001", 4), // 숲의 부탁
            new GuildQuestDefinition("GQ_CASTLE", GuildQuestKind.ClearDungeon, "DG004", 1, 480, "IT_SCROLL_WEAPON_C", 1), // 관문 정찰
            new GuildQuestDefinition("GQ_WIN2", GuildQuestKind.WinBattles, string.Empty, 2, 240, "IT_POTION_SMALL", 2), // 몸풀기
            new GuildQuestDefinition("GQ_WIN4", GuildQuestKind.WinBattles, string.Empty, 4, 420, "IT_RUNE_SHARD", 2), // 연속 출동
            new GuildQuestDefinition("GQ_RIFT", GuildQuestKind.ClearRift, string.Empty, 1, 600, "IT_RUNE_BOX_1", 1, 1), // 균열 봉쇄
            new GuildQuestDefinition("GQ_FIRE", GuildQuestKind.WinWithElement, string.Empty, 2, 300, "IT_MATERIAL_001", 3, 0, ElementType.Fire), // 불 속성 호위
            new GuildQuestDefinition("GQ_WATER", GuildQuestKind.WinWithElement, string.Empty, 2, 300, "IT_MATERIAL_001", 3, 0, ElementType.Water), // 물 속성 호위
            new GuildQuestDefinition("GQ_GRASS", GuildQuestKind.WinWithElement, string.Empty, 2, 300, "IT_MATERIAL_001", 3, 0, ElementType.Grass), // 풀 속성 호위
            new GuildQuestDefinition("GQ_LIGHT", GuildQuestKind.WinWithElement, string.Empty, 2, 320, "IT_RUNE_SHARD", 2, 0, ElementType.Light), // 빛 속성 호위
            new GuildQuestDefinition("GQ_DARK", GuildQuestKind.WinWithElement, string.Empty, 2, 320, "IT_RUNE_SHARD", 2, 0, ElementType.Dark) // 어둠 속성 호위
        };

        public static IReadOnlyList<GuildQuestDefinition> All => Definitions; // 전체 의뢰 후보

        public static GuildQuestDefinition Find(string questId) // 의뢰 조회
        {
            foreach (GuildQuestDefinition definition in Definitions) // 후보 순회
            {
                if (definition.Id == questId) return definition; // 일치
            }

            return null; // 없음
        }

        public static string GetTitle(GuildQuestDefinition definition, string dungeonName) // 의뢰 제목
        {
            if (definition == null) return string.Empty; // 입력 확인

            switch (definition.Kind) // 종류 분기
            {
                case GuildQuestKind.ClearDungeon: return $"{(string.IsNullOrEmpty(dungeonName) ? definition.Target : dungeonName)} 클리어"; // 던전
                case GuildQuestKind.WinBattles: return $"전투 {definition.Required}회 승리"; // 전투
                case GuildQuestKind.ClearRift: return "검은 균열 막기"; // 균열
                default: return $"{GetElementLabel(definition.Element)} 속성 동료와 {definition.Required}회 승리"; // 속성
            }
        }

        public static string GetElementLabel(ElementType element) // 속성 한글 이름
        {
            switch (element) // 속성 분기
            {
                case ElementType.Fire: return "불"; // 불
                case ElementType.Water: return "물"; // 물
                case ElementType.Grass: return "풀"; // 풀
                case ElementType.Light: return "빛"; // 빛
                case ElementType.Dark: return "어둠"; // 어둠
                default: return "무"; // 무속성
            }
        }

        public static string GetItemLabel(string itemId) // 보상 아이템 짧은 이름 (데이터 관리자 없이 표시)
        {
            switch (itemId) // 아이템 분기
            {
                case "IT_POTION_SMALL": return "회복약"; // 회복약
                case "IT_MATERIAL_001": return "재료"; // 재료
                case "IT_RUNE_SHARD": return "룬 조각"; // 룬 조각
                case "IT_RUNE_BOX_1": return "룬 상자"; // 룬 상자
                case "IT_SCROLL_WEAPON_C": return "무기 주문서 C"; // 주문서
                case "IT_SCROLL_ARMOR_C": return "방어구 주문서 C"; // 주문서
                default: return itemId; // 그 외
            }
        }
    }
}
