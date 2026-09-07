namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public sealed class BattleEquipmentStatBonus // 장비 전투 능력치 합계
    {
        public static BattleEquipmentStatBonus Empty { get; } = new BattleEquipmentStatBonus(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f); // 빈 장비 보정값 생성
        public float MaxHp { get; } // 최대 체력 보정값
        public float Attack { get; } // 공격력 보정값
        public float Defense { get; } // 방어력 보정값
        public float Resistance { get; } // 저항력 보정값
        public float AttackSpeed { get; } // 공격속도 보정값
        public float Accuracy { get; } // 명중률 보정값
        public float CriticalRate { get; } // 치명타율 보정값
        public float AttackRange { get; } // 공격 사거리 보정값
        public float MoveSpeed { get; } // 이동속도 보정값

        public BattleEquipmentStatBonus(float maxHp, float attack, float defense, float resistance, float attackSpeed, float accuracy, float criticalRate, float attackRange, float moveSpeed) // 장비 보정값 생성
        {
            MaxHp = maxHp; // 최대 체력 보정값 저장
            Attack = attack; // 공격력 보정값 저장
            Defense = defense; // 방어력 보정값 저장
            Resistance = resistance; // 저항력 보정값 저장
            AttackSpeed = attackSpeed; // 공격속도 보정값 저장
            Accuracy = accuracy; // 명중률 보정값 저장
            CriticalRate = criticalRate; // 치명타율 보정값 저장
            AttackRange = attackRange; // 공격 사거리 보정값 저장
            MoveSpeed = moveSpeed; // 이동속도 보정값 저장
        }
    }
}
