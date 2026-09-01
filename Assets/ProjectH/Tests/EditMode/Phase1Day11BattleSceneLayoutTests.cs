using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 Scene 기능
using UnityEditor.SceneManagement; // 에디터 씬 관리
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // Unity 씬 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class Phase1Day11BattleSceneLayoutTests // 11일차 전투 Scene 구조 테스트
    {
        private const string BattleScenePath = "Assets/ProjectH/Scenes/Battle.unity"; // 전투 씬 경로

        [Test] // 테스트 표시
        public void BattleScene_HasCameraControllerAndFormationAnchors() // 전투 Scene 핵심 구조 검증
        {
            Scene scene = EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Single); // 전투 씬 열기
            Camera camera = FindComponent<Camera>(scene, "Main Camera"); // 메인 카메라 조회
            BattleScreenController controller = FindComponent<BattleScreenController>(scene, "BattleController"); // 전투 컨트롤러 조회
            BattleFormationAnchors anchors = FindComponent<BattleFormationAnchors>(scene, "BattleFormation"); // 전투 배치 앵커 조회

            Assert.That(camera, Is.Not.Null); // 메인 카메라 존재 검증
            Assert.That(camera.CompareTag("MainCamera"), Is.True); // 메인 카메라 태그 검증
            Assert.That(camera.orthographic, Is.True); // 직교 카메라 검증
            Assert.That(controller, Is.Not.Null); // 전투 컨트롤러 존재 검증
            Assert.That(anchors, Is.Not.Null); // 전투 배치 컴포넌트 존재 검증
            Assert.That(anchors.AllyCount, Is.EqualTo(4)); // 아군 슬롯 수 검증
            Assert.That(anchors.EnemyCount, Is.EqualTo(5)); // 적군 슬롯 수 검증
        }

        private static T FindComponent<T>(Scene scene, string objectName) where T : Component // 씬 컴포넌트 이름 검색
        {
            foreach (GameObject root in scene.GetRootGameObjects()) // 씬 루트 순회
            {
                T found = FindRecursive<T>(root.transform, objectName); // 하위 객체 검색

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

                if (found != null) // 검색 결과 확인
                {
                    return found; // 검색 컴포넌트 반환
                }
            }

            return null; // 검색 실패 반환
        }
    }
}
