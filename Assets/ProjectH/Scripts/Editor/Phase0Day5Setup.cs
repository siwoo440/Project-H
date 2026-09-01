using System.Collections.Generic; // 목록 자료형
using ProjectH.Core; // 프로젝트 핵심 기능
using ProjectH.Events; // 이벤트 기능
using ProjectH.SaveSystem; // 저장 기능
using UnityEditor; // Unity 에디터 기능
using UnityEditor.SceneManagement; // 에디터 씬 관리
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // 씬 관리 기능

namespace ProjectH.EditorTools // 프로젝트 에디터 도구 영역
{
    public static class Phase0Day5Setup // 5일차 자동 설정 도구
    {
        private const string EventRoot = "Assets/ProjectH/Data/Events"; // 이벤트 데이터 루트
        private const string EventDatabaseRoot = "Assets/ProjectH/Data/Events/Database"; // 이벤트 데이터베이스 루트
        private const string CatalogPath = "Assets/ProjectH/Data/Events/Database/ProjectHEventCatalog.asset"; // 이벤트 카탈로그 경로
        private const string BootstrapScenePath = "Assets/ProjectH/Scenes/Bootstrap.unity"; // 부트스트랩 씬 경로
        private const string BootstrapRootName = "[ProjectH] Bootstrap"; // 부트스트랩 객체 이름

        [MenuItem("Tools/Project H/Phase 0/5일차 설정 실행")] // 설정 메뉴 등록
        public static void Setup() // 5일차 설정 실행
        {
            EnsureFolders(); // 이벤트 폴더 구성
            List<EventDefinition> definitions = CreatePrototypeDefinitions(); // 프로토타입 이벤트 생성
            ProjectHEventCatalog catalog = CreateCatalog(definitions, true); // 이벤트 카탈로그 생성
            ConfigureBootstrap(catalog); // 부트스트랩 이벤트 연결
            AssetDatabase.SaveAssets(); // 에셋 변경 저장
            AssetDatabase.Refresh(); // 에셋 목록 갱신
            Debug.Log("[Project H] Phase 0 Day 5 setup complete."); // 설정 완료 로그
        }

