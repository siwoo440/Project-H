using UnityEngine; // Unity 기본 기능

namespace ProjectH.Battle.Rhythm // 프로젝트 전투 리듬 영역 (Day49)
{
    public static class RhythmCircleSpriteFactory // 리듬 원형 스프라이트 절차적 생성 기능 (프로젝트에 원형 아트가 없어 런타임 생성)
    {
        private const int TextureSize = 256; // 생성 텍스처 한 변 크기(px)
        private const float RingInnerRatio = 0.78f; // 링 내부 반지름 비율 (테두리 두께 결정)
        private static Sprite cachedDiscSprite; // 캐싱된 채움 원 스프라이트
        private static Sprite cachedRingSprite; // 캐싱된 테두리 링 스프라이트

        public static Sprite GetDiscSprite() // 채움 원 스프라이트 조회
        {
            if (cachedDiscSprite == null) // 캐시 유효성 확인 (도메인 리로드 대응)
            {
                cachedDiscSprite = CreateCircleSprite("RhythmDiscSprite", -1f); // 내부 반지름 없는 채움 원 생성
            }

            return cachedDiscSprite; // 채움 원 스프라이트 반환
        }

        public static Sprite GetRingSprite() // 테두리 링 스프라이트 조회
        {
            if (cachedRingSprite == null) // 캐시 유효성 확인 (도메인 리로드 대응)
            {
                cachedRingSprite = CreateCircleSprite("RhythmRingSprite", RingInnerRatio); // 내부가 비어 있는 링 생성
            }

            return cachedRingSprite; // 테두리 링 스프라이트 반환
        }

        private static Sprite CreateCircleSprite(string spriteName, float innerRatio) // 원형 스프라이트 절차적 생성
        {
            Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false); // 원형 텍스처 생성
            texture.name = spriteName + "Texture"; // 텍스처 이름 지정
            texture.wrapMode = TextureWrapMode.Clamp; // 가장자리 반복 방지
            texture.filterMode = FilterMode.Bilinear; // 부드러운 확대 보간 적용
            texture.hideFlags = HideFlags.HideAndDontSave; // Scene 저장 대상 제외
            float center = (TextureSize - 1) * 0.5f; // 텍스처 중심 좌표 계산
            float outerRadius = center; // 바깥 반지름 계산
            float innerRadius = innerRatio * outerRadius; // 안쪽 반지름 계산 (음수면 채움 원)
            Color32[] pixels = new Color32[TextureSize * TextureSize]; // 픽셀 버퍼 생성

            for (int y = 0; y < TextureSize; y++) // 텍스처 세로 순회
            {
                for (int x = 0; x < TextureSize; x++) // 텍스처 가로 순회
                {
                    float dx = x - center; // 중심 기준 가로 거리 계산
                    float dy = y - center; // 중심 기준 세로 거리 계산
                    float distance = Mathf.Sqrt((dx * dx) + (dy * dy)); // 중심까지 거리 계산
                    float outerAlpha = Mathf.Clamp01(outerRadius - distance); // 바깥 경계 1px 안티에일리어싱 계산
                    float innerAlpha = Mathf.Clamp01(distance - innerRadius); // 안쪽 경계 1px 안티에일리어싱 계산
                    float alpha = Mathf.Min(outerAlpha, innerAlpha); // 최종 알파 결정
                    pixels[(y * TextureSize) + x] = new Color32(255, 255, 255, (byte)(alpha * 255f)); // 흰색 기반 픽셀 저장 (색상은 Image에서 지정)
                }
            }

            texture.SetPixels32(pixels); // 픽셀 버퍼 적용
            texture.Apply(false, false); // 텍스처 갱신 적용
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect); // 중심 피벗 스프라이트 생성
            sprite.name = spriteName; // 스프라이트 이름 지정
            sprite.hideFlags = HideFlags.HideAndDontSave; // Scene 저장 대상 제외
            return sprite; // 생성 스프라이트 반환
        }
    }
}
