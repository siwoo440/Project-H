using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Data; // 캐릭터 포지션 기능
using ProjectH.UI; // 파티 필터 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class PartyRosterFilterTests // 파티 역할 필터 테스트
    {
        [TestCase(PartyRoleFilter.All, BattlePosition.Tank, true)] // 전체 탱커 사례
        [TestCase(PartyRoleFilter.Tank, BattlePosition.Tank, true)] // 탱커 일치 사례
        [TestCase(PartyRoleFilter.Tank, BattlePosition.Dealer, false)] // 탱커 불일치 사례
        [TestCase(PartyRoleFilter.Dealer, BattlePosition.Dealer, true)] // 딜러 일치 사례
        [TestCase(PartyRoleFilter.Healer, BattlePosition.Healer, true)] // 힐러 일치 사례
        public void Matches_ReturnsExpectedResult(PartyRoleFilter filter, BattlePosition position, bool expected) // 역할 필터 결과 검증
        {
            bool result = PartyRosterFilter.Matches(filter, position); // 역할 필터 적용
            Assert.That(result, Is.EqualTo(expected)); // 역할 필터 결과 검증
        }
    }
}
