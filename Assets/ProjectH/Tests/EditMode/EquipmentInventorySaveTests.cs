using System.Collections.Generic; // 목록 자료형
using System.Reflection; // 비공개 필드 설정 기능
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.SaveSystem; // 저장 데이터 기능
using UnityEngine; // Unity JSON 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class EquipmentInventorySaveTests // 장비 인벤토리 저장 테스트
    {
        [Test] // 테스트 표시
        public void CreateNewGame_EquipmentInventoryStartsEmpty() // 새 게임 장비 인벤토리 초기 상태 검증
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_A" }); // 신규 저장 데이터 생성

            Assert.That(saveData.EquipmentInventory, Is.Not.Null); // 장비 인벤토리 생성 검증
            Assert.That(saveData.EquipmentInventory.Count, Is.EqualTo(0)); // 초기 장비 인벤토리 비어 있음 검증
        }

        [Test] // 테스트 표시
        public void TryCreateEquipmentInstance_SameEquipmentCreatesUniqueInstancesAndCount() // 동일 장비 다중 획득 고유 인스턴스 검증
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_A" }); // 신규 저장 데이터 생성

            bool firstAdded = saveData.TryCreateEquipmentInstance("EQ_SWORD_001", out EquipmentInstanceSaveData first, out string firstError); // 첫 장비 획득
            bool secondAdded = saveData.TryCreateEquipmentInstance("EQ_SWORD_001", out EquipmentInstanceSaveData second, out string secondError); // 둘째 장비 획득

            Assert.That(firstAdded, Is.True, firstError); // 첫 장비 획득 성공 검증
            Assert.That(secondAdded, Is.True, secondError); // 둘째 장비 획득 성공 검증
            Assert.That(first, Is.Not.Null); // 첫 장비 인스턴스 생성 검증
            Assert.That(second, Is.Not.Null); // 둘째 장비 인스턴스 생성 검증
            Assert.That(first.InstanceId, Is.Not.EqualTo(second.InstanceId)); // 장비 인스턴스 ID 고유성 검증
            Assert.That(first.EquipmentId, Is.EqualTo("EQ_SWORD_001")); // 첫 장비 원본 ID 검증
            Assert.That(second.EquipmentId, Is.EqualTo("EQ_SWORD_001")); // 둘째 장비 원본 ID 검증
            Assert.That(saveData.GetEquipmentCount("EQ_SWORD_001"), Is.EqualTo(2)); // 동일 장비 보유 수량 검증
        }

        [Test] // 테스트 표시
        public void TryAddEquipmentInstance_DuplicateInstanceIdIsRejected() // 중복 장비 인스턴스 ID 거부 검증
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_A" }); // 신규 저장 데이터 생성

            bool firstAdded = saveData.TryAddEquipmentInstance("EQI_TEST_001", "EQ_SWORD_001", out string firstError); // 첫 지정 ID 장비 추가
            bool secondAdded = saveData.TryAddEquipmentInstance("EQI_TEST_001", "EQ_ARMOR_001", out string secondError); // 중복 지정 ID 장비 추가 시도

            Assert.That(firstAdded, Is.True, firstError); // 첫 장비 추가 성공 검증
            Assert.That(secondAdded, Is.False); // 중복 장비 추가 거부 검증
            Assert.That(secondError, Does.Contain("이미 존재하는 장비 인스턴스 ID")); // 중복 장비 오류 문구 검증
            Assert.That(saveData.EquipmentInventory.Count, Is.EqualTo(1)); // 중복 거부 후 인벤토리 개수 검증
        }

        [Test] // 테스트 표시
        public void FindAndRemoveEquipmentInstance_UsesUniqueInstanceId() // 장비 인스턴스 조회 및 제거 검증
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_A" }); // 신규 저장 데이터 생성
            bool added = saveData.TryAddEquipmentInstance("EQI_TEST_REMOVE", "EQ_ARMOR_001", out string addError); // 제거 대상 장비 추가

            EquipmentInstanceSaveData found = saveData.FindEquipmentInstance("EQI_TEST_REMOVE"); // 장비 인스턴스 조회
            bool removed = saveData.TryRemoveEquipmentInstance("EQI_TEST_REMOVE", out EquipmentInstanceSaveData removedInstance, out string removeError); // 장비 인스턴스 제거

            Assert.That(added, Is.True, addError); // 제거 대상 장비 추가 성공 검증
            Assert.That(found, Is.Not.Null); // 장비 인스턴스 조회 성공 검증
            Assert.That(found.EquipmentId, Is.EqualTo("EQ_ARMOR_001")); // 조회 장비 원본 ID 검증
            Assert.That(removed, Is.True, removeError); // 장비 인스턴스 제거 성공 검증
            Assert.That(removedInstance, Is.SameAs(found)); // 제거 장비 인스턴스 동일성 검증
            Assert.That(saveData.FindEquipmentInstance("EQI_TEST_REMOVE"), Is.Null); // 제거 후 장비 조회 실패 검증
            Assert.That(saveData.GetEquipmentCount("EQ_ARMOR_001"), Is.EqualTo(0)); // 제거 후 보유 수량 검증
        }

        [Test] // 테스트 표시
        public void JsonRoundTrip_RetainsEquipmentInventory() // JSON 저장 왕복 장비 인벤토리 유지 검증
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_A" }); // 신규 저장 데이터 생성
            bool firstAdded = saveData.TryAddEquipmentInstance("EQI_SAVE_001", "EQ_SWORD_001", out string firstError); // 첫 저장 대상 장비 추가
            bool secondAdded = saveData.TryAddEquipmentInstance("EQI_SAVE_002", "EQ_SWORD_001", out string secondError); // 둘째 저장 대상 장비 추가
            bool thirdAdded = saveData.TryAddEquipmentInstance("EQI_SAVE_003", "EQ_ARMOR_001", out string thirdError); // 셋째 저장 대상 장비 추가
            string json = JsonUtility.ToJson(saveData, true); // 저장 데이터 JSON 직렬화
            SaveData loadedSave = JsonUtility.FromJson<SaveData>(json); // 저장 데이터 JSON 역직렬화
            loadedSave.EnsureDefaults(); // 불러온 저장 기본값 복원

            Assert.That(firstAdded, Is.True, firstError); // 첫 저장 대상 장비 추가 검증
            Assert.That(secondAdded, Is.True, secondError); // 둘째 저장 대상 장비 추가 검증
            Assert.That(thirdAdded, Is.True, thirdError); // 셋째 저장 대상 장비 추가 검증
            Assert.That(loadedSave.EquipmentInventory.Count, Is.EqualTo(3)); // 재로드 장비 인벤토리 개수 검증
            Assert.That(loadedSave.GetEquipmentCount("EQ_SWORD_001"), Is.EqualTo(2)); // 재로드 무기 보유 수량 검증
            Assert.That(loadedSave.GetEquipmentCount("EQ_ARMOR_001"), Is.EqualTo(1)); // 재로드 방어구 보유 수량 검증
            Assert.That(loadedSave.FindEquipmentInstance("EQI_SAVE_001").EquipmentId, Is.EqualTo("EQ_SWORD_001")); // 재로드 첫 장비 원본 ID 검증
            Assert.That(loadedSave.FindEquipmentInstance("EQI_SAVE_003").EquipmentId, Is.EqualTo("EQ_ARMOR_001")); // 재로드 셋째 장비 원본 ID 검증
        }

        [Test] // 테스트 표시
        public void EnsureDefaults_NullEquipmentInventoryRestoresEmptyList() // 이전 저장 장비 인벤토리 누락 복원 검증
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_A" }); // 신규 저장 데이터 생성
            FieldInfo inventoryField = typeof(SaveData).GetField("equipmentInventory", BindingFlags.Instance | BindingFlags.NonPublic); // 장비 인벤토리 비공개 필드 조회
            Assert.That(inventoryField, Is.Not.Null); // 장비 인벤토리 필드 존재 검증
            inventoryField.SetValue(saveData, null); // 이전 저장 상태처럼 장비 인벤토리 제거

            saveData.EnsureDefaults(); // 이전 저장 기본값 복원

            Assert.That(saveData.EquipmentInventory, Is.Not.Null); // 누락 인벤토리 복원 검증
            Assert.That(saveData.EquipmentInventory.Count, Is.EqualTo(0)); // 복원 인벤토리 빈 상태 검증
            Assert.That(saveData.SaveVersion, Is.EqualTo(1)); // 저장 버전 유지 검증
        }
    }
}
