namespace ProjectH.Data // 프로젝트 데이터 영역
{
    public enum CharacterJob // 캐릭터 직군
    {
        None = 0, // 미지정 직군
        Cleric = 1, // 성직자 직군
        Guardian = 2, // 수호자 직군
        Mage = 3, // 마법사 직군
        Ranger = 4, // 원거리 직군
        Knight = 5, // 기사 직군
        Rogue = 6, // 도적 직군
        Archer = 7, // 궁수 직군
        Alchemist = 8, // 연금술사 직군
        Gunner = 9, // 총사수 직군
        Lancer = 10, // 창병 직군
        Monk = 11, // 무도승 직군
        Explorer = 12, // 탐험가 직군
        Pilgrim = 13 // 순례자 직군
    }

    public enum BattlePosition // 전투 포지션
    {
        Tank = 0, // 탱커 포지션
        Dealer = 1, // 딜러 포지션
        Healer = 2 // 힐러 포지션
    }

    public enum CharacterRole // 이전 역할 호환 열거형
    {
        Tank = 0, // 탱커 역할
        Dealer = 1, // 딜러 역할
        Healer = 2 // 힐러 역할
    }

    public enum ItemType // 아이템 유형
    {
        Consumable = 0, // 소비 아이템
        Material = 1, // 재료 아이템
        Equipment = 2, // 장비 아이템
        Gift = 3, // 선물 아이템
        Special = 4, // 특수 아이템
        Quest = 5 // 퀘스트 아이템
    }

    public enum ItemGrade // 아이템 등급
    {
        Common = 0, // 일반 등급
        Uncommon = 1, // 고급 등급
        Rare = 2, // 희귀 등급
        Epic = 3, // 영웅 등급
        Legendary = 4 // 전설 등급
    }

    public enum EquipmentSlot // 장비 착용 슬롯
    {
        Weapon = 0, // 무기 슬롯
        Armor = 1 // 방어구 슬롯
    }

    public enum EquipmentStatType // 장비 능력치 종류
    {
        MaxHp = 0, // 최대 체력
        Attack = 1, // 공격력
        Defense = 2, // 방어력
        Resistance = 3, // 저항력
        AttackSpeed = 4, // 공격 속도
        Accuracy = 5, // 명중률
        CriticalRate = 6, // 치명타율
        AttackRange = 7, // 공격 사거리
        MoveSpeed = 8 // 이동 속도
    }
}
