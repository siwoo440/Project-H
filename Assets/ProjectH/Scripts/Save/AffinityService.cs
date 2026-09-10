using UnityEngine; // Unity 기본 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    public enum AffinityTier // 호감도 5단계 등급 (0~100을 20%씩 구간 분할, Day43)
    {
        Stranger = 0, // 0~19 : 낯섦
        Acquaintance = 1, // 20~39 : 안면
        Friendly = 2, // 40~59 : 호감
        Trusted = 3, // 60~79 : 신뢰
        Bonded = 4 // 80~100 : 유대
    }

    public static class AffinityService // 캐릭터 호감도 공통 처리 기능 (Day43)
    {
        private const int TierRangeSize = 20; // 등급 구간 크기 (20%)

        public static int GetAffinity(SaveData saveData, string characterId) // 현재 호감도 조회
        {
            CharacterSaveData character = saveData == null ? null : saveData.FindCharacter(characterId); // 대상 캐릭터 진행 조회
            return character == null ? 0 : character.Affinity; // 캐릭터 미보유 시 0 반환
        }

        public static AffinityTier GetAffinityTier(SaveData saveData, string characterId) // 현재 호감도 등급 조회
        {
            return ResolveTier(GetAffinity(saveData, characterId)); // 조회 호감도 기반 등급 반환
        }

        public static int AddAffinity(SaveData saveData, string characterId, int delta) // 호감도 증감 (음수 입력 시 감소)
        {
            CharacterSaveData character = saveData == null ? null : saveData.FindCharacter(characterId); // 대상 캐릭터 진행 조회

            if (character == null) // 캐릭터 존재 확인
            {
                return 0; // 미보유 캐릭터 호감도 변경 없음 반환
            }

            character.SetAffinity(character.Affinity + delta); // 범위 보정 포함 호감도 변경
            return character.Affinity; // 변경 후 호감도 반환
        }

        public static AffinityTier ResolveTier(int affinity) // 호감도 수치를 등급으로 변환
        {
            int clamped = Mathf.Clamp(affinity, CharacterSaveData.MinAffinity, CharacterSaveData.MaxAffinity); // 등급 계산용 범위 보정
            int tierIndex = Mathf.Clamp(clamped / TierRangeSize, 0, 4); // 20% 단위 등급 인덱스 계산 (100은 4번 등급으로 보정)
            return (AffinityTier)tierIndex; // 등급 값 반환
        }
    }
}
