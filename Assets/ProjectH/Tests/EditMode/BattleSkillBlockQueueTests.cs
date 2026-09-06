using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle.SkillBlock; // 스킬 블록 Queue 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleSkillBlockQueueTests // 스킬 블록 Queue 테스트
    {
        [Test] // 테스트 표시
        public void TryAdd_StopsAtMaximumCount() // 최대 보유 블록 제한 검증
        {
            BattleSkillBlockQueue queue = new BattleSkillBlockQueue(3); // 최대 3칸 Queue 생성
            Assert.That(queue.TryAdd(BattleSkillBlock.CreateTest("A", "CH_A", 1)), Is.True); // 첫 블록 추가 검증
            Assert.That(queue.TryAdd(BattleSkillBlock.CreateTest("B", "CH_B", 1)), Is.True); // 둘째 블록 추가 검증
            Assert.That(queue.TryAdd(BattleSkillBlock.CreateTest("C", "CH_C", 1)), Is.True); // 셋째 블록 추가 검증
            Assert.That(queue.TryAdd(BattleSkillBlock.CreateTest("D", "CH_D", 1)), Is.False); // 최대 초과 추가 차단 검증
            Assert.That(queue.Count, Is.EqualTo(3)); // 최종 Queue 개수 검증
        }

        [Test] // 테스트 표시
        public void Move_ReordersBlocksWithoutChangingCount() // 드래그 순서 변경 규칙 검증
        {
            BattleSkillBlockQueue queue = new BattleSkillBlockQueue(7); // 테스트 Queue 생성
            queue.TryAdd(BattleSkillBlock.CreateTest("A", "CH_A", 1)); // A 블록 추가
            queue.TryAdd(BattleSkillBlock.CreateTest("B", "CH_B", 1)); // B 블록 추가
            queue.TryAdd(BattleSkillBlock.CreateTest("C", "CH_C", 1)); // C 블록 추가
            queue.TryAdd(BattleSkillBlock.CreateTest("D", "CH_D", 1)); // D 블록 추가

            Assert.That(queue.Move(0, 2), Is.True); // A 블록을 세 번째 위치로 이동
            Assert.That(queue.Count, Is.EqualTo(4)); // 이동 후 블록 개수 유지 검증
            Assert.That(queue[0].SkillId, Is.EqualTo("B")); // 첫 위치 B 검증
            Assert.That(queue[1].SkillId, Is.EqualTo("C")); // 둘째 위치 C 검증
            Assert.That(queue[2].SkillId, Is.EqualTo("A")); // 셋째 위치 A 검증
            Assert.That(queue[3].SkillId, Is.EqualTo("D")); // 넷째 위치 D 검증
        }

        [Test] // 테스트 표시
        public void Consume_RemovesOnlyRequestedGroup() // 스킬 사용 블록 소비 범위 검증
        {
            BattleSkillBlockQueue queue = new BattleSkillBlockQueue(7); // 테스트 Queue 생성
            queue.TryAdd(BattleSkillBlock.CreateTest("A", "CH_A", 1)); // 첫 A 블록 추가
            queue.TryAdd(BattleSkillBlock.CreateTest("A", "CH_A", 1)); // 둘째 A 블록 추가
            queue.TryAdd(BattleSkillBlock.CreateTest("B", "CH_B", 1)); // B 블록 추가

            Assert.That(queue.Consume(0, 2), Is.EqualTo(2)); // A 블록 2개 소비 검증
            Assert.That(queue.Count, Is.EqualTo(1)); // 소비 후 Queue 개수 검증
            Assert.That(queue[0].SkillId, Is.EqualTo("B")); // 남은 B 블록 검증
        }

        [Test] // 테스트 표시
        public void RemoveByCharacterId_RemovesDeadCharacterBlocks() // 사망 캐릭터 블록 정리 검증
        {
            BattleSkillBlockQueue queue = new BattleSkillBlockQueue(7); // 테스트 Queue 생성
            queue.TryAdd(BattleSkillBlock.CreateTest("A1", "CH_A", 1)); // A 캐릭터 첫 블록 추가
            queue.TryAdd(BattleSkillBlock.CreateTest("B1", "CH_B", 1)); // B 캐릭터 블록 추가
            queue.TryAdd(BattleSkillBlock.CreateTest("A2", "CH_A", 2)); // A 캐릭터 둘째 블록 추가

            Assert.That(queue.RemoveByCharacterId("CH_A"), Is.EqualTo(2)); // A 캐릭터 블록 2개 제거 검증
            Assert.That(queue.Count, Is.EqualTo(1)); // 제거 후 Queue 개수 검증
            Assert.That(queue[0].CharacterId, Is.EqualTo("CH_B")); // B 캐릭터 블록 유지 검증
        }
    }
}
