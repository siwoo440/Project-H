using System; // 문자열 비교 기능
using ProjectH.Core; // 씬 이름 기능
using ProjectH.Dungeon; // 노드형 던전 탐험 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // 씬 로드 이벤트 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class DungeonMapRuntimePatch // 던전 선택 씬 복귀 시 탐험 지도 자동 재오픈 (Day55 신규)
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // Runtime 지도 패치 등록
        private static void Register() // 씬 로드 이벤트 등록
        {
            SceneRuntimePatch.Register(GameScenes.DungeonSelect, OnSceneLoaded); // DungeonSelect 씬 로드 시 주입 등록 (최적화 — 공통 등록기 사용)
        }

        private static void OnSceneLoaded(Scene scene) // 던전 선택 씬 로드 완료 처리
        {
            if (!DungeonRunState.IsActive && DungeonRunState.LastEndKind == DungeonRunEndKind.None) // 탐험 진행·종료 요약 대기 여부 확인
            {
                return; // 탐험과 무관한 방문 무시
            }

            DungeonMapOverlayView.Open(); // 전투 복귀 후 지도 재오픈 (진행 중이면 지도, 종료 시 요약 표시)
        }
    }
}
