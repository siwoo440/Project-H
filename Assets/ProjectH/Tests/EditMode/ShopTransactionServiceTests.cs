using System.Collections.Generic; // 테스트 목록 기능
using System.Reflection; // 테스트 전용 private 필드 설정 기능
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Data; // 아이템 및 상점 데이터 기능
using ProjectH.SaveSystem; // 저장 및 Gold 기능
using ProjectH.Shop; // 상점 거래 기능
using UnityEngine; // Unity 테스트 객체 기능

namespace ProjectH.Tests.EditMode // EditMode 테스트 영역
{
    public sealed class ShopTransactionServiceTests // Day40 상점 거래 회귀 테스트
    {
        [Test] // 일반 아이템 구매 성공 테스트
        public void TryPurchase_ValidItem_DeductsGoldAndAddsInventory() // 구매 거래 원자 흐름 검증
        {
            ItemData item = CreateItem("IT_TEST_POTION", ItemType.Consumable, 10); // 테스트 아이템 생성
            ProjectHDataCatalog catalog = CreateCatalog(item); // 테스트 카탈로그 생성
            DataManager dataManager = CreateDataManager(catalog); // 테스트 데이터 관리자 생성
            ShopData shop = CreateShop(new ShopProductEntry("PRD_TEST", item.Id, 100, 40)); // 테스트 상점 생성
            SaveData saveData = SaveData.CreateNewGame(System.Array.Empty<string>()); // 테스트 저장 데이터 생성
            GoldCurrencyService.AddGold(saveData, 500); // 테스트 Gold 지급

            bool result = ShopTransactionService.TryPurchase(saveData, dataManager, shop, "PRD_TEST", 2, out string error); // 아이템 두 개 구매 실행

            Assert.That(result, Is.True, error); // 구매 성공 확인
            Assert.That(GoldCurrencyService.GetGold(saveData), Is.EqualTo(300)); // 구매 Gold 차감 확인
            Assert.That(saveData.GetItemCount(item.Id), Is.EqualTo(2)); // 구매 아이템 수량 확인
            DestroyObjects(dataManager, catalog, shop, item); // 테스트 객체 정리
        }

        [Test] // MaxStack 실패 원자성 테스트
        public void TryPurchase_ExceedsMaxStack_KeepsGoldAndInventory() // MaxStack 실패 시 상태 보존 검증
        {
            ItemData item = CreateItem("IT_TEST_STACK", ItemType.Material, 2); // 최대 스택 테스트 아이템 생성
            ProjectHDataCatalog catalog = CreateCatalog(item); // 테스트 카탈로그 생성
            DataManager dataManager = CreateDataManager(catalog); // 테스트 데이터 관리자 생성
            ShopData shop = CreateShop(new ShopProductEntry("PRD_STACK", item.Id, 100, 40)); // 테스트 상점 생성
            SaveData saveData = SaveData.CreateNewGame(System.Array.Empty<string>()); // 테스트 저장 데이터 생성
            GoldCurrencyService.AddGold(saveData, 500); // 테스트 Gold 지급
            Assert.That(ItemInventoryService.TryAdd(saveData, dataManager, item.Id, 2, out string addError), Is.True, addError); // 최대 수량까지 아이템 지급

            bool result = ShopTransactionService.TryPurchase(saveData, dataManager, shop, "PRD_STACK", 1, out _); // MaxStack 초과 구매 실행

            Assert.That(result, Is.False); // 구매 실패 확인
            Assert.That(GoldCurrencyService.GetGold(saveData), Is.EqualTo(500)); // Gold 유지 확인
            Assert.That(saveData.GetItemCount(item.Id), Is.EqualTo(2)); // 아이템 수량 유지 확인
            DestroyObjects(dataManager, catalog, shop, item); // 테스트 객체 정리
        }

