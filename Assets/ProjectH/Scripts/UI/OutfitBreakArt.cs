using ProjectH.Dialogue; // 표정 연결표 기능
using UnityEngine; // 스프라이트 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class OutfitBreakArt // 의상 파괴 그림 찾기 (Day87 신규 — 전용 그림이 없으면 스탠딩의 표정 그림으로 대신한다)
    {
        public const string Folder = "BattleBreak/"; // 전용 그림 Resources 경로 ({캐릭터 ID}_{단계}.png — 스탠딩과 같은 1024×1536 · 같은 자세)

        public static string GetResourcePath(string characterId, int stage) // 전용 그림 경로 (예 : BattleBreak/CH_SERENA_2)
        {
            return $"{Folder}{characterId}_{stage}"; // 캐릭터 ID + 단계
        }

        public static string GetFallbackExpression(int stage) // 전용 그림이 없을 때 쓸 스탠딩 표정
        {
            return stage >= 2 ? ExpressionCatalog.Sad : ExpressionCatalog.Surprised; // 2단계는 슬픈 얼굴, 1단계는 놀란 얼굴
        }

        public static bool HasArt(string characterId, int stage) // 그 단계의 전용 그림이 있는지
        {
            return !string.IsNullOrEmpty(characterId) && stage > 0 && RuntimeSpriteLoader.Load(GetResourcePath(characterId, stage)) != null; // 파일 존재 여부
        }

        public static Sprite Get(string characterId, int stage, out bool dedicated) // 보여 줄 그림 (그 단계 그림 → 아래 단계 그림 → 스탠딩 표정 → 없으면 null)
        {
            dedicated = false; // 전용 그림 여부
            if (string.IsNullOrEmpty(characterId) || stage <= 0) return null; // 대상 없음

            for (int current = stage; current >= 1; current--) // 그 단계부터 아래 단계로 (2단계 그림이 아직 없으면 1단계 그림)
            {
                Sprite sprite = RuntimeSpriteLoader.Load(GetResourcePath(characterId, current)); // 전용 그림
                if (sprite == null) continue; // 없음
                dedicated = true; // 전용 그림
                return sprite; // 전용 그림 반환
            }

            Sprite standing = DialogueArtFactory.GetStanding(characterId, GetFallbackExpression(stage), out bool placeholder); // 스탠딩 표정으로 대신
            return placeholder ? null : standing; // 스탠딩도 없는 캐릭터(실루엣)는 보여 주지 않는다
        }
    }
}
