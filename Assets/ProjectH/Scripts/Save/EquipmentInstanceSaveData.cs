using System; // 직렬화 기능
using UnityEngine; // Unity 직렬화 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    [Serializable] // JSON 직렬화 허용
    public sealed class EquipmentInstanceSaveData // 보유 장비 인스턴스 저장 데이터
    {
        [SerializeField] private string instanceId; // 장비 인스턴스 고유 ID
        [SerializeField] private string equipmentId; // 장비 원본 데이터 ID

        public string InstanceId => instanceId; // 장비 인스턴스 ID 반환
        public string EquipmentId => equipmentId; // 장비 원본 ID 반환

        public EquipmentInstanceSaveData(string newInstanceId, string newEquipmentId) // 장비 인스턴스 저장 데이터 생성
        {
            instanceId = newInstanceId ?? string.Empty; // 장비 인스턴스 ID 저장
            equipmentId = newEquipmentId ?? string.Empty; // 장비 원본 ID 저장
        }
    }
}
