using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Dungeon; // 모험 지역 표 기능
using ProjectH.UI; // 지역 아이콘 기능
using UnityEngine; // 스프라이트 기능
using UnityEngine.UI; // Image · 비율 맞춤 기능

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

        [Test] // 정식 세계 지도가 있고, 지역 좌표가 기준으로 삼는 비율(1400×1100)과 같다
        public void WorldMap_HasOfficialArtWithDesignAspect() // 세계 지도 테스트 (Day80)
        {
            Sprite map = RuntimeSpriteLoader.Load(AdventureMapArt.ResourcePath); // 정식 지도
            Assert.That(map, Is.Not.Null, "세계 지도 없음 : Resources/Map/WORLD.png"); // 존재
            Assert.That(map.rect.width / map.rect.height, Is.EqualTo(AdventureMapArt.Aspect).Within(0.01f), "지도 비율이 달라지면 지역 표시가 다른 땅 위에 놓인다"); // 비율
            Assert.That(AdventureMapArt.Get(), Is.SameAs(map)); // 임시 지도 대신 정식 지도
        }

        [Test] // 지도는 늘이지 않고 영역 안에 맞춘다 (여러 번 불러도 맞춤 컴포넌트는 하나)
        public void WorldMap_FitKeepsAspectInsideArea() // 지도 비율 맞춤 테스트 (Day80)
        {
            GameObject mapObject = new GameObject("WorldMap", typeof(RectTransform), typeof(Image)); // 지도 역할

            try // 정리 보장
            {
                Image map = mapObject.GetComponent<Image>(); // 지도 이미지
                AdventureMapArt.Fit(map); // 맞춤
                AdventureMapArt.Fit(map); // 다시 맞춤
                AspectRatioFitter[] fitters = mapObject.GetComponents<AspectRatioFitter>(); // 맞춤 컴포넌트
                Assert.That(fitters.Length, Is.EqualTo(1)); // 하나만
                Assert.That(fitters[0].aspectMode, Is.EqualTo(AspectRatioFitter.AspectMode.FitInParent)); // 영역 안에 다 보이게
                Assert.That(fitters[0].aspectRatio, Is.EqualTo(AdventureMapArt.Aspect).Within(0.0001f)); // 지도 비율
                Assert.That(() => AdventureMapArt.Fit(null), Throws.Nothing); // null 안전
            }
            finally // 임시 객체 정리
            {
                Object.DestroyImmediate(mapObject); // 지도 제거
            }
        }

        [Test] // 모든 지역은 지도 안쪽에 놓인다 (가장자리에 붙으면 아이콘과 이름이 지도 밖으로 나간다)
        public void EveryRegion_SitsInsideTheMap() // 지역 좌표 테스트 (Day80)
        {
            foreach (AdventureRegion region in AdventureRegionCatalog.All) // 지역 순회
            {
                Assert.That(region.Position.x, Is.InRange(0.08f, 0.92f), $"가로 좌표가 가장자리 : {region.Id}"); // 가로
                Assert.That(region.Position.y, Is.InRange(0.10f, 0.90f), $"세로 좌표가 가장자리 : {region.Id}"); // 세로
            }
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
