using System; // 콜백 기능
using ProjectH.Data; // 캐릭터 데이터 기능
using ProjectH.SaveSystem; // 캐릭터 저장 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 중복 뷰 방지
    public sealed class PartyCharacterCardView : MonoBehaviour // 캐릭터 선택 카드 뷰
    {
        [SerializeField] private Button selectButton; // 캐릭터 선택 버튼
        [SerializeField] private Image roleBadge; // 역할 배지 이미지
        [SerializeField] private Text roleText; // 역할 표시 텍스트
        [SerializeField] private Text portraitText; // 초상화 자리 텍스트
        [SerializeField] private Text levelText; // 레벨 표시 텍스트
        [SerializeField] private Text nameText; // 이름 표시 텍스트
        [SerializeField] private Text stateText; // 편성 상태 텍스트

        public void ConfigureReferences(Button button, Image badge, Text role, Text portrait, Text level, Text displayName, Text state) // 에디터 참조 설정
        {
            selectButton = button; // 선택 버튼 연결
            roleBadge = badge; // 역할 배지 연결
            roleText = role; // 역할 텍스트 연결
            portraitText = portrait; // 초상화 텍스트 연결
            levelText = level; // 레벨 텍스트 연결
            nameText = displayName; // 이름 텍스트 연결
            stateText = state; // 상태 텍스트 연결
        }

        public void Bind(CharacterData character, CharacterSaveData progress, bool isCurrentSlot, bool isOtherPartySlot, Action<string> onSelected) // 캐릭터 카드 데이터 연결
        {
            if (character == null) // 캐릭터 데이터 확인
            {
                gameObject.SetActive(false); // 잘못된 카드 숨김
                return; // 카드 연결 중단
            }

            int level = progress == null ? 1 : progress.Level; // 표시 레벨 결정
            SetText(roleText, PartySlotView.GetRoleLabel(character.Position)); // 역할 표시
            SetText(portraitText, character.DisplayName); // 임시 초상화 이름 표시
            SetText(levelText, $"LV.{level}"); // 레벨 표시
            SetText(nameText, character.DisplayName); // 이름 표시
            SetText(stateText, isCurrentSlot ? "✓ 현재 선택" : isOtherPartySlot ? "● 편성 중" : "선택 가능"); // 편성 상태 표시

            if (roleBadge != null) // 역할 배지 확인
            {
                roleBadge.color = PartySlotView.GetRoleColor(character.Position); // 역할 배지 색상 적용
            }

            if (selectButton != null) // 선택 버튼 확인
            {
                selectButton.onClick.RemoveAllListeners(); // 이전 카드 콜백 제거
                selectButton.interactable = !isOtherPartySlot; // 다른 슬롯 캐릭터 선택 방지
                string characterId = character.Id; // 콜백 캐릭터 ID 복사
                selectButton.onClick.AddListener(() => onSelected?.Invoke(characterId)); // 캐릭터 선택 콜백 연결
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
