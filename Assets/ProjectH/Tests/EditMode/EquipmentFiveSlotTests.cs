using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 장비 보정 계산 기능
using ProjectH.Data; // 장비 데이터 기능
using ProjectH.SaveSystem; // 장착·룬 기능
using UnityEditor; // 에셋 로드 기능
using UnityEngine; // Unity 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class EquipmentFiveSlotTests // Day60 장비 5칸 (무기·투구·방어구·장갑·신발) 테스트
    {
        private const string Serena = "CH_SERENA"; // 테스트 캐릭터
        private GameObject dataObject; // 데이터 관리자 오브젝트
        private DataManager dataManager; // 실제 카탈로그 데이터 관리자

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 실제 카탈로그 준비 (새 장비 GUID 등록 검증 포함)
        {
            dataObject = new GameObject("EquipmentFiveSlotTests"); // 오브젝트 생성
            dataManager = dataObject.AddComponent<DataManager>(); // 컴포넌트 추가
            dataManager.Configure(AssetDatabase.LoadAssetAtPath<ProjectHDataCatalog>("Assets/ProjectH/Data/Database/ProjectHDataCatalog.asset")); // 실제 카탈로그
            dataManager.Initialize(); // 초기화
            Assert.That(dataManager.IsInitialized, Is.True, string.Join("\n", dataManager.ValidationErrors)); // 초기화 검증
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 정리
        {
            Object.DestroyImmediate(dataObject); // 오브젝트 제거
        }

        private EquipmentInstanceSaveData Equip(SaveData saveData, string equipmentId) // 장비 생성 후 세레나에게 장착
        {
            Assert.That(saveData.TryCreateEquipmentInstance(equipmentId, out EquipmentInstanceSaveData instance, out string createError), Is.True, createError); // 인스턴스 생성
            Assert.That(CharacterEquipmentService.TryEquip(saveData, dataManager, Serena, instance.InstanceId, out string equipError), Is.True, equipError); // 장착
            return instance; // 인스턴스 반환
        }

        [Test] // 새 장비 에셋 슬롯 검증
        public void NewGear_IsRegisteredWithCorrectSlots() // 에셋 테스트
        {
            Assert.That(dataManager.GetEquipment("EQ_HELMET_TRAINING").Slot, Is.EqualTo(EquipmentSlot.Helmet)); // 투구
            Assert.That(dataManager.GetEquipment("EQ_GLOVES_HUNTER").Slot, Is.EqualTo(EquipmentSlot.Gloves)); // 장갑
            Assert.That(dataManager.GetEquipment("EQ_BOOTS_SWIFT").Slot, Is.EqualTo(EquipmentSlot.Boots)); // 신발
            Assert.That(EquipmentSlotInfo.All.Count, Is.EqualTo(5)); // 5칸
        }

        [Test] // 5칸 저장 데이터 검증
        public void SaveData_StoresAllFiveSlots_AndClearsByInstance() // 저장 테스트
        {
            CharacterEquipmentSaveData equipment = new CharacterEquipmentSaveData(); // 빈 장착 정보

            foreach (EquipmentSlot slot in EquipmentSlotInfo.All) equipment.SetInstanceId(slot, $"EQI_{slot}"); // 5칸 설정

            foreach (EquipmentSlot slot in EquipmentSlotInfo.All) Assert.That(equipment.GetInstanceId(slot), Is.EqualTo($"EQI_{slot}")); // 5칸 조회
            Assert.That(equipment.ContainsInstance("EQI_Boots"), Is.True); // 신발 착용 확인
            equipment.ClearInstance("EQI_Boots"); // 신발 해제
            Assert.That(equipment.GetInstanceId(EquipmentSlot.Boots), Is.Empty); // 해제 검증
        }

        [Test] // 투구·장갑·신발 능력치 합산 검증
        public void Calculator_SumsHelmetGlovesAndBoots() // 합산 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { Serena }); // 새 게임
            Equip(saveData, "EQ_WEAPON_TRAINING"); // 무기 (공격력 +20)
            Equip(saveData, "EQ_HELMET_TRAINING"); // 투구 (체력 +60, 방어 +5)
            Equip(saveData, "EQ_GLOVES_HUNTER"); // 장갑 (공격력 +14, 치명 +0.03)
            Equip(saveData, "EQ_BOOTS_SWIFT"); // 신발 (이동 +0.35, 공속 +0.05)

            Assert.That(BattleEquipmentStatCalculator.TryCalculate(saveData.FindCharacter(Serena), saveData, dataManager, out BattleEquipmentStatBonus bonus, out string error), Is.True, error); // 계산
            Assert.That(bonus.Attack, Is.EqualTo(34f).Within(0.001f)); // 20 + 14
            Assert.That(bonus.MaxHp, Is.EqualTo(60f).Within(0.001f)); // 투구 체력
            Assert.That(bonus.CriticalRate, Is.EqualTo(0.03f).Within(0.0001f)); // 장갑 치명
            Assert.That(bonus.MoveSpeed, Is.EqualTo(0.35f).Within(0.0001f)); // 신발 이동
        }

        [Test] // 같은 칸 교체 검증
        public void EquippingSameSlot_ReplacesPrevious() // 교체 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { Serena }); // 새 게임
            EquipmentInstanceSaveData training = Equip(saveData, "EQ_BOOTS_TRAINING"); // 훈련용 신발
            EquipmentInstanceSaveData swift = Equip(saveData, "EQ_BOOTS_SWIFT"); // 질풍 신발

            Assert.That(CharacterEquipmentService.GetEquippedInstance(saveData, Serena, EquipmentSlot.Boots), Is.SameAs(swift)); // 새 신발 장착
            Assert.That(saveData.FindCharacter(Serena).Equipment.ContainsInstance(training.InstanceId), Is.False); // 이전 신발 해제
        }

        [Test] // 룬 4번 슬롯 : 새 칸의 고급 장비로도 해금 검증
        public void RuneSlotFour_UnlocksWithUncommonGloves() // 룬 연계 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { Serena }); // 새 게임
            Equip(saveData, "EQ_GLOVES_TRAINING"); // 일반 장갑

            Assert.That(RuneService.IsSlotUnlocked(saveData, dataManager, Serena, 3), Is.False); // 일반 등급은 잠김
            Equip(saveData, "EQ_GLOVES_HUNTER"); // 고급 장갑
            Assert.That(RuneService.IsSlotUnlocked(saveData, dataManager, Serena, 3), Is.True); // 해금
        }

        [Test] // 이전 세이브 호환 검증
        public void OldSave_WithTwoSlots_LoadsNewSlotsEmpty() // 호환 테스트
        {
            SaveData saveData = JsonUtility.FromJson<SaveData>("{\"characters\":[{\"characterId\":\"CH_SERENA\",\"equipment\":{\"weaponInstanceId\":\"EQI_A\",\"armorInstanceId\":\"\"}}]}"); // Day60 이전 세이브
            saveData.EnsureDefaults(); // 보정
            CharacterEquipmentSaveData equipment = saveData.FindCharacter(Serena).Equipment; // 장착 정보

            Assert.That(equipment.GetInstanceId(EquipmentSlot.Weapon), Is.EqualTo("EQI_A")); // 기존 무기 유지
            Assert.That(equipment.GetInstanceId(EquipmentSlot.Helmet), Is.Empty); // 새 칸 빈 칸
            Assert.That(equipment.GetInstanceId(EquipmentSlot.Boots), Is.Empty); // 새 칸 빈 칸
        }
    }
}
