using System; // 시스템 자료형
using System.Collections.Generic; // 목록 자료형
using ProjectH.Core; // 프로젝트 핵심 기능
using ProjectH.Data; // 프로젝트 데이터 기능
using UnityEditor; // Unity 에디터 기능
using UnityEditor.SceneManagement; // 에디터 씬 관리
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // 런타임 씬 자료형

namespace ProjectH.EditorTools // 프로젝트 에디터 도구 영역
{
    public static class Phase0Day2Setup // 2일차 자동 설정 도구
    {
        private const string ProjectRoot = "Assets/ProjectH"; // 프로젝트 루트 경로
        private const string DataRoot = "Assets/ProjectH/Data"; // 데이터 루트 경로
        private const string CharacterRoot = "Assets/ProjectH/Data/Characters"; // 캐릭터 루트 경로
        private const string BootstrapScenePath = "Assets/ProjectH/Scenes/Bootstrap.unity"; // 부트스트랩 씬 경로
        private const string BootstrapRootName = "[ProjectH] Bootstrap"; // 부트스트랩 객체 이름
        private const string CatalogPath = "Assets/ProjectH/Data/Database/ProjectHDataCatalog.asset"; // 카탈로그 경로

        [MenuItem("Tools/Project H/Phase 0/2일차 설정 실행")] // 설정 메뉴 등록
        public static void Setup() // 2일차 설정 실행
        {
            EnsureFolders(); // 데이터 폴더 구성
            EnsureInitialCharacters(); // 초기 캐릭터 존재 보장
            List<CharacterData> characters = LoadAllCharacters(); // 현재 전체 캐릭터 로드
            List<MonsterData> monsters = CreateMonsters(); // 몬스터 샘플 생성
            List<DungeonData> dungeons = CreateDungeons(); // 던전 샘플 생성
            List<ItemData> items = CreateItems(); // 아이템 샘플 생성
            ProjectHDataCatalog catalog = CreateCatalog(characters, monsters, dungeons, items); // 카탈로그 생성
            ConfigureBootstrap(catalog); // 부트스트랩 데이터 연결
            AssetDatabase.SaveAssets(); // 에셋 변경 저장
            AssetDatabase.Refresh(); // 에셋 목록 갱신
            Debug.Log($"[Project H] Phase 0 Day 2 setup complete. Characters={characters.Count}"); // 설정 완료 로그
        }

        private static void EnsureFolders() // 데이터 폴더 생성
        {
            EnsureFolder("Assets", "ProjectH"); // 프로젝트 루트 보장
            EnsureFolder(ProjectRoot, "Data"); // 데이터 루트 보장
            EnsureFolder(DataRoot, "Characters"); // 캐릭터 폴더 보장
            EnsureFolder(DataRoot, "Monsters"); // 몬스터 폴더 보장
            EnsureFolder(DataRoot, "Dungeons"); // 던전 폴더 보장
            EnsureFolder(DataRoot, "Items"); // 아이템 폴더 보장
            EnsureFolder(DataRoot, "Database"); // 데이터베이스 폴더 보장
        }

        private static void EnsureFolder(string parentPath, string folderName) // 단일 폴더 생성
        {
            string folderPath = parentPath + "/" + folderName; // 전체 폴더 경로 생성

            if (AssetDatabase.IsValidFolder(folderPath)) // 기존 폴더 확인
            {
                return; // 중복 생성 중단
            }

            AssetDatabase.CreateFolder(parentPath, folderName); // 새 폴더 생성
        }

        private static void EnsureInitialCharacters() // 초기 4인 캐릭터 존재 보장
        {
            CreateCharacterIfMissing("CH_SERENA", "세레나", CharacterJob.Cleric, BattlePosition.Healer, 2200, 180, 120, 0.90f, 0.98f); // 세레나 기본 데이터 보장
            CreateCharacterIfMissing("CH_ELLEN", "엘렌", CharacterJob.Knight, BattlePosition.Tank, 3200, 230, 200, 0.85f, 0.96f); // 엘렌 기본 데이터 보장
            CreateCharacterIfMissing("CH_LILIA", "릴리아", CharacterJob.Mage, BattlePosition.Dealer, 1700, 300, 100, 1.00f, 0.92f); // 릴리아 기본 데이터 보장
            CreateCharacterIfMissing("CH_EVE", "이브", CharacterJob.Archer, BattlePosition.Dealer, 1750, 270, 105, 1.25f, 0.94f); // 이브 기본 데이터 보장
        }

