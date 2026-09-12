using System.Collections.Generic; // 목록 자료형
using System.Linq; // 목록 검색 기능
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 던전 클리어 기록 기능
using ProjectH.Diary; // 세계관 단어 기능
using ProjectH.SaveSystem; // 저장 기능
using ProjectH.Story; // 챕터 진행 기능
using ProjectH.Village; // 길드 합류 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class StoryLateChapterTests // Day69 챕터 3 · 4 · 5 (합류 7인 · 봉인의 진실 · 네메시스) 테스트
    {
        private static SaveData CreateStorySave() // 프롤로그부터 시작하는 새 게임
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 세레나 1인
            ChapterService.EnsureStarted(saveData); // 스토리 시작
            return saveData; // 저장 반환
        }

        private static void PlayThroughChapters(SaveData saveData, int guardLimit = 64) // 자동으로 진행 가능한 모든 단계 진행
        {
            int guard = 0; // 무한 반복 방지

            while (guard++ < guardLimit) // 단계 순회
            {
                ChapterStep step = ChapterService.GetCurrentStep(saveData); // 현재 단계
                if (step == null) break; // 준비된 이야기 끝

                if (step.Kind == ChapterStepKind.Dialogue) ChapterService.CompleteDialogue(saveData, step.Target); // 이야기 보기
                else // 던전 단계
                {
                    DungeonProgressSaveAdapter.MarkCleared(saveData, step.Target); // 클리어 기록
                    ChapterService.NotifyDungeonCleared(saveData, step.Target); // 진행
                }
            }
        }

        [Test] // 챕터 3~5 : 단계 구성과 던전 연결
        public void LateChapters_HaveStepsAndDungeons() // 챕터 구성 테스트
        {
            Assert.That(ChapterCatalog.All.Count, Is.EqualTo(6)); // 프롤로그 + 챕터 1~5
            string[] lateChapters = { "CHAPTER_03", "CHAPTER_04", "CHAPTER_05" }; // 이번 챕터

            foreach (string chapterId in lateChapters) // 챕터 순회
            {
                ChapterDefinition chapter = ChapterCatalog.Find(chapterId); // 챕터
                Assert.That(chapter, Is.Not.Null, chapterId); // 존재
                int dungeons = chapter.Steps.Count(step => step.Kind == ChapterStepKind.ClearDungeon); // 던전 단계 수
                Assert.That(dungeons, Is.EqualTo(2), chapterId); // 챕터마다 던전 2개
                Assert.That(chapter.Steps.Count(step => step.Kind == ChapterStepKind.Dialogue), Is.GreaterThanOrEqualTo(4), chapterId); // 이야기 4편 이상

                foreach (ChapterStep step in chapter.Steps) // 단계 순회
                {
                    Assert.That(step.Summary, Is.Not.Empty, $"{chapterId} {step.Target}"); // 다음 할 일 문구
                }
            }
        }

        [Test] // 끝까지 진행하면 12인이 모이고 봉인의 진실·네메시스가 열린다
        public void FullStory_RecruitsEveryoneAndUnlocksLore() // 전체 진행 테스트
        {
            SaveData saveData = CreateStorySave(); // 새 게임
            PlayThroughChapters(saveData); // 끝까지 진행
            Assert.That(saveData.ChapterProgress.IsFinished, Is.True); // 준비된 이야기 끝
            Assert.That(saveData.ChapterProgress.FinishedChapterId, Is.EqualTo("CHAPTER_05")); // 마지막 챕터 기록
            Assert.That(saveData.Characters.Count, Is.EqualTo(12)); // 12인 완성

            foreach (string characterId in new[] { "CH_CLAIRE", "CH_MERCIA", "CH_PYRA", "CH_TYRIA", "CH_NOEL", "CH_NATASHA", "CH_SEPHIRA" }) // 챕터 3~5 합류
            {
                Assert.That(saveData.HasCharacter(characterId), Is.True, characterId); // 합류 확인
            }

            Assert.That(saveData.HasStoryFlag(WorldGlossaryCatalog.SealTruthFlag), Is.True); // 봉인의 진실
            Assert.That(saveData.HasStoryFlag(ChapterCatalog.NemesisFlag), Is.True); // 네메시스
            GlossaryEntry seal = WorldGlossaryCatalog.All.First(entry => entry.Term == "봉인의 진실"); // 단어
            GlossaryEntry nemesis = WorldGlossaryCatalog.All.First(entry => entry.Term == "네메시스"); // 단어
            Assert.That(seal.IsUnlocked(saveData), Is.True); // 열림
            Assert.That(nemesis.IsUnlocked(saveData), Is.True); // 열림
            Assert.That(nemesis.IsUnlocked(CreateStorySave()), Is.False); // 새 게임에서는 잠김
            Assert.That(ChapterService.GetProgressText(saveData), Does.Contain("71일차")); // 이후 안내
        }

        [Test] // 챕터 5 마지막 이야기 전에는 네메시스가 잠겨 있다
        public void NemesisFlag_UnlocksOnlyAtFinalChapterScene() // 해금 시점 테스트
        {
            SaveData saveData = CreateStorySave(); // 새 게임
            int guard = 0; // 무한 반복 방지

            while (guard++ < 64) // CH5_06 직전까지 진행
            {
                ChapterStep step = ChapterService.GetCurrentStep(saveData); // 현재 단계
                if (step == null || step.Target == "CH5_06") break; // 마지막 이야기 앞에서 멈춤
                if (step.Kind == ChapterStepKind.Dialogue) ChapterService.CompleteDialogue(saveData, step.Target); // 이야기
                else { DungeonProgressSaveAdapter.MarkCleared(saveData, step.Target); ChapterService.NotifyDungeonCleared(saveData, step.Target); } // 던전
            }

            Assert.That(ChapterService.GetPendingDialogueId(saveData), Is.EqualTo("CH5_06")); // 마지막 이야기 대기
            Assert.That(saveData.HasStoryFlag(WorldGlossaryCatalog.SealTruthFlag), Is.True); // 봉인의 진실은 이미 열림
            Assert.That(saveData.HasStoryFlag(ChapterCatalog.NemesisFlag), Is.False); // 네메시스는 아직
            ChapterService.CompleteDialogue(saveData, "CH5_06"); // 마지막 이야기
            Assert.That(saveData.HasStoryFlag(ChapterCatalog.NemesisFlag), Is.True); // 이제 열림
        }

        [Test] // 업데이트로 새 챕터가 생기면 이야기를 다 본 저장도 이어서 진행
        public void FinishedSave_ContinuesWhenNewChapterArrives() // 이어 붙이기 테스트
        {
            SaveData saveData = CreateStorySave(); // 새 게임
            saveData.ChapterProgress.Finish("CHAPTER_02"); // 68일차까지 본 상태로 가정
            ChapterService.Refresh(saveData); // 화면 열기
            Assert.That(saveData.ChapterProgress.ChapterId, Is.EqualTo("CHAPTER_03")); // 챕터 3부터 이어짐
            Assert.That(ChapterService.GetPendingDialogueId(saveData), Is.EqualTo("CH3_01")); // 첫 이야기

            SaveData legacy = SaveData.CreateNewGame(new[] { "CH_SERENA", "CH_ELLEN" }); // 68일차 이전 저장
            ChapterService.EnsureStarted(legacy); // 스토리 건너뜀
            ChapterService.Refresh(legacy); // 화면 열기
            Assert.That(legacy.ChapterProgress.IsFinished, Is.True); // 그대로 완료 상태
            Assert.That(ChapterService.GetPendingDialogueId(legacy), Is.Empty); // 이야기 재생 없음
        }

        [Test] // 메인 스토리 진행 중에는 길드 합류 소식이 뜨지 않는다 (이야기에서 합류)
        public void GuildRecruits_WaitUntilStoryFinished() // 길드 합류 시점 테스트
        {
            SaveData saveData = CreateStorySave(); // 새 게임 (스토리 진행 중)
            DungeonProgressSaveAdapter.MarkCleared(saveData, "DG001"); // 클레어·메르시아 조건 충족
            Assert.That(RecruitService.GetPending(saveData), Is.Empty); // 이야기 중에는 소식 없음

            SaveData legacy = SaveData.CreateNewGame(new[] { "CH_SERENA", "CH_ELLEN", "CH_LILIA", "CH_EVE" }); // 기존 저장
            ChapterService.EnsureStarted(legacy); // 스토리 건너뜀 (완료 상태)
            DungeonProgressSaveAdapter.MarkCleared(legacy, "DG001"); // 조건 충족
            Assert.That(RecruitService.GetPending(legacy).Count, Is.EqualTo(2), "기존 세이브는 길드에서 합류"); // 클레어·메르시아
        }
    }
}
