namespace ProjectH.Core // 프로젝트 핵심 영역
{
    public static class GameScenes // 공통 씬 이름
    {
        public const string Bootstrap = "Bootstrap"; // 부트스트랩 씬
        public const string Title = "Title"; // 타이틀 씬
        public const string Lobby = "Lobby"; // 로비 씬
        public const string Character = "Character"; // 캐릭터 상세 씬
        public const string Bag = "Bag"; // 가방 씬
        public const string Shop = "Shop"; // 상점 씬
        public const string Blacksmith = "Blacksmith"; // 대장간 씬 (Day61 추가, 로비 하단 버튼으로 진입)
        public const string Village = "Village"; // 마을 씬 (Day62 추가, 로비 하단 버튼으로 진입)
        public const string Diary = "Diary"; // 일기장 씬 (Day63 추가, 타이틀·로비 [일기장]으로 진입)
        public const string Party = "Party"; // 파티 씬
        public const string DungeonSelect = "DungeonSelect"; // 던전 선택 씬
        public const string Battle = "Battle"; // 전투 씬
        public const string Result = "Result"; // 결과 씬
    }
}
