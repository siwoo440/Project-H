using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 궁극기 게이지 기능
using ProjectH.Data; // 캐릭터 포지션 기능
using UnityEngine; // Unity GameObject 테스트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleUltimateGaugeTimedDriverTests // 초 단위 궁극기 게이지 Driver 테스트
    {
        private GameObject registryObject; // 테스트 Registry 객체
        private GameObject aliveAllyObject; // 생존 아군 객체
        private GameObject deadAllyObject; // 사망 아군 객체
        private GameObject enemyObject; // 적군 객체

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 시간 충전 테스트 준비
        {
            BattleUltimateGaugeRuntimeState.ResetAll(); // 이전 테스트 궁극기 게이지 초기화
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 시간 충전 테스트 정리
        {
            Object.DestroyImmediate(enemyObject); // 적군 테스트 객체 제거
            Object.DestroyImmediate(deadAllyObject); // 사망 아군 테스트 객체 제거
            Object.DestroyImmediate(aliveAllyObject); // 생존 아군 테스트 객체 제거
            Object.DestroyImmediate(registryObject); // Registry 테스트 객체 제거
            BattleUltimateGaugeRuntimeState.ResetAll(); // 테스트 종료 궁극기 게이지 초기화
        }

        [Test] // 테스트 표시
        public void TickUltimateGauge_AddsOnePerBattleSecondToLivingAlliesOnly() // 생존 아군 초당 1 충전 검증
        {
            registryObject = new GameObject("Registry"); // Registry GameObject 생성
            BattleCombatRegistry registry = registryObject.AddComponent<BattleCombatRegistry>(); // 전투 Registry 생성
            BattleSkillRuntimeDriver driver = registryObject.AddComponent<BattleSkillRuntimeDriver>(); // 전투 Runtime Driver 생성
            BattleStats aliveStats = CreateStats("ALLY_0", "CH_ALIVE"); // 생존 아군 전투 스탯 생성
            BattleStats deadStats = CreateStats("ALLY_1", "CH_DEAD"); // 사망 아군 전투 스탯 생성
            BattleStats enemyStats = CreateStats("ENEMY_0", "MON_TEST"); // 적군 전투 스탯 생성
            deadStats.SetCurrentHp(0); // 사망 아군 체력 0 설정
            aliveAllyObject = CreateActor(registry, BattleTeam.Ally, aliveStats, "AliveAlly"); // 생존 아군 등록
            deadAllyObject = CreateActor(registry, BattleTeam.Ally, deadStats, "DeadAlly"); // 사망 아군 등록
            enemyObject = CreateActor(registry, BattleTeam.Enemy, enemyStats, "Enemy"); // 적군 등록

            driver.TickUltimateGauge(0.25f); // 첫 0.25초 시간 진행
            driver.TickUltimateGauge(0.75f); // 총 1초 시간 진행
            driver.TickUltimateGauge(2f); // 추가 2초 시간 진행

            Assert.That(BattleUltimateGaugeRuntimeState.GetGauge("CH_ALIVE"), Is.EqualTo(3)); // 생존 아군 총 3 게이지 검증
            Assert.That(BattleUltimateGaugeRuntimeState.GetGauge("CH_DEAD"), Is.EqualTo(0)); // 사망 아군 시간 충전 차단 검증
            Assert.That(BattleUltimateGaugeRuntimeState.GetGauge("MON_TEST"), Is.EqualTo(0)); // 적군 시간 충전 차단 검증
        }

        private static BattleStats CreateStats(string runtimeId, string characterId) // 테스트 전투 스탯 생성
        {
            return new BattleStats(runtimeId, characterId, characterId, BattlePosition.Dealer, 1, 100, 10, 5, 1f, 1f, 0f); // 기본 테스트 스탯 반환
        }

        private static GameObject CreateActor(BattleCombatRegistry registry, BattleTeam team, BattleStats stats, string objectName) // 테스트 전투 액터 생성
        {
            GameObject actorObject = new GameObject(objectName); // 전투 액터 GameObject 생성
            BattleActor actor = actorObject.AddComponent<BattleActor>(); // 전투 액터 컴포넌트 생성
            actor.Initialize(team, stats, Vector3.zero); // 전투 액터 상태 초기화
            registry.Register(actor); // 전투 Registry 등록
            return actorObject; // 생성된 전투 액터 객체 반환
        }
    }
}
