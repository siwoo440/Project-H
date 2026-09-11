using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 패시브 기능
using ProjectH.Data; // 전투 포지션 기능
using UnityEngine; // Unity 객체 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattlePassiveSystemTests // 초기 4인 패시브 테스트
    {
        private GameObject registryObject; // 전투 Registry 객체
        private BattleCombatRegistry registry; // 전투 Registry 참조

        [SetUp] // 테스트 초기화
        public void SetUp() // 전투 패시브 테스트 준비
        {
            BattleRuntimeStates.ResetAll(); // 전투 정적 상태 전체 초기화 (최적화 통합 창구)
            registryObject = new GameObject("Registry"); // Registry 테스트 객체 생성
            registry = registryObject.AddComponent<BattleCombatRegistry>(); // Registry 컴포넌트 생성
            BattlePassiveSystem.Initialize(registry); // 패시브 시스템 초기화
        }

        [TearDown] // 테스트 정리
        public void TearDown() // 테스트 객체 제거
        {
            BattlePassiveSystem.Shutdown(registry); // 패시브 시스템 종료
            Object.DestroyImmediate(registryObject); // Registry 테스트 객체 제거
        }

        [Test] // 테스트 표시
        public void Serena_GrantsSixPercentShield_WhenAllyFallsToThirtyPercent() // 세레나 보호막 패시브 검증
        {
            BattleActor serena = CreateAlly("ALLY_0", "CH_SERENA", 100, 10, 10, 0f); // 세레나 테스트 액터 생성
            BattleActor ally = CreateAlly("ALLY_1", "CH_EVE", 200, 10, 10, 0f); // 저체력 아군 테스트 액터 생성
            ((BattleStats)ally.Stats).SetCurrentHp(60); // 아군 체력 30퍼센트 설정

            BattlePassiveSystem.Handle(BattlePassiveEventContext.CreateDamageTaken(ally, 1)); // 피해 Trigger 처리

            Assert.That(BattlePassiveRuntimeState.GetShield(ally.Stats.RuntimeId), Is.EqualTo(12)); // 최대 체력 6퍼센트 보호막 검증
            Object.DestroyImmediate(serena.gameObject); // 세레나 테스트 객체 제거
            Object.DestroyImmediate(ally.gameObject); // 아군 테스트 객체 제거
        }

        [Test] // 테스트 표시
        public void Ellen_GainsDefenseBuff_WhenHpFallsToFortyPercent() // 엘렌 방어 패시브 검증
        {
            BattleActor ellen = CreateAlly("ALLY_0", "CH_ELLEN", 100, 10, 100, 0f); // 엘렌 테스트 액터 생성
            ((BattleStats)ellen.Stats).SetCurrentHp(40); // 엘렌 체력 40퍼센트 설정

            BattlePassiveSystem.Handle(BattlePassiveEventContext.CreateDamageTaken(ellen, 1)); // 피해 Trigger 처리
            int effectiveDefense = BattleSkillRuntimeState.GetEffectiveDefense(ellen.Stats); // 패시브 적용 방어력 조회

            Assert.That(effectiveDefense, Is.EqualTo(120)); // 방어력 20퍼센트 증가 검증
            Object.DestroyImmediate(ellen.gameObject); // 엘렌 테스트 객체 제거
        }

        [Test] // 테스트 표시
        public void Eve_GainsCriticalBonus_AfterFiveConsecutiveHits() // 이브 연속 적중 패시브 검증
        {
            BattleActor eve = CreateAlly("ALLY_0", "CH_EVE", 100, 10, 10, 0.10f); // 이브 테스트 액터 생성

            for (int index = 0; index < 5; index++) // 5회 연속 적중 반복
            {
                BattlePassiveSystem.Handle(BattlePassiveEventContext.CreateBasicAttackHit(eve, null, 1)); // 기본 공격 적중 Trigger 처리
            }

            Assert.That(BattlePassiveRuntimeState.GetCriticalChanceBonus(eve.Stats.RuntimeId), Is.EqualTo(0.04f).Within(0.0001f)); // 치명타율 4퍼센트포인트 증가 검증
            Object.DestroyImmediate(eve.gameObject); // 이브 테스트 객체 제거
        }

        [Test] // 테스트 표시
        public void Eve_MissResetsConsecutiveHitCounter() // 이브 연속 적중 초기화 검증
        {
            BattleActor eve = CreateAlly("ALLY_0", "CH_EVE", 100, 10, 10, 0.10f); // 이브 테스트 액터 생성

            for (int index = 0; index < 4; index++) // 4회 연속 적중 반복
            {
                BattlePassiveSystem.Handle(BattlePassiveEventContext.CreateBasicAttackHit(eve, null, 1)); // 기본 공격 적중 Trigger 처리
            }

            BattlePassiveSystem.Handle(BattlePassiveEventContext.CreateBasicAttackMiss(eve, null)); // 기본 공격 빗나감 Trigger 처리
            BattlePassiveSystem.Handle(BattlePassiveEventContext.CreateBasicAttackHit(eve, null, 1)); // 빗나감 후 1회 적중 처리

            Assert.That(BattlePassiveRuntimeState.GetCounter(eve.Stats.RuntimeId, "EVE_BASIC_HIT"), Is.EqualTo(1)); // 연속 적중 카운터 재시작 검증
            Object.DestroyImmediate(eve.gameObject); // 이브 테스트 객체 제거
        }

        private BattleActor CreateAlly(string runtimeId, string characterId, int maxHp, int attack, int defense, float criticalRate) // 아군 테스트 액터 생성
        {
            GameObject gameObject = new GameObject(runtimeId); // 전투 액터 객체 생성
            BattleActor actor = gameObject.AddComponent<BattleActor>(); // 전투 액터 컴포넌트 생성
            BattleStats stats = new BattleStats(runtimeId, characterId, characterId, BattlePosition.Dealer, 1, maxHp, attack, defense, 1f, 1f, criticalRate); // 테스트 전투 스탯 생성
            actor.Initialize(BattleTeam.Ally, stats, Vector3.zero); // 아군 전투 액터 초기화
            registry.Register(actor); // Registry 전투 액터 등록
            return actor; // 생성 전투 액터 반환
        }
    }
}
