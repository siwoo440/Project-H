using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 Scene 기능
using UnityEditor.SceneManagement; // 에디터 씬 관리
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // Unity 씬 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class Phase1Day12BattleSceneLayoutTests // 12일차 전투 Scene 구조 테스트
    {
        private const string BattleScenePath = "Assets/ProjectH/Scenes/Battle.unity"; // 전투 씬 경로

        [Test] // 테스트 표시
        public void BattleScene_HasCombatRegistryEnemyTemplateAndDebugButtons() // 전투 행동 Scene 구조 검증
        {
            Scene scene = EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Single); // 전투 씬 열기
            BattleCombatRegistry registry = FindComponent<BattleCombatRegistry>(scene, "BattleController"); // 전투 레지스트리 조회
            BattleEnemyView enemyTemplate = FindComponent<BattleEnemyView>(scene, "BattleEnemyTemplate"); // 적군 템플릿 조회
            BattleActionDebugText allyDebugText = FindComponent<BattleActionDebugText>(scene, "ActionDebugText"); // 아군 행동 텍스트 조회
            Button attackButton = FindComponent<Button>(scene, "DebugAttackButton"); // 공격 디버그 버튼 조회
            Button skillButton = FindComponent<Button>(scene, "DebugSkillButton"); // 스킬 디버그 버튼 조회
            Button ultimateButton = FindComponent<Button>(scene, "DebugUltimateButton"); // 궁극기 디버그 버튼 조회

            Assert.That(registry, Is.Not.Null); // 전투 레지스트리 존재 검증
            Assert.That(enemyTemplate, Is.Not.Null); // 적군 템플릿 존재 검증
            Assert.That(allyDebugText, Is.Not.Null); // 아군 행동 디버그 텍스트 존재 검증
            Assert.That(attackButton, Is.Not.Null); // 공격 디버그 버튼 존재 검증
            Assert.That(skillButton, Is.Not.Null); // 스킬 디버그 버튼 존재 검증
            Assert.That(ultimateButton, Is.Not.Null); // 궁극기 디버그 버튼 존재 검증
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
                T component = root.GetComponent<T>(); // 현재 객체 컴포넌트 조회

                if (component != null) // 현재 객체 컴포넌트 확인
                {
                    return component; // 현재 객체 컴포넌트 반환
                }
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
