using ProjectH.Core; // 프로젝트 핵심 기능
using ProjectH.Data; // 프로젝트 데이터 기능
using ProjectH.SaveSystem; // 프로젝트 저장 기능
using UnityEditor; // Unity 에디터 기능
using UnityEditor.SceneManagement; // 에디터 씬 관리
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // 런타임 씬 자료형

namespace ProjectH.EditorTools // 프로젝트 에디터 도구 영역
{
    public static class Phase0Day3Setup // 3일차 자동 설정 도구
    {
        private const string BootstrapScenePath = "Assets/ProjectH/Scenes/Bootstrap.unity"; // 부트스트랩 씬 경로
        private const string BootstrapRootName = "[ProjectH] Bootstrap"; // 부트스트랩 객체 이름

        [MenuItem("Tools/Project H/Phase 0/3일차 설정 실행")] // 설정 메뉴 등록
        public static void Setup() // 3일차 설정 실행
        {
            ConfigureBootstrap(); // 부트스트랩 저장 시스템 연결
            AssetDatabase.SaveAssets(); // 에셋 변경 저장
            AssetDatabase.Refresh(); // 에셋 목록 갱신
            Debug.Log("[Project H] Phase 0 Day 3 setup complete."); // 설정 완료 로그
        }

        [MenuItem("Tools/Project H/Phase 0/3일차 Debug/1. 새 게임 생성")] // 새 게임 테스트 메뉴
        private static void DebugCreateNewGame() // 새 게임 테스트 실행
        {
            SaveManager saveManager = GetRuntimeSaveManager(); // 런타임 저장 관리자 조회

            if (saveManager != null) // 저장 관리자 확인
            {
                saveManager.CreateNewGame(); // 새 게임 생성 실행
            }
        }

        [MenuItem("Tools/Project H/Phase 0/3일차 Debug/2. 샘플 진행도 적용")] // 진행도 테스트 메뉴
        private static void DebugApplyProgress() // 진행도 테스트 실행
        {
            SaveManager saveManager = GetRuntimeSaveManager(); // 런타임 저장 관리자 조회

            if (saveManager != null) // 저장 관리자 확인
            {
                saveManager.ApplyDebugProgress(); // 샘플 진행도 적용
            }
        }

        [MenuItem("Tools/Project H/Phase 0/3일차 Debug/3. 현재 저장")] // 저장 테스트 메뉴
        private static void DebugSaveCurrent() // 저장 테스트 실행
        {
            SaveManager saveManager = GetRuntimeSaveManager(); // 런타임 저장 관리자 조회

            if (saveManager != null) // 저장 관리자 확인
            {
                saveManager.SaveCurrent(); // 현재 진행 저장
            }
        }

        [MenuItem("Tools/Project H/Phase 0/3일차 Debug/4. 불러오기")] // 불러오기 테스트 메뉴
        private static void DebugLoadCurrent() // 불러오기 테스트 실행
        {
            SaveManager saveManager = GetRuntimeSaveManager(); // 런타임 저장 관리자 조회

            if (saveManager != null) // 저장 관리자 확인
            {
                saveManager.LoadCurrent(); // 기존 진행 불러오기
            }
        }

        [MenuItem("Tools/Project H/Phase 0/3일차 Debug/5. 현재 상태 출력")] // 상태 출력 메뉴
        private static void DebugLogCurrent() // 상태 출력 실행
        {
            SaveManager saveManager = GetRuntimeSaveManager(); // 런타임 저장 관리자 조회

            if (saveManager != null) // 저장 관리자 확인
            {
                saveManager.LogCurrentState(); // 현재 상태 출력
            }
        }

        [MenuItem("Tools/Project H/Phase 0/3일차 Debug/6. 저장 삭제")] // 삭제 테스트 메뉴
        private static void DebugDeleteSave() // 저장 삭제 실행
        {
            SaveManager saveManager = GetRuntimeSaveManager(); // 런타임 저장 관리자 조회

            if (saveManager != null) // 저장 관리자 확인
            {
                saveManager.DeleteSave(); // 저장 파일 삭제
            }
        }

        private static void ConfigureBootstrap() // 부트스트랩 저장 연결
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

            if (bootstrapRoot.GetComponent<DataManager>() == null) // 데이터 관리자 확인
            {
                Debug.LogError("[Project H] DataManager is missing. Run Day 2 setup first."); // 데이터 관리자 누락 로그
                return; // 설정 중단
            }

            if (bootstrapRoot.GetComponent<SaveManager>() == null) // 저장 관리자 확인
            {
                bootstrapRoot.AddComponent<SaveManager>(); // 저장 관리자 추가
            }

            if (bootstrapRoot.GetComponent<GameManager>() == null) // 게임 관리자 확인
            {
                bootstrapRoot.AddComponent<GameManager>(); // 게임 관리자 추가
            }

            EditorSceneManager.MarkSceneDirty(bootstrapScene); // 씬 변경 상태 표시
            EditorSceneManager.SaveScene(bootstrapScene, BootstrapScenePath); // 부트스트랩 씬 저장
        }

        private static GameObject FindBootstrapRoot(Scene bootstrapScene) // 부트스트랩 루트 검색
        {
            GameObject[] rootObjects = bootstrapScene.GetRootGameObjects(); // 씬 루트 목록 조회

            foreach (GameObject rootObject in rootObjects) // 루트 객체 순회
            {
                if (rootObject.name == BootstrapRootName) // 지정 루트 이름 확인
                {
                    return rootObject; // 기존 루트 반환
                }
            }

            return null; // 기존 루트 없음
        }

        private static SaveManager GetRuntimeSaveManager() // 런타임 저장 관리자 조회
        {
            if (!Application.isPlaying) // 플레이 모드 확인
            {
                Debug.LogWarning("[Project H] Enter Play Mode before using Day 3 Debug menus."); // 플레이 모드 안내
                return null; // 조회 실패
            }

            if (GameManager.Instance == null) // 게임 관리자 확인
            {
                Debug.LogError("[Project H] GameManager instance is missing."); // 게임 관리자 누락 로그
                return null; // 조회 실패
            }

            if (GameManager.Instance.Save == null) // 저장 관리자 확인
            {
                Debug.LogError("[Project H] SaveManager instance is missing."); // 저장 관리자 누락 로그
                return null; // 조회 실패
            }

            return GameManager.Instance.Save; // 저장 관리자 반환
        }
    }
}
