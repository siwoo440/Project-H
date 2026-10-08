using System.Collections.Generic; // 사전 자료형
using UnityEngine; // 벡터·사각형 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public readonly struct StandingFace // 스탠딩 그림 안의 얼굴 위치 (그림 크기에 대한 비율, Day77 신규)
    {
        public readonly float CenterX; // 얼굴 중심 가로 (0 왼쪽 ~ 1 오른쪽)
        public readonly float CenterY; // 얼굴 중심 세로 (0 위 ~ 1 아래)
        public readonly float Height; // 얼굴 높이 (그림 높이에 대한 비율)

        public StandingFace(float centerX, float centerY, float height) // 얼굴 위치 생성
        {
            CenterX = centerX; // 가로 저장
            CenterY = centerY; // 세로 저장
            Height = height; // 높이 저장
        }
    }

    public static class StandingFaceCatalog // 스탠딩 얼굴 위치표 (Day77 신규 — 초상화 자르기와 궁극기 컷인 구도가 함께 쓴다)
    {
        private const float SourceWidth = 1024f; // 값을 잰 그림의 가로 픽셀
        private const float SourceHeight = 1536f; // 값을 잰 그림의 세로 픽셀
        public const float PortraitScale = 2.6f; // 초상화 한 변 = 얼굴 높이 × 이 값 (머리 전체와 어깨선까지 들어온다)

        private static readonly Dictionary<string, StandingFace> Faces = new Dictionary<string, StandingFace> // 캐릭터별 얼굴 위치 (76일차 그림의 표정 영역을 잰 값 — 그림을 새로 받으면 다시 잴 것)
        {
            { "CH_SERENA", FromPixels(512f, 170f, 62f) }, // 세레나
            { "CH_ELLEN", FromPixels(510f, 174f, 62f) }, // 엘렌
            { "CH_LILIA", FromPixels(512f, 172f, 61f) }, // 릴리아
            { "CH_EVE", FromPixels(514f, 171f, 61f) }, // 이브
            { "CH_LUCIA", FromPixels(508f, 173f, 59f) }, // 루시아
            { "CH_CLAIRE", FromPixels(507f, 180f, 61f) }, // 클레어
            { "CH_MERCIA", FromPixels(514f, 174f, 61f) }, // 메르시아
            { "CH_PYRA", FromPixels(528f, 207f, 58f) }, // 파이라 (높은 포니테일이라 얼굴이 조금 아래)
            { "CH_TYRIA", FromPixels(513f, 177f, 61f) }, // 티리아
            { "CH_NOEL", FromPixels(499f, 195f, 60f) }, // 노엘
            { "CH_NATASHA", FromPixels(512f, 184f, 60f) }, // 나타샤
            { "CH_SEPHIRA", FromPixels(514f, 179f, 62f) } // 세피라
        };

        public static readonly StandingFace Default = FromPixels(512f, 178f, 61f); // 위치표에 없는 그림에 쓰는 기준값 (12인 평균)
        public static IEnumerable<string> CharacterIds => Faces.Keys; // 위치를 잰 캐릭터 목록

        private static StandingFace FromPixels(float centerX, float centerY, float halfHeight) // 잰 픽셀 값을 비율로 변환 (세로는 위에서부터)
        {
            return new StandingFace(centerX / SourceWidth, centerY / SourceHeight, (halfHeight * 2f) / SourceHeight); // 비율로 저장
        }

        public static bool TryGet(string characterId, out StandingFace face) // 얼굴 위치 조회 (없으면 false)
        {
            if (!string.IsNullOrEmpty(characterId) && Faces.TryGetValue(characterId, out face)) return true; // 잰 값 있음
            face = Default; // 기준값
            return false; // 잰 값 없음
        }

        public static Rect GetPortraitRect(StandingFace face, Rect spriteRect) // 초상화로 자를 정사각 영역 (스프라이트 픽셀 좌표 · 원점은 왼쪽 아래)
        {
            float side = Mathf.Floor(Mathf.Min(face.Height * PortraitScale * spriteRect.height, Mathf.Min(spriteRect.width, spriteRect.height))); // 한 변 (그림보다 클 수 없음)
            float centerX = spriteRect.x + (face.CenterX * spriteRect.width); // 얼굴 중심 가로
            float centerY = spriteRect.y + ((1f - face.CenterY) * spriteRect.height); // 얼굴 중심 세로 (아래 기준으로 뒤집음)
            float x = Mathf.Floor(Mathf.Clamp(centerX - (side * 0.5f), spriteRect.x, spriteRect.xMax - side)); // 그림 안으로 제한
            float y = Mathf.Floor(Mathf.Clamp(centerY - (side * 0.5f), spriteRect.y, spriteRect.yMax - side)); // 그림 안으로 제한
            return new Rect(x, y, side, side); // 정사각 영역 반환
        }

        public static Vector2 GetFacePivot(StandingFace face) // 얼굴 중심을 RectTransform 피벗으로 (세로는 아래 기준)
        {
            return new Vector2(face.CenterX, 1f - face.CenterY); // 피벗 반환
        }

        public static Vector2 GetSizeForFaceHeight(StandingFace face, float spriteAspect, float faceHeight) // 얼굴이 원하는 높이로 보이게 하는 그림 전체 크기
        {
            float height = faceHeight / Mathf.Max(0.0001f, face.Height); // 그림 전체 높이
            return new Vector2(height * spriteAspect, height); // 비율을 지킨 크기
        }

        public static void FrameOnFace(RectTransform rect, StandingFace face, float spriteAspect, float faceHeight) // 기준점에 얼굴 중심이 오고 얼굴이 정해진 높이로 보이게 맞춤
        {
            if (rect == null) return; // 대상 없음
            rect.pivot = GetFacePivot(face); // 얼굴 중심이 기준점
            rect.sizeDelta = GetSizeForFaceHeight(face, spriteAspect, faceHeight); // 얼굴 높이에 맞춘 크기
        }
    }
}
