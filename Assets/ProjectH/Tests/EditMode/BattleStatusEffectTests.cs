using System; // 열거형 순회 기능
using System.Collections.Generic; // 목록 자료형
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 상태이상 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleStatusEffectTests // Day51 상태이상 통합 회귀 테스트
    {
        private readonly List<BattleStatusEffectSnapshot> buffer = new List<BattleStatusEffectSnapshot>(); // 상태이상 수집 재사용 버퍼

        [SetUp] // 각 테스트 시작 전 처리
        public void SetUp() // 스킬 Runtime 상태 초기화
        {
            BattleSkillRuntimeState.ResetAll(); // 이전 테스트 잔여 상태 제거
            buffer.Clear(); // 수집 버퍼 초기화
        }

        [TearDown] // 각 테스트 종료 후 처리
        public void TearDown() // 스킬 Runtime 상태 정리
        {
            BattleSkillRuntimeState.ResetAll(); // 다음 테스트 영향 방지
        }

        [Test] // 카탈로그 전체 항목 정보 보유 검증
        public void Catalog_EveryEffect_HasLabelAndShortLabel() // 카탈로그 완전성 테스트
        {
            foreach (BattleStatusEffectId id in Enum.GetValues(typeof(BattleStatusEffectId))) // 전체 상태이상 종류 순회
            {
                if (id == BattleStatusEffectId.None) // 상태이상 없음 항목 확인
                {
                    continue; // 표시 대상 아님 제외
                }

                Assert.That(BattleStatusEffectCatalog.GetLabel(id), Is.Not.EqualTo("-")); // 표시 문구 정의 검증
                Assert.That(BattleStatusEffectCatalog.GetShortLabel(id), Is.Not.EqualTo("?")); // 축약 문구 정의 검증
                Assert.That(BattleStatusEffectCatalog.IsDebuff(id) || BattleStatusEffectCatalog.IsBuff(id), Is.True); // 버프 또는 디버프 분류 검증
            }
        }

        [Test] // Modifier 종류별 상태이상 매핑 검증
        public void Catalog_MapsEveryModifierKind() // Modifier 매핑 테스트
        {
            foreach (BattleRuntimeModifierKind kind in Enum.GetValues(typeof(BattleRuntimeModifierKind))) // 전체 Modifier 종류 순회
            {
                Assert.That(BattleStatusEffectCatalog.FromModifierKind(kind), Is.Not.EqualTo(BattleStatusEffectId.None)); // 모든 Modifier가 상태이상으로 매핑되는지 검증
            }
        }

        [Test] // 활성 Modifier 수집 검증
        public void CollectStatusEffects_ReturnsActiveModifier() // 상태이상 수집 테스트
        {
            BattleSkillRuntimeState.AddModifier("ALLY_0", BattleRuntimeModifierKind.DefensePercent, 0.5f, 6f, "TEST_DEF", 10f); // 방어 증가 6초 등록
            BattleSkillRuntimeState.CollectStatusEffects("ALLY_0", buffer, 12f); // 2초 경과 시점 상태이상 수집

            Assert.That(buffer.Count, Is.EqualTo(1)); // 수집 항목 수 검증
            Assert.That(buffer[0].Id, Is.EqualTo(BattleStatusEffectId.DefenseUp)); // 방어 증가 상태이상 검증
            Assert.That(buffer[0].RemainingSeconds, Is.EqualTo(4f).Within(0.0001f)); // 남은 지속시간 검증
            Assert.That(buffer[0].StackCount, Is.EqualTo(1)); // 중첩 수 검증
        }

        [Test] // 만료 상태이상 제외 검증
        public void CollectStatusEffects_ExcludesExpiredModifier() // 만료 제외 테스트
        {
            BattleSkillRuntimeState.AddModifier("ALLY_0", BattleRuntimeModifierKind.Stun, 1f, 2f, "TEST_STUN", 10f); // 기절 2초 등록
            BattleSkillRuntimeState.CollectStatusEffects("ALLY_0", buffer, 13f); // 만료 이후 시점 상태이상 수집

            Assert.That(buffer.Count, Is.EqualTo(0)); // 만료 상태이상 제외 검증
        }

        [Test] // 동일 종류 상태이상 병합 검증
        public void CollectStatusEffects_MergesSameKindIntoStack() // 중첩 병합 테스트
        {
            BattleSkillRuntimeState.AddModifier("ENEMY_0", BattleRuntimeModifierKind.ResistanceReductionPercent, 0.2f, 4f, "SRC_A", 10f); // 첫 저항 감소 등록
            BattleSkillRuntimeState.AddModifier("ENEMY_0", BattleRuntimeModifierKind.ResistanceReductionPercent, 0.1f, 8f, "SRC_B", 10f); // 두 번째 저항 감소 등록
            BattleSkillRuntimeState.CollectStatusEffects("ENEMY_0", buffer, 11f); // 두 효과 활성 시점 상태이상 수집

            Assert.That(buffer.Count, Is.EqualTo(1)); // 동일 종류 병합 검증
            Assert.That(buffer[0].StackCount, Is.EqualTo(2)); // 중첩 수 검증
            Assert.That(buffer[0].RemainingSeconds, Is.EqualTo(7f).Within(0.0001f)); // 가장 늦게 끝나는 지속시간 검증
        }

        [Test] // 침묵 상태 조회 검증
        public void IsSilenced_TracksSilenceModifier() // 침묵 조회 테스트
        {
            Assert.That(BattleSkillRuntimeState.IsSilenced("ALLY_0", 10f), Is.False); // 침묵 미적용 상태 검증
            BattleSkillRuntimeState.AddModifier("ALLY_0", BattleRuntimeModifierKind.Silence, 1f, 3f, "TEST_SILENCE", 10f); // 침묵 3초 등록

            Assert.That(BattleSkillRuntimeState.IsSilenced("ALLY_0", 12f), Is.True); // 침묵 지속 중 상태 검증
            Assert.That(BattleSkillRuntimeState.IsSilenced("ALLY_0", 13.01f), Is.False); // 침묵 만료 후 상태 검증
        }

        [Test] // 둔화 공격 주기 반영 검증
        public void AttackSpeedReduction_SlowsBasicAttackInterval() // 둔화 주기 테스트
        {
            float normal = BattleBasicAttackTiming.GetInterval(2f); // 둔화 없는 공격 주기 계산
            float slowed = BattleBasicAttackTiming.GetInterval(2f, 0.5f); // 50퍼센트 둔화 공격 주기 계산

            Assert.That(normal, Is.EqualTo(0.5f).Within(0.0001f)); // 기존 계산 유지 검증
            Assert.That(slowed, Is.EqualTo(1f).Within(0.0001f)); // 둔화 반영 주기 2배 검증
            Assert.That(BattleSkillRuntimeState.GetAttackSpeedReduction("ALLY_0", 10f), Is.EqualTo(0f).Within(0.0001f)); // 둔화 미적용 감소율 0 검증
        }

        [Test] // 둔화 상한 적용 검증
        public void AttackSpeedReduction_IsClampedByUpperBound() // 둔화 상한 테스트
        {
            BattleSkillRuntimeState.AddModifier("ENEMY_0", BattleRuntimeModifierKind.AttackSpeedReductionPercent, 0.9f, 5f, "SLOW_A", 10f); // 90퍼센트 둔화 등록

            Assert.That(BattleSkillRuntimeState.GetAttackSpeedReduction("ENEMY_0", 11f), Is.EqualTo(0.70f).Within(0.0001f)); // 상한 70퍼센트 보정 검증
        }

        [Test] // 공격력 버프 배율 검증
        public void AttackModifier_IncreasesAttackMultiplier() // 공격력 버프 테스트
        {
            Assert.That(BattleSkillRuntimeState.GetAttackMultiplier("ALLY_0", 10f), Is.EqualTo(1f).Within(0.0001f)); // 버프 미적용 배율 1.0 검증
            BattleSkillRuntimeState.AddModifier("ALLY_0", BattleRuntimeModifierKind.AttackPercent, 0.3f, 5f, "BUFF_ATK", 10f); // 공격력 30퍼센트 증가 등록

            Assert.That(BattleSkillRuntimeState.GetAttackMultiplier("ALLY_0", 11f), Is.EqualTo(1.3f).Within(0.0001f)); // 버프 반영 배율 검증
        }

        [Test] // 정화가 디버프만 제거하는지 검증
        public void RemoveDebuffs_RemovesDebuffsButKeepsBuffs() // 정화 분류 테스트
        {
            BattleSkillRuntimeState.AddModifier("ALLY_0", BattleRuntimeModifierKind.Stun, 1f, 5f, "SRC_STUN", 10f); // 기절 디버프 등록 (출처 미등록)
            BattleSkillRuntimeState.AddModifier("ALLY_0", BattleRuntimeModifierKind.Silence, 1f, 5f, "SRC_SILENCE", 10f); // 침묵 디버프 등록 (출처 미등록)
            BattleSkillRuntimeState.AddModifier("ALLY_0", BattleRuntimeModifierKind.DefensePercent, 0.5f, 5f, "SRC_DEF", 10f); // 방어 증가 버프 등록

            int removed = BattleSkillRuntimeState.RemoveDebuffs("ALLY_0", int.MaxValue); // 전체 정화 실행
            BattleSkillRuntimeState.CollectStatusEffects("ALLY_0", buffer, 11f); // 정화 이후 상태이상 수집

            Assert.That(removed, Is.EqualTo(2)); // 디버프 2개 제거 검증
            Assert.That(buffer.Count, Is.EqualTo(1)); // 버프만 잔존 검증
            Assert.That(buffer[0].Id, Is.EqualTo(BattleStatusEffectId.DefenseUp)); // 잔존 항목이 버프인지 검증
        }
    }
}
