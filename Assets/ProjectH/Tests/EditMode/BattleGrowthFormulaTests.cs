using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 성장 공식 기능

namespace ProjectH.Tests.EditMode // 프로젝트 EditMode 테스트 영역
{
    public sealed class BattleGrowthFormulaTests // 전투 성장 공식 테스트
    {
        [Test] // 레벨 범위 보정 테스트 지정
        public void NormalizeLevel_ClampsToSupportedRange() // 성장 적용 레벨 범위 보정 확인
        {
            Assert.AreEqual(1, BattleGrowthFormula.NormalizeLevel(-5)); // 최소 미만 레벨 보정 확인
            Assert.AreEqual(1, BattleGrowthFormula.NormalizeLevel(1)); // 최소 레벨 유지 확인
            Assert.AreEqual(20, BattleGrowthFormula.NormalizeLevel(20)); // 최대 레벨 유지 확인
            Assert.AreEqual(20, BattleGrowthFormula.NormalizeLevel(99)); // 최대 초과 레벨 보정 확인
        }

        [Test] // 성장 배율 테스트 지정
        public void GetLevelMultiplier_UsesLinearFivePercentGrowthAndCapsAtMaxLevel() // 레벨당 5퍼센트 성장 및 최대 레벨 상한 확인
        {
            Assert.That(BattleGrowthFormula.GetLevelMultiplier(1), Is.EqualTo(1.00f).Within(0.0001f)); // 1레벨 배율 확인
            Assert.That(BattleGrowthFormula.GetLevelMultiplier(5), Is.EqualTo(1.20f).Within(0.0001f)); // 5레벨 배율 확인
            Assert.That(BattleGrowthFormula.GetLevelMultiplier(20), Is.EqualTo(1.95f).Within(0.0001f)); // 20레벨 배율 확인
            Assert.That(BattleGrowthFormula.GetLevelMultiplier(99), Is.EqualTo(1.95f).Within(0.0001f)); // 최대 초과 배율 상한 확인
        }

        [Test] // 스탯 성장 테스트 지정
        public void ScaleStat_AppliesLevelGrowthAndRoundsToInteger() // 기본 스탯 성장 및 정수 반올림 확인
        {
            Assert.AreEqual(100, BattleGrowthFormula.ScaleStat(100, 1)); // 1레벨 기본값 확인
            Assert.AreEqual(120, BattleGrowthFormula.ScaleStat(100, 5)); // 5레벨 성장값 확인
            Assert.AreEqual(195, BattleGrowthFormula.ScaleStat(100, 20)); // 20레벨 성장값 확인
            Assert.AreEqual(195, BattleGrowthFormula.ScaleStat(100, 99)); // 최대 초과 성장 상한 확인
        }

        [Test] // 음수 기본값 테스트 지정
        public void ScaleStat_NegativeBaseValueBecomesZero() // 음수 기본 스탯 안전 보정 확인
        {
            Assert.AreEqual(0, BattleGrowthFormula.ScaleStat(-100, 10)); // 음수 기본값 0 보정 확인
        }
    }
}
