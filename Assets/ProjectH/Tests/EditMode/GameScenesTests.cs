using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Core; // 프로젝트 핵심 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class GameScenesTests // 씬 이름 테스트
    {
        [Test] // 테스트 표시
        public void CoreSceneNames_AreUnique() // 핵심 씬 이름 중복 검증
        {
            string[] names = // 씬 이름 목록
            {
                GameScenes.Bootstrap, // 부트스트랩 씬
                GameScenes.Title, // 타이틀 씬
                GameScenes.Lobby, // 로비 씬
                GameScenes.Party, // 파티 씬
                GameScenes.Shop, // 상점 씬 (Day61 추가)
                GameScenes.Blacksmith, // 대장간 씬 (Day61 추가)
                GameScenes.Village, // 마을 씬 (Day62 추가)
                GameScenes.Diary, // 일기장 씬 (Day63 추가)
                GameScenes.DungeonSelect, // 던전 씬
                GameScenes.Battle // 전투 씬
            }; // 씬 이름 목록 종료

            CollectionAssert.AllItemsAreUnique(names); // 씬 이름 고유성 검증
        }

        [Test] // 빌드 목록의 씬은 모두 실제 파일이 있다 (씬을 지우고 목록에 남기면 빌드가 실패한다)
        public void BuildSettings_ListOnlyExistingScenes() // 빌드 씬 목록 테스트 (Day83 — 쓰이지 않던 Result 씬 삭제)
        {
            Assert.That(UnityEditor.EditorBuildSettings.scenes.Length, Is.GreaterThanOrEqualTo(12)); // 12개 씬 이상

            foreach (UnityEditor.EditorBuildSettingsScene scene in UnityEditor.EditorBuildSettings.scenes) // 빌드 씬 순회
            {
                Assert.That(System.IO.File.Exists(scene.path), Is.True, $"빌드 목록에 있지만 파일이 없는 씬 : {scene.path}"); // 파일 존재
            }
        }
    }
}
