using System; // 난수 기능
using System.Collections.Generic; // 읽기 전용 목록 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class LoadingTipCatalog // 로딩창 Tip 문구 목록 (Day55 신규)
    {
        private static readonly string[] Tips = // 게임 시스템 안내 문구 목록
        {
            "약점 속성으로 공격하면 흐트러짐 게이지가 2배로 쌓입니다.", // Day52·53 속성 연계
            "불은 풀에, 풀은 물에, 물은 불에 강합니다.", // Day52 삼각 상성
            "빛은 어둠에 강하고, 무속성 적은 빛과 어둠에 약합니다.", // Day52 빛·어둠 상성
            "흐트러진 적은 4초 동안 움직이지 못하고 받는 피해가 1.6배가 됩니다.", // Day53 흐트러짐
            "보스가 기술을 준비할 때 흐트러뜨리면 그 기술을 저지할 수 있습니다.", // Day54 보스 저지
            "보스는 체력 60%와 25%에서 페이즈가 바뀌며 더 강해집니다.", // Day54 보스 페이즈
            "궁극기 리듬에서 PERFECT를 이어가면 위력이 최대 1.5배가 됩니다.", // Day50 리듬 배율
            "캐릭터 초상화가 금색으로 빛나면 눌러서 궁극기를 쓸 수 있습니다.", // Day48 초상화 궁극기
            "숫자키 1~4로 캐릭터를 선택하고, 우클릭으로 이동시킬 수 있습니다.", // 수동 이동·숫자키 배지
            "세레나의 궁극기는 아군의 해로운 상태이상을 모두 정화합니다.", // Day51 정화
            "탐험 중 휴식 노드를 거치면 다음 전투를 유리하게 시작할 수 있습니다.", // Day55 휴식
            "함정과 휴식의 효과는 바로 다음 전투 한 번에만 적용됩니다.", // Day55 다음 전투 효과
            "던전 클리어는 탐험 마지막의 보스를 쓰러뜨려야 기록됩니다.", // Day55 클리어 규칙
            "하루가 지나면 활력이 모두 회복됩니다." // Day42 활력
        };

        private static int lastIndex = -1; // 직전 표시 Tip 인덱스
        private static readonly Random random = new Random(); // Tip 선택 난수 생성기

        public static IReadOnlyList<string> All => Tips; // 전체 Tip 목록 반환

        public static string PickNext() // 직전과 다른 무작위 Tip 선택
        {
            return PickNext(random); // 공용 난수 생성기로 선택
        }

        public static string PickNext(Random source) // 지정 난수 생성기 기반 Tip 선택 (테스트용)
        {
            if (Tips.Length == 1) // 단일 Tip 확인
            {
                lastIndex = 0; // 인덱스 기록
                return Tips[0]; // 유일 Tip 반환
            }

            int index = lastIndex < 0 ? source.Next(Tips.Length) : source.Next(Tips.Length - 1); // 첫 표시는 전체, 이후는 직전 Tip 제외 범위에서 추첨

            if (lastIndex >= 0 && index >= lastIndex) // 직전 인덱스 이후 구간 확인
            {
                index++; // 직전 Tip 건너뛰기 (연속 중복 방지)
            }

            lastIndex = index; // 직전 인덱스 갱신
            return Tips[index]; // 선택 Tip 반환
        }
    }
}
