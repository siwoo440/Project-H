using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 사망 처리 기능
using ProjectH.Data; // 전투 포지션 기능
using UnityEngine; // Unity 게임 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleEnemyDeathHandlerTests // 적군 사망 제외 테스트
    {
        private GameObject rootObject; // 테스트 루트 객체
        private BattleCombatRegistry registry; // 테스트 전투 레지스트리

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 테스트 전투 구조 준비
        {
            rootObject = new GameObject("BattleEnemyDeathHandlerTests"); // 테스트 루트 생성
            registry = rootObject.AddComponent<BattleCombatRegistry>(); // 테스트 레지스트리 추가
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 테스트 전투 구조 정리
        {
            Object.DestroyImmediate(rootObject); // 테스트 루트 제거
        }

        [Test] // 테스트 표시
        public void DeathHandler_UnregistersAndHidesDefeatedMonster() // 사망 몬스터 제외 및 숨김 검증
        {
            GameObject enemyObject = new GameObject("ENEMY_0"); // 테스트 적군 객체 생성
            enemyObject.transform.SetParent(rootObject.transform, false); // 테스트 루트 연결
            BattleActor actor = enemyObject.AddComponent<BattleActor>(); // 적군 전투 액터 추가
            BattleBasicAttackController attackController = enemyObject.AddComponent<BattleBasicAttackController>(); // 적군 공격 컨트롤러 추가
            BattleEnemyBrain brain = enemyObject.AddComponent<BattleEnemyBrain>(); // 적군 AI Brain 추가
            BattleEnemyDeathHandler deathHandler = enemyObject.AddComponent<BattleEnemyDeathHandler>(); // 적군 사망 처리기 추가
            BattleEnemyStats stats = new BattleEnemyStats("ENEMY_0", "MON_TEST", "TEST", 10, 5, 1, 1, 1f, 1.5f, 2f, EnemyAIType.Normal); // 테스트 적군 스탯 생성
            actor.Initialize(BattleTeam.Enemy, stats, Vector3.zero); // 적군 전투 액터 초기화
            registry.Register(actor); // 적군 전투 레지스트리 등록
            brain.Configure(actor, registry, EnemyAIType.Normal); // 적군 AI Brain 초기화
            attackController.Configure(actor, registry, brain); // 적군 기본 공격 AI 연결
            deathHandler.Configure(actor, stats, registry, attackController, brain, null); // 적군 사망 처리 참조 연결
            stats.TakeDamage(999); // 적군 전투 불능 처리

            Assert.That(stats.IsAlive, Is.False); // 적군 사망 상태 검증
            Assert.That(registry.Contains(actor), Is.False); // 사망 적군 레지스트리 제외 검증
            Assert.That(attackController.enabled, Is.False); // 사망 적군 공격 중지 검증
            Assert.That(brain.enabled, Is.False); // 사망 적군 AI 중지 검증
            Assert.That(enemyObject.activeSelf, Is.False); // EditMode 즉시 숨김 검증
        }
    }
}
