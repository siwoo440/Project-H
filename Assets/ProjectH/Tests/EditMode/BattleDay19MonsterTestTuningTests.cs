using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Data; // 몬스터 데이터 기능
using UnityEditor; // Unity 에디터 에셋 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleDay19MonsterTestTuningTests // 19일차 스킬 테스트용 몬스터 수치 테스트
    {
        [TestCase("MON_CORRUPTED_SOLDIER", 3250, 11)] // 침식 병사 테스트 수치
        [TestCase("MON_CORRUPTED_WOLF", 2100, 10)] // 침식 늑대 테스트 수치
        [TestCase("MON_POLLUTED_PLANT", 2600, 8)] // 오염 식물 테스트 수치
        public void MonsterData_UsesDay19LongBattleTestValues(string monsterId, int expectedHp, int expectedAttack) // 스킬 테스트용 HP 및 공격력 검증
        {
            MonsterData monster = AssetDatabase.LoadAssetAtPath<MonsterData>($"Assets/ProjectH/Data/Monsters/{monsterId}.asset"); // 몬스터 데이터 에셋 로드

            Assert.That(monster, Is.Not.Null); // 몬스터 데이터 존재 검증
            Assert.That(monster.MaxHp, Is.EqualTo(expectedHp)); // 테스트용 최대 HP 검증
            Assert.That(monster.Attack, Is.EqualTo(expectedAttack)); // 테스트용 공격력 검증
        }
    }
}
