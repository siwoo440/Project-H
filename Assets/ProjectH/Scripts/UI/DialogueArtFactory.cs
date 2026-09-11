using System.Collections.Generic; // 사전 자료형
using UnityEngine; // Unity 기본 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class DialogueArtFactory // 대화 화면 배경·스탠딩 제공 (Day58 신규 — 정식 아트가 없으면 절차적 임시 이미지 생성)
    {
        private const string BackgroundFolder = "Dialogues/Backgrounds/"; // 정식 배경 Resources 경로 (배경 키 이름의 Sprite)
        private const string StandingFolder = "Dialogues/Standing/"; // 정식 스탠딩 Resources 경로 ({캐릭터ID}_{표정} 또는 {캐릭터ID})
        private const string CutInFolder = "UltimateCutIns/"; // 궁극기 컷인 전용 일러스트 Resources 경로 (Day59 추가, {캐릭터ID})
        private const int BackgroundWidth = 320; // 임시 배경 가로 픽셀
        private const int BackgroundHeight = 180; // 임시 배경 세로 픽셀
        private const int StandingWidth = 256; // 임시 스탠딩 가로 픽셀
        private const int StandingHeight = 512; // 임시 스탠딩 세로 픽셀
        private static readonly Dictionary<string, Sprite> BackgroundCache = new Dictionary<string, Sprite>(); // 배경 캐시
        private static Sprite silhouetteSprite; // 임시 실루엣 캐시

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
            Sprite art = Resources.Load<Sprite>(BackgroundFolder + safeKey); // 정식 배경 우선 사용
            if (art != null) return art; // 정식 배경 반환

            if (!BackgroundCache.TryGetValue(safeKey, out Sprite cached) || cached == null) // 캐시 확인 (도메인 리로드 대응)
            {
                cached = CreateBackground(safeKey, GetPalette(safeKey)); // 임시 배경 생성
                BackgroundCache[safeKey] = cached; // 캐시 저장
            }

            return cached; // 임시 배경 반환
        }

        public static Sprite GetStanding(string characterId, string expression, out bool isPlaceholder) // 스탠딩 스프라이트 조회
        {
            Sprite art = string.IsNullOrEmpty(expression) ? null : Resources.Load<Sprite>($"{StandingFolder}{characterId}_{expression}"); // 표정별 정식 스탠딩
            if (art == null) art = Resources.Load<Sprite>(StandingFolder + characterId); // 기본 정식 스탠딩
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
            Sprite art = Resources.Load<Sprite>(CutInFolder + characterId); // 궁극기 전용 일러스트 우선
            if (art != null) { isPlaceholder = false; return art; } // 전용 일러스트 반환
            return GetStanding(characterId, string.Empty, out isPlaceholder); // 없으면 대화 스탠딩 재사용
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
