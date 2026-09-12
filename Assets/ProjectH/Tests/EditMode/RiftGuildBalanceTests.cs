using System.Collections.Generic; // 목록 자료형
using System.Linq; // 목록 검색 기능
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 진행·밸런스 계산 기능
using ProjectH.Data; // 던전·몬스터·캐릭터 데이터 기능
using ProjectH.Diary; // 세계관 단어 기능
using ProjectH.Dungeon; // 균열·지역 기능
using ProjectH.SaveSystem; // 저장·침식도 기능
using ProjectH.UI; // 지원 던전 목록 기능
using ProjectH.Village; // 길드 의뢰 기능
using UnityEditor; // 에셋 로드 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class RiftGuildBalanceTests // Day67 검은 균열 · 길드 의뢰 · 1차 밸런스 테스트
    {
        private static readonly string[] Starters = { "CH_SERENA", "CH_ELLEN", "CH_LILIA", "CH_EVE" }; // 초기 4인

        private static SaveData CreateSave() => SaveData.CreateNewGame(Starters); // 새 게임

        private static DungeonData LoadDungeon(string id) => AssetDatabase.LoadAssetAtPath<DungeonData>($"Assets/ProjectH/Data/Dungeons/{id}.asset"); // 던전 에셋

        private static MonsterData LoadMonster(string id) => AssetDatabase.LoadAssetAtPath<MonsterData>($"Assets/ProjectH/Data/Monsters/{id}.asset"); // 몬스터 에셋

        private static CharacterData LoadCharacter(string id) => AssetDatabase.LoadAssetAtPath<CharacterData>($"Assets/ProjectH/Data/Characters/{id}.asset"); // 캐릭터 에셋

        private static SaveData CreateLateSave(int day) // 마왕성까지 클리어한 저장 (균열 해금 상태)
        {
            SaveData saveData = CreateSave(); // 새 게임
            foreach (string dungeonId in new[] { "DG001", "DG002", "DG003", "DG004" }) DungeonProgressSaveAdapter.MarkCleared(saveData, dungeonId); // 진행 기록
            saveData.SetCurrentDay(day); // 일차 설정
            return saveData; // 저장 반환
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() => RiftRunState.End(); // 균열 탐험 표시 초기화

        [Test] // 바다 지역 : 던전 2개 · 균열 침식 특징 · 릴리아 첫 방문 · 세계관 단어 해금
        public void SeaRegion_OpensWithRiftDungeons() // 바다 지역 테스트
        {
            AdventureRegion sea = AdventureRegionCatalog.Get("REGION_SEA"); // 바다
            Assert.That(sea.Kind, Is.EqualTo(AdventureRegionKind.Dungeon)); // 열림
            Assert.That(sea.DungeonIds, Is.EqualTo(new[] { "DG015", "DG016" })); // 던전 2개
            Assert.That(sea.ErosionRegionId, Is.EqualTo("REG_RIFT")); // 침식도 지역 연결
            Assert.That(BattleRegionTraitCatalog.GetKind("DG015"), Is.EqualTo(BattleRegionTraitKind.RiftErosion)); // 균열 침식
            Assert.That(BattleRegionTraitCatalog.GetKind("DG016"), Is.EqualTo(BattleRegionTraitKind.RiftErosion)); // 균열 침식
            Assert.That(LoadDungeon("DG016").RecommendedLevel, Is.GreaterThan(LoadDungeon("DG015").RecommendedLevel)); // 두 번째가 더 어려움

            SaveData saveData = CreateSave(); // 새 게임
            RegionArrivalDefinition arrival = RegionVisitService.Find("REGION_SEA"); // 첫 방문 이야기
            Assert.That(arrival.CharacterId, Is.EqualTo("CH_LILIA")); // 릴리아
            GlossaryEntry rift = WorldGlossaryCatalog.All.First(entry => entry.Term == "검은 균열"); // 단어
            Assert.That(rift.IsUnlocked(saveData), Is.False); // 방문 전 잠김
            RegionVisitService.CompleteArrival(saveData, arrival, null); // 방문
            Assert.That(rift.IsUnlocked(saveData), Is.True); // 열림
        }

        [Test] // 균열은 마왕성 클리어 후부터 · 주기마다 한 지역에 열리고 같은 날이면 항상 같은 곳
        public void Rift_OpensAfterCastle_AndIsDeterministic() // 균열 발생 테스트
        {
            SaveData early = CreateSave(); // 새 게임
            early.SetCurrentDay(9); // 충분히 지난 날
            Assert.That(RiftService.Refresh(early), Is.Empty); // 아직 균열 없음
            Assert.That(RiftService.IsOpen(early), Is.False); // 열린 균열 없음

            SaveData saveData = CreateLateSave(9); // 마왕성 클리어 · 9일차
            string message = RiftService.Refresh(saveData); // 균열 갱신
            Assert.That(message, Does.Contain("검은 균열")); // 발생 안내
            Assert.That(RiftService.IsOpen(saveData), Is.True); // 균열 열림
            Assert.That(RiftService.GetRemainingDays(saveData), Is.EqualTo(RiftService.DurationDays)); // 남은 일수
            AdventureRegion region = AdventureRegionCatalog.Get(saveData.RiftState.RegionId); // 지역
            Assert.That(RiftService.IsRiftRegion(saveData, region), Is.True); // 그 지역 표시
            Assert.That(region.DungeonIds, Has.Member(saveData.RiftState.DungeonId)); // 그 지역 던전에서 열림
            Assert.That(DungeonProgressionPolicy.IsUnlocked(saveData, saveData.RiftState.DungeonId), Is.True); // 열린 던전에서만
            Assert.That(RiftService.PickRegion(CreateLateSave(9), 9).Id, Is.EqualTo(region.Id)); // 같은 날이면 같은 지역
            Assert.That(RiftService.Refresh(saveData), Is.Empty); // 이미 열려 있으면 그대로
        }

        [Test] // 균열을 막으면 침식도 감소 · 보상 1.5배, 놓치면 침식도 증가
        public void Rift_ClearRelievesErosion_MissRaisesIt() // 균열 결과 테스트
        {
            SaveData saveData = CreateLateSave(9); // 균열 해금 저장
            RiftService.Refresh(saveData); // 균열 발생
            AdventureRegion region = AdventureRegionCatalog.Get(saveData.RiftState.RegionId); // 지역
            int before = RegionErosionService.GetOrInitializeErosion(saveData, region.ErosionRegionId); // 이전 침식도
            Assert.That(RiftService.BeginRun(saveData), Is.True); // 균열 탐험 시작
            Assert.That(RiftService.ApplyRewardBonus(1000), Is.EqualTo(1500)); // 골드 1.5배
            string cleared = RiftService.CompleteRun(saveData, saveData.RiftState.DungeonId); // 클리어
            Assert.That(cleared, Does.Contain("막았습니다")); // 안내
            Assert.That(RegionErosionService.GetErosion(saveData, region.ErosionRegionId), Is.EqualTo(System.Math.Max(0, before - RiftService.ClearErosionRelief))); // 침식도 감소
            Assert.That(RiftService.ApplyRewardBonus(1000), Is.EqualTo(1000)); // 균열 탐험 종료 후 보상 원래대로
            Assert.That(RiftService.IsOpen(saveData), Is.False); // 막은 균열은 더 이상 대상 아님

            SaveData missed = CreateLateSave(9); // 다른 저장
            RiftService.Refresh(missed); // 균열 발생
            AdventureRegion missedRegion = AdventureRegionCatalog.Get(missed.RiftState.RegionId); // 지역
            int beforeMiss = RegionErosionService.GetOrInitializeErosion(missed, missedRegion.ErosionRegionId); // 이전 침식도
            missed.SetCurrentDay(missed.RiftState.EndDay + 1); // 기한 초과
            Assert.That(RiftService.Refresh(missed), Does.Contain("막지 못했습니다")); // 실패 안내
            Assert.That(RegionErosionService.GetErosion(missed, missedRegion.ErosionRegionId), Is.EqualTo(System.Math.Min(RegionErosionSaveData.MaxErosion, beforeMiss + RiftService.MissErosionPenalty))); // 침식도 증가 (최대 100까지)
            Assert.That(missed.RiftState.IsOpen, Is.False); // 균열 닫힘
        }

        [Test] // 오늘의 의뢰 : 3개 · 같은 날이면 같은 의뢰 · 날짜가 바뀌면 새 의뢰
        public void GuildQuests_AreDailyAndDeterministic() // 의뢰 발행 테스트
        {
            SaveData saveData = CreateSave(); // 새 게임
            List<GuildQuestProgress> quests = GuildQuestService.GetToday(saveData); // 오늘 의뢰
            Assert.That(quests.Count, Is.EqualTo(GuildQuestCatalog.DailyQuestCount)); // 3개
            Assert.That(quests.Select(quest => quest.Definition.Id), Is.Unique); // 중복 없음
            Assert.That(GuildQuestService.PickToday(CreateSave(), saveData.CurrentDay), Is.EqualTo(quests.Select(quest => quest.Definition.Id).ToList())); // 같은 날 같은 의뢰

            foreach (GuildQuestProgress quest in quests) // 의뢰 순회
            {
                Assert.That(GuildQuestService.IsAvailable(saveData, quest.Definition), Is.True, quest.Definition.Id); // 지금 할 수 있는 의뢰만
                Assert.That(GuildQuestCatalog.GetTitle(quest.Definition, string.Empty), Is.Not.Empty); // 제목
            }

            saveData.SetCurrentDay(saveData.CurrentDay + 1); // 다음 날
            Assert.That(GuildQuestService.GetToday(saveData).Count, Is.EqualTo(GuildQuestCatalog.DailyQuestCount)); // 새 의뢰
            Assert.That(saveData.QuestBoard.Day, Is.EqualTo(saveData.CurrentDay)); // 오늘 기준
        }

        [Test] // 의뢰 진행 : 전투 승리 · 던전 클리어 · 속성 동료 · 균열
        public void GuildQuests_ProgressFromBattleResults() // 의뢰 진행 테스트
        {
            SaveData saveData = CreateSave(); // 새 게임
            saveData.QuestBoard.ResetForDay(saveData.CurrentDay, new[] { "GQ_WIN2", "GQ_FOREST", "GQ_LIGHT" }); // 테스트 의뢰 고정
            GuildQuestService.RecordVictory(saveData, "DG002", true, Starters, CharacterElementTable.Get, false); // 다른 던전 승리
            Assert.That(saveData.QuestBoard.Find("GQ_WIN2").Progress, Is.EqualTo(1)); // 전투 승리 진행
            Assert.That(saveData.QuestBoard.Find("GQ_FOREST").Progress, Is.EqualTo(0)); // 다른 던전은 진행 없음
            Assert.That(saveData.QuestBoard.Find("GQ_LIGHT").Progress, Is.EqualTo(1)); // 빛 속성(세레나) 참가
            GuildQuestService.RecordVictory(saveData, "DG001", true, Starters, CharacterElementTable.Get, false); // 숲 클리어
            Assert.That(saveData.QuestBoard.Find("GQ_FOREST").Progress, Is.EqualTo(1)); // 던전 진행
            Assert.That(saveData.QuestBoard.Find("GQ_WIN2").Progress, Is.EqualTo(2)); // 목표 도달
            GuildQuestService.RecordVictory(saveData, "DG001", false, Starters, CharacterElementTable.Get, false); // 일반 전투
            Assert.That(saveData.QuestBoard.Find("GQ_WIN2").Progress, Is.EqualTo(2)); // 목표를 넘지 않음

            saveData.QuestBoard.ResetForDay(saveData.CurrentDay, new[] { "GQ_RIFT" }); // 균열 의뢰
            GuildQuestService.RecordVictory(saveData, "DG009", true, Starters, CharacterElementTable.Get, false); // 균열 아님
            Assert.That(saveData.QuestBoard.Find("GQ_RIFT").Progress, Is.EqualTo(0)); // 진행 없음
            GuildQuestService.RecordVictory(saveData, "DG009", true, Starters, CharacterElementTable.Get, true); // 균열 클리어
            Assert.That(saveData.QuestBoard.Find("GQ_RIFT").Progress, Is.EqualTo(1)); // 진행
        }

        [Test] // 의뢰 보상 : 완료해야 받고 두 번 받지 못하며, 전부 완료하면 보너스
        public void GuildQuests_ClaimRewardsOnceAndBonus() // 의뢰 보상 테스트
        {
            SaveData saveData = CreateSave(); // 새 게임
            saveData.QuestBoard.ResetForDay(saveData.CurrentDay, new[] { "GQ_WIN2" }); // 전투 2회 의뢰
            Assert.That(GuildQuestService.TryClaim(saveData, null, "GQ_WIN2"), Does.Contain("아직")); // 미완료
            GuildQuestService.RecordVictory(saveData, "DG001", false, Starters, CharacterElementTable.Get, false); // 1회
            GuildQuestService.RecordVictory(saveData, "DG001", false, Starters, CharacterElementTable.Get, false); // 2회
            int gold = GoldCurrencyService.GetGold(saveData); // 이전 골드
            Assert.That(GuildQuestService.TryClaim(saveData, null, "GQ_WIN2"), Does.Contain("의뢰 완료")); // 수령
            Assert.That(GoldCurrencyService.GetGold(saveData), Is.EqualTo(gold + GuildQuestCatalog.Find("GQ_WIN2").RewardGold)); // 골드 지급
            Assert.That(GuildQuestService.TryClaim(saveData, null, "GQ_WIN2"), Does.Contain("이미")); // 중복 불가
            Assert.That(GuildQuestService.AllComplete(saveData), Is.True); // 전부 완료
            int bonusGold = GoldCurrencyService.GetGold(saveData); // 보너스 전 골드
            Assert.That(GuildQuestService.CanClaimBonus(saveData), Is.True); // 보너스 가능
            Assert.That(GuildQuestService.TryClaimBonus(saveData), Does.Contain("전부 완료")); // 보너스 수령
            Assert.That(GoldCurrencyService.GetGold(saveData), Is.EqualTo(bonusGold + GuildQuestCatalog.BonusGold)); // 보너스 골드
            Assert.That(GuildQuestService.TryClaimBonus(saveData), Does.Contain("이미")); // 중복 불가
        }

        [Test] // 캐릭터 속성 표가 실제 에셋과 일치
        public void CharacterElementTable_MatchesAssets() // 속성 표 테스트
        {
            Assert.That(CharacterElementTable.All.Count, Is.EqualTo(12)); // 12인

            foreach (KeyValuePair<string, ElementType> pair in CharacterElementTable.All) // 표 순회
            {
                CharacterData character = LoadCharacter(pair.Key); // 캐릭터 에셋
                Assert.That(character, Is.Not.Null, pair.Key); // 존재
                Assert.That(character.Element, Is.EqualTo(pair.Value), pair.Key); // 속성 일치
            }
        }

        [Test] // 1차 밸런스 : 권장 레벨 파티 기준으로 일반 웨이브는 1분 안에, 보스는 1~2분 30초, 전멸 여유가 남아야 함
        public void Balance_EveryDungeonMeetsTargets() // 밸런스 테스트
        {
            List<CharacterData> party = new List<CharacterData>(); // 기준 파티
            foreach (string id in Starters) party.Add(LoadCharacter(id)); // 초기 4인

            foreach (string dungeonId in DungeonSelectionRuntimeState.SupportedDungeonIds) // 던전 순회
            {
                DungeonData dungeon = LoadDungeon(dungeonId); // 던전
                DungeonBattleTestProfile profile = DungeonBattleTestProfile.Get(dungeonId); // 배율

                foreach (DungeonEncounterWave wave in dungeon.EncounterWaves) // 웨이브 순회
                {
                    List<MonsterData> monsters = new List<MonsterData>(); // 웨이브 몬스터
                    foreach (string monsterId in wave.MonsterIds) monsters.Add(LoadMonster(monsterId)); // 로드
                    BalanceEstimate estimate = BalanceSimulator.Estimate(party, dungeon.RecommendedLevel, monsters, profile); // 예상 결과
                    bool boss = BalanceSimulator.IsBossWave(monsters); // 보스 웨이브 여부
                    string label = BalanceSimulator.Describe(dungeonId, dungeon.RecommendedLevel, estimate, boss); // 설명

                    if (boss) // 보스
                    {
                        Assert.That(estimate.ClearSeconds, Is.InRange(BalanceSimulator.BossClearMinSeconds, BalanceSimulator.BossClearMaxSeconds), label); // 처치 시간
                        Assert.That(estimate.SafetyRatio, Is.GreaterThanOrEqualTo(BalanceSimulator.MinSafetyRatio), label); // 전멸 여유
                    }
                    else // 일반
                    {
                        Assert.That(estimate.ClearSeconds, Is.LessThanOrEqualTo(BalanceSimulator.RegularClearMaxSeconds), label); // 처치 시간
                        Assert.That(estimate.SafetyRatio, Is.GreaterThanOrEqualTo(2f), label); // 일반 웨이브는 더 여유
                    }
                }
            }
        }

        [Test] // 밸런스 표 : 레벨이 오를수록 보스 전투가 조금씩 길어지고, 성장률은 8%
        public void Balance_BossFightsGrowWithLevel() // 밸런스 곡선 테스트
        {
            Assert.That(BattleGrowthFormula.GrowthPerLevel, Is.EqualTo(0.08f).Within(0.0001f)); // 레벨당 성장 8%
            List<CharacterData> party = new List<CharacterData>(); // 기준 파티
            foreach (string id in Starters) party.Add(LoadCharacter(id)); // 초기 4인
            float previous = 0f; // 이전 보스 시간
            int checkedCount = 0; // 검사한 보스 수

            foreach (string dungeonId in new[] { "DG005", "DG004", "DG007", "DG008", "DG013", "DG014", "DG012", "DG016" }) // 레벨 오름차순 보스 던전
            {
                DungeonData dungeon = LoadDungeon(dungeonId); // 던전
                List<MonsterData> monsters = new List<MonsterData>(); // 보스 웨이브
                foreach (string monsterId in dungeon.EncounterWaves[dungeon.EncounterWaves.Count - 1].MonsterIds) monsters.Add(LoadMonster(monsterId)); // 로드
                BalanceEstimate estimate = BalanceSimulator.Estimate(party, dungeon.RecommendedLevel, monsters, DungeonBattleTestProfile.Get(dungeonId)); // 예상
                Assert.That(estimate.ClearSeconds, Is.GreaterThan(previous - 5f), BalanceSimulator.Describe(dungeonId, dungeon.RecommendedLevel, estimate, true)); // 뒤 던전이 더 길다
                previous = estimate.ClearSeconds; // 갱신
                checkedCount++; // 검사 수 증가
            }

            Assert.That(checkedCount, Is.EqualTo(8)); // 8개 확인
        }
    }
}
