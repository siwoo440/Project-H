using System; // 날짜 자료형

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    public sealed class SaveSlotInfo // 저장 슬롯 한 칸의 요약 (Day72 신규 — 목록 표시용, 전체를 불러오지 않는다)
    {
        public int Slot { get; } // 슬롯 번호 (0부터)
        public bool Exists { get; } // 저장이 들어 있는지
        public DateTime SavedAt { get; } // 저장 시각
        public int Day { get; } // 진행 일차
        public string TimeLabel { get; } // 시간대 (아침 · 점심 · 저녁 · 밤)
        public string Chapter { get; } // 진행 중인 챕터
        public string HeroName { get; } // 주인공 이름
        public int PartyCount { get; } // 동료 수
        public int TopLevel { get; } // 가장 높은 레벨

        public SaveSlotInfo(int slot) // 빈 슬롯 생성
        {
            Slot = slot; // 번호 저장
            Exists = false; // 비어 있음
            TimeLabel = string.Empty; // 빈 값
            Chapter = string.Empty; // 빈 값
            HeroName = string.Empty; // 빈 값
        }

        public SaveSlotInfo(int slot, DateTime savedAt, int day, string timeLabel, string chapter, string heroName, int partyCount, int topLevel) // 저장이 있는 슬롯 생성
        {
            Slot = slot; // 번호 저장
            Exists = true; // 저장 있음
            SavedAt = savedAt; // 시각 저장
            Day = Math.Max(1, day); // 일차 저장
            TimeLabel = timeLabel ?? string.Empty; // 시간대 저장
            Chapter = chapter ?? string.Empty; // 챕터 저장
            HeroName = heroName ?? string.Empty; // 이름 저장
            PartyCount = Math.Max(0, partyCount); // 동료 수 저장
            TopLevel = Math.Max(1, topLevel); // 레벨 저장
        }

        public string GetDateText() // 날짜·시간 문구 (예: 2026-09-12 14:03)
        {
            return Exists && SavedAt > DateTime.MinValue ? SavedAt.ToString("yyyy-MM-dd HH:mm") : "기록 없음"; // 문구 반환
        }

        public string GetSummaryText() // 진행 요약 문구 (예: 용사 · 12일차 저녁 · 동료 5명 · 최고 Lv.14)
        {
            if (!Exists) return "비어 있음"; // 빈 슬롯
            string hero = string.IsNullOrEmpty(HeroName) ? "이름 미정" : HeroName; // 주인공 이름
            string phase = string.IsNullOrEmpty(TimeLabel) ? string.Empty : $" {TimeLabel}"; // 시간대
            return $"{hero} · {Day}일차{phase} · 동료 {PartyCount}명 · 최고 Lv.{TopLevel}"; // 문구 반환
        }
    }

    public static class SaveSlotCatalog // 저장 슬롯 구성 (Day72 신규 — 한 페이지에 가로 3 × 세로 2 = 6칸)
    {
        public const int Columns = 3; // 한 줄에 들어가는 칸 수
        public const int Rows = 2; // 한 페이지의 줄 수
        public const int SlotsPerPage = Columns * Rows; // 한 페이지 6칸
        public const int PageCount = 3; // 페이지 수
        public const int SlotCount = SlotsPerPage * PageCount; // 전체 18칸

        public static bool IsValidSlot(int slot) => slot >= 0 && slot < SlotCount; // 슬롯 번호 확인

        public static bool IsValidPage(int page) => page >= 0 && page < PageCount; // 페이지 번호 확인

        public static string GetFileName(int slot) => $"save_slot_{slot + 1:00}.json"; // 슬롯 파일 이름 (save_slot_01.json …)

        public static int GetSlotNumber(int slot) => slot + 1; // 화면에 보여 줄 칸 번호 (1부터)

        public static int GetPage(int slot) => slot / SlotsPerPage; // 슬롯이 속한 페이지

        public static int GetFirstSlotOfPage(int page) => page * SlotsPerPage; // 페이지의 첫 슬롯 번호

        public static int GetColumn(int slot) => (slot % SlotsPerPage) % Columns; // 페이지 안 가로 위치

        public static int GetRow(int slot) => (slot % SlotsPerPage) / Columns; // 페이지 안 세로 위치
    }
}
