using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.UI; // 로딩창 Tip 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class LoadingTipCatalogTests // Day55 로딩창 Tip 회귀 테스트
    {
        [Test] // Tip 목록 구성 검증
        public void Catalog_HasNonEmptyTips() // Tip 목록 테스트
        {
            Assert.That(LoadingTipCatalog.All.Count, Is.GreaterThanOrEqualTo(10)); // 충분한 Tip 수 검증

            foreach (string tip in LoadingTipCatalog.All) // 전체 Tip 순회
            {
                Assert.That(tip, Is.Not.Null.And.Not.Empty); // 빈 Tip 없음 검증
            }
        }

        [Test] // 연속 중복 방지 검증
        public void PickNext_NeverRepeatsImmediately() // 연속 중복 테스트
        {
            System.Random random = new System.Random(2024); // 고정 시드 난수 생성기
            string previous = LoadingTipCatalog.PickNext(random); // 첫 Tip 선택

            for (int index = 0; index < 500; index++) // 반복 선택
            {
                string current = LoadingTipCatalog.PickNext(random); // 다음 Tip 선택
                Assert.That(current, Is.Not.EqualTo(previous)); // 직전과 다른 Tip 검증
                previous = current; // 직전 Tip 갱신
            }
        }

        [Test] // 모든 Tip이 고르게 등장하는지 검증
        public void PickNext_EventuallyShowsEveryTip() // 전체 등장 테스트
        {
            System.Random random = new System.Random(7); // 고정 시드 난수 생성기
            System.Collections.Generic.HashSet<string> seen = new System.Collections.Generic.HashSet<string>(); // 등장 Tip 집합

            for (int index = 0; index < 1000; index++) // 충분히 반복 선택
            {
                seen.Add(LoadingTipCatalog.PickNext(random)); // 등장 Tip 기록
            }

            Assert.That(seen.Count, Is.EqualTo(LoadingTipCatalog.All.Count)); // 모든 Tip 등장 검증
        }
    }

    public sealed class LoadingScreenPolicyTests // Day55 로딩창 표시 대상 전환 회귀 테스트
    {
        [Test] // 무거운 전환에서만 표시 검증
        public void ShouldShow_OnlyForBattleAndTitleTransitions() // 표시 대상 테스트
        {
            Assert.That(LoadingScreenView.ShouldShow(ProjectH.Core.GameScenes.Title, ProjectH.Core.GameScenes.Lobby), Is.True); // 타이틀 → 로비 표시 검증
            Assert.That(LoadingScreenView.ShouldShow(ProjectH.Core.GameScenes.DungeonSelect, ProjectH.Core.GameScenes.Battle), Is.True); // 던전 선택 → 전투 표시 검증
            Assert.That(LoadingScreenView.ShouldShow(ProjectH.Core.GameScenes.Battle, ProjectH.Core.GameScenes.DungeonSelect), Is.True); // 전투 → 던전 선택 표시 검증
        }

        [Test] // 메뉴 이동은 표시하지 않음 검증
        public void ShouldShow_SkipsMenuTransitions() // 메뉴 전환 테스트
        {
            string[] menus = { ProjectH.Core.GameScenes.Character, ProjectH.Core.GameScenes.Bag, ProjectH.Core.GameScenes.Shop, ProjectH.Core.GameScenes.Party, ProjectH.Core.GameScenes.DungeonSelect }; // 로비 메뉴 씬 목록

            foreach (string menu in menus) // 메뉴 씬 순회
            {
                Assert.That(LoadingScreenView.ShouldShow(ProjectH.Core.GameScenes.Lobby, menu), Is.False, menu); // 로비 → 메뉴 미표시 검증
                Assert.That(LoadingScreenView.ShouldShow(menu, ProjectH.Core.GameScenes.Lobby), Is.False, menu); // 메뉴 → 로비 미표시 검증
            }

            Assert.That(LoadingScreenView.ShouldShow(ProjectH.Core.GameScenes.Bootstrap, ProjectH.Core.GameScenes.Title), Is.False); // 부트스트랩 → 타이틀 미표시 검증
        }

        [Test] // 전투 진입만 터널 모드인지 검증
        public void ResolveMode_TunnelOnlyWhenEnteringBattle() // 연출 모드 테스트
        {
            Assert.That(LoadingScreenView.ResolveMode(ProjectH.Core.GameScenes.DungeonSelect, ProjectH.Core.GameScenes.Battle), Is.EqualTo(LoadingScreenMode.Tunnel)); // 던전 선택 → 전투 터널 모드 검증
            Assert.That(LoadingScreenView.ResolveMode(ProjectH.Core.GameScenes.Battle, ProjectH.Core.GameScenes.DungeonSelect), Is.EqualTo(LoadingScreenMode.Corner)); // 전투 → 던전 선택 우하단 모드 검증
            Assert.That(LoadingScreenView.ResolveMode(ProjectH.Core.GameScenes.Title, ProjectH.Core.GameScenes.Lobby), Is.EqualTo(LoadingScreenMode.Corner)); // 타이틀 → 로비 우하단 모드 검증
        }
    }

    public sealed class SceneRuntimePatchTests // 씬 Runtime Patch 공통 등록기 회귀 테스트 (최적화)
    {
        private static void NoOp(UnityEngine.SceneManagement.Scene scene) // 테스트용 빈 콜백
        {
        }

        [Test] // 같은 콜백 중복 등록 무시 검증
        public void Register_SameCallbackTwice_RegistersOnce() // 중복 등록 테스트
        {
            const string sceneName = "__TEST_SCENE_RUNTIME_PATCH__"; // 실제 씬과 겹치지 않는 테스트 씬 이름
            int before = ProjectH.Core.SceneRuntimePatch.CountFor(sceneName); // 등록 전 개수

            ProjectH.Core.SceneRuntimePatch.Register(sceneName, NoOp); // 첫 등록
            ProjectH.Core.SceneRuntimePatch.Register(sceneName, NoOp); // 같은 콜백 재등록

            Assert.That(ProjectH.Core.SceneRuntimePatch.CountFor(sceneName), Is.EqualTo(before == 0 ? 1 : before)); // 한 번만 등록 검증
        }

        [Test] // 잘못된 입력 무시 검증
        public void Register_InvalidInput_IsIgnored() // 잘못된 입력 테스트
        {
            ProjectH.Core.SceneRuntimePatch.Register(string.Empty, NoOp); // 빈 씬 이름 등록 시도
            ProjectH.Core.SceneRuntimePatch.Register("__TEST_NULL_CALLBACK__", null); // 빈 콜백 등록 시도

            Assert.That(ProjectH.Core.SceneRuntimePatch.CountFor(string.Empty), Is.EqualTo(0)); // 빈 씬 이름 미등록 검증
            Assert.That(ProjectH.Core.SceneRuntimePatch.CountFor("__TEST_NULL_CALLBACK__"), Is.EqualTo(0)); // 빈 콜백 미등록 검증
        }
    }
}
