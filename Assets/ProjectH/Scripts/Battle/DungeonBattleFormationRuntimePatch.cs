using System.Collections.Generic; // 목록 자료형
using ProjectH.Core; // 프로젝트 씬 상수 기능
using ProjectH.Data; // 던전 데이터 기능
using ProjectH.UI; // 던전 선택 상태 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // Unity 씬 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class DungeonBattleFormationRuntimePatch // 던전별 적 편성 런타임 연결
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // 첫 씬 로드 전 이벤트 구독 지정
        private static void RegisterSceneLoadedHandler() // 씬 로드 이벤트 구독
        {
            SceneRuntimePatch.Register(GameScenes.Battle, HandleSceneLoaded); // Battle 씬 로드 시 주입 등록 (최적화 — 공통 등록기 사용)
        }

        private static void HandleSceneLoaded(Scene scene) // 씬 로드 완료 처리
        {
            string dungeonId = BattleContextRuntimeState.Capture(DungeonSelectionRuntimeState.SelectedDungeonId); // 선택 던전 전투 컨텍스트 확정
            ApplyFormation(dungeonId); // 확정 던전 실제 적 편성 적용
        }

        private static void ApplyFormation(string dungeonId) // Battle 컨트롤러 적 편성 적용
        {
            BattleScreenController controller = UnityEngine.Object.FindFirstObjectByType<BattleScreenController>(); // Battle 화면 컨트롤러 조회

            if (controller == null) // Battle 화면 컨트롤러 존재 확인
            {
                Debug.LogError("[Project H][DAY28] BattleScreenController를 찾을 수 없습니다."); // 컨트롤러 누락 오류 출력
                return; // 적 편성 적용 중단
            }

            DungeonBattleFormationProfile profile = DungeonBattleFormationProfile.Get(dungeonId); // 확정 던전 적 편성 조회
            controller.ConfigureDefaultEnemies(profile.CreateEnemyIds()); // 기존 적군 생성 입력에 던전 편성 주입 (레거시 단일 웨이브 폴백, 공개 설정 함수 사용)
            Debug.Log($"[Project H][DAY28] {profile.DungeonId} 적 편성 {profile.EnemyCount}명 적용"); // 던전 편성 적용 로그 출력
            ApplyEncounterWaves(controller, dungeonId, profile); // 던전 인카운터 웨이브 편성 적용 (Day45)
        }

        private static void ApplyEncounterWaves(BattleScreenController controller, string dungeonId, DungeonBattleFormationProfile fallbackProfile) // 던전 인카운터 웨이브 편성 적용 (Day45)
        {
            DungeonData dungeon = GameManager.Instance == null || GameManager.Instance.Data == null ? null : GameManager.Instance.Data.GetDungeon(dungeonId); // 확정 던전 원본 데이터 조회
            List<string[]> waves = DungeonEncounterResolver.ResolveWaves(dungeon, fallbackProfile.CreateEnemyIds()); // 던전 데이터 기반 웨이브 목록 해석
            waves = ProjectH.Dungeon.DungeonRunState.ResolveBattleWaves(waves); // 노드형 탐험 중이면 노드 종류별 웨이브로 교체 (Day55 추가, 탐험 외 전투는 원본 유지)
            controller.ConfigureEncounterWaves(waves); // 전투 화면에 웨이브 편성 주입
            Debug.Log($"[Project H][DAY45] {dungeonId} 인카운터 웨이브 {waves.Count}개 적용"); // 웨이브 편성 적용 로그 출력
        }
    }
}