        [Test] // 일반 아이템 판매 성공 테스트
        public void TrySell_ValidItem_RemovesInventoryAndAddsGold() // 판매 거래 흐름 검증
        {
            ItemData item = CreateItem("IT_TEST_MATERIAL", ItemType.Material, 20); // 테스트 판매 아이템 생성
            ProjectHDataCatalog catalog = CreateCatalog(item); // 테스트 카탈로그 생성
            DataManager dataManager = CreateDataManager(catalog); // 테스트 데이터 관리자 생성
            ShopData shop = CreateShop(new ShopProductEntry("PRD_SELL", item.Id, 100, 30)); // 테스트 상점 생성
            SaveData saveData = SaveData.CreateNewGame(System.Array.Empty<string>()); // 테스트 저장 데이터 생성
            Assert.That(ItemInventoryService.TryAdd(saveData, dataManager, item.Id, 5, out string addError), Is.True, addError); // 판매용 아이템 지급

            bool result = ShopTransactionService.TrySell(saveData, dataManager, shop, "PRD_SELL", 2, out string error); // 아이템 두 개 판매 실행

            Assert.That(result, Is.True, error); // 판매 성공 확인
            Assert.That(saveData.GetItemCount(item.Id), Is.EqualTo(3)); // 판매 후 아이템 수량 확인
            Assert.That(GoldCurrencyService.GetGold(saveData), Is.EqualTo(60)); // 판매 Gold 지급 확인
            DestroyObjects(dataManager, catalog, shop, item); // 테스트 객체 정리
        }

        private static ItemData CreateItem(string id, ItemType type, int maxStack) // 테스트 ItemData 생성
        {
            ItemData item = ScriptableObject.CreateInstance<ItemData>(); // ItemData 인스턴스 생성
            SetPrivateField(item, "id", id); // 테스트 아이템 ID 설정
            SetPrivateField(item, "displayName", id); // 테스트 아이템 이름 설정
            SetPrivateField(item, "itemType", type); // 테스트 아이템 유형 설정
            SetPrivateField(item, "grade", ItemGrade.Common); // 테스트 아이템 등급 설정
            SetPrivateField(item, "maxStack", maxStack); // 테스트 최대 스택 설정
            SetPrivateField(item, "description", "TEST"); // 테스트 설명 설정
            return item; // 테스트 ItemData 반환
        }

        private static ProjectHDataCatalog CreateCatalog(ItemData item) // 테스트 카탈로그 생성
        {
            ProjectHDataCatalog catalog = ScriptableObject.CreateInstance<ProjectHDataCatalog>(); // 카탈로그 인스턴스 생성
            SetPrivateField(catalog, "items", new List<ItemData> { item }); // 테스트 아이템 목록 설정
            return catalog; // 테스트 카탈로그 반환
        }

        private static DataManager CreateDataManager(ProjectHDataCatalog catalog) // 테스트 DataManager 생성
        {
            GameObject managerObject = new GameObject("Day40DataManagerTest"); // 테스트 관리자 객체 생성
            DataManager manager = managerObject.AddComponent<DataManager>(); // DataManager 컴포넌트 추가
            manager.Configure(catalog); // 테스트 카탈로그 연결
            manager.Initialize(); // 데이터 관리자 초기화
            Assert.That(manager.IsInitialized, Is.True, string.Join("\n", manager.ValidationErrors)); // 초기화 성공 확인
            return manager; // 테스트 DataManager 반환
        }

        private static ShopData CreateShop(ShopProductEntry product) // 테스트 ShopData 생성
        {
            ShopData shop = ScriptableObject.CreateInstance<ShopData>(); // ShopData 인스턴스 생성
            shop.Configure("SHOP_TEST", "테스트 상점", new[] { product }); // 테스트 상품 연결
            return shop; // 테스트 ShopData 반환
        }

        private static void SetPrivateField(object target, string fieldName, object value) // 테스트 private 필드 값 설정
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic); // 대상 private 필드 조회
            Assert.That(field, Is.Not.Null, fieldName); // 필드 존재 확인
            field.SetValue(target, value); // 테스트 필드 값 적용
        }

        private static void DestroyObjects(DataManager manager, ProjectHDataCatalog catalog, ShopData shop, ItemData item) // 테스트 Unity 객체 정리
        {
            Object.DestroyImmediate(manager.gameObject); // DataManager 객체 제거
            Object.DestroyImmediate(catalog); // 카탈로그 객체 제거
            Object.DestroyImmediate(shop); // 상점 객체 제거
            Object.DestroyImmediate(item); // 아이템 객체 제거
        }
    }
}
