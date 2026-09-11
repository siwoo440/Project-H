using System; // 문자열 비교 기능
using ProjectH.Core; // 씬 이름 기능
using ProjectH.Dungeon; // 노드형 던전 탐험 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // 씬 로드 이벤트 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class DungeonMapRuntimePatch // 던전 선택 씬 복귀 시 탐험 지도 자동 재오픈 (Day55 신규)
    {
        private static bool registered; // 씬 이벤트 등록 상태

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // Runtime 지도 패치 등록
        private static void Register() // 씬 로드 이벤트 등록
        {
            if (registered) // 기존 등록 상태 확인
            {
                return; // 중복 등록 차단
            }

            registered = true; // 등록 상태 저장
            SceneManager.sceneLoaded += OnSceneLoaded; // 씬 로드 완료 이벤트 연결
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) // 던전 선택 씬 로드 완료 처리
        {
            if (!string.Equals(scene.name, GameScenes.DungeonSelect, StringComparison.Ordinal)) // 던전 선택 씬 여부 확인
            {
                return; // 다른 씬 무시
            }

            if (!DungeonRunState.IsActive && DungeonRunState.LastEndKind == DungeonRunEndKind.None) // 탐험 진행·종료 요약 대기 여부 확인
            {
                return; // 탐험과 무관한 방문 무시
            }

            DungeonMapOverlayView.Open(); // 전투 복귀 후 지도 재오픈 (진행 중이면 지도, 종료 시 요약 표시)
        }
    }
}
