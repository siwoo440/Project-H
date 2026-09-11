namespace ProjectH.Data // 프로젝트 데이터 영역
{
    public sealed class CharacterProfile // 캐릭터 프로필 (Day60 신규 — 기획서 캐릭터 상세 시트 1~3·6번)
    {
        public string CharacterId { get; } // 캐릭터 ID
        public string Voice { get; } // 성우 (CV)
        public string Age { get; } // 나이
        public string Origin { get; } // 출신
        public string Height { get; } // 키
        public string Weight { get; } // 몸무게
        public string ThreeSize { get; } // 3사이즈
        public string Story { get; } // 캐릭터 스토리

        public CharacterProfile(string characterId, string voice, string age, string origin, string height, string weight, string threeSize, string story) // 프로필 생성
        {
            CharacterId = characterId; // ID 저장
            Voice = voice; // 성우 저장
            Age = age; // 나이 저장
            Origin = origin; // 출신 저장
            Height = height; // 키 저장
            Weight = weight; // 몸무게 저장
            ThreeSize = threeSize; // 3사이즈 저장
            Story = story; // 스토리 저장
        }
    }

    public static class CharacterProfileCatalog // 12인 프로필 목록 (Day60 신규 — 기획서 공란·확정 대기 항목은 '###'로 표시)
    {
        public const string Unknown = "###"; // 미정 표시 (목업 표기와 동일)

        private static readonly CharacterProfile[] Profiles = // 기획서 캐릭터 시트 기준 (키·체중 대부분 임시안)
        {
            new CharacterProfile("CH_SERENA", Unknown, "19세", "레티시아 왕국", "164cm", "52kg", "B87 / W58 / H86", "여신 리리아스의 계시로 주인공을 처음 맞이한 성녀. 레티시아 왕국과 리리아스 교단을 대표하며 파티의 생존을 책임지는 메인 힐러다. 온화하고 헌신적이지만, 모두를 구해야 한다는 부담을 안고 자신의 약함을 좀처럼 드러내지 않는다."), // 세레나
            new CharacterProfile("CH_ELLEN", Unknown, Unknown, "카르니안 제국(서부)", "172cm", "63kg", "B92 / W61 / H90", "카르니안 제국의 영웅 출신 기사. 과거 명령으로 민간인을 희생시킨 죄책감을 안고 살며, 감정 표현이 서툴다. 강철 같은 방어와 도발 능력으로 전면을 지킨다."), // 엘렌 (나이 확정 대기)
            new CharacterProfile("CH_LILIA", Unknown, "19세", "노아르 마법도시", "167cm", "51kg", "B83 / W56 / H84", "마법도시 노아르의 천재 마법사. 논리밖에 믿지 않지만, 주인공의 ‘해석 불가능한 힘’에 호기심과 집착을 보인다. 범위 마법과 디버프에 특화."), // 릴리아
            new CharacterProfile("CH_NATASHA", Unknown, "23세", "암흑도시 모르가스", "168cm", "54kg", "B86 / W57 / H85", "암흑도시 모르가스 출신 잠입자. 마왕군에게 가족을 잃고 복수를 위해 어둠의 세계로 들어갔다. 은신, 연속타, 출혈을 기반으로 적을 암살하는 암흑 딜러."), // 나타샤
            new CharacterProfile("CH_EVE", Unknown, "121세 (외모 19세)", "실바란 숲", Unknown, Unknown, "B79 / W54 / H82", "실바란 숲의 정령궁수. 인간과의 전쟁으로 고향을 잃어 인간을 불신하지만 주인공에게서 ‘평화의 기운’을 느낀다. 연속 사격과 치명타 기반의 지속 딜러."), // 이브
            new CharacterProfile("CH_CLAIRE", Unknown, Unknown, "기계도시 메리디안", Unknown, Unknown, "B88 / W59 / H89", "기계도시 메리디안 출신의 연금술사. 생명연금술로 ‘죽은 이를 살리는 법’을 찾고 있어 위험한 실험도 마다하지 않는다. 버프·디버프·회복을 모두 다루는 만능형."), // 클레어
            new CharacterProfile("CH_LUCIA", Unknown, "27세", "길드연합 용병 출신", Unknown, Unknown, "B84 / W58 / H87", "길드연합의 용병 출신. 감정에 흔들리지 않는 현실주의자이며, 이익 없는 싸움은 절대 하지 않는다. 관통 사격과 헤드샷을 기반으로 한 고정 크리 딜러."), // 루시아
            new CharacterProfile("CH_PYRA", Unknown, "24세", "카르니안 제국", Unknown, Unknown, "B90 / W60 / H89", "카르니안 제국의 창기사. 한때 엘렌의 부하였지만, 그녀의 이상주의를 비판하며 독자 행동 중. 폭발적 돌진기와 도트 데미지가 특기인 공격형 브레이커."), // 파이라
            new CharacterProfile("CH_TYRIA", Unknown, "28세", "제국 남부 수호대", Unknown, Unknown, "B91 / W62 / H92", "제국 남부 수호대 출신. 마왕군에게 마을이 멸망한 후 유일한 생존자가 되어 약자를 지키겠다는 맹세를 했다. 반격기와 보호막 생성에 특화된 탱커."), // 티리아
            new CharacterProfile("CH_MERCIA", Unknown, Unknown, "동방 사원", Unknown, Unknown, "B82 / W55 / H83", "동방 사원의 승려. 신의 부름을 받고 순례를 떠났다. 정령 소환과 근접 공격을 겸하며, 순간 폭딜과 속박 기술이 특징."), // 메르시아
            new CharacterProfile("CH_NOEL", Unknown, Unknown, "아스트라 길드연합", Unknown, Unknown, "B85 / W57 / H86", "길드연합의 자유로운 모험가. 유적 조사 중 동료를 잃은 뒤 혼자 떠돌고 있다. 함정 설치, 이동속도 디버프, 빙결, 약점 노출 등 CC 특화 캐릭터."), // 노엘
            new CharacterProfile("CH_SEPHIRA", Unknown, Unknown, "동방 사원", Unknown, Unknown, "B90 / W60 / H88", "신의 사도단 엘리트 순례자. “신의 명령”이라면 악행도 서슴지 않았으나 주인공을 만나며 가치관이 뒤흔들린다. 공격+회복+부활을 모두 갖춘 전투 성직자.") // 세피라
        };

        public static CharacterProfile Get(string characterId) // 프로필 조회 (없으면 전부 미정)
        {
            for (int index = 0; index < Profiles.Length; index++) // 목록 순회
            {
                if (Profiles[index].CharacterId == characterId) // ID 일치 확인
                {
                    return Profiles[index]; // 프로필 반환
                }
            }

            return new CharacterProfile(characterId, Unknown, Unknown, Unknown, Unknown, Unknown, Unknown, "프로필 준비 중"); // 기본 프로필 반환
        }
    }
}
