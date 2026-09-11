using System.Collections.Generic; // 목록 자료형
using System.Linq; // 목록 비교 기능
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Data; // 상점 데이터 기능
using ProjectH.SaveSystem; // 저장·골드 기능
using ProjectH.Shop; // 상점 재고·리롤 기능
using UnityEditor; // 에셋 로드 기능
using UnityEngine; // Unity 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class ShopRotationTests // Day61 상시 상품·오늘의 상품·재고·품절·리롤 테스트
    {
        private GameObject dataObject; // 데이터 관리자 오브젝트
        private DataManager dataManager; // 실제 카탈로그 데이터 관리자
        private ShopData shop; // 테스트 상점

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 실제 카탈로그 + 테스트 상점 준비
        {
            dataObject = new GameObject("ShopRotationTests"); // 오브젝트 생성
            dataManager = dataObject.AddComponent<DataManager>(); // 컴포넌트 추가
            dataManager.Configure(AssetDatabase.LoadAssetAtPath<ProjectHDataCatalog>("Assets/ProjectH/Data/Database/ProjectHDataCatalog.asset")); // 실제 카탈로그
            dataManager.Initialize(); // 초기화
            shop = ScriptableObject.CreateInstance<ShopData>(); // 테스트 상점
            shop.Configure("SHOP_TEST", "테스트 상점", new[] // 상시 1 + 오늘의 후보 6
            {
                new ShopProductEntry("PRD_POTION", "IT_POTION_SMALL", 100, 40, false, 2), // 상시 · 하루 2개
                new ShopProductEntry("PRD_FLOWER", "IT_GIFT_FLOWER", 60, 24, true, 1), // 후보
                new ShopProductEntry("PRD_CANDLE", "IT_GIFT_CANDLE", 80, 32, true, 1), // 후보
                new ShopProductEntry("PRD_BROOCH", "IT_GIFT_BROOCH", 150, 60, true, 1), // 후보
                new ShopProductEntry("PRD_STARMAP", "IT_GIFT_STARMAP", 150, 60, true, 1), // 후보
                new ShopProductEntry("PRD_SHELL", "IT_GIFT_SHELL", 120, 48, true, 1), // 후보
                new ShopProductEntry("PRD_SWEETS", "IT_GIFT_SWEETS", 40, 16, true, 1) // 후보
            });
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 정리
        {
            Object.DestroyImmediate(shop); // 상점 제거
            Object.DestroyImmediate(dataObject); // 오브젝트 제거
        }

        private static SaveData CreateSave(int gold) // 골드를 가진 새 게임
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 새 게임
            GoldCurrencyService.AddGold(saveData, gold); // 골드 지급
            return saveData; // 저장 반환
        }

        [Test] // 오늘의 상품 : 4칸 · 같은 날은 항상 같은 구성 · 날마다 바뀜
        public void Rotation_IsFourSlots_DeterministicPerDay() // 구성 테스트
        {
            List<string> dayThree = ShopRotationService.PickRotation(shop, 3, 0); // 3일차
            Assert.That(dayThree.Count, Is.EqualTo(ShopRotationService.RotationSlotCount)); // 4칸
            Assert.That(ShopRotationService.PickRotation(shop, 3, 0), Is.EqualTo(dayThree)); // 재현성
            Assert.That(dayThree, Has.No.Member("PRD_POTION")); // 상시 상품은 제외
            bool changed = false; // 다른 날 구성이 달라지는지

            for (int day = 4; day <= 12; day++) // 여러 날 비교
            {
                if (!ShopRotationService.PickRotation(shop, day, 0).SequenceEqual(dayThree)) changed = true; // 다름 확인
            }

            Assert.That(changed, Is.True); // 날마다 바뀜
        }

        [Test] // 하루 재고 → 품절 → 다음 날 재입고
        public void DailyLimit_SellsOut_ThenRestocksNextDay() // 재고 테스트
        {
            SaveData saveData = CreateSave(10000); // 골드 10000
            ShopProductEntry potion = shop.FindProduct("PRD_POTION"); // 상시 포션

            Assert.That(ShopRotationService.TryBuy(saveData, dataManager, shop, "PRD_POTION", 2, out string error), Is.True, error); // 2개 구매
            Assert.That(ShopRotationService.GetRemaining(saveData, shop, potion), Is.EqualTo(0)); // 품절
            Assert.That(ShopRotationService.TryBuy(saveData, dataManager, shop, "PRD_POTION", 1, out string soldOut), Is.False); // 추가 구매 불가
            Assert.That(soldOut, Does.Contain("품절")); // 안내
            Assert.That(GoldCurrencyService.GetGold(saveData), Is.EqualTo(9800)); // 실패 시 골드 유지
            saveData.SetCurrentDay(saveData.CurrentDay + 1); // 다음 날
            Assert.That(ShopRotationService.GetRemaining(saveData, shop, potion), Is.EqualTo(2)); // 재입고
        }

        [Test] // 오늘 진열되지 않은 후보는 살 수 없음
        public void Buy_RotatingProductNotOnDisplay_IsRejected() // 진열 테스트
        {
            SaveData saveData = CreateSave(10000); // 골드 10000
            List<ShopProductEntry> today = ShopRotationService.GetTodayProducts(saveData, shop); // 오늘의 상품
            string hidden = null; // 오늘 없는 후보

            foreach (ShopProductEntry product in shop.Products) // 후보 순회
            {
                if (product.Rotating && !today.Contains(product)) hidden = product.ProductId; // 진열 안 된 후보
            }

            Assert.That(hidden, Is.Not.Null); // 6개 중 2개는 빠짐
            Assert.That(ShopRotationService.TryBuy(saveData, dataManager, shop, hidden, 1, out string error), Is.False); // 구매 불가
            Assert.That(error, Does.Contain("오늘은")); // 안내
            Assert.That(ShopRotationService.TryBuy(saveData, dataManager, shop, today[0].ProductId, 1, out string ok), Is.True, ok); // 진열 상품은 구매
        }

        [Test] // 리롤 : 100 → 200 → 400G · 하루 3회 · 구매 기록 유지 · 다음 날 초기화
        public void Reroll_CostsEscalate_LimitedPerDay_KeepsPurchases() // 리롤 테스트
        {
            SaveData saveData = CreateSave(1000); // 골드 1000
            string bought = ShopRotationService.GetTodayProducts(saveData, shop)[0].ProductId; // 오늘 첫 상품
            Assert.That(ShopRotationService.TryBuy(saveData, dataManager, shop, bought, 1, out string error), Is.True, error); // 구매 (재고 1 → 0)
            int gold = GoldCurrencyService.GetGold(saveData); // 리롤 전 골드

            Assert.That(ShopRotationService.TryReroll(saveData, shop, out _), Is.True); // 1회
            Assert.That(ShopRotationService.TryReroll(saveData, shop, out _), Is.True); // 2회
            Assert.That(ShopRotationService.TryReroll(saveData, shop, out _), Is.True); // 3회
            Assert.That(GoldCurrencyService.GetGold(saveData), Is.EqualTo(gold - 700)); // 100 + 200 + 400
            Assert.That(ShopRotationService.TryReroll(saveData, shop, out string limit), Is.False); // 4회 불가
            Assert.That(limit, Does.Contain("더 이상")); // 안내
            Assert.That(saveData.FindShopState("SHOP_TEST").GetPurchased(bought), Is.EqualTo(1)); // 리롤해도 구매 기록 유지 (재고 초기화 방지)
            saveData.SetCurrentDay(saveData.CurrentDay + 1); // 다음 날
            Assert.That(ShopRotationService.EnsureToday(saveData, shop).RerollCount, Is.EqualTo(0)); // 리롤 초기화
            Assert.That(ShopRotationService.GetRerollCost(0), Is.EqualTo(100)); // 첫 리롤 100G
        }

        [Test] // 상점 상태 저장·불러오기
        public void ShopState_SurvivesJsonRoundTrip() // 저장 테스트
        {
            SaveData saveData = CreateSave(1000); // 골드 1000
            Assert.That(ShopRotationService.TryBuy(saveData, dataManager, shop, "PRD_POTION", 1, out string error), Is.True, error); // 포션 1개
            ShopRotationService.TryReroll(saveData, shop, out _); // 리롤 1회
            SaveData loaded = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(saveData)); // 저장·불러오기
            loaded.EnsureDefaults(); // 보정

            Assert.That(loaded.FindShopState("SHOP_TEST").GetPurchased("PRD_POTION"), Is.EqualTo(1)); // 구매 기록 유지
            Assert.That(loaded.FindShopState("SHOP_TEST").RerollCount, Is.EqualTo(1)); // 리롤 횟수 유지
            Assert.That(ShopRotationService.GetTodayProducts(loaded, shop).Count, Is.EqualTo(4)); // 오늘의 상품 유지
        }

        [Test] // 로비 상점 : 상시·오늘의 상품 구성과 아이템 연결
        public void LobbyShop_HasPermanentAndRotatingProducts() // 데이터 테스트
        {
            ShopData lobby = AssetDatabase.LoadAssetAtPath<ShopData>("Assets/ProjectH/Resources/Shops/SHOP_LOBBY.asset"); // 로비 상점
            int permanent = ShopRotationService.GetPermanentProducts(lobby).Count; // 상시 수
            int rotating = lobby.Products.Count - permanent; // 후보 수

            Assert.That(permanent, Is.InRange(6, 12)); // 상시 1페이지 안
            Assert.That(rotating, Is.GreaterThan(ShopRotationService.RotationSlotCount)); // 후보가 4칸보다 많아야 리롤 의미

            foreach (ShopProductEntry product in lobby.Products) // 상품 순회
            {
                Assert.That(dataManager.GetItem(product.ItemId), Is.Not.Null, product.ProductId); // 아이템 연결
                Assert.That(product.HasDailyLimit, Is.True, product.ProductId); // 모든 상품 하루 재고 (기획서 '재고 제한')
            }
        }
    }
}
