using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Data; // 몬스터 AI 데이터 기능
using UnityEditor; // Unity 에디터 에셋 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class MonsterAITypeAssetTests // 몬스터 AI 유형 에셋 테스트
    {
        [Test] // 테스트 표시
        public void PrototypeMonsters_HaveExpectedAIType() // 프로토타입 3종 AI 유형 검증
        {
            MonsterData soldier = AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/ProjectH/Data/Monsters/MON_CORRUPTED_SOLDIER.asset"); // 침식 병사 데이터 로드
            MonsterData wolf = AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/ProjectH/Data/Monsters/MON_CORRUPTED_WOLF.asset"); // 침식 늑대 데이터 로드
            MonsterData plant = AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/ProjectH/Data/Monsters/MON_POLLUTED_PLANT.asset"); // 오염 식물 데이터 로드

            Assert.That(soldier, Is.Not.Null); // 침식 병사 에셋 존재 검증
            Assert.That(wolf, Is.Not.Null); // 침식 늑대 에셋 존재 검증
            Assert.That(plant, Is.Not.Null); // 오염 식물 에셋 존재 검증
            Assert.That(soldier.AIType, Is.EqualTo(EnemyAIType.Normal)); // 침식 병사 Normal 검증
            Assert.That(wolf.AIType, Is.EqualTo(EnemyAIType.Rush)); // 침식 늑대 Rush 검증
            Assert.That(plant.AIType, Is.EqualTo(EnemyAIType.Ranged)); // 오염 식물 Ranged 검증
        }
    }
}
