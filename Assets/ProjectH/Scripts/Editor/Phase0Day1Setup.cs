using System.Collections.Generic; // 목록 자료형
using ProjectH.Core; // 프로젝트 핵심 기능
using UnityEditor; // Unity 에디터 기능
using UnityEditor.SceneManagement; // 에디터 씬 관리
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // 런타임 씬 자료형

namespace ProjectH.EditorTools // 프로젝트 에디터 도구 영역
{
    public static class Phase0Day1Setup // 1일차 자동 설정 도구
    {
        private const string ProjectRoot = "Assets/ProjectH"; // 프로젝트 루트 경로
        private const string BootstrapScenePath = "Assets/ProjectH/Scenes/Bootstrap.unity"; // 부트스트랩 씬 경로
        private const string BootstrapRootName = "[ProjectH] Bootstrap"; // 부트스트랩 객체 이름

        [MenuItem("Tools/Project H/Phase 0/1일차 설정 실행")] // 설정 메뉴 등록
        public static void Setup() // 1일차 설정 실행
        {
            EnsureFolders(); // 기본 폴더 구성
            EnsureBootstrapScene(); // 부트스트랩 씬 구성
            ConfigureBuildSettings(); // 빌드 씬 순서 구성
            AssetDatabase.SaveAssets(); // 에셋 변경 저장
            AssetDatabase.Refresh(); // 에셋 목록 갱신
            Debug.Log("[Project H] Phase 0 Day 1 setup complete."); // 설정 완료 로그
        }

        private static void EnsureFolders() // 기본 폴더 생성
        {
            EnsureFolder("Assets", "ProjectH"); // 프로젝트 루트 생성
            EnsureFolder(ProjectRoot, "Art"); // 아트 폴더 생성
            EnsureFolder(ProjectRoot, "Audio"); // 오디오 폴더 생성
            EnsureFolder(ProjectRoot, "Data"); // 데이터 폴더 생성
            EnsureFolder(ProjectRoot, "Prefabs"); // 프리팹 폴더 생성
            EnsureFolder(ProjectRoot, "Scenes"); // 씬 폴더 생성
            EnsureFolder(ProjectRoot, "Scripts"); // 스크립트 폴더 생성
            EnsureFolder(ProjectRoot, "UI"); // UI 폴더 생성
            EnsureFolder(ProjectRoot + "/Scripts", "Core"); // 핵심 스크립트 폴더 생성
            EnsureFolder(ProjectRoot + "/Scripts", "Editor"); // 에디터 스크립트 폴더 생성
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

        private static void EnsureBootstrapScene() // 부트스트랩 씬 구성
        {
            Scene bootstrapScene = SceneManager.GetSceneByPath(BootstrapScenePath); // 열린 부트스트랩 씬 조회
            bool shouldCloseScene = false; // 작업 후 닫기 여부 초기화

            if (!bootstrapScene.IsValid() || !bootstrapScene.isLoaded) // 열린 씬 존재 여부 확인
            {
                bootstrapScene = OpenOrCreateBootstrapScene(); // 부트스트랩 씬 열기
                shouldCloseScene = true; // 임시로 연 씬 표시
            }

            GameObject bootstrapRoot = FindBootstrapRoot(bootstrapScene); // 부트스트랩 루트 조회

            if (bootstrapRoot == null) // 부트스트랩 루트 존재 확인
            {
                bootstrapRoot = new GameObject(BootstrapRootName); // 부트스트랩 루트 생성
                SceneManager.MoveGameObjectToScene(bootstrapRoot, bootstrapScene); // 대상 씬으로 객체 이동
            }

            if (bootstrapRoot.GetComponent<SceneLoader>() == null) // 씬 로더 존재 확인
            {
                bootstrapRoot.AddComponent<SceneLoader>(); // 씬 로더 추가
            }

            if (bootstrapRoot.GetComponent<GameManager>() == null) // 게임 관리자 존재 확인
            {
                bootstrapRoot.AddComponent<GameManager>(); // 게임 관리자 추가
            }

            EditorSceneManager.MarkSceneDirty(bootstrapScene); // 씬 변경 상태 표시
            EditorSceneManager.SaveScene(bootstrapScene, BootstrapScenePath); // 부트스트랩 씬 저장

            if (shouldCloseScene) // 임시 씬 여부 확인
            {
                EditorSceneManager.CloseScene(bootstrapScene, true); // 작업 씬 닫기
            }
        }

        private static Scene OpenOrCreateBootstrapScene() // 부트스트랩 씬 준비
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath) != null) // 기존 씬 에셋 확인
            {
                return EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive); // 기존 씬 추가 로드
            }

            return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive); // 빈 씬 추가 생성
        }

        private static GameObject FindBootstrapRoot(Scene bootstrapScene) // 부트스트랩 루트 검색
        {
            GameObject[] rootObjects = bootstrapScene.GetRootGameObjects(); // 씬 루트 객체 조회

            foreach (GameObject rootObject in rootObjects) // 루트 객체 순회
            {
                if (rootObject.name == BootstrapRootName) // 지정 루트 이름 확인
                {
                    return rootObject; // 기존 루트 반환
                }
            }

            return null; // 기존 루트 없음
        }

        private static void ConfigureBuildSettings() // 빌드 씬 목록 구성
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes); // 기존 빌드 씬 복사

            for (int index = scenes.Count - 1; index >= 0; index--) // 기존 씬 역순 확인
            {
                if (scenes[index].path == BootstrapScenePath) // 부트스트랩 중복 확인
                {
                    scenes.RemoveAt(index); // 중복 부트스트랩 제거
                }
            }

            scenes.Insert(0, new EditorBuildSettingsScene(BootstrapScenePath, true)); // 첫 빌드 씬 등록
            EditorBuildSettings.scenes = scenes.ToArray(); // 빌드 씬 목록 적용
        }
    }
}
