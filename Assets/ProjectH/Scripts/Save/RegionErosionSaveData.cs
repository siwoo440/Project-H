using System; // 직렬화 기능
using UnityEngine; // Unity 기본 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역 (최적화 — SaveData.cs에서 분리)
{
    [Serializable] // JSON 직렬화 허용
    public sealed class RegionErosionSaveData // 지역별 침식도 저장 데이터 (Day44)
    {
        public const int MinErosion = 0; // 침식도 최소값
        public const int MaxErosion = 100; // 침식도 최대값
        [SerializeField] private string regionId; // 지역 ID
        [SerializeField] private int erosionLevel; // 지역 침식도 (0~100)
        public string RegionId => regionId; // 지역 ID 반환
        public int ErosionLevel => erosionLevel; // 지역 침식도 반환

        public RegionErosionSaveData(string id, int level) // 지역 침식도 저장 데이터 생성
        {
            regionId = id; // 지역 ID 저장
            erosionLevel = Mathf.Clamp(level, MinErosion, MaxErosion); // 침식도 범위 보정 후 저장
        }

        public void EnsureDefaults() // 지역 침식도 저장 기본값 복원
        {
            if (regionId == null) // 지역 ID null 확인
            {
                regionId = string.Empty; // 지역 ID 기본값 복원
            }

            erosionLevel = Mathf.Clamp(erosionLevel, MinErosion, MaxErosion); // 침식도 범위 보정
        }

        public void SetErosion(int value) // 지역 침식도 변경
        {
            erosionLevel = Mathf.Clamp(value, MinErosion, MaxErosion); // 침식도 범위 보정 후 저장
        }
    }
}
