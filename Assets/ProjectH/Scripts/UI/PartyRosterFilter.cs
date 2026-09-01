using ProjectH.Data; // 캐릭터 포지션 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public enum PartyRoleFilter // 파티 캐릭터 역할 필터
    {
        All = 0, // 전체 역할
        Tank = 1, // 탱커 역할
        Dealer = 2, // 딜러 역할
        Healer = 3 // 힐러 역할
    }

    public static class PartyRosterFilter // 파티 캐릭터 필터 기능
    {
        public static bool Matches(PartyRoleFilter filter, BattlePosition position) // 역할 필터 일치 확인
        {
            switch (filter) // 필터 종류 분기
            {
                case PartyRoleFilter.All: // 전체 필터 처리
                    return true; // 전체 캐릭터 허용
                case PartyRoleFilter.Tank: // 탱커 필터 처리
                    return position == BattlePosition.Tank; // 탱커 일치 반환
                case PartyRoleFilter.Dealer: // 딜러 필터 처리
                    return position == BattlePosition.Dealer; // 딜러 일치 반환
                case PartyRoleFilter.Healer: // 힐러 필터 처리
                    return position == BattlePosition.Healer; // 힐러 일치 반환
                default: // 알 수 없는 필터 처리
                    return true; // 기본 전체 허용
            }
        }
    }
}
