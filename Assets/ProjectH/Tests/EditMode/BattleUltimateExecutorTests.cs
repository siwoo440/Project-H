using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 궁극기 전투 기능
using ProjectH.Data; // 전투 포지션 기능
using UnityEngine; // Unity 게임 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleUltimateExecutorTests // 궁극기 실행 흐름 테스트
    {
        private GameObject registryObject; // 테스트 Registry 객체
        private BattleCombatRegistry registry; // 테스트 전투 Registry

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 궁극기 실행 테스트 준비
        {
            BattleRuntimeStates.ResetAll(); // 전투 정적 상태 전체 초기화 (최적화 통합 창구)
            registryObject = new GameObject("BattleUltimateExecutorTests.Registry"); // 테스트 Registry 객체 생성
            registry = registryObject.AddComponent<BattleCombatRegistry>(); // 테스트 Registry 컴포넌트 생성
            BattleSkillRuntimeState.SetRegistry(registry); // 테스트 스킬 Runtime Registry 연결
            BattlePassiveSystem.Initialize(registry); // 테스트 패시브 시스템 Registry 연결
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 궁극기 실행 테스트 정리
        {
            BattleRuntimeStates.ResetAll(); // 전투 정적 상태 전체 초기화 (최적화 통합 창구)
            BattlePassiveSystem.Shutdown(registry); // 테스트 패시브 시스템 연결 해제
            BattlePassiveRuntimeState.ResetAll(); // 테스트 패시브 Runtime 초기화
            Object.DestroyImmediate(registryObject); // 테스트 Registry 객체 제거
        }

        [Test] // 테스트 표시
        public void TryExecute_NotReady_DoesNotConsumeGauge() // 미완충 게이지 소비 차단 검증
        {
            BattleActor owner = CreateAlly("ALLY_0", "CH_SERENA", 100, 50, 10); // 세레나 테스트 액터 생성
            BattleActor enemy = CreateEnemy("ENEMY_0", 1000, 10, 0, 0); // 전투 진행용 적 생성
            BattleUltimateGaugeRuntimeState.AddGauge("CH_SERENA", 99); // 세레나 게이지 99 충전

            BattleUltimateExecutionResult result = BattleUltimateExecutor.TryExecute("CH_SERENA", registry); // 미완충 궁극기 실행 시도

            Assert.That(result.Succeeded, Is.False); // 미완충 궁극기 실행 실패 검증
            Assert.That(BattleUltimateGaugeRuntimeState.GetGauge("CH_SERENA"), Is.EqualTo(99)); // 실패 후 게이지 유지 검증
            Object.DestroyImmediate(owner.gameObject); // 세레나 테스트 객체 제거
            Object.DestroyImmediate(enemy.gameObject); // 전투 진행용 적 테스트 객체 제거
        }

        [Test] // 테스트 표시
        public void TryExecute_LiliaReady_ConsumesGaugeAndDamagesAllEnemiesOnce() // 릴리아 궁극기 소비와 전체 피해 검증
        {
            BattleActor owner = CreateAlly("ALLY_0", "CH_LILIA", 100, 100, 10); // 릴리아 테스트 액터 생성
            BattleActor enemyA = CreateEnemy("ENEMY_0", 1000, 10, 0, 0); // 첫 번째 적 생성
            BattleActor enemyB = CreateEnemy("ENEMY_1", 1000, 10, 0, 0); // 두 번째 적 생성
            BattleUltimateGaugeRuntimeState.AddGauge("CH_LILIA", 100); // 릴리아 게이지 완충

            BattleUltimateExecutionResult result = BattleUltimateExecutor.TryExecute("CH_LILIA", registry); // 릴리아 궁극기 실행

            Assert.That(result.Succeeded, Is.True); // 릴리아 궁극기 실행 성공 검증
            Assert.That(BattleUltimateGaugeRuntimeState.GetGauge("CH_LILIA"), Is.EqualTo(0)); // 궁극기 사용 후 게이지 0 검증
            Assert.That(enemyA.Stats.CurrentHp, Is.EqualTo(740)); // 첫 번째 적 260퍼센트 피해 검증
            Assert.That(enemyB.Stats.CurrentHp, Is.EqualTo(740)); // 두 번째 적 260퍼센트 피해 검증
            Object.DestroyImmediate(owner.gameObject); // 릴리아 테스트 객체 제거
            Object.DestroyImmediate(enemyA.gameObject); // 첫 번째 적 테스트 객체 제거
            Object.DestroyImmediate(enemyB.gameObject); // 두 번째 적 테스트 객체 제거
        }

        [Test] // 테스트 표시
        public void TryExecute_DeadOwner_DoesNotConsumeGauge() // 사망 캐릭터 궁극기 차단 검증
        {
            BattleActor owner = CreateAlly("ALLY_0", "CH_EVE", 100, 100, 10); // 이브 테스트 액터 생성
            BattleActor enemy = CreateEnemy("ENEMY_0", 1000, 10, 0, 0); // 전투 진행용 적 생성
            BattleStats stats = owner.Stats as BattleStats; // 이브 전투 스탯 변환
            stats.SetCurrentHp(0); // 이브 전투 불능 처리
            BattleUltimateGaugeRuntimeState.AddGauge("CH_EVE", 100); // 이브 게이지 완충

            BattleUltimateExecutionResult result = BattleUltimateExecutor.TryExecute("CH_EVE", registry); // 사망 이브 궁극기 실행 시도

            Assert.That(result.Succeeded, Is.False); // 사망 이브 궁극기 실행 실패 검증
            Assert.That(BattleUltimateGaugeRuntimeState.GetGauge("CH_EVE"), Is.EqualTo(100)); // 사망 실패 후 게이지 유지 검증
            Object.DestroyImmediate(owner.gameObject); // 이브 테스트 객체 제거
            Object.DestroyImmediate(enemy.gameObject); // 전투 진행용 적 테스트 객체 제거
        }

        private BattleActor CreateAlly(string runtimeId, string characterId, int maxHp, int attack, int defense) // 아군 테스트 액터 생성
        {
            GameObject targetObject = new GameObject(runtimeId); // 아군 테스트 객체 생성
            BattleActor actor = targetObject.AddComponent<BattleActor>(); // 아군 전투 액터 추가
            BattleStats stats = new BattleStats(runtimeId, characterId, characterId, BattlePosition.Dealer, 1, maxHp, attack, defense, 1f, 1f, 0f); // 아군 전투 스탯 생성
            actor.Initialize(BattleTeam.Ally, stats, Vector3.zero); // 아군 전투 액터 초기화
            registry.Register(actor); // 아군 Registry 등록
            return actor; // 생성 아군 액터 반환
        }

        private BattleActor CreateEnemy(string runtimeId, int maxHp, int attack, int defense, int resistance) // 적군 테스트 액터 생성
        {
            GameObject targetObject = new GameObject(runtimeId); // 적군 테스트 객체 생성
            BattleActor actor = targetObject.AddComponent<BattleActor>(); // 적군 전투 액터 추가
            BattleEnemyStats stats = new BattleEnemyStats(runtimeId, "MON_TEST", runtimeId, maxHp, attack, defense, resistance, 1f, 1f, 1f); // 적군 전투 스탯 생성
            actor.Initialize(BattleTeam.Enemy, stats, Vector3.right); // 적군 전투 액터 초기화
            registry.Register(actor); // 적군 Registry 등록
            return actor; // 생성 적군 액터 반환
        }
    }
}
