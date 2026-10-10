namespace ProjectH.Core // 프로젝트 핵심 영역
{
    public static class AudioCatalog // 소리 파일 경로와 씬별 배경음 (Day73 신규 — 파일을 넣기만 하면 교체된다)
    {
        public const string BgmFolder = "Audio/Bgm/"; // 배경음 Resources 경로
        public const string SfxFolder = "Audio/Sfx/"; // 효과음 Resources 경로

        public const string BgmTitle = "TITLE"; // 타이틀 배경음
        public const string BgmVillage = "VILLAGE"; // 마을·평상시 배경음
        public const string BgmBattle = "BATTLE"; // 전투 배경음
        public const string BgmBoss = "BOSS"; // 보스전 배경음 (Day88)
        public const string BgmVictory = "VICTORY"; // 전투 승리 배경음 (Day88 — 결과창이 떠 있는 동안)
        public const string BgmEnding = "ENDING"; // 엔딩 배경음 (Day88)

        public const string SfxClick = "UI_CLICK"; // 버튼 누름
        public const string SfxConfirm = "UI_CONFIRM"; // 확인
        public const string SfxCancel = "UI_CANCEL"; // 취소·닫기
        public const string SfxPage = "UI_PAGE"; // 화면·페이지 넘김
        public const string SfxError = "UI_ERROR"; // 할 수 없음 · 실패 (Day88)
        public const string SfxGold = "GET_GOLD"; // 골드 획득
        public const string SfxItem = "GET_ITEM"; // 아이템 획득
        public const string SfxLevelUp = "LEVEL_UP"; // 레벨 업
        public const string SfxDialogueNext = "DIALOGUE_NEXT"; // 대사 넘김 (Day88)
        public const string SfxHit = "BATTLE_HIT"; // 타격
        public const string SfxSkill = "BATTLE_SKILL"; // 스킬 사용 (Day88)
        public const string SfxUltimate = "BATTLE_ULTIMATE"; // 궁극기 발동 (Day88)
        public const string SfxHeal = "BATTLE_HEAL"; // 회복 (Day88)
        public const string SfxDown = "BATTLE_DOWN"; // 쓰러짐 (Day88)
        public const string SfxDisarray = "BATTLE_DISARRAY"; // 흐트러짐 발생 (Day88)
        public const string SfxBreak = "BATTLE_BREAK"; // 의상 파괴 (Day88)
        public const string SfxPerfect = "BATTLE_PERFECT"; // Perfect 판정
        public const string SfxGood = "BATTLE_GOOD"; // Good 판정 (Day88)
        public const string SfxMiss = "BATTLE_MISS"; // Miss 판정 (Day88)
        public const string SfxVictory = "RESULT_VICTORY"; // 전투 승리 (Day88)
        public const string SfxDefeat = "RESULT_DEFEAT"; // 전투 패배 (Day88)
        public const string SfxEnhanceSuccess = "ENHANCE_SUCCESS"; // 강화 · 초월 성공 (Day88)
        public const string SfxEnhanceFail = "ENHANCE_FAIL"; // 강화 실패 (Day88)

        public const float DefaultSfxInterval = 0.05f; // 같은 효과음이 다시 날 수 있는 최소 간격 (초) — 광역 공격이 네 번 겹쳐 시끄러워지지 않게

        public static readonly string[] AllBgm = { BgmTitle, BgmVillage, BgmBattle, BgmBoss, BgmVictory, BgmEnding }; // 배경음 전체 (파일 점검 · 테스트용)
        public static readonly string[] AllSfx = // 효과음 전체 (파일 점검 · 테스트용)
        {
            SfxClick, SfxConfirm, SfxCancel, SfxPage, SfxError, SfxGold, SfxItem, SfxLevelUp, SfxDialogueNext, // 화면
            SfxHit, SfxSkill, SfxUltimate, SfxHeal, SfxDown, SfxDisarray, SfxBreak, SfxPerfect, SfxGood, SfxMiss, // 전투
            SfxVictory, SfxDefeat, SfxEnhanceSuccess, SfxEnhanceFail // 결과
        };

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

        public static string GetBgmFallback(string key) // 그 곡의 파일이 없을 때 대신 틀 곡 (Day88 추가 — 없으면 빈 값)
        {
            switch (key) // 곡 분기
            {
                case BgmBoss: // 보스전
                    return BgmBattle; // 전투 곡
                case BgmVictory: // 승리
                    return BgmVillage; // 평상시 곡
                case BgmEnding: // 엔딩
                    return BgmTitle; // 타이틀 곡
                default: // 그 밖의 곡
                    return string.Empty; // 대신할 곡 없음
            }
        }

        public static float GetSfxVolume(string key) // 효과음별 음량 배율 (Day88 추가 — 자주 나는 소리는 작게)
        {
            switch (key) // 소리 분기
            {
                case SfxHit: // 타격 (전투 내내 난다)
                    return 0.65f; // 작게
                case SfxDialogueNext: // 대사 넘김 (계속 누른다)
                    return 0.5f; // 더 작게
                case SfxClick: // 버튼
                case SfxPage: // 페이지 넘김
                    return 0.8f; // 조금 작게
                default: // 그 밖의 소리
                    return 1f; // 그대로
            }
        }

        public static float GetSfxInterval(string key) // 같은 효과음이 다시 날 수 있는 최소 간격 (Day88 추가)
        {
            return key == SfxHit ? 0.08f : DefaultSfxInterval; // 타격은 조금 더 띄운다 (연타 스킬이 따발총처럼 들리지 않게)
        }
    }
}
