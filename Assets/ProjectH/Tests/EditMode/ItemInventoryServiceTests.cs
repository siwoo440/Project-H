using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Data; // 아이템 데이터 기능
using ProjectH.SaveSystem; // 아이템 인벤토리 저장 기능
using UnityEditor; // Unity 에디터 에셋 기능
using UnityEngine; // Unity 오브젝트 및 JSON 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class ItemInventoryServiceTests // 일반 아이템 인벤토리 테스트
    {
        private const string CatalogPath = "Assets/ProjectH/Data/Database/ProjectHDataCatalog.asset"; // 데이터 카탈로그 경로
        private GameObject dataObject; // 데이터 관리자 오브젝트
        private DataManager dataManager; // 테스트 데이터 관리자

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 실제 데이터 카탈로그 준비
        {
            dataObject = new GameObject("ItemInventoryServiceTests"); // 데이터 관리자 오브젝트 생성
            dataManager = dataObject.AddComponent<DataManager>(); // 데이터 관리자 컴포넌트 추가
            ProjectHDataCatalog catalog = AssetDatabase.LoadAssetAtPath<ProjectHDataCatalog>(CatalogPath); // 실제 데이터 카탈로그 로드
            dataManager.Configure(catalog); // 테스트 카탈로그 연결
            dataManager.Initialize(); // 데이터 관리자 초기화
            Assert.That(dataManager.IsInitialized, Is.True, string.Join("\n", dataManager.ValidationErrors)); // 데이터 관리자 초기화 검증
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 테스트 오브젝트 정리
        {
            Object.DestroyImmediate(dataObject); // 데이터 관리자 오브젝트 제거
        }

        [Test] // 테스트 표시
        public void TryAdd_StacksConsumableQuantity() // 소비 아이템 수량 누적 검증
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 새 저장 데이터 생성

            bool firstAdded = ItemInventoryService.TryAdd(saveData, dataManager, "IT_POTION_SMALL", 3, out string firstError); // 첫 물약 획득 실행
            bool secondAdded = ItemInventoryService.TryAdd(saveData, dataManager, "IT_POTION_SMALL", 2, out string secondError); // 둘째 물약 획득 실행

            Assert.That(firstAdded, Is.True, firstError); // 첫 획득 성공 검증
            Assert.That(secondAdded, Is.True, secondError); // 둘째 획득 성공 검증
            Assert.That(ItemInventoryService.GetCount(saveData, "IT_POTION_SMALL"), Is.EqualTo(5)); // 물약 누적 수량 검증
        }

        [Test] // 테스트 표시
        public void TryAdd_RejectsMaxStackOverflowWithoutMutation() // 최대 스택 초과 차단 검증
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 새 저장 데이터 생성
            bool initialAdded = ItemInventoryService.TryAdd(saveData, dataManager, "IT_POTION_SMALL", 99, out string initialError); // 최대 수량 물약 획득 실행

            bool overflowAdded = ItemInventoryService.TryAdd(saveData, dataManager, "IT_POTION_SMALL", 1, out string overflowError); // 최대 수량 초과 획득 시도

            Assert.That(initialAdded, Is.True, initialError); // 초기 최대 수량 획득 검증
            Assert.That(overflowAdded, Is.False); // 최대 수량 초과 차단 검증
            Assert.That(overflowError, Does.Contain("MaxStack")); // 최대 수량 오류 문구 검증
            Assert.That(ItemInventoryService.GetCount(saveData, "IT_POTION_SMALL"), Is.EqualTo(99)); // 실패 후 수량 유지 검증
        }

        [Test] // 테스트 표시
        public void TryAdd_RejectsEquipmentItem() // 장비 일반 인벤토리 등록 차단 검증
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 새 저장 데이터 생성

            bool added = ItemInventoryService.TryAdd(saveData, dataManager, "EQ_WEAPON_TRAINING", 1, out string error); // 장비 일반 인벤토리 추가 시도

            Assert.That(added, Is.False); // 장비 일반 인벤토리 등록 차단 검증
            Assert.That(error, Does.Contain("EquipmentInventory")); // 장비 전용 인벤토리 안내 검증
            Assert.That(ItemInventoryService.GetCount(saveData, "EQ_WEAPON_TRAINING"), Is.EqualTo(0)); // 장비 일반 수량 미등록 검증
        }

        [Test] // 테스트 표시
        public void TryRemove_DeletesStackWhenQuantityReachesZero() // 아이템 전량 소비 시 스택 제거 검증
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 새 저장 데이터 생성
            bool added = ItemInventoryService.TryAdd(saveData, dataManager, "IT_MATERIAL_001", 4, out string addError); // 재료 아이템 획득 실행

            bool removed = ItemInventoryService.TryRemove(saveData, dataManager, "IT_MATERIAL_001", 4, out string removeError); // 재료 아이템 전량 제거 실행

            Assert.That(added, Is.True, addError); // 재료 획득 성공 검증
            Assert.That(removed, Is.True, removeError); // 재료 제거 성공 검증
            Assert.That(ItemInventoryService.GetCount(saveData, "IT_MATERIAL_001"), Is.EqualTo(0)); // 재료 수량 0 검증
            Assert.That(saveData.ItemInventory.Count, Is.EqualTo(0)); // 빈 스택 제거 검증
        }

        [Test] // 테스트 표시
        public void JsonRoundTrip_RetainsItemStackQuantities() // JSON 저장 왕복 아이템 수량 유지 검증
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 새 저장 데이터 생성
            bool potionAdded = ItemInventoryService.TryAdd(saveData, dataManager, "IT_POTION_SMALL", 7, out string potionError); // 물약 획득 실행
            bool questAdded = ItemInventoryService.TryAdd(saveData, dataManager, "IT_QUEST_KEY_001", 1, out string questError); // 퀘스트 아이템 획득 실행
            string json = JsonUtility.ToJson(saveData, true); // 저장 데이터 JSON 직렬화
            SaveData loaded = JsonUtility.FromJson<SaveData>(json); // 저장 데이터 JSON 역직렬화
            loaded.EnsureDefaults(); // 이전 저장 기본값 복원

            Assert.That(potionAdded, Is.True, potionError); // 물약 획득 성공 검증
            Assert.That(questAdded, Is.True, questError); // 퀘스트 아이템 획득 성공 검증
            Assert.That(ItemInventoryService.GetCount(loaded, "IT_POTION_SMALL"), Is.EqualTo(7)); // 재로드 물약 수량 검증
            Assert.That(ItemInventoryService.GetCount(loaded, "IT_QUEST_KEY_001"), Is.EqualTo(1)); // 재로드 퀘스트 아이템 수량 검증
        }

        [Test] // 테스트 표시
        public void EnsureDefaults_OldSaveWithoutItemInventoryCreatesEmptyInventory() // 이전 저장 일반 인벤토리 호환 검증
        {
            SaveData loaded = JsonUtility.FromJson<SaveData>("{\"saveVersion\":1}"); // 일반 인벤토리 없는 이전 저장 역직렬화

            loaded.EnsureDefaults(); // 이전 저장 기본값 복원

            Assert.That(loaded.ItemInventory, Is.Not.Null); // 일반 아이템 인벤토리 복원 검증
            Assert.That(loaded.ItemInventory.Count, Is.EqualTo(0)); // 이전 저장 빈 인벤토리 검증
        }
    }
}
