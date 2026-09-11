using System.Collections.Generic; // 목록 자료형
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Data; // 던전 데이터 기능
using ProjectH.Dungeon; // 모험 지역 기능
using ProjectH.UI; // 지원 던전 목록 기능
using UnityEditor; // 에셋 로드 기능
using UnityEngine; // 좌표 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class AdventureMapTests // Day63 모험 지도 : 지역 · 던전 연결 테스트
    {
        [Test] // 목업 6지역 + Day65 노아르 · 실바란 구성
        public void Regions_MatchMockupNamesAndKinds() // 구성 테스트
        {
            List<string> names = new List<string>(); // 이름 목록
            foreach (AdventureRegion region in AdventureRegionCatalog.All) names.Add(region.Name); // 이름 수집

            Assert.That(names, Is.EquivalentTo(new[] { "숲", "늪지대", "마왕성", "마을", "사막", "바다", "노아르", "실바란", "카르니안" })); // 목업 10번 지역 + 65~66일차 지역 확장
            Assert.That(AdventureRegionCatalog.Get(AdventureRegionCatalog.VillageRegionId).Kind, Is.EqualTo(AdventureRegionKind.Village)); // 마을 연결
            Assert.That(AdventureRegionCatalog.FindByDungeon("DG003").Name, Is.EqualTo("늪지대")); // 던전 → 지역
        }

        [Test] // 모든 지원 던전은 정확히 한 지역에 속함 · 던전 에셋 존재
        public void EverySupportedDungeon_BelongsToExactlyOneRegion() // 연결 테스트
        {
            foreach (string dungeonId in DungeonSelectionRuntimeState.SupportedDungeonIds) // 던전 순회
            {
                int count = 0; // 속한 지역 수

                foreach (AdventureRegion region in AdventureRegionCatalog.All) // 지역 순회
                {
                    foreach (string id in region.DungeonIds) if (id == dungeonId) count++; // 포함 수
                }

                Assert.That(count, Is.EqualTo(1), dungeonId); // 정확히 한 곳
                Assert.That(AssetDatabase.LoadAssetAtPath<DungeonData>($"Assets/ProjectH/Data/Dungeons/{dungeonId}.asset"), Is.Not.Null, dungeonId); // 에셋 존재
            }
        }

        [Test] // 잠긴 지역은 던전이 없고 안내가 있음 · 던전 지역은 던전이 있음
        public void LockedRegions_HaveHints_DungeonRegionsHaveDungeons() // 종류 테스트
        {
            foreach (AdventureRegion region in AdventureRegionCatalog.All) // 지역 순회
            {
                if (region.Kind == AdventureRegionKind.Locked) // 잠김
                {
                    Assert.That(region.DungeonIds, Is.Empty, region.Id); // 던전 없음
                    Assert.That(region.LockedHint, Is.Not.Empty, region.Id); // 안내 있음
                }
                else if (region.Kind == AdventureRegionKind.Dungeon) // 던전 지역
                {
                    Assert.That(region.DungeonIds, Is.Not.Empty, region.Id); // 던전 있음
                }
            }
        }

        [Test] // 지도 위 위치가 화면 안에 있고 서로 겹치지 않음
        public void RegionPositions_AreInsideMapAndApart() // 위치 테스트
        {
            IReadOnlyList<AdventureRegion> regions = AdventureRegionCatalog.All; // 지역 목록

            for (int a = 0; a < regions.Count; a++) // 지역 순회
            {
                Assert.That(regions[a].Position.x, Is.InRange(0.05f, 0.95f), regions[a].Id); // 가로 범위
                Assert.That(regions[a].Position.y, Is.InRange(0.05f, 0.95f), regions[a].Id); // 세로 범위

                for (int b = a + 1; b < regions.Count; b++) // 다른 지역
                {
                    Assert.That(Vector2.Distance(regions[a].Position, regions[b].Position), Is.GreaterThan(0.12f), $"{regions[a].Id}-{regions[b].Id}"); // 겹치지 않음
                }
            }
        }
    }
}
