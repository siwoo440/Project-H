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
    public sealed class PartyPresetSaveData // 단일 편성 프리셋 저장 데이터
    {
        [SerializeField] private List<string> characterIds = new List<string>(); // 프리셋 캐릭터 ID
        public IReadOnlyList<string> CharacterIds => characterIds; // 프리셋 목록 반환

        public void EnsureDefaults() // 프리셋 기본값 보정
        {
            if (characterIds == null) // 프리셋 목록 확인
            {
                characterIds = new List<string>(); // 프리셋 목록 복원
            }
        }

        public void SetCharacters(IEnumerable<string> values) // 프리셋 캐릭터 교체
        {
            EnsureDefaults(); // 프리셋 기본값 확인
            characterIds.Clear(); // 기존 프리셋 제거

            if (values == null) // 입력 목록 확인
            {
                return; // 빈 프리셋 유지
            }

            foreach (string characterId in values) // 입력 캐릭터 순회
            {
                characterIds.Add(characterId); // 프리셋 캐릭터 추가
            }
        }
    }

    [Serializable] // JSON 직렬화 허용
    public sealed class SaveData // 전체 진행 저장 데이터
    {
        public const int CurrentVersion = 1; // 현재 저장 버전
        public const int MaxPartySize = 4; // 최대 파티 인원
        public const int PartyPresetCount = 4; // 편성 프리셋 개수
        [SerializeField] private int saveVersion = CurrentVersion; // 저장 버전
        [SerializeField] private int currentDay = 1; // 현재 일차
        [SerializeField] private SaveTimeOfDay currentTime = SaveTimeOfDay.Morning; // 현재 시간대
        [SerializeField] private string currentChapter = "CHAPTER_01"; // 현재 챕터
        [SerializeField] private string currentMainQuest = "MAIN_001"; // 현재 메인 목표
        [SerializeField] private List<string> partyCharacterIds = new List<string>(); // 활성 파티 캐릭터 ID
        [SerializeField] private int selectedPartyPresetIndex; // 활성 편성 프리셋 번호
        [SerializeField] private List<PartyPresetSaveData> partyPresets = new List<PartyPresetSaveData>(); // 편성 프리셋 목록
        [SerializeField] private List<CharacterSaveData> characters = new List<CharacterSaveData>(); // 캐릭터 진행 목록
        [SerializeField] private List<string> storyFlags = new List<string>(); // 활성 스토리 플래그 목록
        public int SaveVersion => saveVersion; // 저장 버전 반환
        public int CurrentDay => currentDay; // 현재 일차 반환
        public SaveTimeOfDay CurrentTime => currentTime; // 현재 시간대 반환
        public string CurrentChapter => currentChapter; // 현재 챕터 반환
        public string CurrentMainQuest => currentMainQuest; // 현재 목표 반환
        public IReadOnlyList<string> PartyCharacterIds => partyCharacterIds; // 활성 파티 목록 반환
        public int SelectedPartyPresetIndex => selectedPartyPresetIndex; // 활성 프리셋 번호 반환
        public IReadOnlyList<PartyPresetSaveData> PartyPresets => partyPresets; // 프리셋 목록 반환
        public IReadOnlyList<CharacterSaveData> Characters => characters; // 캐릭터 진행 반환
        public IReadOnlyList<string> StoryFlags => storyFlags; // 스토리 플래그 반환

        public static SaveData CreateNewGame(IEnumerable<string> characterIds) // 새 게임 데이터 생성
        {
            SaveData saveData = new SaveData(); // 기본 저장 데이터 생성
            HashSet<string> uniqueIds = new HashSet<string>(StringComparer.Ordinal); // 캐릭터 중복 검사 집합

            if (characterIds != null) // 초기 캐릭터 목록 확인
            {
                foreach (string characterId in characterIds) // 초기 캐릭터 순회
                {
                    if (string.IsNullOrWhiteSpace(characterId) || !uniqueIds.Add(characterId)) // 캐릭터 ID 유효성 확인
                    {
                        continue; // 잘못된 캐릭터 제외
                    }

                    saveData.characters.Add(new CharacterSaveData(characterId)); // 초기 보유 캐릭터 등록

                    if (saveData.partyCharacterIds.Count < MaxPartySize) // 초기 파티 최대 인원 확인
                    {
                        saveData.partyCharacterIds.Add(characterId); // 초기 파티 등록
                    }
                }
            }

            saveData.InitializePartyPresetsFromActiveParty(); // 초기 프리셋 생성
            return saveData; // 새 게임 데이터 반환
        }

        public void EnsureDefaults() // 이전 저장 기본값 복원
        {
            if (saveVersion <= 0) // 저장 버전 확인
            {
                saveVersion = CurrentVersion; // 기본 저장 버전 적용
            }

            if (partyCharacterIds == null) // 활성 파티 목록 확인
            {
                partyCharacterIds = new List<string>(); // 활성 파티 목록 복원
            }

            if (characters == null) // 캐릭터 목록 확인
            {
                characters = new List<CharacterSaveData>(); // 캐릭터 목록 복원
            }

            if (storyFlags == null) // 플래그 목록 확인
            {
                storyFlags = new List<string>(); // 플래그 목록 복원
            }

            if (partyPresets == null) // 프리셋 목록 확인
            {
                partyPresets = new List<PartyPresetSaveData>(); // 프리셋 목록 복원
            }

            if (currentChapter == null) // 현재 챕터 확인
            {
                currentChapter = string.Empty; // 챕터 기본값 복원
            }

            if (currentMainQuest == null) // 현재 목표 확인
            {
                currentMainQuest = string.Empty; // 목표 기본값 복원
            }

            currentDay = Mathf.Max(1, currentDay); // 최소 일차 복원
            NormalizeActiveParty(); // 활성 파티 데이터 정리
            EnsurePartyPresetCount(); // 편성 프리셋 개수 보정
            selectedPartyPresetIndex = Mathf.Clamp(selectedPartyPresetIndex, 0, PartyPresetCount - 1); // 활성 프리셋 범위 보정
            NormalizePartyPresets(); // 프리셋 데이터 정리
            SyncActivePartyFromSelectedPreset(); // 활성 파티와 프리셋 동기화
        }

        public CharacterSaveData FindCharacter(string characterId) // 캐릭터 진행 조회
        {
            EnsureDefaults(); // 저장 기본값 확인

            foreach (CharacterSaveData character in characters) // 캐릭터 진행 순회
            {
                if (character != null && string.Equals(character.CharacterId, characterId, StringComparison.Ordinal)) // 캐릭터 ID 비교
                {
                    return character; // 일치 캐릭터 반환
                }
            }

            return null; // 조회 실패 반환
        }

        public bool HasCharacter(string characterId) // 캐릭터 보유 여부 확인
        {
            EnsureDefaults(); // 저장 기본값 확인
            return ContainsOwnedCharacterInternal(characterId); // 캐릭터 보유 결과 반환
        }

        public IReadOnlyList<string> GetPartyPreset(int presetIndex) // 편성 프리셋 조회
        {
            EnsureDefaults(); // 저장 기본값 확인

            if (presetIndex < 0 || presetIndex >= PartyPresetCount) // 프리셋 번호 확인
            {
                return Array.Empty<string>(); // 잘못된 프리셋 빈 목록 반환
            }

            return partyPresets[presetIndex].CharacterIds; // 지정 프리셋 목록 반환
        }

        public bool TrySetPartyPreset(int presetIndex, IEnumerable<string> characterIds, out string error) // 편성 프리셋 변경
        {
            EnsureDefaults(); // 저장 기본값 확인
            error = string.Empty; // 오류 문구 초기화

            if (presetIndex < 0 || presetIndex >= PartyPresetCount) // 프리셋 번호 확인
            {
                error = "잘못된 편성 프리셋 번호입니다."; // 프리셋 번호 오류 설정
                return false; // 프리셋 변경 실패
            }

            if (!TryValidatePartyCharacters(characterIds, out List<string> validatedIds, out error)) // 파티 캐릭터 검증
            {
                return false; // 프리셋 변경 실패
            }

            partyPresets[presetIndex].SetCharacters(validatedIds); // 프리셋 캐릭터 적용

            if (selectedPartyPresetIndex == presetIndex) // 활성 프리셋 변경 확인
            {
                CopyIds(validatedIds, partyCharacterIds); // 활성 파티 동기화
            }

            return true; // 프리셋 변경 성공
        }

        public bool TrySelectPartyPreset(int presetIndex, out string error) // 활성 편성 프리셋 선택
        {
            EnsureDefaults(); // 저장 기본값 확인
            error = string.Empty; // 오류 문구 초기화

            if (presetIndex < 0 || presetIndex >= PartyPresetCount) // 프리셋 번호 확인
            {
                error = "잘못된 편성 프리셋 번호입니다."; // 프리셋 번호 오류 설정
                return false; // 프리셋 선택 실패
            }

            if (!TryValidatePartyCharacters(partyPresets[presetIndex].CharacterIds, out List<string> validatedIds, out error)) // 대상 프리셋 검증
            {
                return false; // 프리셋 선택 실패
            }

            partyPresets[presetIndex].SetCharacters(validatedIds); // 검증된 프리셋 적용
            selectedPartyPresetIndex = presetIndex; // 활성 프리셋 번호 저장
            CopyIds(validatedIds, partyCharacterIds); // 활성 파티 동기화
            return true; // 프리셋 선택 성공
        }

        public bool HasStoryFlag(string flagId) // 스토리 플래그 확인
        {
            EnsureDefaults(); // 저장 기본값 확인

            if (string.IsNullOrWhiteSpace(flagId)) // 플래그 ID 확인
            {
                return false; // 잘못된 플래그 반환
            }

            foreach (string flag in storyFlags) // 플래그 목록 순회
            {
                if (string.Equals(flag, flagId, StringComparison.Ordinal)) // 플래그 ID 비교
                {
                    return true; // 플래그 존재 반환
                }
            }

            return false; // 플래그 없음 반환
        }

        public bool SetStoryFlag(string flagId) // 스토리 플래그 활성화
        {
            EnsureDefaults(); // 저장 기본값 확인

            if (string.IsNullOrWhiteSpace(flagId)) // 플래그 ID 확인
            {
                return false; // 플래그 추가 실패
            }

            if (HasStoryFlag(flagId)) // 기존 플래그 확인
            {
                return false; // 중복 추가 중단
            }

            storyFlags.Add(flagId); // 새 플래그 추가
            storyFlags.Sort(StringComparer.Ordinal); // 플래그 순서 정렬
            return true; // 플래그 추가 성공
        }

        public bool RemoveStoryFlag(string flagId) // 스토리 플래그 비활성화
        {
            EnsureDefaults(); // 저장 기본값 확인

            for (int index = storyFlags.Count - 1; index >= 0; index--) // 플래그 역순 순회
            {
                if (!string.Equals(storyFlags[index], flagId, StringComparison.Ordinal)) // 플래그 ID 비교
                {
                    continue; // 다음 플래그 이동
                }

                storyFlags.RemoveAt(index); // 일치 플래그 제거
                return true; // 플래그 제거 성공
            }

            return false; // 플래그 제거 실패
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

        private void InitializePartyPresetsFromActiveParty() // 활성 파티 기반 프리셋 초기화
        {
            partyPresets = new List<PartyPresetSaveData>(); // 프리셋 목록 초기화

            for (int index = 0; index < PartyPresetCount; index++) // 프리셋 개수 순회
            {
                PartyPresetSaveData preset = new PartyPresetSaveData(); // 새 프리셋 생성
                preset.SetCharacters(partyCharacterIds); // 활성 파티 복사
                partyPresets.Add(preset); // 프리셋 목록 추가
            }

            selectedPartyPresetIndex = 0; // 첫 프리셋 활성화
        }

        private void EnsurePartyPresetCount() // 프리셋 개수 보정
        {
            while (partyPresets.Count < PartyPresetCount) // 부족 프리셋 확인
            {
                partyPresets.Add(new PartyPresetSaveData()); // 빈 프리셋 추가
            }

            if (partyPresets.Count > PartyPresetCount) // 초과 프리셋 확인
            {
                partyPresets.RemoveRange(PartyPresetCount, partyPresets.Count - PartyPresetCount); // 초과 프리셋 제거
            }

            for (int index = 0; index < partyPresets.Count; index++) // 프리셋 목록 순회
            {
                if (partyPresets[index] == null) // null 프리셋 확인
                {
                    partyPresets[index] = new PartyPresetSaveData(); // null 프리셋 복원
                }

                partyPresets[index].EnsureDefaults(); // 프리셋 내부 목록 보정
            }
        }

        private void NormalizeActiveParty() // 활성 파티 데이터 정리
        {
            List<string> normalized = NormalizePartyIds(partyCharacterIds); // 활성 파티 정규화

            if (normalized.Count == 0) // 활성 파티 없음 확인
            {
                AddFallbackOwnedCharacters(normalized); // 보유 캐릭터 기반 파티 복원
            }

            CopyIds(normalized, partyCharacterIds); // 정규화 결과 적용
        }

        private void NormalizePartyPresets() // 전체 프리셋 데이터 정리
        {
            for (int index = 0; index < partyPresets.Count; index++) // 프리셋 목록 순회
            {
                List<string> normalized = NormalizePartyIds(partyPresets[index].CharacterIds); // 프리셋 캐릭터 정규화

                if (normalized.Count == 0) // 비어 있는 프리셋 확인
                {
                    normalized.AddRange(partyCharacterIds); // 현재 활성 파티를 기본값으로 복사
                }

                partyPresets[index].SetCharacters(normalized); // 정규화 프리셋 적용
            }
        }

        private void SyncActivePartyFromSelectedPreset() // 활성 파티 프리셋 동기화
        {
            List<string> normalized = NormalizePartyIds(partyPresets[selectedPartyPresetIndex].CharacterIds); // 활성 프리셋 정규화

            if (normalized.Count == 0) // 활성 프리셋 비어 있음 확인
            {
                normalized.AddRange(partyCharacterIds); // 기존 활성 파티 사용
            }

            partyPresets[selectedPartyPresetIndex].SetCharacters(normalized); // 활성 프리셋 정규화 적용
            CopyIds(normalized, partyCharacterIds); // 활성 파티 적용
        }

        private bool TryValidatePartyCharacters(IEnumerable<string> characterIds, out List<string> validatedIds, out string error) // 파티 구성 검증
        {
            validatedIds = new List<string>(); // 검증 결과 목록 생성
            error = string.Empty; // 오류 문구 초기화

            if (characterIds == null) // 입력 목록 확인
            {
                error = "편성 캐릭터 목록이 없습니다."; // 목록 없음 오류 설정
                return false; // 파티 검증 실패
            }

            HashSet<string> uniqueIds = new HashSet<string>(StringComparer.Ordinal); // 중복 검사 집합

            foreach (string characterId in characterIds) // 캐릭터 ID 순회
            {
                if (string.IsNullOrWhiteSpace(characterId)) // 빈 캐릭터 ID 확인
                {
                    error = "빈 캐릭터 ID는 편성할 수 없습니다."; // 빈 ID 오류 설정
                    return false; // 파티 검증 실패
                }

                if (!uniqueIds.Add(characterId)) // 중복 캐릭터 확인
                {
                    error = $"중복 캐릭터는 편성할 수 없습니다. ID={characterId}"; // 중복 오류 설정
                    return false; // 파티 검증 실패
                }

                if (!ContainsOwnedCharacterInternal(characterId)) // 캐릭터 보유 여부 확인
                {
                    error = $"보유하지 않은 캐릭터는 편성할 수 없습니다. ID={characterId}"; // 미보유 오류 설정
                    return false; // 파티 검증 실패
                }

                validatedIds.Add(characterId); // 검증 캐릭터 추가

                if (validatedIds.Count > MaxPartySize) // 최대 파티 인원 확인
                {
                    error = $"파티는 최대 {MaxPartySize}명까지 편성할 수 있습니다."; // 최대 인원 오류 설정
                    return false; // 파티 검증 실패
                }
            }

            if (validatedIds.Count == 0) // 최소 파티 인원 확인
            {
                error = "파티에는 최소 1명의 캐릭터가 필요합니다."; // 최소 인원 오류 설정
                return false; // 파티 검증 실패
            }

            return true; // 파티 검증 성공
        }

        private List<string> NormalizePartyIds(IEnumerable<string> source) // 파티 ID 정규화
        {
            List<string> normalized = new List<string>(); // 정규화 결과 생성
            HashSet<string> uniqueIds = new HashSet<string>(StringComparer.Ordinal); // 중복 검사 집합

            if (source == null) // 원본 목록 확인
            {
                return normalized; // 빈 결과 반환
            }

            foreach (string characterId in source) // 원본 캐릭터 순회
            {
                if (normalized.Count >= MaxPartySize) // 최대 인원 확인
                {
                    break; // 추가 정규화 중단
                }

                if (string.IsNullOrWhiteSpace(characterId) || !uniqueIds.Add(characterId) || !ContainsOwnedCharacterInternal(characterId)) // 캐릭터 유효성 확인
                {
                    continue; // 잘못된 캐릭터 제외
                }

                normalized.Add(characterId); // 정규화 캐릭터 추가
            }

            return normalized; // 정규화 결과 반환
        }

        private void AddFallbackOwnedCharacters(List<string> target) // 보유 캐릭터 기반 기본 파티 생성
        {
            foreach (CharacterSaveData character in characters) // 보유 캐릭터 순회
            {
                if (target.Count >= MaxPartySize) // 최대 인원 확인
                {
                    break; // 기본 파티 생성 종료
                }

                if (character == null || string.IsNullOrWhiteSpace(character.CharacterId) || target.Contains(character.CharacterId)) // 캐릭터 저장 유효성 확인
                {
                    continue; // 잘못된 캐릭터 제외
                }

                target.Add(character.CharacterId); // 기본 파티 캐릭터 추가
            }
        }

        private bool ContainsOwnedCharacterInternal(string characterId) // 내부 캐릭터 보유 여부 확인
        {
            if (string.IsNullOrWhiteSpace(characterId)) // 캐릭터 ID 확인
            {
                return false; // 빈 ID 미보유 반환
            }

            foreach (CharacterSaveData character in characters) // 보유 캐릭터 순회
            {
                if (character != null && string.Equals(character.CharacterId, characterId, StringComparison.Ordinal)) // 캐릭터 ID 비교
                {
                    return true; // 보유 캐릭터 반환
                }
            }

            return false; // 미보유 캐릭터 반환
        }

        private static void CopyIds(IEnumerable<string> source, List<string> target) // 캐릭터 ID 목록 복사
        {
            target.Clear(); // 대상 목록 초기화

            foreach (string characterId in source) // 원본 캐릭터 순회
            {
                target.Add(characterId); // 대상 목록 추가
            }
        }
    }
}
