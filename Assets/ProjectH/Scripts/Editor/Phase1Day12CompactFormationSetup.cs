using ProjectH.Battle; // 전투 진형 기능
using UnityEditor; // Unity 에디터 기능
using UnityEditor.SceneManagement; // 에디터 씬 관리
using UnityEngine; // Unity 위치 기능
using UnityEngine.SceneManagement; // Unity 씬 기능

namespace ProjectH.EditorTools // 프로젝트 에디터 도구 영역
{
    public static class Phase1Day12CompactFormationSetup // 12일차 전투 진형 간격 축소 도구
    {
        private const string BattleScenePath = "Assets/ProjectH/Scenes/Battle.unity"; // 전투 씬 경로

        private static readonly Vector3[] AllyPositions = // 아군 압축 진형 좌표
        {
            new Vector3(-3.20f, -0.25f, 0f), // 아군 슬롯 0 압축 좌표
            new Vector3(-3.50f, -0.05f, 0f), // 아군 슬롯 1 압축 좌표
            new Vector3(-3.75f, 0.20f, 0f), // 아군 슬롯 2 압축 좌표
            new Vector3(-4.00f, 0.40f, 0f) // 아군 슬롯 3 압축 좌표
        }; // 아군 압축 진형 좌표 종료

        private static readonly Vector3[] EnemyPositions = // 적군 압축 진형 좌표
        {
            new Vector3(3.10f, -0.15f, 0f), // 적군 슬롯 0 압축 좌표
            new Vector3(3.45f, 0.15f, 0f), // 적군 슬롯 1 압축 좌표
            new Vector3(3.75f, 0.35f, 0f), // 적군 슬롯 2 압축 좌표
            new Vector3(4.05f, 0.50f, 0f), // 적군 슬롯 3 압축 좌표
            new Vector3(4.35f, 0.05f, 0f) // 적군 슬롯 4 압축 좌표
        }; // 적군 압축 진형 좌표 종료

        [MenuItem("Tools/Project H/Phase 1/12일차 전투 진형 Y 간격 축소")] // 전투 진형 간격 축소 메뉴 등록
        public static void Apply() // 현재 Battle Scene 진형 압축 적용
        {
            Scene scene = EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Single); // 현재 전투 씬 열기
            BattleFormationAnchors formation = FindRequiredComponent<BattleFormationAnchors>(scene, "BattleFormation"); // 전투 진형 컴포넌트 조회
            BattleFormationLayout.ApplyPositions(formation, AllyPositions, EnemyPositions); // Runtime 공통 진형 좌표 적용
            MarkAnchorsDirty(formation); // 진형 Transform 변경 표시
            EditorUtility.SetDirty(formation); // 진형 컴포넌트 변경 표시
            EditorSceneManager.MarkSceneDirty(scene); // 전투 씬 변경 표시
            EditorSceneManager.SaveScene(scene, BattleScenePath); // 전투 씬 저장
            AssetDatabase.SaveAssets(); // 변경 에셋 저장
            Debug.Log("[Project H][BATTLE] Compact formation applied. Ally Y span=0.65, Enemy Y span=0.65."); // 진형 압축 완료 로그
        }

        private static void MarkAnchorsDirty(BattleFormationAnchors formation) // 진형 Transform 변경 표시
        {
            for (int index = 0; index < formation.AllyCount; index++) // 아군 앵커 순회
            {
                Transform anchor = formation.GetAllyAnchor(index); // 아군 앵커 조회

                if (anchor != null) // 아군 앵커 존재 확인
                {
                    EditorUtility.SetDirty(anchor); // 아군 Transform 변경 표시
                }
            }

            for (int index = 0; index < formation.EnemyCount; index++) // 적군 앵커 순회
            {
                Transform anchor = formation.GetEnemyAnchor(index); // 적군 앵커 조회

                if (anchor != null) // 적군 앵커 존재 확인
                {
                    EditorUtility.SetDirty(anchor); // 적군 Transform 변경 표시
                }
            }
        }

        private static T FindRequiredComponent<T>(Scene scene, string objectName) where T : Component // 씬 필수 컴포넌트 조회
        {
            foreach (GameObject root in scene.GetRootGameObjects()) // 씬 루트 객체 순회
            {
                Transform found = FindRecursive(root.transform, objectName); // 하위 객체 검색

                if (found == null) // 현재 루트 검색 결과 확인
                {
                    continue; // 다음 루트 이동
                }

                T component = found.GetComponent<T>(); // 대상 컴포넌트 조회

                if (component != null) // 대상 컴포넌트 존재 확인
                {
                    return component; // 대상 컴포넌트 반환
                }
            }

            throw new System.InvalidOperationException($"{objectName} 또는 {typeof(T).Name}을 찾을 수 없습니다."); // 필수 전투 객체 누락 예외 발생
        }

        private static Transform FindRecursive(Transform root, string objectName) // 하위 Transform 재귀 검색
        {
            if (root.name == objectName) // 현재 객체 이름 확인
            {
                return root; // 현재 Transform 반환
            }

            for (int index = 0; index < root.childCount; index++) // 자식 객체 순회
            {
                Transform found = FindRecursive(root.GetChild(index), objectName); // 자식 객체 재귀 검색

                if (found != null) // 자식 검색 결과 확인
                {
                    return found; // 검색 Transform 반환
                }
            }

            return null; // 검색 실패 반환
        }
    }
}
