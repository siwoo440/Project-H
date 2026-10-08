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
            Image background = FindBackground(scene); // 씬의 배경 이미지
            if (background == null) return; // 배경 오브젝트 없음
            string dungeonId = BattleContextRuntimeState.ResolveDungeonId(DungeonSelectionRuntimeState.SelectedDungeonId); // 이번 전투의 던전 (다른 패치의 실행 순서와 무관하게 선택 상태에서 직접 읽는다)
            Apply(background, BattleBackgroundCatalog.GetKeyForDungeon(dungeonId)); // 지역 배경 적용
        }

        public static bool Apply(Image background, string backgroundKey) // 배경 그림 적용 (그림이 없으면 false — 씬에 있던 배경을 그대로 둔다)
        {
            if (background == null || !DialogueArtFactory.HasBackgroundArt(backgroundKey)) return false; // 대상·그림 없음
            background.sprite = DialogueArtFactory.GetBackground(backgroundKey); // 지역 배경
            background.color = Color.white; // 원래 색 그대로
            BackgroundFit.Apply(background); // 화면 비율이 달라도 늘리지 않고 잘라서 채움
            EnsureDim(background); // 어둡게 덮는 막
            return true; // 적용함
        }

        private static Image FindBackground(Scene scene) // 씬에서 배경 이미지 찾기
        {
            foreach (GameObject root in scene.GetRootGameObjects()) // 루트 순회
            {
                foreach (Image image in root.GetComponentsInChildren<Image>(true)) // 하위 이미지 순회
                {
                    if (image.gameObject.name == BackgroundObjectName) return image; // 이름 일치
                }
            }

            return null; // 없음
        }

        private static void EnsureDim(Image background) // 배경 바로 위에 어두운 막을 한 겹 (유닛과 글자가 배경에 묻히지 않게)
        {
            Transform parent = background.transform.parent; // 배경의 부모
            if (parent == null || parent.Find(DimObjectName) != null) return; // 부모 없음·이미 있음
            Image dim = RuntimeUiKit.CreateImage(parent, DimObjectName, new Color(0f, 0f, 0f, BattleBackgroundCatalog.DimAlpha)); // 검은 반투명 막
            dim.raycastTarget = false; // 입력 통과
            RuntimeUiKit.Stretch(dim.rectTransform); // 화면 전체
            dim.transform.SetSiblingIndex(background.transform.GetSiblingIndex() + 1); // 배경 바로 위
        }
    }
}