        private static void CreateCharacterIfMissing(string id, string displayName, CharacterJob job, BattlePosition position, int hp, int attack, int defense, float attackSpeed, float accuracy) // 누락 캐릭터 생성
        {
            string path = $"{CharacterRoot}/{id}.asset"; // 캐릭터 에셋 경로 생성
            CharacterData existing = AssetDatabase.LoadAssetAtPath<CharacterData>(path); // 기존 캐릭터 조회

            if (existing != null) // 기존 캐릭터 확인
            {
                return; // 기존 7일차 데이터 보존
            }

            CharacterData asset = ScriptableObject.CreateInstance<CharacterData>(); // 새 캐릭터 인스턴스 생성
            AssetDatabase.CreateAsset(asset, path); // 새 캐릭터 에셋 저장
            SerializedObject data = new SerializedObject(asset); // 직렬화 객체 생성
            data.FindProperty("id").stringValue = id; // 캐릭터 ID 설정
            data.FindProperty("displayName").stringValue = displayName; // 표시 이름 설정
            data.FindProperty("job").enumValueIndex = (int)job; // 직군 설정
            data.FindProperty("position").enumValueIndex = (int)position; // 포지션 설정
            data.FindProperty("baseHp").intValue = hp; // 체력 설정
            data.FindProperty("baseAttack").intValue = attack; // 공격력 설정
            data.FindProperty("baseDefense").intValue = defense; // 방어력 설정
            data.FindProperty("attackSpeed").floatValue = attackSpeed; // 공격속도 설정
            data.FindProperty("accuracy").floatValue = accuracy; // 명중률 설정
            data.FindProperty("baseMagic").intValue = 0; // 임시 마력 설정
            data.FindProperty("baseResistance").intValue = 0; // 임시 저항력 설정
            data.FindProperty("criticalRate").floatValue = 0.05f; // 임시 치명타율 설정
            data.ApplyModifiedPropertiesWithoutUndo(); // 캐릭터 값 적용
            asset.name = id; // 에셋 이름 동기화
            EditorUtility.SetDirty(asset); // 캐릭터 변경 표시
        }

        private static List<CharacterData> LoadAllCharacters() // 전체 캐릭터 데이터 로드
        {
            List<CharacterData> result = new List<CharacterData>(); // 결과 목록 생성
            string[] guids = AssetDatabase.FindAssets("t:CharacterData", new[] { CharacterRoot }); // 캐릭터 GUID 검색

            foreach (string guid in guids) // 캐릭터 GUID 순회
            {
                string path = AssetDatabase.GUIDToAssetPath(guid); // 캐릭터 에셋 경로 조회
                CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>(path); // 캐릭터 에셋 로드

                if (character != null) // 캐릭터 존재 확인
                {
                    result.Add(character); // 캐릭터 목록 추가
                }
            }

            result.Sort((left, right) => string.Compare(left.Id, right.Id, StringComparison.Ordinal)); // 캐릭터 ID 순 정렬
            return result; // 전체 캐릭터 목록 반환
        }

        private static List<MonsterData> CreateMonsters() // 몬스터 샘플 생성
        {
            List<MonsterData> result = new List<MonsterData>(); // 결과 목록 생성
            result.Add(CreateMonster("MON_CORRUPTED_WOLF", "침식된 늑대", 420, 48, 24, 18, 1.15f, 1.4f, 2.8f)); // 늑대 샘플 생성
            result.Add(CreateMonster("MON_POLLUTED_PLANT", "오염 식물", 520, 42, 38, 42, 0.75f, 3.2f, 0.5f)); // 식물 샘플 생성
            result.Add(CreateMonster("MON_CORRUPTED_SOLDIER", "침식 병사", 650, 55, 55, 35, 0.9f, 1.6f, 1.8f)); // 병사 샘플 생성
            return result; // 몬스터 목록 반환
        }

