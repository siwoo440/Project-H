using System.Collections.Generic; // 목록 자료형
using ProjectH.SaveSystem; // 스토리 플래그 기능

namespace ProjectH.Diary // 프로젝트 일기장 영역
{
    public enum GlossaryCategory // 세계관 단어 분류 (표시 순서)
    {
        World = 0, // 세계·신화
        Region = 1, // 지역
        Faction = 2, // 세력
        Concept = 3 // 개념
    }

    public sealed class GlossaryEntry // 세계관 단어 하나
    {
        public string Term { get; } // 단어
        public GlossaryCategory Category { get; } // 분류
        public string Description { get; } // 설명
        public string UnlockFlag { get; } // 해금 스토리 플래그 (비우면 처음부터 열림)

        public GlossaryEntry(string term, GlossaryCategory category, string description, string unlockFlag = "") // 단어 생성
        {
            Term = term; // 단어 저장
            Category = category; // 분류 저장
            Description = description; // 설명 저장
            UnlockFlag = unlockFlag ?? string.Empty; // 해금 플래그 저장
        }

        public bool IsUnlocked(SaveData saveData) => string.IsNullOrEmpty(UnlockFlag) || (saveData != null && saveData.HasStoryFlag(UnlockFlag)); // 열람 가능 여부
    }

    public static class WorldGlossaryCatalog // 세계관 단어장 (Day63 신규 — 기획서 2장 세계관 요약, 설명은 이 파일에서 수정)
    {
        public const string SealTruthFlag = "LORE_SEAL_TRUTH"; // 봉인의 진실 해금 플래그 (69일차 챕터 5에서 설정 예정)

