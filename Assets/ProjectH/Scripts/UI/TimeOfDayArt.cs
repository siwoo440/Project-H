using System.Collections.Generic; // 사전 자료형
using ProjectH.SaveSystem; // 시간대 기능
using UnityEngine; // 텍스처·스프라이트 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class TimeOfDayArt // 시간대 그림·문구 (Day62 신규 — 정식 그림이 없으면 하늘·해/달·언덕 임시 그림 생성)
    {
        private const string ResourceFolder = "TimeOfDay/"; // 정식 그림 Resources 경로 (MORNING · DAY · EVENING · NIGHT)
        private const int Size = 256; // 임시 그림 크기
        private static readonly Dictionary<SaveTimeOfDay, Sprite> Cache = new Dictionary<SaveTimeOfDay, Sprite>(); // 임시 그림 캐시

        public static Sprite Get(SaveTimeOfDay phase) // 시간대 그림 (정식 그림 우선)
        {
            Sprite art = Resources.Load<Sprite>(ResourceFolder + phase.ToString().ToUpperInvariant()); // 정식 그림
            if (art != null) return art; // 정식 그림 반환

            if (!Cache.TryGetValue(phase, out Sprite cached) || cached == null) // 캐시 확인 (도메인 리로드 대응)
            {
                cached = Create(phase); // 임시 그림 생성
                Cache[phase] = cached; // 캐시 저장
            }

            return cached; // 임시 그림 반환
        }

        public static string GetSubtitle(SaveTimeOfDay phase) // 시간대 한 줄 문구
        {
            switch (phase) // 시간대 분기
            {
                case SaveTimeOfDay.Morning: return "창밖으로 햇살이 스며든다."; // 아침
                case SaveTimeOfDay.Day: return "해가 머리 위에 높이 떴다."; // 점심
                case SaveTimeOfDay.Evening: return "하늘이 노을빛으로 물든다."; // 저녁
                default: return "별이 하나둘 떠오르는 조용한 밤."; // 밤
            }
        }

        public static Color GetAccent(SaveTimeOfDay phase) // 시간대 강조색 (글자·테두리)
        {
            switch (phase) // 시간대 분기
            {
                case SaveTimeOfDay.Morning: return new Color(1f, 0.84f, 0.52f, 1f); // 아침 금빛
                case SaveTimeOfDay.Day: return new Color(0.62f, 0.86f, 1f, 1f); // 점심 하늘색
                case SaveTimeOfDay.Evening: return new Color(1f, 0.58f, 0.40f, 1f); // 저녁 주황
                default: return new Color(0.72f, 0.72f, 1f, 1f); // 밤 달빛 보라
            }
        }

        private static Sprite Create(SaveTimeOfDay phase) // 임시 그림 생성 (하늘 그라데이션 + 해/달 + 언덕 두 겹)
        {
            Color top; // 하늘 위
            Color horizon; // 지평선
            Color hillFar; // 먼 언덕
            Color hillNear; // 가까운 언덕
            Vector2 orb; // 해·달 위치 (0~1)
            Color orbColor; // 해·달 색
            float orbRadius; // 해·달 반지름 (0~1)

            switch (phase) // 시간대별 구성
            {
                case SaveTimeOfDay.Morning: // 아침 : 왼쪽 낮은 해
                    top = new Color(0.52f, 0.72f, 0.95f); horizon = new Color(1f, 0.86f, 0.64f); hillFar = new Color(0.52f, 0.66f, 0.50f); hillNear = new Color(0.34f, 0.52f, 0.34f); orb = new Vector2(0.27f, 0.50f); orbColor = new Color(1f, 0.86f, 0.46f); orbRadius = 0.09f; // 아침 색
                    break; // 아침 끝
                case SaveTimeOfDay.Day: // 점심 : 가운데 높은 해
                    top = new Color(0.28f, 0.58f, 0.96f); horizon = new Color(0.80f, 0.92f, 1f); hillFar = new Color(0.46f, 0.70f, 0.42f); hillNear = new Color(0.30f, 0.56f, 0.28f); orb = new Vector2(0.50f, 0.80f); orbColor = new Color(1f, 0.98f, 0.82f); orbRadius = 0.10f; // 점심 색
                    break; // 점심 끝
                case SaveTimeOfDay.Evening: // 저녁 : 오른쪽 지는 해
                    top = new Color(0.34f, 0.24f, 0.52f); horizon = new Color(1f, 0.56f, 0.34f); hillFar = new Color(0.40f, 0.26f, 0.34f); hillNear = new Color(0.22f, 0.14f, 0.22f); orb = new Vector2(0.74f, 0.46f); orbColor = new Color(1f, 0.46f, 0.26f); orbRadius = 0.11f; // 저녁 색
                    break; // 저녁 끝
                default: // 밤 : 초승달 + 별
                    top = new Color(0.02f, 0.03f, 0.12f); horizon = new Color(0.14f, 0.16f, 0.34f); hillFar = new Color(0.08f, 0.09f, 0.18f); hillNear = new Color(0.04f, 0.05f, 0.10f); orb = new Vector2(0.70f, 0.76f); orbColor = new Color(0.95f, 0.94f, 0.80f); orbRadius = 0.08f; // 밤 색
                    break; // 밤 끝
            }

            Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false); // 텍스처
            texture.wrapMode = TextureWrapMode.Clamp; // 가장자리 반복 없음
            Color[] pixels = new Color[Size * Size]; // 픽셀 버퍼
            System.Random random = new System.Random(7); // 별 위치 고정 난수

            for (int y = 0; y < Size; y++) // 세로 순회
            {
                float v = y / (float)(Size - 1); // 0 아래 ~ 1 위

                for (int x = 0; x < Size; x++) // 가로 순회
                {
                    float u = x / (float)(Size - 1); // 0 왼쪽 ~ 1 오른쪽
                    Color color = Color.Lerp(horizon, top, Mathf.InverseLerp(0.30f, 1f, v)); // 하늘 그라데이션
                    float glow = Mathf.Clamp01(1f - (Vector2.Distance(new Vector2(u, v), orb) / (orbRadius * 3.2f))); // 해·달 주변 빛
                    color = Color.Lerp(color, orbColor, glow * glow * (phase == SaveTimeOfDay.Night ? 0.25f : 0.45f)); // 빛 번짐
                    float distance = Vector2.Distance(new Vector2(u, v), orb); // 해·달 중심 거리

                    if (distance < orbRadius) // 해·달 원
                    {
                        bool crescentCut = phase == SaveTimeOfDay.Night && Vector2.Distance(new Vector2(u, v), orb + new Vector2(0.035f, 0.025f)) < orbRadius * 0.92f; // 초승달 파낸 부분
                        if (!crescentCut) color = orbColor; // 원 채우기
                    }

                    float far = 0.30f + (0.05f * Mathf.Sin((u * 7.5f) + 1.2f)) + (0.03f * Mathf.Sin(u * 17f)); // 먼 언덕 높이
                    float near = 0.18f + (0.06f * Mathf.Sin((u * 4.2f) + 3.1f)) + (0.02f * Mathf.Sin(u * 23f)); // 가까운 언덕 높이
                    if (v < far) color = hillFar; // 먼 언덕
                    if (v < near) color = hillNear; // 가까운 언덕
                    pixels[(y * Size) + x] = color; // 픽셀 저장
                }
            }

            if (phase == SaveTimeOfDay.Night) // 밤하늘 별
            {
                for (int star = 0; star < 70; star++) // 별 70개
                {
                    int sx = random.Next(Size); // 가로 위치
                    int sy = random.Next((int)(Size * 0.40f), Size); // 언덕 위 하늘
                    pixels[(sy * Size) + sx] = Color.Lerp(pixels[(sy * Size) + sx], Color.white, 0.5f + ((float)random.NextDouble() * 0.5f)); // 별 밝기
                }
            }

            texture.SetPixels(pixels); // 픽셀 적용
            texture.Apply(); // 업로드
            return Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), 100f); // 스프라이트 반환
        }
    }
}
