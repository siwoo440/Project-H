using System.Collections.Generic; // 목록 자료형
using System.Linq; // 목록 검색 기능
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 던전 해금·배율 기능
using ProjectH.Data; // 던전·몬스터 데이터 기능
using ProjectH.Diary; // 세계관 단어 기능
using ProjectH.Dialogue; // 대사 파일·진행기 기능
using ProjectH.SaveSystem; // 저장 기능
using ProjectH.Story; // 챕터·최종장 기능
using ProjectH.UI; // 지원 던전 목록·NPC 기능
using UnityEditor; // 에셋 로드 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class FinaleTests // Day71 최종장(챕터 6) · 아르카이 · 최종 던전 DG017 테스트
    {
        private const string FinalDungeon = "DG017"; // 최종 던전

        private static SaveData CreateStorySave() // 프롤로그부터 시작하는 새 게임
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 세레나 1인
            ChapterService.EnsureStarted(saveData); // 스토리 시작
            return saveData; // 저장 반환
        }

        private static void PlayThroughChapters(SaveData saveData) // 진행 가능한 모든 단계 자동 진행
        {
            int guard = 0; // 무한 반복 방지

            while (guard++ < 96) // 단계 순회
            {
                ChapterStep step = ChapterService.GetCurrentStep(saveData); // 현재 단계
                if (step == null) break; // 준비된 이야기 끝

                if (step.Kind == ChapterStepKind.Dialogue) ChapterService.CompleteDialogue(saveData, step.Target); // 이야기 보기
                else { DungeonProgressSaveAdapter.MarkCleared(saveData, step.Target); ChapterService.NotifyDungeonCleared(saveData, step.Target); } // 던전
            }
        }

        private static DungeonData LoadDungeon(string dungeonId) => AssetDatabase.LoadAssetAtPath<DungeonData>($"Assets/ProjectH/Data/Dungeons/{dungeonId}.asset"); // 던전 에셋

        [Test] // 챕터 6 : 이야기 6편 + 던전 2개 · 모든 단계에 안내 문구
        public void FinalChapter_HasStepsAndDungeons() // 최종장 구성 테스트
        {
            Assert.That(ChapterCatalog.All.Count, Is.EqualTo(7)); // 프롤로그 + 챕터 1~6
            ChapterDefinition chapter = ChapterCatalog.Find("CHAPTER_06"); // 최종장
            Assert.That(chapter, Is.Not.Null); // 존재
            Assert.That(ChapterCatalog.GetNext("CHAPTER_06"), Is.Null); // 마지막 챕터
            Assert.That(chapter.Steps.Count(step => step.Kind == ChapterStepKind.Dialogue), Is.EqualTo(6)); // 이야기 6편
            Assert.That(chapter.Steps.Count(step => step.Kind == ChapterStepKind.ClearDungeon), Is.EqualTo(2)); // 던전 2개
            Assert.That(chapter.Steps.Last().GrantStoryFlag, Is.EqualTo(FinaleCatalog.FinaleClearedFlag)); // 마지막 단계에서 완료 기록
            Assert.That(chapter.Steps.Any(step => step.Target == FinalDungeon), Is.True); // 최종 던전 포함

            foreach (ChapterStep step in chapter.Steps) // 단계 순회
            {
                Assert.That(step.Summary, Is.Not.Empty, step.Target); // 다음 할 일 문구
                if (step.Kind != ChapterStepKind.Dialogue) continue; // 이야기만 파일 확인
                DialogueScript script = DialogueLibrary.Load(step.Target); // 대사 파일
                Assert.That(script, Is.Not.Null, step.Target); // 존재
                Assert.That(DialogueLibrary.Validate(script, new List<string>()), Is.True, step.Target); // 구조 검사
            }
        }

        [Test] // 끝까지 진행하면 최종장까지 마치고 아르카이가 열린다
        public void FullStory_ReachesFinaleAndUnlocksArchai() // 전체 진행 테스트
        {
            SaveData saveData = CreateStorySave(); // 새 게임
            PlayThroughChapters(saveData); // 끝까지
            Assert.That(saveData.ChapterProgress.IsFinished, Is.True); // 이야기 끝
            Assert.That(saveData.ChapterProgress.FinishedChapterId, Is.EqualTo("CHAPTER_06")); // 최종장까지
            Assert.That(saveData.HasStoryFlag(FinaleCatalog.ArchaiFlag), Is.True); // 진명 해금
            Assert.That(FinaleCatalog.IsFinaleCleared(saveData), Is.True); // 최종장 완료
            GlossaryEntry archai = WorldGlossaryCatalog.All.First(entry => entry.Term == FinaleCatalog.ArchaiName); // 단어
            Assert.That(archai.IsUnlocked(saveData), Is.True); // 열림
            Assert.That(archai.IsUnlocked(CreateStorySave()), Is.False); // 새 게임에서는 잠김
            Assert.That(NpcLineCatalog.Find("NPC_ARCHAI"), Is.Not.Null); // NPC 등록
            Assert.That(NpcLineCatalog.Find("NPC_ARCHAI").Name, Is.EqualTo(FinaleCatalog.ArchaiName)); // 이름
        }

        [Test] // 챕터 5를 마친 저장은 로비에 들어오면 최종장부터 이어진다
        public void Chapter5Save_ContinuesIntoFinale() // 이어 붙이기 테스트
        {
            SaveData saveData = CreateStorySave(); // 새 게임
            saveData.ChapterProgress.Finish("CHAPTER_05"); // 70일차까지 본 상태로 가정
            ChapterService.Refresh(saveData); // 로비 진입
            Assert.That(saveData.ChapterProgress.ChapterId, Is.EqualTo("CHAPTER_06")); // 최종장 시작
            Assert.That(ChapterService.GetPendingDialogueId(saveData), Is.EqualTo("CH6_01")); // 첫 이야기
        }

        [Test] // 선택지가 남긴 플래그가 저장에 기록되고, 두 선택이 결말 문구를 만든다
        public void FinaleChoices_RecordFlagsAndBuildEnding() // 선택 기록 테스트
        {
            Assert.That(CountFlaggedChoices("CH6_04"), Is.EqualTo(2)); // 선택 1 두 갈래
            Assert.That(CountFlaggedChoices("CH6_05"), Is.EqualTo(2)); // 선택 2 두 갈래

            foreach (bool stopSeal in new[] { true, false }) // 선택 1 두 경우
            {
                foreach (bool spare in new[] { true, false }) // 선택 2 두 경우
                {
                    SaveData saveData = CreateStorySave(); // 새 게임
                    Assert.That(FinaleCatalog.HasSealChoice(saveData), Is.False); // 아직 고르지 않음
                    ChapterService.ApplyChoiceFlags(saveData, RunChoice("CH6_04", stopSeal ? 0 : 1)); // 선택 1
                    ChapterService.ApplyChoiceFlags(saveData, RunChoice("CH6_05", spare ? 0 : 1)); // 선택 2
                    Assert.That(FinaleCatalog.HasSealChoice(saveData), Is.True); // 기록됨
                    Assert.That(FinaleCatalog.HasNemesisChoice(saveData), Is.True); // 기록됨
                    Assert.That(saveData.HasStoryFlag(stopSeal ? FinaleCatalog.SealStoppedFlag : FinaleCatalog.SealKeptFlag), Is.True); // 고른 쪽
                    Assert.That(saveData.HasStoryFlag(stopSeal ? FinaleCatalog.SealKeptFlag : FinaleCatalog.SealStoppedFlag), Is.False); // 안 고른 쪽
                    Assert.That(saveData.HasStoryFlag(spare ? FinaleCatalog.NemesisSparedFlag : FinaleCatalog.NemesisSlainFlag), Is.True); // 고른 쪽
                    Assert.That(FinaleCatalog.GetEndingSeedText(saveData), Is.Not.Empty); // 결말 문구
                }
            }

            List<string> endings = new List<string>(); // 결말 문구 모음

            foreach (bool stopSeal in new[] { true, false }) // 네 조합
            {
                foreach (bool spare in new[] { true, false }) // 네 조합
                {
                    SaveData saveData = CreateStorySave(); // 새 게임
                    saveData.SetStoryFlag(stopSeal ? FinaleCatalog.SealStoppedFlag : FinaleCatalog.SealKeptFlag); // 선택 1
                    saveData.SetStoryFlag(spare ? FinaleCatalog.NemesisSparedFlag : FinaleCatalog.NemesisSlainFlag); // 선택 2
                    endings.Add(FinaleCatalog.GetEndingSeedText(saveData)); // 결말 수집
                }
            }

            Assert.That(endings.Distinct().Count(), Is.EqualTo(4)); // 네 조합이 서로 다른 결말
        }

        [Test] // 최종 던전 : 균열 심층 뒤에 열리고, 가장 어렵고, 보스는 3페이즈다
        public void FinalDungeon_IsHardestAndUnlocksLast() // 최종 던전 테스트
        {
            Assert.That(DungeonSelectionRuntimeState.SupportedDungeonIds.Last(), Is.EqualTo(FinalDungeon)); // 목록 마지막
            Assert.That(DungeonProgressionPolicy.GetPreviousDungeonId(FinalDungeon), Is.EqualTo("DG016")); // 균열 심층 뒤
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 새 게임
            Assert.That(DungeonProgressionPolicy.IsUnlocked(saveData, FinalDungeon), Is.False); // 잠김
            DungeonProgressSaveAdapter.MarkCleared(saveData, "DG016"); // 심층 클리어
            Assert.That(DungeonProgressionPolicy.IsUnlocked(saveData, FinalDungeon), Is.True); // 열림

            DungeonData dungeon = LoadDungeon(FinalDungeon); // 던전 에셋
            Assert.That(dungeon, Is.Not.Null); // 존재
            Assert.That(dungeon.RewardExp, Is.EqualTo(60 + (20 * dungeon.RecommendedLevel))); // 경험치 공식
            Assert.That(dungeon.EncounterWaves.Count, Is.EqualTo(4)); // 4웨이브

            foreach (string dungeonId in DungeonSelectionRuntimeState.SupportedDungeonIds) // 전체 던전
            {
                if (dungeonId == FinalDungeon) continue; // 자기 자신 제외
                Assert.That(DungeonBattleTestProfile.Get(FinalDungeon).HealthMultiplier, Is.GreaterThan(DungeonBattleTestProfile.Get(dungeonId).HealthMultiplier), dungeonId); // 가장 높은 배율
                Assert.That(dungeon.RecommendedLevel, Is.GreaterThan(LoadDungeon(dungeonId).RecommendedLevel), dungeonId); // 가장 높은 레벨
            }

            string bossId = dungeon.EncounterWaves.Last().MonsterIds[0]; // 최종 보스
            MonsterData boss = AssetDatabase.LoadAssetAtPath<MonsterData>($"Assets/ProjectH/Data/Monsters/{bossId}.asset"); // 보스 에셋
            Assert.That(boss, Is.Not.Null, bossId); // 존재
            Assert.That(boss.BossPattern, Is.Not.Null, bossId); // 보스 패턴
            Assert.That(boss.BossPattern.Phases.Count, Is.EqualTo(3), bossId); // 3페이즈
            Assert.That(boss.BossPattern.Patterns.Any(pattern => pattern.MinPhase == 3), Is.True, bossId); // 3페이즈 전용 패턴
            Assert.That(boss.DisplayName, Is.EqualTo(FinaleCatalog.ArchaiName)); // 아르카이

            ProjectHDataCatalog catalog = AssetDatabase.LoadAssetAtPath<ProjectHDataCatalog>("Assets/ProjectH/Data/Database/ProjectHDataCatalog.asset"); // 카탈로그
            Assert.That(catalog.Dungeons.Any(item => item != null && item.Id == FinalDungeon), Is.True); // 던전 등록

            foreach (string monsterId in dungeon.EncounterWaves.SelectMany(wave => wave.MonsterIds).Distinct()) // 등장 몬스터
            {
                Assert.That(catalog.Monsters.Any(item => item != null && item.Id == monsterId), Is.True, monsterId); // 몬스터 등록
            }
        }

        private static int CountFlaggedChoices(string scriptId) // 대사 안에서 플래그를 남기는 선택지 수
        {
            DialogueScript script = DialogueLibrary.Load(scriptId); // 대사 파일
            Assert.That(script, Is.Not.Null, scriptId); // 존재
            return script.Nodes.SelectMany(node => node.Choices).Count(choice => !string.IsNullOrEmpty(choice.Flag)); // 플래그 선택지 수
        }

        private static DialogueRunner RunChoice(string scriptId, int choiceIndex) // 지정 선택지를 고르고 끝까지 본 진행기
        {
            DialogueRunner runner = new DialogueRunner(DialogueLibrary.Load(scriptId)); // 진행기
            runner.SkipToChoiceOrEnd(); // 선택지까지
            Assert.That(runner.IsWaitingForChoice, Is.True, scriptId); // 선택지 대기
            Assert.That(runner.Choose(choiceIndex), Is.True, scriptId); // 선택
            runner.SkipToChoiceOrEnd(); // 끝까지
            Assert.That(runner.IsFinished, Is.True, scriptId); // 종료
            return runner; // 진행기 반환
        }
    }
}
