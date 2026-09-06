using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 객체 및 스킬 타겟 기능
using ProjectH.Data; // 스킬 대상 종류 기능
using UnityEngine; // Unity 게임 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleSkillTargetSelectorDay19Tests // 19일차 범위 타겟 테스트
    {
        private GameObject registryObject; // 테스트 Registry 객체
        private BattleCombatRegistry registry; // 테스트 Registry
        private GameObject ownerObject; // 테스트 사용자 객체
        private GameObject enemyAObject; // 테스트 적 A 객체
        private GameObject enemyBObject; // 테스트 적 B 객체
        private GameObject enemyCObject; // 테스트 적 C 객체
        private BattleActor owner; // 테스트 사용자
        private BattleActor enemyA; // 테스트 적 A
        private BattleActor enemyB; // 테스트 적 B
        private BattleActor enemyC; // 테스트 적 C

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 테스트 전투 객체 준비
        {
            registryObject = new GameObject("Registry"); // Registry 객체 생성
            registry = registryObject.AddComponent<BattleCombatRegistry>(); // Registry 컴포넌트 추가
            ownerObject = CreateActorObject("Owner", BattleTeam.Ally, "ALLY_0", 0f, out owner); // 아군 사용자 생성
            enemyAObject = CreateActorObject("EnemyA", BattleTeam.Enemy, "ENEMY_A", 2f, out enemyA); // 가까운 적 생성
            enemyBObject = CreateActorObject("EnemyB", BattleTeam.Enemy, "ENEMY_B", 3f, out enemyB); // 중간 적 생성
            enemyCObject = CreateActorObject("EnemyC", BattleTeam.Enemy, "ENEMY_C", 7f, out enemyC); // 먼 적 생성
            registry.Register(owner); // 사용자 Registry 등록
            registry.Register(enemyA); // 적 A Registry 등록
            registry.Register(enemyB); // 적 B Registry 등록
            registry.Register(enemyC); // 적 C Registry 등록
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 테스트 전투 객체 정리
        {
            Object.DestroyImmediate(ownerObject); // 사용자 객체 제거
            Object.DestroyImmediate(enemyAObject); // 적 A 객체 제거
            Object.DestroyImmediate(enemyBObject); // 적 B 객체 제거
            Object.DestroyImmediate(enemyCObject); // 적 C 객체 제거
            Object.DestroyImmediate(registryObject); // Registry 객체 제거
        }

        [Test] // 테스트 표시
        public void LineEnemies_ReturnsLivingOpponentsInForwardOrder() // 직선 적 타겟 순서 검증
        {
            var targets = BattleSkillEffectTargetSelector.Select(owner, registry, SkillTargetType.LineEnemies); // 직선 적 전체 선택

            Assert.That(targets.Count, Is.EqualTo(3)); // 직선 적 3명 검증
            Assert.That(targets[0], Is.SameAs(enemyA)); // 가장 가까운 적 첫 순서 검증
            Assert.That(targets[1], Is.SameAs(enemyB)); // 중간 적 둘째 순서 검증
            Assert.That(targets[2], Is.SameAs(enemyC)); // 먼 적 셋째 순서 검증
        }

        [Test] // 테스트 표시
        public void NearbyEnemiesFromPrimary_ExcludesPrimaryAndFarEnemy() // 주 대상 주변 폭발 범위 검증
        {
            BattleSkillTargetContext context = BattleSkillTargetContext.FromPrimary(enemyA); // 적 A 주 대상 Context 생성
            var targets = BattleSkillEffectTargetSelector.Select(owner, registry, SkillTargetType.NearbyEnemiesFromPrimary, context, 2f, true); // 주 대상 반경 2 주변 적 선택

            Assert.That(targets.Count, Is.EqualTo(1)); // 주변 적 1명 검증
            Assert.That(targets[0], Is.SameAs(enemyB)); // 주 대상 제외 및 근접 적 B 선택 검증
        }

        private static GameObject CreateActorObject(string name, BattleTeam team, string runtimeId, float x, out BattleActor actor) // 테스트 전투 객체 생성
        {
            GameObject targetObject = new GameObject(name); // 테스트 게임 오브젝트 생성
            targetObject.transform.position = new Vector3(x, 0f, 0f); // 테스트 X 위치 설정
            actor = targetObject.AddComponent<BattleActor>(); // BattleActor 컴포넌트 추가
            BattleEnemyStats stats = new BattleEnemyStats(runtimeId, runtimeId, name, 100, 10, 0, 0, 1f, 1f, 1f); // 공통 테스트 전투 스탯 생성
            actor.Initialize(team, stats, targetObject.transform.position); // 전투 객체 초기화
            return targetObject; // 테스트 게임 오브젝트 반환
        }
    }
}