        private static MonsterData CreateMonster(string id, string displayName, int hp, int attack, int defense, int resistance, float attackSpeed, float attackRange, float moveSpeed) // 몬스터 단일 생성
        {
            string path = $"{DataRoot}/Monsters/{id}.asset"; // 몬스터 에셋 경로
            MonsterData asset = LoadOrCreateAsset<MonsterData>(path); // 몬스터 에셋 준비
            SerializedObject data = new SerializedObject(asset); // 직렬화 객체 생성
            data.FindProperty("id").stringValue = id; // 몬스터 ID 설정
            data.FindProperty("displayName").stringValue = displayName; // 표시 이름 설정
            data.FindProperty("maxHp").intValue = hp; // 체력 설정
            data.FindProperty("attack").intValue = attack; // 공격력 설정
            data.FindProperty("defense").intValue = defense; // 방어력 설정
            data.FindProperty("resistance").intValue = resistance; // 저항력 설정
            data.FindProperty("attackSpeed").floatValue = attackSpeed; // 공격속도 설정
            data.FindProperty("attackRange").floatValue = attackRange; // 공격 사거리 설정
            data.FindProperty("moveSpeed").floatValue = moveSpeed; // 이동속도 설정
            data.ApplyModifiedPropertiesWithoutUndo(); // 몬스터 값 적용
            EditorUtility.SetDirty(asset); // 몬스터 변경 표시
            return asset; // 몬스터 에셋 반환
        }

        private static List<DungeonData> CreateDungeons() // 던전 샘플 생성
        {
            List<DungeonData> result = new List<DungeonData>(); // 결과 목록 생성
            result.Add(CreateDungeon("DG_LETICIA_FOREST", "무너진 성역의 숲", "REG_LETICIA", 1, 100, 100)); // 던전 샘플 생성
            return result; // 던전 목록 반환
        }

        private static DungeonData CreateDungeon(string id, string displayName, string regionId, int recommendedLevel, int rewardGold, int rewardExp) // 던전 단일 생성
        {
            string path = $"{DataRoot}/Dungeons/{id}.asset"; // 던전 에셋 경로
            DungeonData asset = LoadOrCreateAsset<DungeonData>(path); // 던전 에셋 준비
            SerializedObject data = new SerializedObject(asset); // 직렬화 객체 생성
            data.FindProperty("id").stringValue = id; // 던전 ID 설정
            data.FindProperty("displayName").stringValue = displayName; // 표시 이름 설정
            data.FindProperty("regionId").stringValue = regionId; // 지역 ID 설정
            data.FindProperty("recommendedLevel").intValue = recommendedLevel; // 권장 레벨 설정
            data.FindProperty("rewardGold").intValue = rewardGold; // 골드 보상 설정
            data.FindProperty("rewardExp").intValue = rewardExp; // 경험치 보상 설정
            data.ApplyModifiedPropertiesWithoutUndo(); // 던전 값 적용
            EditorUtility.SetDirty(asset); // 던전 변경 표시
            return asset; // 던전 에셋 반환
        }

        private static List<ItemData> CreateItems() // 아이템 샘플 생성
        {
            List<ItemData> result = new List<ItemData>(); // 결과 목록 생성
            result.Add(CreateItem("IT_POTION_SMALL", "소형 회복 물약", ItemType.Consumable, ItemGrade.Common, 99, "프로토타입용 회복 아이템 샘플")); // 회복 아이템 샘플 생성
            result.Add(CreateItem("IT_MATERIAL_001", "침식 결정 조각", ItemType.Material, ItemGrade.Common, 999, "프로토타입용 재료 아이템 샘플")); // 재료 아이템 샘플 생성
            return result; // 아이템 목록 반환
        }

        private static ItemData CreateItem(string id, string displayName, ItemType itemType, ItemGrade grade, int maxStack, string description) // 아이템 단일 생성
        {
            string path = $"{DataRoot}/Items/{id}.asset"; // 아이템 에셋 경로
            ItemData asset = LoadOrCreateAsset<ItemData>(path); // 아이템 에셋 준비
            SerializedObject data = new SerializedObject(asset); // 직렬화 객체 생성
            data.FindProperty("id").stringValue = id; // 아이템 ID 설정
            data.FindProperty("displayName").stringValue = displayName; // 표시 이름 설정
            data.FindProperty("itemType").enumValueIndex = (int)itemType; // 아이템 유형 설정
            data.FindProperty("grade").enumValueIndex = (int)grade; // 아이템 등급 설정
            data.FindProperty("maxStack").intValue = maxStack; // 최대 수량 설정
            data.FindProperty("description").stringValue = description; // 설명 설정
            data.ApplyModifiedPropertiesWithoutUndo(); // 아이템 값 적용
            EditorUtility.SetDirty(asset); // 아이템 변경 표시
            return asset; // 아이템 에셋 반환
        }

