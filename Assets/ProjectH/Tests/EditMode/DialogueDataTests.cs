using System.Collections.Generic; // 목록 자료형
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Dialogue; // 대화 불러오기·검사 기능
using ProjectH.SaveSystem; // 개인 이벤트 목록·시간대 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class DialogueDataTests // Day58 실제 대화 파일 점검 (대사를 고칠 때마다 끊긴 분기를 자동 검사)
    {
        private static readonly string[] Starters = { "CH_SERENA", "CH_ELLEN", "CH_LILIA", "CH_EVE" }; // 초기 4인

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 캐시 비우기 (파일 수정 반영)
        {
            DialogueLibrary.ClearCache(); // 대화 캐시 초기화
        }

        [Test] // 개인 이벤트 1화 파일 검사
        public void EpisodeOneScripts_LoadValidateAndMatchCatalog() // 1화 파일 테스트
        {
            int checkedCount = 0; // 검사한 1화 수

            foreach (CharacterEventDefinition definition in CharacterEventCatalog.All) // 개인 이벤트 순회
            {
                if (definition.Episode != 1) // 1화만 확인 (2화는 다음 일차)
                {
                    continue; // 다음 이벤트
                }

                DialogueScript script = DialogueLibrary.Load(definition.ScriptId); // 대화 파일 불러오기
                List<string> errors = new List<string>(); // 오류 목록
                Assert.That(script, Is.Not.Null, definition.ScriptId); // 파일 존재 검증
                Assert.That(DialogueLibrary.Validate(script, errors), Is.True, string.Join("\n", errors)); // 구조 검사 검증
                Assert.That(script.CharacterId, Is.EqualTo(definition.CharacterId), definition.ScriptId); // 스탠딩 캐릭터 일치 검증
                Assert.That(script.Title, Is.EqualTo(definition.Title), definition.ScriptId); // 제목 일치 검증
                Assert.That(HasPreferredChoice(script), Is.True, $"{definition.ScriptId} 선호 선택지 없음"); // 선호 선택지 존재 검증
                checkedCount++; // 검사 수 증가
            }

            Assert.That(checkedCount, Is.EqualTo(12)); // 12인 1화 검증 (Day70 — 합류 8인 추가)
        }

        [Test] // 모든 선택 경로가 끝까지 도달하는지 검증
        public void EpisodeOneScripts_EveryChoicePathFinishes() // 경로 테스트
        {
            foreach (string characterId in Starters) // 초기 4인 순회
            {
                DialogueScript script = DialogueLibrary.Load(CharacterEventCatalog.GetForCharacter(characterId)[0].ScriptId); // 1화 불러오기

                for (int choice = 0; choice < 2; choice++) // 첫째·둘째 선택지 경로
                {
                    DialogueRunner runner = new DialogueRunner(script); // 진행기 생성
                    int guard = 0; // 무한 반복 방지

                    while (!runner.IsFinished && guard++ < 200) // 끝까지 진행
                    {
                        if (runner.IsWaitingForChoice) runner.Choose(System.Math.Min(choice, runner.Current.Choices.Count - 1)); // 선택
                        else runner.Advance(); // 넘기기
                    }

                    Assert.That(runner.IsFinished, Is.True, $"{script.Id} {choice}번 경로가 끝나지 않음"); // 종료 검증
                }
            }
        }

        [Test] // 일상 대화 16개 파일 검사
        public void DailyTalks_ExistForEveryStarterAndPhase() // 일상 대화 파일 테스트
        {
            SaveTimeOfDay[] phases = { SaveTimeOfDay.Morning, SaveTimeOfDay.Day, SaveTimeOfDay.Evening, SaveTimeOfDay.Night }; // 4시간대

            foreach (string characterId in Starters) // 초기 4인 순회
            {
                foreach (SaveTimeOfDay phase in phases) // 시간대 순회
                {
                    string scriptId = DialogueService.GetTalkScriptId(characterId, phase); // 파일 ID
                    DialogueScript script = DialogueLibrary.Load(scriptId); // 불러오기
                    List<string> errors = new List<string>(); // 오류 목록
                    Assert.That(script, Is.Not.Null, scriptId); // 파일 존재 검증
                    Assert.That(DialogueLibrary.Validate(script, errors), Is.True, string.Join("\n", errors)); // 구조 검사 검증
                    Assert.That(script.CharacterId, Is.EqualTo(characterId), scriptId); // 캐릭터 일치 검증
                }
            }
        }

        [Test] // 결속 대사 20편 파일 검사 (Day59 추가)
        public void BondDialogues_ExistForEveryStarterAndStage() // 결속 대사 파일 테스트
        {
            foreach (string characterId in Starters) // 초기 4인 순회
            {
                for (int level = 1; level <= BondCatalog.MaxLevel; level++) // 1~5단계
                {
                    string scriptId = BondCatalog.GetBondScriptId(characterId, level); // 파일 ID
                    DialogueScript script = DialogueLibrary.Load(scriptId); // 불러오기
                    List<string> errors = new List<string>(); // 오류 목록
                    Assert.That(script, Is.Not.Null, scriptId); // 파일 존재 검증
                    Assert.That(DialogueLibrary.Validate(script, errors), Is.True, string.Join("\n", errors)); // 구조 검사 검증
                    Assert.That(script.CharacterId, Is.EqualTo(characterId), scriptId); // 캐릭터 일치 검증
                }
            }
        }

        private static bool HasPreferredChoice(DialogueScript script) // 선호 선택지 포함 여부
        {
            foreach (DialogueNode node in script.Nodes) // 노드 순회
            {
                foreach (DialogueChoice choice in node.Choices) // 선택지 순회
                {
                    if (choice.Preferred) return true; // 선호 선택지 발견
                }
            }

            return false; // 없음 반환
        }
    }
}
