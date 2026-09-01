using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 상단 HUD 배치 기능
using UnityEngine; // Unity 게임 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleTopHudLayoutTests // 상단 HUD 배치 테스트
    {
        private GameObject panelObject; // 테스트 TimePanel 객체
        private RectTransform rect; // 테스트 TimePanel RectTransform

        [SetUp] // 테스트 준비 표시
        public void SetUp() // TimePanel 테스트 구조 준비
        {
            panelObject = new GameObject("TimePanel", typeof(RectTransform)); // 테스트 TimePanel 생성
            rect = panelObject.GetComponent<RectTransform>(); // 테스트 RectTransform 조회
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // TimePanel 테스트 구조 정리
        {
            Object.DestroyImmediate(panelObject); // 테스트 TimePanel 제거
        }

        [Test] // 테스트 표시
        public void ApplyTimePanel_CentersPanelAtTopHudHeight() // TimePanel 중앙 및 동일 높이 배치 검증
        {
            BattleTopHudLayout.ApplyTimePanel(rect); // TimePanel 상단 중앙 배치 적용

            Assert.That(rect.anchorMin.x, Is.EqualTo(0.455f).Within(0.0001f)); // TimePanel 왼쪽 앵커 검증
            Assert.That(rect.anchorMax.x, Is.EqualTo(0.545f).Within(0.0001f)); // TimePanel 오른쪽 앵커 검증
            Assert.That(rect.anchorMin.y, Is.EqualTo(0.92f).Within(0.0001f)); // TimePanel 하단 높이 검증
            Assert.That(rect.anchorMax.y, Is.EqualTo(0.985f).Within(0.0001f)); // TimePanel 상단 높이 검증
            Assert.That((rect.anchorMin.x + rect.anchorMax.x) * 0.5f, Is.EqualTo(0.5f).Within(0.0001f)); // TimePanel 화면 중앙 검증
            Assert.That(rect.offsetMin, Is.EqualTo(Vector2.zero)); // TimePanel 최소 오프셋 초기화 검증
            Assert.That(rect.offsetMax, Is.EqualTo(Vector2.zero)); // TimePanel 최대 오프셋 초기화 검증
        }
    }
}
