namespace ProjectH.Battle.SkillBlock // 스킬 블록 전투 영역
{
    public readonly struct BattleSkillChain // 동일 SkillId 인접 강화 그룹
    {
        public int StartIndex { get; } // 강화 그룹 시작 인덱스
        public int Count { get; } // 강화 그룹 블록 수
        public string SkillId { get; } // 강화 그룹 SkillId
        public int EnhancementLevel => Count; // 블록 수 기반 강화도 반환
        public bool IsValid => StartIndex >= 0 && Count > 0 && !string.IsNullOrWhiteSpace(SkillId); // 강화 그룹 유효성 반환

        public BattleSkillChain(int startIndex, int count, string skillId) // 강화 그룹 생성
        {
            StartIndex = startIndex; // 강화 그룹 시작 위치 저장
            Count = count; // 강화 그룹 블록 수 저장
            SkillId = skillId ?? string.Empty; // 강화 그룹 SkillId 저장
        }

        public bool Contains(int index) // 인덱스가 강화 그룹에 포함되는지 확인
        {
            return index >= StartIndex && index < StartIndex + Count; // 강화 그룹 범위 포함 여부 반환
        }

        public static BattleSkillChain Invalid() // 잘못된 강화 그룹 반환
        {
            return new BattleSkillChain(-1, 0, string.Empty); // 비유효 강화 그룹 생성
        }
    }
}
