using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Core; // 설정 · 회차 기록 기능
using ProjectH.Dialogue; // 빨리 넘기기 규칙 기능
using ProjectH.Diary; // 본 이야기 기록 기능
using ProjectH.SaveSystem; // 저장 데이터 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class DialogueSkipTests // Day81 이미 본 이야기 빨리 넘기기 테스트
    {
        [SetUp] // 테스트 준비 표시
        public void SetUp() // 깨끗한 기록에서 시작
        {
            PlayerProfile.ResetRecords(); // 회차 기록 초기화
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 기록과 설정을 되돌려 다른 테스트에 영향 주지 않기
        {
            PlayerProfile.ResetRecords(); // 회차 기록 초기화
            GameSettings.ResetToDefault(); // 설정 기본값 복원
        }

        [Test] // 기본은 이미 본 이야기만 빨리 넘긴다. 처음 보는 이야기는 설정에서 허용했을 때만
        public void FastForward_OnlySeenStoriesByDefault() // 빨리 넘기기 범위 테스트
        {
            Assert.That(DialogueSkipRules.CanFastForward(true, false), Is.True); // 본 이야기
            Assert.That(DialogueSkipRules.CanFastForward(false, false), Is.False); // 처음 보는 이야기
            Assert.That(DialogueSkipRules.CanFastForward(false, true), Is.True); // 설정에서 허용
            Assert.That(DialogueSkipRules.NeedsSkipConfirm(true), Is.False); // 본 이야기는 확인 창 없이 건너뜀
            Assert.That(DialogueSkipRules.NeedsSkipConfirm(false), Is.True); // 처음 보는 이야기는 확인
        }

        [Test] // 누르고 있는 동안 정해진 간격마다 한 줄씩 넘긴다 (프레임이 길어도 한 번에 한 줄)
        public void Tick_AdvancesOneLinePerStep() // 넘김 간격 테스트
        {
            float timer = 0f; // 누적 시간
            float part = DialogueSkipRules.StepSeconds * 0.4f; // 간격의 40%
            Assert.That(DialogueSkipRules.Tick(ref timer, part), Is.False); // 40% — 아직
            Assert.That(DialogueSkipRules.Tick(ref timer, part), Is.False); // 80% — 아직
            Assert.That(DialogueSkipRules.Tick(ref timer, part), Is.True); // 120% — 한 줄
            Assert.That(timer, Is.EqualTo(0f)); // 다음 줄을 위해 처음부터
            Assert.That(DialogueSkipRules.Tick(ref timer, 1f), Is.True); // 긴 프레임도 한 줄
            Assert.That(timer, Is.EqualTo(0f)); // 남은 시간을 쌓아 두지 않는다 (한꺼번에 여러 줄을 넘기지 않게)
            Assert.That(DialogueSkipRules.Tick(ref timer, -1f), Is.False); // 음수 시간은 무시
            Assert.That(timer, Is.EqualTo(0f)); // 그대로
        }

        [Test] // 안내 문구 : 본 이야기 · 설정 허용 · 넘길 수 없음
        public void Hint_TellsWhyFastForwardIsAvailable() // 안내 문구 테스트
        {
            Assert.That(DialogueSkipRules.GetHint(true, false), Does.Contain("이미 본 이야기")); // 본 이야기
            Assert.That(DialogueSkipRules.GetHint(true, true), Does.Contain("이미 본 이야기")); // 본 이야기가 우선
            Assert.That(DialogueSkipRules.GetHint(false, true), Does.Contain("Ctrl").And.Not.Contain("이미 본")); // 설정에서 허용
            Assert.That(DialogueSkipRules.GetHint(false, false), Is.Empty); // 넘길 수 없으면 안내 없음
        }

        [Test] // 끝까지 본 이야기는 회차를 넘어 기억한다 (새 게임에서도 빨리 넘길 수 있다)
        public void Profile_RemembersSeenStoriesAcrossPlaythroughs() // 회차 기록 테스트
        {
            Assert.That(PlayerProfile.HasSeenDialogue("CH1_01"), Is.False); // 처음에는 없음
            Assert.That(PlayerProfile.RecordSeenDialogue("CH1_01"), Is.True); // 처음 기록
            Assert.That(PlayerProfile.RecordSeenDialogue("CH1_01"), Is.False); // 같은 이야기는 새 기록이 아님
            Assert.That(PlayerProfile.HasSeenDialogue("CH1_01"), Is.True); // 기록됨
            Assert.That(PlayerProfile.SeenDialogueCount, Is.EqualTo(1)); // 한 편
            Assert.That(PlayerProfile.RecordSeenDialogue(null), Is.False); // null 안전
            Assert.That(PlayerProfile.RecordSeenDialogue(string.Empty), Is.False); // 빈 ID
            Assert.That(PlayerProfile.RecordSeenDialogue("A|B"), Is.False); // 구분자가 든 ID는 받지 않음
            Assert.That(PlayerProfile.SeenDialogueCount, Is.EqualTo(1)); // 그대로

            SaveData newGame = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 다음 회차의 새 저장
            Assert.That(DialogueSkipRules.IsSeen(newGame, "CH1_01"), Is.True); // 지난 회차에 본 이야기
            Assert.That(DialogueSkipRules.IsSeen(newGame, "CH1_02"), Is.False); // 본 적 없는 이야기
            Assert.That(DialogueSkipRules.IsSeen(null, "CH1_01"), Is.True); // 저장이 없어도 회차 기록으로 판단
            Assert.That(DialogueSkipRules.IsSeen(newGame, null), Is.False); // null 안전

            PlayerProfile.ResetRecords(); // 기록 지우기
            Assert.That(PlayerProfile.HasSeenDialogue("CH1_01"), Is.False); // 지워짐
            Assert.That(PlayerProfile.SeenDialogueCount, Is.EqualTo(0)); // 없음
        }

        [Test] // 이번 회차의 저장에만 기록이 있어도 본 이야기다
        public void Save_RecordAlsoCountsAsSeen() // 저장 기록 테스트
        {
            SaveData save = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 새 게임
            Assert.That(DialogueSkipRules.IsSeen(save, "CH1_02"), Is.False); // 아직 안 봄
            Assert.That(DiaryService.MarkDialogueSeen(save, "CH1_02"), Is.True); // 저장에 기록
            Assert.That(PlayerProfile.HasSeenDialogue("CH1_02"), Is.False); // 회차 기록에는 없음
            Assert.That(DialogueSkipRules.IsSeen(save, "CH1_02"), Is.True); // 그래도 본 이야기
        }

        [Test] // 설정 "처음 보는 대사도 빨리 넘기기"는 꺼진 채로 시작하고, 기본값 복원으로 다시 꺼진다
        public void Setting_SkipUnread_DefaultsOff() // 설정 테스트
        {
            GameSettings.ResetToDefault(); // 기본값
            Assert.That(GameSettings.SkipUnread, Is.False); // 꺼짐
            GameSettings.SetSkipUnread(true); // 켬
            Assert.That(GameSettings.SkipUnread, Is.True); // 켜짐
            Assert.That(DialogueSkipRules.CanFastForward(false, GameSettings.SkipUnread), Is.True); // 처음 보는 이야기도 넘길 수 있음
            GameSettings.ResetToDefault(); // 기본값 복원
            Assert.That(GameSettings.SkipUnread, Is.False); // 다시 꺼짐
        }
    }
}
