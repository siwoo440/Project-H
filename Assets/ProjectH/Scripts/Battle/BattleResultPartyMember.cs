namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public sealed class BattleResultPartyMember // 전투 종료 파티원 스냅샷
    {
        public string RuntimeId { get; } // 전투 런타임 ID 반환
        public string CharacterId { get; } // 캐릭터 원본 ID 반환
        public string DisplayName { get; } // 캐릭터 표시 이름 반환
        public int Level { get; } // 전투 적용 레벨 반환
        public int CurrentHp { get; } // 종료 현재 체력 반환
        public int MaxHp { get; } // 종료 최대 체력 반환
        public bool IsAlive { get; } // 종료 생존 상태 반환
        public float HealthRatio => MaxHp <= 0 ? 0f : (float)CurrentHp / MaxHp; // 종료 체력 비율 반환

        private BattleResultPartyMember(BattleStats stats) // 파티원 스냅샷 생성
        {
            RuntimeId = stats.RuntimeId; // 런타임 ID 복사
            CharacterId = stats.CharacterId; // 캐릭터 ID 복사
            DisplayName = stats.DisplayName; // 표시 이름 복사
            Level = stats.Level; // 레벨 복사
            CurrentHp = stats.CurrentHp; // 현재 체력 복사
            MaxHp = stats.MaxHp; // 최대 체력 복사
            IsAlive = stats.IsAlive; // 생존 상태 복사
        }

        public static BattleResultPartyMember Create(BattleStats stats) // 전투 스탯 기반 파티원 스냅샷 생성
        {
            if (stats == null) // 전투 스탯 존재 확인
            {
                return null; // 빈 파티원 스냅샷 반환
            }

            return new BattleResultPartyMember(stats); // 파티원 종료 상태 스냅샷 반환
        }
    }
}
