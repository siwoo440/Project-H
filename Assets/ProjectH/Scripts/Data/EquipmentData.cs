using System.Collections.Generic; // 읽기 전용 목록 기능
using UnityEngine; // Unity 에셋 기능

namespace ProjectH.Data // 프로젝트 데이터 영역
{
    [CreateAssetMenu(fileName = "EquipmentData", menuName = "Project H/Data/Equipment")] // 장비 에셋 메뉴
    public sealed class EquipmentData : ScriptableObject, IDataRecord // 장비 원본 데이터
    {
        [SerializeField] private ItemData item; // 장비 기반 아이템 데이터
        [SerializeField] private EquipmentSlot slot = EquipmentSlot.Weapon; // 장비 착용 슬롯
        [SerializeField] private List<EquipmentStatOption> statOptions = new List<EquipmentStatOption>(); // 장비 능력치 옵션 목록

        public string Id => item != null ? item.Id : null; // 기반 아이템 ID 반환
        public ItemData Item => item; // 기반 아이템 반환
        public string DisplayName => item != null ? item.DisplayName : string.Empty; // 장비 표시 이름 반환
        public string Description => item != null ? item.Description : string.Empty; // 장비 설명 반환
        public ItemGrade Grade => item != null ? item.Grade : ItemGrade.Common; // 장비 등급 반환
        public EquipmentSlot Slot => slot; // 장비 슬롯 반환
        public IReadOnlyList<EquipmentStatOption> StatOptions => statOptions; // 장비 옵션 목록 반환

        public float GetStatValue(EquipmentStatType statType) // 특정 능력치 총합 조회
        {
            if (statOptions == null) // 옵션 목록 누락 확인
            {
                return 0f; // 옵션 없음 반환
            }

            float total = 0f; // 능력치 합계 초기화
            for (int index = 0; index < statOptions.Count; index++) // 장비 옵션 순회
            {
                EquipmentStatOption option = statOptions[index]; // 현재 옵션 조회
                if (option.StatType == statType) // 요청 능력치 일치 확인
                {
                    total += option.Value; // 일치 옵션 수치 누적
                }
            }

            return total; // 능력치 합계 반환
        }
    }
}
