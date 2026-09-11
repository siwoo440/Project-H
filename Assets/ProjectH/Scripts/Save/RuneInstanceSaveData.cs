using System; // 직렬화 기능
using UnityEngine; // 직렬화 속성·수학 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    [Serializable] // JSON 직렬화 허용
    public sealed class RuneInstanceSaveData // 보유 룬 1개 (Day60 신규 — 장비처럼 개별 인스턴스, 레벨·잠금·장착 위치 포함)
    {
        [SerializeField] private string instanceId; // 룬 고유 ID
        [SerializeField] private int kind; // 룬 종류 (RuneKind)
        [SerializeField] private int grade = 1; // 등급 ★1~★3
        [SerializeField] private int level = 1; // 강화 레벨 1~10
        [SerializeField] private bool locked; // 잠금 (합성·분해 보호)
        [SerializeField] private string equippedCharacterId; // 장착 캐릭터 (빈 문자열이면 미장착)
        [SerializeField] private int slotIndex = -1; // 장착 슬롯 0~4 (-1 미장착)

        public string InstanceId => instanceId; // ID 반환
        public RuneKind Kind => (RuneKind)kind; // 종류 반환
        public int Grade => grade; // 등급 반환
        public int Level => level; // 레벨 반환
        public bool Locked => locked; // 잠금 반환
        public string EquippedCharacterId => equippedCharacterId ?? string.Empty; // 장착 캐릭터 반환
        public int SlotIndex => slotIndex; // 슬롯 반환
        public bool IsEquipped => !string.IsNullOrEmpty(equippedCharacterId) && slotIndex >= 0; // 장착 여부

        public RuneInstanceSaveData(string id, RuneKind runeKind, int runeGrade) // 새 룬 생성 (Lv.1)
        {
            instanceId = id ?? string.Empty; // ID 저장
            kind = (int)runeKind; // 종류 저장
            grade = Mathf.Clamp(runeGrade, 1, RuneCatalog.MaxGrade); // 등급 저장
            level = 1; // 레벨 1
            locked = false; // 잠금 해제
            equippedCharacterId = string.Empty; // 미장착
            slotIndex = -1; // 슬롯 없음
        }

        public void EnsureDefaults() // 기본값 보정
        {
            instanceId = instanceId ?? string.Empty; // ID 보정
            kind = Mathf.Clamp(kind, 0, RuneCatalog.KindCount - 1); // 종류 범위 보정
            grade = Mathf.Clamp(grade, 1, RuneCatalog.MaxGrade); // 등급 범위 보정
            level = Mathf.Clamp(level, 1, RuneCatalog.MaxLevel); // 레벨 범위 보정
            equippedCharacterId = equippedCharacterId ?? string.Empty; // 장착 캐릭터 보정

            if (string.IsNullOrEmpty(equippedCharacterId) || slotIndex < 0 || slotIndex >= RuneCatalog.SlotCount) // 잘못된 장착 정보 확인
            {
                Unequip(); // 장착 정보 해제
            }
        }

        internal void SetLevel(int value) => level = Mathf.Clamp(value, 1, RuneCatalog.MaxLevel); // 레벨 변경
        internal void SetLocked(bool value) => locked = value; // 잠금 변경

        internal void Equip(string characterId, int slot) // 장착 기록
        {
            equippedCharacterId = characterId ?? string.Empty; // 캐릭터 저장
            slotIndex = slot; // 슬롯 저장
        }

        internal void Unequip() // 장착 해제
        {
            equippedCharacterId = string.Empty; // 캐릭터 해제
            slotIndex = -1; // 슬롯 해제
        }
    }
}
