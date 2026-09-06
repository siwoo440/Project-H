using System.Collections.Generic; // 집합 자료형
using NUnit.Framework; // Unity 테스트 기능
using ProjectH.Battle; // 전투 테스트 프로필 기능
using ProjectH.Data; // 던전 데이터 기능
using ProjectH.UI; // 던전 ID 목록 기능
using UnityEditor; // 에디터 에셋 조회 기능

namespace ProjectH.Tests // 프로젝트 테스트 영역
{
    public sealed class DungeonBattleTestProfileTests // 27일차 던전별 차등값 테스트
    {
        [Test] // 전투 프로필 차등값 검증
        public void Day27Profiles_HaveDistinctEnemyValues() // 4개 던전 적 능력치 차등 검증
        {
            HashSet<string> signatures = new HashSet<string>(); // 프로필 값 조합 집합 생성

            foreach (string dungeonId in DungeonSelectionRuntimeState.SupportedDungeonIds) // 지원 던전 ID 순회
            {
                DungeonBattleTestProfile profile = DungeonBattleTestProfile.Get(dungeonId); // 현재 던전 프로필 조회
                signatures.Add($"{profile.HealthMultiplier:0.00}:{profile.AttackMultiplier:0.00}:{profile.DefenseMultiplier:0.00}:{profile.ResistanceMultiplier:0.00}"); // 현재 프로필 값 조합 저장
            }

            Assert.That(signatures.Count, Is.EqualTo(DungeonSelectionRuntimeState.SupportedDungeonIds.Count)); // 모든 던전 프로필 차등 검증
        }

        [Test] // 던전 보상 차등값 검증
        public void Day27DungeonAssets_HaveDistinctVisibleTestValues() // 4개 던전 레벨 및 보상 차등 검증
        {
            HashSet<string> signatures = new HashSet<string>(); // 던전 값 조합 집합 생성

            foreach (string dungeonId in DungeonSelectionRuntimeState.SupportedDungeonIds) // 지원 던전 ID 순회
            {
                string path = $"Assets/ProjectH/Data/Dungeons/{dungeonId}.asset"; // 현재 던전 에셋 경로 생성
                DungeonData dungeon = AssetDatabase.LoadAssetAtPath<DungeonData>(path); // 현재 던전 에셋 조회
                Assert.That(dungeon, Is.Not.Null, dungeonId); // 현재 던전 에셋 존재 검증
                signatures.Add($"{dungeon.RecommendedLevel}:{dungeon.RewardGold}:{dungeon.RewardExp}"); // 현재 던전 레벨 및 보상 값 조합 저장
            }

            Assert.That(signatures.Count, Is.EqualTo(DungeonSelectionRuntimeState.SupportedDungeonIds.Count)); // 모든 던전 표시값 차등 검증
        }
    }
}
