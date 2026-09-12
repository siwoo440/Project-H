using System; // 문자열 비교·난수 기능
using System.Collections.Generic; // 목록 자료형
using ProjectH.Battle; // 던전 진행·클리어 기록 기능
using ProjectH.SaveSystem; // 저장·침식도 기능

namespace ProjectH.Dungeon // 프로젝트 던전 탐험 영역
{
    public static class RiftRunState // 이번 탐험이 긴급 균열인지 (Day67 신규 — 씬을 넘어가도 유지되는 정적 상태)
    {
        public static bool IsActive { get; private set; } // 균열 탐험 여부
        public static string RegionId { get; private set; } = string.Empty; // 균열 지역
        public static string DungeonId { get; private set; } = string.Empty; // 균열 던전

        public static void Begin(string regionId, string dungeonId) // 균열 탐험 시작
        {
            IsActive = true; // 활성
            RegionId = regionId ?? string.Empty; // 지역 저장
            DungeonId = dungeonId ?? string.Empty; // 던전 저장
        }

        public static void End() // 균열 탐험 종료 (일반 탐험 시작·균열 처리 완료)
        {
            IsActive = false; // 비활성
            RegionId = string.Empty; // 지역 비움
            DungeonId = string.Empty; // 던전 비움
        }
    }

    public static class RiftService // 검은 균열 · 긴급 던전 (Day67 신규 — 며칠마다 한 지역에 열리고, 기한 안에 막으면 침식도가 내려간다)
    {
        public const int CycleDays = 3; // 균열 발생 주기 (일)
        public const int DurationDays = 3; // 균열 유지 기간 (당일 포함)
        public const int ClearErosionRelief = 20; // 막았을 때 침식도 감소량
        public const int MissErosionPenalty = 10; // 놓쳤을 때 침식도 증가량
        public const float RewardMultiplier = 1.5f; // 긴급 던전 골드 배율
        public const string UnlockDungeonId = "DG004"; // 마왕성(심연의 관문)을 클리어하면 균열이 나타나기 시작

        public static bool IsUnlocked(SaveData saveData) => DungeonProgressSaveAdapter.IsCleared(saveData, UnlockDungeonId); // 균열 기능 해금 여부

        public static bool IsOpen(SaveData saveData) => saveData != null && saveData.RiftState.IsOpen && !saveData.RiftState.Cleared; // 지금 막을 수 있는 균열이 있는지

        public static bool IsRiftRegion(SaveData saveData, AdventureRegion region) => region != null && IsOpen(saveData) && saveData.RiftState.RegionId == region.Id; // 이 지역에 균열이 열렸는지

        public static int GetRemainingDays(SaveData saveData) // 남은 도전 일수 (오늘 포함)
        {
            if (!IsOpen(saveData)) return 0; // 균열 없음
            return Math.Max(0, saveData.RiftState.EndDay - saveData.CurrentDay + 1); // 남은 일수
        }

        public static string Refresh(SaveData saveData) // 기한 만료 처리 + 주기마다 새 균열 (화면을 열 때마다 호출, 안내 문구 반환)
        {
            if (saveData == null || !IsUnlocked(saveData)) return string.Empty; // 아직 균열이 없는 단계
            RiftStateSaveData state = saveData.RiftState; // 균열 상태
            int day = saveData.CurrentDay; // 오늘
            string message = string.Empty; // 안내

            if (state.IsOpen && (state.Cleared || day > state.EndDay)) // 막았거나 기한이 지남
            {
                if (!state.Cleared) // 놓친 경우
                {
                    AdventureRegion missed = AdventureRegionCatalog.Get(state.RegionId); // 지역
                    RegionErosionService.AddErosion(saveData, missed == null ? string.Empty : missed.ErosionRegionId, MissErosionPenalty); // 침식도 증가
                    message = $"{(missed == null ? "어느 지역" : missed.Name)}의 균열을 막지 못했습니다. 침식도 +{MissErosionPenalty}"; // 안내
                }

                state.Close(); // 균열 닫기
                if (!string.IsNullOrEmpty(message)) return message; // 실패 안내를 먼저 보여 주고, 새 균열은 다음에 열림 (Day68 수정)
            }

            if (!state.IsOpen && day - state.LastSpawnDay >= CycleDays) // 주기 도달
            {
                AdventureRegion target = PickRegion(saveData, day); // 이번 균열 지역
                string dungeonId = target == null ? string.Empty : GetRiftDungeonId(saveData, target); // 긴급 던전

                if (target != null && !string.IsNullOrEmpty(dungeonId)) // 대상 확인
                {
                    state.Open(target.Id, dungeonId, day, DurationDays); // 균열 열기
                    message = $"{target.Name}에 검은 균열이 열렸습니다! {DurationDays}일 안에 막으세요."; // 안내
                }
            }

            return message; // 안내 반환
        }

