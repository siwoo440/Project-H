using System.Linq; // 목록 검색 기능
using NUnit.Framework; // Unity 테스트 기능
using ProjectH.Data; // 프로젝트 데이터 기능
using ProjectH.UI; // 던전 선택 기능
using UnityEditor; // 에디터 에셋 조회 기능

namespace ProjectH.Tests // 프로젝트 테스트 영역
{
    public sealed class DungeonSelectionDataAssetTests // 26일차 던전 데이터 에셋 테스트
    {
        [TestCase("DG001")] // 첫 던전 검증 지정
        [TestCase("DG002")] // 두 번째 던전 검증 지정
        [TestCase("DG003")] // 세 번째 던전 검증 지정
        [TestCase("DG004")] // 네 번째 던전 검증 지정
        [TestCase("DG005")] // 노아르 던전 검증 지정 (Day65)
        [TestCase("DG006")] // 실바란 던전 검증 지정 (Day65)
        public void DungeonAsset_ExistsWithMatchingId(string dungeonId) // 던전 에셋 ID 일치 검증
        {
            string path = $"Assets/ProjectH/Data/Dungeons/{dungeonId}.asset"; // 던전 에셋 경로 생성
            DungeonData dungeon = AssetDatabase.LoadAssetAtPath<DungeonData>(path); // 던전 에셋 조회

            Assert.That(dungeon, Is.Not.Null); // 던전 에셋 존재 검증
            Assert.That(dungeon.Id, Is.EqualTo(dungeonId)); // 던전 ID 일치 검증
            Assert.That(string.IsNullOrWhiteSpace(dungeon.DisplayName), Is.False); // 던전 이름 존재 검증
            Assert.That(dungeon.RecommendedLevel, Is.GreaterThan(0)); // 권장 레벨 유효성 검증
        }

        [Test] // 카탈로그 등록 검증
        public void ProjectCatalog_ContainsAllDay26Dungeons() // 4개 던전 카탈로그 등록 검증
        {
            ProjectHDataCatalog catalog = AssetDatabase.LoadAssetAtPath<ProjectHDataCatalog>("Assets/ProjectH/Data/Database/ProjectHDataCatalog.asset"); // 데이터 카탈로그 조회

            Assert.That(catalog, Is.Not.Null); // 카탈로그 존재 검증

            foreach (string dungeonId in DungeonSelectionRuntimeState.SupportedDungeonIds) // 지원 던전 ID 순회
            {
                Assert.That(catalog.Dungeons.Any(dungeon => dungeon != null && dungeon.Id == dungeonId), Is.True, dungeonId); // 카탈로그 던전 등록 검증
            }
        }
    }
}
