using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Dialogue; // 대화 진행·결과 반영 기능
using ProjectH.SaveSystem; // 저장·호감도 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class DialogueServiceTests // Day58 개인 이벤트·일상 대화 저장 연결 테스트
    {
        private const string Serena = "CH_SERENA"; // 테스트 캐릭터
        private const string ChoiceJson = // 선호 선택지 1개가 있는 짧은 대화
            "{'id':'T_EVENT','characterId':'CH_SERENA','nodes':[" +
            "{'id':'c1','speaker':'CH_SERENA','text':'고르세요','choices':[{'text':'선호','affinity':3,'preferred':true},{'text':'보통','affinity':1}]}," +
            "{'id':'e1','speaker':'NARRATION','text':'끝'}]}";

        private static SaveData CreateSave(int affinity) // 지정 호감도 새 게임 데이터
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { Serena }); // 새 게임 생성
            AffinityService.AddAffinity(saveData, Serena, affinity); // 호감도 설정
            return saveData; // 데이터 반환
        }

        private static DialogueRunner FinishedRunner(int choiceIndex) // 선택 후 끝까지 본 진행기
        {
            DialogueRunner runner = new DialogueRunner(DialogueLibrary.Parse(ChoiceJson.Replace('\'', '"'))); // 진행기 생성
            runner.Choose(choiceIndex); // 선택
            runner.SkipToChoiceOrEnd(); // 끝까지
            Assert.That(runner.IsFinished, Is.True); // 종료 확인
            return runner; // 진행기 반환
        }

        private static CharacterEventDefinition SerenaEpisodeOne => CharacterEventCatalog.Find("EVT_SERENA_01"); // 세레나 1화 (조건: 호감 이상)

        [Test] // 이벤트 상태 전환 검증
        public void EventState_LockedThenAvailableThenCompleted() // 상태 테스트
        {
            SaveData saveData = CreateSave(39); // 호감 직전

            Assert.That(DialogueService.GetEventState(saveData, SerenaEpisodeOne), Is.EqualTo(CharacterEventState.Locked)); // 잠김 검증
            AffinityService.AddAffinity(saveData, Serena, 1); // 호감 40 도달
            Assert.That(DialogueService.GetEventState(saveData, SerenaEpisodeOne), Is.EqualTo(CharacterEventState.Available)); // 볼 수 있음 검증
            DialogueService.CompleteEvent(saveData, SerenaEpisodeOne, FinishedRunner(0)); // 완료
            Assert.That(DialogueService.GetEventState(saveData, SerenaEpisodeOne), Is.EqualTo(CharacterEventState.Completed)); // 완료 검증
        }

        [Test] // 첫 완료 보상 검증
        public void CompleteEvent_FirstTime_GrantsBasePlusChoiceAffinity() // 첫 완료 테스트
        {
            SaveData saveData = CreateSave(40); // 호감 도달

            DialogueRewardResult result = DialogueService.CompleteEvent(saveData, SerenaEpisodeOne, FinishedRunner(0)); // 선호 선택으로 완료

            Assert.That(result.Applied, Is.True, result.Message); // 반영 검증
            Assert.That(result.AffinityGain, Is.EqualTo(DialogueService.PersonalEventAffinity + 3)); // +10 +3 검증
            Assert.That(AffinityService.GetAffinity(saveData, Serena), Is.EqualTo(53)); // 53 검증
            Assert.That(saveData.HasStoryFlag("SYS_EVENT_DONE:EVT_SERENA_01"), Is.True); // 완료 플래그 검증
        }

        [Test] // 다시보기 보상 없음 검증
        public void CompleteEvent_Replay_GrantsNothing() // 다시보기 테스트
        {
            SaveData saveData = CreateSave(40); // 호감 도달
            DialogueService.CompleteEvent(saveData, SerenaEpisodeOne, FinishedRunner(0)); // 첫 완료
            int affinity = AffinityService.GetAffinity(saveData, Serena); // 첫 완료 후 호감도

            DialogueRewardResult replay = DialogueService.CompleteEvent(saveData, SerenaEpisodeOne, FinishedRunner(0)); // 다시보기

            Assert.That(replay.Applied, Is.False); // 미반영 검증
            Assert.That(replay.Message, Does.Contain("다시보기")); // 안내 문구 검증
            Assert.That(AffinityService.GetAffinity(saveData, Serena), Is.EqualTo(affinity)); // 호감도 유지 검증
        }

        [Test] // 중간 종료·잠김 미반영 검증
        public void CompleteEvent_UnfinishedOrLocked_ChangesNothing() // 미반영 테스트
        {
            SaveData saveData = CreateSave(40); // 호감 도달
            DialogueRunner unfinished = new DialogueRunner(DialogueLibrary.Parse(ChoiceJson.Replace('\'', '"'))); // 선택 전 진행기

            Assert.That(DialogueService.CompleteEvent(saveData, SerenaEpisodeOne, unfinished).Applied, Is.False); // 중간 종료 미반영 검증
            Assert.That(DialogueService.IsEventCompleted(saveData, "EVT_SERENA_01"), Is.False); // 완료 기록 없음 검증

            SaveData locked = CreateSave(10); // 잠김 호감도
            Assert.That(DialogueService.CompleteEvent(locked, SerenaEpisodeOne, FinishedRunner(0)).Applied, Is.False); // 잠김 미반영 검증
            Assert.That(AffinityService.GetAffinity(locked, Serena), Is.EqualTo(10)); // 호감도 유지 검증
        }

        [Test] // 단계 도달 안내 검증
        public void CompleteEvent_CrossingTier_ReportsNewTier() // 단계 도달 테스트
        {
            SaveData saveData = CreateSave(50); // 호감 50

            DialogueRewardResult result = DialogueService.CompleteEvent(saveData, SerenaEpisodeOne, FinishedRunner(0)); // 50 → 63

            Assert.That(result.NewTiers, Is.EquivalentTo(new[] { AffinityTier.Trusted })); // 신뢰 도달 검증
            Assert.That(result.Message, Does.Contain("호감도 보상")); // Day56 보상 안내 검증
        }

        [Test] // 일상 대화 시간대당 1회 검증
        public void DailyTalk_OncePerTimeSlot() // 일상 대화 제한 테스트
        {
            SaveData saveData = CreateSave(0); // 새 게임 (1일차 아침)
            DialogueRunner talk = FinishedRunner(1); // 끝까지 본 대화 (보통 선택 +1)

            Assert.That(DialogueService.CanTalk(saveData, Serena, out _), Is.True); // 첫 대화 가능 검증
            Assert.That(DialogueService.CompleteTalk(saveData, Serena, talk).AffinityGain, Is.EqualTo(DialogueService.DailyTalkAffinity + 1)); // +2 +1 검증
            Assert.That(DialogueService.CanTalk(saveData, Serena, out string reason), Is.False); // 같은 시간대 불가 검증
            Assert.That(reason, Does.Contain("이미")); // 안내 문구 검증
            Assert.That(DialogueService.CompleteTalk(saveData, Serena, talk).Applied, Is.False); // 중복 반영 차단 검증
            GameTimeService.AdvanceTime(saveData); // 낮으로 진행
            Assert.That(DialogueService.CanTalk(saveData, Serena, out _), Is.True); // 다음 시간대 가능 검증
        }

        [Test] // 대화 파일 ID 규칙 검증
        public void TalkScriptId_UsesCharacterAndPhase() // 파일 ID 테스트
        {
            Assert.That(DialogueService.GetTalkScriptId("CH_SERENA", SaveTimeOfDay.Morning), Is.EqualTo("TALK_SERENA_MORNING")); // 아침 ID 검증
            Assert.That(DialogueService.GetTalkScriptId("CH_EVE", SaveTimeOfDay.Night), Is.EqualTo("TALK_EVE_NIGHT")); // 밤 ID 검증
        }

        [Test] // 이전 저장 호환 검증
        public void OldSave_WithoutTalkSlot_CanTalk() // 저장 호환 테스트
        {
            SaveData saveData = UnityEngine.JsonUtility.FromJson<SaveData>("{\"characters\":[{\"characterId\":\"CH_SERENA\",\"affinity\":20}]}"); // Day58 이전 저장
            saveData.EnsureDefaults(); // 기본값 보정

            Assert.That(saveData.FindCharacter(Serena).LastTalkSlot, Is.EqualTo(0)); // 기록 없음 검증
            Assert.That(DialogueService.CanTalk(saveData, Serena, out _), Is.True); // 대화 가능 검증
        }
    }
}
