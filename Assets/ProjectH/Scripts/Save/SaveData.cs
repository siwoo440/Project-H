using System; // 직렬화 기능
using System.Collections.Generic; // 목록 자료형
using UnityEngine; // Unity 기본 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    public enum SaveTimeOfDay // 저장 시간대
    {
        Morning = 0, // 아침
        Afternoon = 1, // 낮
        Evening = 2, // 저녁
        Night = 3 // 밤
    }

    [Serializable] // JSON 직렬화 허용
    public sealed class CharacterSaveData // 캐릭터 진행 저장 데이터
    {
        [SerializeField] private string characterId; // 캐릭터 ID
        [SerializeField] private int level = 1; // 캐릭터 레벨
        [SerializeField] private int experience; // 캐릭터 경험치

        public string CharacterId => characterId; // 캐릭터 ID 반환
        public int Level => level; // 레벨 반환
        public int Experience => experience; // 경험치 반환

        public CharacterSaveData(string id) // 캐릭터 저장 데이터 생성
        {
            characterId = id; // 캐릭터 ID 저장
            level = 1; // 초기 레벨 설정
            experience = 0; // 초기 경험치 설정
        }

        public void SetLevel(int value) // 레벨 변경
        {
            level = Mathf.Max(1, value); // 최소 레벨 보장
        }

        public void SetExperience(int value) // 경험치 변경
        {
            experience = Mathf.Max(0, value); // 음수 경험치 방지
        }
    }

    [Serializable] // JSON 직렬화 허용
    public sealed class SaveData // 전체 진행 저장 데이터
    {
        public const int CurrentVersion = 1; // 현재 저장 버전

        [SerializeField] private int saveVersion = CurrentVersion; // 저장 버전
        [SerializeField] private int currentDay = 1; // 현재 일차
        [SerializeField] private SaveTimeOfDay currentTime = SaveTimeOfDay.Morning; // 현재 시간대
        [SerializeField] private string currentChapter = "CHAPTER_01"; // 현재 챕터
        [SerializeField] private string currentMainQuest = "MAIN_001"; // 현재 메인 목표
        [SerializeField] private List<string> partyCharacterIds = new List<string>(); // 파티 캐릭터 ID
        [SerializeField] private List<CharacterSaveData> characters = new List<CharacterSaveData>(); // 캐릭터 진행 목록

        public int SaveVersion => saveVersion; // 저장 버전 반환
        public int CurrentDay => currentDay; // 현재 일차 반환
        public SaveTimeOfDay CurrentTime => currentTime; // 현재 시간대 반환
        public string CurrentChapter => currentChapter; // 현재 챕터 반환
        public string CurrentMainQuest => currentMainQuest; // 현재 목표 반환
        public IReadOnlyList<string> PartyCharacterIds => partyCharacterIds; // 파티 목록 반환
        public IReadOnlyList<CharacterSaveData> Characters => characters; // 캐릭터 진행 반환

        public static SaveData CreateNewGame(IEnumerable<string> characterIds) // 새 게임 데이터 생성
        {
            SaveData saveData = new SaveData(); // 기본 저장 데이터 생성

            foreach (string characterId in characterIds) // 초기 캐릭터 순회
            {
                saveData.partyCharacterIds.Add(characterId); // 초기 파티 등록
                saveData.characters.Add(new CharacterSaveData(characterId)); // 초기 캐릭터 진행 등록
            }

            return saveData; // 새 게임 데이터 반환
        }

        public CharacterSaveData FindCharacter(string characterId) // 캐릭터 진행 조회
        {
            foreach (CharacterSaveData character in characters) // 캐릭터 진행 순회
            {
                if (character != null && string.Equals(character.CharacterId, characterId, StringComparison.Ordinal)) // 캐릭터 ID 비교
                {
                    return character; // 일치 캐릭터 반환
                }
            }

            return null; // 조회 실패 반환
        }

        public void SetCurrentDay(int value) // 현재 일차 변경
        {
            currentDay = Mathf.Max(1, value); // 최소 일차 보장
        }

        public void SetCurrentTime(SaveTimeOfDay value) // 현재 시간대 변경
        {
            currentTime = value; // 시간대 저장
        }

        public void SetCurrentChapter(string value) // 현재 챕터 변경
        {
            currentChapter = value ?? string.Empty; // null 문자열 방지
        }

        public void SetCurrentMainQuest(string value) // 현재 목표 변경
        {
            currentMainQuest = value ?? string.Empty; // null 문자열 방지
        }
    }
}
