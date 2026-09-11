using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 결속 기능
using ProjectH.Battle.Rhythm; // 리듬 배율 기능
using ProjectH.Data; // 캐릭터 데이터 기능
using ProjectH.SaveSystem; // 결속 저장 기능
using UnityEditor; // 에셋 로드 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BondBattleTests // Day59 결속 전투 효과·궁극기 컷인 테스트
    {
        private const string Serena = "CH_SERENA"; // 테스트 캐릭터

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 전투 정적 상태 정리
        {
            BattleRuntimeStates.ResetAll(); // 통합 초기화
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 다음 테스트 영향 방지
        {
            BattleRuntimeStates.ResetAll(); // 통합 초기화
        }

        [Test] // 결속 없으면 게이지 그대로 검증
        public void Gauge_WithoutBond_IsUnchanged() // 기존 동작 유지 테스트
        {
            for (int index = 0; index < 10; index++) BattleUltimateGaugeRuntimeState.AddGauge(Serena, 1); // 1씩 10회 충전

            Assert.That(BattleUltimateGaugeRuntimeState.GetGauge(Serena), Is.EqualTo(10)); // 10 검증
        }

        [Test] // 2단계 +10% (소수점 이월) 검증
        public void Gauge_StageTwo_AddsTenPercentWithRemainder() // 게이지 보너스 테스트
        {
            BattleBondRuntimeState.Register(Serena, 2); // 결속 2단계 등록

            for (int index = 0; index < 10; index++) BattleUltimateGaugeRuntimeState.AddGauge(Serena, 1); // 1씩 10회 충전

            Assert.That(BattleUltimateGaugeRuntimeState.GetGauge(Serena), Is.EqualTo(11)); // 10 × 1.1 = 11 검증
        }

        [Test] // 결속의 원탁 추가 배율 검증
        public void Gauge_RoundTable_StacksWithStageTwo() // 원탁 테스트
        {
            BattleBondRuntimeState.Register(Serena, 3); // 결속 3단계
            BattleBondRuntimeState.SetRoundTable(true); // 원탁 활성

            Assert.That(BattleBondRuntimeState.GetGaugeGainMultiplier(Serena), Is.EqualTo(1.2f).Within(0.0001f)); // 1 + 0.1 + 0.1 검증
            Assert.That(BattleUltimateGaugeRuntimeState.AddGauge(Serena, 10), Is.EqualTo(12)); // 10 × 1.2 = 12 검증
        }

        [Test] // 3단계 Perfect 보너스·상한 검증
        public void Rhythm_StageThreePerfectBonus_AddsAndCaps() // Perfect 보너스 테스트
        {
            RhythmChallengeResult allPerfect = new RhythmChallengeResult(4, 0, 0, 4); // 전부 Perfect 4개
            RhythmChallengeResult manyPerfect = new RhythmChallengeResult(8, 0, 0, 8); // Perfect 8개 (상한 확인)

            Assert.That(RhythmPowerScaler.Evaluate(allPerfect, 0f), Is.EqualTo(RhythmPowerScaler.MaxMultiplier).Within(0.0001f)); // 결속 없으면 1.5 그대로 검증
            Assert.That(RhythmPowerScaler.Evaluate(allPerfect, BondCatalog.PerfectBonusPerHit), Is.EqualTo(1.66f).Within(0.0001f)); // 1.5 + 4 × 0.04 검증
            Assert.That(RhythmPowerScaler.Evaluate(manyPerfect, BondCatalog.PerfectBonusPerHit), Is.EqualTo(1.70f).Within(0.0001f)); // 상한 1.7 검증
        }

        [Test] // 1단계 체력 +2%·단계 등록 검증
        public void StatsFactory_StageOne_AddsHpAndRegistersLevel() // 스탯 테스트
        {
            CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/ProjectH/Data/Characters/CH_SERENA.asset"); // 세레나 데이터
            CharacterSaveData plain = new CharacterSaveData(Serena); // 결속 0
            CharacterSaveData bonded = new CharacterSaveData(Serena); // 결속 1
            bonded.SetBondLevel(1); // 1단계 설정

            int plainHp = BattleStatsFactory.CreateCharacter(character, plain, "ALLY_0").MaxHp; // 기존 체력
            int bondedHp = BattleStatsFactory.CreateCharacter(character, bonded, "ALLY_0").MaxHp; // 결속 체력

            Assert.That(plainHp, Is.EqualTo(character.BaseHp)); // 결속 0이면 기존과 같음 검증
            Assert.That(bondedHp, Is.EqualTo(UnityEngine.Mathf.RoundToInt(character.BaseHp * 1.02f))); // +2% 검증
            Assert.That(BattleBondRuntimeState.GetLevel(Serena), Is.EqualTo(1)); // 전투 결속 단계 등록 검증
        }

        [Test] // 결속 스킬 전투당 1회 검증
        public void BondSkill_OncePerBattle_ResetsOnNewBattle() // 1회 제한 테스트
        {
            Assert.That(BattleBondRuntimeState.TryUseOnce("BOND_SERENA:ALLY_0"), Is.True); // 첫 사용 검증
            Assert.That(BattleBondRuntimeState.TryUseOnce("BOND_SERENA:ALLY_0"), Is.False); // 두 번째 차단 검증
            BattleRuntimeStates.BeginBattle(null); // 새 전투 시작
            Assert.That(BattleBondRuntimeState.TryUseOnce("BOND_SERENA:ALLY_0"), Is.True); // 새 전투 재사용 검증
        }

        [Test] // 전투 시작 시 등록 단계 유지 검증 (Day52 등록형 규칙)
        public void BeginBattle_KeepsRegisteredLevels() // 등록 유지 테스트
        {
            BattleBondRuntimeState.Register(Serena, 4); // 4단계 등록
            BattleBondRuntimeState.SetRoundTable(true); // 원탁 활성

            BattleRuntimeStates.BeginBattle(null); // 전투 시작 초기화

            Assert.That(BattleBondRuntimeState.GetLevel(Serena), Is.EqualTo(4)); // 단계 유지 검증
            Assert.That(BattleBondRuntimeState.RoundTableActive, Is.False); // 시너지는 초기화 검증
            Assert.That(BattleBondRuntimeState.HasPassiveBoost(Serena), Is.True); // 4단계 패시브 강화 검증
            Assert.That(BattleBondRuntimeState.HasBondSkill(Serena), Is.False); // 결속 스킬은 5단계 검증
        }

        [Test] // 컷인 타임라인 구간 검증
        public void CutInTimeline_PhasesAndSkip() // 타임라인 테스트
        {
            UltimateCutInTimeline timeline = new UltimateCutInTimeline(); // 타임라인 생성
            Assert.That(timeline.Phase, Is.EqualTo(UltimateCutInPhase.Enter)); // 등장 검증
            timeline.Advance(0.5f); // 0.5초
            Assert.That(timeline.Phase, Is.EqualTo(UltimateCutInPhase.Hold)); // 유지 검증
            Assert.That(timeline.Visibility, Is.EqualTo(1f)); // 완전 표시 검증
            timeline.Advance(0.8f); // 1.3초
            Assert.That(timeline.Phase, Is.EqualTo(UltimateCutInPhase.Exit)); // 퇴장 검증
            timeline.Advance(0.3f); // 1.6초
            Assert.That(timeline.IsDone, Is.True); // 종료 검증

            UltimateCutInTimeline skipped = new UltimateCutInTimeline(); // 새 타임라인
            skipped.Skip(); // 클릭 스킵
            Assert.That(skipped.IsDone, Is.True); // 즉시 종료 검증
        }

        [Test] // 컷인 정보·결속 5단계 금색 연출 검증
        public void CutInInfo_BondBurstOnlyAtStageFive() // 컷인 정보 테스트
        {
            UltimateCutInInfo normal = UltimateCutInCatalog.Build(Serena, "세레나", "성광의 심판", 4); // 4단계
            UltimateCutInInfo burst = UltimateCutInCatalog.Build(Serena, "세레나", "성광의 심판", 5); // 5단계

            Assert.That(normal.UltimateName, Is.EqualTo("성광의 심판")); // 궁극기 이름 검증
            Assert.That(normal.IsBondBurst, Is.False); // 4단계 일반 연출 검증
            Assert.That(normal.SubTitle, Does.Contain("세레나")); // 캐릭터 이름 부제 검증
            Assert.That(burst.IsBondBurst, Is.True); // 5단계 금색 연출 검증
            Assert.That(burst.SubTitle, Does.Contain("수호의 기도")); // 결속 스킬 이름 부제 검증
            Assert.That(normal.Line, Is.Not.Empty); // 기합 대사 검증
        }
    }
}
