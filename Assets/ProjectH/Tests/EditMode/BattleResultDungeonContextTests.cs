using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 결과 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleResultDungeonContextTests // 전투 결과 던전 컨텍스트 테스트
    {
        [SetUp] // 테스트 시작 초기화
        public void SetUp() // 전투 컨텍스트 초기화
        {
            BattleContextRuntimeState.ResetAll(); // 전투 컨텍스트 상태 초기화
        }

        [TearDown] // 테스트 종료 초기화
        public void TearDown() // 전투 컨텍스트 정리
        {
            BattleContextRuntimeState.ResetAll(); // 전투 컨텍스트 상태 정리
        }

        [Test] // 결과 던전 ID 저장 검증
        public void Create_StoresExplicitDungeonId() // 지정 던전 결과 스냅샷 검증
        {
            BattleResultData result = BattleResultData.Create(BattleOutcome.Victory, null, "DAY28_RESULT", "DG003"); // DG003 전투 결과 생성
            Assert.That(result.ResultId, Is.EqualTo("DAY28_RESULT")); // 결과 ID 저장 검증
            Assert.That(result.DungeonId, Is.EqualTo("DG003")); // 결과 던전 ID 저장 검증
            Assert.That(result.Outcome, Is.EqualTo(BattleOutcome.Victory)); // 승리 결과 저장 검증
        }

        [Test] // 미지원 던전 보정 검증
        public void Create_FallsBackToDg001ForUnsupportedDungeon() // 미지원 던전 결과 기본값 검증
        {
            BattleResultData result = BattleResultData.Create(BattleOutcome.Defeat, null, "DAY28_FALLBACK", "DG999"); // 미지원 던전 결과 생성
            Assert.That(result.DungeonId, Is.EqualTo("DG001")); // DG001 기본 결과 던전 검증
            Assert.That(result.Gold, Is.Zero); // 패배 골드 없음 검증
            Assert.That(result.Experience, Is.Zero); // 패배 경험치 없음 검증
        }
    }
}
