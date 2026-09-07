using System; // 직렬화 기능
using ProjectH.Data; // 장비 슬롯 데이터 기능
using UnityEngine; // Unity 직렬화 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    [Serializable] // JSON 직렬화 허용
    public sealed class CharacterEquipmentSaveData // 캐릭터 장착 장비 저장 데이터
    {
        [SerializeField] private string weaponInstanceId = string.Empty; // 무기 인스턴스 ID
        [SerializeField] private string armorInstanceId = string.Empty; // 방어구 인스턴스 ID

        public string WeaponInstanceId => weaponInstanceId ?? string.Empty; // 무기 인스턴스 ID 반환
        public string ArmorInstanceId => armorInstanceId ?? string.Empty; // 방어구 인스턴스 ID 반환

        public void EnsureDefaults() // 장착 장비 기본값 복원
        {
            if (weaponInstanceId == null) // 무기 인스턴스 null 확인
            {
                weaponInstanceId = string.Empty; // 무기 인스턴스 기본값 복원
            }

            if (armorInstanceId == null) // 방어구 인스턴스 null 확인
            {
                armorInstanceId = string.Empty; // 방어구 인스턴스 기본값 복원
            }
        }

        public string GetInstanceId(EquipmentSlot slot) // 슬롯별 장착 장비 ID 조회
        {
            EnsureDefaults(); // 장착 장비 기본값 확인

            switch (slot) // 장비 슬롯 분기
            {
                case EquipmentSlot.Weapon: // 무기 슬롯 처리
                    return weaponInstanceId; // 무기 인스턴스 반환
                case EquipmentSlot.Armor: // 방어구 슬롯 처리
                    return armorInstanceId; // 방어구 인스턴스 반환
                default: // 미지원 슬롯 처리
                    return string.Empty; // 빈 장비 인스턴스 반환
            }
        }

        public void SetInstanceId(EquipmentSlot slot, string instanceId) // 슬롯별 장착 장비 ID 설정
        {
            EnsureDefaults(); // 장착 장비 기본값 확인
            string safeInstanceId = instanceId ?? string.Empty; // null 장비 인스턴스 보정

            switch (slot) // 장비 슬롯 분기
            {
                case EquipmentSlot.Weapon: // 무기 슬롯 처리
                    weaponInstanceId = safeInstanceId; // 무기 인스턴스 설정
                    break; // 무기 슬롯 분기 종료
                case EquipmentSlot.Armor: // 방어구 슬롯 처리
                    armorInstanceId = safeInstanceId; // 방어구 인스턴스 설정
                    break; // 방어구 슬롯 분기 종료
            }
        }

        public bool ClearSlot(EquipmentSlot slot, out string removedInstanceId) // 슬롯 장비 해제
        {
            removedInstanceId = GetInstanceId(slot); // 기존 장비 인스턴스 조회

            if (string.IsNullOrWhiteSpace(removedInstanceId)) // 기존 장비 존재 확인
            {
                removedInstanceId = string.Empty; // 해제 결과 기본값 적용
                return false; // 빈 슬롯 해제 실패
            }

            SetInstanceId(slot, string.Empty); // 대상 슬롯 장비 제거
            return true; // 장비 해제 성공
        }

        public void ClearInstance(string instanceId) // 특정 장비 인스턴스 장착 해제
        {
            EnsureDefaults(); // 장착 장비 기본값 확인

            if (string.IsNullOrWhiteSpace(instanceId)) // 대상 장비 인스턴스 확인
            {
                return; // 빈 장비 인스턴스 해제 중단
            }

            if (string.Equals(weaponInstanceId, instanceId, StringComparison.Ordinal)) // 무기 장착 여부 확인
            {
                weaponInstanceId = string.Empty; // 무기 슬롯 해제
            }

            if (string.Equals(armorInstanceId, instanceId, StringComparison.Ordinal)) // 방어구 장착 여부 확인
            {
                armorInstanceId = string.Empty; // 방어구 슬롯 해제
            }
        }

        public bool ContainsInstance(string instanceId) // 특정 장비 착용 여부 확인
        {
            EnsureDefaults(); // 장착 장비 기본값 확인

            if (string.IsNullOrWhiteSpace(instanceId)) // 대상 장비 인스턴스 확인
            {
                return false; // 빈 장비 인스턴스 미착용 반환
            }

            return string.Equals(weaponInstanceId, instanceId, StringComparison.Ordinal) || string.Equals(armorInstanceId, instanceId, StringComparison.Ordinal); // 무기 또는 방어구 착용 결과 반환
        }
    }
}
