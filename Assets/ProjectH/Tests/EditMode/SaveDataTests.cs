using System.Collections.Generic; // 목록 자료형
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.SaveSystem; // 프로젝트 저장 기능
using UnityEngine; // Unity JSON 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class SaveDataTests // 저장 데이터 테스트
    {
        [Test] // 테스트 표시
        public void CreateNewGame_WithInitialCharacters_CreatesDefaultProgress() // 새 게임 기본값 테스트
        {
            string[] characterIds = // 초기 캐릭터 ID 생성
            {
                "CH_SERENA", // 세레나 ID
                "CH_ELLEN" // 엘렌 ID
            }; // 초기 캐릭터 ID 목록 종료

            SaveData saveData = SaveData.CreateNewGame(characterIds); // 새 게임 데이터 생성

            Assert.That(saveData.SaveVersion, Is.EqualTo(1)); // 저장 버전 검증
            Assert.That(saveData.CurrentDay, Is.EqualTo(1)); // 초기 일차 검증
            Assert.That(saveData.CurrentTime, Is.EqualTo(SaveTimeOfDay.Morning)); // 초기 시간대 검증
            Assert.That(saveData.Characters.Count, Is.EqualTo(2)); // 캐릭터 개수 검증
            Assert.That(saveData.PartyCharacterIds.Count, Is.EqualTo(2)); // 파티 개수 검증
            Assert.That(saveData.FindCharacter("CH_SERENA").Level, Is.EqualTo(1)); // 초기 레벨 검증
        }

        [Test] // 테스트 표시
        public void JsonRoundTrip_WithChangedProgress_PreservesValues() // JSON 왕복 저장 테스트
        {
            SaveData source = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 원본 저장 데이터 생성
            source.SetCurrentDay(7); // 테스트 일차 변경
            CharacterSaveData serena = source.FindCharacter("CH_SERENA"); // 세레나 진행 조회
            serena.SetLevel(5); // 테스트 레벨 변경
            serena.SetExperience(350); // 테스트 경험치 변경
            string json = JsonUtility.ToJson(source, true); // JSON 직렬화
            SaveData loaded = JsonUtility.FromJson<SaveData>(json); // JSON 역직렬화
            CharacterSaveData loadedSerena = loaded.FindCharacter("CH_SERENA"); // 복원 세레나 조회

            Assert.That(loaded.CurrentDay, Is.EqualTo(7)); // 복원 일차 검증
            Assert.That(loadedSerena, Is.Not.Null); // 복원 캐릭터 검증
            Assert.That(loadedSerena.Level, Is.EqualTo(5)); // 복원 레벨 검증
            Assert.That(loadedSerena.Experience, Is.EqualTo(350)); // 복원 경험치 검증
        }

        [Test] // 테스트 표시
        public void FindCharacter_WithUnknownId_ReturnsNull() // 미등록 캐릭터 조회 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(new List<string> { "CH_SERENA" }); // 새 게임 데이터 생성

            CharacterSaveData result = saveData.FindCharacter("CH_UNKNOWN"); // 미등록 캐릭터 조회

            Assert.That(result, Is.Null); // null 결과 검증
        }
    }
}
