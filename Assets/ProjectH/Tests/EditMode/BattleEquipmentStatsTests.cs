using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 스탯 기능
using ProjectH.Data; // 데이터 관리자 기능
using ProjectH.SaveSystem; // 저장 장비 기능
using UnityEditor; // Unity 에디터 기능
using UnityEngine; // Unity 게임 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleEquipmentStatsTests // 장비 전투 스탯 테스트
    {
        private const string CatalogPath = "Assets/ProjectH/Data/Database/ProjectHDataCatalog.asset"; // 데이터 카탈로그 경로
        private const string SerenaPath = "Assets/ProjectH/Data/Characters/CH_SERENA.asset"; // 세레나 데이터 경로
        private GameObject dataObject; // 데이터 관리자 게임 오브젝트
        private DataManager dataManager; // 테스트 데이터 관리자

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 데이터 관리자 준비
        {
            dataObject = new GameObject("BattleEquipmentStatsTests"); // 테스트 게임 오브젝트 생성
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
        public void TryCalculate_CombinesEquippedWeaponAndArmorOptions() // 무기 및 방어구 옵션 합산 검증
        {
            SaveData saveData = CreateEquippedSerena("EQ_WEAPON_TRAINING", "EQ_ARMOR_TRAINING"); // 훈련 장비 세레나 저장 생성
            CharacterSaveData characterSave = saveData.FindCharacter("CH_SERENA"); // 세레나 저장 데이터 조회

            bool calculated = BattleEquipmentStatCalculator.TryCalculate(characterSave, saveData, dataManager, out BattleEquipmentStatBonus bonus, out string error); // 장비 보정값 계산

            Assert.That(calculated, Is.True, error); // 장비 보정 계산 성공 검증
            Assert.That(bonus.MaxHp, Is.EqualTo(120f).Within(0.0001f)); // 방어구 체력 옵션 검증
            Assert.That(bonus.Attack, Is.EqualTo(20f).Within(0.0001f)); // 무기 공격력 옵션 검증
            Assert.That(bonus.Defense, Is.EqualTo(15f).Within(0.0001f)); // 방어구 방어력 옵션 검증
            Assert.That(bonus.Resistance, Is.EqualTo(0f).Within(0.0001f)); // 미지정 저항력 옵션 검증
        }

        [Test] // 테스트 표시
        public void TryCreate_AppliesEquippedStatsToRuntimeParty() // 장착 장비 전투 파티 반영 검증
        {
            SaveData saveData = CreateEquippedSerena("EQ_WEAPON_TRAINING", "EQ_ARMOR_TRAINING"); // 훈련 장비 세레나 저장 생성

            bool created = BattlePartyRuntime.TryCreate(dataManager, saveData, out BattlePartyRuntime party, out string error); // 장비 포함 전투 파티 생성

            Assert.That(created, Is.True, error); // 전투 파티 생성 성공 검증
            Assert.That(party[0].MaxHp, Is.EqualTo(2320)); // 장비 포함 최대 체력 검증
            Assert.That(party[0].Attack, Is.EqualTo(200)); // 장비 포함 공격력 검증
            Assert.That(party[0].Defense, Is.EqualTo(135)); // 장비 포함 방어력 검증
        }

        [Test] // 테스트 표시
        public void TryCreate_CombinesLevelGrowthAndEquippedStats() // 레벨 성장 및 장비 합산 검증
        {
            SaveData saveData = CreateEquippedSerena("EQ_WEAPON_TRAINING", "EQ_ARMOR_TRAINING"); // 훈련 장비 세레나 저장 생성
            CharacterSaveData characterSave = saveData.FindCharacter("CH_SERENA"); // 세레나 저장 데이터 조회
            characterSave.SetLevel(5); // 세레나 5레벨 설정

            bool created = BattlePartyRuntime.TryCreate(dataManager, saveData, out BattlePartyRuntime party, out string error); // 성장 및 장비 포함 전투 파티 생성

            Assert.That(created, Is.True, error); // 전투 파티 생성 성공 검증
            Assert.That(party[0].Level, Is.EqualTo(5)); // 성장 적용 레벨 검증
            Assert.That(party[0].MaxHp, Is.EqualTo(2760)); // 성장 및 장비 체력 합산 검증
            Assert.That(party[0].Attack, Is.EqualTo(236)); // 성장 및 장비 공격력 합산 검증
            Assert.That(party[0].Defense, Is.EqualTo(159)); // 성장 및 장비 방어력 합산 검증
        }

        [Test] // 테스트 표시
        public void CreateCharacter_AppliesAllSupportedEquipmentStatTypes() // 전체 장비 능력치 유형 반영 검증
        {
            CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>(SerenaPath); // 세레나 원본 데이터 로드
            CharacterSaveData saveData = new CharacterSaveData("CH_SERENA"); // 세레나 저장 데이터 생성
            BattleEquipmentStatBonus bonus = new BattleEquipmentStatBonus(100f, 10f, 11f, 12f, 0.1f, 0.05f, 0.02f, 0.4f, 0.3f); // 전체 장비 보정값 생성

            BattleStats stats = BattleStatsFactory.CreateCharacter(character, saveData, "ALLY_0", bonus); // 장비 포함 런타임 스탯 생성

            Assert.That(stats.MaxHp, Is.EqualTo(character.BaseHp + 100)); // 최대 체력 반영 검증
            Assert.That(stats.Attack, Is.EqualTo(character.BaseAttack + 10)); // 공격력 반영 검증
            Assert.That(stats.Defense, Is.EqualTo(character.BaseDefense + 11)); // 방어력 반영 검증
            Assert.That(stats.Resistance, Is.EqualTo(character.BaseResistance + 12)); // 저항력 반영 검증
            Assert.That(stats.AttackSpeed, Is.EqualTo(character.AttackSpeed + 0.1f).Within(0.0001f)); // 공격속도 반영 검증
            Assert.That(stats.Accuracy, Is.EqualTo(Mathf.Clamp01(character.Accuracy + 0.05f)).Within(0.0001f)); // 명중률 반영 검증
            Assert.That(stats.CriticalRate, Is.EqualTo(Mathf.Clamp01(character.CriticalRate + 0.02f)).Within(0.0001f)); // 치명타율 반영 검증
            Assert.That(stats.AttackRange, Is.EqualTo(Mathf.Max(0.2f, character.AttackRange + 0.4f)).Within(0.0001f)); // 공격 사거리 반영 검증
            Assert.That(stats.MoveSpeed, Is.EqualTo(Mathf.Max(0.01f, character.MoveSpeed + 0.3f)).Within(0.0001f)); // 이동속도 반영 검증
        }

        [Test] // 테스트 표시
        public void TryCalculate_FailsForMissingEquippedInstance() // 손상 장착 인스턴스 실패 검증
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 세레나 새 저장 데이터 생성
            CharacterSaveData characterSave = saveData.FindCharacter("CH_SERENA"); // 세레나 저장 데이터 조회
            characterSave.Equipment.SetInstanceId(EquipmentSlot.Weapon, "EQI_MISSING"); // 존재하지 않는 장비 장착 상태 주입

            bool calculated = BattleEquipmentStatCalculator.TryCalculate(characterSave, saveData, dataManager, out BattleEquipmentStatBonus bonus, out string error); // 손상 장비 보정 계산 시도

            Assert.That(calculated, Is.False); // 장비 보정 계산 실패 검증
            Assert.That(bonus, Is.SameAs(BattleEquipmentStatBonus.Empty)); // 실패 기본 보정값 검증
            Assert.That(error, Does.Contain("EQI_MISSING")); // 누락 장비 오류 ID 검증
        }

        private SaveData CreateEquippedSerena(string weaponEquipmentId, string armorEquipmentId) // 장비 착용 세레나 저장 생성
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 세레나 새 저장 데이터 생성
            bool weaponAdded = saveData.TryAddEquipmentInstance("EQI_TEST_WEAPON", weaponEquipmentId, out string weaponAddError); // 테스트 무기 인스턴스 추가
            bool armorAdded = saveData.TryAddEquipmentInstance("EQI_TEST_ARMOR", armorEquipmentId, out string armorAddError); // 테스트 방어구 인스턴스 추가
            bool weaponEquipped = CharacterEquipmentService.TryEquip(saveData, dataManager, "CH_SERENA", "EQI_TEST_WEAPON", out string weaponEquipError); // 테스트 무기 장착
            bool armorEquipped = CharacterEquipmentService.TryEquip(saveData, dataManager, "CH_SERENA", "EQI_TEST_ARMOR", out string armorEquipError); // 테스트 방어구 장착
            Assert.That(weaponAdded, Is.True, weaponAddError); // 무기 인스턴스 추가 성공 검증
            Assert.That(armorAdded, Is.True, armorAddError); // 방어구 인스턴스 추가 성공 검증
            Assert.That(weaponEquipped, Is.True, weaponEquipError); // 무기 장착 성공 검증
            Assert.That(armorEquipped, Is.True, armorEquipError); // 방어구 장착 성공 검증
            return saveData; // 장비 착용 저장 반환
        }
    }
}
