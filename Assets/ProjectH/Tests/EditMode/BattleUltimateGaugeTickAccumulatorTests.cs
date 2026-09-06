using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 궁극기 게이지 시간 누적 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleUltimateGaugeTickAccumulatorTests // 궁극기 게이지 초 단위 누적 테스트
    {
        [Test] // 테스트 표시
        public void Advance_ReturnsOneTickAfterOneSecond() // 1초 누적 시 1회 Tick 검증
        {
            BattleUltimateGaugeTickAccumulator accumulator = new BattleUltimateGaugeTickAccumulator(); // 시간 누적기 생성

            int firstTicks = accumulator.Advance(0.25f); // 0.25초 누적
            int secondTicks = accumulator.Advance(0.75f); // 추가 0.75초 누적

            Assert.That(firstTicks, Is.EqualTo(0)); // 1초 미만 Tick 없음 검증
            Assert.That(secondTicks, Is.EqualTo(1)); // 총 1초 도달 1회 Tick 검증
        }

        [Test] // 테스트 표시
        public void Advance_PreservesRemainderAcrossFrames() // 프레임 간 잔여 시간 보존 검증
        {
            BattleUltimateGaugeTickAccumulator accumulator = new BattleUltimateGaugeTickAccumulator(); // 시간 누적기 생성

            int firstTicks = accumulator.Advance(2.25f); // 2.25초 일괄 누적
            int secondTicks = accumulator.Advance(0.5f); // 추가 0.5초 누적
            int thirdTicks = accumulator.Advance(0.25f); // 추가 0.25초 누적

            Assert.That(firstTicks, Is.EqualTo(2)); // 2초 기준 2회 Tick 검증
            Assert.That(secondTicks, Is.EqualTo(0)); // 누적 0.75초 Tick 없음 검증
            Assert.That(thirdTicks, Is.EqualTo(1)); // 잔여 포함 1초 도달 Tick 검증
        }

        [Test] // 테스트 표시
        public void Advance_IgnoresNonPositiveDeltaTime() // 0 이하 시간 입력 무시 검증
        {
            BattleUltimateGaugeTickAccumulator accumulator = new BattleUltimateGaugeTickAccumulator(); // 시간 누적기 생성

            int zeroTicks = accumulator.Advance(0f); // 0초 입력
            int negativeTicks = accumulator.Advance(-1f); // 음수 시간 입력

            Assert.That(zeroTicks, Is.EqualTo(0)); // 0초 Tick 없음 검증
            Assert.That(negativeTicks, Is.EqualTo(0)); // 음수 시간 Tick 없음 검증
        }
    }
}
