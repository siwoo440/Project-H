using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Data; // 던전 데이터 기능
using ProjectH.Dungeon; // 던전 입장 기능
using ProjectH.SaveSystem; // 활력·강화 수치 기능
using UnityEditor; // 에셋 로드 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class EconomyBalanceTests // Day61 경제 밸런스 (던전 활력 소비·골드 조정 → 강화·초월 목표 일수) 테스트
    {
        private static readonly string[] DungeonIds = { "DG001", "DG002", "DG003", "DG004" }; // 던전 4곳

        private static DungeonData Load(string id) => AssetDatabase.LoadAssetAtPath<DungeonData>($"Assets/ProjectH/Data/Dungeons/{id}.asset"); // 던전 에셋

        private static int BestDailyGold() // 활력 100으로 벌 수 있는 하루 최대 골드
        {
            int best = 0; // 최대값

            foreach (string id in DungeonIds) // 던전 순회
            {
                DungeonData dungeon = Load(id); // 던전
                int runs = SaveData.MaxVitality / dungeon.VitalityCost; // 하루 입장 횟수
                best = System.Math.Max(best, runs * dungeon.RewardGold); // 최대 갱신
            }

            return best; // 반환
        }

        [Test] // 던전 입장은 활력을 소비하고 부족하면 막음
        public void DungeonEntry_SpendsVitality_AndBlocksWhenEmpty() // 입장 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 새 게임 (활력 100)
            DungeonData forest = Load("DG001"); // 첫 던전

            Assert.That(DungeonEntryService.TryPayEntry(saveData, forest, out string error), Is.True, error); // 입장
            Assert.That(saveData.CurrentVitality, Is.EqualTo(SaveData.MaxVitality - forest.VitalityCost)); // 활력 소비
            saveData.SetCurrentVitality(forest.VitalityCost - 1); // 부족 상태
            Assert.That(DungeonEntryService.TryPayEntry(saveData, forest, out string blocked), Is.False); // 입장 불가
            Assert.That(blocked, Does.Contain("활력")); // 안내
            Assert.That(saveData.CurrentVitality, Is.EqualTo(forest.VitalityCost - 1)); // 활력 유지
        }

        [Test] // 깊은 던전일수록 활력·골드 모두 증가
        public void Dungeons_CostAndRewardAscend() // 곡선 테스트
        {
            for (int index = 1; index < DungeonIds.Length; index++) // 인접 던전 비교
            {
                DungeonData previous = Load(DungeonIds[index - 1]); // 이전
                DungeonData current = Load(DungeonIds[index]); // 현재
                Assert.That(current.VitalityCost, Is.GreaterThan(previous.VitalityCost), current.Id); // 활력 증가
                Assert.That(current.RewardGold, Is.GreaterThan(previous.RewardGold), current.Id); // 골드 증가
                Assert.That(current.RewardGold / (float)current.VitalityCost, Is.GreaterThanOrEqualTo(previous.RewardGold / (float)previous.VitalityCost), current.Id); // 활력당 골드 효율도 증가
            }
        }

        [Test] // +0 → +5 강화는 2~6일, ★1 → ★2 초월은 6~14일 분량의 골드
        public void UpgradeGoals_TakeReasonableDays() // 목표 일수 테스트
        {
            int daily = BestDailyGold(); // 하루 최대 골드
            float expectedScrolls = 0f; // 기대 주문서 수 (실패 보정 무시 = 보수적)
            int enhanceGold = 0; // 강화 골드 합계

            for (int level = 0; level < EquipmentUpgradeCatalog.MaxEnhanceLevel; level++) // +0→+5
            {
                expectedScrolls += 1f / EquipmentUpgradeCatalog.GetSuccessChance(level, ScrollGrade.C, 0); // 기대 시도 수
                enhanceGold += EquipmentUpgradeCatalog.GetEnhanceGold(level); // 골드 누적
            }

            float plusFiveDays = (enhanceGold + (expectedScrolls * 150f)) / daily; // 주문서 C 150G 기준
            float transcendDays = EquipmentUpgradeCatalog.GetTranscendGold(1) / (float)daily; // ★1→★2

            Assert.That(daily, Is.InRange(1500, 3000)); // 하루 수입 목표
            Assert.That(plusFiveDays, Is.InRange(2f, 6f)); // +5 목표 일수
            Assert.That(transcendDays, Is.InRange(6f, 14f)); // ★2 목표 일수
        }
    }
}
