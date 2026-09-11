using System.Collections.Generic; // 목록 자료형
using System.Linq; // 목록 검색 기능
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 지역 특징·진행 기능
using ProjectH.Data; // 던전·몬스터 데이터 기능
using ProjectH.Dialogue; // 대화 파일 기능
using ProjectH.Diary; // 세계관 단어 기능
using ProjectH.Dungeon; // 지역·첫 방문 기능
using ProjectH.SaveSystem; // 저장 기능
using UnityEditor; // 에셋 로드 기능
using UnityEngine; // 게임 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class RegionExpansionTests // Day65 노아르 마법도시 · 실바란 숲 지역 확장 테스트
    {
        private static readonly string[] NewDungeonIds = { "DG005", "DG006" }; // 신규 던전
        private GameObject registryObject; // 테스트 Registry 객체
        private BattleCombatRegistry registry; // 테스트 전투 Registry

        private static DungeonData LoadDungeon(string id) => AssetDatabase.LoadAssetAtPath<DungeonData>($"Assets/ProjectH/Data/Dungeons/{id}.asset"); // 던전 에셋

        private static MonsterData LoadMonster(string id) => AssetDatabase.LoadAssetAtPath<MonsterData>($"Assets/ProjectH/Data/Monsters/{id}.asset"); // 몬스터 에셋

        private static SaveData CreateSave() => SaveData.CreateNewGame(new[] { "CH_SERENA", "CH_ELLEN", "CH_LILIA", "CH_EVE" }); // 새 게임

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 전투 준비
        {
            BattleRuntimeStates.ResetAll(); // 전투 정적 상태 초기화
            registryObject = new GameObject("RegionExpansionTests.Registry"); // Registry 객체
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

        [Test] // 지도 : 노아르 → DG005, 실바란 → DG006, 사막 안내는 66일차
        public void Regions_LinkNewDungeons() // 지역 연결 테스트
        {
            Assert.That(AdventureRegionCatalog.FindByDungeon("DG005").Id, Is.EqualTo("REGION_NOIR")); // 노아르
            Assert.That(AdventureRegionCatalog.FindByDungeon("DG006").Id, Is.EqualTo("REGION_SILVARAN")); // 실바란
            Assert.That(AdventureRegionCatalog.Get("REGION_NOIR").Kind, Is.EqualTo(AdventureRegionKind.Dungeon)); // 던전 지역
            Assert.That(AdventureRegionCatalog.Get("REGION_DESERT").LockedHint, Does.Contain("66일차")); // 사막 안내 갱신
        }

        [Test] // 신규 던전 : 웨이브 4개(마지막은 2페이즈 보스) · 몬스터 에셋 · 카탈로그 등록
        public void NewDungeons_HaveBossWavesAndRegisteredMonsters() // 던전 데이터 테스트
        {
            ProjectHDataCatalog catalog = AssetDatabase.LoadAssetAtPath<ProjectHDataCatalog>("Assets/ProjectH/Data/Database/ProjectHDataCatalog.asset"); // 데이터 카탈로그
            HashSet<string> monsterIds = new HashSet<string>(); // 신규 몬스터

            foreach (string dungeonId in NewDungeonIds) // 던전 순회
            {
                DungeonData dungeon = LoadDungeon(dungeonId); // 던전
                Assert.That(catalog.Dungeons.Any(entry => entry != null && entry.Id == dungeonId), Is.True, dungeonId); // 카탈로그 등록
                Assert.That(dungeon.EncounterWaves.Count, Is.EqualTo(4), dungeonId); // 웨이브 4개
                string bossId = dungeon.EncounterWaves[3].MonsterIds[0]; // 마지막 웨이브
                MonsterData boss = LoadMonster(bossId); // 보스
                Assert.That(boss.BossPattern, Is.Not.Null, bossId); // 보스 패턴
                Assert.That(boss.BossPattern.Phases.Count, Is.EqualTo(2), bossId); // 2페이즈
                Assert.That(boss.BossPattern.Patterns.Count, Is.GreaterThanOrEqualTo(4), bossId); // 패턴 4개 이상

                foreach (DungeonEncounterWave wave in dungeon.EncounterWaves) // 웨이브 순회
                {
                    foreach (string monsterId in wave.MonsterIds) monsterIds.Add(monsterId); // 몬스터 수집
                }
            }

            Assert.That(monsterIds.Count, Is.EqualTo(8)); // 지역당 3종 + 보스 1종

            foreach (string monsterId in monsterIds) // 몬스터 순회
            {
                MonsterData monster = LoadMonster(monsterId); // 몬스터
                Assert.That(monster, Is.Not.Null, monsterId); // 에셋 존재
                Assert.That(monster.Description, Is.Not.Empty, monsterId); // 일기장 설명
                Assert.That(catalog.Monsters.Any(entry => entry != null && entry.Id == monsterId), Is.True, monsterId); // 카탈로그 등록
            }
        }

        [Test] // 난이도 계단 : 권장 레벨 · 전투 배율 · 골드/활력이 늪지대(DG003)와 마왕성(DG004) 사이
        public void NewDungeons_SitBetweenSwampAndCastle() // 난이도 테스트
        {
            DungeonData swamp = LoadDungeon("DG003"); // 늪지대
            DungeonData castle = LoadDungeon("DG004"); // 마왕성

            foreach (string dungeonId in NewDungeonIds) // 던전 순회
            {
                DungeonData dungeon = LoadDungeon(dungeonId); // 던전
                Assert.That(dungeon.RecommendedLevel, Is.InRange(swamp.RecommendedLevel + 1, castle.RecommendedLevel - 1), dungeonId); // 권장 레벨
                Assert.That(DungeonBattleTestProfile.Get(dungeonId).HealthMultiplier, Is.InRange(DungeonBattleTestProfile.Get("DG003").HealthMultiplier, DungeonBattleTestProfile.Get("DG004").HealthMultiplier), dungeonId); // 적 체력 배율
                float ratio = dungeon.RewardGold / (float)dungeon.VitalityCost; // 활력당 골드
                Assert.That(ratio, Is.InRange(swamp.RewardGold / (float)swamp.VitalityCost, castle.RewardGold / (float)castle.VitalityCost), dungeonId); // 경제 균형
            }
        }

        [Test] // 해금 : 늪지대(DG003)를 클리어하면 두 지역 모두 열림
        public void NewDungeons_UnlockAfterSwamp() // 해금 테스트
        {
            SaveData saveData = CreateSave(); // 새 게임
            Assert.That(DungeonProgressionPolicy.GetState(saveData, "DG005"), Is.EqualTo(DungeonProgressState.Locked)); // 처음엔 잠김
            DungeonProgressSaveAdapter.MarkCleared(saveData, "DG003"); // 늪지대 클리어
            Assert.That(DungeonProgressionPolicy.GetState(saveData, "DG005"), Is.EqualTo(DungeonProgressState.Available)); // 노아르 열림
            Assert.That(DungeonProgressionPolicy.GetState(saveData, "DG006"), Is.EqualTo(DungeonProgressState.Available)); // 실바란 열림
        }

        [Test] // 지역 특징 : 던전별 종류와 안내 문구
        public void Traits_MapToDungeons() // 특징 연결 테스트
        {
            Assert.That(BattleRegionTraitCatalog.GetKind("DG005"), Is.EqualTo(BattleRegionTraitKind.ManaSurge)); // 노아르
            Assert.That(BattleRegionTraitCatalog.GetKind("DG006"), Is.EqualTo(BattleRegionTraitKind.SpiritBlessing)); // 실바란
            Assert.That(BattleRegionTraitCatalog.GetKind("DG001"), Is.EqualTo(BattleRegionTraitKind.None)); // 기존 던전
            Assert.That(BattleRegionTraitCatalog.GetDescription(BattleRegionTraitKind.ManaSurge), Does.Contain("10초")); // 안내 문구
        }

        [Test] // 마나 폭주 : 10초마다 적 공격 +8% · 방어 -8%, 최대 5중첩
        public void ManaSurge_StacksAttackUpAndDefenseDown() // 마나 폭주 테스트
        {
            BattleActor enemy = CreateEnemy("ENEMY_0", 1000, 100, 100); // 적
            BattleRegionTraitTicker ticker = new BattleRegionTraitTicker(); // 발동기
            Assert.That(ticker.Tick(registry, BattleRegionTraitKind.ManaSurge, 9.5f), Is.EqualTo(0)); // 10초 전
            Assert.That(ticker.Tick(registry, BattleRegionTraitKind.ManaSurge, 0.5f), Is.EqualTo(1)); // 10초 도달
            Assert.That(ticker.ManaStacks, Is.EqualTo(1)); // 1중첩
            Assert.That(BattleSkillRuntimeState.GetAttackMultiplier(enemy.Stats.RuntimeId), Is.EqualTo(1.08f).Within(0.001f)); // 공격 +8%
            Assert.That(BattleSkillRuntimeState.GetEffectiveDefense(enemy.Stats), Is.EqualTo(92)); // 방어 -8%
            ticker.Tick(registry, BattleRegionTraitKind.ManaSurge, 100f); // 오래 끌기
            Assert.That(ticker.ManaStacks, Is.EqualTo(BattleRegionTraitCatalog.ManaSurgeMaxStacks)); // 5중첩에서 멈춤
            Assert.That(BattleSkillRuntimeState.GetAttackMultiplier(enemy.Stats.RuntimeId), Is.EqualTo(1.40f).Within(0.001f)); // 공격 +40%
            Assert.That(BattleSkillRuntimeState.GetEffectiveDefense(enemy.Stats), Is.EqualTo(60)); // 방어 -40%
        }

        [Test] // 정령의 가호 : 5초마다 적 3% 회복, 약화 상태면 절반
        public void SpiritBlessing_HealsEnemies_HalfWhenDebuffed() // 정령의 가호 테스트
        {
            BattleActor healthy = CreateEnemy("ENEMY_0", 1000, 10, 0); // 약화 없는 적
            BattleActor debuffed = CreateEnemy("ENEMY_1", 1000, 10, 0); // 약화 걸린 적
            Hit(healthy, 500); // 체력 500
            Hit(debuffed, 500); // 체력 500
            BattleSkillRuntimeState.AddModifier(debuffed.Stats.RuntimeId, BattleRuntimeModifierKind.AttackReductionPercent, 0.1f, 30f, "TEST_ATK_DOWN"); // 공격 감소 (약화)
            BattleRegionTraitTicker ticker = new BattleRegionTraitTicker(); // 발동기
            Assert.That(ticker.Tick(registry, BattleRegionTraitKind.SpiritBlessing, 5f), Is.EqualTo(1)); // 5초 발동
            Assert.That(healthy.Stats.CurrentHp, Is.EqualTo(530)); // 3% 회복
            Assert.That(debuffed.Stats.CurrentHp, Is.EqualTo(515)); // 절반 1.5% 회복
        }

        [Test] // 첫 방문 : 1회만 · 방문 기록으로 세계관 단어 열림 · 끝까지 보면 안내 동료 호감도
        public void Arrival_PlaysOnce_UnlocksGlossary_AndRewardsOnFinish() // 첫 방문 테스트
        {
            SaveData saveData = CreateSave(); // 새 게임
            GlossaryEntry noir = WorldGlossaryCatalog.All.First(entry => entry.Term == "노아르 마법도시"); // 노아르 단어
            Assert.That(noir.IsUnlocked(saveData), Is.False); // 방문 전 잠김
            RegionArrivalDefinition noirArrival = RegionVisitService.GetPendingArrival(saveData, "DG005"); // 노아르 첫 방문
            Assert.That(noirArrival.CharacterId, Is.EqualTo("CH_LILIA")); // 릴리아의 고향
            int liliaBefore = AffinityService.GetAffinity(saveData, "CH_LILIA"); // 이전 호감도
            RegionVisitService.CompleteArrival(saveData, noirArrival, new DialogueRunner(DialogueLibrary.Load(noirArrival.ScriptId))); // 중간에 닫음
            Assert.That(RegionVisitService.GetPendingArrival(saveData, "DG005"), Is.Null); // 다시 나오지 않음
            Assert.That(noir.IsUnlocked(saveData), Is.True); // 단어 열림
            Assert.That(AffinityService.GetAffinity(saveData, "CH_LILIA"), Is.EqualTo(liliaBefore)); // 끝까지 안 봐서 보상 없음

            RegionArrivalDefinition silvaran = RegionVisitService.GetPendingArrival(saveData, "DG006"); // 실바란 첫 방문
            int eveBefore = AffinityService.GetAffinity(saveData, "CH_EVE"); // 이전 호감도
            RegionVisitService.CompleteArrival(saveData, silvaran, FinishedRunner(silvaran.ScriptId)); // 끝까지 봄
            Assert.That(AffinityService.GetAffinity(saveData, "CH_EVE"), Is.GreaterThanOrEqualTo(eveBefore + RegionVisitService.ArrivalAffinity)); // 호감도 +3 이상
            Assert.That(RegionVisitService.GetPendingArrival(saveData, "DG001"), Is.Null); // 기존 지역은 첫 방문 이야기 없음
        }

        [Test] // 첫 방문 대사 2편 : 구조 검사 · 모든 선택 경로 종료
        public void ArrivalScripts_Validate() // 대사 테스트
        {
            foreach (RegionArrivalDefinition definition in RegionVisitService.All) // 정의 순회
            {
                DialogueScript script = DialogueLibrary.Load(definition.ScriptId); // 불러오기
                Assert.That(script, Is.Not.Null, definition.ScriptId); // 파일 존재
                List<string> errors = new List<string>(); // 오류 목록
                Assert.That(DialogueLibrary.Validate(script, errors), Is.True, string.Join("\n", errors)); // 구조 검사
                Assert.That(script.CharacterId, Is.EqualTo(definition.CharacterId)); // 안내 동료

                for (int choice = 0; choice < 2; choice++) // 두 선택 경로
                {
                    DialogueRunner runner = new DialogueRunner(script); // 진행기
                    int guard = 0; // 무한 반복 방지

                    while (!runner.IsFinished && guard++ < 200) // 끝까지
                    {
                        if (runner.IsWaitingForChoice) runner.Choose(System.Math.Min(choice, runner.Current.Choices.Count - 1)); // 선택
                        else runner.Advance(); // 넘기기
                    }

                    Assert.That(runner.IsFinished, Is.True, $"{definition.ScriptId} {choice}번 경로"); // 종료
                }
            }
        }

        private static DialogueRunner FinishedRunner(string scriptId) // 끝까지 본 대화 진행기
        {
            DialogueRunner runner = new DialogueRunner(DialogueLibrary.Load(scriptId)); // 진행기
            int guard = 0; // 무한 반복 방지

            while (!runner.IsFinished && guard++ < 200) // 끝까지
            {
                if (runner.IsWaitingForChoice) runner.Choose(0); // 첫 선택지
                else runner.Advance(); // 넘기기
            }

            return runner; // 반환
        }

        private static void Hit(BattleActor target, int damage) // 테스트용 고정 피해
        {
            target.ApplyDamage(new BattleDamageResult(BattleDamageType.True, "TEST", target.Stats.RuntimeId, damage, 0, damage, BattleElement.None, BattleElementAffinity.Neutral)); // 고정 피해 적용
        }

        private BattleActor CreateEnemy(string runtimeId, int maxHp, int attack, int defense) // 적군 테스트 액터 생성
        {
            GameObject targetObject = new GameObject(runtimeId); // 객체 생성
            BattleActor actor = targetObject.AddComponent<BattleActor>(); // 전투 액터
            BattleEnemyStats stats = new BattleEnemyStats(runtimeId, "MON_TEST", runtimeId, maxHp, attack, defense, 0, 1f, 1f, 1f); // 스탯
            actor.Initialize(BattleTeam.Enemy, stats, Vector3.right); // 초기화
            registry.Register(actor); // 등록
            return actor; // 반환
        }
    }
}
