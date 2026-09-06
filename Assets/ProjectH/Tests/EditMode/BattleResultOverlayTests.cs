using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 결과 기능
using ProjectH.Data; // 전투 포지션 기능
using UnityEngine; // Unity 객체 기능

namespace ProjectH.Tests.EditMode // 프로젝트 EditMode 테스트 영역
{
    public sealed class BattleResultOverlayTests // 전투 결과 Overlay 테스트
    {
        [TearDown] // 테스트 종료 정리 지정
        public void TearDown() // 생성 결과 Overlay 정리
        {
            BattleResultOverlay active = BattleResultOverlay.ActiveOverlay; // 현재 활성 Overlay 조회

            if (active != null) // 활성 Overlay 존재 확인
            {
                Object.DestroyImmediate(active.gameObject); // 활성 Overlay 즉시 제거
            }
        }

        [Test] // 결과 화면 구성 테스트 지정
        public void ShowRuntime_BuildsPartyCardsFromResultData() // 결과 데이터 기반 파티 카드 구성 확인
        {
            BattleStats[] members = new BattleStats[2]; // 2인 테스트 파티 생성
            members[0] = CreateStats("ALLY_0", "CH_A", "Alpha", 5); // 첫 번째 테스트 파티원 생성
            members[1] = CreateStats("ALLY_1", "CH_B", "Beta", 6); // 두 번째 테스트 파티원 생성
            members[1].TakeDamage(1000); // 두 번째 파티원 전투 불능 처리
            BattleResultData result = BattleResultData.Create(BattleOutcome.Victory, members); // 테스트 결과 데이터 생성

            BattleResultOverlay overlay = BattleResultOverlay.ShowRuntime(result, null); // 결과 Overlay 생성

            Assert.AreSame(overlay, BattleResultOverlay.ActiveOverlay); // 활성 Overlay 참조 확인
            Assert.AreSame(result, overlay.Result); // 표시 결과 데이터 참조 확인
            Assert.AreEqual(2, overlay.RenderedMemberCount); // 생성 파티 카드 수 확인
            Assert.AreEqual("WIN!", overlay.TitleText); // 승리 제목 표시 확인
        }

        [Test] // 중복 Overlay 방지 테스트 지정
        public void ShowRuntime_ReplacesPreviousActiveOverlay() // 기존 결과 Overlay 교체 확인
        {
            BattleResultData firstResult = BattleResultData.Create(BattleOutcome.Victory, new[] { CreateStats("ALLY_0", "CH_A", "A", 1) }); // 첫 번째 결과 데이터 생성
            BattleResultData secondResult = BattleResultData.Create(BattleOutcome.Defeat, new[] { CreateStats("ALLY_0", "CH_A", "A", 1) }); // 두 번째 결과 데이터 생성
            BattleResultOverlay firstOverlay = BattleResultOverlay.ShowRuntime(firstResult, null); // 첫 번째 결과 Overlay 생성

            BattleResultOverlay secondOverlay = BattleResultOverlay.ShowRuntime(secondResult, null); // 두 번째 결과 Overlay 생성

            Assert.AreNotSame(firstOverlay, secondOverlay); // 결과 Overlay 인스턴스 교체 확인
            Assert.IsTrue(firstOverlay == null); // 이전 Overlay 제거 확인
            Assert.AreSame(secondOverlay, BattleResultOverlay.ActiveOverlay); // 신규 Overlay 활성 참조 확인
            Assert.AreEqual("LOSE", secondOverlay.TitleText); // 패배 제목 표시 확인
        }

        private static BattleStats CreateStats(string runtimeId, string characterId, string displayName, int level) // 테스트 전투 스탯 생성
        {
            return new BattleStats(runtimeId, characterId, displayName, BattlePosition.Dealer, level, 100, 20, 10, 1f, 1f, 0.1f); // 기본 테스트 전투 스탯 반환
        }
    }
}
