using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 장비 보정 계산 기능
using ProjectH.Data; // 장비 데이터 기능
using ProjectH.SaveSystem; // 강화·초월 기능
using UnityEditor; // 에셋 로드 기능
using UnityEngine; // Unity 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class EquipmentUpgradeTests // Day61 장비 강화 +0~+5 · 초월 ★1~★3 테스트
    {
        private const string Serena = "CH_SERENA"; // 테스트 캐릭터
        private const string Weapon = "EQ_WEAPON_IRON"; // 테스트 무기 (공격력 35)
        private const string WeaponScrollC = "IT_SCROLL_WEAPON_C"; // 무기 주문서 C
        private GameObject dataObject; // 데이터 관리자 오브젝트
        private DataManager dataManager; // 실제 카탈로그 데이터 관리자

        private sealed class FixedRandom : System.Random // 항상 같은 값을 내는 난수 (성공·실패 강제)
        {
            private readonly double value; // 고정 값

            public FixedRandom(double fixedValue) // 생성
            {
                value = fixedValue; // 값 저장
            }

            public override double NextDouble() => value; // 고정 값 반환
        }

        private static readonly System.Random AlwaysSuccess = new FixedRandom(0.0); // 항상 성공
        private static readonly System.Random AlwaysFail = new FixedRandom(0.999); // 100% 미만이면 실패

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 실제 카탈로그 준비 (주문서 GUID 등록 검증 포함)
        {
            dataObject = new GameObject("EquipmentUpgradeTests"); // 오브젝트 생성
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

        private SaveData CreateSave(int gold, int scrolls) // 골드·무기 주문서 C를 가진 새 게임
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { Serena }); // 새 게임
            GoldCurrencyService.TrySpendGold(saveData, GoldCurrencyService.GetGold(saveData), out _); // 시작 골드 비우기
            GoldCurrencyService.AddGold(saveData, gold); // 골드 지급
            if (scrolls > 0) ItemInventoryService.TryAdd(saveData, dataManager, WeaponScrollC, scrolls, out _); // 주문서 지급
            return saveData; // 저장 반환
        }

        private static EquipmentInstanceSaveData CreateWeapon(SaveData saveData) // 무기 인스턴스 생성
        {
            Assert.That(saveData.TryCreateEquipmentInstance(Weapon, out EquipmentInstanceSaveData instance, out string error), Is.True, error); // 생성
            return instance; // 반환
        }

        private void EnhanceTo(SaveData saveData, EquipmentInstanceSaveData instance, int level) // 목표 단계까지 강제 성공
        {
            while (instance.EnhanceLevel < level) // 목표까지
            {
                Assert.That(EquipmentUpgradeService.TryEnhance(saveData, dataManager, instance.InstanceId, WeaponScrollC, out string message, AlwaysSuccess), Is.EqualTo(EnhanceOutcome.Success), message); // 성공
            }
        }

        [Test] // 비용·확률·배율 표 검증
        public void Catalog_CostsChancesAndMultipliers() // 수치 테스트
        {
            Assert.That(EquipmentUpgradeCatalog.GetEnhanceGold(0), Is.EqualTo(500)); // +0→+1 500G (기획서)
            Assert.That(EquipmentUpgradeCatalog.GetEnhanceGold(4), Is.EqualTo(2500)); // +4→+5 2500G
            Assert.That(EquipmentUpgradeCatalog.GetEnhanceGold(5), Is.EqualTo(0)); // 최대
            Assert.That(EquipmentUpgradeCatalog.GetTranscendGold(1), Is.EqualTo(20000)); // ★1→★2 20,000G (기획서)
            Assert.That(EquipmentUpgradeCatalog.GetTranscendGold(2), Is.EqualTo(30000)); // ★2→★3 30,000G
            Assert.That(EquipmentUpgradeCatalog.GetSuccessChance(2, ScrollGrade.C, 0), Is.EqualTo(0.9f).Within(0.0001f)); // +2→+3 90%
            Assert.That(EquipmentUpgradeCatalog.GetSuccessChance(4, ScrollGrade.C, 0), Is.EqualTo(0.6f).Within(0.0001f)); // +4→+5 60%
            Assert.That(EquipmentUpgradeCatalog.GetSuccessChance(4, ScrollGrade.A, 0), Is.EqualTo(0.9f).Within(0.0001f)); // A 주문서 +30%
            Assert.That(EquipmentUpgradeCatalog.GetSuccessChance(4, ScrollGrade.C, 2), Is.EqualTo(0.8f).Within(0.0001f)); // 실패 2회 보정 +20%
            Assert.That(EquipmentUpgradeCatalog.GetSuccessChance(3, ScrollGrade.A, 1), Is.EqualTo(1f)); // 100% 상한
            Assert.That(EquipmentUpgradeCatalog.GetStatMultiplier(0, 1), Is.EqualTo(1f)); // +0 ★1 = 1배
            Assert.That(EquipmentUpgradeCatalog.GetStatMultiplier(5, 3), Is.EqualTo(3f).Within(0.0001f)); // +5 ★3 = 2 × 1.5 = 3배
        }

        [Test] // 주문서 등록·상점 판매 검증
        public void Scrolls_AreRegisteredAndSoldInLobbyShop() // 데이터 테스트
        {
            foreach (string id in new[] { "IT_SCROLL_WEAPON_C", "IT_SCROLL_WEAPON_B", "IT_SCROLL_WEAPON_A", "IT_SCROLL_ARMOR_C", "IT_SCROLL_ARMOR_B", "IT_SCROLL_ARMOR_A" }) // 주문서 6종
            {
                Assert.That(dataManager.GetItem(id), Is.Not.Null, id); // 카탈로그 등록
                Assert.That(dataManager.GetItem(id).Type, Is.EqualTo(ItemType.Material), id); // 재료
            }

            ShopData shop = AssetDatabase.LoadAssetAtPath<ShopData>("Assets/ProjectH/Resources/Shops/SHOP_LOBBY.asset"); // 로비 상점
            Assert.That(shop.FindProduct("PRD_SCROLL_WEAPON_C").Rotating, Is.False); // C는 상시 상품
            Assert.That(shop.FindProduct("PRD_SCROLL_WEAPON_A").Rotating, Is.True); // A는 오늘의 상품
            Assert.That(EquipmentUpgradeCatalog.GetScrollItemId(EquipmentSlot.Boots, ScrollGrade.B), Is.EqualTo("IT_SCROLL_ARMOR_B")); // 방어구류는 방어구 주문서
        }

        [Test] // 강화 성공 : 주문서 1장 + 골드 소모
        public void Enhance_Success_ConsumesScrollAndGold() // 성공 테스트
        {
            SaveData saveData = CreateSave(10000, 3); // 골드 10000 · 주문서 3
            EquipmentInstanceSaveData weapon = CreateWeapon(saveData); // 무기

            Assert.That(EquipmentUpgradeService.TryEnhance(saveData, dataManager, weapon.InstanceId, WeaponScrollC, out string message, AlwaysSuccess), Is.EqualTo(EnhanceOutcome.Success), message); // 강화
            Assert.That(weapon.EnhanceLevel, Is.EqualTo(1)); // +1
            Assert.That(ItemInventoryService.GetCount(saveData, WeaponScrollC), Is.EqualTo(2)); // 주문서 1 소모
            Assert.That(GoldCurrencyService.GetGold(saveData), Is.EqualTo(9500)); // 500G 소모
        }

        [Test] // 강화 실패 : 주문서만 소모·장비 유지·확률 보정 누적
        public void Enhance_Fail_ConsumesOnlyScroll_AndStacksBonus() // 실패 테스트
        {
            SaveData saveData = CreateSave(10000, 10); // 넉넉한 재료
            EquipmentInstanceSaveData weapon = CreateWeapon(saveData); // 무기
            EnhanceTo(saveData, weapon, 3); // +3 (500+1000+1500 = 3000G)
            int gold = GoldCurrencyService.GetGold(saveData); // 실패 전 골드

            Assert.That(EquipmentUpgradeService.TryEnhance(saveData, dataManager, weapon.InstanceId, WeaponScrollC, out _, AlwaysFail), Is.EqualTo(EnhanceOutcome.Failed)); // +3→+4 실패
            Assert.That(weapon.EnhanceLevel, Is.EqualTo(3)); // 단계 유지 (파괴·하락 없음)
            Assert.That(weapon.FailStreak, Is.EqualTo(1)); // 실패 누적
            Assert.That(GoldCurrencyService.GetGold(saveData), Is.EqualTo(gold)); // 골드 유지
            Assert.That(ItemInventoryService.GetCount(saveData, WeaponScrollC), Is.EqualTo(6)); // 주문서만 1 소모 (10 - 3 - 1)
            Assert.That(EquipmentUpgradeCatalog.GetSuccessChance(3, ScrollGrade.C, weapon.FailStreak), Is.EqualTo(0.85f).Within(0.0001f)); // 75% + 10%
            EnhanceTo(saveData, weapon, 4); // 다음 성공
            Assert.That(weapon.FailStreak, Is.EqualTo(0)); // 성공 시 보정 초기화
        }

        [Test] // 조건 미충족 : 아무것도 소모하지 않음
        public void Enhance_Rejected_WrongScrollOrMissingGold() // 거부 테스트
        {
            SaveData saveData = CreateSave(100, 2); // 골드 100 (부족)
            ItemInventoryService.TryAdd(saveData, dataManager, "IT_SCROLL_ARMOR_C", 1, out _); // 방어구 주문서
            EquipmentInstanceSaveData weapon = CreateWeapon(saveData); // 무기

            Assert.That(EquipmentUpgradeService.TryEnhance(saveData, dataManager, weapon.InstanceId, "IT_SCROLL_ARMOR_C", out string wrong, AlwaysSuccess), Is.EqualTo(EnhanceOutcome.Rejected)); // 부위 불일치
            Assert.That(wrong, Does.Contain("무기 강화 주문서")); // 안내
            Assert.That(EquipmentUpgradeService.TryEnhance(saveData, dataManager, weapon.InstanceId, WeaponScrollC, out _, AlwaysSuccess), Is.EqualTo(EnhanceOutcome.Rejected)); // 골드 부족
            Assert.That(ItemInventoryService.GetCount(saveData, WeaponScrollC), Is.EqualTo(2)); // 주문서 유지
            Assert.That(ItemInventoryService.GetCount(saveData, "IT_SCROLL_ARMOR_C"), Is.EqualTo(1)); // 방어구 주문서 유지
            Assert.That(weapon.EnhanceLevel, Is.EqualTo(0)); // 단계 유지
        }

        [Test] // 초월 : +5 필요 · 같은 장비 1개 + 골드 · 강화 +0 초기화 · 배율 상승
        public void Transcend_RequiresPlusFive_ConsumesDuplicate_AndResetsEnhance() // 초월 테스트
        {
            SaveData saveData = CreateSave(40000, 10); // 골드 40000
            EquipmentInstanceSaveData weapon = CreateWeapon(saveData); // 대상 무기
            CreateWeapon(saveData); // 재료 무기

            Assert.That(EquipmentUpgradeService.TryTranscend(saveData, dataManager, weapon.InstanceId, out string early), Is.False); // +0에서는 불가
            Assert.That(early, Does.Contain("+5")); // 안내
            EnhanceTo(saveData, weapon, 5); // +5 (7500G)
            Assert.That(EquipmentUpgradeService.TryTranscend(saveData, dataManager, weapon.InstanceId, out string message), Is.True, message); // 초월

            Assert.That(weapon.TranscendStage, Is.EqualTo(2)); // ★2
            Assert.That(weapon.EnhanceLevel, Is.EqualTo(0)); // +0 초기화
            Assert.That(saveData.GetEquipmentCount(Weapon), Is.EqualTo(1)); // 재료 무기 소모
            Assert.That(GoldCurrencyService.GetGold(saveData), Is.EqualTo(40000 - 7500 - 20000)); // 강화·초월 골드 소모
            Assert.That(EquipmentUpgradeCatalog.GetStatMultiplier(weapon), Is.EqualTo(1.5f).Within(0.0001f)); // ★2 +0 = 1.5배
            Assert.That(EquipmentUpgradeService.TryTranscend(saveData, dataManager, weapon.InstanceId, out _), Is.False); // 다시 +5 전까지 불가
        }

        [Test] // 장착 중인 장비는 초월 재료가 되지 않음
        public void Transcend_IgnoresEquippedDuplicate() // 재료 보호 테스트
        {
            SaveData saveData = CreateSave(0, 0); // 새 게임
            EquipmentInstanceSaveData target = CreateWeapon(saveData); // 대상
            EquipmentInstanceSaveData equipped = CreateWeapon(saveData); // 장착할 같은 무기
            Assert.That(CharacterEquipmentService.TryEquip(saveData, dataManager, Serena, equipped.InstanceId, out string error), Is.True, error); // 세레나 장착

            Assert.That(EquipmentUpgradeService.FindTranscendMaterial(saveData, target), Is.Null); // 재료 없음
        }

        [Test] // 전투 능력치에 강화·초월 배율 반영 + 룬 5번 슬롯 ★2 해금
        public void Upgrades_ApplyToBattleStats_AndUnlockRuneSlotFive() // 연계 테스트
        {
            SaveData saveData = CreateSave(40000, 10); // 넉넉한 재료
            EquipmentInstanceSaveData weapon = CreateWeapon(saveData); // 무기
            CreateWeapon(saveData); // 재료 무기
            Assert.That(CharacterEquipmentService.TryEquip(saveData, dataManager, Serena, weapon.InstanceId, out string error), Is.True, error); // 장착
            CharacterSaveData serena = saveData.FindCharacter(Serena); // 세레나 저장

            Assert.That(BattleEquipmentStatCalculator.TryCalculate(serena, saveData, dataManager, out BattleEquipmentStatBonus plain, out _), Is.True); // +0 계산
            Assert.That(plain.Attack, Is.EqualTo(35f).Within(0.001f)); // 기본 공격력 35
            Assert.That(RuneService.IsSlotUnlocked(saveData, dataManager, Serena, 4), Is.False); // 5번 잠김
            EnhanceTo(saveData, weapon, 5); // +5
            Assert.That(BattleEquipmentStatCalculator.TryCalculate(serena, saveData, dataManager, out BattleEquipmentStatBonus plusFive, out _), Is.True); // +5 계산
            Assert.That(plusFive.Attack, Is.EqualTo(52.5f).Within(0.001f)); // 35 × 1.5
            Assert.That(EquipmentUpgradeService.TryTranscend(saveData, dataManager, weapon.InstanceId, out string message), Is.True, message); // ★2
            Assert.That(RuneService.IsSlotUnlocked(saveData, dataManager, Serena, 4), Is.True); // ★2 장비 착용 → 5번 해금
            Assert.That(RuneCatalog.GetSlotRequirement(4), Does.Contain("★2")); // 조건 문구
        }

        [Test] // 이전 세이브 장비는 +0 ★1로 불러옴
        public void OldSave_Equipment_LoadsAsPlusZeroStarOne() // 호환 테스트
        {
            SaveData saveData = JsonUtility.FromJson<SaveData>("{\"equipmentInventory\":[{\"instanceId\":\"EQI_A\",\"equipmentId\":\"EQ_WEAPON_IRON\"}]}"); // Day61 이전 세이브
            saveData.EnsureDefaults(); // 보정
            EquipmentInstanceSaveData instance = saveData.FindEquipmentInstance("EQI_A"); // 장비

            Assert.That(instance.EnhanceLevel, Is.EqualTo(0)); // +0
            Assert.That(instance.TranscendStage, Is.EqualTo(1)); // ★1 (0 저장값 보정)
            Assert.That(EquipmentUpgradeCatalog.FormatName("철제 검", instance), Is.EqualTo("철제 검")); // 표시 이름 그대로
        }
    }
}