        [MenuItem("Tools/Project H/Event/이벤트 카탈로그 재구성")] // 이벤트 카탈로그 재구성 메뉴
        public static void RebuildCatalog() // 이벤트 카탈로그 전체 재구성
        {
            EnsureFolders(); // 이벤트 폴더 구성
            string[] guids = AssetDatabase.FindAssets("t:EventDefinition", new[] { EventRoot + "/Definitions" }); // 이벤트 정의 GUID 조회
            List<EventDefinition> definitions = new List<EventDefinition>(); // 이벤트 정의 목록 생성

            foreach (string guid in guids) // 이벤트 GUID 순회
            {
                string path = AssetDatabase.GUIDToAssetPath(guid); // 이벤트 에셋 경로 조회
                EventDefinition definition = AssetDatabase.LoadAssetAtPath<EventDefinition>(path); // 이벤트 정의 로드

                if (definition != null) // 이벤트 정의 확인
                {
                    definitions.Add(definition); // 이벤트 정의 목록 추가
                }
            }

            definitions.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id)); // 이벤트 ID 순 정렬
            ProjectHEventCatalog catalog = CreateCatalog(definitions, false); // 전체 이벤트 카탈로그 재생성
            ConfigureBootstrap(catalog); // 부트스트랩 카탈로그 연결
            AssetDatabase.SaveAssets(); // 에셋 변경 저장
            AssetDatabase.Refresh(); // 에셋 목록 갱신
            Debug.Log($"[Project H] Event catalog rebuilt. Definitions={definitions.Count}"); // 카탈로그 재구성 로그
        }

        private static void EnsureFolders() // 이벤트 폴더 생성
        {
            EnsureFolder("Assets/ProjectH/Data", "Events"); // 이벤트 루트 보장
            EnsureFolder(EventRoot, "Database"); // 이벤트 데이터베이스 보장
            EnsureFolder(EventRoot, "Definitions"); // 이벤트 정의 폴더 보장
        }

        private static void EnsureFolder(string parentPath, string folderName) // 단일 폴더 보장
        {
            string path = parentPath + "/" + folderName; // 전체 폴더 경로 생성

            if (AssetDatabase.IsValidFolder(path)) // 기존 폴더 확인
            {
                return; // 중복 생성 중단
            }

            AssetDatabase.CreateFolder(parentPath, folderName); // 새 폴더 생성
        }

        private static List<EventDefinition> CreatePrototypeDefinitions() // 프로토타입 이벤트 생성
        {
            List<EventDefinition> definitions = new List<EventDefinition>(); // 이벤트 결과 목록 생성
            definitions.Add(CreateDefinition("EV_DEBUG_ALWAYS", "DEBUG · 항상 활성", EventConditionGroupMode.All, new[] { EventCondition.Always() })); // 항상 이벤트 생성
            definitions.Add(CreateDefinition("EV_DEBUG_SERENA_DAY3", "DEBUG · 세레나 Day 3", EventConditionGroupMode.All, new[] // 복합 조건 이벤트 생성
            {
                EventCondition.StoryFlag("STORY_SERENA_JOINED", true), // 세레나 합류 조건
                EventCondition.DayAtLeast(3), // 최소 일차 조건
                EventCondition.CharacterLevelAtLeast("CH_SERENA", 2) // 세레나 레벨 조건
            })); // 복합 조건 이벤트 등록
            return definitions; // 이벤트 결과 반환
        }

        private static EventDefinition CreateDefinition(string id, string displayName, EventConditionGroupMode groupMode, EventCondition[] conditions) // 이벤트 정의 생성
        {
            string path = $"{EventRoot}/Definitions/{id}.asset"; // 이벤트 에셋 경로 생성
            EventDefinition definition = AssetDatabase.LoadAssetAtPath<EventDefinition>(path); // 기존 이벤트 조회

            if (definition == null) // 기존 이벤트 확인
            {
                definition = ScriptableObject.CreateInstance<EventDefinition>(); // 이벤트 인스턴스 생성
                AssetDatabase.CreateAsset(definition, path); // 이벤트 에셋 생성
            }

            SerializedObject serialized = new SerializedObject(definition); // 이벤트 직렬화 객체 생성
            serialized.FindProperty("id").stringValue = id; // 이벤트 ID 설정
            serialized.FindProperty("displayName").stringValue = displayName; // 표시 이름 설정
            serialized.FindProperty("groupMode").enumValueIndex = (int)groupMode; // 조건 그룹 설정
            SerializedProperty conditionProperty = serialized.FindProperty("conditions"); // 조건 배열 조회
            conditionProperty.arraySize = conditions.Length; // 조건 배열 크기 설정

            for (int index = 0; index < conditions.Length; index++) // 조건 목록 순회
            {
                SerializedProperty target = conditionProperty.GetArrayElementAtIndex(index); // 대상 조건 조회
                EventCondition source = conditions[index]; // 원본 조건 조회
                target.FindPropertyRelative("conditionType").enumValueIndex = (int)source.ConditionType; // 조건 종류 복사
                target.FindPropertyRelative("stringValue").stringValue = source.StringValue; // 문자열 값 복사
                target.FindPropertyRelative("intValue").intValue = source.IntValue; // 정수 값 복사
                target.FindPropertyRelative("boolValue").boolValue = source.BoolValue; // 논리 값 복사
            }

            serialized.ApplyModifiedPropertiesWithoutUndo(); // 이벤트 값 적용
            EditorUtility.SetDirty(definition); // 이벤트 변경 표시
            return definition; // 이벤트 정의 반환
        }

        private static ProjectHEventCatalog CreateCatalog(List<EventDefinition> definitions, bool preserveExisting) // 이벤트 카탈로그 생성
        {
            ProjectHEventCatalog catalog = AssetDatabase.LoadAssetAtPath<ProjectHEventCatalog>(CatalogPath); // 기존 카탈로그 조회

            if (catalog == null) // 기존 카탈로그 확인
            {
                catalog = ScriptableObject.CreateInstance<ProjectHEventCatalog>(); // 카탈로그 인스턴스 생성
                AssetDatabase.CreateAsset(catalog, CatalogPath); // 카탈로그 에셋 생성
            }

            List<EventDefinition> merged = new List<EventDefinition>(); // 최종 이벤트 목록 생성

            if (preserveExisting) // 기존 이벤트 보존 확인
            {
                foreach (EventDefinition existing in catalog.Events) // 기존 이벤트 순회
                {
                    if (existing != null) // 기존 이벤트 확인
                    {
                        merged.Add(existing); // 기존 이벤트 보존
                    }
                }
            }

            foreach (EventDefinition definition in definitions) // 신규 이벤트 순회
            {
                bool replaced = false; // 동일 ID 교체 상태 초기화

                for (int index = 0; index < merged.Count; index++) // 기존 이벤트 목록 순회
                {
                    if (!string.Equals(merged[index].Id, definition.Id, System.StringComparison.Ordinal)) // 이벤트 ID 비교
                    {
                        continue; // 다음 이벤트 이동
                    }

                    merged[index] = definition; // 동일 ID 이벤트 갱신
                    replaced = true; // 교체 상태 기록
                    break; // 동일 ID 검색 종료
                }

                if (!replaced) // 기존 동일 ID 확인
                {
                    merged.Add(definition); // 신규 이벤트 추가
                }
            }

            merged.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id)); // 이벤트 ID 순 정렬
            SerializedObject serialized = new SerializedObject(catalog); // 카탈로그 직렬화 객체 생성
            SerializedProperty eventProperty = serialized.FindProperty("events"); // 이벤트 배열 조회
            eventProperty.arraySize = merged.Count; // 이벤트 배열 크기 설정

            for (int index = 0; index < merged.Count; index++) // 이벤트 정의 순회
            {
                eventProperty.GetArrayElementAtIndex(index).objectReferenceValue = merged[index]; // 이벤트 참조 등록
            }

            serialized.ApplyModifiedPropertiesWithoutUndo(); // 카탈로그 값 적용
            EditorUtility.SetDirty(catalog); // 카탈로그 변경 표시
            return catalog; // 이벤트 카탈로그 반환
        }

        private static void ConfigureBootstrap(ProjectHEventCatalog catalog) // 부트스트랩 이벤트 시스템 연결
        {
            Scene scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single); // 부트스트랩 씬 열기
            GameObject root = FindBootstrapRoot(scene); // 부트스트랩 루트 조회

            if (root == null) // 부트스트랩 루트 확인
            {
                Debug.LogError("[Project H] Bootstrap root is missing."); // 루트 누락 로그
                return; // 설정 중단
            }

            if (root.GetComponent<SaveManager>() == null) // 저장 관리자 확인
            {
                Debug.LogError("[Project H] SaveManager is missing. Run previous setup first."); // 저장 관리자 누락 로그
                return; // 설정 중단
            }

            EventManager eventManager = root.GetComponent<EventManager>(); // 이벤트 관리자 조회

            if (eventManager == null) // 이벤트 관리자 확인
            {
                eventManager = root.AddComponent<EventManager>(); // 이벤트 관리자 추가
            }

            SerializedObject serialized = new SerializedObject(eventManager); // 이벤트 관리자 직렬화 객체 생성
            serialized.FindProperty("catalog").objectReferenceValue = catalog; // 이벤트 카탈로그 연결
            serialized.ApplyModifiedPropertiesWithoutUndo(); // 이벤트 관리자 값 적용
            EditorUtility.SetDirty(eventManager); // 이벤트 관리자 변경 표시

            if (root.GetComponent<GameManager>() == null) // 게임 관리자 확인
            {
                root.AddComponent<GameManager>(); // 게임 관리자 추가
            }

            EditorSceneManager.MarkSceneDirty(scene); // 부트스트랩 변경 표시
            EditorSceneManager.SaveScene(scene, BootstrapScenePath); // 부트스트랩 씬 저장
        }

        private static GameObject FindBootstrapRoot(Scene scene) // 부트스트랩 루트 검색
        {
            foreach (GameObject root in scene.GetRootGameObjects()) // 루트 객체 순회
            {
                if (root.name == BootstrapRootName) // 루트 이름 확인
                {
                    return root; // 일치 루트 반환
                }
            }

            return null; // 일치 루트 없음
        }

    }
}
