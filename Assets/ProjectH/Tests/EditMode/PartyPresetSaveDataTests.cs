using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.SaveSystem; // 저장 데이터 기능
using UnityEngine; // JSON 직렬화 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class PartyPresetSaveDataTests // 파티 프리셋 저장 테스트
    {
        [Test] // 테스트 표시
        public void CreateNewGame_WithSixOwnedCharacters_UsesFirstFourAsParty() // 6인 보유 초기 파티 검증
        {
            SaveData saveData = CreateSixCharacterSave(); // 6인 저장 데이터 생성
            Assert.That(saveData.Characters.Count, Is.EqualTo(6)); // 보유 캐릭터 수 검증
            Assert.That(saveData.PartyCharacterIds.Count, Is.EqualTo(4)); // 활성 파티 인원 검증
            Assert.That(saveData.PartyCharacterIds[0], Is.EqualTo("CH_SERENA")); // 첫 슬롯 검증
            Assert.That(saveData.PartyCharacterIds[3], Is.EqualTo("CH_EVE")); // 네 번째 슬롯 검증
            Assert.That(saveData.PartyPresets.Count, Is.EqualTo(4)); // 프리셋 개수 검증
        }

        [Test] // 테스트 표시
        public void TrySetPartyPreset_RejectsDuplicateCharacter() // 중복 캐릭터 편성 방지 검증
        {
            SaveData saveData = CreateSixCharacterSave(); // 6인 저장 데이터 생성
            bool changed = saveData.TrySetPartyPreset(1, new[] { "CH_SERENA", "CH_SERENA" }, out string error); // 중복 파티 설정 시도
            Assert.That(changed, Is.False); // 변경 실패 검증
            Assert.That(error, Does.Contain("중복")); // 중복 오류 문구 검증
        }

        [Test] // 테스트 표시
        public void TrySetPartyPreset_RejectsUnownedCharacter() // 미보유 캐릭터 편성 방지 검증
        {
            SaveData saveData = CreateSixCharacterSave(); // 6인 저장 데이터 생성
            bool changed = saveData.TrySetPartyPreset(1, new[] { "CH_SEPHIRA" }, out string error); // 미보유 캐릭터 편성 시도
            Assert.That(changed, Is.False); // 변경 실패 검증
            Assert.That(error, Does.Contain("보유")); // 보유 오류 문구 검증
        }

        [Test] // 테스트 표시
        public void SelectingPreset_UpdatesActivePartyOrder() // 프리셋 활성 파티 동기화 검증
        {
            SaveData saveData = CreateSixCharacterSave(); // 6인 저장 데이터 생성
            bool configured = saveData.TrySetPartyPreset(1, new[] { "CH_NATASHA", "CH_CLAIRE", "CH_ELLEN", "CH_SERENA" }, out string configureError); // 두 번째 프리셋 설정
            bool selected = saveData.TrySelectPartyPreset(1, out string selectError); // 두 번째 프리셋 선택
            Assert.That(configured, Is.True, configureError); // 프리셋 설정 성공 검증
            Assert.That(selected, Is.True, selectError); // 프리셋 선택 성공 검증
            Assert.That(saveData.SelectedPartyPresetIndex, Is.EqualTo(1)); // 선택 프리셋 번호 검증
            Assert.That(saveData.PartyCharacterIds[0], Is.EqualTo("CH_NATASHA")); // 활성 첫 슬롯 검증
            Assert.That(saveData.PartyCharacterIds[3], Is.EqualTo("CH_SERENA")); // 활성 마지막 슬롯 검증
        }

        [Test] // 테스트 표시
        public void OldSaveWithoutPresets_MigratesToFourPresets() // 구버전 저장 프리셋 보정 검증
        {
            string json = "{\"saveVersion\":1,\"partyCharacterIds\":[\"CH_SERENA\",\"CH_ELLEN\"],\"characters\":[{\"characterId\":\"CH_SERENA\",\"level\":1,\"experience\":0},{\"characterId\":\"CH_ELLEN\",\"level\":1,\"experience\":0}],\"storyFlags\":[]}"; // 구버전 JSON 생성
            SaveData saveData = JsonUtility.FromJson<SaveData>(json); // 구버전 저장 역직렬화
            saveData.EnsureDefaults(); // 신규 기본값 마이그레이션
            Assert.That(saveData.PartyPresets.Count, Is.EqualTo(4)); // 프리셋 4개 생성 검증
            Assert.That(saveData.GetPartyPreset(0).Count, Is.EqualTo(2)); // 첫 프리셋 인원 검증
            Assert.That(saveData.GetPartyPreset(3)[1], Is.EqualTo("CH_ELLEN")); // 네 번째 프리셋 복제 검증
        }

        private static SaveData CreateSixCharacterSave() // 6인 테스트 저장 생성
        {
            return SaveData.CreateNewGame(new[] { "CH_SERENA", "CH_ELLEN", "CH_LILIA", "CH_EVE", "CH_NATASHA", "CH_CLAIRE" }); // 6인 저장 데이터 반환
        }
    }
}
