using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Dungeon; // 모험 지역 표 기능
using ProjectH.UI; // 지역 아이콘 기능
using UnityEngine; // 스프라이트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class AdventureRegionArtTests // Day78 모험 지도 지역 아이콘 테스트
    {
        [TestCase("REGION_FOREST")] // 숲
        [TestCase("REGION_SWAMP")] // 늪지대
        [TestCase("REGION_DEMON_CASTLE")] // 마왕성
        [TestCase("REGION_VILLAGE")] // 마을
        [TestCase("REGION_NOIR")] // 노아르
        [TestCase("REGION_SILVARAN")] // 실바란
        [TestCase("REGION_KARNIAN")] // 카르니안
        [TestCase("REGION_DESERT")] // 사막
        public void Region_HasSquareIcon(string regionId) // 지역 아이콘 테스트
        {
            Assert.That(AdventureRegionCatalog.Get(regionId), Is.Not.Null, $"없는 지역 : {regionId}"); // 실제 지역
            Sprite icon = AdventureRegionArt.Get(regionId); // 아이콘
            Assert.That(icon, Is.Not.Null, $"지역 아이콘 없음 : {regionId}"); // 존재
            Assert.That(icon.rect.width, Is.EqualTo(icon.rect.height), $"아이콘이 정사각이 아님 : {regionId}"); // 정사각 (지역마다 크기가 같아 보이도록)
        }

        [Test] // 아이콘이 없는 지역은 null을 돌려준다 (지도는 예전의 색 원과 글자로 그린다)
        public void UnknownRegion_ReturnsNull() // 없는 아이콘 테스트
        {
            Assert.That(AdventureRegionArt.Get("REGION_NOT_EXIST"), Is.Null); // 없는 지역
            Assert.That(AdventureRegionArt.Get(null), Is.Null); // null 안전
            Assert.That(AdventureRegionArt.Get(string.Empty), Is.Null); // 빈 ID
        }

        [Test] // 아이콘은 받침보다 크고, "!" 안내는 아이콘 머리 위에 온다
        public void Layout_KeepsAlertAboveIcon() // 배치 상수 테스트
        {
            Assert.That(AdventureRegionArt.IconSize, Is.GreaterThan(AdventureRegionArt.PedestalSize.y)); // 아이콘이 받침보다 큼
            Assert.That(AdventureRegionArt.AlertLift, Is.GreaterThanOrEqualTo(AdventureRegionArt.IconLift + (AdventureRegionArt.IconSize * 0.5f))); // "!"가 아이콘 위
            Assert.That(AdventureRegionArt.LabelDrop, Is.LessThan(-AdventureRegionArt.PedestalSize.y * 0.5f)); // 이름이 받침 아래
        }
    }
}
