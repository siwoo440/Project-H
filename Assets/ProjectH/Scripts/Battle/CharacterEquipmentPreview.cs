using ProjectH.Data; // 장비 슬롯 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public sealed class CharacterEquipmentPreview // 장비 변경 전후 능력치 미리보기
    {
        public BattleStats CurrentStats { get; } // 현재 최종 전투 능력치
        public BattleStats PreviewStats { get; } // 변경 후 예상 전투 능력치
        public EquipmentSlot Slot { get; } // 변경 대상 장비 슬롯
        public string SelectedInstanceId { get; } // 선택 장비 인스턴스 ID
        public string ReplacedInstanceId { get; } // 기존 장착 장비 인스턴스 ID
        public bool WillUnequip { get; } // 해제 동작 여부
        public bool WillReplace => !WillUnequip && !string.IsNullOrWhiteSpace(ReplacedInstanceId); // 교체 동작 여부
        public string ActionLabel => WillUnequip ? "해제" : WillReplace ? "교체" : "장착"; // 장비 액션 문구

        public CharacterEquipmentPreview(BattleStats currentStats, BattleStats previewStats, EquipmentSlot slot, string selectedInstanceId, string replacedInstanceId, bool willUnequip) // 장비 미리보기 생성
        {
            CurrentStats = currentStats; // 현재 능력치 저장
            PreviewStats = previewStats; // 예상 능력치 저장
            Slot = slot; // 변경 슬롯 저장
            SelectedInstanceId = selectedInstanceId ?? string.Empty; // 선택 인스턴스 저장
            ReplacedInstanceId = replacedInstanceId ?? string.Empty; // 기존 인스턴스 저장
            WillUnequip = willUnequip; // 해제 여부 저장
        }
    }
}
