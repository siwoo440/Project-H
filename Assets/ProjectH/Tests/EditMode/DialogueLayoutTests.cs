using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Dialogue; // 대화 배치·검사 기능
using ProjectH.UI; // 이름표·대화 설정 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class DialogueLayoutTests // Day62 대화 UI 개편 : 2인 좌우 배치 · 이름/소속 · 속도 설정 테스트
    {
        private static DialogueScript Parse(string json) // JSON 해석
        {
            DialogueScript script = DialogueLibrary.Parse(json); // 해석
            Assert.That(script, Is.Not.Null); // 해석 성공
            return script; // 반환
        }

        [Test] // 1인 대화는 가운데
        public void SingleCharacter_StandsInCenter() // 1인 테스트
        {
            DialogueStageLayout layout = DialogueStageLayout.Build(Parse("{\"characterId\":\"CH_SERENA\",\"nodes\":[{\"speaker\":\"CH_SERENA\",\"text\":\"a\"},{\"speaker\":\"HERO\",\"text\":\"b\"}]}")); // 세레나 + 주인공

            Assert.That(layout.Center, Is.EqualTo("CH_SERENA")); // 가운데
            Assert.That(layout.Left, Is.Empty); // 왼쪽 없음
            Assert.That(layout.GetSlot("CH_SERENA"), Is.EqualTo(DialogueStageSlot.Center)); // 자리
            Assert.That(layout.GetSlot("HERO"), Is.EqualTo(DialogueStageSlot.None)); // 주인공은 스탠딩 없음
        }

        [Test] // 2인 대화는 등장 순서대로 왼쪽·오른쪽 (기존 파일 수정 없이 자동)
        public void TwoCharacters_AutoLeftAndRight() // 2인 테스트
        {
            DialogueStageLayout layout = DialogueStageLayout.Build(Parse("{\"characterId\":\"CH_SERENA\",\"nodes\":[{\"speaker\":\"NARRATION\",\"text\":\"a\"},{\"speaker\":\"CH_LILIA\",\"text\":\"b\"},{\"speaker\":\"CH_SERENA\",\"text\":\"c\"},{\"speaker\":\"CH_EVE\",\"text\":\"d\"}]}")); // 세레나(주인공 캐릭터) · 릴리아 · 이브

            Assert.That(layout.Left, Is.EqualTo("CH_SERENA")); // 대화 주인공 캐릭터 먼저 왼쪽
            Assert.That(layout.Right, Is.EqualTo("CH_LILIA")); // 두 번째 등장 오른쪽
            Assert.That(layout.Center, Is.Empty); // 가운데 없음
            Assert.That(layout.GetSlot("CH_EVE"), Is.EqualTo(DialogueStageSlot.None)); // 세 번째는 이름표만
        }

        [Test] // leftCharacterId·rightCharacterId 직접 지정이 우선
        public void ExplicitSides_OverrideAutoLayout() // 직접 지정 테스트
        {
            DialogueStageLayout layout = DialogueStageLayout.Build(Parse("{\"characterId\":\"CH_SERENA\",\"leftCharacterId\":\"CH_EVE\",\"rightCharacterId\":\"CH_SERENA\",\"nodes\":[{\"speaker\":\"CH_SERENA\",\"text\":\"a\"}]}")); // 지정 배치

            Assert.That(layout.GetSlot("CH_EVE"), Is.EqualTo(DialogueStageSlot.Left)); // 지정 왼쪽
            Assert.That(layout.GetSlot("CH_SERENA"), Is.EqualTo(DialogueStageSlot.Right)); // 지정 오른쪽
        }

        [Test] // 주인공·나레이션만 있으면 스탠딩 없음, 기존 일상 대화는 1인 가운데
        public void NoCharacters_EmptyStage_ExistingTalkIsCentered() // 빈 무대 테스트
        {
            DialogueStageLayout empty = DialogueStageLayout.Build(Parse("{\"nodes\":[{\"speaker\":\"HERO\",\"text\":\"a\"},{\"text\":\"b\"}]}")); // 주인공·나레이션
            DialogueStageLayout talk = DialogueStageLayout.Build(DialogueLibrary.Load("TALK_SERENA_DAY")); // 기존 파일

            Assert.That(empty.Left + empty.Right + empty.Center, Is.Empty); // 빈 무대
            Assert.That(talk.Center, Is.EqualTo("CH_SERENA")); // 기존 파일 호환
        }

        [Test] // 이름표 소속 : 캐릭터는 출신, NPC는 직업, 주인공은 없음 · NPC 화자 허용
        public void SpeakerAffiliation_AndNpcSpeakers() // 이름표 테스트
        {
            Assert.That(DialogueSpeakerInfo.ResolveAffiliation("CH_SERENA"), Is.EqualTo("레티시아 왕국")); // 캐릭터 출신
            Assert.That(DialogueSpeakerInfo.ResolveAffiliation("NPC_BLACKSMITH"), Is.EqualTo("대장장이")); // NPC 직업
            Assert.That(DialogueSpeakerInfo.ResolveName("NPC_SHOPKEEPER"), Is.EqualTo("로웰")); // NPC 이름
            Assert.That(DialogueSpeakerInfo.ResolveAffiliation("HERO"), Is.Empty); // 주인공 소속 없음
            Assert.That(DialogueSpeakerInfo.ResolveName("NARRATION"), Is.Empty); // 나레이션 이름 없음
            Assert.That(DialogueLibrary.IsValidSpeaker("NPC_SHOPKEEPER"), Is.True); // NPC 화자 허용
            Assert.That(DialogueLibrary.IsValidSpeaker("BOB"), Is.False); // 잘못된 화자
        }

        [Test] // 속도 설정 : 통합 설정(Day72)을 따라 빠를수록 글자 속도 ↑ · 자동 대기 ↓
        public void DialogueSettings_FollowSpeedSetting() // 설정 테스트
        {
            ProjectH.Core.GameSettings.SetDialogueSpeed(ProjectH.Core.GameSettings.MinDialogueSpeed); // 가장 느리게
            float slowChars = DialogueSettings.GetCharsPerSecond(); // 초당 글자
            float slowDelay = DialogueSettings.GetAutoDelay(20); // 자동 대기
            ProjectH.Core.GameSettings.SetDialogueSpeed(ProjectH.Core.GameSettings.MaxDialogueSpeed); // 가장 빠르게
            Assert.That(DialogueSettings.GetCharsPerSecond(), Is.GreaterThan(slowChars)); // 글자가 더 빨리 나옴
            Assert.That(DialogueSettings.GetAutoDelay(20), Is.LessThan(slowDelay)); // 덜 기다림
            Assert.That(DialogueSettings.GetAutoDelay(40), Is.GreaterThan(DialogueSettings.GetAutoDelay(10))); // 긴 대사는 더 기다림
            ProjectH.Core.GameSettings.ResetToDefault(); // 기본값 복원
        }
    }
}
