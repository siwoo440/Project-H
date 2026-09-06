using UnityEngine; // Unity 기본 기능
using UnityEngine.EventSystems; // UI 포인터 및 드래그 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Battle.SkillBlock // 스킬 블록 전투 영역
{
    [DisallowMultipleComponent] // 중복 스킬 블록 View 방지
    public sealed class BattleSkillBlockView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler // 클릭·드래그 가능한 단일 스킬 블록 View
    {
        [SerializeField] private Image backgroundImage; // 블록 배경 이미지
        [SerializeField] private Text labelText; // 블록 스킬 및 강화도 텍스트
        [SerializeField] private CanvasGroup canvasGroup; // 드래그 입력 및 투명도 제어
        private BattleSkillBlockPanel panel; // 소속 스킬 블록 패널
        private bool dragging; // 현재 드래그 상태
        private bool movedDuringDrag; // 실제 드래그 이동 발생 상태
        public int QueueIndex { get; private set; } // 현재 Queue 인덱스
        public BattleSkillBlock Block { get; private set; } // 연결된 블록 Runtime 데이터
        public RectTransform RectTransform => transform as RectTransform; // 블록 RectTransform 반환

        public void ConfigureVisuals(Image background, Text label, CanvasGroup group) // 블록 View 시각 참조 설정
        {
            backgroundImage = background; // 블록 배경 이미지 연결
            labelText = label; // 블록 라벨 텍스트 연결
            canvasGroup = group; // 블록 CanvasGroup 연결
        }

        public void Bind(BattleSkillBlockPanel ownerPanel, int queueIndex, BattleSkillBlock block, int enhancementLevel) // Queue 데이터와 블록 View 연결
        {
            panel = ownerPanel; // 소속 패널 저장
            QueueIndex = queueIndex; // Queue 인덱스 저장
            Block = block; // 블록 Runtime 데이터 저장
            dragging = false; // 드래그 상태 초기화
            movedDuringDrag = false; // 드래그 이동 상태 초기화
            SetDraggingVisual(false); // 기본 블록 시각 상태 적용

            if (labelText != null) // 블록 라벨 텍스트 확인
            {
                string displayName = block == null || block.Skill == null ? block?.SkillId ?? "SKILL" : block.Skill.DisplayName; // 블록 스킬 표시 이름 계산
                labelText.text = $"{displayName}\n강화 {Mathf.Clamp(enhancementLevel, 1, 3)}"; // 스킬 종류와 강화도 분리 표시
            }

            if (backgroundImage != null) // 블록 배경 이미지 확인
            {
                backgroundImage.color = GetEnhancementColor(enhancementLevel); // 강화도 기반 임시 블록 강조 적용
            }
        }

        public void OnPointerClick(PointerEventData eventData) // 블록 클릭 사용 처리
        {
            if (dragging || movedDuringDrag || panel == null) // 드래그 및 패널 상태 확인
            {
                return; // 드래그 종료 클릭 오작동 차단
            }

            panel.HandleBlockClick(this); // 현재 블록 강화 그룹 사용 요청
        }

        public void OnBeginDrag(PointerEventData eventData) // 블록 드래그 시작 처리
        {
            if (panel == null || !panel.BeginBlockDrag(this)) // 패널 드래그 시작 가능 여부 확인
            {
                return; // 블록 드래그 시작 중단
            }

            dragging = true; // 블록 드래그 상태 활성화
            movedDuringDrag = false; // 실제 이동 상태 초기화
            SetDraggingVisual(true); // 드래그 시각 상태 적용
            transform.SetAsLastSibling(); // 드래그 블록 최상단 표시
        }

        public void OnDrag(PointerEventData eventData) // 블록 드래그 이동 처리
        {
            if (!dragging || panel == null) // 현재 드래그 상태 확인
            {
                return; // 블록 드래그 이동 중단
            }

            movedDuringDrag = true; // 실제 드래그 이동 기록
            panel.DragBlock(this, eventData); // 패널 좌표 기준 블록 이동 처리
        }

        public void OnEndDrag(PointerEventData eventData) // 블록 드래그 종료 처리
        {
            if (!dragging || panel == null) // 현재 드래그 상태 확인
            {
                return; // 블록 드래그 종료 중단
            }

            dragging = false; // 블록 드래그 상태 비활성화
            SetDraggingVisual(false); // 기본 블록 시각 상태 복원
            panel.EndBlockDrag(this, eventData); // 드롭 위치 기반 Queue 순서 변경
        }

        private void SetDraggingVisual(bool isDragging) // 블록 드래그 시각 상태 설정
        {
            if (canvasGroup == null) // CanvasGroup 존재 확인
            {
                return; // 드래그 시각 설정 중단
            }

            canvasGroup.alpha = isDragging ? 0.72f : 1f; // 드래그 중 반투명 표시
            canvasGroup.blocksRaycasts = !isDragging; // 드래그 중 자기 자신 Raycast 차단
        }

        private static Color GetEnhancementColor(int enhancementLevel) // 강화도별 임시 블록 색상 반환
        {
            if (enhancementLevel >= 3) // 강화도 3 확인
            {
                return new Color(0.80f, 0.64f, 0.28f, 1f); // 강화도 3 강조색 반환
            }

            if (enhancementLevel == 2) // 강화도 2 확인
            {
                return new Color(0.34f, 0.56f, 0.82f, 1f); // 강화도 2 강조색 반환
            }

            return new Color(0.26f, 0.30f, 0.38f, 1f); // 강화도 1 기본색 반환
        }
    }
}
