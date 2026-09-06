using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 궁극기 게이지 Runtime 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleUltimateGaugeRuntimeStateTests // 캐릭터별 궁극기 게이지 Runtime 테스트
    {
        [SetUp] // 테스트 준비 표시
        public void SetUp() // 궁극기 게이지 테스트 준비
        {
            BattleUltimateGaugeRuntimeState.ResetAll(); // 이전 테스트 궁극기 게이지 초기화
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 궁극기 게이지 테스트 정리
        {
            BattleUltimateGaugeRuntimeState.ResetAll(); // 테스트 종료 궁극기 게이지 초기화
        }

        [Test] // 테스트 표시
        public void AddGauge_StoresPerCharacterAndClampsAtMaximum() // 캐릭터별 저장과 최대치 보정 검증
        {
            BattleUltimateGaugeRuntimeState.AddGauge("CH_SERENA", 70); // 세레나 게이지 70 충전
            BattleUltimateGaugeRuntimeState.AddGauge("CH_SERENA", 50); // 세레나 게이지 최대 초과 충전
            BattleUltimateGaugeRuntimeState.AddGauge("CH_ELLEN", 20); // 엘렌 게이지 별도 충전

            Assert.That(BattleUltimateGaugeRuntimeState.GetGauge("CH_SERENA"), Is.EqualTo(100)); // 세레나 게이지 100 제한 검증
            Assert.That(BattleUltimateGaugeRuntimeState.GetGauge("CH_ELLEN"), Is.EqualTo(20)); // 엘렌 개별 게이지 검증
        }

        [Test] // 테스트 표시
        public void CanUseUltimate_RequiresFullGaugeAndLivingCharacter() // Ready와 생존 조건 검증
        {
            BattleUltimateGaugeRuntimeState.AddGauge("CH_LILIA", 100); // 릴리아 게이지 완충

            Assert.That(BattleUltimateGaugeRuntimeState.CanUseUltimate("CH_LILIA", true), Is.True); // 생존 완충 상태 사용 가능 검증
            Assert.That(BattleUltimateGaugeRuntimeState.CanUseUltimate("CH_LILIA", false), Is.False); // 사망 완충 상태 사용 불가 검증
            Assert.That(BattleUltimateGaugeRuntimeState.CanUseUltimate("CH_EVE", true), Is.False); // 미충전 캐릭터 사용 불가 검증
        }

        [Test] // 테스트 표시
        public void TryConsumeGauge_ConsumesOnlyWhenUltimateIsUsable() // 게이지 소비 조건 검증
        {
            BattleUltimateGaugeRuntimeState.AddGauge("CH_EVE", 100); // 이브 게이지 완충

            bool deadResult = BattleUltimateGaugeRuntimeState.TryConsumeGauge("CH_EVE", false); // 사망 상태 소비 시도
            int gaugeAfterDeadAttempt = BattleUltimateGaugeRuntimeState.GetGauge("CH_EVE"); // 사망 소비 시도 후 게이지 조회
            bool aliveResult = BattleUltimateGaugeRuntimeState.TryConsumeGauge("CH_EVE", true); // 생존 상태 소비 시도

            Assert.That(deadResult, Is.False); // 사망 상태 소비 실패 검증
            Assert.That(gaugeAfterDeadAttempt, Is.EqualTo(100)); // 사망 상태 게이지 보존 검증
            Assert.That(aliveResult, Is.True); // 생존 Ready 상태 소비 성공 검증
            Assert.That(BattleUltimateGaugeRuntimeState.GetGauge("CH_EVE"), Is.EqualTo(0)); // 소비 후 게이지 초기화 검증
        }

        [Test] // 테스트 표시
        public void ResetGaugeAndResetAll_ClearBattleRuntimeValues() // 개별 및 전체 초기화 검증
        {
            BattleUltimateGaugeRuntimeState.AddGauge("CH_SERENA", 30); // 세레나 게이지 충전
            BattleUltimateGaugeRuntimeState.AddGauge("CH_ELLEN", 40); // 엘렌 게이지 충전
            BattleUltimateGaugeRuntimeState.ResetGauge("CH_SERENA"); // 세레나 게이지 개별 초기화

            Assert.That(BattleUltimateGaugeRuntimeState.GetGauge("CH_SERENA"), Is.EqualTo(0)); // 세레나 개별 초기화 검증
            Assert.That(BattleUltimateGaugeRuntimeState.GetGauge("CH_ELLEN"), Is.EqualTo(40)); // 엘렌 게이지 유지 검증

            BattleUltimateGaugeRuntimeState.ResetAll(); // 전투 전체 게이지 초기화

            Assert.That(BattleUltimateGaugeRuntimeState.GetGauge("CH_ELLEN"), Is.EqualTo(0)); // 전체 초기화 검증
        }
    }
}
