using ProjectH.Data; // 캐릭터 데이터 기능
using UnityEngine; // Unity 수학 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public sealed class BattleStats // 단일 전투 런타임 스탯
    {
        private int currentHp; // 현재 체력

        public string RuntimeId { get; } // 전투 인스턴스 ID
        public string CharacterId { get; } // 캐릭터 원본 ID
        public string DisplayName { get; } // 캐릭터 표시 이름
        public BattlePosition Position { get; } // 전투 포지션
        public int Level { get; } // 전투 적용 레벨
        public int MaxHp { get; } // 최대 체력
        public int CurrentHp => currentHp; // 현재 체력 반환
        public int Attack { get; } // 공격력
        public int Defense { get; } // 방어력
        public float AttackSpeed { get; } // 공격속도
        public float Accuracy { get; } // 명중률
        public float CriticalRate { get; } // 치명타율
        public bool IsAlive => currentHp > 0; // 생존 상태 반환
        public float HealthRatio => MaxHp <= 0 ? 0f : (float)currentHp / MaxHp; // 체력 비율 반환

        public BattleStats(string runtimeId, string characterId, string displayName, BattlePosition position, int level, int maxHp, int attack, int defense, float attackSpeed, float accuracy, float criticalRate) // 런타임 스탯 생성
        {
            RuntimeId = runtimeId ?? string.Empty; // 런타임 ID 저장
            CharacterId = characterId ?? string.Empty; // 캐릭터 ID 저장
            DisplayName = displayName ?? string.Empty; // 표시 이름 저장
            Position = position; // 전투 포지션 저장
            Level = Mathf.Max(1, level); // 최소 레벨 적용
            MaxHp = Mathf.Max(1, maxHp); // 최소 최대 체력 적용
            currentHp = MaxHp; // 시작 체력 완전 회복
            Attack = Mathf.Max(0, attack); // 공격력 음수 방지
            Defense = Mathf.Max(0, defense); // 방어력 음수 방지
            AttackSpeed = Mathf.Max(0.01f, attackSpeed); // 공격속도 최소값 적용
            Accuracy = Mathf.Clamp01(accuracy); // 명중률 범위 보정
            CriticalRate = Mathf.Clamp01(criticalRate); // 치명타율 범위 보정
        }

        public int TakeDamage(int amount) // 계산 완료 피해 적용
        {
            int safeAmount = Mathf.Max(0, amount); // 음수 피해 방지
            int before = currentHp; // 적용 전 체력 저장
            currentHp = Mathf.Clamp(currentHp - safeAmount, 0, MaxHp); // 체력 감소 및 범위 보정
            return before - currentHp; // 실제 피해량 반환
        }

        public int Heal(int amount) // 계산 완료 회복 적용
        {
            int safeAmount = Mathf.Max(0, amount); // 음수 회복 방지
            int before = currentHp; // 적용 전 체력 저장
            currentHp = Mathf.Clamp(currentHp + safeAmount, 0, MaxHp); // 체력 회복 및 범위 보정
            return currentHp - before; // 실제 회복량 반환
        }

        public void SetCurrentHp(int value) // 현재 체력 직접 설정
        {
            currentHp = Mathf.Clamp(value, 0, MaxHp); // 체력 범위 보정 적용
        }

        public void RestoreFullHp() // 전체 체력 회복
        {
            currentHp = MaxHp; // 최대 체력으로 복원
        }
    }
}
