using System; // 수학 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public enum NpcLineKind // NPC 대사 상황 (Day61 신규)
    {
        Greeting = 0, // 입장 인사
        Describe = 1, // 상품 설명 앞말
        Buy = 2, // 구매 성공
        SoldOut = 3, // 품절
        NoGold = 4, // 골드 부족
        Reroll = 5, // 오늘의 상품 새로고침
        Sell = 6, // 판매
        EnhanceSuccess = 7, // 강화 성공
        EnhanceFail = 8, // 강화 실패
        TranscendSuccess = 9, // 초월 성공
        NeedMaterial = 10, // 재료 부족
        SelectEquipment = 11 // 장비 선택 안내
    }

    public sealed class NpcProfile // NPC 이름·소속 (Day61 신규 — Day62 대화 UI 이름/소속 표시와 공유)
    {
        public string Id { get; } // 스탠딩 ID (Resources/Dialogues/Standing/{Id})
        public string Name { get; } // 이름
        public string Title { get; } // 소속·직업

        public NpcProfile(string id, string name, string title) // 프로필 생성
        {
            Id = id; // ID 저장
            Name = name; // 이름 저장
            Title = title; // 소속 저장
        }
    }

    public static class NpcLineCatalog // 상점·대장간 NPC 대사 (Day61 신규 — 이름·대사는 이 파일에서 수정)
    {
        public static readonly NpcProfile Shopkeeper = new NpcProfile("NPC_SHOPKEEPER", "로웰", "잡화점 주인"); // 상점 주인 (임시 이름)
        public static readonly NpcProfile Blacksmith = new NpcProfile("NPC_BLACKSMITH", "브론", "대장장이"); // 대장장이 (임시 이름)
        public static readonly NpcProfile Shadow = new NpcProfile("NPC_SHADOW", "???", "이름 없는 그림자"); // 챕터 5 네메시스 예고 (Day69 추가)
        public static readonly NpcProfile Archai = new NpcProfile("NPC_ARCHAI", ProjectH.Story.FinaleCatalog.ArchaiName, "지워진 이름"); // 챕터 6에서 정체가 드러난 고대 존재 (Day71 추가)

        private static readonly string[][] ShopLines = // 상점 주인 대사 (NpcLineKind 순서)
        {
            new[] { "어서 오세요! 오늘도 좋은 물건 많이 들어왔답니다.", "오셨군요. 천천히 둘러보세요.", "오늘의 상품은 매일 바뀌니까 놓치지 마세요!" }, // 인사
            new[] { "그건요,", "좋은 눈이시네요.", "아, 그 물건은요." }, // 설명 앞말
            new[] { "감사합니다! 또 오세요.", "좋은 선택이에요.", "잘 쓰시길 바랄게요!" }, // 구매
            new[] { "죄송해요, 오늘 물량은 다 나갔어요. 내일 다시 들어와요.", "아쉽게도 품절이에요." }, // 품절
            new[] { "음… 골드가 조금 모자라신 것 같아요.", "외상은 안 돼요!" }, // 골드 부족
            new[] { "창고에서 다른 물건을 꺼내 올게요!", "자, 새로 진열했어요." }, // 새로고침
            new[] { "좋아요, 제가 사들일게요.", "상태가 좋네요. 여기 골드요." }, // 판매
            new[] { string.Empty }, // 강화 성공 (대장장이 전용)
            new[] { string.Empty }, // 강화 실패 (대장장이 전용)
            new[] { string.Empty }, // 초월 성공 (대장장이 전용)
            new[] { "재료가 부족하네요." }, // 재료 부족
            new[] { "강화는 로비에서 대장간의 브론 씨를 찾아가세요." } // 장비 안내
        };

        private static readonly string[][] SmithLines = // 대장장이 대사 (NpcLineKind 순서)
        {
            new[] { "왔나. 두드릴 물건이 있으면 올려놔.", "불은 늘 달궈져 있다. 뭘 강화할 거지?", "장비는 쓰는 사람을 닮는 법이지." }, // 인사
            new[] { "그건", "흠," }, // 설명 앞말
            new[] { string.Empty }, // 구매 (상점 전용)
            new[] { string.Empty }, // 품절 (상점 전용)
            new[] { "골드가 모자라. 쇠는 공짜로 달궈지지 않아.", "돈부터 챙겨 와." }, // 골드 부족
            new[] { string.Empty }, // 새로고침 (상점 전용)
            new[] { string.Empty }, // 판매 (상점 전용)
            new[] { "좋아, 잘 먹혔군!", "훌륭해. 날이 한층 살아났어.", "이 정도면 어디 내놔도 부끄럽지 않지." }, // 강화 성공
            new[] { "칫… 주문서만 타 버렸군. 장비는 멀쩡하니 걱정 마.", "이번엔 불이 안 따라줬어. 다음엔 더 잘 붙을 거다." }, // 강화 실패
            new[] { "…이건 내 평생의 역작 중 하나로 남겠군.", "별이 하나 더 새겨졌다. 전혀 다른 물건이 됐어." }, // 초월 성공
            new[] { "재료가 모자라. 주문서나 같은 장비를 더 구해 와.", "이걸로는 부족해." }, // 재료 부족
            new[] { "강화할 장비를 골라 봐.", "어떤 걸 손볼까?" } // 장비 선택
        };

        public static string GetLine(NpcProfile npc, NpcLineKind kind, int seed) // 상황별 대사 (seed로 변주)
        {
            string[][] table = npc == Blacksmith ? SmithLines : ShopLines; // NPC별 대사 표
            string[] lines = table[Math.Max(0, Math.Min(table.Length - 1, (int)kind))]; // 상황 대사
            return lines[Math.Abs(seed) % lines.Length]; // 변주 선택
        }

        public static string FormatSpeaker(NpcProfile npc) => $"{npc.Name}  ·  {npc.Title}"; // 이름표 문구

        public static NpcProfile Find(string id) => id == Shopkeeper.Id ? Shopkeeper : id == Blacksmith.Id ? Blacksmith : id == Shadow.Id ? Shadow : id == Archai.Id ? Archai : null; // ID로 NPC 조회 (Day62 추가 — 대화 이름표용, 없으면 null)
    }
}
