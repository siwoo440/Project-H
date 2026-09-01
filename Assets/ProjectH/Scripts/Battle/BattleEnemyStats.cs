using System; // 이벤트 기능
using ProjectH.Data; // 몬스터 데이터 기능
using UnityEngine; // Unity 수학 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public sealed class BattleEnemyStats : IBattleMutableCombatantStats, IBattleResistanceStats // 적 전투 런타임 스탯
    {
        private int currentHp; // 현재 체력
        public event Action HealthChanged; // 체력 변경 이벤트
        public string RuntimeId { get; } // 전투 인스턴스 ID
        public string MonsterId { get; } // 몬스터 원본 ID
        public string DisplayName { get; } // 몬스터 표시 이름
        public int MaxHp { get; } // 최대 체력
        public int CurrentHp => currentHp; // 현재 체력 반환
        public int Attack { get; } // 공격력
        public int Defense { get; } // 방어력
        public int Resistance { get; } // 저항력
        public float AttackSpeed { get; } // 공격속도
        public float AttackRange { get; } // 기본 공격 사거리
        public float MoveSpeed { get; } // 전투 이동속도
        public bool IsAlive => currentHp > 0; // 생존 상태 반환
        public float HealthRatio => MaxHp <= 0 ? 0f : (float)currentHp / MaxHp; // 체력 비율 반환

        public BattleEnemyStats(string runtimeId, string monsterId, string displayName, int maxHp, int attack, int defense, int resistance, float attackSpeed, float attackRange, float moveSpeed) // 적 전투 스탯 생성
        {
            RuntimeId = runtimeId ?? string.Empty; // 런타임 ID 저장
            MonsterId = monsterId ?? string.Empty; // 몬스터 ID 저장
            DisplayName = displayName ?? string.Empty; // 표시 이름 저장
            MaxHp = Mathf.Max(1, maxHp); // 최소 최대 체력 적용
            currentHp = MaxHp; // 시작 체력 완전 회복
            Attack = Mathf.Max(0, attack); // 공격력 음수 방지
            Defense = Mathf.Max(0, defense); // 방어력 음수 방지
            Resistance = Mathf.Max(0, resistance); // 저항력 음수 방지
            AttackSpeed = Mathf.Max(0.01f, attackSpeed); // 공격속도 최소값 적용
            AttackRange = Mathf.Max(0.2f, attackRange); // 공격 사거리 최소값 적용
            MoveSpeed = Mathf.Max(0.01f, moveSpeed); // 이동속도 최소값 적용
        }

        public int TakeDamage(int amount) // 계산 완료 피해 적용
        {
            int safeAmount = Mathf.Max(0, amount); // 음수 피해 방지
            int before = currentHp; // 적용 전 체력 저장
            currentHp = Mathf.Clamp(currentHp - safeAmount, 0, MaxHp); // 체력 감소 및 범위 보정
            int applied = before - currentHp; // 실제 피해량 계산

            if (applied > 0) // 실제 피해 발생 확인
            {
                HealthChanged?.Invoke(); // 체력 변경 이벤트 발생
            }

            return applied; // 실제 피해량 반환
        }

        public int Heal(int amount) // 계산 완료 회복 적용
        {
            if (!IsAlive) // 전투 불능 상태 확인
            {
                return 0; // 일반 회복 부활 차단
            }

            int safeAmount = Mathf.Max(0, amount); // 음수 회복 방지
            int before = currentHp; // 적용 전 체력 저장
            currentHp = Mathf.Clamp(currentHp + safeAmount, 0, MaxHp); // 체력 회복 및 범위 보정
            int applied = currentHp - before; // 실제 회복량 계산

            if (applied > 0) // 실제 회복 발생 확인
            {
                HealthChanged?.Invoke(); // 체력 변경 이벤트 발생
            }

            return applied; // 실제 회복량 반환
        }
    }

    public static class BattleEnemyStatsFactory // 몬스터 전투 스탯 생성기
    {
        public static BattleEnemyStats Create(MonsterData monsterData, string runtimeId) // 몬스터 데이터 기반 전투 스탯 생성
        {
            if (monsterData == null) // 몬스터 원본 데이터 확인
            {
                return null; // 몬스터 원본 누락 반환
            }

            return new BattleEnemyStats(runtimeId, monsterData.Id, monsterData.DisplayName, monsterData.MaxHp, monsterData.Attack, monsterData.Defense, monsterData.Resistance, monsterData.AttackSpeed, monsterData.AttackRange, monsterData.MoveSpeed); // 적 전투 런타임 스탯 반환
        }
    }
}
