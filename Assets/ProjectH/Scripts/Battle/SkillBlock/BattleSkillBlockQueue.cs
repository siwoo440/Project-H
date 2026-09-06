using System.Collections.Generic; // 목록 자료형
using UnityEngine; // Unity 수학 기능

namespace ProjectH.Battle.SkillBlock // 스킬 블록 전투 영역
{
    public sealed class BattleSkillBlockQueue // 전투 스킬 블록 순서 관리자
    {
        private readonly List<BattleSkillBlock> blocks = new List<BattleSkillBlock>(); // 현재 보유 블록 목록
        public int MaxCount { get; } // 최대 보유 블록 수
        public int Count => blocks.Count; // 현재 보유 블록 수
        public IReadOnlyList<BattleSkillBlock> Blocks => blocks; // 현재 블록 읽기 전용 목록
        public BattleSkillBlock this[int index] => index >= 0 && index < blocks.Count ? blocks[index] : null; // 인덱스 블록 반환

        public BattleSkillBlockQueue(int maxCount) // 스킬 블록 Queue 생성
        {
            MaxCount = Mathf.Max(1, maxCount); // 최소 1칸 최대 보유 수 적용
        }

        public bool TryAdd(BattleSkillBlock block) // 새 블록 추가 시도
        {
            if (block == null || Count >= MaxCount) // 블록 유효성 및 최대 보유 확인
            {
                return false; // 블록 추가 실패 반환
            }

            blocks.Add(block); // Queue 마지막에 블록 추가
            return true; // 블록 추가 성공 반환
        }

        public bool Move(int fromIndex, int toIndex) // 드래그 기반 블록 순서 이동
        {
            if (fromIndex < 0 || fromIndex >= blocks.Count) // 시작 인덱스 범위 확인
            {
                return false; // 잘못된 시작 인덱스 이동 실패
            }

            int safeTarget = Mathf.Clamp(toIndex, 0, blocks.Count - 1); // 이동 대상 인덱스 범위 보정

            if (fromIndex == safeTarget) // 동일 위치 이동 확인
            {
                return true; // 동일 위치 이동 성공 처리
            }

            BattleSkillBlock moving = blocks[fromIndex]; // 이동 대상 블록 저장
            blocks.RemoveAt(fromIndex); // 기존 위치 블록 제거
            safeTarget = Mathf.Clamp(safeTarget, 0, blocks.Count); // 제거 후 삽입 위치 재보정
            blocks.Insert(safeTarget, moving); // 새 위치에 블록 삽입
            return true; // 블록 순서 이동 성공 반환
        }

        public int Consume(int startIndex, int count) // 스킬 사용 블록 묶음 소비
        {
            if (startIndex < 0 || startIndex >= blocks.Count || count <= 0) // 소비 범위 유효성 확인
            {
                return 0; // 잘못된 소비 요청 차단
            }

            int safeCount = Mathf.Min(count, blocks.Count - startIndex); // 실제 소비 가능한 개수 계산
            blocks.RemoveRange(startIndex, safeCount); // 요청 블록 묶음 제거
            return safeCount; // 실제 소비 블록 수 반환
        }

        public int RemoveByCharacterId(string characterId) // 캐릭터 기준 보유 블록 일괄 제거
        {
            if (string.IsNullOrWhiteSpace(characterId)) // 캐릭터 ID 확인
            {
                return 0; // 빈 캐릭터 ID 제거 중단
            }

            int removed = 0; // 제거 블록 수 초기화

            for (int index = blocks.Count - 1; index >= 0; index--) // 블록 목록 역순 순회
            {
                BattleSkillBlock block = blocks[index]; // 현재 블록 조회

                if (block == null || block.CharacterId != characterId) // 캐릭터 ID 일치 확인
                {
                    continue; // 제거 대상 아님 처리
                }

                blocks.RemoveAt(index); // 해당 캐릭터 블록 제거
                removed++; // 제거 블록 수 증가
            }

            return removed; // 제거 블록 수 반환
        }

        public void Clear() // 전체 스킬 블록 초기화
        {
            blocks.Clear(); // Queue 전체 블록 제거
        }
    }
}