        private static readonly GlossaryEntry[] Entries = // 단어 목록 (분류 → 등장 순)
        {
            new GlossaryEntry("아르데라 대륙", GlossaryCategory.World, "이야기의 무대가 되는 판타지 대륙. 중앙의 인간 왕국, 서부의 마법도시, 동부의 엘프 숲, 북부의 군사 제국, 남부의 고대 사막 유적, 외곽의 금지된 땅으로 나뉜다."), // 대륙
            new GlossaryEntry("여신 리리아스", GlossaryCategory.World, "혼돈의 마나를 정리해 하늘과 땅을 나누고 대륙을 만든 여신. 오래전 마왕을 봉인하느라 힘을 대부분 잃어, 지금은 다른 세계에서 주인공을 불러내 뜻을 맡겼다."), // 여신
            new GlossaryEntry("마왕 네메시스", GlossaryCategory.World, "생명이 품은 두려움·증오·질투·탐욕이 쌓여 태어난 어둠의 의지. 여신이 만든 질서를 부정하며, 침식으로 세계를 자신의 영역으로 바꾸려 한다."), // 마왕
            new GlossaryEntry("제1차 마왕전쟁", GlossaryCategory.World, "네메시스가 마족과 침식된 생명체를 이끌고 대륙 전역을 공격한 옛 전쟁. 여신이 인간·엘프·수인·정령과 함께 맞서 마왕을 대륙 깊은 곳에 봉인하며 끝났다."), // 전쟁
            new GlossaryEntry("봉인의 진실", GlossaryCategory.World, "아스타르 사막의 고대 장치와 여신이 숨겨 온 과거. 마왕의 봉인이 왜 약해졌는지에 대한 답이 여기에 있다.", SealTruthFlag), // 스토리 진행 후 해금
            new GlossaryEntry("레티시아 왕국", GlossaryCategory.Region, "대륙 중앙의 인간 국가. 리리아스 신앙과 기사도로 발전했으며, 주인공이 처음 소환된 성역이 있다. 최근 국경에서 마물이 늘고 성역 근처에서도 침식이 발견되고 있다."), // 레티시아
            new GlossaryEntry("노아르 마법도시", GlossaryCategory.Region, "대륙 서부, 마법사 의회가 다스리는 독립 도시. 혈통보다 지식이 중요하며 밤이면 마법등이 도시를 푸르게 밝힌다. 지하에는 금지된 실험의 흔적이 숨어 있다."), // 노아르
            new GlossaryEntry("실바란 숲", GlossaryCategory.Region, "대륙 동부의 거대한 원시림이자 마나 흐름이 모이는 신성한 장소. 엘프와 정령이 살지만, 침식이 번지며 온화하던 정령들까지 사나워지고 있다."), // 실바란
            new GlossaryEntry("카르니안 제국", GlossaryCategory.Region, "대륙 북부의 군사 국가. 혹독한 기후 속에서 강한 군대와 계급 체계로 성장했다. 마왕과 싸운다는 명분으로 주변까지 통제하려 한다."), // 카르니안
            new GlossaryEntry("아스타르 사막", GlossaryCategory.Region, "대륙 남부, 멸망한 고대 문명의 신전과 지하 도시가 잠든 사막. 마왕을 봉인하던 고대 장치의 잔해가 남아 있다."), // 아스타르
            new GlossaryEntry("검은 균열지대", GlossaryCategory.Region, "대륙 외곽의 금지된 땅. 마왕전쟁 때 네메시스의 힘이 가장 크게 폭주한 곳으로, 하늘은 붉고 검으며 땅에서는 검은 안개가 피어오른다."), // 균열지대
            new GlossaryEntry("리리아스 교단", GlossaryCategory.Faction, "여신을 섬기는 종교 조직. 사제·성녀·성기사가 사람들을 치유하고 지키며, 침식을 정화하는 신성력을 다룬다. 다만 일부 고위 성직자는 권위를 더 중시한다."), // 교단
            new GlossaryEntry("레티시아 왕실", GlossaryCategory.Faction, "왕국을 다스리는 정치 세력. 교단과 협력하지만 늘 뜻이 같지는 않다. 처음에는 주인공을 감시하고 시험하지만, 활약에 따라 협력자가 된다."), // 왕실
            new GlossaryEntry("노아르 마법사 의회", GlossaryCategory.Faction, "마법도시를 다스리는 최고 기관. 침식을 저주가 아닌 분석 가능한 에너지로 보고 연구하지만, 그 힘을 이용하려는 마법사 때문에 사고가 끊이지 않는다."), // 의회
            new GlossaryEntry("실바란 수호자", GlossaryCategory.Faction, "숲을 지키는 엘프와 정령 계약자들. 자연의 균형을 무엇보다 중시해 인간과 제국을 경계하지만, 침식 앞에서 협력을 선택해야 하는 처지다."), // 수호자
            new GlossaryEntry("카르니안 제국군", GlossaryCategory.Faction, "대륙에서 가장 체계적인 군사 조직. 승리를 위해 희생을 감수해야 한다고 믿으며, 작은 마을을 전략 자원으로 여기기도 한다."), // 제국군
            new GlossaryEntry("마왕군", GlossaryCategory.Faction, "네메시스의 부활을 위해 움직이는 세력. 마물뿐 아니라 침식된 인간, 타락한 마법사, 마족, 고대 병기가 섞여 있으며 배신과 유혹으로 지역을 안에서부터 무너뜨린다."), // 마왕군
            new GlossaryEntry("침식", GlossaryCategory.Concept, "마왕의 힘이 세상에 번지는 현상. 땅과 생명, 마나와 기억, 감정까지 오염시키며 생명체를 마물로 바꾸고 지역을 던전으로 만든다. 침식도가 높을수록 적이 강해진다."), // 침식
            new GlossaryEntry("마물", GlossaryCategory.Concept, "침식의 영향을 받은 생명체, 또는 마왕의 힘으로 만들어진 존재. 평범한 늑대나 식물도 침식되면 마물이 된다."), // 마물
            new GlossaryEntry("던전", GlossaryCategory.Concept, "침식으로 변형된 위험 지역. 마물과 함정, 보상과 보스가 기다리는 주인공 일행의 주요 탐험 장소다."), // 던전
            new GlossaryEntry("성역", GlossaryCategory.Concept, "여신의 힘이 강하게 남은 장소. 침식을 약하게 만들며, 회복하거나 중요한 계시를 받는 곳이 된다."), // 성역
            new GlossaryEntry("신성석", GlossaryCategory.Concept, "여신의 힘이 담긴 결정. 성역을 유지하고 침식을 정화하며, 특별한 장비와 의식을 강화한다."), // 신성석
            new GlossaryEntry("결속", GlossaryCategory.Concept, "주인공과 동료 사이의 신뢰와 유대. 여신이 주인공에게 준 힘으로, 결속이 깊어질수록 동료의 잠재력이 깨어나 전투와 이야기가 함께 성장한다."), // 결속
            new GlossaryEntry("룬", GlossaryCategory.Concept, "고대 문명과 마법도시의 연구로 만들어진 마법 각인. 캐릭터에 새겨 싸우는 방식을 바꾼다."), // 룬
            new GlossaryEntry("활력", GlossaryCategory.Concept, "탐험과 전투를 버틸 수 있는 여유. 던전에 들어갈 때 쓰고, 날이 바뀌거나 온천에서 쉬면 돌아온다."), // 활력
            new GlossaryEntry("날짜", GlossaryCategory.Concept, "전체 진행을 재는 시간 단위. 정해진 날 안에 마왕의 부활을 막아야 하며, 날짜에 따라 이벤트·던전·침식도가 바뀐다.") // 날짜
        };

        public static IReadOnlyList<GlossaryEntry> All => Entries; // 전체 단어

        public static string GetCategoryLabel(GlossaryCategory category) // 분류 이름
        {
            switch (category) // 분류 분기
            {
                case GlossaryCategory.World: return "세계 · 신화"; // 세계
                case GlossaryCategory.Region: return "지역"; // 지역
                case GlossaryCategory.Faction: return "세력"; // 세력
                default: return "개념"; // 개념
            }
        }
    }
}
