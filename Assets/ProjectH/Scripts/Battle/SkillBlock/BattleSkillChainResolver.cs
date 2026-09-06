using System.Collections.Generic; // 목록 자료형
using ProjectH.Data; // 스킬 최대 강화도 기능

namespace ProjectH.Battle.SkillBlock // 스킬 블록 전투 영역
{
    public static class BattleSkillChainResolver // 동일 SkillId 인접 강화도 판정 기능
    {
        public static IReadOnlyList<BattleSkillChain> ResolveAll(IReadOnlyList<BattleSkillBlock> blocks) // 전체 Queue 강화 그룹 판정
        {
            List<BattleSkillChain> result = new List<BattleSkillChain>(); // 강화 그룹 결과 목록 생성

            if (blocks == null || blocks.Count == 0) // 블록 목록 존재 확인
            {
                return result; // 빈 강화 그룹 목록 반환
            }

            int index = 0; // Queue 탐색 인덱스 초기화

            while (index < blocks.Count) // 전체 블록 순회
            {
                BattleSkillBlock first = blocks[index]; // 현재 그룹 시작 블록 조회

                if (first == null || string.IsNullOrWhiteSpace(first.SkillId)) // 유효 스킬 블록 확인
                {
                    index++; // 잘못된 블록 다음 위치 이동
                    continue; // 잘못된 블록 그룹 판정 제외
                }

                int runEnd = index + 1; // 동일 SkillId 연속 구간 끝 초기화

                while (runEnd < blocks.Count) // 동일 SkillId 연속 구간 탐색
                {
                    BattleSkillBlock next = blocks[runEnd]; // 다음 블록 조회

                    if (next == null || next.SkillId != first.SkillId) // 동일 SkillId 연속 여부 확인
                    {
                        break; // 동일 스킬 연속 구간 종료
                    }

                    runEnd++; // 동일 스킬 연속 구간 확장
                }

                int remaining = runEnd - index; // 동일 SkillId 연속 블록 수 계산
                int chunkStart = index; // 강화 그룹 분할 시작 위치 설정

                while (remaining > 0) // 동일 스킬 연속 구간 분할
                {
                    int chunkCount = System.Math.Min(SkillData.MaxEnhancementLevel, remaining); // 최대 강화도 3 기준 그룹 크기 계산
                    result.Add(new BattleSkillChain(chunkStart, chunkCount, first.SkillId)); // 강화 그룹 결과 추가
                    chunkStart += chunkCount; // 다음 강화 그룹 시작 위치 이동
                    remaining -= chunkCount; // 남은 연속 블록 수 감소
                }

                index = runEnd; // 다음 다른 SkillId 위치로 이동
            }

            return result; // 전체 강화 그룹 결과 반환
        }

        public static BattleSkillChain ResolveAtIndex(IReadOnlyList<BattleSkillBlock> blocks, int index) // 클릭 블록 소속 강화 그룹 조회
        {
            if (blocks == null || index < 0 || index >= blocks.Count) // 블록 목록 및 인덱스 확인
            {
                return BattleSkillChain.Invalid(); // 잘못된 강화 그룹 반환
            }

            IReadOnlyList<BattleSkillChain> chains = ResolveAll(blocks); // 전체 강화 그룹 판정

            for (int chainIndex = 0; chainIndex < chains.Count; chainIndex++) // 강화 그룹 순회
            {
                BattleSkillChain chain = chains[chainIndex]; // 현재 강화 그룹 조회

                if (chain.Contains(index)) // 클릭 블록 포함 여부 확인
                {
                    return chain; // 클릭 블록 소속 강화 그룹 반환
                }
            }

            return BattleSkillChain.Invalid(); // 소속 강화 그룹 없음 반환
        }
    }
}