        private static ProjectHDataCatalog CreateCatalog(List<CharacterData> characters, List<MonsterData> monsters, List<DungeonData> dungeons, List<ItemData> items) // 카탈로그 생성
        {
            ProjectHDataCatalog catalog = LoadOrCreateAsset<ProjectHDataCatalog>(CatalogPath); // 카탈로그 에셋 준비
            SerializedObject data = new SerializedObject(catalog); // 카탈로그 직렬화 객체
            SetArray(data.FindProperty("characters"), characters); // 전체 캐릭터 목록 설정
            SetArray(data.FindProperty("monsters"), monsters); // 몬스터 목록 설정
            SetArray(data.FindProperty("dungeons"), dungeons); // 던전 목록 설정
            SetArray(data.FindProperty("items"), items); // 아이템 목록 설정
            data.ApplyModifiedPropertiesWithoutUndo(); // 카탈로그 값 적용
            EditorUtility.SetDirty(catalog); // 카탈로그 변경 표시
            return catalog; // 카탈로그 반환
        }

        private static void SetArray<T>(SerializedProperty property, List<T> values) where T : UnityEngine.Object // 오브젝트 배열 설정
        {
            property.arraySize = values.Count; // 배열 크기 설정

            for (int index = 0; index < values.Count; index++) // 값 목록 순회
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index]; // 배열 참조 설정
            }
        }

        private static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject // 에셋 준비
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path); // 기존 에셋 조회

            if (asset != null) // 기존 에셋 확인
            {
                return asset; // 기존 에셋 반환
            }

            asset = ScriptableObject.CreateInstance<T>(); // 새 에셋 인스턴스 생성
            AssetDatabase.CreateAsset(asset, path); // 새 에셋 저장
            return asset; // 새 에셋 반환
        }

        private static void ConfigureBootstrap(ProjectHDataCatalog catalog) // 부트스트랩 데이터 연결
        {
            Scene bootstrapScene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single); // 부트스트랩 씬 열기
            GameObject bootstrapRoot = FindBootstrapRoot(bootstrapScene); // 부트스트랩 루트 조회

            if (bootstrapRoot == null) // 부트스트랩 루트 확인
            {
                bootstrapRoot = new GameObject(BootstrapRootName); // 부트스트랩 루트 생성
                SceneManager.MoveGameObjectToScene(bootstrapRoot, bootstrapScene); // 대상 씬으로 객체 이동
            }

            if (bootstrapRoot.GetComponent<SceneLoader>() == null) // 씬 로더 확인
            {
                bootstrapRoot.AddComponent<SceneLoader>(); // 씬 로더 추가
            }

            DataManager dataManager = bootstrapRoot.GetComponent<DataManager>(); // 데이터 관리자 조회

            if (dataManager == null) // 데이터 관리자 확인
            {
                dataManager = bootstrapRoot.AddComponent<DataManager>(); // 데이터 관리자 추가
            }

            if (bootstrapRoot.GetComponent<GameManager>() == null) // 게임 관리자 확인
            {
                bootstrapRoot.AddComponent<GameManager>(); // 게임 관리자 추가
            }

            SerializedObject dataManagerObject = new SerializedObject(dataManager); // 데이터 관리자 직렬화 객체
            dataManagerObject.FindProperty("catalog").objectReferenceValue = catalog; // 카탈로그 참조 설정
            dataManagerObject.ApplyModifiedPropertiesWithoutUndo(); // 카탈로그 참조 적용
            EditorUtility.SetDirty(dataManager); // 데이터 관리자 변경 표시
            EditorSceneManager.MarkSceneDirty(bootstrapScene); // 씬 변경 표시
            EditorSceneManager.SaveScene(bootstrapScene, BootstrapScenePath); // 부트스트랩 씬 저장
        }

        private static GameObject FindBootstrapRoot(Scene bootstrapScene) // 부트스트랩 루트 검색
        {
            GameObject[] rootObjects = bootstrapScene.GetRootGameObjects(); // 씬 루트 목록 조회

            foreach (GameObject rootObject in rootObjects) // 루트 객체 순회
            {
                if (rootObject.name == BootstrapRootName) // 지정 이름 확인
                {
                    return rootObject; // 기존 루트 반환
                }
            }

            return null; // 기존 루트 없음
        }
    }
}
