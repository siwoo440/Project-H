using System.Collections.Generic; // 테스트 컬렉션 기능
using System.Reflection; // 비공개 직렬화 필드 설정 기능
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Data; // 장비 원본 데이터 기능
using ProjectH.SaveSystem; // 장비 저장 및 장착 기능
using UnityEngine; // Unity 오브젝트 및 JSON 기능

namespace ProjectH.Tests.EditMode // 프로젝트 EditMode 테스트 영역
{
    public sealed class CharacterEquipmentServiceTests // 캐릭터 장비 장착 서비스 테스트
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

            createdObjects.Clear(); // 생성 오브젝트 목록 초기화
        }

        [Test] // 테스트 표시
        public void TryEquip_WeaponStoresUniqueInstanceOnCharacter() // 무기 인스턴스 캐릭터 장착 검증
        {
            DataManager manager = CreateDataManager(); // 테스트 데이터 관리자 생성
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_A", "CH_B" }); // 테스트 저장 데이터 생성
            bool added = saveData.TryAddEquipmentInstance("EQI_WEAPON_A", "EQ_TEST_WEAPON", out string addError); // 테스트 무기 인스턴스 추가

            bool equipped = CharacterEquipmentService.TryEquip(saveData, manager, "CH_A", "EQI_WEAPON_A", out string equipError); // 테스트 무기 장착 실행

            Assert.That(added, Is.True, addError); // 테스트 무기 추가 성공 검증
            Assert.That(equipped, Is.True, equipError); // 테스트 무기 장착 성공 검증
            Assert.That(saveData.FindCharacter("CH_A").Equipment.WeaponInstanceId, Is.EqualTo("EQI_WEAPON_A")); // 캐릭터 무기 슬롯 인스턴스 검증
            Assert.That(saveData.FindCharacter("CH_A").Equipment.ArmorInstanceId, Is.Empty); // 캐릭터 방어구 슬롯 유지 검증
        }

        [Test] // 테스트 표시
        public void TryEquip_SameSlotReplacesPreviousInstance() // 동일 슬롯 장비 교체 검증
        {
            DataManager manager = CreateDataManager(); // 테스트 데이터 관리자 생성
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_A" }); // 테스트 저장 데이터 생성
            saveData.TryAddEquipmentInstance("EQI_WEAPON_A", "EQ_TEST_WEAPON", out string firstAddError); // 첫 무기 인스턴스 추가
            saveData.TryAddEquipmentInstance("EQI_WEAPON_B", "EQ_TEST_WEAPON", out string secondAddError); // 둘째 무기 인스턴스 추가
            bool firstEquipped = CharacterEquipmentService.TryEquip(saveData, manager, "CH_A", "EQI_WEAPON_A", out string firstEquipError); // 첫 무기 장착 실행

            bool secondEquipped = CharacterEquipmentService.TryEquip(saveData, manager, "CH_A", "EQI_WEAPON_B", out string secondEquipError); // 둘째 무기 교체 실행

            Assert.That(firstEquipped, Is.True, firstEquipError); // 첫 무기 장착 성공 검증
            Assert.That(secondEquipped, Is.True, secondEquipError); // 둘째 무기 교체 성공 검증
            Assert.That(saveData.FindCharacter("CH_A").Equipment.WeaponInstanceId, Is.EqualTo("EQI_WEAPON_B")); // 무기 슬롯 교체 결과 검증
            Assert.That(CharacterEquipmentService.FindEquippedCharacter(saveData, "EQI_WEAPON_A"), Is.Null); // 기존 무기 미착용 상태 검증
        }

        [Test] // 테스트 표시
        public void TryEquip_SameInstanceCannotBeEquippedByAnotherCharacter() // 동일 장비 인스턴스 중복 착용 차단 검증
        {
            DataManager manager = CreateDataManager(); // 테스트 데이터 관리자 생성
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_A", "CH_B" }); // 테스트 저장 데이터 생성
            saveData.TryAddEquipmentInstance("EQI_SHARED", "EQ_TEST_WEAPON", out string addError); // 공유 시도 무기 인스턴스 추가
            bool firstEquipped = CharacterEquipmentService.TryEquip(saveData, manager, "CH_A", "EQI_SHARED", out string firstError); // 첫 캐릭터 무기 장착 실행

            bool secondEquipped = CharacterEquipmentService.TryEquip(saveData, manager, "CH_B", "EQI_SHARED", out string secondError); // 둘째 캐릭터 동일 무기 장착 시도

            Assert.That(firstEquipped, Is.True, firstError); // 첫 캐릭터 장착 성공 검증
            Assert.That(secondEquipped, Is.False); // 둘째 캐릭터 중복 장착 차단 검증
            Assert.That(secondError, Does.Contain("다른 캐릭터")); // 중복 장착 오류 문구 검증
            Assert.That(saveData.FindCharacter("CH_B").Equipment.WeaponInstanceId, Is.Empty); // 둘째 캐릭터 무기 슬롯 미변경 검증
        }

        [Test] // 테스트 표시
        public void TryEquip_ArmorUsesArmorSlot() // 방어구 슬롯 자동 선택 검증
        {
            DataManager manager = CreateDataManager(); // 테스트 데이터 관리자 생성
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_A" }); // 테스트 저장 데이터 생성
            saveData.TryAddEquipmentInstance("EQI_ARMOR_A", "EQ_TEST_ARMOR", out string addError); // 테스트 방어구 인스턴스 추가

            bool equipped = CharacterEquipmentService.TryEquip(saveData, manager, "CH_A", "EQI_ARMOR_A", out string equipError); // 테스트 방어구 장착 실행

            Assert.That(equipped, Is.True, equipError); // 방어구 장착 성공 검증
            Assert.That(saveData.FindCharacter("CH_A").Equipment.ArmorInstanceId, Is.EqualTo("EQI_ARMOR_A")); // 방어구 슬롯 인스턴스 검증
            Assert.That(saveData.FindCharacter("CH_A").Equipment.WeaponInstanceId, Is.Empty); // 무기 슬롯 미변경 검증
        }

        [Test] // 테스트 표시
        public void TryUnequip_ClearsSlotAndKeepsInventoryInstance() // 장비 해제 후 인벤토리 유지 검증
        {
            DataManager manager = CreateDataManager(); // 테스트 데이터 관리자 생성
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_A" }); // 테스트 저장 데이터 생성
            saveData.TryAddEquipmentInstance("EQI_WEAPON_A", "EQ_TEST_WEAPON", out string addError); // 테스트 무기 인스턴스 추가
            CharacterEquipmentService.TryEquip(saveData, manager, "CH_A", "EQI_WEAPON_A", out string equipError); // 테스트 무기 장착 실행

            bool unequipped = CharacterEquipmentService.TryUnequip(saveData, "CH_A", EquipmentSlot.Weapon, out EquipmentInstanceSaveData removedInstance, out string unequipError); // 테스트 무기 해제 실행

            Assert.That(unequipped, Is.True, unequipError); // 테스트 무기 해제 성공 검증
            Assert.That(removedInstance.InstanceId, Is.EqualTo("EQI_WEAPON_A")); // 해제 장비 인스턴스 검증
            Assert.That(saveData.FindCharacter("CH_A").Equipment.WeaponInstanceId, Is.Empty); // 캐릭터 무기 슬롯 비움 검증
            Assert.That(saveData.FindEquipmentInstance("EQI_WEAPON_A"), Is.Not.Null); // 해제 장비 인벤토리 유지 검증
        }

        [Test] // 테스트 표시
        public void TryRemoveEquipmentInstance_EquippedInstanceIsRejected() // 장착 중 장비 인벤토리 제거 차단 검증
        {
            DataManager manager = CreateDataManager(); // 테스트 데이터 관리자 생성
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_A" }); // 테스트 저장 데이터 생성
            saveData.TryAddEquipmentInstance("EQI_WEAPON_A", "EQ_TEST_WEAPON", out string addError); // 테스트 무기 인스턴스 추가
            CharacterEquipmentService.TryEquip(saveData, manager, "CH_A", "EQI_WEAPON_A", out string equipError); // 테스트 무기 장착 실행

            bool removed = saveData.TryRemoveEquipmentInstance("EQI_WEAPON_A", out EquipmentInstanceSaveData removedInstance, out string removeError); // 장착 중 장비 제거 시도

            Assert.That(removed, Is.False); // 장착 중 장비 제거 차단 검증
            Assert.That(removedInstance, Is.Null); // 제거 장비 결과 미생성 검증
            Assert.That(removeError, Does.Contain("장착 중인 장비")); // 장착 중 제거 오류 문구 검증
            Assert.That(saveData.FindEquipmentInstance("EQI_WEAPON_A"), Is.Not.Null); // 장착 중 장비 인벤토리 유지 검증
        }

        [Test] // 테스트 표시
        public void JsonRoundTrip_RetainsCharacterEquipmentInstanceIds() // JSON 저장 왕복 장착 상태 유지 검증
        {
            DataManager manager = CreateDataManager(); // 테스트 데이터 관리자 생성
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_A" }); // 테스트 저장 데이터 생성
            saveData.TryAddEquipmentInstance("EQI_WEAPON_SAVE", "EQ_TEST_WEAPON", out string weaponAddError); // 저장 대상 무기 추가
            saveData.TryAddEquipmentInstance("EQI_ARMOR_SAVE", "EQ_TEST_ARMOR", out string armorAddError); // 저장 대상 방어구 추가
            CharacterEquipmentService.TryEquip(saveData, manager, "CH_A", "EQI_WEAPON_SAVE", out string weaponEquipError); // 저장 대상 무기 장착
            CharacterEquipmentService.TryEquip(saveData, manager, "CH_A", "EQI_ARMOR_SAVE", out string armorEquipError); // 저장 대상 방어구 장착
            string json = JsonUtility.ToJson(saveData, true); // 저장 데이터 JSON 직렬화
            SaveData loaded = JsonUtility.FromJson<SaveData>(json); // 저장 데이터 JSON 역직렬화
            loaded.EnsureDefaults(); // 불러온 저장 기본값 복원

            CharacterSaveData loadedCharacter = loaded.FindCharacter("CH_A"); // 불러온 캐릭터 저장 조회
            Assert.That(loadedCharacter.Equipment.WeaponInstanceId, Is.EqualTo("EQI_WEAPON_SAVE")); // 재로드 무기 장착 상태 검증
            Assert.That(loadedCharacter.Equipment.ArmorInstanceId, Is.EqualTo("EQI_ARMOR_SAVE")); // 재로드 방어구 장착 상태 검증
            Assert.That(loaded.FindEquipmentInstance("EQI_WEAPON_SAVE"), Is.Not.Null); // 재로드 무기 인벤토리 유지 검증
            Assert.That(loaded.FindEquipmentInstance("EQI_ARMOR_SAVE"), Is.Not.Null); // 재로드 방어구 인벤토리 유지 검증
        }

        private DataManager CreateDataManager() // 장비 테스트 데이터 관리자 생성
        {
            ItemData weaponItem = CreateItem("EQ_TEST_WEAPON", "테스트 무기"); // 테스트 무기 ItemData 생성
            ItemData armorItem = CreateItem("EQ_TEST_ARMOR", "테스트 방어구"); // 테스트 방어구 ItemData 생성
            EquipmentData weapon = CreateEquipment(weaponItem, EquipmentSlot.Weapon); // 테스트 무기 EquipmentData 생성
            EquipmentData armor = CreateEquipment(armorItem, EquipmentSlot.Armor); // 테스트 방어구 EquipmentData 생성
            ProjectHDataCatalog catalog = Track(ScriptableObject.CreateInstance<ProjectHDataCatalog>()); // 테스트 데이터 카탈로그 생성
            SetPrivateField(catalog, "items", new List<ItemData> { weaponItem, armorItem }); // 테스트 카탈로그 아이템 목록 설정
            SetPrivateField(catalog, "equipments", new List<EquipmentData> { weapon, armor }); // 테스트 카탈로그 장비 목록 설정
            GameObject managerObject = Track(new GameObject("CharacterEquipment_DataManager_Test")); // 테스트 데이터 관리자 오브젝트 생성
            DataManager manager = managerObject.AddComponent<DataManager>(); // 테스트 데이터 관리자 컴포넌트 추가
            manager.Configure(catalog); // 테스트 데이터 카탈로그 연결
            manager.Initialize(); // 테스트 데이터 관리자 초기화
            return manager; // 초기화된 테스트 데이터 관리자 반환
        }

        private ItemData CreateItem(string id, string displayName) // 테스트 장비 기반 아이템 생성
        {
            ItemData item = Track(ScriptableObject.CreateInstance<ItemData>()); // 테스트 ItemData 인스턴스 생성
            SetPrivateField(item, "id", id); // 테스트 아이템 ID 설정
            SetPrivateField(item, "displayName", displayName); // 테스트 아이템 이름 설정
            SetPrivateField(item, "itemType", ItemType.Equipment); // 테스트 아이템 유형 장비 설정
            SetPrivateField(item, "grade", ItemGrade.Common); // 테스트 아이템 등급 설정
            return item; // 테스트 ItemData 반환
        }

        private EquipmentData CreateEquipment(ItemData item, EquipmentSlot slot) // 테스트 EquipmentData 생성
        {
            EquipmentData equipment = Track(ScriptableObject.CreateInstance<EquipmentData>()); // 테스트 EquipmentData 인스턴스 생성
            SetPrivateField(equipment, "item", item); // 테스트 장비 기반 아이템 연결
            SetPrivateField(equipment, "slot", slot); // 테스트 장비 슬롯 설정
            SetPrivateField(equipment, "statOptions", new List<EquipmentStatOption>()); // 테스트 장비 옵션 목록 설정
            return equipment; // 테스트 EquipmentData 반환
        }

        private T Track<T>(T target) where T : UnityEngine.Object // 테스트 Unity 오브젝트 추적
        {
            createdObjects.Add(target); // 테스트 정리 대상 등록
            return target; // 테스트 오브젝트 반환
        }

        private static void SetPrivateField(object target, string fieldName, object value) // 비공개 직렬화 필드 설정
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic); // 비공개 필드 정보 조회
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found on {target.GetType().Name}."); // 테스트 필드 존재 검증
            field.SetValue(target, value); // 테스트 필드 값 주입
        }
    }
}
