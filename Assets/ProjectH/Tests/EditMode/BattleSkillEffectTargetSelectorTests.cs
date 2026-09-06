using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 객체 및 스킬 타겟 기능
using ProjectH.Data; // 스킬 대상 종류 기능
using UnityEngine; // Unity 게임 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleSkillEffectTargetSelectorTests // 스킬 효과 타겟 선택 테스트
    {
        private GameObject registryObject; // 테스트 Registry 객체
        private BattleCombatRegistry registry; // 테스트 전투 Registry
        private GameObject ownerObject; // 테스트 스킬 사용자 객체
        private GameObject allyObject; // 테스트 아군 객체
        private GameObject enemyObject; // 테스트 적군 객체
        private BattleActor owner; // 테스트 스킬 사용자
        private BattleActor ally; // 테스트 아군
        private BattleActor enemy; // 테스트 적군

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 테스트 전투 객체 준비
        {
            registryObject = new GameObject("Registry"); // 테스트 Registry 객체 생성
            registry = registryObject.AddComponent<BattleCombatRegistry>(); // 테스트 Registry 컴포넌트 추가
            ownerObject = new GameObject("Owner"); // 테스트 사용자 객체 생성
            allyObject = new GameObject("Ally"); // 테스트 아군 객체 생성
            enemyObject = new GameObject("Enemy"); // 테스트 적군 객체 생성
            owner = ownerObject.AddComponent<BattleActor>(); // 테스트 사용자 BattleActor 추가
            ally = allyObject.AddComponent<BattleActor>(); // 테스트 아군 BattleActor 추가
            enemy = enemyObject.AddComponent<BattleActor>(); // 테스트 적군 BattleActor 추가
            owner.Initialize(BattleTeam.Ally, CreateStats("ALLY_0", "CH_OWNER", 100), Vector3.zero); // 사용자 전투 스탯 초기화
            ally.Initialize(BattleTeam.Ally, CreateStats("ALLY_1", "CH_ALLY", 100), Vector3.zero); // 아군 전투 스탯 초기화
            enemy.Initialize(BattleTeam.Enemy, new BattleEnemyStats("ENEMY_0", "MON_TEST", "Enemy", 100, 10, 1, 0, 1f, 1f, 1f, ProjectH.Data.EnemyAIType.Normal), Vector3.zero); // 적군 전투 스탯 초기화
            registry.Register(owner); // 사용자 Registry 등록
            registry.Register(ally); // 아군 Registry 등록
            registry.Register(enemy); // 적군 Registry 등록
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 테스트 전투 객체 정리
        {
            Object.DestroyImmediate(ownerObject); // 사용자 객체 제거
            Object.DestroyImmediate(allyObject); // 아군 객체 제거
            Object.DestroyImmediate(enemyObject); // 적군 객체 제거
            Object.DestroyImmediate(registryObject); // Registry 객체 제거
        }

        [Test] // 테스트 표시
        public void LowestHpAlly_SelectsLowestHealthRatio() // 최저 체력 아군 선택 검증
        {
            ((BattleStats)owner.Stats).SetCurrentHp(80); // 사용자 체력 80퍼센트 설정
            ((BattleStats)ally.Stats).SetCurrentHp(25); // 아군 체력 25퍼센트 설정

            var targets = BattleSkillEffectTargetSelector.Select(owner, registry, SkillTargetType.LowestHpAlly); // 최저 체력 아군 타겟 선택

            Assert.That(targets.Count, Is.EqualTo(1)); // 단일 타겟 개수 검증
            Assert.That(targets[0], Is.SameAs(ally)); // 가장 낮은 체력 아군 선택 검증
        }

        [Test] // 테스트 표시
        public void AllAllies_DoesNotIncludeEnemy() // 전체 아군 선택 범위 검증
        {
            var targets = BattleSkillEffectTargetSelector.Select(owner, registry, SkillTargetType.AllAllies); // 전체 아군 타겟 선택

            Assert.That(targets.Count, Is.EqualTo(2)); // 사용자 포함 생존 아군 2명 검증
            Assert.That(targets, Has.Member(owner)); // 사용자 포함 검증
            Assert.That(targets, Has.Member(ally)); // 아군 포함 검증
            Assert.That(targets, Has.No.Member(enemy)); // 적군 제외 검증
        }

        private static BattleStats CreateStats(string runtimeId, string characterId, int maxHp) // 테스트 아군 스탯 생성
        {
            return new BattleStats(runtimeId, characterId, characterId, BattlePosition.Dealer, 1, maxHp, 10, 5, 1f, 1f, 0f); // 테스트 BattleStats 반환
        }
    }
}
