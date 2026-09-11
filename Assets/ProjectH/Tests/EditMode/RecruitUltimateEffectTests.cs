using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 궁극기·전투 상태 기능
using ProjectH.Data; // 전투 포지션 기능
using UnityEngine; // Unity 게임 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class RecruitUltimateEffectTests // Day64 합류 8인 궁극기 · 신규 효과(방어/공격 감소 · 버프 제거) 테스트
    {
        private GameObject registryObject; // 테스트 Registry 객체
        private BattleCombatRegistry registry; // 테스트 전투 Registry

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 테스트 준비
        {
            BattleRuntimeStates.ResetAll(); // 전투 정적 상태 전체 초기화
            registryObject = new GameObject("RecruitUltimateEffectTests.Registry"); // Registry 객체 생성
            registry = registryObject.AddComponent<BattleCombatRegistry>(); // Registry 컴포넌트 생성
            BattleSkillRuntimeState.SetRegistry(registry); // 스킬 Runtime Registry 연결
            BattlePassiveSystem.Initialize(registry); // 패시브 시스템 Registry 연결
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 테스트 정리
        {
            BattleRuntimeStates.ResetAll(); // 전투 정적 상태 전체 초기화
            BattlePassiveSystem.Shutdown(registry); // 패시브 시스템 연결 해제
            BattlePassiveRuntimeState.ResetAll(); // 패시브 Runtime 초기화
            foreach (BattleActor actor in Object.FindObjectsByType<BattleActor>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.DestroyImmediate(actor.gameObject); // 테스트 액터 제거
            Object.DestroyImmediate(registryObject); // Registry 객체 제거
        }

        [Test] // 티리아 : 아군 전체에 티리아 최대 체력 18% 보호막 + 피해 감소 15%
        public void Tyria_ShieldsAllAlliesByOwnerMaxHp() // 티리아 테스트
        {
            BattleActor tyria = CreateAlly("ALLY_0", "CH_TYRIA", BattlePosition.Tank, 1000, 50, 100); // 티리아
            BattleActor ally = CreateAlly("ALLY_1", "CH_SERENA", BattlePosition.Healer, 500, 50, 20); // 체력 낮은 아군
            CreateEnemy("ENEMY_0", 1000, 10, 0, 0); // 적
            Assert.That(Execute("CH_TYRIA"), Is.True); // 실행
            Assert.That(BattlePassiveRuntimeState.GetShield(tyria.Stats.RuntimeId), Is.EqualTo(180)); // 1000 × 18%
            Assert.That(BattlePassiveRuntimeState.GetShield(ally.Stats.RuntimeId), Is.EqualTo(180)); // 아군도 티리아 체력 기준
            Assert.That(BattleSkillRuntimeState.GetDamageReduction(ally.Stats.RuntimeId), Is.EqualTo(0.15f).Within(0.001f)); // 피해 감소 15%
        }

        [Test] // 세피라 : 전체 35% 회복 + 쓰러진 아군 1명 35% 부활 + 공격력 18%
        public void Sephira_HealsRevivesAndBuffsAttack() // 세피라 테스트
        {
            BattleActor sephira = CreateAlly("ALLY_0", "CH_SEPHIRA", BattlePosition.Healer, 1000, 50, 20); // 세피라
            BattleActor wounded = CreateAlly("ALLY_1", "CH_ELLEN", BattlePosition.Tank, 1000, 50, 100); // 부상 아군
            BattleActor defeated = CreateAlly("ALLY_2", "CH_EVE", BattlePosition.Dealer, 100, 50, 10); // 쓰러진 아군
            BattleDeathHandler deathHandler = defeated.gameObject.AddComponent<BattleDeathHandler>(); // 전투불능 처리기
            deathHandler.Configure(defeated, registry, null, null, null, null); // 참조 연결
            ((BattleStats)wounded.Stats).SetCurrentHp(500); // 50%
            ((BattleStats)defeated.Stats).SetCurrentHp(0); // 전투불능
            CreateEnemy("ENEMY_0", 1000, 10, 0, 0); // 적
            Assert.That(Execute("CH_SEPHIRA"), Is.True); // 실행
            Assert.That(wounded.Stats.CurrentHp, Is.EqualTo(850)); // 500 + 1000 × 35%
            Assert.That(defeated.Stats.CurrentHp, Is.EqualTo(35)); // 100 × 35% 부활
            Assert.That(BattleSkillRuntimeState.GetAttackMultiplier(sephira.Stats.RuntimeId), Is.EqualTo(1.18f).Within(0.001f)); // 공격력 +18%
        }

        [Test] // 파이라 : 전체 300% 마법 피해 · 리듬 배율이 피해에 곱해짐
        public void Pyra_DealsMagicDamage_ScaledByRhythm() // 파이라 테스트
        {
            BattleActor pyra = CreateAlly("ALLY_0", "CH_PYRA", BattlePosition.Dealer, 1000, 100, 20); // 파이라
            BattleActor enemyA = CreateEnemy("ENEMY_0", 1000, 10, 100, 0); // 방어 높고 저항 0
            BattleActor enemyB = CreateEnemy("ENEMY_1", 1000, 10, 100, 0); // 두 번째 적
            Assert.That(Execute("CH_PYRA"), Is.True); // 실행 (배율 1.0)
            Assert.That(enemyA.Stats.CurrentHp, Is.EqualTo(700)); // 300 마법 피해 (방어 무관)
            Assert.That(enemyB.Stats.CurrentHp, Is.EqualTo(700)); // 전체 공격
            BattleUltimateEffectExecutor.Execute("CH_PYRA", pyra, registry, 1.5f); // 리듬 배율 1.5
            Assert.That(enemyA.Stats.CurrentHp, Is.EqualTo(250)); // 450 피해
        }

        [Test] // 루시아 : 방어 무시 200% × 3발
        public void Lucia_IgnoresDefense() // 루시아 테스트
        {
            CreateAlly("ALLY_0", "CH_LUCIA", BattlePosition.Dealer, 1000, 100, 20); // 루시아
            BattleActor enemy = CreateEnemy("ENEMY_0", 1000, 10, 80, 0); // 방어 80
            Assert.That(Execute("CH_LUCIA"), Is.True); // 실행
            Assert.That(enemy.Stats.CurrentHp, Is.EqualTo(400)); // 600 고정 피해
        }

        [Test] // 나타샤 : 150% × 4, 자신 체력 50% 이하면 150% 추가
        public void Natasha_DealsBonusDamageWhenLowHp() // 나타샤 테스트
        {
            BattleActor natasha = CreateAlly("ALLY_0", "CH_NATASHA", BattlePosition.Dealer, 1000, 100, 20); // 나타샤
            BattleActor enemy = CreateEnemy("ENEMY_0", 2000, 10, 0, 0); // 적
            Assert.That(Execute("CH_NATASHA"), Is.True); // 실행 (체력 가득)
            Assert.That(enemy.Stats.CurrentHp, Is.EqualTo(1400)); // 600 피해
            ((BattleStats)natasha.Stats).SetCurrentHp(500); // 체력 50%
            BattleUltimateEffectExecutor.Execute("CH_NATASHA", natasha, registry); // 다시 실행
            Assert.That(enemy.Stats.CurrentHp, Is.EqualTo(650)); // 750 피해
        }

        [Test] // 메르시아 : 전체 220% + 기절 + 방어 15% 감소
        public void Mercia_StunsAndLowersDefense() // 메르시아 테스트
        {
            CreateAlly("ALLY_0", "CH_MERCIA", BattlePosition.Dealer, 1000, 100, 20); // 메르시아
            BattleActor enemy = CreateEnemy("ENEMY_0", 1000, 10, 100, 0); // 방어 100
            Assert.That(Execute("CH_MERCIA"), Is.True); // 실행
            Assert.That(enemy.Stats.CurrentHp, Is.EqualTo(880)); // 220 - 방어 100
            Assert.That(BattleSkillRuntimeState.IsStunned(enemy.Stats.RuntimeId), Is.True); // 기절
            Assert.That(BattleSkillRuntimeState.GetEffectiveDefense(enemy.Stats), Is.EqualTo(85)); // 방어 15% 감소
        }

        [Test] // 노엘 : 전체 60% × 5연타
        public void Noel_HitsAllEnemiesFiveTimes() // 노엘 테스트
        {
            CreateAlly("ALLY_0", "CH_NOEL", BattlePosition.Dealer, 1000, 100, 20); // 노엘
            BattleActor enemyA = CreateEnemy("ENEMY_0", 1000, 10, 0, 0); // 적 1
            BattleActor enemyB = CreateEnemy("ENEMY_1", 1000, 10, 0, 0); // 적 2
            Assert.That(Execute("CH_NOEL"), Is.True); // 실행
            Assert.That(enemyA.Stats.CurrentHp, Is.EqualTo(700)); // 300 피해
            Assert.That(enemyB.Stats.CurrentHp, Is.EqualTo(700)); // 300 피해
        }

        [Test] // 클레어 : 전체 25% 회복 + 피해 감소 10%
        public void Claire_HealsAndReducesDamage() // 클레어 테스트
        {
            CreateAlly("ALLY_0", "CH_CLAIRE", BattlePosition.Healer, 1000, 50, 20); // 클레어
            BattleActor wounded = CreateAlly("ALLY_1", "CH_ELLEN", BattlePosition.Tank, 1000, 50, 100); // 부상 아군
            ((BattleStats)wounded.Stats).SetCurrentHp(500); // 50%
            CreateEnemy("ENEMY_0", 1000, 10, 0, 0); // 적
            Assert.That(Execute("CH_CLAIRE"), Is.True); // 실행
            Assert.That(wounded.Stats.CurrentHp, Is.EqualTo(750)); // 25% 회복
            Assert.That(BattleSkillRuntimeState.GetDamageReduction(wounded.Stats.RuntimeId), Is.EqualTo(0.10f).Within(0.001f)); // 피해 감소 10%
        }

        [Test] // 방어 감소 · 공격 감소 Modifier 계산과 상태 표시 분류
        public void DefenseAndAttackReduction_AreDebuffs() // 신규 약화 테스트
        {
            BattleActor enemy = CreateEnemy("ENEMY_0", 1000, 100, 100, 0); // 적
            BattleSkillRuntimeState.AddModifier(enemy.Stats.RuntimeId, BattleRuntimeModifierKind.DefenseReductionPercent, 0.2f, 5f, "TEST_DEF"); // 방어 20% 감소
            BattleSkillRuntimeState.AddModifier(enemy.Stats.RuntimeId, BattleRuntimeModifierKind.AttackReductionPercent, 0.25f, 5f, "TEST_ATK"); // 공격 25% 감소
            Assert.That(BattleSkillRuntimeState.GetEffectiveDefense(enemy.Stats), Is.EqualTo(80)); // 방어 80
            Assert.That(BattleSkillRuntimeState.GetAttackMultiplier(enemy.Stats.RuntimeId), Is.EqualTo(0.75f).Within(0.001f)); // 공격 배율 0.75
            Assert.That(BattleStatusEffectCatalog.IsDebuff(BattleStatusEffectCatalog.FromModifierKind(BattleRuntimeModifierKind.DefenseReductionPercent)), Is.True); // 약화 분류
            Assert.That(BattleStatusEffectCatalog.IsDebuff(BattleStatusEffectCatalog.FromModifierKind(BattleRuntimeModifierKind.AttackReductionPercent)), Is.True); // 약화 분류
        }

        [Test] // 버프 제거 : 남은 시간이 긴 버프부터, 약화는 남김
        public void RemoveBuffs_RemovesLongestBuffOnly() // 버프 제거 테스트
        {
            BattleActor enemy = CreateEnemy("ENEMY_0", 1000, 100, 100, 0); // 적
            string id = enemy.Stats.RuntimeId; // 런타임 ID
            BattleSkillRuntimeState.AddModifier(id, BattleRuntimeModifierKind.AttackPercent, 0.2f, 5f, "TEST_ATK_UP"); // 공격 증가 5초
            BattleSkillRuntimeState.AddModifier(id, BattleRuntimeModifierKind.DefensePercent, 0.5f, 10f, "TEST_DEF_UP"); // 방어 증가 10초
            BattleSkillRuntimeState.AddModifier(id, BattleRuntimeModifierKind.DefenseReductionPercent, 0.1f, 20f, "TEST_DEF_DOWN"); // 방어 감소 20초 (약화)
            Assert.That(BattleSkillRuntimeState.RemoveBuffs(id, 1), Is.EqualTo(1)); // 1개 제거
            Assert.That(BattleSkillRuntimeState.GetEffectiveDefense(enemy.Stats), Is.EqualTo(90)); // 방어 증가(10초)가 먼저 제거, 감소는 유지
            Assert.That(BattleSkillRuntimeState.GetAttackMultiplier(id), Is.EqualTo(1.2f).Within(0.001f)); // 공격 증가는 남음
            Assert.That(BattleSkillRuntimeState.RemoveBuffs(id, 5), Is.EqualTo(1)); // 남은 버프 1개만
        }

        private bool Execute(string characterId) // 게이지 완충 후 궁극기 실행
        {
            BattleUltimateGaugeRuntimeState.AddGauge(characterId, 100); // 게이지 완충
            return BattleUltimateExecutor.TryExecute(characterId, registry).Succeeded; // 실행 결과
        }

        private BattleActor CreateAlly(string runtimeId, string characterId, BattlePosition position, int maxHp, int attack, int defense) // 아군 테스트 액터 생성
        {
            GameObject targetObject = new GameObject(runtimeId); // 객체 생성
            BattleActor actor = targetObject.AddComponent<BattleActor>(); // 전투 액터
            BattleStats stats = new BattleStats(runtimeId, characterId, characterId, position, 1, maxHp, attack, defense, 1f, 1f, 0f); // 스탯
            actor.Initialize(BattleTeam.Ally, stats, Vector3.zero); // 초기화
            registry.Register(actor); // 등록
            return actor; // 반환
        }

        private BattleActor CreateEnemy(string runtimeId, int maxHp, int attack, int defense, int resistance) // 적군 테스트 액터 생성
        {
            GameObject targetObject = new GameObject(runtimeId); // 객체 생성
            BattleActor actor = targetObject.AddComponent<BattleActor>(); // 전투 액터
            BattleEnemyStats stats = new BattleEnemyStats(runtimeId, "MON_TEST", runtimeId, maxHp, attack, defense, resistance, 1f, 1f, 1f); // 스탯
            actor.Initialize(BattleTeam.Enemy, stats, Vector3.right); // 초기화
            registry.Register(actor); // 등록
            return actor; // 반환
        }
    }
}
