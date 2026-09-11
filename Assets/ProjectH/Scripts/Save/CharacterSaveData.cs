using System; // 직렬화 기능
using UnityEngine; // Unity 기본 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역 (최적화 — SaveData.cs에서 분리)
{
    [Serializable] // JSON 직렬화 허용
    public sealed class CharacterSaveData // 캐릭터 진행 저장 데이터
    {
        public const int MinAffinity = 0; // 호감도 최소값 (Day43)
        public const int MaxAffinity = 100; // 호감도 최대값 (Day43)
        [SerializeField] private string characterId; // 캐릭터 ID
        [SerializeField] private int level = 1; // 캐릭터 레벨
        [SerializeField] private int experience; // 캐릭터 경험치
        [SerializeField] private int affinity; // 캐릭터 호감도 (0~100)
        [SerializeField] private int affinityRewardClaimMask; // 수령한 호감도 단계 보상 비트 목록 (Day56 추가, 등급 번호별 1비트)
        [SerializeField] private int lastGiftDay; // 마지막으로 선물한 일차 (Day57 추가, 0이면 선물 기록 없음)
        [SerializeField] private int giftsGivenToday; // 마지막 선물 일차에 받은 선물 횟수 (Day57 추가)
        [SerializeField] private CharacterEquipmentSaveData equipment = new CharacterEquipmentSaveData(); // 캐릭터 장착 장비 저장
        public string CharacterId => characterId; // 캐릭터 ID 반환
        public int Level => level; // 레벨 반환
        public int Experience => experience; // 경험치 반환
        public int Affinity => affinity; // 호감도 반환
        public int AffinityRewardClaimMask => affinityRewardClaimMask; // 수령한 호감도 단계 보상 비트 목록 반환 (Day56 추가)
        public int LastGiftDay => lastGiftDay; // 마지막 선물 일차 반환 (Day57 추가)
        public CharacterEquipmentSaveData Equipment // 캐릭터 장착 장비 반환
        {
            get
            {
                EnsureDefaults(); // 캐릭터 저장 기본값 확인
                return equipment; // 장착 장비 저장 반환
            }
        }

        public CharacterSaveData(string id) // 캐릭터 저장 데이터 생성
        {
            characterId = id; // 캐릭터 ID 저장
            level = 1; // 초기 레벨 설정
            experience = 0; // 초기 경험치 설정
            affinity = 0; // 초기 호감도 설정
            lastGiftDay = 0; // 초기 선물 기록 없음 (Day57 추가)
            giftsGivenToday = 0; // 초기 선물 횟수 설정 (Day57 추가)
            equipment = new CharacterEquipmentSaveData(); // 초기 장착 장비 저장 생성
        }

        public void EnsureDefaults() // 캐릭터 저장 기본값 복원
        {
            if (characterId == null) // 캐릭터 ID null 확인
            {
                characterId = string.Empty; // 캐릭터 ID 기본값 복원
            }

            level = Mathf.Max(1, level); // 최소 레벨 복원
            experience = Mathf.Max(0, experience); // 최소 경험치 복원
            affinity = Mathf.Clamp(affinity, MinAffinity, MaxAffinity); // 호감도 범위 보정
            affinityRewardClaimMask = Mathf.Max(0, affinityRewardClaimMask); // 호감도 보상 수령 기록 음수 방지 (Day56 추가, 기존 저장은 0으로 시작)
            lastGiftDay = Mathf.Max(0, lastGiftDay); // 선물 일차 음수 방지 (Day57 추가, 기존 저장은 0으로 시작)
            giftsGivenToday = Mathf.Max(0, giftsGivenToday); // 선물 횟수 음수 방지 (Day57 추가)

            if (equipment == null) // 장착 장비 저장 확인
            {
                equipment = new CharacterEquipmentSaveData(); // 장착 장비 저장 복원
            }

            equipment.EnsureDefaults(); // 장착 장비 내부 기본값 복원
        }

        public void SetLevel(int value) // 레벨 변경
        {
            level = Mathf.Max(1, value); // 최소 레벨 보장
        }

        public void SetExperience(int value) // 경험치 변경
        {
            experience = Mathf.Max(0, value); // 음수 경험치 방지
        }

        public void SetAffinity(int value) // 호감도 변경
        {
            affinity = Mathf.Clamp(value, MinAffinity, MaxAffinity); // 호감도 범위 보정 후 저장
        }

        public bool IsAffinityRewardClaimed(AffinityTier tier) // 호감도 단계 보상 수령 여부 확인 (Day56 추가)
        {
            return (affinityRewardClaimMask & (1 << (int)tier)) != 0; // 등급 비트 확인
        }

        public void MarkAffinityRewardClaimed(AffinityTier tier) // 호감도 단계 보상 수령 기록 (Day56 추가)
        {
            affinityRewardClaimMask |= 1 << (int)tier; // 등급 비트 설정
        }

        public int GetGiftsGivenToday(int currentDay) // 지정 일차에 받은 선물 횟수 조회 (Day57 추가)
        {
            return lastGiftDay == currentDay ? giftsGivenToday : 0; // 날짜가 바뀌었으면 0회로 취급
        }

        public int RecordGift(int currentDay) // 선물 1회 기록 후 오늘 횟수 반환 (Day57 추가)
        {
            if (lastGiftDay != currentDay) // 날짜 변경 확인
            {
                lastGiftDay = currentDay; // 선물 일차 갱신
                giftsGivenToday = 0; // 새 날짜 횟수 초기화
            }

            giftsGivenToday++; // 오늘 선물 횟수 증가
            return giftsGivenToday; // 오늘 선물 횟수 반환
        }
    }
}
