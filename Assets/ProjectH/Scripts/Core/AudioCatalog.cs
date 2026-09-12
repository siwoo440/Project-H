namespace ProjectH.Core // 프로젝트 핵심 영역
{
    public static class AudioCatalog // 소리 파일 경로와 씬별 배경음 (Day73 신규 — 파일을 넣기만 하면 교체된다)
    {
        public const string BgmFolder = "Audio/Bgm/"; // 배경음 Resources 경로
        public const string SfxFolder = "Audio/Sfx/"; // 효과음 Resources 경로

        public const string BgmTitle = "TITLE"; // 타이틀 배경음
        public const string BgmVillage = "VILLAGE"; // 마을·평상시 배경음
        public const string BgmBattle = "BATTLE"; // 전투 배경음

        public const string SfxClick = "UI_CLICK"; // 버튼 누름
        public const string SfxConfirm = "UI_CONFIRM"; // 확인
        public const string SfxCancel = "UI_CANCEL"; // 취소·닫기
        public const string SfxPage = "UI_PAGE"; // 화면·페이지 넘김
        public const string SfxGold = "GET_GOLD"; // 골드 획득
        public const string SfxItem = "GET_ITEM"; // 아이템 획득
        public const string SfxHit = "BATTLE_HIT"; // 타격
        public const string SfxPerfect = "BATTLE_PERFECT"; // Perfect 판정
        public const string SfxLevelUp = "LEVEL_UP"; // 레벨 업

        public static string GetSceneBgm(string sceneName) // 씬에 맞는 배경음 (없으면 빈 값 = 끄기)
        {
            switch (sceneName) // 씬 분기
            {
                case GameScenes.Title: // 타이틀
                    return BgmTitle; // 타이틀 곡
                case GameScenes.Battle: // 전투
                    return BgmBattle; // 전투 곡
                case GameScenes.Bootstrap: // 부트스트랩
                    return string.Empty; // 소리 없음
                default: // 로비 · 마을 · 상점 · 대장간 · 지도 · 일기장 등
                    return BgmVillage; // 평상시 곡
            }
        }
    }
}
