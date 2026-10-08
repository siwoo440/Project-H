using System; // 문자열 비교 기능
using System.Collections.Generic; // 목록 · 사전 자료형
using ProjectH.Battle; // 상태이상 · 속성 기능
using ProjectH.SaveSystem; // 룬 기능
using UnityEngine; // 텍스처 · 스프라이트 기능
using UnityEngine.UI; // Image 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public readonly struct UiSkinPart // UI 스킨 조각 하나의 규격 (그림 요청문과 같은 값 — 여기를 바꾸면 요청문도 바꿔야 한다)
    {
        public readonly string Key; // 파일 이름 (확장자 없음)
        public readonly int Width; // 그림 가로
        public readonly int Height; // 그림 세로
        public readonly int Border; // 늘어나지 않는 테두리 두께 (0이면 통째로 쓴다)
        public readonly string Usage; // 쓰는 곳

        public UiSkinPart(string key, int width, int height, int border, string usage) // 규격 생성
        {
            Key = key; // 이름 저장
            Width = width; // 가로 저장
            Height = height; // 세로 저장
            Border = border; // 테두리 저장
            Usage = usage; // 쓰는 곳 저장
        }
    }

    public static class UiSkin // UI 스킨 (Day81 신규 — Resources/UI/Skin/{이름}.png를 9조각으로 늘여 쓴다. 파일이 없으면 null → 지금의 색 상자 그대로)
    {
        public const string Folder = "UI/Skin/"; // 스킨 Resources 경로
        public const string PanelLight = "panel_light"; // 밝은 창
        public const string PanelDark = "panel_dark"; // 어두운 창
        public const string PanelInset = "panel_inset"; // 안쪽 칸
        public const string Card = "card"; // 카드
        public const string Slot = "slot"; // 아이템 · 장비 칸
        public const string BarTop = "bar_top"; // 상단 바
        public const string BarBottom = "bar_bottom"; // 하단 바
        public const string DialogueWindow = "dialogue_window"; // 대화창
        public const string NamePlate = "name_plate"; // 이름표
        public const string ButtonPrimary = "button_primary"; // 주요 버튼
        public const string ButtonSecondary = "button_secondary"; // 보조 버튼
        public const string ButtonNav = "button_nav"; // 하단 내비 버튼
        public const string ButtonTabOn = "button_tab_on"; // 선택된 탭
        public const string ButtonTabOff = "button_tab_off"; // 선택되지 않은 탭
        public const string ButtonRound = "button_round"; // 원형 아이콘 버튼
        public const string ButtonChoice = "button_choice"; // 대화 선택지
        public const string GaugeBack = "gauge_back"; // 게이지 바탕
        public const string GaugeFill = "gauge_fill"; // 게이지 채움 (흰색 — 코드가 색을 입힌다)

        public static readonly IReadOnlyList<UiSkinPart> Parts = new[] // 스킨 조각 18종
        {
            new UiSkinPart(PanelLight, 256, 256, 64, "밝은 화면의 큰 창 (가방 · 캐릭터 · 일기장 · 설정)"), // 밝은 창
            new UiSkinPart(PanelDark, 256, 256, 64, "어두운 화면의 큰 창 (모험 지도 · 전투 · 로그 · 팝업)"), // 어두운 창
            new UiSkinPart(PanelInset, 128, 128, 32, "창 안의 목록 바탕 (스크롤 영역)"), // 안쪽 칸
            new UiSkinPart(Card, 192, 192, 48, "카드 (상품 · 캐릭터 · 던전 · 결과)"), // 카드
            new UiSkinPart(Slot, 128, 128, 32, "아이템 · 장비 · 룬 칸"), // 칸
            new UiSkinPart(BarTop, 256, 128, 48, "화면 위쪽 바 (제목 · 재화)"), // 상단 바
            new UiSkinPart(BarBottom, 256, 128, 48, "로비 아래쪽 내비 바"), // 하단 바
            new UiSkinPart(DialogueWindow, 512, 256, 64, "대화창"), // 대화창
            new UiSkinPart(NamePlate, 256, 96, 40, "대화 이름표"), // 이름표
            new UiSkinPart(ButtonPrimary, 256, 96, 40, "주요 버튼 (탐험 시작 · 확인 · 구매)"), // 주요 버튼
            new UiSkinPart(ButtonSecondary, 256, 96, 40, "보조 버튼 (뒤로 · 닫기 · 취소)"), // 보조 버튼
            new UiSkinPart(ButtonNav, 192, 128, 40, "로비 하단 내비 버튼"), // 내비 버튼
            new UiSkinPart(ButtonTabOn, 192, 80, 32, "선택된 탭"), // 탭 켬
            new UiSkinPart(ButtonTabOff, 192, 80, 32, "선택되지 않은 탭"), // 탭 끔
            new UiSkinPart(ButtonRound, 128, 128, 0, "원형 아이콘 버튼 (대화 메뉴)"), // 원형 버튼
            new UiSkinPart(ButtonChoice, 512, 96, 40, "대화 선택지"), // 선택지
            new UiSkinPart(GaugeBack, 256, 48, 20, "게이지 바탕 (체력 · 궁극기 · 흐트러짐)"), // 게이지 바탕
            new UiSkinPart(GaugeFill, 256, 48, 20, "게이지 채움 (흰색 — 색은 코드가 입힌다)") // 게이지 채움
        };

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>(); // 9조각 스프라이트 캐시 (모든 화면이 함께 쓰는 작은 그림이라 씬이 바뀌어도 둔다)

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 플레이 시작 시 초기화 (도메인 리로드를 끈 Play Mode 대응)
        private static void ResetCache() // 캐시 비우기
        {
            Cache.Clear(); // 이전 플레이의 캐시 제거
        }

        public static bool TryGetPart(string key, out UiSkinPart part) // 규격 조회 (없으면 false)
        {
            foreach (UiSkinPart candidate in Parts) // 조각 순회
            {
                if (!string.Equals(candidate.Key, key, StringComparison.Ordinal)) continue; // 다른 조각
                part = candidate; // 규격
                return true; // 찾음
            }

            part = default; // 없음
            return false; // 못 찾음
        }

        public static Sprite Get(string key) // 9조각 스프라이트 (규격에 없거나 파일이 없으면 null)
        {
            if (!TryGetPart(key, out UiSkinPart part)) return null; // 규격에 없는 이름
            if (Cache.TryGetValue(key, out Sprite cached) && cached != null) return cached; // 캐시
            Texture2D texture = Resources.Load<Texture2D>(Folder + key); // 스킨 그림
            if (texture == null) return null; // 그림 없음
            float border = part.Border * (texture.width / (float)part.Width); // 받은 그림 크기가 규격과 달라도 테두리 비율을 지킨다
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect, new Vector4(border, border, border, border)); // 9조각 스프라이트
            sprite.name = key; // 이름
            Cache[key] = sprite; // 캐시 저장
            return sprite; // 반환
        }

        public static bool Apply(Image image, string key, float cornerScale = 1f) // 이미지에 스킨 입히기 (파일이 없으면 false — 색 상자를 그대로 둔다)
        {
            Sprite sprite = image == null ? null : Get(key); // 스킨
            if (sprite == null) return false; // 대상 · 그림 없음
            image.sprite = sprite; // 그림 적용
            image.type = sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple; // 테두리가 있으면 9조각
            image.pixelsPerUnitMultiplier = Mathf.Max(0.1f, cornerScale); // 값이 클수록 모서리가 작게 그려진다 (작은 칸용)
            return true; // 적용함
        }
    }

    public static class UiIcon // UI 아이콘 (Day81 신규 — 없으면 null → 지금의 글자 기호 · 색 원 그대로)
    {
        public const string Folder = "UI/Icons/"; // 메뉴 · 내비 · 재화 아이콘 Resources 경로
        public const string RuneFolder = "Icons/Runes/"; // 룬 아이콘 경로 ({RuneKind 이름}.png)
        public const string StatusFolder = "Icons/Status/"; // 상태이상 아이콘 경로 ({BattleStatusEffectId 이름}.png)
        public const string ElementFolder = "Icons/Elements/"; // 속성 아이콘 경로 ({BattleElement 이름}.png)

        public static readonly IReadOnlyList<string> MenuKeys = new[] // 메뉴 아이콘 20종 (흰색 — 코드가 색을 입힌다)
        {
            "back", "close", "settings", "log", "auto", "skip", "hide", "help", "alert", "lock", // 뒤로 · 닫기 · 설정 · 로그 · 자동 · 건너뛰기 · 숨김 · 도움말 · 알림 · 잠김
            "check", "plus", "minus", "arrow_left", "arrow_right", "next", "save", "load", "home", "menu" // 완료 · 더하기 · 빼기 · 왼쪽 · 오른쪽 · 넘김 · 저장 · 불러오기 · 로비 · 전투 메뉴
        };

        public static readonly IReadOnlyList<string> NavKeys = new[] // 로비 내비 아이콘 8종
        {
            "nav_adventure", "nav_character", "nav_party", "nav_bag", "nav_shop", "nav_blacksmith", "nav_village", "nav_diary" // 모험 · 캐릭터 · 파티 · 가방 · 상점 · 대장간 · 마을 · 일기장
        };

        public static readonly IReadOnlyList<string> CurrencyKeys = new[] // 재화 아이콘 2종
        {
            "gold", "vitality" // 골드 · 활력
        };

        public static Sprite Get(string key) // 메뉴 · 내비 · 재화 아이콘 (없으면 null)
        {
            return string.IsNullOrEmpty(key) ? null : RuntimeSpriteLoader.Load(Folder + key); // 아이콘
        }

        public static string GetRunePath(RuneKind kind) => RuneFolder + kind; // 룬 아이콘 경로

        public static string GetStatusPath(BattleStatusEffectId id) => StatusFolder + id; // 상태이상 아이콘 경로

        public static string GetElementPath(BattleElement element) => ElementFolder + element; // 속성 아이콘 경로

        public static Sprite GetRune(RuneKind kind) => RuntimeSpriteLoader.Load(GetRunePath(kind)); // 룬 아이콘 (없으면 null)

        public static Sprite GetStatus(BattleStatusEffectId id) => id == BattleStatusEffectId.None ? null : RuntimeSpriteLoader.Load(GetStatusPath(id)); // 상태이상 아이콘 (없으면 null)

        public static Sprite GetElement(BattleElement element) => element == BattleElement.None ? null : RuntimeSpriteLoader.Load(GetElementPath(element)); // 속성 아이콘 (없으면 null)
    }
}
