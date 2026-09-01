using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 적 전투 런타임 기능
using ProjectH.Data; // 몬스터 데이터 기능
using UnityEditor; // Unity 에디터 에셋 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleEnemyStatsFactoryTests // 적 전투 스탯 생성 테스트
    {
        private const string MonsterPath = "Assets/ProjectH/Data/Monsters/MON_CORRUPTED_WOLF.asset"; // 테스트 몬스터 경로

        [Test] // 테스트 표시
        public void Create_UsesMonsterMovementAndAttackValues() // 몬스터 전투 수치 반영 검증
        {
            MonsterData monster = AssetDatabase.LoadAssetAtPath<MonsterData>(MonsterPath); // 테스트 몬스터 데이터 로드
            BattleEnemyStats stats = BattleEnemyStatsFactory.Create(monster, "ENEMY_2"); // 적 전투 스탯 생성

            Assert.That(monster, Is.Not.Null); // 몬스터 에셋 존재 검증
            Assert.That(stats.RuntimeId, Is.EqualTo("ENEMY_2")); // 적 런타임 ID 검증
            Assert.That(stats.DisplayName, Is.EqualTo(monster.DisplayName)); // 적 표시 이름 검증
            Assert.That(stats.AttackSpeed, Is.EqualTo(monster.AttackSpeed)); // 적 공격속도 검증
            Assert.That(stats.AttackRange, Is.EqualTo(monster.AttackRange)); // 적 공격 사거리 검증
            Assert.That(stats.MoveSpeed, Is.EqualTo(monster.MoveSpeed)); // 적 이동속도 검증
            Assert.That(stats.IsAlive, Is.True); // 적 시작 생존 상태 검증
        }
    }
}
