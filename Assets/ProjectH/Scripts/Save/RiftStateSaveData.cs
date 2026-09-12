using System; // 숫자 범위 기능
using UnityEngine; // 직렬화 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    [Serializable] // 저장 직렬화 허용
    public sealed class RiftStateSaveData // 검은 균열(긴급 던전) 상태 (Day67 신규 — 며칠마다 한 지역에 열리고 기한이 지나면 사라짐)
    {
        [SerializeField] private string regionId = string.Empty; // 균열이 열린 지역 (빈 값 = 열린 균열 없음)
        [SerializeField] private string dungeonId = string.Empty; // 긴급 던전 ID
        [SerializeField] private int startDay; // 열린 일차
        [SerializeField] private int endDay; // 마지막 도전 가능 일차 (이 날이 지나면 실패)
        [SerializeField] private bool cleared; // 이번 균열을 막았는지
        [SerializeField] private int lastSpawnDay; // 마지막으로 균열이 열린 일차 (다음 주기 계산용)
        [SerializeField] private int clearedCount; // 지금까지 막은 균열 수 (길드 의뢰·기록용)

        public string RegionId => regionId ?? string.Empty; // 지역 반환
        public string DungeonId => dungeonId ?? string.Empty; // 던전 반환
        public int StartDay => Math.Max(0, startDay); // 열린 일차 반환
        public int EndDay => Math.Max(0, endDay); // 기한 반환
        public bool Cleared => cleared; // 막았는지 반환
        public int LastSpawnDay => Math.Max(0, lastSpawnDay); // 마지막 발생 일차 반환
        public int ClearedCount => Math.Max(0, clearedCount); // 막은 균열 수 반환
        public bool IsOpen => !string.IsNullOrWhiteSpace(RegionId); // 현재 열린 균열 여부

        public void Open(string riftRegionId, string riftDungeonId, int day, int durationDays) // 새 균열 기록
        {
            regionId = riftRegionId ?? string.Empty; // 지역 저장
            dungeonId = riftDungeonId ?? string.Empty; // 던전 저장
            startDay = Math.Max(1, day); // 시작 일차 저장
            endDay = startDay + Math.Max(0, durationDays - 1); // 기한 저장 (당일 포함)
            cleared = false; // 아직 막지 않음
            lastSpawnDay = startDay; // 주기 기준 저장
        }

        public void MarkCleared() // 균열 막음 기록
        {
            if (cleared) return; // 이미 막음
            cleared = true; // 막음 저장
            clearedCount++; // 누적 수 증가
        }

        public void Close() // 균열 닫기 (막았거나 기한이 지남)
        {
            regionId = string.Empty; // 지역 비움
            dungeonId = string.Empty; // 던전 비움
            startDay = 0; // 시작 초기화
            endDay = 0; // 기한 초기화
            cleared = false; // 상태 초기화
        }

        public void EnsureDefaults() // 이전 저장 기본값 복원
        {
            if (regionId == null) regionId = string.Empty; // 지역 복원
            if (dungeonId == null) dungeonId = string.Empty; // 던전 복원
            startDay = Math.Max(0, startDay); // 시작 보정
            endDay = Math.Max(0, endDay); // 기한 보정
            lastSpawnDay = Math.Max(0, lastSpawnDay); // 주기 보정
            clearedCount = Math.Max(0, clearedCount); // 누적 보정
        }
    }
}
