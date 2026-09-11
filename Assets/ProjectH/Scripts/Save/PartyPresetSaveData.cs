using System; // 직렬화 기능
using System.Collections.Generic; // 목록 자료형
using UnityEngine; // Unity 기본 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역 (최적화 — SaveData.cs에서 분리)
{
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
}
