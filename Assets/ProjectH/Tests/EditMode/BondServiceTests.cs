using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.SaveSystem; // 결속·저장 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BondServiceTests // Day59 결속 단계·자원 회귀 테스트
    {
        private const string Serena = "CH_SERENA"; // 테스트 캐릭터

        private static SaveData CreateSave(int affinity) // 지정 호감도 새 게임
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { Serena }); // 새 게임
            AffinityService.AddAffinity(saveData, Serena, affinity); // 호감도 설정
            return saveData; // 반환
        }

        [Test] // 새 게임 초기값 검증
        public void NewSave_StartsAtLevelZeroWithOneResource() // 초기값 테스트
        {
            SaveData saveData = CreateSave(0); // 새 게임

            Assert.That(BondService.GetLevel(saveData, Serena), Is.EqualTo(0)); // 결속 0 검증
            Assert.That(BondService.GetResource(saveData), Is.EqualTo(BondCatalog.StartingResource)); // 자원 1 검증
        }

        [Test] // 하루 회복·최대치 검증
        public void Resource_RefillsOnePerDay_CapsAtFive() // 회복 테스트
        {
            SaveData saveData = CreateSave(0); // 새 게임
            BondService.GetResource(saveData); // 첫 지급 (1일차, 1개)

            saveData.SetCurrentDay(3); // 이틀 경과
            Assert.That(BondService.GetResource(saveData), Is.EqualTo(3)); // 1 + 2 검증
            saveData.SetCurrentDay(20); // 오래 경과
            Assert.That(BondService.GetResource(saveData), Is.EqualTo(BondCatalog.MaxResource)); // 최대 5 검증
        }

        [Test] // 1단계 조건·자원 소모 검증
        public void TryRaise_StageOne_NeedsAcquaintanceAndConsumesResource() // 1단계 테스트
        {
            SaveData locked = CreateSave(19); // 안면 직전
            Assert.That(BondService.TryRaise(locked, Serena, out string lockedMessage), Is.False); // 조건 부족 실패 검증
            Assert.That(lockedMessage, Does.Contain("안면")); // 부족 조건 안내 검증
            Assert.That(BondService.GetResource(locked), Is.EqualTo(1)); // 자원 유지 검증

            SaveData saveData = CreateSave(20); // 안면 도달
            Assert.That(BondService.TryRaise(saveData, Serena, out string message), Is.True, message); // 성공 검증
            Assert.That(BondService.GetLevel(saveData, Serena), Is.EqualTo(1)); // 1단계 검증
            Assert.That(BondService.GetResource(saveData), Is.EqualTo(0)); // 자원 1 소모 검증
        }

        [Test] // 자원 부족 거부 검증
        public void TryRaise_WithoutResource_FailsWithoutChange() // 자원 부족 테스트
        {
            SaveData saveData = CreateSave(100); // 모든 조건 충족 호감도
            BondService.TryRaise(saveData, Serena, out _); // 1단계 (자원 소진)

            Assert.That(BondService.TryRaise(saveData, Serena, out string message), Is.False); // 2단계 실패 검증
            Assert.That(BondService.GetLevel(saveData, Serena), Is.EqualTo(1)); // 단계 유지 검증
            Assert.That(message, Does.Contain("부족")); // 부족 안내 검증 (조건 또는 자원)
        }

        [Test] // 2단계는 1화 완료 필요 검증 (Day58 연계)
        public void StageTwo_RequiresEpisodeOneCompletion() // 1화 조건 테스트
        {
            SaveData saveData = CreateSave(45); // 호감 도달
            BondService.AddResource(saveData, 4); // 자원 충분
            BondService.TryRaise(saveData, Serena, out _); // 1단계

            Assert.That(BondService.GetStageState(saveData, Serena, 2), Is.EqualTo(BondStageState.Locked)); // 1화 전 잠김 검증
            saveData.SetStoryFlag("SYS_EVENT_DONE:EVT_SERENA_01"); // 1화 완료 기록
            Assert.That(BondService.GetStageState(saveData, Serena, 2), Is.EqualTo(BondStageState.Available)); // 해금 검증
            Assert.That(BondService.TryRaise(saveData, Serena, out string message), Is.True, message); // 2단계 성공 검증
        }

        [Test] // 순서 강제·5단계 호감도 100 검증
        public void Stages_AreSequential_AndStageFiveNeedsFullAffinity() // 순서·최종 단계 테스트
        {
            SaveData saveData = CreateSave(99); // 호감도 99
            saveData.SetStoryFlag("SYS_EVENT_DONE:EVT_SERENA_01"); // 1화 완료
            BondService.AddResource(saveData, 10); // 자원 가득

            Assert.That(BondService.GetStageState(saveData, Serena, 3), Is.EqualTo(BondStageState.Locked)); // 건너뛰기 불가 검증

            for (int stage = 1; stage <= 4; stage++) // 1~4단계
            {
                Assert.That(BondService.TryRaise(saveData, Serena, out string message), Is.True, message); // 순서대로 성공
            }

            Assert.That(BondService.TryRaise(saveData, Serena, out _), Is.False); // 호감도 99로 5단계 실패 검증
            AffinityService.AddAffinity(saveData, Serena, 1); // 호감도 100
            BondService.AddResource(saveData, 1); // 자원 보충
            Assert.That(BondService.TryRaise(saveData, Serena, out _), Is.True); // 5단계 성공 검증
            Assert.That(BondService.TryRaise(saveData, Serena, out string maxMessage), Is.False); // 최대 이후 실패 검증
            Assert.That(maxMessage, Does.Contain("최고")); // 최대 안내 검증
        }

        [Test] // 이전 저장 호환 검증
        public void OldSave_WithoutBondFields_StartsAtZeroAndGetsFirstResource() // 저장 호환 테스트
        {
            SaveData saveData = UnityEngine.JsonUtility.FromJson<SaveData>("{\"currentDay\":7,\"characters\":[{\"characterId\":\"CH_SERENA\",\"affinity\":50}]}"); // Day59 이전 저장
            saveData.EnsureDefaults(); // 기본값 보정

            Assert.That(BondService.GetLevel(saveData, Serena), Is.EqualTo(0)); // 결속 0 검증
            Assert.That(BondService.GetResource(saveData), Is.EqualTo(1)); // 첫 자원 1 (과거 날짜만큼 몰아주지 않음) 검증
        }

        [Test] // 조합 시너지 판정 검증
        public void Synergy_BalancedPartyAndRoundTable() // 시너지 테스트
        {
            string[] starters = { "CH_SERENA", "CH_ELLEN", "CH_LILIA", "CH_EVE" }; // 초기 4인

            BondSynergyResult low = BondCatalog.EvaluateSynergy(starters, new[] { 3, 3, 3, 2 }); // 결속 3 이상 3명
            BondSynergyResult high = BondCatalog.EvaluateSynergy(starters, new[] { 3, 4, 5, 3 }); // 결속 3 이상 4명
            BondSynergyResult mixed = BondCatalog.EvaluateSynergy(new[] { "CH_SERENA", "CH_ELLEN", "CH_LILIA", "CH_CLAIRE" }, new[] { 5, 5, 5, 5 }); // 이브 대신 클레어

            Assert.That(low.BalancedParty, Is.True); // 균형 파티 성립 검증
            Assert.That(low.RoundTable, Is.False); // 원탁 미성립 검증
            Assert.That(high.RoundTable, Is.True); // 원탁 성립 검증
            Assert.That(mixed.BalancedParty, Is.False); // 균형 파티 미성립 검증
            Assert.That(mixed.RoundTable, Is.True); // 원탁은 구성과 무관 검증
        }
    }
}
