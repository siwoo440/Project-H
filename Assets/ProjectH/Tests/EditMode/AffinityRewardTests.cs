using System.Collections.Generic; // 목록 자료형
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 스탯 팩토리·던전 클리어 기능
using ProjectH.SaveSystem; // 저장·호감도 보상 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class AffinityRewardTests // Day56 호감도 단계 보상 회귀 테스트
    {
        private const string CharacterId = "CH_SERENA"; // 테스트 캐릭터 ID

        private static SaveData CreateSave(int affinity) // 지정 호감도 새 게임 데이터 생성
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { CharacterId }); // 새 게임 데이터 생성
            AffinityService.AddAffinity(saveData, CharacterId, affinity); // 호감도 설정
            return saveData; // 데이터 반환
        }

        [Test] // 미도달 단계 잠김 검증
        public void GetState_BelowThreshold_IsLocked() // 잠김 상태 테스트
        {
            SaveData saveData = CreateSave(19); // 안면 직전 호감도

            Assert.That(AffinityRewardService.GetState(saveData, CharacterId, AffinityTier.Acquaintance), Is.EqualTo(AffinityRewardState.Locked)); // 안면 잠김 검증
            Assert.That(AffinityRewardService.TryClaim(saveData, null, CharacterId, AffinityTier.Acquaintance, out _), Is.False); // 잠김 수령 불가 검증
        }

        [Test] // 도달 단계 받기 가능 및 수령 검증
        public void TryClaim_ReachedTier_GrantsGoldAndMarksClaimed() // 수령 테스트
        {
            SaveData saveData = CreateSave(20); // 안면 도달 호감도
            int goldBefore = GoldCurrencyService.GetGold(saveData); // 수령 전 골드 조회

            Assert.That(AffinityRewardService.GetState(saveData, CharacterId, AffinityTier.Acquaintance), Is.EqualTo(AffinityRewardState.Claimable)); // 받기 가능 검증
            Assert.That(AffinityRewardService.TryClaim(saveData, null, CharacterId, AffinityTier.Acquaintance, out _), Is.True); // 수령 성공 검증
            Assert.That(GoldCurrencyService.GetGold(saveData), Is.EqualTo(goldBefore + 100)); // 골드 100 지급 검증
            Assert.That(AffinityRewardService.GetState(saveData, CharacterId, AffinityTier.Acquaintance), Is.EqualTo(AffinityRewardState.Claimed)); // 받음 상태 검증
        }

        [Test] // 중복 수령 차단 검증
        public void TryClaim_Twice_OnlyGrantsOnce() // 중복 수령 테스트
        {
            SaveData saveData = CreateSave(45); // 호감 도달 호감도
            AffinityRewardService.TryClaim(saveData, null, CharacterId, AffinityTier.Friendly, out _); // 첫 수령
            int goldAfterFirst = GoldCurrencyService.GetGold(saveData); // 첫 수령 후 골드

            Assert.That(AffinityRewardService.TryClaim(saveData, null, CharacterId, AffinityTier.Friendly, out string message), Is.False); // 두 번째 수령 실패 검증
            Assert.That(GoldCurrencyService.GetGold(saveData), Is.EqualTo(goldAfterFirst)); // 골드 추가 지급 없음 검증
            Assert.That(message, Does.Contain("이미")); // 중복 안내 문구 검증
        }

        [Test] // 여러 단계를 한 번에 넘으면 각각 받기 가능 검증
        public void JumpingTiers_MakesEveryReachedTierClaimable() // 다단계 도달 테스트
        {
            SaveData saveData = CreateSave(65); // 신뢰 도달 호감도 (안면·호감·신뢰)

            Assert.That(AffinityRewardService.GetState(saveData, CharacterId, AffinityTier.Acquaintance), Is.EqualTo(AffinityRewardState.Claimable)); // 안면 받기 가능 검증
            Assert.That(AffinityRewardService.GetState(saveData, CharacterId, AffinityTier.Friendly), Is.EqualTo(AffinityRewardState.Claimable)); // 호감 받기 가능 검증
            Assert.That(AffinityRewardService.GetState(saveData, CharacterId, AffinityTier.Trusted), Is.EqualTo(AffinityRewardState.Claimable)); // 신뢰 받기 가능 검증
            Assert.That(AffinityRewardService.GetState(saveData, CharacterId, AffinityTier.Bonded), Is.EqualTo(AffinityRewardState.Locked)); // 유대 잠김 검증
        }

        [Test] // 호감도 감소 후에도 받은 보상 유지 검증
        public void ClaimedReward_StaysClaimedAfterAffinityDrops() // 보상 회수 없음 테스트
        {
            SaveData saveData = CreateSave(25); // 안면 도달 호감도
            AffinityRewardService.TryClaim(saveData, null, CharacterId, AffinityTier.Acquaintance, out _); // 안면 수령
            AffinityService.AddAffinity(saveData, CharacterId, -25); // 호감도 0으로 감소

            Assert.That(AffinityRewardService.GetState(saveData, CharacterId, AffinityTier.Acquaintance), Is.EqualTo(AffinityRewardState.Claimed)); // 받음 상태 유지 검증
            Assert.That(AffinityRewardCatalog.GetClaimedBonus(saveData.FindCharacter(CharacterId)).HpPercent, Is.EqualTo(0.02f).Within(0.0001f)); // 보너스 유지 검증
        }

        [Test] // 받은 단계만 전투 보너스 누적 검증
        public void ClaimedBonus_OnlyCountsClaimedTiers() // 누적 보너스 테스트
        {
            SaveData saveData = CreateSave(100); // 전 단계 도달 호감도
            CharacterSaveData character = saveData.FindCharacter(CharacterId); // 캐릭터 조회

            Assert.That(AffinityRewardCatalog.GetClaimedBonus(character).IsEmpty, Is.True); // 도달만 하고 안 받으면 보너스 없음 검증

            foreach (AffinityTierReward reward in AffinityRewardCatalog.All) // 전 단계 수령
            {
                AffinityRewardService.TryClaim(saveData, null, CharacterId, reward.Tier, out _); // 단계 수령
            }

            AffinityBattleBonus bonus = AffinityRewardCatalog.GetClaimedBonus(character); // 누적 보너스 조회
            Assert.That(bonus.HpPercent, Is.EqualTo(0.04f).Within(0.0001f)); // 체력 +4% 검증
            Assert.That(bonus.AttackPercent, Is.EqualTo(0.04f).Within(0.0001f)); // 공격력 +4% 검증
            Assert.That(bonus.StartUltimateGauge, Is.EqualTo(25)); // 유대 시작 게이지 25 검증
        }

        [Test] // 보너스 없으면 전투 스탯 불변 검증 (기존 전투 결과 보장)
        public void StatsFactory_WithoutBonus_MatchesLegacyStats() // 기존 스탯 불변 테스트
        {
            ProjectH.Data.CharacterData data = UnityEngine.ScriptableObject.CreateInstance<ProjectH.Data.CharacterData>(); // 빈 캐릭터 원본 생성
            BattleStats legacy = BattleStatsFactory.CreateCharacter(data, 5, "ALLY_0", BattleEquipmentStatBonus.Empty); // 기존 경로 스탯
            BattleStats withEmpty = BattleStatsFactory.CreateCharacter(data, 5, "ALLY_0", BattleEquipmentStatBonus.Empty, AffinityBattleBonus.Empty); // 빈 호감도 보너스 경로 스탯
            BattleStats boosted = BattleStatsFactory.CreateCharacter(data, 5, "ALLY_0", BattleEquipmentStatBonus.Empty, new AffinityBattleBonus(0.5f, 0.5f, 0)); // 50% 보너스 경로 스탯

            Assert.That(withEmpty.MaxHp, Is.EqualTo(legacy.MaxHp)); // 체력 불변 검증
            Assert.That(withEmpty.Attack, Is.EqualTo(legacy.Attack)); // 공격력 불변 검증
            Assert.That(boosted.MaxHp, Is.GreaterThanOrEqualTo(legacy.MaxHp)); // 보너스 적용 시 체력 증가(또는 동일) 검증
            UnityEngine.Object.DestroyImmediate(data); // 임시 원본 정리
        }
    }

    public sealed class GameEventConditionTests // Day56 이벤트 해금 조건 회귀 테스트
    {
        private const string CharacterId = "CH_EVE"; // 테스트 캐릭터 ID

        [Test] // 빈 조건 항상 통과 검증
        public void Evaluate_NoConditions_AlwaysPasses() // 빈 조건 테스트
        {
            Assert.That(GameEventConditionEvaluator.Evaluate(SaveData.CreateNewGame(new[] { CharacterId }), null), Is.True); // null 조건 통과 검증
            Assert.That(GameEventConditionEvaluator.Evaluate(SaveData.CreateNewGame(new[] { CharacterId }), new List<GameEventCondition>()), Is.True); // 빈 목록 통과 검증
        }

        [Test] // 호감도 단계 조건 검증
        public void AffinityCondition_UsesCurrentTier() // 호감도 조건 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { CharacterId }); // 새 게임 데이터 생성
            GameEventCondition condition = GameEventCondition.AffinityAtLeast(CharacterId, AffinityTier.Friendly); // 호감 이상 조건

            Assert.That(GameEventConditionEvaluator.IsMet(saveData, condition), Is.False); // 호감도 0 미충족 검증
            AffinityService.AddAffinity(saveData, CharacterId, 40); // 호감 도달
            Assert.That(GameEventConditionEvaluator.IsMet(saveData, condition), Is.True); // 호감 충족 검증
        }

        [Test] // 일차 조건 검증
        public void DayCondition_UsesCurrentDay() // 일차 조건 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { CharacterId }); // 새 게임 데이터 생성 (1일차)
            GameEventCondition condition = GameEventCondition.DayAtLeast(3); // 3일차 이후 조건

            Assert.That(GameEventConditionEvaluator.IsMet(saveData, condition), Is.False); // 1일차 미충족 검증
            saveData.SetCurrentDay(3); // 3일차로 변경
            Assert.That(GameEventConditionEvaluator.IsMet(saveData, condition), Is.True); // 3일차 충족 검증
        }

        [Test] // 시간대 조건 검증
        public void TimeCondition_UsesCurrentPhase() // 시간대 조건 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { CharacterId }); // 새 게임 데이터 생성 (아침)
            GameEventCondition evening = GameEventCondition.AtTime(SaveTimeOfDay.Evening); // 저녁 조건

            Assert.That(GameEventConditionEvaluator.IsMet(saveData, evening), Is.False); // 아침 미충족 검증
            GameTimeService.AdvanceTime(saveData); // 낮으로 진행
            GameTimeService.AdvanceTime(saveData); // 저녁으로 진행
            Assert.That(GameEventConditionEvaluator.IsMet(saveData, evening), Is.True); // 저녁 충족 검증
        }

        [Test] // 던전 클리어·스토리 플래그 조건 검증
        public void ClearAndFlagConditions_UseSaveProgress() // 진행 조건 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { CharacterId }); // 새 게임 데이터 생성
            GameEventCondition cleared = GameEventCondition.Cleared("DG002", "성역 외곽 폐허"); // 던전 클리어 조건
            GameEventCondition flag = GameEventCondition.Flag("EVT_TEST_SEEN", "테스트 이벤트 감상"); // 스토리 플래그 조건

            Assert.That(GameEventConditionEvaluator.IsMet(saveData, cleared), Is.False); // 미클리어 검증
            Assert.That(GameEventConditionEvaluator.IsMet(saveData, flag), Is.False); // 플래그 없음 검증
            DungeonProgressSaveAdapter.MarkCleared(saveData, "DG002"); // 클리어 기록
            saveData.SetStoryFlag("EVT_TEST_SEEN"); // 플래그 설정
            Assert.That(GameEventConditionEvaluator.IsMet(saveData, cleared), Is.True); // 클리어 충족 검증
            Assert.That(GameEventConditionEvaluator.IsMet(saveData, flag), Is.True); // 플래그 충족 검증
        }

        [Test] // 여러 조건 AND 결합과 미충족 사유 수집 검증
        public void Evaluate_CollectsAllUnmetReasons() // AND 결합·사유 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { CharacterId }); // 새 게임 데이터 생성
            List<GameEventCondition> conditions = new List<GameEventCondition> // 호감·일차·시간 세 조건
            {
                GameEventCondition.AffinityAtLeast(CharacterId, AffinityTier.Trusted), // 신뢰 이상
                GameEventCondition.DayAtLeast(3), // 3일차 이후
                GameEventCondition.AtTime(SaveTimeOfDay.Morning) // 아침 (새 게임 기본 충족)
            };
            List<string> reasons = new List<string>(); // 사유 버퍼

            Assert.That(GameEventConditionEvaluator.Evaluate(saveData, conditions, reasons), Is.False); // 전체 미충족 검증
            Assert.That(reasons, Is.EquivalentTo(new[] { "신뢰 이상", "3일차 이후" })); // 미충족 두 사유만 수집 검증
        }

        [Test] // 초기 4인 개인 이벤트 목록 구성 검증
        public void CharacterEventCatalog_HasTwoEpisodesPerStarter() // 개인 이벤트 목록 테스트
        {
            foreach (string characterId in new[] { "CH_SERENA", "CH_ELLEN", "CH_LILIA", "CH_EVE" }) // 초기 4인 순회
            {
                List<CharacterEventDefinition> events = CharacterEventCatalog.GetForCharacter(characterId); // 캐릭터 이벤트 조회
                Assert.That(events.Count, Is.EqualTo(2), characterId); // 2화 구성 검증
                Assert.That(events[0].Episode, Is.EqualTo(1), characterId); // 화수 정렬 검증
                Assert.That(events[0].Conditions.Count, Is.GreaterThan(0), characterId); // 해금 조건 존재 검증
            }
        }
    }
}
