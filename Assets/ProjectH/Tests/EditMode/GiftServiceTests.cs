using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Data; // 아이템·상점 데이터 기능
using ProjectH.SaveSystem; // 선물·호감도 기능
using UnityEditor; // Unity 에디터 에셋 기능
using UnityEngine; // Unity 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class GiftServiceTests // Day57 선물 시스템 회귀 테스트
    {
        private const string CatalogPath = "Assets/ProjectH/Data/Database/ProjectHDataCatalog.asset"; // 실제 데이터 카탈로그 경로
        private const string ShopPath = "Assets/ProjectH/Resources/Shops/SHOP_LOBBY.asset"; // 로비 상점 에셋 경로
        private const string Serena = "CH_SERENA"; // 테스트 캐릭터 세레나
        private GameObject dataObject; // 데이터 관리자 오브젝트
        private DataManager dataManager; // 실제 카탈로그 데이터 관리자

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 실제 데이터 카탈로그 준비 (선물 에셋 GUID 등록까지 함께 검증)
        {
            dataObject = new GameObject("GiftServiceTests"); // 데이터 관리자 오브젝트 생성
            dataManager = dataObject.AddComponent<DataManager>(); // 데이터 관리자 컴포넌트 추가
            dataManager.Configure(AssetDatabase.LoadAssetAtPath<ProjectHDataCatalog>(CatalogPath)); // 실제 카탈로그 연결
            dataManager.Initialize(); // 데이터 관리자 초기화
            Assert.That(dataManager.IsInitialized, Is.True, string.Join("\n", dataManager.ValidationErrors)); // 초기화 성공 검증
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 테스트 오브젝트 정리
        {
            Object.DestroyImmediate(dataObject); // 데이터 관리자 오브젝트 제거
        }

        private SaveData CreateSave(string itemId, int quantity, int affinity = 0) // 선물 보유 새 게임 데이터 생성
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { Serena, "CH_ELLEN" }); // 새 게임 데이터 생성
            AffinityService.AddAffinity(saveData, Serena, affinity); // 시작 호감도 설정

            if (quantity > 0) // 지급 수량 확인
            {
                Assert.That(ItemInventoryService.TryAdd(saveData, dataManager, itemId, quantity, out string error), Is.True, error); // 선물 지급 검증
            }

            return saveData; // 데이터 반환
        }

        [Test] // 선물 에셋 6종 등록 검증
        public void Catalog_ContainsSixGiftItems() // 카탈로그·아이템 유형 테스트
        {
            Assert.That(GiftPreferenceCatalog.AllGiftIds.Count, Is.EqualTo(6)); // 선물 6종 검증

            foreach (string itemId in GiftPreferenceCatalog.AllGiftIds) // 선물 목록 순회
            {
                ItemData item = dataManager.GetItem(itemId); // 선물 원본 조회
                Assert.That(item, Is.Not.Null, itemId); // 카탈로그 등록 검증
                Assert.That(item.Type, Is.EqualTo(ItemType.Gift), itemId); // 선물 유형 검증
            }
        }

        [Test] // 로비 상점 판매 가격 검증
        public void LobbyShop_SellsGiftsAtPlannedPrices() // 상점 상품 테스트
        {
            ShopData shop = AssetDatabase.LoadAssetAtPath<ShopData>(ShopPath); // 로비 상점 로드
            Assert.That(shop, Is.Not.Null); // 상점 존재 검증
            Assert.That(shop.FindProduct("PRD_GIFT_FLOWER").BuyPrice, Is.EqualTo(60)); // 들꽃 꽃다발 60 검증
            Assert.That(shop.FindProduct("PRD_GIFT_CANDLE").BuyPrice, Is.EqualTo(80)); // 성수 향초 80 검증
            Assert.That(shop.FindProduct("PRD_GIFT_BROOCH").BuyPrice, Is.EqualTo(150)); // 브로치 150 검증
            Assert.That(shop.FindProduct("PRD_GIFT_STARMAP").BuyPrice, Is.EqualTo(150)); // 별자리 지도 150 검증
            Assert.That(shop.FindProduct("PRD_GIFT_SHELL").BuyPrice, Is.EqualTo(120)); // 정령의 조개 120 검증
            Assert.That(shop.FindProduct("PRD_GIFT_SWEETS").BuyPrice, Is.EqualTo(40)); // 달콤한 과자 40 검증
        }

        [Test] // 취향 표 및 증가량 검증
        public void PreferenceTable_LovesAndDefaultNormal() // 취향 표 테스트
        {
            Assert.That(GiftPreferenceCatalog.GetPreference("CH_SERENA", GiftPreferenceCatalog.Candle), Is.EqualTo(GiftPreference.Love)); // 세레나 향초 검증
            Assert.That(GiftPreferenceCatalog.GetPreference("CH_ELLEN", GiftPreferenceCatalog.Brooch), Is.EqualTo(GiftPreference.Love)); // 엘렌 브로치 검증
            Assert.That(GiftPreferenceCatalog.GetPreference("CH_LILIA", GiftPreferenceCatalog.StarMap), Is.EqualTo(GiftPreference.Love)); // 릴리아 별자리 지도 검증
            Assert.That(GiftPreferenceCatalog.GetPreference("CH_EVE", GiftPreferenceCatalog.Shell), Is.EqualTo(GiftPreference.Love)); // 이브 조개 검증
            Assert.That(GiftPreferenceCatalog.GetPreference("CH_CLAIRE", GiftPreferenceCatalog.Candle), Is.EqualTo(GiftPreference.Normal)); // 표에 없는 조합 보통 검증
            Assert.That(GiftPreferenceCatalog.GetAffinityGain(GiftPreference.Love), Is.EqualTo(15)); // 아주 좋아함 +15 검증
            Assert.That(GiftPreferenceCatalog.GetAffinityGain(GiftPreference.Like), Is.EqualTo(8)); // 좋아함 +8 검증
            Assert.That(GiftPreferenceCatalog.GetAffinityGain(GiftPreference.Normal), Is.EqualTo(4)); // 보통 +4 검증
            Assert.That(GiftPreferenceCatalog.GetAffinityGain(GiftPreference.Dislike), Is.EqualTo(1)); // 별로 +1 검증
        }

        [Test] // 선물 성공 시 차감·호감도·횟수 검증
        public void TryGive_LovedGift_ConsumesOneAndAddsFifteen() // 기본 선물 흐름 테스트
        {
            SaveData saveData = CreateSave(GiftPreferenceCatalog.Candle, 2); // 향초 2개 보유

            bool given = GiftService.TryGive(saveData, dataManager, Serena, GiftPreferenceCatalog.Candle, out GiftResult result); // 향초 선물

            Assert.That(given, Is.True, result.Message); // 선물 성공 검증
            Assert.That(saveData.GetItemCount(GiftPreferenceCatalog.Candle), Is.EqualTo(1)); // 1개 차감 검증
            Assert.That(AffinityService.GetAffinity(saveData, Serena), Is.EqualTo(15)); // 호감도 +15 검증
            Assert.That(result.AffinityGain, Is.EqualTo(15)); // 결과 증가량 검증
            Assert.That(GiftService.GetGiftsGivenToday(saveData, Serena), Is.EqualTo(1)); // 오늘 1회 검증
        }

        [Test] // 표에 없는 조합·별로 증가량 검증
        public void TryGive_NormalAndDislikedGifts_UseTableGain() // 보통·별로 테스트
        {
            SaveData saveData = CreateSave(GiftPreferenceCatalog.Shell, 1); // 조개 1개 보유 (세레나 보통)
            ItemInventoryService.TryAdd(saveData, dataManager, GiftPreferenceCatalog.Brooch, 1, out _); // 브로치 1개 보유 (세레나 별로)

            GiftService.TryGive(saveData, dataManager, Serena, GiftPreferenceCatalog.Shell, out GiftResult normal); // 조개 선물
            GiftService.TryGive(saveData, dataManager, Serena, GiftPreferenceCatalog.Brooch, out GiftResult dislike); // 브로치 선물

            Assert.That(normal.Preference, Is.EqualTo(GiftPreference.Normal)); // 보통 취향 검증
            Assert.That(normal.AffinityGain, Is.EqualTo(4)); // 보통 +4 검증
            Assert.That(dislike.Preference, Is.EqualTo(GiftPreference.Dislike)); // 별로 취향 검증
            Assert.That(dislike.AffinityGain, Is.EqualTo(1)); // 별로 +1 검증
            Assert.That(AffinityService.GetAffinity(saveData, Serena), Is.EqualTo(5)); // 누적 호감도 5 검증
        }

        [Test] // 하루 3회 제한 검증
        public void TryGive_FourthGiftSameDay_IsRejectedWithoutConsuming() // 일일 제한 테스트
        {
            SaveData saveData = CreateSave(GiftPreferenceCatalog.Sweets, 5); // 과자 5개 보유

            for (int index = 0; index < GiftService.DailyGiftLimit; index++) // 3회 선물
            {
                Assert.That(GiftService.TryGive(saveData, dataManager, Serena, GiftPreferenceCatalog.Sweets, out _), Is.True); // 선물 성공 검증
            }

            bool fourth = GiftService.TryGive(saveData, dataManager, Serena, GiftPreferenceCatalog.Sweets, out GiftResult result); // 4번째 선물

            Assert.That(fourth, Is.False); // 4번째 거부 검증
            Assert.That(saveData.GetItemCount(GiftPreferenceCatalog.Sweets), Is.EqualTo(2)); // 거부 시 선물 유지 검증
            Assert.That(result.Message, Does.Contain("오늘은")); // 제한 안내 검증
            Assert.That(GiftService.GetRemainingToday(saveData, Serena), Is.EqualTo(0)); // 남은 횟수 0 검증
        }

        [Test] // 하루 제한은 캐릭터별 검증
        public void DailyLimit_IsCountedPerCharacter() // 캐릭터별 제한 테스트
        {
            SaveData saveData = CreateSave(GiftPreferenceCatalog.Sweets, 4); // 과자 4개 보유

            for (int index = 0; index < GiftService.DailyGiftLimit; index++) // 세레나 3회 선물
            {
                GiftService.TryGive(saveData, dataManager, Serena, GiftPreferenceCatalog.Sweets, out _); // 세레나 선물
            }

            Assert.That(GiftService.TryGive(saveData, dataManager, "CH_ELLEN", GiftPreferenceCatalog.Sweets, out GiftResult result), Is.True, result.Message); // 엘렌은 선물 가능 검증
            Assert.That(result.Preference, Is.EqualTo(GiftPreference.Like)); // 엘렌 과자 좋아함 검증
        }

        [Test] // 날짜 변경 시 횟수 초기화 검증
        public void TryGive_NextDay_ResetsDailyCount() // 날짜 초기화 테스트
        {
            SaveData saveData = CreateSave(GiftPreferenceCatalog.Sweets, 4); // 과자 4개 보유

            for (int index = 0; index < GiftService.DailyGiftLimit; index++) // 3회 선물
            {
                GiftService.TryGive(saveData, dataManager, Serena, GiftPreferenceCatalog.Sweets, out _); // 선물
            }

            saveData.SetCurrentDay(saveData.CurrentDay + 1); // 다음 날로 진행

            Assert.That(GiftService.GetGiftsGivenToday(saveData, Serena), Is.EqualTo(0)); // 새 날짜 0회 검증
            Assert.That(GiftService.TryGive(saveData, dataManager, Serena, GiftPreferenceCatalog.Sweets, out _), Is.True); // 다시 선물 가능 검증
            Assert.That(GiftService.GetGiftsGivenToday(saveData, Serena), Is.EqualTo(1)); // 새 날짜 1회 검증
        }

        [Test] // 호감도 100 거부 검증
        public void TryGive_MaxAffinity_RejectsWithoutConsuming() // 최대 호감도 테스트
        {
            SaveData saveData = CreateSave(GiftPreferenceCatalog.Candle, 1, CharacterSaveData.MaxAffinity); // 호감도 100, 향초 1개

            bool given = GiftService.TryGive(saveData, dataManager, Serena, GiftPreferenceCatalog.Candle, out GiftResult result); // 선물 시도

            Assert.That(given, Is.False); // 거부 검증
            Assert.That(saveData.GetItemCount(GiftPreferenceCatalog.Candle), Is.EqualTo(1)); // 선물 유지 검증
            Assert.That(GiftService.GetGiftsGivenToday(saveData, Serena), Is.EqualTo(0)); // 횟수 미사용 검증
            Assert.That(result.Message, Does.Contain("최대")); // 최대 안내 검증
        }

        [Test] // 100 초과분 제외 검증
        public void TryGive_NearMax_ClampsGainAtHundred() // 상한 테스트
        {
            SaveData saveData = CreateSave(GiftPreferenceCatalog.Candle, 1, 95); // 호감도 95, 향초 1개

            GiftService.TryGive(saveData, dataManager, Serena, GiftPreferenceCatalog.Candle, out GiftResult result); // 향초 선물

            Assert.That(result.AffinityAfter, Is.EqualTo(CharacterSaveData.MaxAffinity)); // 100 도달 검증
            Assert.That(result.AffinityGain, Is.EqualTo(5)); // 실제 증가량 5 검증
        }

        [Test] // 미보유·선물 아닌 아이템 실패 검증
        public void TryGive_MissingOrNonGiftItem_Fails() // 실패 조건 테스트
        {
            SaveData saveData = CreateSave(GiftPreferenceCatalog.Candle, 0); // 선물 없음
            ItemInventoryService.TryAdd(saveData, dataManager, "IT_POTION_SMALL", 1, out _); // 물약 1개 보유

            Assert.That(GiftService.TryGive(saveData, dataManager, Serena, GiftPreferenceCatalog.Candle, out GiftResult missing), Is.False); // 미보유 실패 검증
            Assert.That(missing.Message, Does.Contain("가방")); // 가방 없음 안내 검증
            Assert.That(GiftService.TryGive(saveData, dataManager, Serena, "IT_POTION_SMALL", out _), Is.False); // 물약 선물 불가 검증
            Assert.That(saveData.GetItemCount("IT_POTION_SMALL"), Is.EqualTo(1)); // 물약 유지 검증
            Assert.That(AffinityService.GetAffinity(saveData, Serena), Is.EqualTo(0)); // 호감도 변화 없음 검증
        }

        [Test] // 취향 발견 플래그 검증
        public void TryGive_RecordsPreferenceDiscoveryOnce() // 취향 발견 테스트
        {
            SaveData saveData = CreateSave(GiftPreferenceCatalog.Candle, 2); // 향초 2개 보유
            Assert.That(GiftService.IsPreferenceKnown(saveData, Serena, GiftPreferenceCatalog.Candle), Is.False); // 선물 전 모름 검증

            GiftService.TryGive(saveData, dataManager, Serena, GiftPreferenceCatalog.Candle, out GiftResult first); // 첫 선물
            GiftService.TryGive(saveData, dataManager, Serena, GiftPreferenceCatalog.Candle, out GiftResult second); // 두 번째 선물

            Assert.That(saveData.HasStoryFlag("SYS_GIFT_KNOWN:CH_SERENA:IT_GIFT_CANDLE"), Is.True); // 플래그 형식 검증
            Assert.That(first.DiscoveredPreference, Is.True); // 첫 선물 발견 검증
            Assert.That(second.DiscoveredPreference, Is.False); // 두 번째는 이미 앎 검증
            Assert.That(GiftService.IsPreferenceKnown(saveData, "CH_ELLEN", GiftPreferenceCatalog.Candle), Is.False); // 다른 캐릭터는 모름 검증
        }

        [Test] // 단계 도달 시 Day56 보상 연계 검증
        public void TryGive_CrossingTier_ReportsNewTierAndRewardBecomesClaimable() // 단계 도달 테스트
        {
            SaveData saveData = CreateSave(GiftPreferenceCatalog.Candle, 1, 10); // 호감도 10, 향초 1개

            GiftService.TryGive(saveData, dataManager, Serena, GiftPreferenceCatalog.Candle, out GiftResult result); // 향초 선물 (10 → 25)

            Assert.That(result.NewTiers, Is.EquivalentTo(new[] { AffinityTier.Acquaintance })); // 안면 단계 새로 도달 검증
            Assert.That(result.Message, Does.Contain("호감도 보상")); // 보상 안내 문구 검증
            Assert.That(AffinityRewardService.GetState(saveData, Serena, AffinityTier.Acquaintance), Is.EqualTo(AffinityRewardState.Claimable)); // 보상 받기 가능 검증
        }

        [Test] // 저장·불러오기 후 오늘 횟수 유지 검증
        public void JsonRoundTrip_PreservesDailyGiftCount() // 저장 왕복 테스트
        {
            SaveData source = CreateSave(GiftPreferenceCatalog.Flower, 2); // 꽃다발 2개 보유
            GiftService.TryGive(source, dataManager, Serena, GiftPreferenceCatalog.Flower, out _); // 1회 선물
            GiftService.TryGive(source, dataManager, Serena, GiftPreferenceCatalog.Flower, out _); // 2회 선물

            SaveData loaded = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(source)); // JSON 왕복

            Assert.That(GiftService.GetGiftsGivenToday(loaded, Serena), Is.EqualTo(2)); // 오늘 2회 유지 검증
            Assert.That(GiftService.IsPreferenceKnown(loaded, Serena, GiftPreferenceCatalog.Flower), Is.True); // 발견 취향 유지 검증
        }

        [Test] // 이전 저장 호환 검증
        public void OldSave_WithoutGiftFields_StartsAtZero() // 저장 호환 테스트
        {
            string json = "{\"characters\":[{\"characterId\":\"CH_SERENA\",\"level\":3,\"affinity\":30}]}"; // Day57 이전 형식 저장
            SaveData saveData = JsonUtility.FromJson<SaveData>(json); // 저장 복원
            saveData.EnsureDefaults(); // 기본값 보정

            Assert.That(saveData.FindCharacter(Serena).LastGiftDay, Is.EqualTo(0)); // 선물 기록 없음 검증
            Assert.That(GiftService.GetRemainingToday(saveData, Serena), Is.EqualTo(GiftService.DailyGiftLimit)); // 오늘 3회 가능 검증
        }
    }
}