        public static AdventureRegion PickRegion(SaveData saveData, int day) // 균열이 열릴 지역 (같은 날이면 항상 같은 곳)
        {
            List<AdventureRegion> candidates = new List<AdventureRegion>(); // 후보 지역

            foreach (AdventureRegion region in AdventureRegionCatalog.All) // 지역 순회
            {
                if (region.Kind == AdventureRegionKind.Dungeon && !string.IsNullOrEmpty(GetRiftDungeonId(saveData, region))) candidates.Add(region); // 열린 던전이 있는 지역
            }

            if (candidates.Count == 0) return null; // 후보 없음
            return candidates[new Random(StableHash($"RIFT|{day}")).Next(candidates.Count)]; // 결정적 선택
        }

        public static string GetRiftDungeonId(SaveData saveData, AdventureRegion region) // 그 지역에서 균열이 열리는 던전 (열린 던전 중 가장 어려운 곳)
        {
            string result = string.Empty; // 결과

            if (region != null) // 지역 확인
            {
                foreach (string dungeonId in region.DungeonIds) // 쉬운 순서대로
                {
                    if (DungeonProgressionPolicy.IsUnlocked(saveData, dungeonId)) result = dungeonId; // 열린 던전 갱신
                }
            }

            return result; // 결과 반환
        }

        public static bool BeginRun(SaveData saveData) // 긴급 균열 탐험 시작 표시
        {
            if (!IsOpen(saveData)) return false; // 균열 없음
            RiftRunState.Begin(saveData.RiftState.RegionId, saveData.RiftState.DungeonId); // 균열 탐험 기록
            return true; // 시작
        }

        public static string CompleteRun(SaveData saveData, string dungeonId) // 긴급 던전 클리어 반영 (침식도 감소)
        {
            if (saveData == null || !RiftRunState.IsActive || !IsOpen(saveData) || saveData.RiftState.DungeonId != dungeonId) return string.Empty; // 균열 클리어가 아님
            AdventureRegion region = AdventureRegionCatalog.Get(saveData.RiftState.RegionId); // 지역
            saveData.RiftState.MarkCleared(); // 막음 기록
            RegionErosionService.AddErosion(saveData, region == null ? string.Empty : region.ErosionRegionId, -ClearErosionRelief); // 침식도 감소
            RiftRunState.End(); // 균열 탐험 종료
            return $"{(region == null ? "지역" : region.Name)}의 검은 균열을 막았습니다! 침식도 -{ClearErosionRelief}"; // 안내
        }

        public static int ApplyRewardBonus(int gold) => RiftRunState.IsActive ? (int)Math.Round(gold * RewardMultiplier) : gold; // 긴급 던전 골드 보너스

        private static int StableHash(string value) // 실행 환경과 무관한 문자열 해시 (FNV-1a)
        {
            unchecked // 오버플로 허용
            {
                int hash = (int)2166136261; // 시작값

                for (int index = 0; index < value.Length; index++) // 문자 순회
                {
                    hash = (hash ^ value[index]) * 16777619; // 해시 갱신
                }

                return hash & 0x7fffffff; // 양수 반환
            }
        }
    }
}
