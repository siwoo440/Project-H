using ProjectH.Core; // 씬 이름·런타임 패치 등록 기능
using ProjectH.UI; // 배경 그림·비율 맞춤·던전 선택 상태 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // Unity 씬 기능
using UnityEngine.UI; // Image 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleBackgroundRuntimePatch // 전투 배경 연결 (Day78 신규 — 던전의 지역에 맞는 배경으로 바꾸고 살짝 어둡게 덮는다)
    {
        private const string BackgroundObjectName = "BattleBackground"; // 씬에 놓인 배경 오브젝트 이름
        private const string DimObjectName = "BattleBackgroundDim"; // 배경을 어둡게 덮는 막 이름

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // 첫 씬 로드 전 이벤트 구독 지정
        private static void RegisterSceneLoadedHandler() // 씬 로드 이벤트 구독
        {
            SceneRuntimePatch.Register(GameScenes.Battle, HandleSceneLoaded); // Battle 씬 로드 시 배경 적용 등록
        }

        private static void HandleSceneLoaded(Scene scene) // 씬 로드 완료 처리
        {
            Image background = SceneBackdrop.Find(scene, BackgroundObjectName); // 씬의 배경 이미지 (Day80 — 공용 기능 사용)
            if (background == null) return; // 배경 오브젝트 없음
            string dungeonId = BattleContextRuntimeState.ResolveDungeonId(DungeonSelectionRuntimeState.SelectedDungeonId); // 이번 전투의 던전 (다른 패치의 실행 순서와 무관하게 선택 상태에서 직접 읽는다)
            Apply(background, BattleBackgroundCatalog.GetKeyForDungeon(dungeonId)); // 지역 배경 적용
        }

        public static bool Apply(Image background, string backgroundKey) // 배경 그림 적용 (그림이 없으면 false — 씬에 있던 배경을 그대로 둔다)
        {
            return SceneBackdrop.Apply(background, backgroundKey, BattleBackgroundCatalog.DimAlpha, DimObjectName); // 그림 · 비율 맞춤 · 어두운 막 (Day80 — 로비와 같은 공용 기능)
        }
    }
}
