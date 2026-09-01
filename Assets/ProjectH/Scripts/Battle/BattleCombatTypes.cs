namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public enum BattleTeam // 전투 소속 팀
    {
        Ally = 0, // 아군 팀
        Enemy = 1 // 적군 팀
    }

    public enum BattleActionKind // 전투 행동 종류
    {
        BasicAttack = 0, // 기본 공격 행동
        Skill = 1, // 스킬 행동
        Ultimate = 2 // 궁극기 행동
    }

    public enum BattleAttackState // 기본 공격 행동 상태
    {
        Idle = 0, // 타겟 탐색 대기
        Approach = 1, // 공격 사거리 접근
        Attack = 2, // 기본 공격 실행
        Return = 3, // 이전 버전 호환 복귀 상태
        Cooldown = 4 // 다음 공격 대기
    }

    public enum BattleDamageType // 전투 피해 종류
    {
        Physical = 0, // 물리 피해
        Magic = 1, // 마법 피해
        True = 2 // 방어 무시 피해
    }

    public interface IBattleCombatantStats // 공통 전투 스탯 계약
    {
        string RuntimeId { get; } // 전투 인스턴스 ID
        string DisplayName { get; } // 전투 표시 이름
        int MaxHp { get; } // 최대 체력
        int CurrentHp { get; } // 현재 체력
        int Attack { get; } // 공격력
        int Defense { get; } // 방어력
        float AttackSpeed { get; } // 공격속도
        float AttackRange { get; } // 기본 공격 사거리
        float MoveSpeed { get; } // 전투 이동속도
        bool IsAlive { get; } // 생존 상태
    }

    public interface IBattleMutableCombatantStats : IBattleCombatantStats // 변경 가능한 전투 스탯 계약
    {
        int TakeDamage(int amount); // 피해 적용
        int Heal(int amount); // 회복 적용
    }

    public interface IBattleResistanceStats // 마법 저항력 선택 계약
    {
        int Resistance { get; } // 마법 저항력
    }
}
