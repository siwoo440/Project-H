using ProjectH.Data; // 캐릭터 데이터 기능
using ProjectH.SaveSystem; // 캐릭터 저장 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 중복 뷰 방지
    public sealed class PartySlotView : MonoBehaviour // 파티 슬롯 표시 뷰
    {
        [SerializeField] private Button slotButton; // 슬롯 선택 버튼
        [SerializeField] private Image roleBadge; // 역할 배지 이미지
        [SerializeField] private Text roleText; // 역할 텍스트
        [SerializeField] private Text portraitText; // 초상화 자리 텍스트
        [SerializeField] private Text levelText; // 레벨 텍스트
        [SerializeField] private Text nameText; // 이름 텍스트
        [SerializeField] private Text hintText; // 슬롯 안내 텍스트
        public Button SlotButton => slotButton; // 슬롯 버튼 반환

        public void Configure(Button button, Image badge, Text role, Text portrait, Text level, Text displayName, Text hint) // 에디터 참조 설정
        {
            slotButton = button; // 슬롯 버튼 연결
            roleBadge = badge; // 역할 배지 연결
            roleText = role; // 역할 텍스트 연결
            portraitText = portrait; // 초상화 텍스트 연결
            levelText = level; // 레벨 텍스트 연결
            nameText = displayName; // 이름 텍스트 연결
            hintText = hint; // 안내 텍스트 연결
        }

        public void SetCharacter(CharacterData character, CharacterSaveData progress, bool interactable) // 캐릭터 슬롯 표시
        {
            if (character == null) // 캐릭터 데이터 확인
            {
                SetEmpty(interactable); // 빈 슬롯 표시
                return; // 캐릭터 표시 중단
            }

            int level = progress == null ? 1 : progress.Level; // 표시 레벨 결정
            SetText(roleText, GetRoleLabel(character.Position)); // 역할 텍스트 표시
            SetText(portraitText, character.DisplayName); // 임시 초상화 이름 표시
            SetText(levelText, $"LV.{level}"); // 캐릭터 레벨 표시
            SetText(nameText, character.DisplayName); // 캐릭터 이름 표시
            SetText(hintText, "클릭하여 교체"); // 슬롯 교체 안내 표시

            if (roleBadge != null) // 역할 배지 확인
            {
                roleBadge.color = GetRoleColor(character.Position); // 역할 배지 색상 적용
            }

            if (slotButton != null) // 슬롯 버튼 확인
            {
                slotButton.interactable = interactable; // 슬롯 선택 가능 상태 적용
            }
        }

        public void SetEmpty(bool interactable) // 빈 슬롯 표시
        {
            SetText(roleText, "+"); // 빈 역할 표시
            SetText(portraitText, "EMPTY SLOT"); // 빈 초상화 표시
            SetText(levelText, "--"); // 빈 레벨 표시
            SetText(nameText, "캐릭터 선택"); // 빈 이름 표시
            SetText(hintText, interactable ? "클릭하여 추가" : "앞 슬롯부터 편성"); // 빈 슬롯 안내 표시

            if (roleBadge != null) // 역할 배지 확인
            {
                roleBadge.color = new Color(0.55f, 0.60f, 0.67f, 1f); // 빈 슬롯 배지색 적용
            }

            if (slotButton != null) // 슬롯 버튼 확인
            {
                slotButton.interactable = interactable; // 빈 슬롯 선택 상태 적용
            }
        }

        public static Color GetRoleColor(BattlePosition position) // 역할 배지 색상 반환
        {
            switch (position) // 역할 종류 분기
            {
                case BattlePosition.Tank: // 탱커 역할 처리
                    return new Color(0.24f, 0.48f, 0.78f, 1f); // 탱커 파랑 반환
                case BattlePosition.Healer: // 힐러 역할 처리
                    return new Color(0.25f, 0.65f, 0.42f, 1f); // 힐러 초록 반환
                default: // 딜러 역할 처리
                    return new Color(0.76f, 0.31f, 0.38f, 1f); // 딜러 붉은색 반환
            }
        }

        public static string GetRoleLabel(BattlePosition position) // 역할 표시 이름 반환
        {
            switch (position) // 역할 종류 분기
            {
                case BattlePosition.Tank: // 탱커 역할 처리
                    return "TANK"; // 탱커 라벨 반환
                case BattlePosition.Healer: // 힐러 역할 처리
                    return "HEALER"; // 힐러 라벨 반환
                default: // 딜러 역할 처리
                    return "DEALER"; // 딜러 라벨 반환
            }
        }

        private static void SetText(Text target, string value) // 텍스트 안전 설정
        {
            if (target != null) // 텍스트 참조 확인
            {
                target.text = value; // 텍스트 값 적용
            }
        }
    }
}
