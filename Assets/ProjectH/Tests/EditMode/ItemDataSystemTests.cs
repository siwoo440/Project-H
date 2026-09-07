using System.Collections.Generic; // 테스트 컬렉션 기능
using System.Reflection; // 비공개 직렬화 필드 설정 기능
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Data; // 아이템 데이터 기능
using UnityEditor; // Unity 에디터 에셋 기능
using UnityEngine; // Unity 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class ItemDataSystemTests // Day37 아이템 데이터 시스템 테스트
    {
        private const string CatalogPath = "Assets/ProjectH/Data/Database/ProjectHDataCatalog.asset"; // 데이터 카탈로그 경로
        private const string QuestItemPath = "Assets/ProjectH/Data/Items/IT_QUEST_KEY_001.asset"; // 퀘스트 아이템 경로
        private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>(); // 테스트 생성 오브젝트 목록

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 테스트 생성 오브젝트 정리
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--) // 생성 오브젝트 역순 순회
            {
                if (createdObjects[index] != null) // 오브젝트 존재 확인
                {
                    UnityEngine.Object.DestroyImmediate(createdObjects[index]); // 테스트 오브젝트 즉시 제거
                }
            }

            createdObjects.Clear(); // 생성 목록 초기화
        }

        [Test] // 테스트 표시
        public void ItemType_QuestIsAppendedWithoutChangingExistingValues() // ItemType 기존 값 유지 및 Quest 추가 검증
        {
            Assert.That((int)ItemType.Consumable, Is.EqualTo(0)); // 소비 아이템 값 유지 검증
            Assert.That((int)ItemType.Material, Is.EqualTo(1)); // 재료 아이템 값 유지 검증
            Assert.That((int)ItemType.Equipment, Is.EqualTo(2)); // 장비 아이템 값 유지 검증
            Assert.That((int)ItemType.Gift, Is.EqualTo(3)); // 선물 아이템 값 유지 검증
            Assert.That((int)ItemType.Special, Is.EqualTo(4)); // 특수 아이템 값 유지 검증
            Assert.That((int)ItemType.Quest, Is.EqualTo(5)); // 퀘스트 아이템 신규 값 검증
        }

        [Test] // 테스트 표시
        public void Catalog_QuestSampleRegistersAndCanBeFoundById() // 퀘스트 샘플 카탈로그 등록 검증
        {
            ProjectHDataCatalog catalog = AssetDatabase.LoadAssetAtPath<ProjectHDataCatalog>(CatalogPath); // 실제 데이터 카탈로그 로드
            ItemData questItem = AssetDatabase.LoadAssetAtPath<ItemData>(QuestItemPath); // 실제 퀘스트 아이템 로드
            GameObject managerObject = Track(new GameObject("ItemDataSystem_Catalog_Test")); // 데이터 관리자 오브젝트 생성
            DataManager manager = managerObject.AddComponent<DataManager>(); // 데이터 관리자 컴포넌트 추가
            manager.Configure(catalog); // 실제 카탈로그 연결
            manager.Initialize(); // 실제 카탈로그 초기화

            Assert.That(catalog, Is.Not.Null); // 데이터 카탈로그 존재 검증
            Assert.That(questItem, Is.Not.Null); // 퀘스트 아이템 에셋 존재 검증
            Assert.That(manager.IsInitialized, Is.True, string.Join("\n", manager.ValidationErrors)); // 카탈로그 초기화 검증
            Assert.That(manager.GetItem("IT_QUEST_KEY_001"), Is.SameAs(questItem)); // 퀘스트 ID 조회 검증
            Assert.That(questItem.Type, Is.EqualTo(ItemType.Quest)); // 퀘스트 유형 검증
            Assert.That(questItem.MaxStack, Is.EqualTo(1)); // 퀘스트 최대 수량 검증
        }

        [Test] // 테스트 표시
        public void Initialize_NonEquipmentItemWithoutItPrefixReportsValidationError() // 일반 아이템 ID 접두사 검증
        {
            ItemData item = CreateItem("MATERIAL_BAD", ItemType.Material, 99); // 잘못된 재료 아이템 생성
            ProjectHDataCatalog catalog = CreateCatalog(new List<ItemData> { item }); // 테스트 카탈로그 생성
            DataManager manager = CreateDataManager(catalog); // 데이터 관리자 생성

            InitializeWithoutErrorLogs(manager); // 의도적 오류 로그 없이 초기화

            Assert.That(manager.IsInitialized, Is.False); // 잘못된 ID 초기화 차단 검증
            Assert.That(manager.ValidationErrors, Has.Member("[Item] ID 접두사 오류: MATERIAL_BAD, Type=Material, Expected=IT_")); // 일반 아이템 ID 오류 검증
        }

        [Test] // 테스트 표시
        public void Initialize_EquipmentItemWithoutEqPrefixReportsValidationError() // 장비 아이템 ID 접두사 검증
        {
            ItemData item = CreateItem("IT_BAD_EQUIPMENT", ItemType.Equipment, 1); // 잘못된 장비 ID 생성
            ProjectHDataCatalog catalog = CreateCatalog(new List<ItemData> { item }); // 테스트 카탈로그 생성
            DataManager manager = CreateDataManager(catalog); // 데이터 관리자 생성

            InitializeWithoutErrorLogs(manager); // 의도적 오류 로그 없이 초기화

            Assert.That(manager.IsInitialized, Is.False); // 잘못된 장비 ID 초기화 차단 검증
            Assert.That(manager.ValidationErrors, Has.Member("[Item] ID 접두사 오류: IT_BAD_EQUIPMENT, Type=Equipment, Expected=EQ_")); // 장비 ID 오류 검증
        }

        [Test] // 테스트 표시
        public void Initialize_EquipmentMaxStackGreaterThanOneReportsValidationError() // 장비 최대 수량 검증
        {
            ItemData item = CreateItem("EQ_STACK_BAD", ItemType.Equipment, 2); // 잘못된 장비 최대 수량 생성
            ProjectHDataCatalog catalog = CreateCatalog(new List<ItemData> { item }); // 테스트 카탈로그 생성
            DataManager manager = CreateDataManager(catalog); // 데이터 관리자 생성

            InitializeWithoutErrorLogs(manager); // 의도적 오류 로그 없이 초기화

            Assert.That(manager.IsInitialized, Is.False); // 잘못된 장비 스택 초기화 차단 검증
            Assert.That(manager.ValidationErrors, Has.Member("[Item] 장비 MaxStack은 1이어야 합니다: EQ_STACK_BAD, MaxStack=2")); // 장비 최대 수량 오류 검증
        }

        [Test] // 테스트 표시
        public void Initialize_ValidMaterialItemUsesItPrefixAndStack() // 정상 재료 아이템 검증
        {
            ItemData item = CreateItem("IT_TEST_MATERIAL", ItemType.Material, 999); // 정상 재료 아이템 생성
            ProjectHDataCatalog catalog = CreateCatalog(new List<ItemData> { item }); // 테스트 카탈로그 생성
            DataManager manager = CreateDataManager(catalog); // 데이터 관리자 생성

            manager.Initialize(); // 정상 재료 데이터 초기화

            Assert.That(manager.IsInitialized, Is.True, string.Join("\n", manager.ValidationErrors)); // 정상 재료 초기화 검증
            Assert.That(manager.GetItem("IT_TEST_MATERIAL"), Is.SameAs(item)); // 정상 재료 조회 검증
        }

        private ItemData CreateItem(string id, ItemType itemType, int maxStack) // 테스트 아이템 생성
        {
            ItemData item = Track(ScriptableObject.CreateInstance<ItemData>()); // ItemData 인스턴스 생성
            SetPrivateField(item, "id", id); // 아이템 ID 설정
            SetPrivateField(item, "displayName", id); // 아이템 이름 설정
            SetPrivateField(item, "itemType", itemType); // 아이템 유형 설정
            SetPrivateField(item, "grade", ItemGrade.Common); // 아이템 등급 설정
            SetPrivateField(item, "maxStack", maxStack); // 최대 보유 수량 설정
            SetPrivateField(item, "description", "Day37 Test"); // 아이템 설명 설정
            return item; // 테스트 아이템 반환
        }

        private ProjectHDataCatalog CreateCatalog(List<ItemData> items) // 테스트 카탈로그 생성
        {
            ProjectHDataCatalog catalog = Track(ScriptableObject.CreateInstance<ProjectHDataCatalog>()); // 카탈로그 인스턴스 생성
            SetPrivateField(catalog, "items", items); // 아이템 목록 설정
            SetPrivateField(catalog, "equipments", new List<EquipmentData>()); // 빈 장비 목록 설정
            return catalog; // 테스트 카탈로그 반환
        }

        private DataManager CreateDataManager(ProjectHDataCatalog catalog) // 테스트 데이터 관리자 생성
        {
            GameObject managerObject = Track(new GameObject("ItemDataSystem_DataManager_Test")); // 데이터 관리자 오브젝트 생성
            DataManager manager = managerObject.AddComponent<DataManager>(); // 데이터 관리자 컴포넌트 추가
            manager.Configure(catalog); // 테스트 카탈로그 연결
            return manager; // 테스트 데이터 관리자 반환
        }

        private static void InitializeWithoutErrorLogs(DataManager manager) // 의도적 검증 오류 로그 억제 초기화
        {
            bool previousLogEnabled = Debug.unityLogger.logEnabled; // 기존 Unity 로거 상태 저장

            try // 로거 상태 보호
            {
                Debug.unityLogger.logEnabled = false; // 테스트용 오류 로그 출력 차단
                manager.Initialize(); // 데이터 검증 초기화 실행
            }
            finally // 로거 상태 복원 보장
            {
                Debug.unityLogger.logEnabled = previousLogEnabled; // 기존 Unity 로거 상태 복원
            }
        }

        private T Track<T>(T target) where T : UnityEngine.Object // 테스트 Unity 오브젝트 추적
        {
            createdObjects.Add(target); // 테스트 정리 대상 등록
            return target; // 테스트 오브젝트 반환
        }

        private static void SetPrivateField(object target, string fieldName, object value) // 비공개 직렬화 필드 설정
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic); // 비공개 필드 정보 조회
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found on {target.GetType().Name}."); // 테스트 필드 존재 검증
            field.SetValue(target, value); // 테스트 필드 값 주입
        }
    }
}
