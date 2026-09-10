using UnityEngine; // Unity 기본 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    public enum RegionErosionTier // 지역 침식도 5단계 등급 (0~100을 20%씩 구간 분할, Day44)
    {
        Stable = 0, // 0~19 : 안정
        Cracked = 1, // 20~39 : 균열
        Eroded = 2, // 40~59 : 침식
        Dangerous = 3, // 60~79 : 위험
        Collapsed = 4 // 80~100 : 붕괴
    }

    public static class RegionErosionService // 지역 침식도 공통 처리 기능 (Day44)
    {
        private const int TierRangeSize = 20; // 등급 구간 크기 (20%)

        private static readonly float[] EnemyStatMultipliers = // 등급별 적 스탯 배율 (Day44 임시 기본값, 추후 기획 수치로 조정)
        {
            1.00f, // Stable 배율
            1.10f, // Cracked 배율
            1.20f, // Eroded 배율
            1.35f, // Dangerous 배율
            1.50f // Collapsed 배율
        };

        public static int GetErosion(SaveData saveData, string regionId) // 현재 지역 침식도 조회
        {
            return saveData == null ? 0 : saveData.GetRegionErosion(regionId); // 저장 데이터 없음 시 0 반환
        }

        public static RegionErosionTier GetErosionTier(SaveData saveData, string regionId) // 현재 지역 침식도 등급 조회
        {
            return ResolveTier(GetErosion(saveData, regionId)); // 조회 침식도 기반 등급 반환
        }

        public static int AddErosion(SaveData saveData, string regionId, int delta) // 지역 침식도 증감 (음수 입력 시 감소)
        {
            if (saveData == null || string.IsNullOrWhiteSpace(regionId)) // 저장 데이터 및 지역 ID 확인
            {
                return 0; // 변경 없음 반환
            }

            int current = saveData.GetRegionErosion(regionId); // 현재 지역 침식도 조회
            return saveData.SetRegionErosion(regionId, current + delta); // 범위 보정 포함 침식도 변경 반환
        }

        public static RegionErosionTier ResolveTier(int erosion) // 침식도 수치를 등급으로 변환
        {
            int clamped = Mathf.Clamp(erosion, RegionErosionSaveData.MinErosion, RegionErosionSaveData.MaxErosion); // 등급 계산용 범위 보정
            int tierIndex = Mathf.Clamp(clamped / TierRangeSize, 0, 4); // 20% 단위 등급 인덱스 계산 (100은 4번 등급으로 보정)
            return (RegionErosionTier)tierIndex; // 등급 값 반환
        }

        public static float GetEnemyStatMultiplier(RegionErosionTier tier) // 등급 기반 적 스탯 배율 조회
        {
            int index = Mathf.Clamp((int)tier, 0, EnemyStatMultipliers.Length - 1); // 등급 인덱스 범위 보정
            return EnemyStatMultipliers[index]; // 등급별 적 스탯 배율 반환
        }

        public static float GetEnemyStatMultiplier(SaveData saveData, string regionId) // 지역 기반 적 스탯 배율 조회
        {
            return GetEnemyStatMultiplier(GetErosionTier(saveData, regionId)); // 지역 등급 기반 배율 반환
        }

        public static int GetEnemyStatBonusPercent(SaveData saveData, string regionId) // 지역 기반 적 스탯 증가율(%) 조회
        {
            float multiplier = GetEnemyStatMultiplier(saveData, regionId); // 지역 기반 적 스탯 배율 조회
            return Mathf.RoundToInt((multiplier - 1f) * 100f); // 배율을 증가율(%)로 변환
        }

        public static bool TryInitializeRandomErosion(SaveData saveData, string regionId, out int erosion) // 미등록 지역 침식도 랜덤 초기화 (Day44 추가 작업)
        {
            erosion = 0; // 결과 침식도 초기화

            if (saveData == null || string.IsNullOrWhiteSpace(regionId)) // 저장 데이터 및 지역 ID 확인
            {
                return false; // 초기화 실패 반환
            }

            if (saveData.HasRegionErosion(regionId)) // 기존 등록 지역 확인
            {
                erosion = saveData.GetRegionErosion(regionId); // 기존 침식도 조회
                return false; // 이미 등록된 지역이므로 새로 초기화하지 않음
            }

            int randomErosion = UnityEngine.Random.Range(RegionErosionSaveData.MinErosion, RegionErosionSaveData.MaxErosion + 1); // 0~100 랜덤 침식도 생성
            erosion = saveData.SetRegionErosion(regionId, randomErosion); // 랜덤 침식도 저장
            return true; // 신규 랜덤 초기화 성공 반환
        }

        public static int GetOrInitializeErosion(SaveData saveData, string regionId) // 지역 침식도 조회 (미등록 시 랜덤 초기화 포함, Day44 추가 작업)
        {
            TryInitializeRandomErosion(saveData, regionId, out int erosion); // 미등록 지역 랜덤 초기화 시도
            return erosion; // 조회 또는 초기화 침식도 반환
        }
    }
}
