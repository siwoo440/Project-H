using ProjectH.SaveSystem; // 룬 합계 기능 (Day60 추가)

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
        public RuneLoadout Runes { get; private set; } = RuneLoadout.Empty; // 장착 룬 효과 합계 (Day60 추가)

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

        public BattleEquipmentStatBonus WithRunes(RuneLoadout runes) // 장착 룬 합계를 붙인 복사본 (Day60 추가)
        {
            BattleEquipmentStatBonus copy = new BattleEquipmentStatBonus(MaxHp, Attack, Defense, Resistance, AttackSpeed, Accuracy, CriticalRate, AttackRange, MoveSpeed); // 장비 수치 복사
            copy.Runes = runes ?? RuneLoadout.Empty; // 룬 합계 연결
            return copy; // 복사본 반환
        }
    }
}
