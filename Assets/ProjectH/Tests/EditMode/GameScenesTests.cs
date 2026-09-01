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
                GameScenes.DungeonSelect, // 던전 씬
                GameScenes.Battle, // 전투 씬
                GameScenes.Result // 결과 씬
            }; // 씬 이름 목록 종료

            CollectionAssert.AllItemsAreUnique(names); // 씬 이름 고유성 검증
        }
    }
}
