using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 흐트러짐 게이지 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleDisarrayTests // Day53 흐트러짐 게이지 회귀 테스트
    {
        private const string TargetId = "ENEMY_0"; // 테스트 대상 Runtime ID
        private const int TestMaxHp = 800; // 테스트 대상 최대 체력 (최대치 100 산출)

        [SetUp] // 각 테스트 시작 전 처리
        public void SetUp() // 흐트러짐 상태 초기화
        {
            BattleDisarrayRuntimeState.ResetAll(); // 이전 테스트 잔여 상태 제거
            BattleDisarrayRuntimeState.Register(TargetId, TestMaxHp); // 테스트 대상 흐트러짐 게이지 등록
        }

        [TearDown] // 각 테스트 종료 후 처리
        public void TearDown() // 흐트러짐 상태 정리
        {
            BattleDisarrayRuntimeState.ResetAll(); // 다음 테스트 영향 방지
        }

        [Test] // 최대 체력 비례 최대치 산출 검증
        public void CalculateMaxGauge_ScalesWithMaxHpAndRespectsFloor() // 최대치 산출 테스트
        {
            Assert.That(BattleDisarrayRuntimeState.CalculateMaxGauge(800), Is.EqualTo(100)); // 최대 체력 800 기준 최대치 100 검증
            Assert.That(BattleDisarrayRuntimeState.CalculateMaxGauge(16000), Is.EqualTo(2000)); // 보스급 최대 체력 비례 증가 검증
            Assert.That(BattleDisarrayRuntimeState.CalculateMaxGauge(100), Is.EqualTo(BattleDisarrayRuntimeState.MinMaxGauge)); // 낮은 최대 체력 하한 적용 검증
            Assert.That(BattleDisarrayRuntimeState.GetMaxGauge(TargetId), Is.EqualTo(100)); // 등록 대상 최대치 검증
        }

        [Test] // 미등록 대상 안전 기본값 검증
        public void UnregisteredTarget_ReturnsSafeDefaults() // 미등록 대상 테스트
        {
            Assert.That(BattleDisarrayRuntimeState.IsRegistered("ALLY_0"), Is.False); // 아군 미등록 검증 (흐트러짐은 적 전용)
            Assert.That(BattleDisarrayRuntimeState.GetGaugeRatio("ALLY_0", 0f), Is.EqualTo(0f).Within(0.0001f)); // 미등록 대상 게이지 0 검증
            Assert.That(BattleDisarrayRuntimeState.IsDisarrayed("ALLY_0", 0f), Is.False); // 미등록 대상 흐트러짐 아님 검증
            Assert.That(BattleDisarrayRuntimeState.AddDisarray("ALLY_0", 999, 0f), Is.False); // 미등록 대상 누적 무시 검증
            Assert.That(BattleDisarrayRuntimeState.GetDamageMultiplier("ALLY_0", 0f), Is.EqualTo(1f).Within(0.0001f)); // 미등록 대상 추가 피해 없음 검증
        }

        [Test] // 속성 상성별 누적량 검증 (Day52 연계)
        public void GetHitAmount_ScalesWithElementAffinity() // 상성 누적량 테스트
        {
            Assert.That(BattleDisarrayRuntimeState.GetHitAmount(BattleElementAffinity.Neutral), Is.EqualTo(10)); // 상성 없음 기본 누적량 검증
            Assert.That(BattleDisarrayRuntimeState.GetHitAmount(BattleElementAffinity.Weak), Is.EqualTo(20)); // 약점 적중 2배 누적량 검증
            Assert.That(BattleDisarrayRuntimeState.GetHitAmount(BattleElementAffinity.Resist), Is.EqualTo(5)); // 저항 적중 절반 누적량 검증
        }

        [Test] // 약점 적중이 흐트러짐을 절반 횟수에 도달시키는지 검증
        public void WeakHits_ReachDisarrayInHalfTheHits() // 약점 연계 핵심 테스트
        {
            for (int index = 0; index < 4; index++) // 약점 적중 4회 반복
            {
                Assert.That(BattleDisarrayRuntimeState.AddDisarray(TargetId, BattleDisarrayRuntimeState.GetHitAmount(BattleElementAffinity.Weak), 0f), Is.False); // 4회까지 흐트러짐 미발생 검증
            }

            Assert.That(BattleDisarrayRuntimeState.AddDisarray(TargetId, BattleDisarrayRuntimeState.GetHitAmount(BattleElementAffinity.Weak), 0f), Is.True); // 약점 5회에 흐트러짐 발생 검증 (상성 없음은 10회 필요)
        }

        [Test] // 최대치 도달 시 흐트러짐 발생 검증
        public void AddDisarray_ReachingMax_TriggersDisarray() // 흐트러짐 발생 테스트
        {
            Assert.That(BattleDisarrayRuntimeState.AddDisarray(TargetId, 90, 10f), Is.False); // 최대치 미달 흐트러짐 미발생 검증
            Assert.That(BattleDisarrayRuntimeState.IsDisarrayed(TargetId, 10f), Is.False); // 흐트러짐 상태 아님 검증
            Assert.That(BattleDisarrayRuntimeState.AddDisarray(TargetId, 10, 10f), Is.True); // 최대치 도달 흐트러짐 발생 검증
            Assert.That(BattleDisarrayRuntimeState.IsDisarrayed(TargetId, 10f), Is.True); // 흐트러짐 상태 검증
            Assert.That(BattleDisarrayRuntimeState.GetGaugeRatio(TargetId, 10f), Is.EqualTo(1f).Within(0.0001f)); // 흐트러짐 중 게이지 가득 참 검증
            Assert.That(BattleDisarrayRuntimeState.GetDisarrayCount(TargetId), Is.EqualTo(1)); // 흐트러짐 발생 횟수 검증
        }

        [Test] // 흐트러짐 중 추가 누적 무시 검증
        public void AddDisarray_WhileDisarrayed_IsIgnored() // 무한 연장 방지 테스트
        {
            BattleDisarrayRuntimeState.AddDisarray(TargetId, 100, 10f); // 흐트러짐 발생

            Assert.That(BattleDisarrayRuntimeState.AddDisarray(TargetId, 100, 11f), Is.False); // 흐트러짐 중 추가 누적 무시 검증
            Assert.That(BattleDisarrayRuntimeState.GetDisarrayCount(TargetId), Is.EqualTo(1)); // 흐트러짐 횟수 증가 없음 검증
        }

        [Test] // 흐트러짐 중 추가 피해 배율 검증
        public void GetDamageMultiplier_IncreasesWhileDisarrayed() // 추가 피해 Window 테스트
        {
            Assert.That(BattleDisarrayRuntimeState.GetDamageMultiplier(TargetId, 10f), Is.EqualTo(1f).Within(0.0001f)); // 평상시 배율 1.0 검증
            BattleDisarrayRuntimeState.AddDisarray(TargetId, 100, 10f); // 흐트러짐 발생

            Assert.That(BattleDisarrayRuntimeState.GetDamageMultiplier(TargetId, 11f), Is.EqualTo(1.6f).Within(0.0001f)); // 흐트러짐 중 추가 피해 배율 검증
            Assert.That(BattleDisarrayRuntimeState.GetDamageMultiplier(TargetId, 20f), Is.EqualTo(1f).Within(0.0001f)); // 흐트러짐 해제 후 배율 복귀 검증
        }

        [Test] // 유지 시간 경과 후 해제 및 게이지 초기화 검증
        public void Disarray_Recovers_AfterDuration() // 흐트러짐 해제 테스트
        {
            BattleDisarrayRuntimeState.AddDisarray(TargetId, 100, 10f); // 10초 시점 흐트러짐 발생

            Assert.That(BattleDisarrayRuntimeState.IsDisarrayed(TargetId, 13.9f), Is.True); // 유지 시간 내 흐트러짐 지속 검증
            Assert.That(BattleDisarrayRuntimeState.IsDisarrayed(TargetId, 14.1f), Is.False); // 4초 경과 후 해제 검증
            Assert.That(BattleDisarrayRuntimeState.GetCurrentGauge(TargetId), Is.EqualTo(0)); // 해제 후 게이지 초기화 검증
            Assert.That(BattleDisarrayRuntimeState.GetGaugeRatio(TargetId, 14.1f), Is.EqualTo(0f).Within(0.0001f)); // 해제 후 표시 비율 0 검증
        }

        [Test] // 흐트러짐마다 최대치 증가 검증
        public void Disarray_IncreasesMaxGaugeEachTime() // 최대치 증가 테스트
        {
            Assert.That(BattleDisarrayRuntimeState.GetMaxGauge(TargetId), Is.EqualTo(100)); // 초기 최대치 검증
            BattleDisarrayRuntimeState.AddDisarray(TargetId, 100, 10f); // 첫 흐트러짐 발생
            BattleDisarrayRuntimeState.IsDisarrayed(TargetId, 14.1f); // 해제 시각 경과 반영

            Assert.That(BattleDisarrayRuntimeState.GetMaxGauge(TargetId), Is.EqualTo(125)); // 1.25배 증가 최대치 검증
            BattleDisarrayRuntimeState.AddDisarray(TargetId, 125, 15f); // 두 번째 흐트러짐 발생
            BattleDisarrayRuntimeState.IsDisarrayed(TargetId, 19.1f); // 해제 시각 경과 반영

            Assert.That(BattleDisarrayRuntimeState.GetMaxGauge(TargetId), Is.EqualTo(156)); // 누적 증가 최대치 검증
            Assert.That(BattleDisarrayRuntimeState.GetDisarrayCount(TargetId), Is.EqualTo(2)); // 흐트러짐 발생 횟수 검증
        }

        [Test] // 잔여 시간 조회 검증
        public void GetRemainingSeconds_TracksDisarrayWindow() // 잔여 시간 테스트
        {
            Assert.That(BattleDisarrayRuntimeState.GetRemainingSeconds(TargetId, 10f), Is.EqualTo(0f).Within(0.0001f)); // 평상시 잔여 시간 0 검증
            BattleDisarrayRuntimeState.AddDisarray(TargetId, 100, 10f); // 흐트러짐 발생

            Assert.That(BattleDisarrayRuntimeState.GetRemainingSeconds(TargetId, 10f), Is.EqualTo(4f).Within(0.0001f)); // 발생 직후 잔여 시간 검증
            Assert.That(BattleDisarrayRuntimeState.GetRemainingSeconds(TargetId, 12.5f), Is.EqualTo(1.5f).Within(0.0001f)); // 경과 후 잔여 시간 검증
        }

        [Test] // 게이지 누적 비율 표시 검증
        public void GetGaugeRatio_ReflectsAccumulation() // 게이지 비율 테스트
        {
            BattleDisarrayRuntimeState.AddDisarray(TargetId, 25, 10f); // 25 누적

            Assert.That(BattleDisarrayRuntimeState.GetGaugeRatio(TargetId, 10f), Is.EqualTo(0.25f).Within(0.0001f)); // 25퍼센트 비율 검증
            BattleDisarrayRuntimeState.AddDisarray(TargetId, 35, 10f); // 35 추가 누적

            Assert.That(BattleDisarrayRuntimeState.GetGaugeRatio(TargetId, 10f), Is.EqualTo(0.60f).Within(0.0001f)); // 60퍼센트 비율 검증
        }

        [Test] // 재등록 시 상태 초기화 검증
        public void Register_ResetsExistingState() // 재등록 초기화 테스트
        {
            BattleDisarrayRuntimeState.AddDisarray(TargetId, 100, 10f); // 흐트러짐 발생
            BattleDisarrayRuntimeState.Register(TargetId, TestMaxHp); // 같은 대상 재등록 (다음 전투 시작 상황)

            Assert.That(BattleDisarrayRuntimeState.IsDisarrayed(TargetId, 10f), Is.False); // 흐트러짐 상태 초기화 검증
            Assert.That(BattleDisarrayRuntimeState.GetCurrentGauge(TargetId), Is.EqualTo(0)); // 누적 수치 초기화 검증
            Assert.That(BattleDisarrayRuntimeState.GetMaxGauge(TargetId), Is.EqualTo(100)); // 최대치 초기화 검증
            Assert.That(BattleDisarrayRuntimeState.GetDisarrayCount(TargetId), Is.EqualTo(0)); // 발생 횟수 초기화 검증
        }
    }
}
