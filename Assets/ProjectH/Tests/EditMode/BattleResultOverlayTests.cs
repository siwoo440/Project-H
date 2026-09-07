using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 결과 기능
using ProjectH.Data; // 전투 포지션 기능
using ProjectH.SaveSystem; // 저장 데이터 기능
using UnityEngine; // Unity 객체 기능
using UnityEngine.UI; // Unity Text 확인 기능

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

        [Test] // 성장 결과 화면 표시 테스트 지정
        public void ShowRuntime_CommittedGrowth_ShowsLevelUpAndExperience() // 실제 반영된 레벨업 및 경험치 문구 표시 확인
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_A" }); // 신규 테스트 저장 생성
            CharacterSaveData character = saveData.FindCharacter("CH_A"); // 테스트 저장 캐릭터 조회
            character.SetExperience(CharacterLevelProgression.GetRequiredExperience(1) - 1); // 다음 승리에서 레벨업 가능한 경험치 설정
            BattleStats[] members = { CreateStats("ALLY_0", "CH_A", "Alpha", 1) }; // 성장 UI 테스트 파티 생성
            BattleResultData result = BattleResultData.Create(BattleOutcome.Victory, members, "RESULT_OVERLAY_GROWTH", "DG001"); // 성장 UI 테스트 결과 생성
            CharacterLevelProgressionResult expected = CharacterLevelProgression.Apply(1, character.Experience, result.Experience); // 예상 성장 결과 계산
            bool committed = BattleResultCommitService.CommitOnce(saveData, result); // 실제 성장 결과 영구 반영

            BattleResultOverlay overlay = BattleResultOverlay.ShowRuntime(result, null); // 성장 반영 결과 Overlay 생성
            Transform memberCard = overlay.transform.Find("ResultPanel/MemberCard_1"); // 첫 번째 결과 파티원 카드 조회
            Text levelText = memberCard == null ? null : memberCard.Find("Level")?.GetComponent<Text>(); // 성장 레벨 텍스트 조회
            Text growthText = memberCard == null ? null : memberCard.Find("Growth")?.GetComponent<Text>(); // 성장 경험치 텍스트 조회
            string expectedLevelText = expected.LevelsGained > 0 ? $"Lv. 1 → Lv. {expected.Level}" : $"Lv. {expected.Level}"; // 예상 레벨 표시 문구 생성
            string expectedGrowthText = expected.LevelsGained > 1 ? $"LEVEL UP ×{expected.LevelsGained} · EXP +{result.Experience}" : $"LEVEL UP! · EXP +{result.Experience}"; // 예상 레벨업 표시 문구 생성

            Assert.IsTrue(committed); // 성장 결과 반영 성공 확인
            Assert.Greater(result.Experience, 0); // 승리 경험치 보상 존재 확인
            Assert.GreaterOrEqual(expected.LevelsGained, 1); // 테스트 조건상 레벨업 발생 확인
            Assert.NotNull(memberCard); // 첫 번째 결과 카드 존재 확인
            Assert.NotNull(levelText); // 레벨 결과 Text 존재 확인
            Assert.NotNull(growthText); // 성장 결과 Text 존재 확인
            Assert.AreEqual(expectedLevelText, levelText.text); // 레벨 상승 전후 문구 확인
            Assert.AreEqual(expectedGrowthText, growthText.text); // 획득 경험치 및 레벨업 문구 확인
        }

        private static BattleStats CreateStats(string runtimeId, string characterId, string displayName, int level) // 테스트 전투 스탯 생성
        {
            return new BattleStats(runtimeId, characterId, displayName, BattlePosition.Dealer, level, 100, 20, 10, 1f, 1f, 0.1f); // 기본 테스트 전투 스탯 반환
        }
    }
}
