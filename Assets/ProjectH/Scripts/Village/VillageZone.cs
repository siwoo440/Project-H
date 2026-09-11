using System.Collections.Generic; // 목록 자료형
using ProjectH.SaveSystem; // 시간대 기능

namespace ProjectH.Village // 프로젝트 마을 영역 (Day62 신규)
{
    public enum VillageZone // 마을 구역 5곳 (저장·가중치 순서이므로 순서 변경 금지)
    {
        Plaza = 0, // 광장
        Market = 1, // 시장
        Onsen = 2, // 온천
        Inn = 3, // 여관
        Guild = 4 // 길드
    }

    public sealed class VillageZoneInfo // 구역 표시 정보
    {
        public VillageZone Zone { get; } // 구역
        public string Name { get; } // 이름
        public string BackgroundKey { get; } // 배경 키 (DialogueArtFactory)
        public string Description { get; } // 짧은 설명

        public VillageZoneInfo(VillageZone zone, string name, string backgroundKey, string description) // 정보 생성
        {
            Zone = zone; // 구역 저장
            Name = name; // 이름 저장
            BackgroundKey = backgroundKey; // 배경 저장
            Description = description; // 설명 저장
        }
    }

    public static class VillageZoneCatalog // 구역 이름·배경·운영 시간 (Day62 신규 — 기획서 9장 마을)
    {
        private static readonly VillageZoneInfo[] Zones = // 구역 표 (VillageZone 순서)
        {
            new VillageZoneInfo(VillageZone.Plaza, "광장", "VILLAGE_PLAZA", "분수대가 있는 마을 한가운데. 산책하는 사람들로 늘 붐빈다."), // 광장
            new VillageZoneInfo(VillageZone.Market, "시장", "VILLAGE_MARKET", "호감도 선물과 이벤트 물건을 파는 노점 거리. 상점·대장간으로 이어진다."), // 시장
            new VillageZoneInfo(VillageZone.Onsen, "온천", "VILLAGE_ONSEN", "김이 오르는 노천 온천. 몸을 담그면 활력이 돌아온다."), // 온천
            new VillageZoneInfo(VillageZone.Inn, "여관", "VILLAGE_INN", "모험가들이 묵는 여관. 저녁부터는 방을 잡고 쉴 수 있다."), // 여관
            new VillageZoneInfo(VillageZone.Guild, "길드", "VILLAGE_GUILD", "의뢰 게시판과 던전 정보가 모이는 모험가 길드.") // 길드
        };

        public static IReadOnlyList<VillageZoneInfo> All => Zones; // 전체 구역

        public static VillageZoneInfo Get(VillageZone zone) => Zones[(int)zone]; // 구역 정보 (enum 순서 = 배열 순서)

        public static bool IsOpen(VillageZone zone, SaveTimeOfDay phase) => !(zone == VillageZone.Market && phase == SaveTimeOfDay.Night); // 운영 여부 (시장만 밤에 닫힘)
    }
}
