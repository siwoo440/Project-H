using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 궁극기 효과 기능
using ProjectH.Data; // 전투 포지션 기능
using UnityEngine; // Unity 게임 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleUltimateEffectTests // 초기 4인 궁극기 효과 테스트
    {
        private GameObject registryObject; // 테스트 Registry 객체
        private BattleCombatRegistry registry; // 테스트 전투 Registry

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 궁극기 효과 테스트 준비
        {
            BattleUltimateGaugeRuntimeState.ResetAll(); // 이전 궁극기 게이지 초기화
            BattleSkillRuntimeState.ResetAll(); // 이전 스킬 Runtime 초기화
            BattlePassiveRuntimeState.ResetAll(); // 이전 패시브 Runtime 초기화
            registryObject = new GameObject("BattleUltimateEffectTests.Registry"); // 테스트 Registry 객체 생성
            registry = registryObject.AddComponent<BattleCombatRegistry>(); // 테스트 Registry 컴포넌트 생성
            BattleSkillRuntimeState.SetRegistry(registry); // 테스트 스킬 Runtime Registry 연결
            BattlePassiveSystem.Initialize(registry); // 테스트 패시브 시스템 Registry 연결
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 궁극기 효과 테스트 정리
        {
            BattleUltimateGaugeRuntimeState.ResetAll(); // 테스트 궁극기 게이지 초기화
            BattleSkillRuntimeState.ResetAll(); // 테스트 스킬 Runtime 초기화
            BattlePassiveSystem.Shutdown(registry); // 테스트 패시브 시스템 연결 해제
            BattlePassiveRuntimeState.ResetAll(); // 테스트 패시브 Runtime 초기화
            Object.DestroyImmediate(registryObject); // 테스트 Registry 객체 제거
        }

        [Test] // 테스트 표시
        public void EllenUltimate_AppliesPartyDamageReductionAndSelfDefense() // 엘렌 파티 피해 감소와 자기 방어 증가 검증
        {
            BattleActor ellen = CreateAlly("ALLY_0", "CH_ELLEN", BattlePosition.Tank, 1000, 100, 100); // 엘렌 테스트 액터 생성
            BattleActor ally = CreateAlly("ALLY_1", "CH_SERENA", BattlePosition.Healer, 1000, 50, 50); // 아군 테스트 액터 생성
            BattleActor enemy = CreateEnemy("ENEMY_0", 1000, 10, 0, 0); // 전투 진행용 적 생성
            BattleUltimateGaugeRuntimeState.AddGauge("CH_ELLEN", 100); // 엘렌 게이지 완충

            BattleUltimateExecutionResult result = BattleUltimateExecutor.TryExecute("CH_ELLEN", registry); // 엘렌 궁극기 실행

            Assert.That(result.Succeeded, Is.True); // 엘렌 궁극기 실행 성공 검증
            Assert.That(BattleSkillRuntimeState.GetDamageReduction(ellen.Stats.RuntimeId), Is.EqualTo(0.20f).Within(0.001f)); // 엘렌 피해 감소 20퍼센트 검증
            Assert.That(BattleSkillRuntimeState.GetDamageReduction(ally.Stats.RuntimeId), Is.EqualTo(0.20f).Within(0.001f)); // 아군 피해 감소 20퍼센트 검증
            Assert.That(BattleSkillRuntimeState.GetEffectiveDefense(ellen.Stats), Is.EqualTo(150)); // 엘렌 방어력 50퍼센트 증가 검증
            Object.DestroyImmediate(ellen.gameObject); // 엘렌 테스트 객체 제거
            Object.DestroyImmediate(ally.gameObject); // 아군 테스트 객체 제거
            Object.DestroyImmediate(enemy.gameObject); // 적군 테스트 객체 제거
        }

        [Test] // 테스트 표시
        public void LiliaUltimate_AppliesResistanceReductionForTenSeconds() // 릴리아 마법 저항 감소 검증
        {
            BattleActor lilia = CreateAlly("ALLY_0", "CH_LILIA", BattlePosition.Dealer, 1000, 100, 20); // 릴리아 테스트 액터 생성
            BattleActor enemy = CreateEnemy("ENEMY_0", 1000, 10, 0, 100); // 저항력 보유 적 생성
            BattleUltimateGaugeRuntimeState.AddGauge("CH_LILIA", 100); // 릴리아 게이지 완충

            BattleUltimateExecutionResult result = BattleUltimateExecutor.TryExecute("CH_LILIA", registry); // 릴리아 궁극기 실행

            Assert.That(result.Succeeded, Is.True); // 릴리아 궁극기 실행 성공 검증
            Assert.That(enemy.Stats.CurrentHp, Is.EqualTo(840)); // 저항 적용 전 260퍼센트 마법 피해 검증
            Assert.That(BattleSkillRuntimeState.GetEffectiveResistance(enemy.Stats), Is.EqualTo(80)); // 마법 저항 20퍼센트 감소 검증
            Object.DestroyImmediate(lilia.gameObject); // 릴리아 테스트 객체 제거
            Object.DestroyImmediate(enemy.gameObject); // 적군 테스트 객체 제거
        }

        [Test] // 테스트 표시
        public void EveUltimate_DamagesAndStunsAllLivingEnemies() // 이브 전체 피해와 1초 기절 검증
        {
            BattleActor eve = CreateAlly("ALLY_0", "CH_EVE", BattlePosition.Dealer, 1000, 100, 20); // 이브 테스트 액터 생성
            BattleActor enemyA = CreateEnemy("ENEMY_0", 1000, 10, 0, 0); // 첫 번째 적 생성
            BattleActor enemyB = CreateEnemy("ENEMY_1", 1000, 10, 0, 0); // 두 번째 적 생성
            BattleUltimateGaugeRuntimeState.AddGauge("CH_EVE", 100); // 이브 게이지 완충

            BattleUltimateExecutionResult result = BattleUltimateExecutor.TryExecute("CH_EVE", registry); // 이브 궁극기 실행

            Assert.That(result.Succeeded, Is.True); // 이브 궁극기 실행 성공 검증
            Assert.That(enemyA.Stats.CurrentHp, Is.EqualTo(780)); // 첫 번째 적 220퍼센트 피해 검증
            Assert.That(enemyB.Stats.CurrentHp, Is.EqualTo(780)); // 두 번째 적 220퍼센트 피해 검증
            Assert.That(BattleSkillRuntimeState.IsStunned(enemyA.Stats.RuntimeId), Is.True); // 첫 번째 적 1초 기절 활성 검증
            Assert.That(BattleSkillRuntimeState.IsStunned(enemyB.Stats.RuntimeId), Is.True); // 두 번째 적 1초 기절 활성 검증
            Object.DestroyImmediate(eve.gameObject); // 이브 테스트 객체 제거
            Object.DestroyImmediate(enemyA.gameObject); // 첫 번째 적 테스트 객체 제거
            Object.DestroyImmediate(enemyB.gameObject); // 두 번째 적 테스트 객체 제거
        }

        [Test] // 테스트 표시
        public void SerenaUltimate_HealsCleansesAndRevivesOneAlly() // 세레나 회복 정화 부활 검증
        {
            BattleActor serena = CreateAlly("ALLY_0", "CH_SERENA", BattlePosition.Healer, 1000, 50, 20); // 세레나 테스트 액터 생성
            BattleActor wounded = CreateAlly("ALLY_1", "CH_ELLEN", BattlePosition.Tank, 1000, 50, 100); // 부상 아군 테스트 액터 생성
            BattleActor defeated = CreateAlly("ALLY_2", "CH_EVE", BattlePosition.Dealer, 100, 50, 10); // 전투불능 아군 테스트 액터 생성
            BattleDeathHandler deathHandler = defeated.gameObject.AddComponent<BattleDeathHandler>(); // 전투불능 처리기 추가
            deathHandler.Configure(defeated, registry, null, null, null, null); // 전투불능 처리기 참조 연결
            BattleStats woundedStats = wounded.Stats as BattleStats; // 부상 아군 스탯 변환
            BattleStats defeatedStats = defeated.Stats as BattleStats; // 전투불능 아군 스탯 변환
            woundedStats.SetCurrentHp(500); // 부상 아군 체력 50퍼센트 설정
            BattleSkillRuntimeState.AddModifier(wounded.Stats.RuntimeId, BattleRuntimeModifierKind.Stun, 1f, 30f, "TEST_STUN"); // 부상 아군 기절 Debuff 추가
            BattleSkillRuntimeState.RegisterRemovableDebuff(wounded.Stats.RuntimeId, "TEST_STUN"); // 기절 Debuff 정화 대상으로 등록
            defeatedStats.SetCurrentHp(0); // 아군 전투불능 처리
            BattleActor enemy = CreateEnemy("ENEMY_0", 1000, 10, 0, 0); // 전투 진행용 적 생성
            BattleUltimateGaugeRuntimeState.AddGauge("CH_SERENA", 100); // 세레나 게이지 완충

            BattleUltimateExecutionResult result = BattleUltimateExecutor.TryExecute("CH_SERENA", registry); // 세레나 궁극기 실행

            Assert.That(result.Succeeded, Is.True); // 세레나 궁극기 실행 성공 검증
            Assert.That(woundedStats.CurrentHp, Is.EqualTo(750)); // 아군 최대 체력 25퍼센트 회복 검증
            Assert.That(BattleSkillRuntimeState.IsStunned(wounded.Stats.RuntimeId), Is.False); // 아군 상태이상 전체 제거 검증
            Assert.That(defeatedStats.CurrentHp, Is.EqualTo(25)); // 전투불능 아군 25퍼센트 체력 부활 검증
            Assert.That(registry.Contains(defeated), Is.True); // 부활 아군 Registry 복귀 검증
            Object.DestroyImmediate(serena.gameObject); // 세레나 테스트 객체 제거
            Object.DestroyImmediate(wounded.gameObject); // 부상 아군 테스트 객체 제거
            Object.DestroyImmediate(defeated.gameObject); // 부활 아군 테스트 객체 제거
            Object.DestroyImmediate(enemy.gameObject); // 전투 진행용 적 테스트 객체 제거
        }

        private BattleActor CreateAlly(string runtimeId, string characterId, BattlePosition position, int maxHp, int attack, int defense) // 아군 테스트 액터 생성
        {
            GameObject targetObject = new GameObject(runtimeId); // 아군 테스트 객체 생성
            BattleActor actor = targetObject.AddComponent<BattleActor>(); // 아군 전투 액터 추가
            BattleStats stats = new BattleStats(runtimeId, characterId, characterId, position, 1, maxHp, attack, defense, 1f, 1f, 0f); // 아군 전투 스탯 생성
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
