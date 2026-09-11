using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Data; // 데이터 관리자·상점 기능
using ProjectH.SaveSystem; // 룬·저장 기능
using UnityEditor; // 에셋 로드 기능
using UnityEngine; // Unity 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class RuneServiceTests // Day60 룬 획득·장착·강화·합성·분해 테스트
    {
        private const string Serena = "CH_SERENA"; // 테스트 캐릭터
        private GameObject dataObject; // 데이터 관리자 오브젝트
        private DataManager dataManager; // 실제 카탈로그 데이터 관리자

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 실제 카탈로그 준비 (룬 조각·상자 GUID 등록 검증 포함)
        {
            dataObject = new GameObject("RuneServiceTests"); // 오브젝트 생성
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

        private SaveData CreateSave(int gold = 0, int shards = 0) // 골드·조각 보유 새 게임
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { Serena, "CH_ELLEN" }); // 새 게임
            if (gold > 0) GoldCurrencyService.AddGold(saveData, gold); // 골드
            if (shards > 0) Assert.That(ItemInventoryService.TryAdd(saveData, dataManager, RuneCatalog.ShardItemId, shards, out string error), Is.True, error); // 조각
            return saveData; // 반환
        }

        [Test] // 룬 아이템 등록·상점 판매 검증
        public void RuneItems_AreRegisteredAndSoldInLobbyShop() // 데이터 테스트
        {
            Assert.That(dataManager.GetItem(RuneCatalog.ShardItemId).Type, Is.EqualTo(ItemType.Material)); // 조각 재료
            Assert.That(dataManager.GetItem(RuneCatalog.Box1ItemId).Type, Is.EqualTo(ItemType.Consumable)); // 상자 소비 (가방에서 사용)
            ShopData shop = AssetDatabase.LoadAssetAtPath<ShopData>("Assets/ProjectH/Resources/Shops/SHOP_LOBBY.asset"); // 로비 상점
            Assert.That(shop.FindProduct("PRD_RUNE_BOX_1").BuyPrice, Is.EqualTo(100)); // 기획서 ★1 룬 100G
            Assert.That(shop.FindProduct("PRD_RUNE_BOX_2").BuyPrice, Is.EqualTo(200)); // 기획서 ★2 룬 200G
        }

        [Test] // 상자 열기 검증
        public void OpenBox_ConsumesBoxAndGrantsRuneOfBoxGrade() // 상자 테스트
        {
            SaveData saveData = CreateSave(); // 새 게임
            ItemInventoryService.TryAdd(saveData, dataManager, RuneCatalog.Box1ItemId, 1, out _); // 하급 상자
            ItemInventoryService.TryAdd(saveData, dataManager, RuneCatalog.Box2ItemId, 1, out _); // 중급 상자

            Assert.That(RuneService.TryOpenBox(saveData, dataManager, RuneCatalog.Box1ItemId, out RuneInstanceSaveData low, out string message, new System.Random(7)), Is.True, message); // 하급 열기
            Assert.That(RuneService.TryOpenBox(saveData, dataManager, RuneCatalog.Box2ItemId, out RuneInstanceSaveData mid, out _, new System.Random(7)), Is.True); // 중급 열기
            Assert.That(low.Grade, Is.EqualTo(1)); // ★1 검증
            Assert.That(mid.Grade, Is.EqualTo(2)); // ★2 검증
            Assert.That(saveData.RuneInventory.Count, Is.EqualTo(2)); // 룬 2개 검증
            Assert.That(ItemInventoryService.GetCount(saveData, RuneCatalog.Box1ItemId), Is.EqualTo(0)); // 상자 소모 검증
            Assert.That(RuneService.TryOpenBox(saveData, dataManager, RuneCatalog.Box1ItemId, out _, out _), Is.False); // 상자 없음 실패 검증
        }

        [Test] // 슬롯 해금 조건 검증
        public void Slots_UnlockByLevelAndBond() // 슬롯 테스트
        {
            SaveData saveData = CreateSave(); // 새 게임
            RuneInstanceSaveData rune = RuneService.Grant(saveData, RuneKind.Power, 1); // 룬

            Assert.That(RuneService.TryEquip(saveData, dataManager, Serena, rune.InstanceId, 1, out string locked), Is.False); // 2번 잠김
            Assert.That(locked, Does.Contain("레벨 10")); // 조건 안내
            saveData.FindCharacter(Serena).SetLevel(10); // 레벨 10
            Assert.That(RuneService.TryEquip(saveData, dataManager, Serena, rune.InstanceId, 1, out string message), Is.True, message); // 2번 장착
            Assert.That(RuneService.IsSlotUnlocked(saveData, dataManager, Serena, 2), Is.False); // 3번 잠김 (결속 3 필요)
            saveData.FindCharacter(Serena).SetBondLevel(3); // 결속 3
            Assert.That(RuneService.IsSlotUnlocked(saveData, dataManager, Serena, 2), Is.True); // 3번 해금
        }

        [Test] // 특수 룬 1개 제한·교체 검증
        public void Equip_UniqueRuneOnce_AndOccupiedSlotSwaps() // 장착 규칙 테스트
        {
            SaveData saveData = CreateSave(); // 새 게임
            saveData.FindCharacter(Serena).SetLevel(10); // 슬롯 2개
            RuneInstanceSaveData reviveA = RuneService.Grant(saveData, RuneKind.Revive, 1); // 부활 A
            RuneInstanceSaveData reviveB = RuneService.Grant(saveData, RuneKind.Revive, 2); // 부활 B
            RuneInstanceSaveData power = RuneService.Grant(saveData, RuneKind.Power, 1); // 힘

            Assert.That(RuneService.TryEquip(saveData, dataManager, Serena, reviveA.InstanceId, 0, out _), Is.True); // 부활 A 장착
            Assert.That(RuneService.TryEquip(saveData, dataManager, Serena, reviveB.InstanceId, 1, out string unique), Is.False); // 부활 B 거부
            Assert.That(unique, Does.Contain("1개")); // 안내
            Assert.That(RuneService.TryEquip(saveData, dataManager, Serena, power.InstanceId, 0, out _), Is.True); // 같은 칸 교체
            Assert.That(reviveA.IsEquipped, Is.False); // 기존 룬 해제 검증
            Assert.That(RuneService.GetEquipped(saveData, Serena)[0], Is.SameAs(power)); // 교체 검증
        }

        [Test] // 강화 비용·수치 검증
        public void Enhance_ConsumesMaterialsAndRaisesValue() // 강화 테스트
        {
            SaveData saveData = CreateSave(500, 10); // 골드 500 · 조각 10
            RuneInstanceSaveData rune = RuneService.Grant(saveData, RuneKind.Power, 1); // ★1 힘 Lv.1

            Assert.That(RuneService.TryEnhance(saveData, dataManager, rune.InstanceId, out string message), Is.True, message); // Lv.2
            Assert.That(rune.Level, Is.EqualTo(2)); // 레벨 검증
            Assert.That(ItemInventoryService.GetCount(saveData, RuneCatalog.ShardItemId), Is.EqualTo(9)); // 조각 1 소모
            Assert.That(GoldCurrencyService.GetGold(saveData), Is.EqualTo(450)); // 50G 소모
            Assert.That(RuneCatalog.GetValue(RuneKind.Power, 1, 2), Is.EqualTo(0.105f).Within(0.0001f)); // 10% × 1.05
            Assert.That(RuneCatalog.GetValue(RuneKind.Power, 3, 10), Is.EqualTo(0.29f).Within(0.0001f)); // ★3 Lv.10 = 20% × 1.45
        }

        [Test] // 재료 부족·최대 강화 거부 검증
        public void Enhance_WithoutMaterialsOrAtMax_Fails() // 강화 실패 테스트
        {
            SaveData saveData = CreateSave(); // 재료 없음
            RuneInstanceSaveData rune = RuneService.Grant(saveData, RuneKind.Guard, 1); // 수비 룬

            Assert.That(RuneService.TryEnhance(saveData, dataManager, rune.InstanceId, out string message), Is.False); // 실패
            Assert.That(message, Does.Contain("부족")); // 안내
            Assert.That(rune.Level, Is.EqualTo(1)); // 레벨 유지

            SaveData rich = CreateSave(100000, 500); // 재료 충분
            RuneInstanceSaveData max = RuneService.Grant(rich, RuneKind.Guard, 1); // 수비 룬
            for (int count = 0; count < 9; count++) RuneService.TryEnhance(rich, dataManager, max.InstanceId, out _); // Lv.10까지
            Assert.That(max.Level, Is.EqualTo(RuneCatalog.MaxLevel)); // Lv.10 검증
            Assert.That(RuneService.TryEnhance(rich, dataManager, max.InstanceId, out _), Is.False); // 초과 거부
        }

        [Test] // 합성 검증
        public void Synthesize_SameKindAndGrade_MakesHigherGradeWithRefund() // 합성 테스트
        {
            SaveData saveData = CreateSave(1000, 20); // 재료
            RuneInstanceSaveData a = RuneService.Grant(saveData, RuneKind.Critical, 1); // 치명타 A
            RuneInstanceSaveData b = RuneService.Grant(saveData, RuneKind.Critical, 1); // 치명타 B
            RuneService.TryEnhance(saveData, dataManager, a.InstanceId, out _); // A Lv.2 (조각 1)
            RuneService.TryEnhance(saveData, dataManager, a.InstanceId, out _); // A Lv.3 (조각 2)
            int shardsBefore = ItemInventoryService.GetCount(saveData, RuneCatalog.ShardItemId); // 합성 전 조각 (17)
            int goldBefore = GoldCurrencyService.GetGold(saveData); // 합성 전 골드

            Assert.That(RuneService.TrySynthesize(saveData, dataManager, a.InstanceId, b.InstanceId, out RuneInstanceSaveData result, out string message), Is.True, message); // 합성
            Assert.That(result.Grade, Is.EqualTo(2)); // ★2 검증
            Assert.That(result.Level, Is.EqualTo(1)); // Lv.1 검증
            Assert.That(saveData.RuneInventory.Count, Is.EqualTo(1)); // 재료 2개 → 1개
            Assert.That(ItemInventoryService.GetCount(saveData, RuneCatalog.ShardItemId), Is.EqualTo(shardsBefore + 1)); // 투자 조각 3의 50% = 1 반환
            Assert.That(GoldCurrencyService.GetGold(saveData), Is.EqualTo(goldBefore - 100)); // 합성 100G
        }

        [Test] // 합성 불가 조건 검증
        public void Synthesize_RejectsMismatchLockedOrEquipped() // 합성 실패 테스트
        {
            SaveData saveData = CreateSave(1000); // 골드
            RuneInstanceSaveData power = RuneService.Grant(saveData, RuneKind.Power, 1); // 힘 ★1
            RuneInstanceSaveData guard = RuneService.Grant(saveData, RuneKind.Guard, 1); // 수비 ★1
            RuneInstanceSaveData power2 = RuneService.Grant(saveData, RuneKind.Power, 2); // 힘 ★2
            RuneInstanceSaveData powerB = RuneService.Grant(saveData, RuneKind.Power, 1); // 힘 ★1

            Assert.That(RuneService.TrySynthesize(saveData, dataManager, power.InstanceId, guard.InstanceId, out _, out _), Is.False); // 종류 다름
            Assert.That(RuneService.TrySynthesize(saveData, dataManager, power.InstanceId, power2.InstanceId, out _, out _), Is.False); // 등급 다름
            RuneService.ToggleLock(saveData, powerB.InstanceId); // 잠금
            Assert.That(RuneService.TrySynthesize(saveData, dataManager, power.InstanceId, powerB.InstanceId, out _, out string locked), Is.False); // 잠금 거부
            Assert.That(locked, Does.Contain("잠긴")); // 안내
            RuneService.ToggleLock(saveData, powerB.InstanceId); // 잠금 해제
            RuneService.TryEquip(saveData, dataManager, Serena, powerB.InstanceId, 0, out _); // 장착
            Assert.That(RuneService.TrySynthesize(saveData, dataManager, power.InstanceId, powerB.InstanceId, out _, out string equipped), Is.False); // 장착 거부
            Assert.That(equipped, Does.Contain("장착")); // 안내
            Assert.That(saveData.RuneInventory.Count, Is.EqualTo(4)); // 변화 없음
        }

        [Test] // 분해 검증
        public void Dismantle_GivesShards_LockedIsProtected() // 분해 테스트
        {
            SaveData saveData = CreateSave(); // 새 게임
            RuneInstanceSaveData rune = RuneService.Grant(saveData, RuneKind.Regen, 2); // 재생 ★2
            RuneInstanceSaveData locked = RuneService.Grant(saveData, RuneKind.Regen, 3); // 재생 ★3
            RuneService.ToggleLock(saveData, locked.InstanceId); // 잠금

            Assert.That(RuneService.TryDismantle(saveData, dataManager, rune.InstanceId, out string message), Is.True, message); // 분해
            Assert.That(ItemInventoryService.GetCount(saveData, RuneCatalog.ShardItemId), Is.EqualTo(5)); // ★2 조각 5
            Assert.That(RuneService.TryDismantle(saveData, dataManager, locked.InstanceId, out _), Is.False); // 잠금 보호
            Assert.That(saveData.RuneInventory.Count, Is.EqualTo(1)); // 잠금 룬만 남음
        }

        [Test] // 장착 룬 합계 검증
        public void BuildLoadout_SumsOnlyEquippedRunes() // 합계 테스트
        {
            SaveData saveData = CreateSave(); // 새 게임
            saveData.FindCharacter(Serena).SetLevel(10); // 슬롯 2개
            RuneInstanceSaveData a = RuneService.Grant(saveData, RuneKind.Power, 1); // 힘 ★1 (10%)
            RuneInstanceSaveData b = RuneService.Grant(saveData, RuneKind.Power, 2); // 힘 ★2 (15%)
            RuneService.Grant(saveData, RuneKind.Power, 3); // 미장착 ★3
            RuneService.TryEquip(saveData, dataManager, Serena, a.InstanceId, 0, out _); // 1번
            RuneService.TryEquip(saveData, dataManager, Serena, b.InstanceId, 1, out _); // 2번

            RuneLoadout loadout = RuneService.BuildLoadout(saveData, Serena); // 합계
            Assert.That(loadout.Get(RuneKind.Power), Is.EqualTo(0.25f).Within(0.0001f)); // 10 + 15 (미장착 제외)
            Assert.That(RuneService.BuildLoadout(saveData, "CH_ELLEN").IsEmpty, Is.True); // 다른 캐릭터 없음
        }

        [Test] // 저장 왕복·이전 세이브 호환 검증
        public void RuneInventory_SurvivesJsonAndOldSaveIsEmpty() // 저장 테스트
        {
            SaveData saveData = CreateSave(); // 새 게임
            RuneInstanceSaveData rune = RuneService.Grant(saveData, RuneKind.Thorns, 2); // 가시 ★2
            RuneService.TryEquip(saveData, dataManager, Serena, rune.InstanceId, 0, out _); // 장착

            SaveData loaded = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(saveData)); // 왕복
            RuneInstanceSaveData restored = loaded.FindRune(rune.InstanceId); // 복원 룬
            Assert.That(restored.Kind, Is.EqualTo(RuneKind.Thorns)); // 종류 유지
            Assert.That(restored.EquippedCharacterId, Is.EqualTo(Serena)); // 장착 유지

            SaveData old = JsonUtility.FromJson<SaveData>("{\"characters\":[{\"characterId\":\"CH_SERENA\"}]}"); // Day60 이전 세이브
            old.EnsureDefaults(); // 보정
            Assert.That(old.RuneInventory.Count, Is.EqualTo(0)); // 빈 목록
        }
    }
}
