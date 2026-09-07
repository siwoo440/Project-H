using System; // 열거형 및 문자열 비교 기능
using System.Collections.Generic; // 검증 오류 목록 기능

namespace ProjectH.Data // 프로젝트 데이터 영역
{
    public static class ItemDataPolicy // 공통 아이템 데이터 정책
    {
        public const string ItemIdPrefix = "IT_"; // 일반 아이템 ID 접두사
        public const string EquipmentIdPrefix = "EQ_"; // 장비 아이템 ID 접두사

        public static string GetExpectedIdPrefix(ItemType itemType) // 아이템 유형별 ID 접두사 조회
        {
            return itemType == ItemType.Equipment ? EquipmentIdPrefix : ItemIdPrefix; // 장비 및 일반 아이템 접두사 반환
        }

        public static void Validate(ItemData item, IList<string> errors) // 단일 아이템 공통 정책 검증
        {
            if (item == null || errors == null) // 검증 대상 및 오류 목록 확인
            {
                return; // 검증 중단
            }

            if (!Enum.IsDefined(typeof(ItemType), item.Type)) // 아이템 유형 정의 여부 확인
            {
                errors.Add($"[Item] 정의되지 않은 ItemType: {item.Id}, Value={(int)item.Type}"); // 잘못된 아이템 유형 오류 추가
                return; // 추가 유형 검증 중단
            }

            if (!Enum.IsDefined(typeof(ItemGrade), item.Grade)) // 아이템 등급 정의 여부 확인
            {
                errors.Add($"[Item] 정의되지 않은 ItemGrade: {item.Id}, Value={(int)item.Grade}"); // 잘못된 아이템 등급 오류 추가
            }

            if (item.MaxStack < 1) // 최대 보유 수량 확인
            {
                errors.Add($"[Item] MaxStack은 1 이상이어야 합니다: {item.Id}, MaxStack={item.MaxStack}"); // 최대 수량 오류 추가
            }

            if (string.IsNullOrWhiteSpace(item.Id)) // 공통 레지스트리 빈 ID 처리 여부 확인
            {
                return; // 중복 빈 ID 오류 추가 방지
            }

            string expectedPrefix = GetExpectedIdPrefix(item.Type); // 아이템 유형별 접두사 조회

            if (!item.Id.StartsWith(expectedPrefix, StringComparison.Ordinal)) // 아이템 ID 접두사 확인
            {
                errors.Add($"[Item] ID 접두사 오류: {item.Id}, Type={item.Type}, Expected={expectedPrefix}"); // 아이템 ID 정책 오류 추가
            }

            if (item.Type == ItemType.Equipment && item.MaxStack != 1) // 장비 최대 수량 정책 확인
            {
                errors.Add($"[Item] 장비 MaxStack은 1이어야 합니다: {item.Id}, MaxStack={item.MaxStack}"); // 장비 최대 수량 오류 추가
            }
        }
    }
}
