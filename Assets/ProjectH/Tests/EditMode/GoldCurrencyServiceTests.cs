using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.SaveSystem; // Gold 및 저장 기능

namespace ProjectH.Tests.EditMode // EditMode 테스트 영역
{
    public sealed class GoldCurrencyServiceTests // Day40 Gold 거래 회귀 테스트
    {
        [Test] // Gold 차감 성공 테스트
        public void TrySpendGold_HasEnoughGold_SubtractsBalance() // 충분한 Gold 차감 검증
        {
            SaveData saveData = SaveData.CreateNewGame(System.Array.Empty<string>()); // 테스트 저장 데이터 생성
            GoldCurrencyService.AddGold(saveData, 500); // 테스트 Gold 지급

            bool result = GoldCurrencyService.TrySpendGold(saveData, 120, out string error); // Gold 차감 실행

            Assert.That(result, Is.True, error); // 차감 성공 확인
            Assert.That(GoldCurrencyService.GetGold(saveData), Is.EqualTo(380)); // 차감 후 잔액 확인
        }

        [Test] // Gold 부족 차단 테스트
        public void TrySpendGold_NotEnoughGold_KeepsBalance() // Gold 부족 시 잔액 보존 검증
        {
            SaveData saveData = SaveData.CreateNewGame(System.Array.Empty<string>()); // 테스트 저장 데이터 생성
            GoldCurrencyService.AddGold(saveData, 50); // 테스트 Gold 지급

            bool result = GoldCurrencyService.TrySpendGold(saveData, 100, out _); // 부족 Gold 차감 실행

            Assert.That(result, Is.False); // 차감 실패 확인
            Assert.That(GoldCurrencyService.GetGold(saveData), Is.EqualTo(50)); // 기존 잔액 유지 확인
        }
    }
}
