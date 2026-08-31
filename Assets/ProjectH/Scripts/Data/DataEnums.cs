namespace ProjectH.Data // 프로젝트 데이터 영역
{
    public enum CharacterJob // 캐릭터 직군
    {
        None = 0, // 미지정 직군
        Cleric = 1, // 성직자 직군
        Guardian = 2, // 수호자 직군
        Mage = 3, // 마법사 직군
        Ranger = 4 // 원거리 직군
    }

    public enum BattlePosition // 전투 위치
    {
        Front = 0, // 전열 위치
        Back = 1 // 후열 위치
    }

    public enum CharacterRole // 전투 역할
    {
        None = 0, // 미지정 역할
        Healer = 1, // 회복 역할
        Tank = 2, // 방어 역할
        MagicDealer = 3, // 마법 공격 역할
        RangedDealer = 4 // 원거리 공격 역할
    }

    public enum ItemType // 아이템 유형
    {
        Consumable = 0, // 소비 아이템
        Material = 1, // 재료 아이템
        Equipment = 2, // 장비 아이템
        Gift = 3, // 선물 아이템
        Special = 4 // 특수 아이템
    }

    public enum ItemGrade // 아이템 등급
    {
        Common = 0, // 일반 등급
        Uncommon = 1, // 고급 등급
        Rare = 2, // 희귀 등급
        Epic = 3, // 영웅 등급
        Legendary = 4 // 전설 등급
    }
}
