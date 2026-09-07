using System.Collections.Generic; // 제네릭 컬렉션 기능
using System.Reflection; // 비공개 직렬화 필드 설정 기능
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Data; // 프로젝트 데이터 기능
using UnityEngine; // Unity 오브젝트 기능
using UnityEngine.TestTools; // Unity 로그 검증 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class EquipmentDataTests // 장비 데이터 구조 테스트
    {
        private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>(); // 테스트 생성 오브젝트 목록

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 테스트 생성 오브젝트 정리
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--) // 생성 오브젝트 역순 순회
            {
                if (createdObjects[index] != null) // 오브젝트 존재 여부 확인
                {
                    UnityEngine.Object.DestroyImmediate(createdObjects[index]); // 테스트 오브젝트 즉시 제거
                }
            }

            createdObjects.Clear(); // 생성 목록 초기화
        }

        [Test] // 테스트 표시
        public void EquipmentStatOption_StoresTypeAndValue() // 장비 옵션 값 보관 검증
        {
            EquipmentStatOption option = new EquipmentStatOption(EquipmentStatType.Attack, 25f); // 공격력 옵션 생성

            Assert.That(option.StatType, Is.EqualTo(EquipmentStatType.Attack)); // 옵션 종류 검증
            Assert.That(option.Value, Is.EqualTo(25f).Within(0.0001f)); // 옵션 수치 검증
        }

        [Test] // 테스트 표시
        public void EquipmentData_UsesBackingItemAndSumsMatchingStats() // 장비 원본 연동 및 옵션 합산 검증
        {
            ItemData item = CreateItem("EQ_TEST_SWORD", "테스트 검", ItemType.Equipment, ItemGrade.Rare); // 장비 아이템 생성
            List<EquipmentStatOption> options = new List<EquipmentStatOption>(); // 장비 옵션 목록 생성
            options.Add(new EquipmentStatOption(EquipmentStatType.Attack, 20f)); // 첫 공격력 옵션 추가
            options.Add(new EquipmentStatOption(EquipmentStatType.Attack, 5.5f)); // 둘째 공격력 옵션 추가
            options.Add(new EquipmentStatOption(EquipmentStatType.Defense, 7f)); // 방어력 옵션 추가
            EquipmentData equipment = CreateEquipment(item, EquipmentSlot.Weapon, options); // 무기 장비 생성

            Assert.That(equipment.Id, Is.EqualTo("EQ_TEST_SWORD")); // 장비 ID 검증
            Assert.That(equipment.DisplayName, Is.EqualTo("테스트 검")); // 장비 이름 검증
            Assert.That(equipment.Grade, Is.EqualTo(ItemGrade.Rare)); // 장비 등급 검증
            Assert.That(equipment.Slot, Is.EqualTo(EquipmentSlot.Weapon)); // 장비 슬롯 검증
            Assert.That(equipment.GetStatValue(EquipmentStatType.Attack), Is.EqualTo(25.5f).Within(0.0001f)); // 공격력 옵션 합산 검증
            Assert.That(equipment.GetStatValue(EquipmentStatType.Defense), Is.EqualTo(7f).Within(0.0001f)); // 방어력 옵션 검증
            Assert.That(equipment.GetStatValue(EquipmentStatType.MaxHp), Is.EqualTo(0f).Within(0.0001f)); // 미지정 옵션 기본값 검증
        }

        [Test] // 테스트 표시
        public void DataManager_RegistersAndFindsEquipment() // 장비 등록 및 조회 검증
        {
            ItemData item = CreateItem("EQ_TEST_ARMOR", "테스트 갑옷", ItemType.Equipment, ItemGrade.Common); // 장비 아이템 생성
            EquipmentData equipment = CreateEquipment(item, EquipmentSlot.Armor, new List<EquipmentStatOption>()); // 방어구 데이터 생성
            ProjectHDataCatalog catalog = CreateCatalog(new List<ItemData> { item }, new List<EquipmentData> { equipment }); // 테스트 카탈로그 생성
            DataManager manager = CreateDataManager(catalog); // 데이터 관리자 생성

            Assert.That(manager.IsInitialized, Is.True); // 데이터 초기화 검증
            Assert.That(manager.GetEquipment("EQ_TEST_ARMOR"), Is.SameAs(equipment)); // 장비 조회 검증
            Assert.That(manager.EquipmentCount, Is.EqualTo(1)); // 장비 등록 수 검증
        }

        [Test] // 테스트 표시
        public void Initialize_NonEquipmentBackingItemReportsValidationError() // 비장비 아이템 연결 거부 검증
        {
            ItemData item = CreateItem("ITEM_TEST_MATERIAL", "테스트 재료", ItemType.Material, ItemGrade.Common); // 재료 아이템 생성
            EquipmentData equipment = CreateEquipment(item, EquipmentSlot.Weapon, new List<EquipmentStatOption>()); // 잘못된 장비 데이터 생성
            ProjectHDataCatalog catalog = CreateCatalog(new List<ItemData> { item }, new List<EquipmentData> { equipment }); // 테스트 카탈로그 생성
            DataManager manager = CreateDataManagerWithoutInitialize(catalog); // 데이터 관리자 생성
            LogAssert.Expect(LogType.Error, "[Project H] [Equipment] 장비가 아닌 ItemData 연결: ITEM_TEST_MATERIAL"); // 예상 검증 오류 로그 등록

            manager.Initialize(); // 데이터 초기화 실행

            Assert.That(manager.IsInitialized, Is.False); // 초기화 거부 검증
            Assert.That(manager.ValidationErrors, Has.Member("[Equipment] 장비가 아닌 ItemData 연결: ITEM_TEST_MATERIAL")); // 아이템 유형 오류 검증
        }

        [Test] // 테스트 표시
        public void Initialize_UnregisteredBackingItemReportsValidationError() // 미등록 원본 아이템 연결 거부 검증
        {
            ItemData item = CreateItem("EQ_UNREGISTERED", "미등록 장비", ItemType.Equipment, ItemGrade.Common); // 미등록 장비 아이템 생성
            EquipmentData equipment = CreateEquipment(item, EquipmentSlot.Weapon, new List<EquipmentStatOption>()); // 장비 데이터 생성
            ProjectHDataCatalog catalog = CreateCatalog(new List<ItemData>(), new List<EquipmentData> { equipment }); // 원본 누락 카탈로그 생성
            DataManager manager = CreateDataManagerWithoutInitialize(catalog); // 데이터 관리자 생성
            LogAssert.Expect(LogType.Error, "[Project H] [Equipment] Items 미등록 ItemData 연결: EQ_UNREGISTERED"); // 예상 검증 오류 로그 등록

            manager.Initialize(); // 데이터 초기화 실행

            Assert.That(manager.IsInitialized, Is.False); // 초기화 거부 검증
            Assert.That(manager.ValidationErrors, Has.Member("[Equipment] Items 미등록 ItemData 연결: EQ_UNREGISTERED")); // 미등록 원본 오류 검증
        }

        [Test] // 테스트 표시
        public void Initialize_DuplicateEquipmentIdReportsValidationError() // 장비 ID 중복 거부 검증
        {
            ItemData item = CreateItem("EQ_DUPLICATE", "중복 장비", ItemType.Equipment, ItemGrade.Common); // 공통 장비 아이템 생성
            EquipmentData first = CreateEquipment(item, EquipmentSlot.Weapon, new List<EquipmentStatOption>()); // 첫 장비 데이터 생성
            EquipmentData second = CreateEquipment(item, EquipmentSlot.Armor, new List<EquipmentStatOption>()); // 둘째 장비 데이터 생성
            ProjectHDataCatalog catalog = CreateCatalog(new List<ItemData> { item }, new List<EquipmentData> { first, second }); // 중복 장비 카탈로그 생성
            DataManager manager = CreateDataManagerWithoutInitialize(catalog); // 데이터 관리자 생성
            LogAssert.Expect(LogType.Error, "[Project H] [Equipment] 중복 ID: EQ_DUPLICATE"); // 예상 검증 오류 로그 등록

            manager.Initialize(); // 데이터 초기화 실행

            Assert.That(manager.IsInitialized, Is.False); // 초기화 거부 검증
            Assert.That(manager.ValidationErrors, Has.Member("[Equipment] 중복 ID: EQ_DUPLICATE")); // 중복 ID 오류 검증
        }

        private ItemData CreateItem(string id, string displayName, ItemType itemType, ItemGrade grade) // 테스트 아이템 생성
        {
            ItemData item = Track(ScriptableObject.CreateInstance<ItemData>()); // 아이템 인스턴스 생성
            SetPrivateField(item, "id", id); // 아이템 ID 설정
            SetPrivateField(item, "displayName", displayName); // 아이템 이름 설정
            SetPrivateField(item, "itemType", itemType); // 아이템 종류 설정
            SetPrivateField(item, "grade", grade); // 아이템 등급 설정
            return item; // 테스트 아이템 반환
        }

        private EquipmentData CreateEquipment(ItemData item, EquipmentSlot slot, List<EquipmentStatOption> statOptions) // 테스트 장비 생성
        {
            EquipmentData equipment = Track(ScriptableObject.CreateInstance<EquipmentData>()); // 장비 인스턴스 생성
            SetPrivateField(equipment, "item", item); // 장비 원본 아이템 설정
            SetPrivateField(equipment, "slot", slot); // 장비 슬롯 설정
            SetPrivateField(equipment, "statOptions", statOptions); // 장비 옵션 설정
            return equipment; // 테스트 장비 반환
        }

        private ProjectHDataCatalog CreateCatalog(List<ItemData> items, List<EquipmentData> equipments) // 테스트 카탈로그 생성
        {
            ProjectHDataCatalog catalog = Track(ScriptableObject.CreateInstance<ProjectHDataCatalog>()); // 카탈로그 인스턴스 생성
            SetPrivateField(catalog, "items", items); // 아이템 목록 설정
            SetPrivateField(catalog, "equipments", equipments); // 장비 목록 설정
            return catalog; // 테스트 카탈로그 반환
        }

        private DataManager CreateDataManager(ProjectHDataCatalog catalog) // 초기화된 데이터 관리자 생성
        {
            DataManager manager = CreateDataManagerWithoutInitialize(catalog); // 데이터 관리자 생성
            manager.Initialize(); // 데이터 관리자 초기화
            return manager; // 데이터 관리자 반환
        }

        private DataManager CreateDataManagerWithoutInitialize(ProjectHDataCatalog catalog) // 미초기화 데이터 관리자 생성
        {
            GameObject managerObject = Track(new GameObject("DataManager_Test")); // 데이터 관리자 오브젝트 생성
            DataManager manager = managerObject.AddComponent<DataManager>(); // 데이터 관리자 추가
            manager.Configure(catalog); // 테스트 카탈로그 연결
            return manager; // 데이터 관리자 반환
        }

        private T Track<T>(T target) where T : UnityEngine.Object // 테스트 오브젝트 추적
        {
            createdObjects.Add(target); // 정리 대상 등록
            return target; // 생성 오브젝트 반환
        }

        private static void SetPrivateField(object target, string fieldName, object value) // 비공개 직렬화 필드 설정
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic); // 필드 정보 조회
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found on {target.GetType().Name}."); // 필드 존재 검증
            field.SetValue(target, value); // 필드 값 주입
        }
    }
}
