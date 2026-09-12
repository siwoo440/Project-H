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
        [TestCase("DG007")] // 카르니안 얼어붙은 요새 (Day66)
        [TestCase("DG008")] // 아스타르 잠든 봉인 신전 (Day66)
        [TestCase("DG009")] // 늪지대 독안개 저습지 (Day66)
        [TestCase("DG010")] // 노아르 의회 금단 실험실 (Day66)
        [TestCase("DG011")] // 실바란 세계수 뿌리 성소 (Day66)
        [TestCase("DG012")] // 마왕성 검은 성벽 (Day66)
        [TestCase("DG013")] // 카르니안 설원 전선 병영 (Day66)
        [TestCase("DG014")] // 아스타르 지하 고대 도시 (Day66)
        [TestCase("DG015")] // 바다 검은 균열 해안 (Day67)
        [TestCase("DG016")] // 바다 균열 심층 (Day67)
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
