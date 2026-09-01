using System; // 문자열 비교 기능
using System.Collections.Generic; // 목록 자료형
using ProjectH.SaveSystem; // 저장 데이터 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public sealed class PartyEditState // 파티 화면 임시 편집 상태
    {
        private readonly List<string>[] presets; // 편집 프리셋 목록
        private readonly HashSet<string> ownedCharacterIds; // 보유 캐릭터 ID 집합
        private int selectedPresetIndex; // 선택 프리셋 번호
        public int SelectedPresetIndex => selectedPresetIndex; // 선택 프리셋 번호 반환
        public int MemberCount => presets[selectedPresetIndex].Count; // 현재 파티 인원 반환
        public bool IsDirty { get; private set; } // 편집 변경 여부

        private PartyEditState(List<string>[] editablePresets, HashSet<string> ownedIds, int selectedIndex) // 편집 상태 생성
        {
            presets = editablePresets; // 프리셋 목록 저장
            ownedCharacterIds = ownedIds; // 보유 캐릭터 집합 저장
            selectedPresetIndex = selectedIndex; // 선택 프리셋 저장
            IsDirty = false; // 초기 변경 상태 설정
        }

        public static PartyEditState Create(SaveData saveData) // 저장 데이터 기반 편집 상태 생성
        {
            if (saveData == null) // 저장 데이터 확인
            {
                throw new ArgumentNullException(nameof(saveData)); // 저장 데이터 누락 예외 발생
            }

            saveData.EnsureDefaults(); // 저장 기본값 보정
            List<string>[] editablePresets = new List<string>[SaveData.PartyPresetCount]; // 편집 프리셋 배열 생성

            for (int index = 0; index < editablePresets.Length; index++) // 프리셋 목록 순회
            {
                editablePresets[index] = new List<string>(saveData.GetPartyPreset(index)); // 저장 프리셋 복사
            }

            HashSet<string> ownedIds = new HashSet<string>(StringComparer.Ordinal); // 보유 캐릭터 집합 생성

            foreach (CharacterSaveData character in saveData.Characters) // 보유 캐릭터 순회
            {
                if (character != null && !string.IsNullOrWhiteSpace(character.CharacterId)) // 캐릭터 저장 유효성 확인
                {
                    ownedIds.Add(character.CharacterId); // 보유 캐릭터 ID 등록
                }
            }

            return new PartyEditState(editablePresets, ownedIds, saveData.SelectedPartyPresetIndex); // 편집 상태 반환
        }

        public IReadOnlyList<string> GetSelectedMembers() // 현재 편성 목록 반환
        {
            return presets[selectedPresetIndex]; // 선택 프리셋 목록 반환
        }

        public string GetMemberAtSlot(int slotIndex) // 슬롯 캐릭터 ID 반환
        {
            List<string> members = presets[selectedPresetIndex]; // 현재 파티 목록 조회

            if (slotIndex < 0 || slotIndex >= members.Count) // 슬롯 범위 확인
            {
                return string.Empty; // 빈 슬롯 반환
            }

            return members[slotIndex]; // 슬롯 캐릭터 반환
        }

        public bool CanOpenSlot(int slotIndex) // 슬롯 선택 가능 여부 확인
        {
            return slotIndex >= 0 && slotIndex < SaveData.MaxPartySize && slotIndex <= MemberCount; // 기존 슬롯 또는 첫 빈 슬롯 허용
        }

        public int GetCharacterSlot(string characterId) // 현재 파티 캐릭터 슬롯 조회
        {
            List<string> members = presets[selectedPresetIndex]; // 현재 파티 목록 조회

            for (int index = 0; index < members.Count; index++) // 현재 파티 순회
            {
                if (string.Equals(members[index], characterId, StringComparison.Ordinal)) // 캐릭터 ID 비교
                {
                    return index; // 캐릭터 슬롯 반환
                }
            }

            return -1; // 캐릭터 미편성 반환
        }

        public bool TrySelectPreset(int presetIndex, out string error) // 편집 프리셋 선택
        {
            error = string.Empty; // 오류 문구 초기화

            if (presetIndex < 0 || presetIndex >= SaveData.PartyPresetCount) // 프리셋 범위 확인
            {
                error = "잘못된 편성 프리셋 번호입니다."; // 프리셋 오류 설정
                return false; // 프리셋 선택 실패
            }

            if (selectedPresetIndex == presetIndex) // 현재 프리셋 확인
            {
                return true; // 동일 프리셋 선택 성공
            }

            selectedPresetIndex = presetIndex; // 선택 프리셋 변경
            IsDirty = true; // 변경 상태 기록
            return true; // 프리셋 선택 성공
        }

        public bool TryAssignCharacter(int slotIndex, string characterId, out string error) // 슬롯 캐릭터 배정
        {
            error = string.Empty; // 오류 문구 초기화

            if (!CanOpenSlot(slotIndex)) // 슬롯 선택 가능 여부 확인
            {
                error = "앞쪽 빈 슬롯부터 캐릭터를 편성해 주세요."; // 슬롯 순서 오류 설정
                return false; // 캐릭터 배정 실패
            }

            if (string.IsNullOrWhiteSpace(characterId) || !ownedCharacterIds.Contains(characterId)) // 캐릭터 보유 여부 확인
            {
                error = $"보유하지 않은 캐릭터입니다. ID={characterId}"; // 미보유 오류 설정
                return false; // 캐릭터 배정 실패
            }

            List<string> members = presets[selectedPresetIndex]; // 현재 파티 목록 조회
            int existingSlot = GetCharacterSlot(characterId); // 기존 편성 슬롯 조회

            if (existingSlot >= 0 && existingSlot != slotIndex) // 다른 슬롯 편성 여부 확인
            {
                error = "이미 현재 편성에 포함된 캐릭터입니다."; // 중복 편성 오류 설정
                return false; // 캐릭터 배정 실패
            }

            if (existingSlot == slotIndex) // 현재 슬롯 동일 캐릭터 확인
            {
                return true; // 변경 없이 성공 반환
            }

            if (slotIndex < members.Count) // 기존 슬롯 교체 확인
            {
                members[slotIndex] = characterId; // 기존 슬롯 캐릭터 교체
            }
            else // 첫 빈 슬롯 추가 처리
            {
                members.Add(characterId); // 첫 빈 슬롯 캐릭터 추가
            }

            IsDirty = true; // 변경 상태 기록
            return true; // 캐릭터 배정 성공
        }

        public bool TryClearSlot(int slotIndex, out string error) // 파티 슬롯 비우기
        {
            error = string.Empty; // 오류 문구 초기화
            List<string> members = presets[selectedPresetIndex]; // 현재 파티 목록 조회

            if (slotIndex < 0 || slotIndex >= members.Count) // 기존 슬롯 범위 확인
            {
                error = "비울 캐릭터 슬롯이 없습니다."; // 빈 슬롯 오류 설정
                return false; // 슬롯 비우기 실패
            }

            if (members.Count <= 1) // 최소 파티 인원 확인
            {
                error = "파티에는 최소 1명의 캐릭터가 필요합니다."; // 최소 인원 오류 설정
                return false; // 슬롯 비우기 실패
            }

            members.RemoveAt(slotIndex); // 슬롯 캐릭터 제거 및 앞으로 정렬
            IsDirty = true; // 변경 상태 기록
            return true; // 슬롯 비우기 성공
        }

        public bool CommitTo(SaveData saveData, out string error) // 편집 상태 저장 데이터 반영
        {
            error = string.Empty; // 오류 문구 초기화

            if (saveData == null) // 저장 데이터 확인
            {
                error = "저장 데이터가 없습니다."; // 저장 데이터 오류 설정
                return false; // 편집 반영 실패
            }

            for (int index = 0; index < presets.Length; index++) // 프리셋 목록 순회
            {
                if (!saveData.TrySetPartyPreset(index, presets[index], out error)) // 프리셋 저장 반영
                {
                    return false; // 편집 반영 실패
                }
            }

            if (!saveData.TrySelectPartyPreset(selectedPresetIndex, out error)) // 활성 프리셋 저장 반영
            {
                return false; // 편집 반영 실패
            }

            IsDirty = false; // 변경 상태 초기화
            return true; // 편집 반영 성공
        }
    }
}
