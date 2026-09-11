using System; // 직렬화·수학 기능
using UnityEngine; // Unity 직렬화 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    [Serializable] // JSON 직렬화 허용
    public sealed class EquipmentInstanceSaveData // 보유 장비 인스턴스 저장 데이터
    {
        [SerializeField] private string instanceId; // 장비 인스턴스 고유 ID
        [SerializeField] private string equipmentId; // 장비 원본 데이터 ID
        [SerializeField] private int enhanceLevel; // 강화 단계 +0~+5 (Day61 추가)
        [SerializeField] private int transcendStage; // 초월 단계 ★1~★3 (Day61 추가, 기존 세이브 0은 ★1로 취급)
        [SerializeField] private int failStreak; // 연속 강화 실패 횟수 (Day61 추가, 보정 확률 누적)

        public string InstanceId => instanceId; // 장비 인스턴스 ID 반환
        public string EquipmentId => equipmentId; // 장비 원본 ID 반환
        public int EnhanceLevel => Math.Max(0, enhanceLevel); // 강화 단계 반환 (Day61 추가)
        public int TranscendStage => Math.Max(1, transcendStage); // 초월 단계 반환 (Day61 추가, 최소 ★1)
        public int FailStreak => Math.Max(0, failStreak); // 연속 실패 반환 (Day61 추가)

        public EquipmentInstanceSaveData(string newInstanceId, string newEquipmentId) // 장비 인스턴스 저장 데이터 생성
        {
            instanceId = newInstanceId ?? string.Empty; // 장비 인스턴스 ID 저장
            equipmentId = newEquipmentId ?? string.Empty; // 장비 원본 ID 저장
            enhanceLevel = 0; // +0 시작 (Day61 추가)
            transcendStage = 1; // ★1 시작 (Day61 추가)
            failStreak = 0; // 실패 기록 없음 (Day61 추가)
        }

        internal void SetEnhanceLevel(int value) => enhanceLevel = Mathf.Clamp(value, 0, EquipmentUpgradeCatalog.MaxEnhanceLevel); // 강화 단계 변경 (Day61 추가)
        internal void SetTranscendStage(int value) => transcendStage = Mathf.Clamp(value, 1, EquipmentUpgradeCatalog.MaxTranscendStage); // 초월 단계 변경 (Day61 추가)
        internal void SetFailStreak(int value) => failStreak = Mathf.Max(0, value); // 연속 실패 변경 (Day61 추가)
    }
}
