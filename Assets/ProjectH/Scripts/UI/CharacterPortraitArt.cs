using System.Collections.Generic; // 사전 자료형
using UnityEngine; // 텍스처·스프라이트 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class CharacterPortraitArt // 캐릭터 정사각 초상화 (Day73 신규 · Day77 — Resources/Portraits/{캐릭터ID} → 스탠딩에서 얼굴 주변을 잘라 사용 → 색 원 임시 그림)
    {
        public const string PortraitFolder = "Portraits/"; // 초상화 Resources 경로
        private const int PlaceholderSize = 128; // 임시 초상화 크기
        private static readonly Dictionary<string, Sprite> CropCache = new Dictionary<string, Sprite>(); // 스탠딩에서 잘라 낸 초상화 캐시 (Day77)
        private static readonly Color BackdropBase = new Color(0.30f, 0.32f, 0.38f, 1f); // 초상화 칸 바탕에 섞는 회청색 (Day77)
        private static Sprite placeholder; // 임시 초상화 캐시 (색은 사용처에서 입힌다)

        public static Sprite Get(string characterId, out bool isPlaceholder) // 초상화 조회 (전용 그림 → 스탠딩에서 자른 얼굴 → 임시)
        {
            Sprite art = string.IsNullOrEmpty(characterId) ? null : RuntimeSpriteLoader.Load(PortraitFolder + characterId); // 전용 초상화
            if (art == null) art = GetFaceCrop(characterId); // 없으면 스탠딩에서 얼굴 주변을 잘라 사용 (Day77)
            isPlaceholder = art == null; // 임시 여부
            if (art != null) return art; // 정식 반환
            if (placeholder == null) placeholder = CreatePlaceholder(); // 임시 생성
            return placeholder; // 임시 반환
        }

        public static Color GetPlaceholderTint(string characterId) => DialogueArtFactory.GetCharacterTint(characterId); // 임시 초상화에 입힐 색

        public static Color GetBackdropColor(string characterId) // 초상화 칸 바탕색 (Day77 추가 — 잘라 낸 얼굴은 배경이 투명하므로 캐릭터 색을 가라앉혀 깐다)
        {
            return Color.Lerp(DialogueArtFactory.GetCharacterTint(characterId), BackdropBase, 0.5f); // 캐릭터 색 + 회청색
        }

        private static Sprite GetFaceCrop(string characterId) // 스탠딩에서 얼굴 주변 정사각을 잘라 낸 초상화 (Day77 추가 — 그림이 없으면 null)
        {
            if (string.IsNullOrEmpty(characterId)) return null; // 빈 ID
            if (CropCache.TryGetValue(characterId, out Sprite cached) && cached != null) return cached; // 캐시 (도메인 리로드 대응)
            if (!StandingFaceCatalog.TryGet(characterId, out StandingFace face)) return null; // 얼굴 위치를 재지 않은 캐릭터
            Sprite standing = DialogueArtFactory.GetStanding(characterId, null, out bool standingPlaceholder); // 기본 표정 스탠딩
            if (standingPlaceholder || standing == null || standing.texture == null) return null; // 정식 스탠딩 없음
            Rect rect = StandingFaceCatalog.GetPortraitRect(face, standing.rect); // 얼굴 주변 정사각 영역
            if (rect.width < 8f) return null; // 그림이 너무 작음
            Sprite crop = Sprite.Create(standing.texture, rect, new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect); // 같은 텍스처의 일부만 쓰는 스프라이트 (그림을 새로 만들지 않는다)
            crop.name = characterId + "_Portrait"; // 이름
            crop.hideFlags = HideFlags.HideAndDontSave; // 씬 저장 제외
            CropCache[characterId] = crop; // 캐시 저장
            return crop; // 반환
        }

        private static Sprite CreatePlaceholder() // 임시 초상화 : 가운데가 밝은 둥근 원 (색은 사용처에서 곱한다)
        {
            Texture2D texture = new Texture2D(PlaceholderSize, PlaceholderSize, TextureFormat.RGBA32, false); // 텍스처
            texture.wrapMode = TextureWrapMode.Clamp; // 가장자리 반복 없음
            texture.filterMode = FilterMode.Bilinear; // 부드럽게
            Color[] pixels = new Color[PlaceholderSize * PlaceholderSize]; // 픽셀 버퍼
            float center = (PlaceholderSize - 1) * 0.5f; // 중심
            float radius = PlaceholderSize * 0.47f; // 반지름

            for (int y = 0; y < PlaceholderSize; y++) // 세로 순회
            {
                for (int x = 0; x < PlaceholderSize; x++) // 가로 순회
                {
                    float distance = Mathf.Sqrt(((x - center) * (x - center)) + ((y - center) * (y - center))); // 중심 거리
                    float edge = Mathf.Clamp01(radius - distance + 1f); // 가장자리 부드럽게
                    float shade = Mathf.Lerp(1f, 0.72f, Mathf.Clamp01(distance / radius)); // 가운데가 밝음
                    pixels[(y * PlaceholderSize) + x] = new Color(shade, shade, shade, edge); // 픽셀 기록
                }
            }

            texture.SetPixels(pixels); // 픽셀 적용
            texture.Apply(); // 업로드
            return Sprite.Create(texture, new Rect(0f, 0f, PlaceholderSize, PlaceholderSize), new Vector2(0.5f, 0.5f), 100f); // 스프라이트 반환
        }
    }
}
