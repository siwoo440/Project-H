using System; // 직렬화 기능
using System.Collections.Generic; // 목록 자료형
using UnityEngine; // Unity 기본 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    [Serializable] // JSON 직렬화 허용
    public sealed class SaveData // 전체 진행 저장 데이터
    {
        public const int CurrentVersion = 1; // 현재 저장 버전
        public const int MaxPartySize = 4; // 최대 파티 인원
        public const int PartyPresetCount = 4; // 편성 프리셋 개수
        public const int MaxVitality = 100; // 활력 기본 최대값 (Day42 임시 기본값, 추후 기획 수치로 조정)
        private const string EquipmentInstancePrefix = "EQI_"; // 장비 인스턴스 ID 접두사
        [SerializeField] private int saveVersion = CurrentVersion; // 저장 버전
        [SerializeField] private int currentDay = 1; // 현재 일차
        [SerializeField] private SaveTimeOfDay currentTime = SaveTimeOfDay.Morning; // 현재 시간대
        [SerializeField] private int currentVitality = MaxVitality; // 현재 활력
        [SerializeField] private string currentChapter = "CHAPTER_01"; // 현재 챕터
        [SerializeField] private string currentMainQuest = "MAIN_001"; // 현재 메인 목표
        [SerializeField] private List<string> partyCharacterIds = new List<string>(); // 활성 파티 캐릭터 ID
        [SerializeField] private int selectedPartyPresetIndex; // 활성 편성 프리셋 번호
        [SerializeField] private List<PartyPresetSaveData> partyPresets = new List<PartyPresetSaveData>(); // 편성 프리셋 목록
        [SerializeField] private List<CharacterSaveData> characters = new List<CharacterSaveData>(); // 캐릭터 진행 목록
        [SerializeField] private List<string> storyFlags = new List<string>(); // 활성 스토리 플래그 목록
        [SerializeField] private List<EquipmentInstanceSaveData> equipmentInventory = new List<EquipmentInstanceSaveData>(); // 보유 장비 인스턴스 목록
        [SerializeField] private List<ItemStackSaveData> itemInventory = new List<ItemStackSaveData>(); // 보유 일반 아이템 스택 목록
        [SerializeField] private List<RegionErosionSaveData> regionErosion = new List<RegionErosionSaveData>(); // 지역별 침식도 목록 (Day44)
        [SerializeField] private int bondResource; // 결속 자원 (Day59 추가, 최대 5·하루 1 회복)
        [SerializeField] private int lastBondRefillDay; // 결속 자원을 마지막으로 채운 일차 (Day59 추가, 0이면 첫 지급 전)
        [SerializeField] private List<RuneInstanceSaveData> runeInventory = new List<RuneInstanceSaveData>(); // 보유 룬 목록 (Day60 추가)
        [SerializeField] private List<ShopStateSaveData> shopStates = new List<ShopStateSaveData>(); // 상점별 하루 상태 (Day61 추가 — 오늘의 상품·재고·리롤)
        [SerializeField] private List<string> seenDialogueIds = new List<string>(); // 끝까지 본 대화 ID (Day63 추가 — 일기장 다시 보기)
        [SerializeField] private List<string> seenMonsterIds = new List<string>(); // 전투에서 만난 몬스터 ID (Day63 추가 — 일기장 몬스터 정보)
        [SerializeField] private RiftStateSaveData riftState = new RiftStateSaveData(); // 검은 균열(긴급 던전) 상태 (Day67 추가)
        [SerializeField] private GuildQuestBoardSaveData questBoard = new GuildQuestBoardSaveData(); // 오늘의 길드 의뢰 (Day67 추가)
        public int SaveVersion => saveVersion; // 저장 버전 반환
        public int CurrentDay => currentDay; // 현재 일차 반환
        public SaveTimeOfDay CurrentTime => currentTime; // 현재 시간대 반환
        public int CurrentVitality => currentVitality; // 현재 활력 반환
        public string CurrentChapter => currentChapter; // 현재 챕터 반환
        public string CurrentMainQuest => currentMainQuest; // 현재 목표 반환
        public IReadOnlyList<string> PartyCharacterIds => partyCharacterIds; // 활성 파티 목록 반환
        public int SelectedPartyPresetIndex => selectedPartyPresetIndex; // 활성 프리셋 번호 반환
        public IReadOnlyList<PartyPresetSaveData> PartyPresets => partyPresets; // 프리셋 목록 반환
        public IReadOnlyList<CharacterSaveData> Characters => characters; // 캐릭터 진행 반환
        public IReadOnlyList<string> StoryFlags => storyFlags; // 스토리 플래그 반환
        public IReadOnlyList<EquipmentInstanceSaveData> EquipmentInventory => equipmentInventory; // 장비 인벤토리 반환
        public IReadOnlyList<ItemStackSaveData> ItemInventory => itemInventory; // 일반 아이템 인벤토리 반환
        public IReadOnlyList<RegionErosionSaveData> RegionErosion => regionErosion; // 지역별 침식도 목록 반환 (Day44)
        public int BondResource => bondResource; // 결속 자원 반환 (Day59 추가, 회복 반영은 BondService.GetResource 사용)
        public int LastBondRefillDay => lastBondRefillDay; // 결속 자원 마지막 회복 일차 반환 (Day59 추가)
        public IReadOnlyList<RuneInstanceSaveData> RuneInventory => runeInventory; // 보유 룬 목록 반환 (Day60 추가)
        public IReadOnlyList<ShopStateSaveData> ShopStates => shopStates; // 상점 상태 목록 반환 (Day61 추가)
        public IReadOnlyList<string> SeenDialogueIds => seenDialogueIds; // 본 대화 목록 반환 (Day63 추가)
        public IReadOnlyList<string> SeenMonsterIds => seenMonsterIds; // 만난 몬스터 목록 반환 (Day63 추가)
        public RiftStateSaveData RiftState => riftState; // 검은 균열 상태 반환 (Day67 추가)
        public GuildQuestBoardSaveData QuestBoard => questBoard; // 길드 의뢰 게시판 반환 (Day67 추가)

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

            for (int index = 0; index < characters.Count; index++) // 캐릭터 저장 목록 순회
            {
                if (characters[index] != null) // 캐릭터 저장 존재 확인
                {
                    characters[index].EnsureDefaults(); // 캐릭터 장착 상태 포함 기본값 복원
                }
            }

            if (storyFlags == null) // 플래그 목록 확인
            {
                storyFlags = new List<string>(); // 플래그 목록 복원
            }

            if (partyPresets == null) // 프리셋 목록 확인
            {
                partyPresets = new List<PartyPresetSaveData>(); // 프리셋 목록 복원
            }

            if (equipmentInventory == null) // 장비 인벤토리 확인
            {
                equipmentInventory = new List<EquipmentInstanceSaveData>(); // 장비 인벤토리 복원
            }

            if (itemInventory == null) // 일반 아이템 인벤토리 확인
            {
                itemInventory = new List<ItemStackSaveData>(); // 일반 아이템 인벤토리 복원
            }

            NormalizeItemInventory(); // 일반 아이템 스택 정규화

            if (regionErosion == null) // 지역별 침식도 목록 확인
            {
                regionErosion = new List<RegionErosionSaveData>(); // 지역별 침식도 목록 복원
            }

            NormalizeRegionErosion(); // 지역별 침식도 정규화 (Day44)

            if (runeInventory == null) // 룬 목록 확인 (Day60 추가, 기존 세이브는 빈 목록)
            {
                runeInventory = new List<RuneInstanceSaveData>(); // 룬 목록 복원
            }

            runeInventory.RemoveAll(rune => rune == null || string.IsNullOrWhiteSpace(rune.InstanceId)); // 잘못된 룬 제거

            for (int index = 0; index < runeInventory.Count; index++) // 룬 순회
            {
                runeInventory[index].EnsureDefaults(); // 룬 기본값 보정
            }

            if (shopStates == null) // 상점 상태 확인 (Day61 추가, 기존 세이브는 빈 목록)
            {
                shopStates = new List<ShopStateSaveData>(); // 상점 상태 복원
            }

            shopStates.RemoveAll(state => state == null || string.IsNullOrWhiteSpace(state.ShopId)); // 잘못된 상태 제거

            for (int index = 0; index < shopStates.Count; index++) // 상점 상태 순회
            {
                shopStates[index].EnsureDefaults(); // 상태 기본값 보정
            }

            if (riftState == null) riftState = new RiftStateSaveData(); // 균열 상태 복원 (Day67 추가, 기존 세이브는 빈 상태)
            if (questBoard == null) questBoard = new GuildQuestBoardSaveData(); // 의뢰 게시판 복원 (Day67 추가)
            riftState.EnsureDefaults(); // 균열 상태 보정
            questBoard.EnsureDefaults(); // 의뢰 게시판 보정
            if (seenDialogueIds == null) seenDialogueIds = new List<string>(); // 본 대화 목록 복원 (Day63 추가, 기존 세이브는 빈 목록)
            if (seenMonsterIds == null) seenMonsterIds = new List<string>(); // 만난 몬스터 목록 복원 (Day63 추가)
            seenDialogueIds.RemoveAll(string.IsNullOrWhiteSpace); // 빈 ID 제거
            seenMonsterIds.RemoveAll(string.IsNullOrWhiteSpace); // 빈 ID 제거

            if (currentChapter == null) // 현재 챕터 확인
            {
                currentChapter = string.Empty; // 챕터 기본값 복원
            }

            if (currentMainQuest == null) // 현재 목표 확인
            {
                currentMainQuest = string.Empty; // 목표 기본값 복원
            }

            currentDay = Mathf.Max(1, currentDay); // 최소 일차 복원

            if (!Enum.IsDefined(typeof(SaveTimeOfDay), currentTime)) // 정의되지 않은 시간대 값 확인
            {
                currentTime = SaveTimeOfDay.Morning; // 잘못된 시간대 기본값 복원
            }

            currentVitality = Mathf.Clamp(currentVitality, 0, MaxVitality); // 활력 범위 보정
            bondResource = Mathf.Max(0, bondResource); // 결속 자원 음수 방지 (Day59 추가)
            lastBondRefillDay = Mathf.Max(0, lastBondRefillDay); // 결속 회복 일차 음수 방지 (Day59 추가)
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

        internal bool AddCharacterInternal(string characterId) // 새 동료 합류 (Day64 추가 — RecruitService 전용, 이미 있으면 false)
        {
            EnsureDefaults(); // 저장 기본값 확인
            if (string.IsNullOrWhiteSpace(characterId) || FindCharacter(characterId) != null) return false; // 빈 ID·중복
            characters.Add(new CharacterSaveData(characterId)); // 레벨 1로 합류
            return true; // 합류
        }

        public bool HasCharacter(string characterId) // 캐릭터 보유 여부 확인
        {
            EnsureDefaults(); // 저장 기본값 확인
            return ContainsOwnedCharacterInternal(characterId); // 캐릭터 보유 결과 반환
        }

        public EquipmentInstanceSaveData FindEquipmentInstance(string instanceId) // 장비 인스턴스 조회
        {
            EnsureDefaults(); // 저장 기본값 확인
            return FindEquipmentInstanceInternal(instanceId); // 장비 인스턴스 조회 결과 반환
        }

        public int GetItemCount(string itemId) // 일반 아이템 보유 수량 조회
        {
            EnsureDefaults(); // 저장 기본값 확인
            ItemStackSaveData stack = FindItemStackInternal(itemId); // 일반 아이템 스택 조회
            return stack == null ? 0 : stack.Quantity; // 일반 아이템 수량 반환
        }

        public int GetRegionErosion(string regionId) // 지역 침식도 조회 (Day44)
        {
            EnsureDefaults(); // 저장 기본값 확인
            RegionErosionSaveData entry = FindRegionErosionInternal(regionId); // 지역 침식도 항목 조회
            return entry == null ? 0 : entry.ErosionLevel; // 미등록 지역 0 반환
        }

        public bool HasRegionErosion(string regionId) // 지역 침식도 등록 여부 확인 (Day44)
        {
            EnsureDefaults(); // 저장 기본값 확인
            return FindRegionErosionInternal(regionId) != null; // 지역 침식도 항목 존재 여부 반환
        }

        public int SetRegionErosion(string regionId, int value) // 지역 침식도 절대값 저장 (Day44)
        {
            EnsureDefaults(); // 저장 기본값 확인

            if (string.IsNullOrWhiteSpace(regionId)) // 지역 ID 확인
            {
                return 0; // 잘못된 지역 ID 변경 없음 반환
            }

            RegionErosionSaveData entry = FindRegionErosionInternal(regionId); // 기존 지역 침식도 항목 조회

            if (entry == null) // 기존 항목 존재 확인
            {
                entry = new RegionErosionSaveData(regionId, value); // 신규 지역 침식도 항목 생성
                regionErosion.Add(entry); // 지역 침식도 목록 추가
                return entry.ErosionLevel; // 신규 침식도 반환
            }

            entry.SetErosion(value); // 기존 지역 침식도 변경
            return entry.ErosionLevel; // 변경 침식도 반환
        }

        internal bool TrySetItemCountInternal(string itemId, int quantity, out string error) // 일반 아이템 수량 내부 변경
        {
            EnsureDefaults(); // 저장 기본값 확인
            error = string.Empty; // 오류 문구 초기화

            if (string.IsNullOrWhiteSpace(itemId)) // 아이템 ID 확인
            {
                error = "아이템 ID가 비어 있습니다."; // 빈 아이템 ID 오류 설정
                return false; // 아이템 수량 변경 실패
            }

            if (quantity < 0) // 아이템 수량 확인
            {
                error = "아이템 수량은 0 이상이어야 합니다."; // 음수 수량 오류 설정
                return false; // 아이템 수량 변경 실패
            }

            ItemStackSaveData stack = FindItemStackInternal(itemId); // 기존 아이템 스택 조회

            if (quantity == 0) // 스택 제거 여부 확인
            {
                if (stack != null) // 기존 스택 존재 확인
                {
                    itemInventory.Remove(stack); // 일반 아이템 스택 제거
                }

                return true; // 수량 0 적용 성공
            }

            if (stack == null) // 기존 스택 존재 확인
            {
                itemInventory.Add(new ItemStackSaveData(itemId, quantity)); // 신규 일반 아이템 스택 추가
                return true; // 신규 스택 추가 성공
            }

            stack.SetQuantity(quantity); // 기존 일반 아이템 수량 변경
            return true; // 기존 스택 변경 성공
        }

        public int GetEquipmentCount(string equipmentId) // 장비 원본 ID별 보유 수량 조회
        {
            EnsureDefaults(); // 저장 기본값 확인

            if (string.IsNullOrWhiteSpace(equipmentId)) // 장비 원본 ID 확인
            {
                return 0; // 잘못된 ID 보유 수량 반환
            }

            int count = 0; // 장비 보유 수량 초기화

            foreach (EquipmentInstanceSaveData equipmentInstance in equipmentInventory) // 장비 인벤토리 순회
            {
                if (equipmentInstance != null && string.Equals(equipmentInstance.EquipmentId, equipmentId, StringComparison.Ordinal)) // 장비 원본 ID 일치 확인
                {
                    count++; // 동일 장비 보유 수량 증가
                }
            }

            return count; // 장비 보유 수량 반환
        }

        public bool TryCreateEquipmentInstance(string equipmentId, out EquipmentInstanceSaveData equipmentInstance, out string error) // 신규 장비 인스턴스 획득
        {
            EnsureDefaults(); // 저장 기본값 확인
            equipmentInstance = null; // 획득 장비 결과 초기화
            error = string.Empty; // 오류 문구 초기화

            if (string.IsNullOrWhiteSpace(equipmentId)) // 장비 원본 ID 확인
            {
                error = "장비 ID가 비어 있습니다."; // 빈 장비 ID 오류 설정
                return false; // 장비 획득 실패
            }

            string instanceId = CreateUniqueEquipmentInstanceId(); // 고유 장비 인스턴스 ID 생성

            if (!TryAddEquipmentInstance(instanceId, equipmentId, out error)) // 장비 인벤토리 추가 시도
            {
                return false; // 장비 획득 실패
            }

            equipmentInstance = FindEquipmentInstanceInternal(instanceId); // 추가된 장비 인스턴스 조회
            return equipmentInstance != null; // 장비 획득 결과 반환
        }

        public bool TryAddEquipmentInstance(string instanceId, string equipmentId, out string error) // 지정 ID 장비 인스턴스 추가
        {
            EnsureDefaults(); // 저장 기본값 확인
            error = string.Empty; // 오류 문구 초기화

            if (string.IsNullOrWhiteSpace(instanceId)) // 장비 인스턴스 ID 확인
            {
                error = "장비 인스턴스 ID가 비어 있습니다."; // 빈 인스턴스 ID 오류 설정
                return false; // 장비 추가 실패
            }

            if (string.IsNullOrWhiteSpace(equipmentId)) // 장비 원본 ID 확인
            {
                error = "장비 ID가 비어 있습니다."; // 빈 장비 ID 오류 설정
                return false; // 장비 추가 실패
            }

            if (FindEquipmentInstanceInternal(instanceId) != null) // 장비 인스턴스 중복 확인
            {
                error = $"이미 존재하는 장비 인스턴스 ID입니다. ID={instanceId}"; // 중복 인스턴스 오류 설정
                return false; // 장비 추가 실패
            }

            equipmentInventory.Add(new EquipmentInstanceSaveData(instanceId, equipmentId)); // 장비 인스턴스 인벤토리 추가
            return true; // 장비 추가 성공
        }

        public bool TryRemoveEquipmentInstance(string instanceId, out EquipmentInstanceSaveData removedInstance, out string error) // 장비 인스턴스 제거
        {
            EnsureDefaults(); // 저장 기본값 확인
            removedInstance = null; // 제거 장비 결과 초기화
            error = string.Empty; // 오류 문구 초기화

            if (string.IsNullOrWhiteSpace(instanceId)) // 장비 인스턴스 ID 확인
            {
                error = "장비 인스턴스 ID가 비어 있습니다."; // 빈 인스턴스 ID 오류 설정
                return false; // 장비 제거 실패
            }

            CharacterSaveData equippedCharacter = FindEquippedCharacterInternal(instanceId); // 장비 착용 캐릭터 조회

            if (equippedCharacter != null) // 장착 중 장비 확인
            {
                error = $"장착 중인 장비는 제거할 수 없습니다. Character={equippedCharacter.CharacterId}, Instance={instanceId}"; // 장착 중 제거 오류 설정
                return false; // 장착 중 장비 제거 차단
            }

            for (int index = 0; index < equipmentInventory.Count; index++) // 장비 인벤토리 순회
            {
                EquipmentInstanceSaveData equipmentInstance = equipmentInventory[index]; // 현재 장비 인스턴스 조회

                if (equipmentInstance == null || !string.Equals(equipmentInstance.InstanceId, instanceId, StringComparison.Ordinal)) // 대상 장비 인스턴스 확인
                {
                    continue; // 다음 장비 이동
                }

                removedInstance = equipmentInstance; // 제거 대상 장비 저장
                equipmentInventory.RemoveAt(index); // 장비 인벤토리에서 제거
                return true; // 장비 제거 성공
            }

            error = $"장비 인스턴스를 찾을 수 없습니다. ID={instanceId}"; // 미보유 장비 오류 설정
            return false; // 장비 제거 실패
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

        public void SetCurrentVitality(int value) // 현재 활력 변경
        {
            currentVitality = Mathf.Clamp(value, 0, MaxVitality); // 활력 범위 보정 후 저장
        }

        public RuneInstanceSaveData FindRune(string instanceId) // 룬 인스턴스 조회 (Day60 추가)
        {
            EnsureDefaults(); // 저장 기본값 확인

            for (int index = 0; index < runeInventory.Count; index++) // 룬 순회
            {
                if (string.Equals(runeInventory[index].InstanceId, instanceId, StringComparison.Ordinal)) // ID 비교
                {
                    return runeInventory[index]; // 룬 반환
                }
            }

            return null; // 조회 실패 반환
        }

        internal RuneInstanceSaveData AddRuneInternal(RuneKind kind, int grade) // 새 룬 추가 (Day60 추가, RuneService 전용)
        {
            EnsureDefaults(); // 저장 기본값 확인
            string instanceId; // 새 룬 ID

            do // 고유 ID 생성
            {
                instanceId = $"RUNE_{Guid.NewGuid():N}".ToUpperInvariant(); // GUID 기반 ID
            }
            while (FindRune(instanceId) != null); // 중복 시 재생성

            RuneInstanceSaveData rune = new RuneInstanceSaveData(instanceId, kind, grade); // 룬 생성
            runeInventory.Add(rune); // 목록 추가
            return rune; // 룬 반환
        }

        internal bool RemoveRuneInternal(string instanceId) // 룬 제거 (Day60 추가, RuneService 전용)
        {
            EnsureDefaults(); // 저장 기본값 확인
            return runeInventory.RemoveAll(rune => string.Equals(rune.InstanceId, instanceId, StringComparison.Ordinal)) > 0; // 제거 여부 반환
        }

        public ShopStateSaveData FindShopState(string shopId) // 상점 상태 조회 (Day61 추가, 없으면 null)
        {
            EnsureDefaults(); // 저장 기본값 확인

            for (int index = 0; index < shopStates.Count; index++) // 상태 순회
            {
                if (string.Equals(shopStates[index].ShopId, shopId, StringComparison.Ordinal)) // ID 비교
                {
                    return shopStates[index]; // 상태 반환
                }
            }

            return null; // 조회 실패 반환
        }

        public bool HasSeenDialogue(string dialogueId) => !string.IsNullOrEmpty(dialogueId) && seenDialogueIds != null && seenDialogueIds.Contains(dialogueId); // 본 대화 여부 (Day63 추가)

        public bool HasSeenMonster(string monsterId) => !string.IsNullOrEmpty(monsterId) && seenMonsterIds != null && seenMonsterIds.Contains(monsterId); // 만난 몬스터 여부 (Day63 추가)

        internal bool AddSeenDialogue(string dialogueId) // 본 대화 기록 (Day63 추가, 새로 기록하면 true)
        {
            EnsureDefaults(); // 목록 확인
            if (string.IsNullOrWhiteSpace(dialogueId) || seenDialogueIds.Contains(dialogueId)) return false; // 빈 ID·중복
            seenDialogueIds.Add(dialogueId); // 기록
            return true; // 새로 기록
        }

        internal bool AddSeenMonster(string monsterId) // 만난 몬스터 기록 (Day63 추가, 새로 기록하면 true)
        {
            EnsureDefaults(); // 목록 확인
            if (string.IsNullOrWhiteSpace(monsterId) || seenMonsterIds.Contains(monsterId)) return false; // 빈 ID·중복
            seenMonsterIds.Add(monsterId); // 기록
            return true; // 새로 기록
        }

        internal ShopStateSaveData GetOrCreateShopState(string shopId) // 상점 상태 조회·생성 (Day61 추가, ShopRotationService 전용)
        {
            ShopStateSaveData state = FindShopState(shopId); // 기존 상태

            if (state == null) // 첫 방문
            {
                state = new ShopStateSaveData(shopId); // 새 상태 생성
                shopStates.Add(state); // 목록 추가
            }

            return state; // 상태 반환
        }

        internal void SetBondResourceState(int resource, int refillDay) // 결속 자원·회복 일차 변경 (Day59 추가, BondService 전용)
        {
            bondResource = Mathf.Max(0, resource); // 자원 저장
            lastBondRefillDay = Mathf.Max(0, refillDay); // 회복 일차 저장
        }

        public void SetCurrentChapter(string value) // 현재 챕터 변경
        {
            currentChapter = value ?? string.Empty; // null 문자열 방지
        }

        public void SetCurrentMainQuest(string value) // 현재 목표 변경
        {
            currentMainQuest = value ?? string.Empty; // 목표 기본값 방지
        }

        private RegionErosionSaveData FindRegionErosionInternal(string regionId) // 내부 지역 침식도 조회 (Day44)
        {
            if (string.IsNullOrWhiteSpace(regionId)) // 지역 ID 확인
            {
                return null; // 빈 지역 ID 조회 실패 반환
            }

            foreach (RegionErosionSaveData entry in regionErosion) // 지역 침식도 목록 순회
            {
                if (entry != null && string.Equals(entry.RegionId, regionId, StringComparison.Ordinal)) // 지역 ID 비교
                {
                    return entry; // 일치 지역 침식도 반환
                }
            }

            return null; // 지역 침식도 조회 실패 반환
        }

        private void NormalizeRegionErosion() // 지역별 침식도 정규화 (Day44)
        {
            Dictionary<string, int> mergedLevels = new Dictionary<string, int>(StringComparer.Ordinal); // 지역 ID별 병합 침식도 생성

            for (int index = 0; index < regionErosion.Count; index++) // 지역 침식도 목록 순회
            {
                RegionErosionSaveData entry = regionErosion[index]; // 현재 지역 침식도 조회

                if (entry == null) // null 항목 확인
                {
                    continue; // 잘못된 항목 제외
                }

                entry.EnsureDefaults(); // 지역 침식도 기본값 복원

                if (string.IsNullOrWhiteSpace(entry.RegionId)) // 지역 ID 유효성 확인
                {
                    continue; // 빈 지역 ID 제외
                }

                mergedLevels[entry.RegionId] = entry.ErosionLevel; // 동일 지역 최신 침식도로 덮어쓰기
            }

            List<string> regionIds = new List<string>(mergedLevels.Keys); // 정규화 지역 ID 목록 생성
            regionIds.Sort(StringComparer.Ordinal); // 지역 ID 정렬
            regionErosion.Clear(); // 기존 지역 침식도 목록 제거

            for (int index = 0; index < regionIds.Count; index++) // 정렬 지역 ID 순회
            {
                string regionId = regionIds[index]; // 현재 지역 ID 조회
                regionErosion.Add(new RegionErosionSaveData(regionId, mergedLevels[regionId])); // 정규화 지역 침식도 추가
            }
        }

        private ItemStackSaveData FindItemStackInternal(string itemId) // 내부 일반 아이템 스택 조회
        {
            if (string.IsNullOrWhiteSpace(itemId)) // 아이템 ID 확인
            {
                return null; // 빈 아이템 ID 조회 실패 반환
            }

            foreach (ItemStackSaveData stack in itemInventory) // 일반 아이템 인벤토리 순회
            {
                if (stack != null && string.Equals(stack.ItemId, itemId, StringComparison.Ordinal)) // 아이템 ID 비교
                {
                    return stack; // 일치 아이템 스택 반환
                }
            }

            return null; // 일반 아이템 스택 조회 실패 반환
        }

        private void NormalizeItemInventory() // 일반 아이템 인벤토리 정규화
        {
            Dictionary<string, int> mergedCounts = new Dictionary<string, int>(StringComparer.Ordinal); // 아이템 ID별 병합 수량 생성

            for (int index = 0; index < itemInventory.Count; index++) // 일반 아이템 스택 순회
            {
                ItemStackSaveData stack = itemInventory[index]; // 현재 아이템 스택 조회

                if (stack == null) // null 스택 확인
                {
                    continue; // 잘못된 스택 제외
                }

                stack.EnsureDefaults(); // 아이템 스택 기본값 복원

                if (string.IsNullOrWhiteSpace(stack.ItemId) || stack.Quantity <= 0) // 아이템 스택 유효성 확인
                {
                    continue; // 빈 ID 및 0 수량 스택 제외
                }

                if (mergedCounts.TryGetValue(stack.ItemId, out int existingCount)) // 동일 아이템 기존 수량 확인
                {
                    mergedCounts[stack.ItemId] = existingCount + stack.Quantity; // 중복 아이템 수량 병합
                }
                else // 신규 아이템 ID 처리
                {
                    mergedCounts.Add(stack.ItemId, stack.Quantity); // 신규 아이템 수량 등록
                }
            }

            List<string> itemIds = new List<string>(mergedCounts.Keys); // 정규화 아이템 ID 목록 생성
            itemIds.Sort(StringComparer.Ordinal); // 아이템 ID 정렬
            itemInventory.Clear(); // 기존 일반 아이템 스택 제거

            for (int index = 0; index < itemIds.Count; index++) // 정렬 아이템 ID 순회
            {
                string itemId = itemIds[index]; // 현재 아이템 ID 조회
                itemInventory.Add(new ItemStackSaveData(itemId, mergedCounts[itemId])); // 정규화 아이템 스택 추가
            }
        }

        private EquipmentInstanceSaveData FindEquipmentInstanceInternal(string instanceId) // 내부 장비 인스턴스 조회
        {
            if (string.IsNullOrWhiteSpace(instanceId)) // 장비 인스턴스 ID 확인
            {
                return null; // 빈 ID 조회 실패 반환
            }

            foreach (EquipmentInstanceSaveData equipmentInstance in equipmentInventory) // 장비 인벤토리 순회
            {
                if (equipmentInstance != null && string.Equals(equipmentInstance.InstanceId, instanceId, StringComparison.Ordinal)) // 장비 인스턴스 ID 비교
                {
                    return equipmentInstance; // 일치 장비 인스턴스 반환
                }
            }

            return null; // 장비 인스턴스 조회 실패 반환
        }

        private CharacterSaveData FindEquippedCharacterInternal(string instanceId) // 내부 장비 착용 캐릭터 조회
        {
            if (string.IsNullOrWhiteSpace(instanceId)) // 장비 인스턴스 ID 확인
            {
                return null; // 빈 장비 인스턴스 미착용 반환
            }

            foreach (CharacterSaveData character in characters) // 캐릭터 저장 목록 순회
            {
                if (character != null && character.Equipment.ContainsInstance(instanceId)) // 장비 인스턴스 착용 여부 확인
                {
                    return character; // 장비 착용 캐릭터 반환
                }
            }

            return null; // 장비 미착용 반환
        }

        private string CreateUniqueEquipmentInstanceId() // 고유 장비 인스턴스 ID 생성
        {
            string instanceId; // 생성 장비 인스턴스 ID 선언

            do // 고유 ID 생성 반복
            {
                instanceId = $"{EquipmentInstancePrefix}{Guid.NewGuid():N}".ToUpperInvariant(); // GUID 기반 장비 인스턴스 ID 생성
            }
            while (FindEquipmentInstanceInternal(instanceId) != null); // 기존 인스턴스와 중복 시 재생성

            return instanceId; // 고유 장비 인스턴스 ID 반환
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
