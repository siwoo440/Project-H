using System; // 문자열 비교 기능
using System.Collections.Generic; // 목록 자료형

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    public sealed class UniqueEquipmentDefinition // 동료 전용 장비 한 종류 (Day70 신규)
    {
        public string CharacterId { get; } // 주인 캐릭터 ID
        public string EquipmentId { get; } // 장비 ID (EQ_UNIQUE_*)
        public string DisplayName { get; } // 장비 이름
        public string Flavor { get; } // 설명 (그 동료의 이야기)
        public int Attack { get; } // 공격력 옵션
        public string SubStatText { get; } // 보조 옵션 설명

        public UniqueEquipmentDefinition(string characterId, string equipmentId, string displayName, string flavor, int attack, string subStatText) // 정의 생성
        {
            CharacterId = characterId ?? string.Empty; // 주인 저장
            EquipmentId = equipmentId ?? string.Empty; // 장비 ID 저장
            DisplayName = displayName ?? string.Empty; // 이름 저장
            Flavor = flavor ?? string.Empty; // 설명 저장
            Attack = Math.Max(0, attack); // 공격력 저장
            SubStatText = subStatText ?? string.Empty; // 보조 옵션 저장
        }
    }

    public static class UniqueEquipmentCatalog // 12인 전용 장비 목록 (Day70 신규 — 개인 이야기 1화를 마치면 받고, 결속으로 초월한다)
    {
        public const string IdPrefix = "EQ_UNIQUE_"; // 전용 장비 ID 접두사
        public const int TranscendBondLevel = 3; // ★2 초월에 필요한 결속 단계
        public const int MaxTranscendBondLevel = 5; // ★3 초월에 필요한 결속 단계
        public const int RuneSlotTranscendStage = 2; // 룬 5번 칸을 여는 전용 장비 초월 단계

        private static readonly UniqueEquipmentDefinition[] Items = // 전용 장비 12종 (전부 무기 칸 · 전설 등급)
        {
            new UniqueEquipmentDefinition("CH_SERENA", "EQ_UNIQUE_SERENA", "리리아스의 성구", "성녀가 처음 받은 기도 도구. 남을 지킬 때만 빛이 강해진다.", 55, "최대 체력 +380"), // 세레나
            new UniqueEquipmentDefinition("CH_ELLEN", "EQ_UNIQUE_ELLEN", "맹세의 대검", "버린 마을 쪽을 향해 한 번도 겨누지 않은 검.", 60, "방어력 +45"), // 엘렌
            new UniqueEquipmentDefinition("CH_LILIA", "EQ_UNIQUE_LILIA", "해석되지 않는 지팡이", "설명이 안 되는 현상을 처음으로 받아들인 날 만들어졌다.", 78, "명중률 +5%"), // 릴리아
            new UniqueEquipmentDefinition("CH_EVE", "EQ_UNIQUE_EVE", "숲의 기억 장궁", "불타 사라진 고향 나무의 마지막 가지로 만든 활.", 74, "치명타율 +6%"), // 이브
            new UniqueEquipmentDefinition("CH_LUCIA", "EQ_UNIQUE_LUCIA", "계약서 장총", "총열에 스스로 정한 규칙이 새겨져 있다. '먼저 쏘지 않는다.'", 76, "치명타율 +8%"), // 루시아
            new UniqueEquipmentDefinition("CH_CLAIRE", "EQ_UNIQUE_CLAIRE", "생명 순환 플라스크", "죽은 이를 되살리려던 연구가 산 사람을 살리는 쪽으로 바뀐 증거.", 62, "저항력 +40"), // 클레어
            new UniqueEquipmentDefinition("CH_MERCIA", "EQ_UNIQUE_MERCIA", "동방의 염주 곤", "순례길에서 하루에 한 알씩 꿴 염주가 무기가 되었다.", 72, "공격 속도 +6%"), // 메르시아
            new UniqueEquipmentDefinition("CH_PYRA", "EQ_UNIQUE_PYRA", "붉은 서약창", "'명령으로 마을을 버리는 건 한 번으로 충분하다'는 말이 새겨져 있다.", 80, "공격 속도 +4%"), // 파이라
            new UniqueEquipmentDefinition("CH_TYRIA", "EQ_UNIQUE_TYRIA", "마지막 수호대의 방패창", "요새 안뜰에서 피난민 앞을 막고 섰던 그날의 무기.", 58, "방어력 +50"), // 티리아
            new UniqueEquipmentDefinition("CH_NOEL", "EQ_UNIQUE_NOEL", "별자리 측량기", "잃어버린 동료가 마지막으로 그려 준 지도가 들어 있다.", 68, "명중률 +7%"), // 노엘
            new UniqueEquipmentDefinition("CH_NATASHA", "EQ_UNIQUE_NATASHA", "그림자 쌍단검", "복수를 위해 들었지만, 이제는 누군가를 지키기 위해 뽑는다.", 75, "치명타율 +7%"), // 나타샤
            new UniqueEquipmentDefinition("CH_SEPHIRA", "EQ_UNIQUE_SEPHIRA", "사도의 심판봉", "신의 명령을 처음으로 의심한 날, 스스로 무게를 줄였다.", 70, "최대 체력 +300") // 세피라
        };

        public static IReadOnlyList<UniqueEquipmentDefinition> All => Items; // 전체 전용 장비 반환

        public static bool IsUnique(string equipmentId) => !string.IsNullOrEmpty(equipmentId) && equipmentId.StartsWith(IdPrefix, StringComparison.Ordinal); // 전용 장비 여부

        public static UniqueEquipmentDefinition FindByCharacter(string characterId) // 주인으로 조회
        {
            for (int index = 0; index < Items.Length; index++) // 전체 순회
            {
                if (Items[index].CharacterId == characterId) return Items[index]; // 일치 반환
            }

            return null; // 없음
        }

        public static UniqueEquipmentDefinition FindByEquipment(string equipmentId) // 장비 ID로 조회
        {
            for (int index = 0; index < Items.Length; index++) // 전체 순회
            {
                if (Items[index].EquipmentId == equipmentId) return Items[index]; // 일치 반환
            }

            return null; // 없음
        }

        public static string GetOwnerId(string equipmentId) // 전용 장비 주인 ID (전용 장비가 아니면 빈 값)
        {
            UniqueEquipmentDefinition definition = FindByEquipment(equipmentId); // 정의 조회
            return definition == null ? string.Empty : definition.CharacterId; // 주인 반환
        }

        public static int GetRequiredBondLevel(int currentStage) => currentStage >= RuneSlotTranscendStage ? MaxTranscendBondLevel : TranscendBondLevel; // 다음 초월에 필요한 결속 단계 (★2는 3단계 · ★3은 5단계)
    }
}
