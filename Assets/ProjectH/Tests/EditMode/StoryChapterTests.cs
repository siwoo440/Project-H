using System.Collections.Generic; // 목록 자료형
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 던전 클리어 기록 기능
using ProjectH.Diary; // 일기장 목록 기능
using ProjectH.Dialogue; // 대사 파일 기능
using ProjectH.SaveSystem; // 저장·이름 기능
using ProjectH.Story; // 챕터 진행 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class StoryChapterTests // Day68 프롤로그 · 챕터 1 · 2 · 주인공 이름 테스트
    {
        private static SaveData CreateNewStorySave() => SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 새 게임 (세레나 1인으로 시작)

        private static SaveData CreateLegacySave() // 68일차 이전 진행 저장 (4인 보유 · 숲 클리어)
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA", "CH_ELLEN", "CH_LILIA", "CH_EVE" }); // 기존 4인
            DungeonProgressSaveAdapter.MarkCleared(saveData, "DG001"); // 진행 기록
            return saveData; // 저장 반환
        }

        private static void PlayPendingDialogue(SaveData saveData) // 지금 재생할 이야기를 끝까지 본 것으로 처리
        {
            string scriptId = ChapterService.GetPendingDialogueId(saveData); // 현재 이야기
            Assert.That(scriptId, Is.Not.Empty, "재생할 이야기가 없습니다."); // 존재 확인
            ChapterService.CompleteDialogue(saveData, scriptId); // 완료 처리
        }

        [Test] // 주인공 이름 : 정리 규칙 · 저장 · 대사 치환
        public void HeroName_SanitizesAndReplacesInText() // 이름 테스트
        {
            SaveData saveData = CreateNewStorySave(); // 새 게임
            Assert.That(HeroNameService.NeedsInput(saveData), Is.True); // 처음엔 이름 없음
            Assert.That(HeroNameService.Get(saveData), Is.EqualTo(HeroNameService.DefaultName)); // 기본 이름
            Assert.That(HeroNameService.Sanitize("   "), Is.EqualTo(HeroNameService.DefaultName)); // 비우면 기본 이름
            Assert.That(HeroNameService.Sanitize("아주아주아주아주긴이름"), Has.Length.EqualTo(HeroNameService.MaxLength)); // 길이 제한
            HeroNameService.TrySetName(saveData, "  아리안  "); // 이름 저장
            Assert.That(saveData.HeroName, Is.EqualTo("아리안")); // 앞뒤 공백 제거
            Assert.That(HeroNameService.NeedsInput(saveData), Is.False); // 입력 완료
            Assert.That(HeroNameService.Apply("{HERO}님, 부탁드려요.", saveData), Is.EqualTo("아리안님, 부탁드려요.")); // 대사 치환
            Assert.That(HeroNameService.Apply("이름이 없는 문장", saveData), Is.EqualTo("이름이 없는 문장")); // 그대로
            SaveData loaded = UnityEngine.JsonUtility.FromJson<SaveData>(UnityEngine.JsonUtility.ToJson(saveData)); // 저장 왕복
            Assert.That(loaded.HeroName, Is.EqualTo("아리안")); // 유지
        }

        [Test] // 새 게임은 프롤로그부터 · 이야기를 보면 다음 단계로
        public void NewGame_StartsPrologue_AndAdvances() // 프롤로그 테스트
        {
            SaveData saveData = CreateNewStorySave(); // 새 게임
            ChapterService.EnsureStarted(saveData); // 시작
            Assert.That(saveData.ChapterProgress.ChapterId, Is.EqualTo(ChapterCatalog.PrologueId)); // 프롤로그
            Assert.That(ChapterService.GetPendingDialogueId(saveData), Is.EqualTo("PROLOGUE_01")); // 첫 이야기
            Assert.That(saveData.CurrentChapter, Does.Contain("프롤로그")); // 로비 표시
            Assert.That(saveData.CurrentMainQuest, Is.Not.Empty); // 다음 할 일
            ChapterService.CompleteDialogue(saveData, "PROLOGUE_02"); // 순서가 아닌 이야기
            Assert.That(ChapterService.GetPendingDialogueId(saveData), Is.EqualTo("PROLOGUE_01")); // 진행 없음
            PlayPendingDialogue(saveData); // 1편
            Assert.That(ChapterService.GetPendingDialogueId(saveData), Is.EqualTo("PROLOGUE_02")); // 2편
            PlayPendingDialogue(saveData); // 2편
            PlayPendingDialogue(saveData); // 3편
            Assert.That(saveData.ChapterProgress.ChapterId, Is.EqualTo("CHAPTER_01")); // 챕터 1 시작
            Assert.That(ChapterService.GetPendingDialogueId(saveData), Is.EqualTo("CH1_01")); // 첫 이야기
        }

        [Test] // 챕터 1·2 : 던전 클리어로 진행되고 이야기에서 동료가 합류
        public void Chapters_AdvanceWithDungeonClears_AndRecruitCompanions() // 챕터 진행 테스트
        {
            SaveData saveData = CreateNewStorySave(); // 새 게임
            ChapterService.EnsureStarted(saveData); // 시작
            PlayPendingDialogue(saveData); // 프롤로그 1
            PlayPendingDialogue(saveData); // 프롤로그 2
            PlayPendingDialogue(saveData); // 프롤로그 3
            PlayPendingDialogue(saveData); // CH1_01
            Assert.That(ChapterService.GetPendingDialogueId(saveData), Is.EqualTo("CH1_02")); // 길드 용병 고용
            Assert.That(saveData.HasCharacter("CH_LUCIA"), Is.False); // 아직 고용 전
            PlayPendingDialogue(saveData); // CH1_02
            Assert.That(saveData.HasCharacter("CH_LUCIA"), Is.True); // 루시아 합류 (초반 2인 파티)
            Assert.That(saveData.Characters.Count, Is.EqualTo(2)); // 세레나 + 루시아
            Assert.That(ChapterService.GetPendingDialogueId(saveData), Is.Empty); // 다음은 던전 단계
            Assert.That(ChapterService.GetCurrentStep(saveData).Target, Is.EqualTo("DG001")); // 숲
            ChapterService.NotifyDungeonCleared(saveData, "DG002"); // 다른 던전
            Assert.That(ChapterService.GetCurrentStep(saveData).Target, Is.EqualTo("DG001")); // 진행 없음
            ChapterService.NotifyDungeonCleared(saveData, "DG001"); // 숲 클리어
            Assert.That(ChapterService.GetPendingDialogueId(saveData), Is.EqualTo("CH1_03")); // 엘렌 이야기
            Assert.That(saveData.HasCharacter("CH_ELLEN"), Is.False); // 아직 합류 전
            PlayPendingDialogue(saveData); // CH1_03
            Assert.That(saveData.HasCharacter("CH_ELLEN"), Is.True); // 엘렌 합류
            ChapterService.NotifyDungeonCleared(saveData, "DG002"); // 폐허 클리어
            PlayPendingDialogue(saveData); // CH1_04
            Assert.That(saveData.ChapterProgress.ChapterId, Is.EqualTo("CHAPTER_02")); // 챕터 2
            PlayPendingDialogue(saveData); // CH2_01
            Assert.That(saveData.HasCharacter("CH_LILIA"), Is.True); // 릴리아 합류
            PlayPendingDialogue(saveData); // CH2_02
            Assert.That(saveData.HasCharacter("CH_EVE"), Is.True); // 이브 합류
            Assert.That(saveData.Characters.Count, Is.EqualTo(5)); // 세레나 · 루시아 · 엘렌 · 릴리아 · 이브
            ChapterService.NotifyDungeonCleared(saveData, "DG003"); // 회랑 클리어
            PlayPendingDialogue(saveData); // CH2_03
            Assert.That(saveData.ChapterProgress.ChapterId, Is.EqualTo("CHAPTER_03")); // 챕터 3으로 이어짐 (Day69)
            Assert.That(ChapterService.GetPendingDialogueId(saveData), Is.EqualTo("CH3_01")); // 다음 이야기
        }

        [Test] // 이미 깬 던전 단계는 화면을 열 때 자동으로 통과
        public void Refresh_SkipsAlreadyClearedDungeonSteps() // 자동 통과 테스트
        {
            SaveData saveData = CreateNewStorySave(); // 새 게임
            ChapterService.EnsureStarted(saveData); // 시작
            for (int index = 0; index < 5; index++) PlayPendingDialogue(saveData); // 프롤로그 3편 + CH1_01 + CH1_02(용병 고용)
            DungeonProgressSaveAdapter.MarkCleared(saveData, "DG001"); // 다른 경로로 먼저 클리어
            ChapterService.Refresh(saveData); // 화면 열기
            Assert.That(ChapterService.GetPendingDialogueId(saveData), Is.EqualTo("CH1_03")); // 자동 통과 후 다음 이야기
        }

        [Test] // 기존 세이브는 스토리를 건너뛰고 진행·동료를 그대로 유지
        public void LegacySave_SkipsStory() // 기존 세이브 테스트
        {
            SaveData saveData = CreateLegacySave(); // 기존 진행 저장
            ChapterService.EnsureStarted(saveData); // 시작 판정
            Assert.That(saveData.ChapterProgress.IsFinished, Is.True); // 스토리 완료 처리
            Assert.That(ChapterService.GetPendingDialogueId(saveData), Is.Empty); // 이야기 재생 없음
            Assert.That(saveData.Characters.Count, Is.EqualTo(4)); // 동료 유지
            Assert.That(ChapterService.IsLegacySave(CreateNewStorySave()), Is.False); // 새 게임은 기존 세이브가 아님
        }

        [Test] // 메인 스토리 대사 9편 : 파일 존재 · 구조 검사 · 모든 선택 경로 종료 · 이름 자리 사용
        public void ChapterScripts_Validate() // 대사 테스트
        {
            List<string> scriptIds = ChapterCatalog.GetDialogueScriptIds(); // 대사 목록
            Assert.That(scriptIds.Count, Is.EqualTo(26)); // 프롤로그 3 + 챕터 1 4 + 챕터 2 3 + 챕터 3~5 16편 (Day69)
            bool usesHeroName = false; // {HERO} 사용 여부

            foreach (string scriptId in scriptIds) // 대사 순회
            {
                DialogueScript script = DialogueLibrary.Load(scriptId); // 불러오기
                Assert.That(script, Is.Not.Null, scriptId); // 파일 존재
                List<string> errors = new List<string>(); // 오류 목록
                Assert.That(DialogueLibrary.Validate(script, errors), Is.True, string.Join("\n", errors)); // 구조 검사

                foreach (DialogueNode node in script.Nodes) // 노드 순회
                {
                    if (!string.IsNullOrEmpty(node.Text) && node.Text.Contains(HeroNameService.Placeholder)) usesHeroName = true; // 이름 자리 확인
                }

                for (int choice = 0; choice < 2; choice++) // 두 선택 경로
                {
                    DialogueRunner runner = new DialogueRunner(script); // 진행기
                    int guard = 0; // 무한 반복 방지

                    while (!runner.IsFinished && guard++ < 200) // 끝까지
                    {
                        if (runner.IsWaitingForChoice) runner.Choose(System.Math.Min(choice, runner.Current.Choices.Count - 1)); // 선택
                        else runner.Advance(); // 넘기기
                    }

                    Assert.That(runner.IsFinished, Is.True, $"{scriptId} {choice}번 경로"); // 종료
                }
            }

            Assert.That(usesHeroName, Is.True); // 이름이 들어가는 대사가 있음
        }

        [Test] // 일기장 : 메인 스토리 분류로 다시 볼 수 있음
        public void Diary_ContainsMainStory() // 일기장 테스트
        {
            int main = 0; // 메인 스토리 수

            foreach (DiaryScenarioEntry entry in DiaryCatalog.Scenarios) // 시나리오 순회
            {
                if (entry.Category == DiaryScenarioCategory.Main) main++; // 세기
            }

            Assert.That(main, Is.EqualTo(26)); // 26편 (Day69 챕터 3~5 포함)
            Assert.That(DiaryCatalog.GetCategoryLabel(DiaryScenarioCategory.Main), Is.EqualTo("메인 스토리")); // 분류 이름
        }
    }
}
