using NUnit.Framework; // NUnit 테스트 기능
using UnityEditor.SceneManagement; // 에디터 씬 관리
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // Unity 씬 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class Phase1Day10PartySceneLayoutTests // 10일차 파티 Scene 배치 테스트
    {
        private const string PartyScenePath = "Assets/ProjectH/Scenes/Party.unity"; // 파티 씬 경로

        [Test] // 테스트 표시
        public void PartyScene_HasMainCameraAndCenteredPresetButtons() // 카메라와 프리셋 중앙 정렬 검증
        {
            Scene scene = EditorSceneManager.OpenScene(PartyScenePath, OpenSceneMode.Single); // 파티 씬 열기
            Camera mainCamera = FindComponent<Camera>(scene, "Main Camera"); // 메인 카메라 조회
            Assert.That(mainCamera, Is.Not.Null); // 메인 카메라 존재 검증
            Assert.That(mainCamera.CompareTag("MainCamera"), Is.True); // 메인 카메라 태그 검증

            float[] expectedCenters = { 0.2225f, 0.4075f, 0.5925f, 0.7775f }; // 프리셋 버튼 중앙 위치
            for (int index = 0; index < expectedCenters.Length; index++) // 프리셋 버튼 순회
            {
                Button button = FindComponent<Button>(scene, $"PresetButton_{index + 1}"); // 프리셋 버튼 조회
                Assert.That(button, Is.Not.Null); // 프리셋 버튼 존재 검증
                RectTransform rect = button.GetComponent<RectTransform>(); // 프리셋 RectTransform 조회
                float center = (rect.anchorMin.x + rect.anchorMax.x) * 0.5f; // 프리셋 버튼 중심 계산
                Assert.That(center, Is.EqualTo(expectedCenters[index]).Within(0.001f)); // 프리셋 중앙 위치 검증
                Text label = button.GetComponentInChildren<Text>(); // 프리셋 라벨 조회
                Assert.That(label.alignment, Is.EqualTo(TextAnchor.MiddleCenter)); // 프리셋 라벨 중앙 정렬 검증
            }
        }

        private static T FindComponent<T>(Scene scene, string objectName) where T : Component // 씬 컴포넌트 이름 검색
        {
            foreach (GameObject root in scene.GetRootGameObjects()) // 씬 루트 순회
            {
                T found = FindRecursive<T>(root.transform, objectName); // 루트 하위 검색

                if (found != null) // 검색 결과 확인
                {
                    return found; // 검색 컴포넌트 반환
                }
            }

            return null; // 검색 실패 반환
        }

        private static T FindRecursive<T>(Transform root, string objectName) where T : Component // 하위 컴포넌트 재귀 검색
        {
            if (root.name == objectName) // 현재 객체 이름 확인
            {
                return root.GetComponent<T>(); // 현재 객체 컴포넌트 반환
            }

            for (int index = 0; index < root.childCount; index++) // 자식 객체 순회
            {
                T found = FindRecursive<T>(root.GetChild(index), objectName); // 자식 객체 재귀 검색

                if (found != null) // 자식 검색 결과 확인
                {
                    return found; // 검색 컴포넌트 반환
                }
            }

            return null; // 검색 실패 반환
        }
    }
}
