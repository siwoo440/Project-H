using System; // 문자열 비교 기능
using System.Collections.Generic; // 목록 자료형
using ProjectH.Data; // 데이터 관리자 기능
using ProjectH.SaveSystem; // 저장 데이터 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public sealed class BattlePartyRuntime // 전투 파티 런타임
    {
        public const int MaxPartySize = 4; // 최대 파티 인원

        private readonly List<BattleStats> members; // 전투 파티 구성원

        public IReadOnlyList<BattleStats> Members => members; // 파티 구성원 반환
        public int Count => members.Count; // 파티 인원 반환
        public BattleStats this[int index] => members[index]; // 슬롯 캐릭터 반환

        private BattlePartyRuntime(List<BattleStats> battleMembers) // 전투 파티 생성
        {
            members = battleMembers; // 파티 구성원 저장
        }

        public static bool TryCreate(DataManager dataManager, SaveData saveData, out BattlePartyRuntime party, out string error) // 저장 데이터 기반 파티 생성
        {
            party = null; // 실패 기본 파티 설정
            error = string.Empty; // 실패 사유 초기화

            if (dataManager == null || !dataManager.IsInitialized) // 데이터 관리자 상태 확인
            {
                error = "DataManager is missing or not initialized."; // 데이터 관리자 실패 사유 설정
                return false; // 파티 생성 실패
            }

            if (saveData == null) // 저장 데이터 확인
            {
                error = "SaveData is missing."; // 저장 데이터 실패 사유 설정
                return false; // 파티 생성 실패
            }

            saveData.EnsureDefaults(); // 저장 기본값 보정

            if (saveData.PartyCharacterIds.Count == 0) // 빈 파티 확인
            {
                error = "Party has no characters."; // 빈 파티 실패 사유 설정
                return false; // 파티 생성 실패
            }

            if (saveData.PartyCharacterIds.Count > MaxPartySize) // 최대 파티 인원 확인
            {
                error = $"Party cannot exceed {MaxPartySize} characters."; // 최대 파티 실패 사유 설정
                return false; // 파티 생성 실패
            }

            List<BattleStats> battleMembers = new List<BattleStats>(saveData.PartyCharacterIds.Count); // 런타임 파티 목록 생성
            HashSet<string> usedCharacterIds = new HashSet<string>(StringComparer.Ordinal); // 중복 캐릭터 검사 집합

            for (int index = 0; index < saveData.PartyCharacterIds.Count; index++) // 저장 파티 순회
            {
                string characterId = saveData.PartyCharacterIds[index]; // 현재 캐릭터 ID 조회

                if (string.IsNullOrWhiteSpace(characterId)) // 캐릭터 ID 유효성 확인
                {
                    error = $"Party slot {index} has an empty character ID."; // 빈 캐릭터 ID 실패 사유 설정
                    return false; // 파티 생성 실패
                }

                if (!usedCharacterIds.Add(characterId)) // 캐릭터 중복 확인
                {
                    error = $"Duplicate party character ID: {characterId}."; // 중복 캐릭터 실패 사유 설정
                    return false; // 파티 생성 실패
                }

                CharacterData characterData = dataManager.GetCharacter(characterId); // 캐릭터 원본 데이터 조회

                if (characterData == null) // 캐릭터 원본 존재 확인
                {
                    error = $"CharacterData not found: {characterId}."; // 원본 누락 실패 사유 설정
                    return false; // 파티 생성 실패
                }

                CharacterSaveData characterSave = saveData.FindCharacter(characterId); // 캐릭터 저장 데이터 조회

                if (characterSave == null) // 캐릭터 저장 데이터 존재 확인
                {
                    error = $"CharacterSaveData not found: {characterId}."; // 저장 데이터 누락 실패 사유 설정
                    return false; // 파티 생성 실패
                }

                string runtimeId = $"ALLY_{index}"; // 전투 인스턴스 ID 생성
                BattleStats stats = BattleStatsFactory.CreateCharacter(characterData, characterSave, runtimeId); // 캐릭터 런타임 스탯 생성
                battleMembers.Add(stats); // 전투 파티 구성원 추가
            }

            party = new BattlePartyRuntime(battleMembers); // 런타임 파티 생성
            return true; // 파티 생성 성공
        }

        public BattleStats FindByRuntimeId(string runtimeId) // 런타임 ID로 구성원 조회
        {
            foreach (BattleStats member in members) // 파티 구성원 순회
            {
                if (string.Equals(member.RuntimeId, runtimeId, StringComparison.Ordinal)) // 런타임 ID 비교
                {
                    return member; // 일치 구성원 반환
                }
            }

            return null; // 조회 실패 반환
        }

        public BattleStats FindByCharacterId(string characterId) // 캐릭터 ID로 구성원 조회
        {
            foreach (BattleStats member in members) // 파티 구성원 순회
            {
                if (string.Equals(member.CharacterId, characterId, StringComparison.Ordinal)) // 캐릭터 ID 비교
                {
                    return member; // 일치 구성원 반환
                }
            }

            return null; // 조회 실패 반환
        }

        public void RestoreAll() // 파티 전체 체력 복원
        {
            foreach (BattleStats member in members) // 파티 구성원 순회
            {
                member.RestoreFullHp(); // 구성원 전체 회복
            }
        }
    }
}
