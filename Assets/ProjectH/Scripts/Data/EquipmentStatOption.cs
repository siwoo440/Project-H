using System; // 직렬화 기능
using UnityEngine; // Unity 직렬화 기능

namespace ProjectH.Data // 프로젝트 데이터 네임스페이스
{
    [Serializable] // Unity 직렬화 허용
    public struct EquipmentStatOption // 장비 개별 능력치 옵션
    {
        [SerializeField] private EquipmentStatType statType; // 적용 능력치 종류
        [SerializeField] private float value; // 적용 능력치 수치

        public EquipmentStatType StatType => statType; // 능력치 종류 반환
        public float Value => value; // 능력치 수치 반환

        public EquipmentStatOption(EquipmentStatType statType, float value) // 장비 옵션 생성
        {
            this.statType = statType; // 능력치 종류 저장
            this.value = value; // 능력치 수치 저장
        }
    }
}
