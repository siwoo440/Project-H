using System.Collections.Generic; // 목록 자료형
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 던전 드롭 계산 기능
using ProjectH.Data; // 던전 드롭 데이터 기능

namespace ProjectH.Tests.EditMode // EditMode 테스트 영역
{
    public sealed class DungeonDropTests // 던전 드롭 테이블 회귀 테스트
    {
        [Test] // 확정 드롭 수량 범위 테스트
        public void Roll_ChanceOne_DropsWithinConfiguredRange() // 100퍼센트 드롭 수량 범위 검증
        {
            List<DungeonDropEntry> entries = new List<DungeonDropEntry> // 테스트 드롭 테이블 생성
            {
                new DungeonDropEntry("IT_MATERIAL_001", 1f, 2, 4) // 확정 재료 드롭 추가
            };

            List<DungeonDropResult> results = DungeonDropRollService.Roll(entries, "RESULT_A"); // 드롭 테이블 계산

            Assert.That(results.Count, Is.EqualTo(1)); // 단일 드롭 결과 확인
            Assert.That(results[0].ItemId, Is.EqualTo("IT_MATERIAL_001")); // 드롭 아이템 ID 확인
            Assert.That(results[0].Quantity, Is.InRange(2, 4)); // 드롭 수량 범위 확인
        }

        [Test] // 미드롭 확률 테스트
        public void Roll_ChanceZero_DoesNotDrop() // 0퍼센트 드롭 차단 검증
        {
            List<DungeonDropEntry> entries = new List<DungeonDropEntry> // 테스트 드롭 테이블 생성
            {
                new DungeonDropEntry("IT_POTION_SMALL", 0f, 1, 1) // 미드롭 물약 항목 추가
            };

            List<DungeonDropResult> results = DungeonDropRollService.Roll(entries, "RESULT_B"); // 드롭 테이블 계산

            Assert.That(results.Count, Is.EqualTo(0)); // 드롭 없음 확인
        }

        [Test] // 결정적 난수 테스트
        public void Roll_SameSeed_ReturnsSameResults() // 동일 결과 ID 재계산 일치 검증
        {
            List<DungeonDropEntry> entries = new List<DungeonDropEntry> // 테스트 드롭 테이블 생성
            {
                new DungeonDropEntry("IT_MATERIAL_001", 1f, 1, 5), // 확정 재료 드롭 추가
                new DungeonDropEntry("IT_POTION_SMALL", 0.5f, 1, 2) // 확률 물약 드롭 추가
            };

            List<DungeonDropResult> first = DungeonDropRollService.Roll(entries, "RESULT_FIXED"); // 첫 드롭 계산
            List<DungeonDropResult> second = DungeonDropRollService.Roll(entries, "RESULT_FIXED"); // 두 번째 드롭 계산

            Assert.That(second.Count, Is.EqualTo(first.Count)); // 동일 드롭 개수 확인

            for (int index = 0; index < first.Count; index++) // 첫 드롭 결과 순회
            {
                Assert.That(second[index].ItemId, Is.EqualTo(first[index].ItemId)); // 동일 아이템 ID 확인
                Assert.That(second[index].Quantity, Is.EqualTo(first[index].Quantity)); // 동일 아이템 수량 확인
            }
        }

        [Test] // 중복 항목 합산 테스트
        public void Roll_DuplicateItemEntries_AggregatesQuantity() // 동일 아이템 드롭 수량 합산 검증
        {
            List<DungeonDropEntry> entries = new List<DungeonDropEntry> // 테스트 드롭 테이블 생성
            {
                new DungeonDropEntry("IT_MATERIAL_001", 1f, 2, 2), // 첫 재료 드롭 추가
                new DungeonDropEntry("IT_MATERIAL_001", 1f, 3, 3) // 두 번째 재료 드롭 추가
            };

            List<DungeonDropResult> results = DungeonDropRollService.Roll(entries, "RESULT_DUPLICATE"); // 드롭 테이블 계산

            Assert.That(results.Count, Is.EqualTo(1)); // 동일 아이템 단일 결과 확인
            Assert.That(results[0].Quantity, Is.EqualTo(5)); // 동일 아이템 합산 수량 확인
        }
    }
}
