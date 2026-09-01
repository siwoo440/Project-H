using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 체력 기능
using UnityEngine; // Unity 게임 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleActorHealthIntegrationTests // 전투 액터 체력 통합 테스트
    {
        private GameObject actorObject; // 테스트 전투 객체
        private BattleActor actor; // 테스트 전투 액터
        private BattleStats stats; // 테스트 전투 스탯

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 전투 액터 준비
        {
            actorObject = new GameObject("BattleActorHealthIntegrationTests"); // 테스트 전투 객체 생성
            actor = actorObject.AddComponent<BattleActor>(); // 전투 액터 추가
            stats = new BattleStats("ALLY_0", "CH_TEST", "TEST", ProjectH.Data.BattlePosition.Dealer, 1, 100, 30, 5, 1f, 1f, 0f); // 테스트 전투 스탯 생성
            actor.Initialize(BattleTeam.Ally, stats, Vector3.zero); // 전투 액터 초기화
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 테스트 객체 정리
        {
            Object.DestroyImmediate(actorObject); // 테스트 전투 객체 제거
        }

        [Test] // 테스트 표시
        public void ApplyDamage_ChangesCurrentHpAndRaisesHealthEvent() // 피해 적용 및 체력 이벤트 검증
        {
            int eventCount = 0; // 체력 변경 이벤트 횟수 초기화
            stats.HealthChanged += () => eventCount++; // 체력 변경 이벤트 구독
            BattleDamageResult result = new BattleDamageResult(BattleDamageType.Physical, "ENEMY_0", "ALLY_0", 25, 5, 20); // 테스트 피해 결과 생성
            int applied = actor.ApplyDamage(result); // 전투 액터 피해 적용

            Assert.That(applied, Is.EqualTo(20)); // 실제 피해량 검증
            Assert.That(stats.CurrentHp, Is.EqualTo(80)); // 피해 후 체력 검증
            Assert.That(eventCount, Is.EqualTo(1)); // 체력 변경 이벤트 검증
        }

        [Test] // 테스트 표시
        public void ApplyHealing_HealsLivingActorButDoesNotReviveDeadActor() // 생존 회복 및 부활 방지 검증
        {
            stats.TakeDamage(40); // 생존 대상 피해 적용
            BattleHealingResult healing = BattleHealingResolver.Resolve(stats, 25); // 생존 대상 회복 계산
            int healed = actor.ApplyHealing(healing); // 생존 대상 회복 적용
            Assert.That(healed, Is.EqualTo(25)); // 생존 대상 회복량 검증
            Assert.That(stats.CurrentHp, Is.EqualTo(85)); // 생존 대상 회복 후 체력 검증

            stats.TakeDamage(999); // 전투 불능 처리
            BattleHealingResult deadHealing = BattleHealingResolver.Resolve(stats, 50); // 전투 불능 대상 회복 계산
            int revived = actor.ApplyHealing(deadHealing); // 일반 회복 적용 시도
            Assert.That(revived, Is.EqualTo(0)); // 부활 차단 검증
            Assert.That(stats.CurrentHp, Is.EqualTo(0)); // 전투 불능 체력 유지 검증
        }
    }
}
