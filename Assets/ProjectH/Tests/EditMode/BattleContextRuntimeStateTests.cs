using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 컨텍스트 기능
using ProjectH.UI; // 던전 선택 상태 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleContextRuntimeStateTests // 전투 컨텍스트 고정 테스트
    {
        [SetUp] // 테스트 시작 초기화
        public void SetUp() // 테스트 상태 초기화
        {
            DungeonSelectionRuntimeState.ResetAll(); // 던전 선택 상태 초기화
            BattleContextRuntimeState.ResetAll(); // 전투 컨텍스트 초기화
        }

        [TearDown] // 테스트 종료 초기화
        public void TearDown() // 테스트 상태 정리
        {
            DungeonSelectionRuntimeState.ResetAll(); // 던전 선택 상태 정리
            BattleContextRuntimeState.ResetAll(); // 전투 컨텍스트 정리
        }

        [Test] // 전투 컨텍스트 고정 검증
        public void Capture_KeepsDungeonSnapshotAfterSelectionChanges() // 선택 변경 이후 전투 던전 유지 검증
        {
            Assert.That(DungeonSelectionRuntimeState.TrySelect("DG002", _ => true), Is.True); // DG002 선택 성공 검증
            Assert.That(BattleContextRuntimeState.Capture(DungeonSelectionRuntimeState.SelectedDungeonId), Is.EqualTo("DG002")); // DG002 전투 확정 검증
            Assert.That(DungeonSelectionRuntimeState.TrySelect("DG004", _ => true), Is.True); // DG004 후속 선택 성공 검증
            Assert.That(BattleContextRuntimeState.CurrentDungeonId, Is.EqualTo("DG002")); // 확정 전투 던전 유지 검증
        }

        [Test] // 직접 전투 기본값 검증
        public void Capture_UsesDg001ForMissingSelection() // 미선택 직접 전투 기본 던전 검증
        {
            Assert.That(BattleContextRuntimeState.Capture(string.Empty), Is.EqualTo("DG001")); // DG001 기본 전투 확정 검증
            Assert.That(BattleContextRuntimeState.UsedDirectFallback, Is.True); // 직접 전투 기본값 표시 검증
        }
    }
}
