using System.Collections.Generic; // 사전 자료형
using ProjectH.Dialogue; // 표정 연결표 기능 (Day76)
using UnityEngine; // Unity 기본 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class DialogueArtFactory // 대화 화면 배경·스탠딩 제공 (Day58 신규 — 정식 아트가 없으면 절차적 임시 이미지 생성)
    {
        private const string BackgroundFolder = "Dialogues/Backgrounds/"; // 정식 배경 Resources 경로 (배경 키 이름의 Sprite)
        public const string StandingFolder = "Dialogues/Standing/"; // 정식 스탠딩 Resources 경로 ({캐릭터ID}_{표정 키} 또는 {캐릭터ID}, Day76 공개 — 점검 표·테스트 공용)
        private const string CutInFolder = "UltimateCutIns/"; // 궁극기 컷인 전용 일러스트 Resources 경로 (Day59 추가, {캐릭터ID})
        private const int MaxAliasHops = 4; // 대체 배경을 따라가는 최대 횟수 (Day78 추가)
        private const int BackgroundWidth = 320; // 임시 배경 가로 픽셀
        private const int BackgroundHeight = 180; // 임시 배경 세로 픽셀
        private const int StandingWidth = 256; // 임시 스탠딩 가로 픽셀
        private const int StandingHeight = 512; // 임시 스탠딩 세로 픽셀
        private static readonly Dictionary<string, Sprite> BackgroundCache = new Dictionary<string, Sprite>(); // 배경 캐시
        private static Sprite silhouetteSprite; // 임시 실루엣 캐시

        private static readonly Dictionary<string, string> BackgroundAliases = new Dictionary<string, string> // 전용 그림이 없을 때 대신 쓸 배경 (Day76 추가 — 전용 그림을 넣으면 그쪽이 우선)
        {
            { "SHOP", "VILLAGE_MARKET" }, // 상점 화면 → 시장 거리
            { "VILLAGE", "VILLAGE_PLAZA" }, // 마을 지도 화면 → 광장
            { "NOIR", "OBSERVATORY_NIGHT" }, // 노아르 마법도시 → 도시가 내다보이는 밤의 관측실 (Day78 추가 — 도시 그림이 오면 그쪽이 우선)
            { "NOIR_ARCHIVE", "OBSERVATORY_NIGHT" }, // 노아르 지하 서고 → 책장이 있는 밤의 관측실 (Day78 추가)
            { "BATTLE_FOREST", "SILVARAN" }, // 전투 · 숲 → 숲 그림 (Day78 추가 — 전용 전투 배경이 오면 그쪽이 우선)
            { "BATTLE_SWAMP", "GARDEN_POND" }, // 전투 · 늪지대 → 물가 그림
            { "BATTLE_DEMON_CASTLE", "ABYSS" }, // 전투 · 마왕성 → 침식된 심연
            { "BATTLE_NOIR", "NOIR_ARCHIVE" }, // 전투 · 노아르 → 지하 서고 (서고 그림이 없으면 다시 그 대체 배경으로)
            { "BATTLE_SILVARAN", "SILVARAN" }, // 전투 · 실바란
            { "BATTLE_KARNIAN", "KARNIAN" }, // 전투 · 카르니안
            { "BATTLE_DESERT", "DESERT" }, // 전투 · 사막
            { "BATTLE_SEA", "SEA_RIFT" } // 전투 · 바다
        };

        private readonly struct BackgroundPalette // 임시 배경 색 구성
        {
            public readonly Color Top; // 하늘 위쪽 색
            public readonly Color Horizon; // 지평선 색
            public readonly Color Ground; // 바닥 색
            public readonly bool Stars; // 별 표시 여부

            public BackgroundPalette(Color top, Color horizon, Color ground, bool stars) // 팔레트 생성
            {
                Top = top; // 위쪽 색 저장
                Horizon = horizon; // 지평선 색 저장
                Ground = ground; // 바닥 색 저장
                Stars = stars; // 별 여부 저장
            }
        }

        public static Sprite GetBackground(string key) // 배경 스프라이트 조회
        {
            string safeKey = string.IsNullOrEmpty(key) ? "DEFAULT" : key; // 빈 키 기본값
            Sprite art = LoadBackgroundArt(safeKey); // 정식 배경 우선 사용 (없으면 대신 쓸 배경, Day76)
            if (art != null) return art; // 정식 배경 반환

            if (!BackgroundCache.TryGetValue(safeKey, out Sprite cached) || cached == null) // 캐시 확인 (도메인 리로드 대응)
            {
                cached = CreateBackground(safeKey, GetPalette(safeKey)); // 임시 배경 생성
                BackgroundCache[safeKey] = cached; // 캐시 저장
            }

            return cached; // 임시 배경 반환
        }

        public static string GetBackgroundAlias(string key) // 전용 그림이 없을 때 대신 쓸 배경 키 (없으면 빈 문자열, Day76 추가)
        {
            return key != null && BackgroundAliases.TryGetValue(key, out string alias) ? alias : string.Empty; // 대체 키 반환
        }

        public static bool HasBackgroundArt(string key) // 이 배경에 그림 파일이 있는지 (전용 그림 또는 대신 쓸 그림, Day76 추가)
        {
            return !string.IsNullOrEmpty(key) && LoadBackgroundArt(key) != null; // 그림 존재 여부
        }

        private static Sprite LoadBackgroundArt(string key) // 배경 그림 파일 조회 : 전용 그림 → 대신 쓸 그림 (Day76 추가 · Day78 — 대체 배경의 대체 배경까지 따라간다)
        {
            string current = key; // 지금 찾는 키

            for (int hop = 0; hop < MaxAliasHops && !string.IsNullOrEmpty(current); hop++) // 대체 배경을 차례로 따라감 (횟수 제한으로 순환 방지)
            {
                Sprite art = RuntimeSpriteLoader.Load(BackgroundFolder + current); // 그림 조회
                if (art != null) return art; // 찾음
                current = GetBackgroundAlias(current); // 다음 대체 배경
            }

            return null; // 그림 없음
        }

        public static List<string> GetStandingResourcePaths(string characterId, string expression) // 스탠딩을 찾는 순서 (Day76 추가 — 표정 키 → 대사에 적힌 이름 → 기본 표정 → 표정 없는 그림)
        {
            List<string> paths = new List<string>(4); // 찾을 경로
            string key = ExpressionCatalog.Resolve(expression); // 그림 표정 키 (미소 → smile)
            string raw = string.IsNullOrWhiteSpace(expression) ? string.Empty : expression.Trim(); // 대사에 적힌 이름
            paths.Add($"{StandingFolder}{characterId}_{key}"); // 1순위 : {ID}_{표정 키}
            if (raw.Length > 0 && raw != key) paths.Add($"{StandingFolder}{characterId}_{raw}"); // 2순위 : {ID}_{대사에 적힌 이름} (이전 규칙 호환)
            if (key != ExpressionCatalog.Normal) paths.Add($"{StandingFolder}{characterId}_{ExpressionCatalog.Normal}"); // 3순위 : {ID}_normal (그 표정 그림이 아직 없을 때)
            paths.Add(StandingFolder + characterId); // 4순위 : 표정 이름이 없는 그림
            return paths; // 순서대로 반환
        }

        public static Sprite GetStanding(string characterId, string expression, out bool isPlaceholder) // 스탠딩 스프라이트 조회
        {
            Sprite art = null; // 찾은 그림
            List<string> paths = GetStandingResourcePaths(characterId, expression); // 찾는 순서 (Day76)

            for (int index = 0; index < paths.Count && art == null; index++) // 찾을 때까지 순회
            {
                art = RuntimeSpriteLoader.Load(paths[index]); // 정식 스탠딩 조회
            }

            isPlaceholder = art == null; // 임시 이미지 여부
            if (art != null) return art; // 정식 스탠딩 반환

            if (silhouetteSprite == null) // 실루엣 캐시 확인
            {
                silhouetteSprite = CreateSilhouette(); // 임시 실루엣 생성
            }

            return silhouetteSprite; // 임시 실루엣 반환 (색은 GetCharacterTint로 입힘)
        }

        public static Sprite GetCutIn(string characterId, out bool isPlaceholder) // 궁극기 컷인 일러스트 조회 (Day59 추가 — 전용 컷인 → 대화 스탠딩 → 임시 실루엣)
        {
            Sprite art = RuntimeSpriteLoader.Load(CutInFolder + characterId); // 궁극기 전용 일러스트 우선
            if (art != null) { isPlaceholder = false; return art; } // 전용 일러스트 반환
            return GetStanding(characterId, string.Empty, out isPlaceholder); // 없으면 대화 스탠딩 재사용
        }

        public static bool HasCutInArt(string characterId) // 궁극기 전용 일러스트가 있는지 (Day77 추가 — 없으면 스탠딩을 얼굴 기준으로 배치한다)
        {
            return !string.IsNullOrEmpty(characterId) && RuntimeSpriteLoader.Load(CutInFolder + characterId) != null; // 전용 그림 존재 여부
        }

        public static Color GetCharacterTint(string characterId) // 캐릭터 임시 실루엣 색
        {
            switch (characterId) // 캐릭터별 분기
            {
                case "CH_SERENA": // 세레나 처리
                    return new Color(0.98f, 0.93f, 0.80f, 1f); // 아이보리·금색 (신성력)
                case "CH_ELLEN": // 엘렌 처리
                    return new Color(0.62f, 0.68f, 0.82f, 1f); // 강철 남색
                case "CH_LILIA": // 릴리아 처리
                    return new Color(0.62f, 0.55f, 0.90f, 1f); // 청보라 (연산 원)
                case "CH_EVE": // 이브 처리
                    return new Color(0.60f, 0.84f, 0.64f, 1f); // 숲 초록
                case "CH_NATASHA": // 나타샤 처리 (Day64 추가)
                    return new Color(0.52f, 0.40f, 0.62f, 1f); // 그림자 보라
                case "CH_CLAIRE": // 클레어 처리 (Day64 추가)
                    return new Color(0.72f, 0.88f, 0.52f, 1f); // 연금 연두
                case "CH_LUCIA": // 루시아 처리 (Day64 추가)
                    return new Color(0.86f, 0.70f, 0.50f, 1f); // 사막 모래
                case "CH_PYRA": // 파이라 처리 (Day64 추가)
                    return new Color(0.98f, 0.52f, 0.40f, 1f); // 불꽃 주홍
                case "CH_TYRIA": // 티리아 처리 (Day64 추가)
                    return new Color(0.66f, 0.70f, 0.74f, 1f); // 방패 강철
                case "CH_MERCIA": // 메르시아 처리 (Day64 추가)
                    return new Color(0.96f, 0.80f, 0.46f, 1f); // 사원 금빛
                case "CH_NOEL": // 노엘 처리 (Day64 추가)
                    return new Color(0.56f, 0.82f, 0.94f, 1f); // 얼음 하늘
                case "CH_SEPHIRA": // 세피라 처리 (Day64 추가)
                    return new Color(0.94f, 0.94f, 0.98f, 1f); // 순백
                case "NPC_SHOPKEEPER": // 상점 주인 처리 (Day61 추가)
                    return new Color(0.96f, 0.80f, 0.62f, 1f); // 따뜻한 살구색
                case "NPC_BLACKSMITH": // 대장장이 처리 (Day61 추가)
                    return new Color(0.86f, 0.52f, 0.34f, 1f); // 불빛 구릿빛
                default: // 기타 캐릭터 처리
                    return new Color(0.80f, 0.80f, 0.84f, 1f); // 회색
            }
        }

        private static BackgroundPalette GetPalette(string key) // 배경 키별 색 구성
        {
            switch (key) // 키 분기
            {
                case "SANCTUARY_PRACTICE": // 성역 연습 자리
                    return new BackgroundPalette(new Color(0.66f, 0.80f, 0.96f), new Color(0.98f, 0.95f, 0.86f), new Color(0.80f, 0.76f, 0.66f), false); // 맑은 성역
                case "CORRIDOR_MORNING": // 훈련 뒤 회랑
                    return new BackgroundPalette(new Color(0.78f, 0.84f, 0.94f), new Color(0.96f, 0.88f, 0.74f), new Color(0.58f, 0.56f, 0.56f), false); // 아침 석조 회랑
                case "OBSERVATORY_NIGHT": // 밤의 관측실
                    return new BackgroundPalette(new Color(0.05f, 0.06f, 0.18f), new Color(0.30f, 0.22f, 0.46f), new Color(0.14f, 0.12f, 0.22f), true); // 별빛 관측실
                case "GARDEN_POND": // 성역 정원 연못
                    return new BackgroundPalette(new Color(0.60f, 0.78f, 0.86f), new Color(0.90f, 0.94f, 0.80f), new Color(0.42f, 0.62f, 0.46f), false); // 초록 정원
                case "MORNING": // 아침 일상
                    return new BackgroundPalette(new Color(0.80f, 0.88f, 0.98f), new Color(1f, 0.88f, 0.70f), new Color(0.72f, 0.70f, 0.64f), false); // 아침빛
                case "DAY": // 낮 일상
                    return new BackgroundPalette(new Color(0.46f, 0.70f, 0.96f), new Color(0.90f, 0.95f, 0.99f), new Color(0.66f, 0.72f, 0.60f), false); // 파란 하늘
                case "EVENING": // 저녁 일상
                    return new BackgroundPalette(new Color(0.40f, 0.32f, 0.56f), new Color(0.98f, 0.62f, 0.42f), new Color(0.36f, 0.28f, 0.34f), false); // 노을
                case "SHOP": // 상점 내부 (Day61 추가)
                    return new BackgroundPalette(new Color(0.36f, 0.24f, 0.16f), new Color(0.86f, 0.66f, 0.42f), new Color(0.42f, 0.28f, 0.18f), false); // 등불 켠 목조 가게
                case "BLACKSMITH": // 대장간 내부 (Day61 추가)
                    return new BackgroundPalette(new Color(0.08f, 0.06f, 0.06f), new Color(0.72f, 0.30f, 0.10f), new Color(0.16f, 0.12f, 0.10f), false); // 화로 불빛
                case "VILLAGE": // 마을 전경 지도 (Day62 추가)
                    return new BackgroundPalette(new Color(0.52f, 0.74f, 0.92f), new Color(0.92f, 0.90f, 0.78f), new Color(0.50f, 0.66f, 0.42f), false); // 초록 들판 마을
                case "VILLAGE_PLAZA": // 광장 (Day62 추가)
                    return new BackgroundPalette(new Color(0.50f, 0.72f, 0.96f), new Color(0.96f, 0.94f, 0.88f), new Color(0.74f, 0.70f, 0.62f), false); // 밝은 돌바닥 광장
                case "VILLAGE_MARKET": // 시장 (Day62 추가)
                    return new BackgroundPalette(new Color(0.62f, 0.74f, 0.92f), new Color(1f, 0.86f, 0.62f), new Color(0.66f, 0.46f, 0.30f), false); // 천막 노점 거리
                case "VILLAGE_ONSEN": // 온천 (Day62 추가)
                    return new BackgroundPalette(new Color(0.46f, 0.52f, 0.66f), new Color(0.86f, 0.88f, 0.92f), new Color(0.40f, 0.56f, 0.60f), false); // 김 오르는 노천탕
                case "VILLAGE_INN": // 여관 (Day62 추가)
                    return new BackgroundPalette(new Color(0.34f, 0.24f, 0.18f), new Color(0.92f, 0.70f, 0.44f), new Color(0.40f, 0.28f, 0.20f), false); // 등불 켠 목조 여관
                case "VILLAGE_GUILD": // 길드 (Day62 추가)
                    return new BackgroundPalette(new Color(0.30f, 0.34f, 0.44f), new Color(0.78f, 0.72f, 0.60f), new Color(0.36f, 0.32f, 0.30f), false); // 석조 길드 홀
                case "INN_NIGHT": // 여관 밤 (Day62 추가)
                    return new BackgroundPalette(new Color(0.03f, 0.04f, 0.10f), new Color(0.24f, 0.16f, 0.22f), new Color(0.10f, 0.07f, 0.08f), true); // 달빛 비치는 방
                case "ABYSS": // 침식된 심연 · 무명의 옥좌 (Day78 추가)
                    return new BackgroundPalette(new Color(0.05f, 0.03f, 0.10f), new Color(0.34f, 0.14f, 0.42f), new Color(0.08f, 0.06f, 0.12f), true); // 검보랏빛 심연
                case "NOIR_ARCHIVE": // 노아르 지하 서고 (Day78 추가)
                    return new BackgroundPalette(new Color(0.05f, 0.07f, 0.18f), new Color(0.24f, 0.34f, 0.62f), new Color(0.12f, 0.12f, 0.20f), false); // 푸른 등이 켜진 지하 서고
                case "NOIR": // 노아르 마법도시 (Day65 추가)
                    return new BackgroundPalette(new Color(0.06f, 0.08f, 0.22f), new Color(0.32f, 0.46f, 0.86f), new Color(0.14f, 0.16f, 0.30f), true); // 푸른 마법등 밤거리
                case "SILVARAN": // 실바란 숲 (Day65 추가)
                    return new BackgroundPalette(new Color(0.20f, 0.42f, 0.30f), new Color(0.74f, 0.92f, 0.62f), new Color(0.14f, 0.30f, 0.16f), false); // 햇살 드는 원시림
                case "KARNIAN": // 카르니안 설원 (Day66 추가)
                    return new BackgroundPalette(new Color(0.58f, 0.66f, 0.78f), new Color(0.92f, 0.95f, 0.98f), new Color(0.78f, 0.82f, 0.88f), false); // 눈보라 치는 설원 요새
                case "DESERT": // 아스타르 사막 (Day66 추가)
                    return new BackgroundPalette(new Color(0.96f, 0.70f, 0.40f), new Color(1f, 0.90f, 0.66f), new Color(0.82f, 0.62f, 0.36f), false); // 달궈진 모래 언덕
                case "SEA_RIFT": // 서쪽 해안 검은 균열 (Day67 추가)
                    return new BackgroundPalette(new Color(0.10f, 0.08f, 0.24f), new Color(0.52f, 0.28f, 0.78f), new Color(0.14f, 0.20f, 0.34f), true); // 보랏빛 균열이 뜬 바다
                case "NIGHT": // 밤 일상
                    return new BackgroundPalette(new Color(0.03f, 0.05f, 0.14f), new Color(0.16f, 0.20f, 0.38f), new Color(0.08f, 0.09f, 0.16f), true); // 밤하늘
                default: // 기본 처리
                    return new BackgroundPalette(new Color(0.78f, 0.80f, 0.90f), new Color(0.95f, 0.95f, 0.97f), new Color(0.70f, 0.70f, 0.74f), false); // 회색 기본
            }
        }

        private static Sprite CreateBackground(string key, BackgroundPalette palette) // 임시 배경 생성 (하늘 그라데이션·지평선·바닥·별)
        {
            Texture2D texture = NewTexture($"DialogueBg_{key}", BackgroundWidth, BackgroundHeight); // 텍스처 생성
            Color32[] pixels = new Color32[BackgroundWidth * BackgroundHeight]; // 픽셀 버퍼
            float horizon = 0.34f; // 지평선 높이 비율
            System.Random random = new System.Random(key.GetHashCode()); // 키별 고정 난수 (별 위치 고정)

            for (int y = 0; y < BackgroundHeight; y++) // 세로 순회
            {
                float v = y / (float)(BackgroundHeight - 1); // 세로 비율 (0 아래 ~ 1 위)

                for (int x = 0; x < BackgroundWidth; x++) // 가로 순회
                {
                    float u = x / (float)(BackgroundWidth - 1); // 가로 비율
                    Color color = v >= horizon ? Color.Lerp(palette.Horizon, palette.Top, Mathf.Pow((v - horizon) / (1f - horizon), 0.8f)) : Color.Lerp(palette.Ground * 0.82f, palette.Ground, v / horizon); // 하늘·바닥 색
                    float vignette = 1f - (0.18f * Mathf.Pow(Mathf.Abs(u - 0.5f) * 2f, 2f)); // 좌우 가장자리 어둡게
                    Color shaded = color * vignette; // 가장자리 음영 적용
                    shaded.a = 1f; // 불투명 유지 (뒤 화면 비침 방지)
                    pixels[(y * BackgroundWidth) + x] = shaded; // 픽셀 저장
                }
            }

            if (palette.Stars) // 별 표시 확인
            {
                for (int index = 0; index < 90; index++) // 별 90개 배치
                {
                    int x = random.Next(BackgroundWidth); // 별 가로 위치
                    int y = random.Next((int)(BackgroundHeight * (horizon + 0.08f)), BackgroundHeight); // 별 세로 위치 (하늘만)
                    byte bright = (byte)random.Next(170, 256); // 별 밝기
                    pixels[(y * BackgroundWidth) + x] = new Color32(bright, bright, 255, 255); // 별 픽셀 저장
                }
            }

            texture.SetPixels32(pixels); // 픽셀 적용
            texture.Apply(false, false); // 텍스처 갱신
            return ToSprite(texture); // 스프라이트 반환
        }

        private static Sprite CreateSilhouette() // 임시 스탠딩 실루엣 생성 (머리·목·어깨·몸통)
        {
            Texture2D texture = NewTexture("DialogueSilhouette", StandingWidth, StandingHeight); // 텍스처 생성
            Color32[] pixels = new Color32[StandingWidth * StandingHeight]; // 픽셀 버퍼
            float cx = StandingWidth * 0.5f; // 가로 중심

            for (int y = 0; y < StandingHeight; y++) // 세로 순회
            {
                for (int x = 0; x < StandingWidth; x++) // 가로 순회
                {
                    float dx = Mathf.Abs(x - cx); // 중심 가로 거리
                    float head = 60f - Mathf.Sqrt((dx * dx) + ((y - 420f) * (y - 420f))); // 머리 원 (반지름 60) 안쪽 거리
                    float hair = 74f - Mathf.Sqrt((dx * dx * 0.9f) + ((y - 402f) * (y - 402f) * 0.55f)); // 긴 머리 타원
                    float neck = y > 330f && y < 380f ? 20f - dx : -1f; // 목 사각형
                    float bodyHalf = y <= 340f ? 58f + (64f * Mathf.Pow(1f - (y / 340f), 0.55f)) - (y > 300f ? (y - 300f) * 0.9f : 0f) : -1f; // 어깨에서 치마까지 폭
                    float body = bodyHalf - dx; // 몸통 안쪽 거리
                    float inside = Mathf.Max(Mathf.Max(head, hair * 0.9f), Mathf.Max(neck, body)); // 가장 가까운 형태 기준
                    float alpha = Mathf.Clamp01(inside / 2f); // 2px 부드러운 경계
                    float shade = 0.80f + (0.20f * (y / (float)StandingHeight)); // 위쪽이 밝은 음영
                    byte channel = (byte)(255f * shade); // 음영 채널 값
                    pixels[(y * StandingWidth) + x] = new Color32(channel, channel, channel, (byte)(alpha * 235f)); // 반투명 흰색 픽셀 (색은 Image에서 입힘)
                }
            }

            texture.SetPixels32(pixels); // 픽셀 적용
            texture.Apply(false, false); // 텍스처 갱신
            return ToSprite(texture); // 스프라이트 반환
        }

        private static Texture2D NewTexture(string name, int width, int height) // 공용 텍스처 생성
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false); // 텍스처 생성
            texture.name = name; // 이름 지정
            texture.wrapMode = TextureWrapMode.Clamp; // 가장자리 반복 방지
            texture.filterMode = FilterMode.Bilinear; // 부드러운 확대
            texture.hideFlags = HideFlags.HideAndDontSave; // 씬 저장 제외
            return texture; // 텍스처 반환
        }

        private static Sprite ToSprite(Texture2D texture) // 텍스처를 스프라이트로 변환
        {
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0f), 100f, 0u, SpriteMeshType.FullRect); // 아래 중심 피벗 스프라이트
            sprite.name = texture.name; // 이름 지정
            sprite.hideFlags = HideFlags.HideAndDontSave; // 씬 저장 제외
            return sprite; // 스프라이트 반환
        }
    }
}
