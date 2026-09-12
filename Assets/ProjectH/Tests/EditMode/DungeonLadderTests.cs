using System.Collections.Generic; // 목록 자료형
using System.Linq; // 목록 검색 기능
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 진행·지역 특징 기능
using ProjectH.Data; // 던전·몬스터 데이터 기능
using ProjectH.Diary; // 세계관 단어 기능
using ProjectH.Dungeon; // 지역·첫 방문 기능
using ProjectH.SaveSystem; // 저장 기능
using ProjectH.UI; // 지원 던전 목록 기능
using UnityEditor; // 에셋 로드 기능
using UnityEngine; // 게임 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class DungeonLadderTests // Day66 카르니안 · 아스타르 + 지역별 던전 2개 (14던전 난이도 계단) 테스트
    {
        private GameObject registryObject; // 테스트 Registry 객체
        private BattleCombatRegistry registry; // 테스트 전투 Registry

        private static DungeonData LoadDungeon(string id) => AssetDatabase.LoadAssetAtPath<DungeonData>($"Assets/ProjectH/Data/Dungeons/{id}.asset"); // 던전 에셋

        private static SaveData CreateSave() => SaveData.CreateNewGame(new[] { "CH_SERENA", "CH_ELLEN", "CH_LILIA", "CH_EVE" }); // 새 게임

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 전투 준비
        {
            BattleRuntimeStates.ResetAll(); // 전투 정적 상태 초기화
            registryObject = new GameObject("DungeonLadderTests.Registry"); // Registry 객체
            registry = registryObject.AddComponent<BattleCombatRegistry>(); // Registry
            BattleSkillRuntimeState.SetRegistry(registry); // 스킬 Runtime 연결
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 전투 정리
        {
            BattleRuntimeStates.ResetAll(); // 전투 정적 상태 초기화
            foreach (BattleActor actor in Object.FindObjectsByType<BattleActor>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.DestroyImmediate(actor.gameObject); // 테스트 액터 제거
            Object.DestroyImmediate(registryObject); // Registry 제거
        }

        [Test] // 던전 지역 8곳은 던전 2개씩 (마왕성만 최종 던전 포함 3개) · 지역 안 던전은 같은 특징이며 뒤일수록 어렵다
        public void EveryDungeonRegion_HasTwoDungeons_WithSameTrait() // 지역당 던전 구성 테스트
        {
            int regions = 0; // 던전 지역 수

            foreach (AdventureRegion region in AdventureRegionCatalog.All) // 지역 순회
            {
                if (region.Kind != AdventureRegionKind.Dungeon) continue; // 던전 지역만
                regions++; // 수 증가
                Assert.That(region.DungeonIds.Count, Is.EqualTo(region.Id == "REGION_DEMON_CASTLE" ? 3 : 2), region.Id); // 마왕성은 최종 던전까지 3개 (Day71)

                for (int index = 1; index < region.DungeonIds.Count; index++) // 지역 안 던전 순회
                {
                    Assert.That(BattleRegionTraitCatalog.GetKind(region.DungeonIds[index]), Is.EqualTo(BattleRegionTraitCatalog.GetKind(region.DungeonIds[0])), region.Id); // 같은 특징
                    Assert.That(LoadDungeon(region.DungeonIds[index]).RecommendedLevel, Is.GreaterThan(LoadDungeon(region.DungeonIds[index - 1]).RecommendedLevel), region.Id); // 뒤일수록 어려움
                }
            }

            Assert.That(regions, Is.EqualTo(8)); // 숲 · 늪지대 · 마왕성 · 노아르 · 실바란 · 카르니안 · 사막 · 바다
            Assert.That(DungeonSelectionRuntimeState.SupportedDungeonIds.Count, Is.EqualTo(17)); // 17던전 (Day71 최종 던전 DG017 추가)
            Assert.That(AdventureRegionCatalog.Get("REGION_DESERT").Kind, Is.EqualTo(AdventureRegionKind.Dungeon)); // 사막 열림
            Assert.That(AdventureRegionCatalog.FindByDungeon("DG007").Id, Is.EqualTo("REGION_KARNIAN")); // 카르니안 연결
        }

        [Test] // 난이도 계단 : 레벨이 높을수록 골드 · 활력 · 적 체력 배율이 줄지 않음, 경험치 = 60 + 20 × 레벨
        public void Ladder_RewardsAndDifficultyRiseWithLevel() // 난이도 계단 테스트
        {
            List<DungeonData> dungeons = DungeonSelectionRuntimeState.SupportedDungeonIds.Select(LoadDungeon).ToList(); // 전체 던전

            foreach (DungeonData low in dungeons) // 낮은 쪽
            {
                Assert.That(low.RewardExp, Is.EqualTo(60 + (20 * low.RecommendedLevel)), low.Id); // 경험치 공식

                foreach (DungeonData high in dungeons) // 높은 쪽
                {
                    if (high.RecommendedLevel <= low.RecommendedLevel) continue; // 더 높은 레벨만 비교
                    string pair = $"{low.Id}(Lv{low.RecommendedLevel}) → {high.Id}(Lv{high.RecommendedLevel})"; // 비교 이름
                    Assert.That(high.RewardGold, Is.GreaterThanOrEqualTo(low.RewardGold), pair); // 골드
                    Assert.That(high.VitalityCost, Is.GreaterThanOrEqualTo(low.VitalityCost), pair); // 활력
                    Assert.That(DungeonBattleTestProfile.Get(high.Id).HealthMultiplier, Is.GreaterThanOrEqualTo(DungeonBattleTestProfile.Get(low.Id).HealthMultiplier), pair); // 적 체력 배율
                }
            }
        }

        [Test] // 해금 : 앞 던전은 항상 더 쉬운 던전 · 확장 던전은 모두 마지막 웨이브가 보스
        public void Unlocks_FollowEasierDungeons_AndExpansionHasBosses() // 해금 · 보스 테스트
        {
            foreach (string dungeonId in DungeonSelectionRuntimeState.SupportedDungeonIds) // 던전 순회
            {
                DungeonData dungeon = LoadDungeon(dungeonId); // 던전
                string previousId = DungeonProgressionPolicy.GetPreviousDungeonId(dungeonId); // 앞 던전
                if (dungeonId != "DG001") Assert.That(LoadDungeon(previousId).RecommendedLevel, Is.LessThan(dungeon.RecommendedLevel), dungeonId); // 더 쉬운 던전 뒤에 열림
                if (string.CompareOrdinal(dungeonId, "DG005") < 0) continue; // 초기 4던전은 기존 구성 유지
                string bossId = dungeon.EncounterWaves[dungeon.EncounterWaves.Count - 1].MonsterIds[0]; // 마지막 웨이브
                MonsterData boss = AssetDatabase.LoadAssetAtPath<MonsterData>($"Assets/ProjectH/Data/Monsters/{bossId}.asset"); // 보스
                Assert.That(boss != null && boss.BossPattern != null, Is.True, $"{dungeonId} {bossId}"); // 보스 패턴

                foreach (DungeonEncounterWave wave in dungeon.EncounterWaves) // 웨이브 순회
                {
                    foreach (string monsterId in wave.MonsterIds) Assert.That(AssetDatabase.LoadAssetAtPath<MonsterData>($"Assets/ProjectH/Data/Monsters/{monsterId}.asset"), Is.Not.Null, $"{dungeonId} {monsterId}"); // 몬스터 에셋
                }
            }
        }

        [Test] // 해금 순서 : 마왕성 → 카르니안 · 아스타르 → 두 번째 던전 → 검은 성벽
        public void LateRegions_UnlockAfterCastle() // 후반 해금 테스트
        {
            SaveData saveData = CreateSave(); // 새 게임
            Assert.That(DungeonProgressionPolicy.IsUnlocked(saveData, "DG007"), Is.False); // 처음엔 잠김
            DungeonProgressSaveAdapter.MarkCleared(saveData, "DG004"); // 마왕성 클리어
            Assert.That(DungeonProgressionPolicy.IsUnlocked(saveData, "DG007"), Is.True); // 카르니안 열림
            Assert.That(DungeonProgressionPolicy.IsUnlocked(saveData, "DG008"), Is.True); // 아스타르 열림
            Assert.That(DungeonProgressionPolicy.IsUnlocked(saveData, "DG013"), Is.False); // 두 번째는 아직
            Assert.That(DungeonProgressionPolicy.IsUnlocked(saveData, "DG012"), Is.False); // 검은 성벽 잠김
            DungeonProgressSaveAdapter.MarkCleared(saveData, "DG008"); // 봉인 신전 클리어
            Assert.That(DungeonProgressionPolicy.IsUnlocked(saveData, "DG014"), Is.True); // 지하 고대 도시 열림
            DungeonProgressSaveAdapter.MarkCleared(saveData, "DG014"); // 지하 고대 도시 클리어
            Assert.That(DungeonProgressionPolicy.IsUnlocked(saveData, "DG012"), Is.True); // 검은 성벽 열림
        }

        [Test] // 혹한 : 8초마다 아군 공격 속도 -10%, 최대 3중첩, 정화하면 1중첩부터
        public void FrostMarch_StacksOnAllies_ResetByCleanse() // 혹한 테스트
        {
            BattleActor ally = CreateActor("ALLY_0", BattleTeam.Ally); // 아군
            CreateActor("ENEMY_0", BattleTeam.Enemy); // 적 (적이 있을 때만 발동)
            string id = ally.Stats.RuntimeId; // 아군 ID
            BattleRegionTraitTicker ticker = new BattleRegionTraitTicker(); // 발동기
            Assert.That(ticker.Tick(registry, BattleRegionTraitKind.FrostMarch, 8f), Is.EqualTo(1)); // 8초 발동
            Assert.That(BattleSkillRuntimeState.GetAttackSpeedReduction(id), Is.EqualTo(0.10f).Within(0.001f)); // 1중첩
            ticker.Tick(registry, BattleRegionTraitKind.FrostMarch, 8f); // 2중첩
            Assert.That(BattleSkillRuntimeState.GetAttackSpeedReduction(id), Is.EqualTo(0.20f).Within(0.001f)); // -20%
            ticker.Tick(registry, BattleRegionTraitKind.FrostMarch, 40f); // 오래 끌기
            Assert.That(BattleSkillRuntimeState.GetAttackSpeedReduction(id), Is.EqualTo(0.30f).Within(0.001f)); // 최대 -30%
            Assert.That(BattleSkillRuntimeState.RemoveDebuffs(id, 5), Is.GreaterThan(0)); // 정화
            Assert.That(BattleSkillRuntimeState.GetAttackSpeedReduction(id), Is.EqualTo(0f).Within(0.001f)); // 지워짐
            ticker.Tick(registry, BattleRegionTraitKind.FrostMarch, 8f); // 다시 발동
            Assert.That(BattleSkillRuntimeState.GetAttackSpeedReduction(id), Is.EqualTo(0.10f).Within(0.001f)); // 1중첩부터
        }

        [Test] // 모래폭풍 : 12초마다 아군 명중률 -25%, 정화 가능
        public void Sandstorm_LowersAllyAccuracy_Cleansable() // 모래폭풍 테스트
        {
            BattleActor ally = CreateActor("ALLY_0", BattleTeam.Ally); // 아군
            BattleActor enemy = CreateActor("ENEMY_0", BattleTeam.Enemy); // 적
            BattleRegionTraitTicker ticker = new BattleRegionTraitTicker(); // 발동기
            Assert.That(ticker.Tick(registry, BattleRegionTraitKind.Sandstorm, 11f), Is.EqualTo(0)); // 12초 전
            Assert.That(ticker.Tick(registry, BattleRegionTraitKind.Sandstorm, 1f), Is.EqualTo(1)); // 12초 발동
            Assert.That(BattleSkillRuntimeState.GetEffectiveAccuracy(ally.Stats), Is.EqualTo(0.75f).Within(0.001f)); // 명중 75%
            Assert.That(BattleSkillRuntimeState.GetEffectiveAccuracy(enemy.Stats), Is.EqualTo(1f).Within(0.001f)); // 적은 영향 없음
            BattleSkillRuntimeState.RemoveDebuffs(ally.Stats.RuntimeId, 5); // 정화
            Assert.That(BattleSkillRuntimeState.GetEffectiveAccuracy(ally.Stats), Is.EqualTo(1f).Within(0.001f)); // 회복
        }

        [Test] // 첫 방문 : 카르니안은 엘렌, 사막은 세레나 · 세계관 단어 해금
        public void Arrivals_KarnianAndDesert() // 첫 방문 테스트
        {
            SaveData saveData = CreateSave(); // 새 게임
            Assert.That(RegionVisitService.GetPendingArrival(saveData, "DG013").CharacterId, Is.EqualTo("CH_ELLEN")); // 두 번째 던전으로 먼저 가도 첫 방문
            RegionArrivalDefinition desert = RegionVisitService.GetPendingArrival(saveData, "DG008"); // 사막
            Assert.That(desert.CharacterId, Is.EqualTo("CH_SERENA")); // 세레나
            GlossaryEntry astar = WorldGlossaryCatalog.All.First(entry => entry.Term == "아스타르 사막"); // 단어
            Assert.That(astar.IsUnlocked(saveData), Is.False); // 방문 전 잠김
            RegionVisitService.CompleteArrival(saveData, desert, null); // 방문
            Assert.That(astar.IsUnlocked(saveData), Is.True); // 열림
            Assert.That(RegionVisitService.GetPendingArrival(saveData, "DG014"), Is.Null); // 같은 지역 두 번째 던전은 다시 안 나옴
        }

        [Test] // 다음 던전 안내 : 열렸지만 아직 클리어하지 않은 던전 · 그 지역에만 안내 아이콘
        public void Guide_MarksUnlockedUnclearedDungeons() // 안내 테스트
        {
            SaveData saveData = CreateSave(); // 새 게임
            Assert.That(DungeonGuideService.GetNewDungeonIds(saveData), Is.EqualTo(new[] { "DG001" })); // 처음엔 첫 던전만
            Assert.That(DungeonGuideService.HasNewDungeon(saveData, AdventureRegionCatalog.Get("REGION_FOREST")), Is.True); // 숲에 안내
            DungeonProgressSaveAdapter.MarkCleared(saveData, "DG001"); // 1단계 클리어
            Assert.That(DungeonGuideService.IsNewDungeon(saveData, "DG001"), Is.False); // 클리어한 곳은 안내 끝
            Assert.That(DungeonGuideService.GetNewDungeonIds(saveData), Is.EqualTo(new[] { "DG002" })); // 다음 레벨 던전 안내
            DungeonProgressSaveAdapter.MarkCleared(saveData, "DG002"); // 2단계 클리어
            DungeonProgressSaveAdapter.MarkCleared(saveData, "DG003"); // 늪지대 클리어
            Assert.That(DungeonGuideService.GetNewDungeonIds(saveData), Is.EquivalentTo(new[] { "DG004", "DG005", "DG006", "DG009" })); // 새로 열린 4곳
            Assert.That(DungeonGuideService.HasNewDungeon(saveData, AdventureRegionCatalog.Get("REGION_FOREST")), Is.False); // 다 깬 숲은 안내 없음
            Assert.That(DungeonGuideService.HasNewDungeon(saveData, AdventureRegionCatalog.Get("REGION_SWAMP")), Is.True); // 늪지대 두 번째 던전
            Assert.That(DungeonGuideService.HasNewDungeon(saveData, AdventureRegionCatalog.Get("REGION_KARNIAN")), Is.False); // 아직 잠긴 지역
            Assert.That(DungeonGuideService.HasNewDungeon(saveData, AdventureRegionCatalog.Get(AdventureRegionCatalog.VillageRegionId)), Is.False); // 마을은 대상 아님
            Assert.That(DungeonGuideService.GetNewDungeonIds(null), Is.Empty); // 저장 없음
        }

        [Test] // 안내 아이콘 : 글자 없이 그림 (가운데 느낌표 · 바깥 투명)
        public void GuideIcon_IsDrawnShape() // 아이콘 테스트
        {
            Sprite icon = DungeonAlertArt.Get(); // 아이콘
            Assert.That(icon, Is.Not.Null); // 생성
            Texture2D texture = icon.texture; // 텍스처
            Color mark = texture.GetPixel(texture.width / 2, texture.height / 2 + 8); // 느낌표 막대
            Color fill = texture.GetPixel(texture.width / 2 + 14, texture.height / 2); // 노란 원
            Assert.That(texture.GetPixel(0, 0).a, Is.EqualTo(0f).Within(0.01f)); // 모서리 투명 (원 모양)
            Assert.That(fill.r, Is.GreaterThan(0.9f)); // 노랑
            Assert.That(mark.r, Is.LessThan(0.4f)); // 진한 느낌표
        }

        private BattleActor CreateActor(string runtimeId, BattleTeam team) // 테스트 액터 생성
        {
            GameObject targetObject = new GameObject(runtimeId); // 객체 생성
            BattleActor actor = targetObject.AddComponent<BattleActor>(); // 전투 액터
            IBattleCombatantStats stats = team == BattleTeam.Ally ? (IBattleCombatantStats)new BattleStats(runtimeId, "CH_SERENA", "CH_SERENA", BattlePosition.Healer, 1, 1000, 50, 20, 1f, 1f, 0f) : new BattleEnemyStats(runtimeId, "MON_TEST", runtimeId, 1000, 10, 0, 0, 1f, 1f, 1f); // 스탯
            actor.Initialize(team, stats, team == BattleTeam.Ally ? Vector3.zero : Vector3.right); // 초기화
            registry.Register(actor); // 등록
            return actor; // 반환
        }
    }
}
