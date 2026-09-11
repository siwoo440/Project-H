using ProjectH.Battle; // 전투 패시브 기능
using UnityEditor; // Unity 에디터 기능
using UnityEditor.SceneManagement; // Unity Scene 편집 기능
using UnityEngine; // Unity GameObject 기능
using UnityEngine.SceneManagement; // Unity Scene 기능

namespace ProjectH.EditorTools // 프로젝트 에디터 도구 영역
{
    public static class Phase1Day20Setup // 20일차 패시브 시스템 설정 도구
    {
        private const string BattleScenePath = "Assets/ProjectH/Scenes/Battle.unity"; // 전투 Scene 경로

        [MenuItem("Tools/Project H/Phase 1/20일차 패시브 시스템 설정 실행")] // 20일차 설정 메뉴 등록
        public static void Setup() // Battle Scene 패시브 Driver 자동 연결
        {
            Scene scene = EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Single); // Battle Scene 열기
            GameObject battleController = FindRequiredObject(scene, "BattleController"); // BattleController 객체 조회
            BattleCombatRegistry registry = battleController.GetComponent<BattleCombatRegistry>(); // 기존 전투 Registry 조회

            if (registry == null) // 전투 Registry 존재 확인
            {
                throw new System.InvalidOperationException("BattleController에서 BattleCombatRegistry를 찾을 수 없습니다."); // Registry 누락 예외 발생
            }

            BattlePassiveDriver driver = battleController.GetComponent<BattlePassiveDriver>(); // 기존 패시브 Driver 조회

            if (driver == null) // 패시브 Driver 존재 확인
            {
                driver = battleController.AddComponent<BattlePassiveDriver>(); // 신규 패시브 Driver 추가
            }

            driver.Configure(registry); // 패시브 Driver Registry 연결
            EditorUtility.SetDirty(driver); // 패시브 Driver 변경 표시
            EditorSceneManager.MarkSceneDirty(scene); // Battle Scene 변경 표시
            EditorSceneManager.SaveScene(scene, BattleScenePath); // Battle Scene 변경 저장
            AssetDatabase.SaveAssets(); // 변경 에셋 저장
            AssetDatabase.Refresh(); // Unity 에셋 목록 갱신
            Debug.Log("[Project H][DAY20] Passive system setup complete. Serena/Ellen/Lilia/Eve passives connected."); // 20일차 설정 완료 로그
        }

        private static GameObject FindRequiredObject(Scene scene, string objectName) // Scene 객체 이름 기반 필수 조회
        {
            GameObject[] roots = scene.GetRootGameObjects(); // Scene 루트 객체 목록 조회

            for (int index = 0; index < roots.Length; index++) // Scene 루트 객체 순회
            {
                Transform found = FindRecursive(roots[index].transform, objectName); // 현재 루트 하위 객체 재귀 조회

                if (found != null) // 대상 객체 발견 확인
                {
                    return found.gameObject; // 대상 GameObject 반환
                }
            }

            throw new System.InvalidOperationException($"Battle Scene에서 {objectName} 객체를 찾을 수 없습니다."); // 필수 객체 누락 예외 발생
        }

        private static Transform FindRecursive(Transform root, string objectName) // Transform 이름 재귀 조회
        {
            if (root == null) // 루트 Transform 확인
            {
                return null; // 빈 루트 조회 실패
            }

            if (root.name == objectName) // 현재 Transform 이름 일치 확인
            {
                return root; // 일치 Transform 반환
            }

            for (int index = 0; index < root.childCount; index++) // 하위 Transform 순회
            {
                Transform found = FindRecursive(root.GetChild(index), objectName); // 하위 Transform 재귀 조회

                if (found != null) // 하위 대상 발견 확인
                {
                    return found; // 발견 Transform 반환
                }
            }

            return null; // 대상 Transform 없음 반환
        }
    }
}
