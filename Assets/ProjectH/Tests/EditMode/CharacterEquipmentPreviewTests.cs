using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 장비 비교 계산 기능
using ProjectH.Data; // 프로젝트 데이터 기능
using ProjectH.SaveSystem; // 저장 장비 기능
using UnityEditor; // Unity 에디터 에셋 기능
using UnityEngine; // Unity 게임 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class CharacterEquipmentPreviewTests // 장비 비교 미리보기 테스트
    {
        private const string CatalogPath = "Assets/ProjectH/Data/Database/ProjectHDataCatalog.asset"; // 데이터 카탈로그 경로
        private GameObject dataObject; // 데이터 관리자 게임 오브젝트
        private DataManager dataManager; // 테스트 데이터 관리자

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 데이터 관리자 준비
        {
            dataObject = new GameObject("CharacterEquipmentPreviewTests"); // 테스트 게임 오브젝트 생성
            dataManager = dataObject.AddComponent<DataManager>(); // 데이터 관리자 컴포넌트 생성
            ProjectHDataCatalog catalog = AssetDatabase.LoadAssetAtPath<ProjectHDataCatalog>(CatalogPath); // 데이터 카탈로그 로드
            SerializedObject serialized = new SerializedObject(dataManager); // 데이터 관리자 직렬화 객체 생성
            serialized.FindProperty("catalog").objectReferenceValue = catalog; // 데이터 카탈로그 연결
            serialized.ApplyModifiedPropertiesWithoutUndo(); // 데이터 카탈로그 연결 적용
            dataManager.Initialize(); // 데이터 관리자 초기화
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 테스트 객체 정리
        {
            Object.DestroyImmediate(dataObject); // 테스트 게임 오브젝트 제거
        }

        [Test] // 테스트 표시
        public void TryCreate_WeaponReplacementBuildsExpectedStatsWithoutMutatingSave() // 무기 교체 미리보기와 저장 무변경 검증
        {
            SaveData saveData = CreateSerenaWithEquipment(); // 테스트 세레나 저장 생성
            CharacterSaveData characterSave = saveData.FindCharacter("CH_SERENA"); // 세레나 저장 데이터 조회
            string beforeWeapon = characterSave.Equipment.WeaponInstanceId; // 미리보기 전 무기 인스턴스 저장

            bool created = CharacterEquipmentPreviewService.TryCreate(saveData, dataManager, "CH_SERENA", "EQI_IRON", out CharacterEquipmentPreview preview, out string error); // 철제 검 교체 미리보기 생성

            Assert.That(created, Is.True, error); // 미리보기 생성 성공 검증
            Assert.That(preview.ActionLabel, Is.EqualTo("교체")); // 교체 액션 문구 검증
            Assert.That(preview.CurrentStats.Attack, Is.EqualTo(200)); // 현재 공격력 검증
            Assert.That(preview.PreviewStats.Attack, Is.EqualTo(215)); // 교체 후 공격력 검증
            Assert.That(preview.CurrentStats.MaxHp, Is.EqualTo(preview.PreviewStats.MaxHp)); // 무기 교체 체력 유지 검증
            Assert.That(characterSave.Equipment.WeaponInstanceId, Is.EqualTo(beforeWeapon)); // 미리보기 저장 데이터 무변경 검증
        }

        [Test] // 테스트 표시
        public void TryCreate_EquippedItemPreviewsUnequipWithoutMutatingSave() // 장착 장비 선택 시 해제 미리보기 검증
        {
            SaveData saveData = CreateSerenaWithEquipment(); // 테스트 세레나 저장 생성
            CharacterSaveData characterSave = saveData.FindCharacter("CH_SERENA"); // 세레나 저장 데이터 조회

            bool created = CharacterEquipmentPreviewService.TryCreate(saveData, dataManager, "CH_SERENA", "EQI_TRAINING", out CharacterEquipmentPreview preview, out string error); // 현재 무기 해제 미리보기 생성

            Assert.That(created, Is.True, error); // 미리보기 생성 성공 검증
            Assert.That(preview.ActionLabel, Is.EqualTo("해제")); // 해제 액션 문구 검증
            Assert.That(preview.CurrentStats.Attack, Is.EqualTo(200)); // 현재 공격력 검증
            Assert.That(preview.PreviewStats.Attack, Is.EqualTo(180)); // 해제 후 공격력 검증
            Assert.That(characterSave.Equipment.WeaponInstanceId, Is.EqualTo("EQI_TRAINING")); // 실제 장착 상태 유지 검증
        }

        [Test] // 테스트 표시
        public void TryCreate_EmptySlotUsesEquipAction() // 빈 슬롯 장착 미리보기 검증
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 빈 장비 세레나 저장 생성
            bool added = saveData.TryAddEquipmentInstance("EQI_IRON", "EQ_WEAPON_IRON", out string addError); // 철제 검 인스턴스 추가

            bool created = CharacterEquipmentPreviewService.TryCreate(saveData, dataManager, "CH_SERENA", "EQI_IRON", out CharacterEquipmentPreview preview, out string error); // 빈 무기 슬롯 미리보기 생성

            Assert.That(added, Is.True, addError); // 철제 검 추가 성공 검증
            Assert.That(created, Is.True, error); // 미리보기 생성 성공 검증
            Assert.That(preview.ActionLabel, Is.EqualTo("장착")); // 장착 액션 문구 검증
            Assert.That(preview.CurrentStats.Attack, Is.EqualTo(180)); // 현재 기본 공격력 검증
            Assert.That(preview.PreviewStats.Attack, Is.EqualTo(215)); // 장착 후 공격력 검증
        }

        [Test] // 테스트 표시
        public void TryCreate_OtherCharacterOwnedItemIsRejected() // 다른 캐릭터 장착 장비 비교 차단 검증
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA", "CH_ELLEN" }); // 두 캐릭터 저장 생성
            bool added = saveData.TryAddEquipmentInstance("EQI_SHARED", "EQ_WEAPON_IRON", out string addError); // 공유 시도 장비 추가
            bool equipped = CharacterEquipmentService.TryEquip(saveData, dataManager, "CH_ELLEN", "EQI_SHARED", out string equipError); // 엘렌 장비 착용

            bool created = CharacterEquipmentPreviewService.TryCreate(saveData, dataManager, "CH_SERENA", "EQI_SHARED", out CharacterEquipmentPreview preview, out string error); // 세레나 비교 미리보기 시도

            Assert.That(added, Is.True, addError); // 공유 장비 추가 성공 검증
            Assert.That(equipped, Is.True, equipError); // 엘렌 장착 성공 검증
            Assert.That(created, Is.False); // 다른 캐릭터 장착 장비 비교 차단 검증
            Assert.That(preview, Is.Null); // 실패 미리보기 null 검증
            Assert.That(error, Does.Contain("다른 캐릭터")); // 다른 캐릭터 오류 문구 검증
        }

        [Test] // 테스트 표시
        public void TryCalculateWithSlotOverride_DoesNotMutateCharacterEquipment() // 슬롯 교체 계산 저장 무변경 검증
        {
            SaveData saveData = CreateSerenaWithEquipment(); // 테스트 세레나 저장 생성
            CharacterSaveData characterSave = saveData.FindCharacter("CH_SERENA"); // 세레나 저장 데이터 조회

            bool calculated = BattleEquipmentStatCalculator.TryCalculateWithSlotOverride(characterSave, saveData, dataManager, EquipmentSlot.Weapon, "EQI_IRON", out BattleEquipmentStatBonus bonus, out string error); // 철제 검 가상 교체 보정 계산

            Assert.That(calculated, Is.True, error); // 가상 교체 계산 성공 검증
            Assert.That(bonus.Attack, Is.EqualTo(35f).Within(0.0001f)); // 철제 검 공격력 보정 검증
            Assert.That(bonus.MaxHp, Is.EqualTo(120f).Within(0.0001f)); // 기존 방어구 체력 보정 유지 검증
            Assert.That(characterSave.Equipment.WeaponInstanceId, Is.EqualTo("EQI_TRAINING")); // 실제 무기 장착 상태 유지 검증
        }

        private SaveData CreateSerenaWithEquipment() // 훈련 장비 세레나 저장 생성
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 세레나 새 저장 데이터 생성
            bool trainingAdded = saveData.TryAddEquipmentInstance("EQI_TRAINING", "EQ_WEAPON_TRAINING", out string trainingAddError); // 훈련용 검 추가
            bool ironAdded = saveData.TryAddEquipmentInstance("EQI_IRON", "EQ_WEAPON_IRON", out string ironAddError); // 철제 검 추가
            bool armorAdded = saveData.TryAddEquipmentInstance("EQI_ARMOR", "EQ_ARMOR_TRAINING", out string armorAddError); // 훈련용 갑옷 추가
            bool weaponEquipped = CharacterEquipmentService.TryEquip(saveData, dataManager, "CH_SERENA", "EQI_TRAINING", out string weaponEquipError); // 훈련용 검 장착
            bool armorEquipped = CharacterEquipmentService.TryEquip(saveData, dataManager, "CH_SERENA", "EQI_ARMOR", out string armorEquipError); // 훈련용 갑옷 장착
            Assert.That(trainingAdded, Is.True, trainingAddError); // 훈련용 검 추가 성공 검증
            Assert.That(ironAdded, Is.True, ironAddError); // 철제 검 추가 성공 검증
            Assert.That(armorAdded, Is.True, armorAddError); // 훈련용 갑옷 추가 성공 검증
            Assert.That(weaponEquipped, Is.True, weaponEquipError); // 훈련용 검 장착 성공 검증
            Assert.That(armorEquipped, Is.True, armorEquipError); // 훈련용 갑옷 장착 성공 검증
            return saveData; // 테스트 저장 반환
        }
    }
}
