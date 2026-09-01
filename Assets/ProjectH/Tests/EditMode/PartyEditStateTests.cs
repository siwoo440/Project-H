using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.SaveSystem; // 저장 데이터 기능
using ProjectH.UI; // 파티 UI 상태 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class PartyEditStateTests // 파티 편집 상태 테스트
    {
        [Test] // 테스트 표시
        public void AssignCharacter_ReplacesSelectedSlot() // 슬롯 캐릭터 교체 검증
        {
            SaveData saveData = CreateSixCharacterSave(); // 6인 저장 데이터 생성
            PartyEditState state = PartyEditState.Create(saveData); // 편집 상태 생성
            bool changed = state.TryAssignCharacter(2, "CH_NATASHA", out string error); // 세 번째 슬롯 나타샤 교체
            Assert.That(changed, Is.True, error); // 교체 성공 검증
            Assert.That(state.GetMemberAtSlot(2), Is.EqualTo("CH_NATASHA")); // 세 번째 슬롯 검증
        }

        [Test] // 테스트 표시
        public void AssignCharacter_RejectsDuplicateInSameParty() // 동일 파티 중복 방지 검증
        {
            SaveData saveData = CreateSixCharacterSave(); // 6인 저장 데이터 생성
            PartyEditState state = PartyEditState.Create(saveData); // 편집 상태 생성
            bool changed = state.TryAssignCharacter(2, "CH_ELLEN", out string error); // 이미 편성된 엘렌 교체 시도
            Assert.That(changed, Is.False); // 중복 교체 실패 검증
            Assert.That(error, Does.Contain("편성")); // 중복 안내 검증
        }

        [Test] // 테스트 표시
        public void ClearSlot_RemovesMemberAndCompactsParty() // 슬롯 비우기 압축 검증
        {
            SaveData saveData = CreateSixCharacterSave(); // 6인 저장 데이터 생성
            PartyEditState state = PartyEditState.Create(saveData); // 편집 상태 생성
            bool changed = state.TryClearSlot(1, out string error); // 두 번째 슬롯 제거
            Assert.That(changed, Is.True, error); // 제거 성공 검증
            Assert.That(state.MemberCount, Is.EqualTo(3)); // 파티 인원 감소 검증
            Assert.That(state.GetMemberAtSlot(1), Is.EqualTo("CH_LILIA")); // 뒤 슬롯 앞으로 이동 검증
        }

        [Test] // 테스트 표시
        public void Commit_PersistsEditedPresetAndSelection() // 편집 상태 저장 반영 검증
        {
            SaveData saveData = CreateSixCharacterSave(); // 6인 저장 데이터 생성
            PartyEditState state = PartyEditState.Create(saveData); // 편집 상태 생성
            state.TrySelectPreset(1, out string presetError); // 두 번째 프리셋 선택
            state.TryAssignCharacter(0, "CH_NATASHA", out string assignError); // 첫 슬롯 나타샤 교체
            bool committed = state.CommitTo(saveData, out string commitError); // 편집 상태 저장 반영
            Assert.That(presetError, Is.Empty); // 프리셋 선택 오류 없음 검증
            Assert.That(assignError, Is.Empty); // 캐릭터 교체 오류 없음 검증
            Assert.That(committed, Is.True, commitError); // 저장 반영 성공 검증
            Assert.That(saveData.PartyCharacterIds[0], Is.EqualTo("CH_NATASHA")); // 활성 첫 슬롯 검증
        }

        private static SaveData CreateSixCharacterSave() // 6인 테스트 저장 생성
        {
            return SaveData.CreateNewGame(new[] { "CH_SERENA", "CH_ELLEN", "CH_LILIA", "CH_EVE", "CH_NATASHA", "CH_CLAIRE" }); // 6인 저장 데이터 반환
        }
    }
}
