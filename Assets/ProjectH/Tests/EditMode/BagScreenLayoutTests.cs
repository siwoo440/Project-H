using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.UI; // 가방 화면 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BagScreenLayoutTests // 가방 화면 레이아웃 규칙 테스트
    {
        [Test] // 테스트 표시
        public void BagGrid_UsesSevenColumnsAndFourVisibleRowsMinimum() // 가방 7열 및 최소 28칸 검증
        {
            Assert.That(BagScreenController.SlotColumnCount, Is.EqualTo(7)); // 가방 가로 7칸 검증
            Assert.That(BagScreenController.MinimumSlotCount, Is.EqualTo(28)); // 가방 최소 4줄 검증
        }
    }
}
