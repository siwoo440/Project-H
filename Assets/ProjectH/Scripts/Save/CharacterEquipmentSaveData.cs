using System; // 직렬화 기능
using ProjectH.Data; // 장비 슬롯 데이터 기능
using UnityEngine; // Unity 직렬화 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    [Serializable] // JSON 직렬화 허용
    public sealed class CharacterEquipmentSaveData // 캐릭터 장착 장비 저장 데이터 (Day60 투구·장갑·신발 추가로 5칸)
    {
        [SerializeField] private string weaponInstanceId = string.Empty; // 무기 인스턴스 ID
        [SerializeField] private string armorInstanceId = string.Empty; // 방어구 인스턴스 ID
        [SerializeField] private string helmetInstanceId = string.Empty; // 투구 인스턴스 ID (Day60 추가)
        [SerializeField] private string glovesInstanceId = string.Empty; // 장갑 인스턴스 ID (Day60 추가)
        [SerializeField] private string bootsInstanceId = string.Empty; // 신발 인스턴스 ID (Day60 추가)

        public string WeaponInstanceId => weaponInstanceId ?? string.Empty; // 무기 인스턴스 ID 반환
        public string ArmorInstanceId => armorInstanceId ?? string.Empty; // 방어구 인스턴스 ID 반환

        public void EnsureDefaults() // 장착 장비 기본값 복원 (기존 세이브의 새 칸은 빈 칸으로 시작)
        {
            weaponInstanceId = weaponInstanceId ?? string.Empty; // 무기 기본값 복원
            armorInstanceId = armorInstanceId ?? string.Empty; // 방어구 기본값 복원
            helmetInstanceId = helmetInstanceId ?? string.Empty; // 투구 기본값 복원
            glovesInstanceId = glovesInstanceId ?? string.Empty; // 장갑 기본값 복원
            bootsInstanceId = bootsInstanceId ?? string.Empty; // 신발 기본값 복원
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
                case EquipmentSlot.Helmet: // 투구 슬롯 처리
                    return helmetInstanceId; // 투구 인스턴스 반환
                case EquipmentSlot.Gloves: // 장갑 슬롯 처리
                    return glovesInstanceId; // 장갑 인스턴스 반환
                case EquipmentSlot.Boots: // 신발 슬롯 처리
                    return bootsInstanceId; // 신발 인스턴스 반환
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
                    break; // 분기 종료
                case EquipmentSlot.Armor: // 방어구 슬롯 처리
                    armorInstanceId = safeInstanceId; // 방어구 인스턴스 설정
                    break; // 분기 종료
                case EquipmentSlot.Helmet: // 투구 슬롯 처리
                    helmetInstanceId = safeInstanceId; // 투구 인스턴스 설정
                    break; // 분기 종료
                case EquipmentSlot.Gloves: // 장갑 슬롯 처리
                    glovesInstanceId = safeInstanceId; // 장갑 인스턴스 설정
                    break; // 분기 종료
                case EquipmentSlot.Boots: // 신발 슬롯 처리
                    bootsInstanceId = safeInstanceId; // 신발 인스턴스 설정
                    break; // 분기 종료
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

        public void ClearInstance(string instanceId) // 특정 장비 인스턴스 장착 해제 (5칸 전체 확인)
        {
            if (string.IsNullOrWhiteSpace(instanceId)) // 대상 장비 인스턴스 확인
            {
                return; // 빈 장비 인스턴스 해제 중단
            }

            foreach (EquipmentSlot slot in EquipmentSlotInfo.All) // 슬롯 순회
            {
                if (string.Equals(GetInstanceId(slot), instanceId, StringComparison.Ordinal)) // 장착 여부 확인
                {
                    SetInstanceId(slot, string.Empty); // 슬롯 해제
                }
            }
        }

        public bool ContainsInstance(string instanceId) // 특정 장비 착용 여부 확인 (5칸 전체 확인)
        {
            if (string.IsNullOrWhiteSpace(instanceId)) // 대상 장비 인스턴스 확인
            {
                return false; // 빈 장비 인스턴스 미착용 반환
            }

            foreach (EquipmentSlot slot in EquipmentSlotInfo.All) // 슬롯 순회
            {
                if (string.Equals(GetInstanceId(slot), instanceId, StringComparison.Ordinal)) // 장착 여부 확인
                {
                    return true; // 착용 중 반환
                }
            }

            return false; // 미착용 반환
        }
    }
}
