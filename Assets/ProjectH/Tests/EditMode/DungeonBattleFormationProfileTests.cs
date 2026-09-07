using System.Collections.Generic; // 집합 자료형
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 던전 전투 편성 기능
using ProjectH.Data; // 몬스터 데이터 기능
using ProjectH.UI; // 지원 던전 목록 기능
using UnityEditor; // Unity 에디터 에셋 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class DungeonBattleFormationProfileTests // 던전별 실제 적 편성 테스트
    {
        [Test] // 던전별 적 수 차등 검증
        public void Profiles_HaveDistinctEnemyCounts() // 네 던전 적 수 차등 검증
        {
            HashSet<int> counts = new HashSet<int>(); // 적 수 집합 생성

            foreach (string dungeonId in DungeonSelectionRuntimeState.SupportedDungeonIds) // 지원 던전 순회
            {
                DungeonBattleFormationProfile profile = DungeonBattleFormationProfile.Get(dungeonId); // 현재 던전 편성 조회
                counts.Add(profile.EnemyCount); // 현재 던전 적 수 저장
            }

            Assert.That(counts.Count, Is.EqualTo(DungeonSelectionRuntimeState.SupportedDungeonIds.Count)); // 네 던전 적 수 차등 검증
            Assert.That(DungeonBattleFormationProfile.Get("DG001").EnemyCount, Is.EqualTo(1)); // DG001 한 개체 편성 검증
            Assert.That(DungeonBattleFormationProfile.Get("DG002").EnemyCount, Is.EqualTo(2)); // DG002 두 개체 편성 검증
            Assert.That(DungeonBattleFormationProfile.Get("DG003").EnemyCount, Is.EqualTo(3)); // DG003 세 개체 편성 검증
            Assert.That(DungeonBattleFormationProfile.Get("DG004").EnemyCount, Is.EqualTo(4)); // DG004 네 개체 편성 검증
        }

        [Test] // 편성 몬스터 에셋 존재 검증
        public void Profiles_ReferenceExistingMonsterAssets() // 모든 편성 몬스터 데이터 존재 검증
        {
            foreach (string dungeonId in DungeonSelectionRuntimeState.SupportedDungeonIds) // 지원 던전 순회
            {
                DungeonBattleFormationProfile profile = DungeonBattleFormationProfile.Get(dungeonId); // 현재 던전 편성 조회

                foreach (string monsterId in profile.EnemyIds) // 편성 몬스터 ID 순회
                {
                    string path = $"Assets/ProjectH/Data/Monsters/{monsterId}.asset"; // 몬스터 에셋 경로 생성
                    MonsterData monster = AssetDatabase.LoadAssetAtPath<MonsterData>(path); // 몬스터 에셋 조회
                    Assert.That(monster, Is.Not.Null, $"{dungeonId}:{monsterId}"); // 편성 몬스터 에셋 존재 검증
                }
            }
        }
    }
}
