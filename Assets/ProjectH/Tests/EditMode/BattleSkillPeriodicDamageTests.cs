using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 Runtime 및 피해 기능
using UnityEngine; // Unity 게임 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleSkillPeriodicDamageTests // 주기 피해 Runtime 테스트
    {
        private GameObject registryObject; // 테스트 Registry 객체
        private BattleCombatRegistry registry; // 테스트 Registry
        private GameObject attackerObject; // 테스트 공격자 객체
        private GameObject targetObject; // 테스트 대상 객체
        private BattleActor attacker; // 테스트 공격자
        private BattleActor target; // 테스트 대상

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 주기 피해 테스트 준비
        {
            registryObject = new GameObject("Registry"); // Registry 객체 생성
            registry = registryObject.AddComponent<BattleCombatRegistry>(); // Registry 컴포넌트 추가
            attackerObject = new GameObject("Attacker"); // 공격자 객체 생성
            targetObject = new GameObject("Target"); // 대상 객체 생성
            attacker = attackerObject.AddComponent<BattleActor>(); // 공격자 BattleActor 추가
            target = targetObject.AddComponent<BattleActor>(); // 대상 BattleActor 추가
            BattleStats attackerStats = new BattleStats("ALLY_0", "CH_TEST", "Attacker", ProjectH.Data.BattlePosition.Dealer, 1, 100, 50, 0, 1f, 1f, 0f); // 공격자 스탯 생성
            BattleEnemyStats targetStats = new BattleEnemyStats("ENEMY_0", "MON_TEST", "Target", 300, 10, 0, 0, 1f, 1f, 1f); // 대상 스탯 생성
            attacker.Initialize(BattleTeam.Ally, attackerStats, Vector3.zero); // 공격자 초기화
            target.Initialize(BattleTeam.Enemy, targetStats, Vector3.zero); // 대상 초기화
            registry.Register(attacker); // 공격자 Registry 등록
            registry.Register(target); // 대상 Registry 등록
            BattleSkillRuntimeState.SetRegistry(registry); // Runtime Registry 연결
            BattleSkillRuntimeState.ResetAll(); // Runtime 상태 초기화
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 주기 피해 테스트 정리
        {
            BattleSkillRuntimeState.ResetAll(); // Runtime 상태 정리
            BattleSkillRuntimeState.SetRegistry(null); // Runtime Registry 연결 해제
            Object.DestroyImmediate(attackerObject); // 공격자 객체 제거
            Object.DestroyImmediate(targetObject); // 대상 객체 제거
            Object.DestroyImmediate(registryObject); // Registry 객체 제거
        }

        [Test] // 테스트 표시
        public void PeriodicDamage_AppliesRequestedTickCount() // 지정 Tick 수 주기 피해 검증
        {
            BattleSkillRuntimeState.AddPeriodicDamage(attacker.Stats, target.Stats.RuntimeId, BattleDamageType.Magic, 30, 1f, 3, "SK_TEST:DOT", 0f); // 30 피해 3회 DoT 등록

            BattleSkillRuntimeState.TickPeriodicEffects(0.9f); // 첫 Tick 이전 갱신
            Assert.That(target.Stats.CurrentHp, Is.EqualTo(300)); // 첫 Tick 이전 체력 유지 검증
            BattleSkillRuntimeState.TickPeriodicEffects(1f); // 첫 Tick 실행
            Assert.That(target.Stats.CurrentHp, Is.EqualTo(270)); // 첫 Tick 피해 검증
            BattleSkillRuntimeState.TickPeriodicEffects(2f); // 둘째 Tick 실행
            BattleSkillRuntimeState.TickPeriodicEffects(3f); // 셋째 Tick 실행
            Assert.That(target.Stats.CurrentHp, Is.EqualTo(210)); // 총 3회 피해 검증
        }
    }
}
