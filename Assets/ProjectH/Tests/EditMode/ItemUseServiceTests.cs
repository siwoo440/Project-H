using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Data; // 아이템 데이터 기능
using ProjectH.SaveSystem; // 아이템 사용 기능
using UnityEditor; // Unity 에디터 에셋 기능
using UnityEngine; // Unity 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class ItemUseServiceTests // 일반 아이템 사용 서비스 테스트
    {
        private const string CatalogPath = "Assets/ProjectH/Data/Database/ProjectHDataCatalog.asset"; // 데이터 카탈로그 경로
        private GameObject dataObject; // 데이터 관리자 오브젝트
        private DataManager dataManager; // 테스트 데이터 관리자

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 실제 데이터 카탈로그 준비
        {
            dataObject = new GameObject("ItemUseServiceTests"); // 데이터 관리자 오브젝트 생성
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
        public void TryUse_ConsumableConsumesExactlyOne() // 소비 아이템 1개 사용 검증
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 새 저장 데이터 생성
            bool added = ItemInventoryService.TryAdd(saveData, dataManager, "IT_POTION_SMALL", 2, out string addError); // 물약 두 개 획득 실행

            bool used = ItemUseService.TryUse(saveData, dataManager, "IT_POTION_SMALL", out int remainingCount, out string useError); // 물약 한 개 사용 실행

            Assert.That(added, Is.True, addError); // 물약 획득 성공 검증
            Assert.That(used, Is.True, useError); // 물약 사용 성공 검증
            Assert.That(remainingCount, Is.EqualTo(1)); // 사용 후 남은 수량 검증
            Assert.That(ItemInventoryService.GetCount(saveData, "IT_POTION_SMALL"), Is.EqualTo(1)); // 저장 데이터 수량 검증
        }

        [Test] // 테스트 표시
        public void TryUse_MaterialIsRejectedWithoutConsumption() // 재료 아이템 직접 사용 차단 검증
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 새 저장 데이터 생성
            bool added = ItemInventoryService.TryAdd(saveData, dataManager, "IT_MATERIAL_001", 3, out string addError); // 재료 세 개 획득 실행

            bool used = ItemUseService.TryUse(saveData, dataManager, "IT_MATERIAL_001", out int remainingCount, out string useError); // 재료 사용 시도

            Assert.That(added, Is.True, addError); // 재료 획득 성공 검증
            Assert.That(used, Is.False); // 재료 직접 사용 차단 검증
            Assert.That(useError, Does.Contain("Consumable")); // 사용 불가 유형 안내 검증
            Assert.That(remainingCount, Is.EqualTo(3)); // 실패 후 남은 수량 검증
        }

        [Test] // 테스트 표시
        public void TryUse_QuestItemIsRejectedWithoutConsumption() // 퀘스트 아이템 직접 사용 차단 검증
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 새 저장 데이터 생성
            bool added = ItemInventoryService.TryAdd(saveData, dataManager, "IT_QUEST_KEY_001", 1, out string addError); // 퀘스트 아이템 획득 실행

            bool used = ItemUseService.TryUse(saveData, dataManager, "IT_QUEST_KEY_001", out int remainingCount, out string useError); // 퀘스트 아이템 사용 시도

            Assert.That(added, Is.True, addError); // 퀘스트 아이템 획득 성공 검증
            Assert.That(used, Is.False); // 퀘스트 아이템 직접 사용 차단 검증
            Assert.That(useError, Does.Contain("Consumable")); // 퀘스트 아이템 사용 불가 안내 검증
            Assert.That(remainingCount, Is.EqualTo(1)); // 실패 후 퀘스트 아이템 수량 검증
        }

        [Test] // 테스트 표시
        public void TryUse_UnownedConsumableIsRejected() // 미보유 소비 아이템 사용 차단 검증
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 새 저장 데이터 생성

            bool used = ItemUseService.TryUse(saveData, dataManager, "IT_POTION_SMALL", out int remainingCount, out string useError); // 미보유 물약 사용 시도

            Assert.That(used, Is.False); // 미보유 물약 사용 차단 검증
            Assert.That(useError, Does.Contain("보유")); // 미보유 오류 안내 검증
            Assert.That(remainingCount, Is.EqualTo(0)); // 미보유 수량 검증
        }
    }
}
