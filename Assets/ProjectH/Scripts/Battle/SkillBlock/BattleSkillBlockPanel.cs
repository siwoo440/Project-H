using System.Collections.Generic; // 목록 자료형
using UnityEngine; // Unity 기본 기능
using UnityEngine.EventSystems; // UI 드래그 포인터 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Battle.SkillBlock // 스킬 블록 전투 영역
{
    [DisallowMultipleComponent] // 중복 스킬 블록 패널 방지
    public sealed class BattleSkillBlockPanel : MonoBehaviour // 스킬 블록 표시·드래그·클릭 UI 패널
    {
        [SerializeField] private RectTransform blockLayer; // Runtime 블록 표시 레이어
        [SerializeField] private Text counterText; // 현재 블록 보유 수 텍스트
        [SerializeField] private Text hintText; // 블록 조작 안내 텍스트
        [SerializeField, Min(1)] private int maxSlotCount = 7; // UI 최대 블록 슬롯 수
        private readonly List<BattleSkillBlockView> views = new List<BattleSkillBlockView>(); // 생성된 Runtime 블록 View 목록
        private BattleSkillBlockController controller; // 스킬 블록 Runtime 컨트롤러
        private CanvasGroup panelCanvasGroup; // 패널 입력 제어 CanvasGroup
        private int currentBlockCount; // 현재 표시 블록 수
        private bool interactable; // 현재 블록 UI 입력 상태
        private bool lifecycleBlocked; // Disable 및 Destroy 중 Runtime UI 갱신 차단 상태

        private void OnEnable() // 스킬 블록 패널 활성화 처리
        {
            lifecycleBlocked = false; // Runtime UI 갱신 허용
        }

        private void OnDisable() // 스킬 블록 패널 비활성화 처리
        {
            lifecycleBlocked = true; // Scene 종료 및 비활성 중 Runtime UI 갱신 차단
            interactable = false; // 비활성 상태 입력 차단
        }

        public void Configure(RectTransform runtimeBlockLayer, Text counter, Text hint, int maximumSlots) // 에디터 생성 UI 참조 설정
        {
            blockLayer = runtimeBlockLayer; // Runtime 블록 레이어 연결
            counterText = counter; // 블록 개수 텍스트 연결
            hintText = hint; // 블록 안내 텍스트 연결
            maxSlotCount = Mathf.Max(1, maximumSlots); // 최대 슬롯 수 보정
        }

        public void BindController(BattleSkillBlockController runtimeController, int maximumSlots) // Runtime 블록 컨트롤러 연결
        {
            controller = runtimeController; // Runtime 블록 컨트롤러 저장
            maxSlotCount = Mathf.Max(1, maximumSlots); // Runtime 최대 슬롯 수 보정
            panelCanvasGroup = GetComponent<CanvasGroup>(); // 패널 CanvasGroup 조회

            if (panelCanvasGroup == null) // 패널 CanvasGroup 존재 확인
            {
                panelCanvasGroup = gameObject.AddComponent<CanvasGroup>(); // 패널 CanvasGroup 자동 추가
            }

            SetInteractable(false); // 전투 준비 전 블록 입력 차단
        }

        public void Refresh(IReadOnlyList<BattleSkillBlock> blocks) // Queue 데이터 기반 블록 UI 전체 갱신
        {
            if (lifecycleBlocked || !isActiveAndEnabled || blockLayer == null) // Scene 종료 및 UI 생명주기 상태 확인
            {
                return; // 종료 중 Runtime UI 재생성 및 접근 차단
            }

            RemoveDestroyedViews(); // Unity에서 이미 제거된 Runtime View 참조 정리
            currentBlockCount = blocks == null ? 0 : blocks.Count; // 현재 블록 수 저장
            EnsureViewCount(currentBlockCount); // 필요한 Runtime 블록 View 개수 확보

            for (int index = 0; index < views.Count; index++) // 전체 블록 View 순회
            {
                BattleSkillBlockView view = views[index]; // 현재 블록 View 조회

                if (view == null) // Unity Destroy 완료 View 참조 확인
                {
                    continue; // 파괴된 View 접근 차단
                }

                if (index >= currentBlockCount) // 현재 Queue 밖 View 확인
                {
                    view.gameObject.SetActive(false); // 사용하지 않는 블록 View 숨김
                    continue; // 다음 View 처리
                }

                BattleSkillBlock block = blocks[index]; // 현재 Queue 블록 조회
                BattleSkillChain chain = BattleSkillChainResolver.ResolveAtIndex(blocks, index); // 현재 블록 강화 그룹 판정
                view.gameObject.SetActive(true); // 현재 블록 View 표시
                view.Bind(this, index, block, chain.IsValid ? chain.EnhancementLevel : 1); // 스킬 종류와 강화도 UI 연결
                PlaceView(view.RectTransform, index); // 현재 Queue 순서 위치 배치
            }

            if (counterText != null) // 블록 보유 수 텍스트 확인
            {
                counterText.text = $"{currentBlockCount} / {maxSlotCount}"; // 현재 보유 블록 수 표시
            }

            if (hintText != null) // 블록 안내 텍스트 확인
            {
                hintText.text = currentBlockCount == 0 ? "블록 생성 대기" : "같은 SkillId를 붙인 뒤 블록을 클릭하여 강화 스킬 사용"; // 현재 블록 상태 안내 표시
            }
        }

        public void SetInteractable(bool enabled) // 스킬 블록 패널 입력 상태 설정
        {
            if (lifecycleBlocked) // 패널 종료 상태 확인
            {
                interactable = false; // 종료 상태 입력 차단
                return; // CanvasGroup 재접근 중단
            }

            interactable = enabled; // 블록 입력 상태 저장

            if (panelCanvasGroup == null) // 패널 CanvasGroup 확인
            {
                panelCanvasGroup = GetComponent<CanvasGroup>(); // 패널 CanvasGroup 재조회
            }

            if (panelCanvasGroup != null) // 패널 CanvasGroup 존재 확인
            {
                panelCanvasGroup.interactable = enabled; // 패널 Selectable 입력 상태 적용
                panelCanvasGroup.blocksRaycasts = enabled; // 패널 Raycast 입력 상태 적용
                panelCanvasGroup.alpha = enabled ? 1f : 0.82f; // 비활성 상태 시 패널 약한 투명도 적용
            }
        }

        public bool BeginBlockDrag(BattleSkillBlockView view) // 블록 드래그 시작 가능 여부 확인
        {
            return !lifecycleBlocked && interactable && controller != null && controller.CanInteract && view != null; // 현재 전투 및 입력 상태 기반 드래그 허용 반환
        }

        public void DragBlock(BattleSkillBlockView view, PointerEventData eventData) // 드래그 중 블록 화면 위치 이동
        {
            if (lifecycleBlocked || !interactable || view == null || eventData == null) // 드래그 입력 유효성 확인
            {
                return; // 블록 드래그 이동 중단
            }

            RectTransform rect = view.RectTransform; // 드래그 블록 RectTransform 조회

            if (rect != null) // 블록 RectTransform 존재 확인
            {
                rect.position = eventData.position; // 포인터 화면 위치에 블록 이동
            }
        }

        public void EndBlockDrag(BattleSkillBlockView view, PointerEventData eventData) // 블록 드롭 위치 기반 Queue 순서 변경
        {
            if (lifecycleBlocked) // 패널 종료 상태 확인
            {
                return; // 종료 중 드롭 처리 차단
            }

            if (controller == null || view == null || eventData == null) // 드롭 처리 참조 확인
            {
                Refresh(controller?.Queue?.Blocks); // 가능한 현재 Queue UI 복원
                return; // 블록 드롭 처리 중단
            }

            int targetIndex = CalculateDropIndex(eventData.position, eventData.pressEventCamera); // 포인터 위치 기반 Queue 대상 인덱스 계산

            if (!controller.MoveBlock(view.QueueIndex, targetIndex)) // Queue 블록 이동 시도
            {
                Refresh(controller.Queue?.Blocks); // 이동 실패 시 원래 Queue 위치 복원
            }
        }

        public void HandleBlockClick(BattleSkillBlockView view) // 블록 클릭 스킬 사용 처리
        {
            if (lifecycleBlocked || !interactable || controller == null || view == null) // 클릭 사용 가능 상태 확인
            {
                return; // 블록 클릭 사용 중단
            }

            controller.UseBlockAt(view.QueueIndex); // 클릭 블록 소속 강화 그룹 사용 요청
        }

        private int CalculateDropIndex(Vector2 screenPosition, Camera eventCamera) // 화면 포인터 위치를 Queue 인덱스로 변환
        {
            if (blockLayer == null || currentBlockCount <= 1) // 블록 레이어 및 블록 수 확인
            {
                return 0; // 단일 블록 또는 레이어 없음 첫 위치 반환
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(blockLayer, screenPosition, eventCamera, out Vector2 localPoint)) // 블록 레이어 로컬 좌표 변환
            {
                return Mathf.Clamp(currentBlockCount - 1, 0, currentBlockCount - 1); // 좌표 변환 실패 시 마지막 위치 반환
            }

            Rect rect = blockLayer.rect; // 블록 레이어 로컬 Rect 조회
            float normalized = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x); // 포인터 가로 위치 0~1 정규화
            int slotIndex = Mathf.FloorToInt(normalized * maxSlotCount); // 최대 슬롯 기준 드롭 슬롯 계산
            return Mathf.Clamp(slotIndex, 0, currentBlockCount - 1); // 현재 블록 수 기준 이동 인덱스 보정
        }

        private void RemoveDestroyedViews() // Unity Destroy 완료 Runtime View 참조 정리
        {
            for (int index = views.Count - 1; index >= 0; index--) // Runtime View 목록 역순 순회
            {
                if (views[index] == null) // Unity Destroy 완료 View 확인
                {
                    views.RemoveAt(index); // 파괴된 View 참조 목록 제거
                }
            }
        }

        private void EnsureViewCount(int requiredCount) // 필요한 Runtime 블록 View 개수 확보
        {
            if (lifecycleBlocked || blockLayer == null) // 종료 상태 및 Runtime 블록 레이어 확인
            {
                return; // 종료 중 Runtime View 생성 차단
            }

            while (views.Count < requiredCount) // 부족한 블록 View 생성 반복
            {
                views.Add(CreateBlockView(views.Count)); // 신규 Runtime 블록 View 생성 및 등록
            }
        }

        private BattleSkillBlockView CreateBlockView(int index) // Runtime 스킬 블록 View 생성
        {
            if (lifecycleBlocked || blockLayer == null) // 종료 상태 및 Runtime 블록 레이어 확인
            {
                return null; // 종료 중 Runtime View 생성 실패 반환
            }

            GameObject blockObject = new GameObject($"SkillBlock_{index}", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(BattleSkillBlockView)); // Runtime 블록 UI 객체 생성
            blockObject.transform.SetParent(blockLayer, false); // Runtime 블록 레이어 연결
            Image image = blockObject.GetComponent<Image>(); // 블록 배경 Image 조회
            image.color = new Color(0.26f, 0.30f, 0.38f, 1f); // 블록 기본 배경색 적용
            CanvasGroup canvasGroup = blockObject.GetComponent<CanvasGroup>(); // 블록 CanvasGroup 조회
            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text)); // 블록 라벨 객체 생성
            labelObject.transform.SetParent(blockObject.transform, false); // 블록 라벨 부모 연결
            Text label = labelObject.GetComponent<Text>(); // 블록 라벨 Text 조회
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 적용
            label.fontSize = 15; // 블록 라벨 기본 글자 크기 적용
            label.fontStyle = FontStyle.Bold; // 블록 라벨 굵게 표시
            label.color = Color.white; // 블록 라벨 흰색 적용
            label.alignment = TextAnchor.MiddleCenter; // 블록 라벨 중앙 정렬
            label.alignByGeometry = true; // 블록 라벨 글리프 기준 정렬
            label.resizeTextForBestFit = true; // 블록 라벨 자동 크기 적용
            label.resizeTextMinSize = 9; // 블록 라벨 최소 크기 적용
            label.resizeTextMaxSize = 15; // 블록 라벨 최대 크기 적용
            label.raycastTarget = false; // 블록 라벨 Raycast 비활성화
            Stretch(label.rectTransform, 4f); // 블록 라벨 전체 영역 확장
            BattleSkillBlockView view = blockObject.GetComponent<BattleSkillBlockView>(); // 블록 View 컴포넌트 조회
            view.ConfigureVisuals(image, label, canvasGroup); // 블록 View 시각 참조 연결
            return view; // 신규 블록 View 반환
        }

        private void PlaceView(RectTransform rect, int queueIndex) // Queue 인덱스 기반 블록 슬롯 배치
        {
            if (rect == null) // 블록 RectTransform 확인
            {
                return; // 블록 배치 중단
            }

            float slotWidth = 1f / maxSlotCount; // 슬롯당 정규화 너비 계산
            float minX = queueIndex * slotWidth; // 블록 최소 X 앵커 계산
            float maxX = minX + slotWidth; // 블록 최대 X 앵커 계산
            rect.anchorMin = new Vector2(minX, 0.08f); // 블록 최소 앵커 적용
            rect.anchorMax = new Vector2(maxX, 0.92f); // 블록 최대 앵커 적용
            rect.offsetMin = new Vector2(3f, 3f); // 블록 내부 최소 여백 적용
            rect.offsetMax = new Vector2(-3f, -3f); // 블록 내부 최대 여백 적용
            rect.localScale = Vector3.one; // 드래그 후 블록 스케일 복원
        }

        private static void Stretch(RectTransform rect, float padding) // RectTransform 전체 확장
        {
            rect.anchorMin = Vector2.zero; // 최소 앵커 전체 설정
            rect.anchorMax = Vector2.one; // 최대 앵커 전체 설정
            rect.offsetMin = new Vector2(padding, padding); // 최소 내부 여백 적용
            rect.offsetMax = new Vector2(-padding, -padding); // 최대 내부 여백 적용
        }
    }
}
