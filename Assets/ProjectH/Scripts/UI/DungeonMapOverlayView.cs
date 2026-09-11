using System.Collections.Generic; // 목록 자료형
using ProjectH.Battle.Rhythm; // 원형 스프라이트 생성 기능
using ProjectH.Core; // 게임 관리자 및 씬 이름 기능
using ProjectH.Data; // 던전 데이터 기능
using ProjectH.Dungeon; // 노드형 던전 탐험 기능
using ProjectH.SaveSystem; // 저장 데이터 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 중복 지도 오버레이 방지
    public sealed class DungeonMapOverlayView : MonoBehaviour // 가로형 던전 탐험 지도 오버레이 (Day55 신규, 던전 선택 씬 위 런타임 생성)
    {
        private const float NodeSizePixels = 96f; // 노드 원 크기(px)
        private const float EdgeThicknessPixels = 5f; // 연결선 두께(px)
        private const float MapPaddingX = 0.08f; // 지도 좌우 여백 비율
        private const float MapPaddingY = 0.14f; // 지도 상하 여백 비율
        private const float PulseSpeed = 4f; // 선택 가능 노드 맥동 속도
        private const float PulseAmount = 0.06f; // 선택 가능 노드 맥동 폭
        private static readonly Color DimColor = new Color(0.02f, 0.03f, 0.06f, 0.82f); // 배경 어둡게 색상
        private static readonly Color PanelColor = new Color(0.07f, 0.09f, 0.14f, 0.96f); // 지도 패널 색상
        private static readonly Color EdgeColor = new Color(0.55f, 0.62f, 0.75f, 0.55f); // 기본 연결선 색상
        private static readonly Color EdgeActiveColor = new Color(1f, 0.86f, 0.40f, 0.95f); // 이동 가능 연결선 색상
        private static readonly Color TextColor = new Color(0.93f, 0.95f, 1f, 1f); // 기본 문구 색상
        private static readonly Color AccentColor = new Color(1f, 0.86f, 0.40f, 1f); // 강조 문구 색상

        private static DungeonMapOverlayView instance; // 현재 지도 오버레이
        private readonly List<NodeView> nodeViews = new List<NodeView>(); // 노드 표시 목록
        private readonly List<EdgeView> edgeViews = new List<EdgeView>(); // 연결선 표시 목록
        private RectTransform mapArea; // 지도 노드 배치 영역
        private RectTransform edgeLayer; // 연결선 레이어 (노드 아래)
        private RectTransform nodeLayer; // 노드 레이어
        private Text titleText; // 제목 텍스트
        private Text statusText; // 하단 상태 텍스트
        private Button abandonButton; // 탐험 포기 버튼
        private GameObject popupRoot; // 결과·이벤트 팝업 루트
        private Text popupTitle; // 팝업 제목
        private Text popupBody; // 팝업 본문
        private readonly List<Button> popupButtons = new List<Button>(); // 팝업 버튼 목록 (최대 2개)
        private DungeonData dungeon; // 탐험 던전 데이터
        private bool transitioning; // 씬 전환 중 여부

        private sealed class NodeView // 노드 표시 참조
        {
            public DungeonMapNode Node; // 연결 노드
            public RectTransform Rect; // 노드 RectTransform
            public Image Disc; // 노드 원 이미지
            public Button Button; // 노드 버튼
            public Text Label; // 노드 종류 문구
            public Text State; // 노드 상태 문구
            public bool Selectable; // 선택 가능 여부
        }

        private sealed class EdgeView // 연결선 표시 참조
        {
            public int FromId; // 출발 노드 ID
            public int ToId; // 도착 노드 ID
            public Image Line; // 연결선 이미지
        }

        public static void StartRun(string dungeonId) // 신규 탐험 시작 및 지도 열기
        {
            DungeonData data = FindDungeon(dungeonId); // 던전 데이터 조회
            int floorCount = data == null ? 1 : data.MapFloorCount; // 던전 지정 층 수 조회 (없으면 보스 1층)
            DungeonRunState.Begin(dungeonId, floorCount, System.Environment.TickCount); // 시간 기반 시드로 탐험 시작 (입장마다 다른 지도)
            Open(); // 지도 열기
        }

        public static void Open() // 지도 오버레이 열기 (탐험 진행 중 또는 종료 요약 대기 시)
        {
            if (instance != null) // 기존 오버레이 확인
            {
                instance.Rebuild(); // 기존 오버레이 갱신
                return; // 중복 생성 차단
            }

            GameObject canvasObject = new GameObject("DungeonMapOverlayRuntime", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(DungeonMapOverlayView)); // 지도 Canvas 생성
            Canvas canvas = canvasObject.GetComponent<Canvas>(); // Canvas 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 Overlay 렌더링 설정
            canvas.sortingOrder = 450; // 던전 선택 UI 위 표시 설정
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // CanvasScaler 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 스케일 설정
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 해상도 설정
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응 방식 설정
            scaler.matchWidthOrHeight = 0.5f; // 가로 세로 중간 스케일 적용
            instance = canvasObject.GetComponent<DungeonMapOverlayView>(); // 오버레이 컴포넌트 조회
            instance.BuildFrame(); // 고정 프레임 구성
            instance.Rebuild(); // 지도 및 상태 구성
        }

        private static DungeonData FindDungeon(string dungeonId) // 던전 데이터 조회
        {
            return GameManager.Instance == null || GameManager.Instance.Data == null ? null : GameManager.Instance.Data.GetDungeon(dungeonId); // 데이터 관리자 기반 조회
        }

        private void BuildFrame() // 배경·패널·제목·상태·버튼·팝업 고정 프레임 구성
        {
            Image dim = RuntimeUiKit.CreateImage(transform, "Dim", DimColor); // 배경 어둡게 생성
            RuntimeUiKit.Stretch(dim.rectTransform); // 전체 화면 확장
            dim.raycastTarget = true; // 아래 던전 선택 화면 입력 차단

            Image panel = RuntimeUiKit.CreateImage(transform, "MapPanel", PanelColor); // 지도 패널 생성
            RuntimeUiKit.SetRect(panel.rectTransform, new Vector2(0.04f, 0.10f), new Vector2(0.96f, 0.90f)); // 지도 패널 배치
            panel.gameObject.AddComponent<Outline>().effectColor = new Color(1f, 0.86f, 0.40f, 0.35f); // 패널 금색 외곽선 적용

            titleText = CreateText(panel.transform, "Title", string.Empty, 34, AccentColor, TextAnchor.MiddleLeft); // 제목 생성
            RuntimeUiKit.SetRect(titleText.rectTransform, new Vector2(0.03f, 0.88f), new Vector2(0.70f, 0.98f)); // 제목 좌상단 배치

            Text direction = CreateText(panel.transform, "Direction", "입구  →  →  →  보스", 20, new Color(0.70f, 0.76f, 0.88f, 0.9f), TextAnchor.MiddleRight); // 진행 방향 안내 생성
            RuntimeUiKit.SetRect(direction.rectTransform, new Vector2(0.60f, 0.88f), new Vector2(0.97f, 0.98f)); // 진행 방향 우상단 배치

            GameObject mapObject = new GameObject("MapArea", typeof(RectTransform)); // 지도 영역 생성
            mapObject.transform.SetParent(panel.transform, false); // 패널 자식 연결
            mapArea = mapObject.GetComponent<RectTransform>(); // 지도 영역 저장
            RuntimeUiKit.SetRect(mapArea, new Vector2(0.02f, 0.14f), new Vector2(0.98f, 0.87f)); // 지도 영역 배치
            edgeLayer = CreateLayer(mapArea, "Edges"); // 연결선 레이어 생성 (먼저 생성해 노드 아래 렌더링)
            nodeLayer = CreateLayer(mapArea, "Nodes"); // 노드 레이어 생성

            statusText = CreateText(panel.transform, "Status", string.Empty, 20, TextColor, TextAnchor.MiddleLeft); // 하단 상태 생성
            RuntimeUiKit.SetRect(statusText.rectTransform, new Vector2(0.03f, 0.02f), new Vector2(0.78f, 0.12f)); // 하단 상태 배치

            abandonButton = CreateButton(panel.transform, "AbandonButton", "탐험 포기", new Color(0.45f, 0.16f, 0.18f, 0.95f)); // 탐험 포기 버튼 생성
            RuntimeUiKit.SetRect((RectTransform)abandonButton.transform, new Vector2(0.82f, 0.025f), new Vector2(0.97f, 0.11f)); // 포기 버튼 우하단 배치
            abandonButton.onClick.AddListener(HandleAbandonClicked); // 포기 이벤트 연결

            BuildPopup(); // 팝업 구성
        }

        private static RectTransform CreateLayer(RectTransform parent, string name) // 지도 영역 전체 레이어 생성
        {
            GameObject layer = new GameObject(name, typeof(RectTransform)); // 레이어 객체 생성
            layer.transform.SetParent(parent, false); // 지도 영역 자식 연결
            RectTransform rect = layer.GetComponent<RectTransform>(); // 레이어 RectTransform 조회
            RuntimeUiKit.Stretch(rect); // 지도 영역 전체 확장
            return rect; // 레이어 반환
        }

        private void BuildPopup() // 결과·이벤트 팝업 구성
        {
            Image blocker = RuntimeUiKit.CreateImage(transform, "PopupBlocker", new Color(0f, 0f, 0f, 0.55f)); // 팝업 뒤 입력 차단 배경 생성
            RuntimeUiKit.Stretch(blocker.rectTransform); // 전체 화면 확장
            popupRoot = blocker.gameObject; // 팝업 루트 저장

            Image box = RuntimeUiKit.CreateImage(blocker.transform, "PopupBox", new Color(0.10f, 0.12f, 0.19f, 0.98f)); // 팝업 상자 생성
            RuntimeUiKit.SetRect(box.rectTransform, new Vector2(0.30f, 0.30f), new Vector2(0.70f, 0.70f)); // 팝업 중앙 배치
            box.gameObject.AddComponent<Outline>().effectColor = new Color(1f, 0.86f, 0.40f, 0.5f); // 팝업 금색 외곽선 적용

            popupTitle = CreateText(box.transform, "PopupTitle", string.Empty, 30, AccentColor, TextAnchor.MiddleCenter); // 팝업 제목 생성
            RuntimeUiKit.SetRect(popupTitle.rectTransform, new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.95f)); // 팝업 제목 배치
            popupBody = CreateText(box.transform, "PopupBody", string.Empty, 22, TextColor, TextAnchor.MiddleCenter); // 팝업 본문 생성
            popupBody.horizontalOverflow = HorizontalWrapMode.Wrap; // 본문 줄바꿈 허용
            RuntimeUiKit.SetRect(popupBody.rectTransform, new Vector2(0.06f, 0.32f), new Vector2(0.94f, 0.78f)); // 팝업 본문 배치

            for (int index = 0; index < 2; index++) // 선택지 버튼 2개 생성
            {
                Button button = CreateButton(box.transform, $"PopupButton_{index}", string.Empty, new Color(0.20f, 0.30f, 0.52f, 0.95f)); // 팝업 버튼 생성
                popupButtons.Add(button); // 팝업 버튼 목록 등록
            }

            popupRoot.SetActive(false); // 초기 팝업 숨김
        }

        private void Rebuild() // 탐험 상태 기반 지도 전체 재구성
        {
            dungeon = FindDungeon(string.IsNullOrEmpty(DungeonRunState.DungeonId) ? DungeonSelectionRuntimeState.SelectedDungeonId : DungeonRunState.DungeonId); // 탐험 던전 데이터 조회
            titleText.text = dungeon == null ? "던전 탐험" : $"{dungeon.DisplayName} 탐험"; // 종료 요약에서도 던전 이름 표시

            if (!DungeonRunState.IsActive) // 탐험 종료 상태 확인
            {
                ShowEndSummary(DungeonRunState.ConsumeEndSummary()); // 종료 요약 표시
                return; // 지도 구성 중단
            }

            string dungeonName = dungeon == null ? DungeonRunState.DungeonId : dungeon.DisplayName; // 던전 표시 이름 결정
            titleText.text = $"{dungeonName} 탐험 · {DungeonRunState.Map.FloorCount}층"; // 제목 적용
            BuildMap(); // 노드·연결선 구성
            RefreshStates(); // 노드 상태 갱신
        }

        private void BuildMap() // 가로형 노드·연결선 배치 (왼쪽 입구 → 오른쪽 보스)
        {
            ClearChildren(edgeLayer); // 기존 연결선 제거
            ClearChildren(nodeLayer); // 기존 노드 제거
            nodeViews.Clear(); // 노드 표시 목록 초기화
            edgeViews.Clear(); // 연결선 표시 목록 초기화
            Canvas.ForceUpdateCanvases(); // 지도 영역 실제 크기 확정 (연결선 길이·각도 계산용)
            DungeonMap map = DungeonRunState.Map; // 탐험 지도 조회

            for (int index = 0; index < map.Nodes.Count; index++) // 전체 노드 순회
            {
                DungeonMapNode from = map.Nodes[index]; // 출발 노드 조회

                for (int next = 0; next < from.NextNodeIds.Count; next++) // 연결 노드 순회
                {
                    DungeonMapNode to = map.GetNode(from.NextNodeIds[next]); // 도착 노드 조회
                    edgeViews.Add(CreateEdge(from, to, map.FloorCount)); // 연결선 생성
                }
            }

            for (int index = 0; index < map.Nodes.Count; index++) // 전체 노드 순회
            {
                nodeViews.Add(CreateNode(map.Nodes[index], map.FloorCount)); // 노드 생성
            }
        }

        private Vector2 GetNodePosition(DungeonMapNode node, int floorCount) // 노드 지도 좌표 계산 (지도 영역 중심 기준 로컬 좌표)
        {
            Rect rect = mapArea.rect; // 지도 영역 실제 크기 조회
            float x = floorCount <= 1 ? 0.5f : Mathf.Lerp(MapPaddingX, 1f - MapPaddingX, node.Floor / (float)(floorCount - 1)); // 층 기반 가로 비율 (왼쪽→오른쪽)
            float y = Mathf.Lerp(1f - MapPaddingY, MapPaddingY, node.NormalizedLane); // 층 내 위치 기반 세로 비율 (위→아래)
            return new Vector2((x - 0.5f) * rect.width, (y - 0.5f) * rect.height); // 중심 기준 로컬 좌표 반환
        }

        private EdgeView CreateEdge(DungeonMapNode from, DungeonMapNode to, int floorCount) // 두 노드 사이 연결선 생성
        {
            Vector2 start = GetNodePosition(from, floorCount); // 출발 좌표 계산
            Vector2 end = GetNodePosition(to, floorCount); // 도착 좌표 계산
            Vector2 delta = end - start; // 방향 벡터 계산
            Image line = RuntimeUiKit.CreateImage(edgeLayer, $"Edge_{from.Id}_{to.Id}", EdgeColor); // 연결선 이미지 생성
            line.raycastTarget = false; // 연결선 클릭 차단 비활성화
            RectTransform rect = line.rectTransform; // 연결선 RectTransform 조회
            rect.anchorMin = new Vector2(0.5f, 0.5f); // 중심 앵커 설정
            rect.anchorMax = new Vector2(0.5f, 0.5f); // 중심 앵커 설정
            rect.pivot = new Vector2(0f, 0.5f); // 시작점 기준 피벗 설정
            rect.sizeDelta = new Vector2(delta.magnitude, EdgeThicknessPixels); // 연결선 길이·두께 적용
            rect.anchoredPosition = start; // 시작점 위치 적용
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg); // 방향 각도 적용
            return new EdgeView { FromId = from.Id, ToId = to.Id, Line = line }; // 연결선 참조 반환
        }

        private NodeView CreateNode(DungeonMapNode node, int floorCount) // 노드 원형 버튼 생성
        {
            GameObject nodeObject = new GameObject($"Node_{node.Id}", typeof(RectTransform), typeof(Image), typeof(Button)); // 노드 객체 생성
            nodeObject.transform.SetParent(nodeLayer, false); // 노드 레이어 자식 연결
            RectTransform rect = nodeObject.GetComponent<RectTransform>(); // 노드 RectTransform 조회
            rect.anchorMin = new Vector2(0.5f, 0.5f); // 중심 앵커 설정
            rect.anchorMax = new Vector2(0.5f, 0.5f); // 중심 앵커 설정
            rect.pivot = new Vector2(0.5f, 0.5f); // 중심 피벗 설정
            float size = node.Kind == DungeonNodeKind.Boss ? NodeSizePixels * 1.3f : NodeSizePixels; // 보스 노드 확대 크기 결정
            rect.sizeDelta = new Vector2(size, size); // 노드 크기 적용
            rect.anchoredPosition = GetNodePosition(node, floorCount); // 노드 위치 적용

            Image disc = nodeObject.GetComponent<Image>(); // 노드 원 이미지 조회
            disc.sprite = RhythmCircleSpriteFactory.GetDiscSprite(); // Day49 원형 스프라이트 재사용
            disc.alphaHitTestMinimumThreshold = 0.1f; // 원 모양 기준 클릭 판정
            Button button = nodeObject.GetComponent<Button>(); // 노드 버튼 조회
            button.targetGraphic = disc; // 버튼 대상 그래픽 연결
            button.transition = Selectable.Transition.None; // 기본 색 전환 비활성화 (상태 색상 직접 제어)
            int nodeId = node.Id; // 클로저용 노드 ID 복사
            button.onClick.AddListener(() => HandleNodeClicked(nodeId)); // 노드 클릭 이벤트 연결

            Text label = CreateText(nodeObject.transform, "Label", GetKindLabel(node.Kind), node.Kind == DungeonNodeKind.Boss ? 26 : 22, new Color(0.06f, 0.07f, 0.10f, 1f), TextAnchor.MiddleCenter); // 노드 종류 문구 생성
            RuntimeUiKit.Stretch(label.rectTransform); // 노드 전체 확장
            label.GetComponent<Outline>().enabled = false; // 원 안 문구는 외곽선 제거 (가독성)

            Text state = CreateText(nodeObject.transform, "State", string.Empty, 17, TextColor, TextAnchor.MiddleCenter); // 노드 상태 문구 생성
            RuntimeUiKit.SetRect(state.rectTransform, new Vector2(-0.4f, -0.42f), new Vector2(1.4f, -0.04f)); // 노드 아래 배치

            return new NodeView { Node = node, Rect = rect, Disc = disc, Button = button, Label = label, State = state }; // 노드 참조 반환
        }

        private void RefreshStates() // 노드·연결선·하단 상태 갱신
        {
            List<DungeonMapNode> selectable = DungeonRunState.GetSelectableNodes(); // 선택 가능 노드 조회

            for (int index = 0; index < nodeViews.Count; index++) // 노드 표시 순회
            {
                NodeView view = nodeViews[index]; // 노드 표시 조회
                bool isSelectable = Contains(selectable, view.Node.Id); // 선택 가능 여부 판정
                bool isCleared = DungeonRunState.IsCleared(view.Node.Id); // 완료 여부 판정
                bool isCurrent = DungeonRunState.CurrentNodeId == view.Node.Id; // 현재 위치 여부 판정
                Color kindColor = GetKindColor(view.Node.Kind); // 종류 색상 조회
                view.Selectable = isSelectable; // 선택 가능 저장
                view.Button.interactable = isSelectable; // 선택 가능 노드만 입력 허용
                view.Disc.color = isSelectable ? kindColor : isCleared ? new Color(kindColor.r * 0.45f, kindColor.g * 0.45f, kindColor.b * 0.45f, 0.85f) : new Color(kindColor.r, kindColor.g, kindColor.b, 0.35f); // 선택 가능 밝게, 완료 어둡게, 잠김 반투명
                view.Label.color = isSelectable || isCurrent ? new Color(0.06f, 0.07f, 0.10f, 1f) : new Color(0.06f, 0.07f, 0.10f, 0.6f); // 종류 문구 색상 적용
                view.State.text = isSelectable ? (DungeonRunState.PendingBattleNodeId == view.Node.Id ? "재도전" : "선택") : isCurrent ? "현재 위치" : isCleared ? "완료" : string.Empty; // 상태 문구 적용
                view.State.color = isSelectable ? AccentColor : TextColor; // 상태 문구 색상 적용
                view.Rect.localScale = Vector3.one; // 맥동 초기화
            }

            for (int index = 0; index < edgeViews.Count; index++) // 연결선 순회
            {
                EdgeView edge = edgeViews[index]; // 연결선 조회
                bool active = (edge.FromId == DungeonRunState.CurrentNodeId && DungeonRunState.PendingBattleNodeId < 0) && Contains(selectable, edge.ToId); // 현재 위치에서 이동 가능한 연결선 여부
                bool walked = DungeonRunState.IsCleared(edge.FromId) && (DungeonRunState.IsCleared(edge.ToId) || DungeonRunState.CurrentNodeId == edge.ToId); // 지나온 연결선 여부
                edge.Line.color = active ? EdgeActiveColor : walked ? new Color(0.85f, 0.88f, 0.95f, 0.85f) : EdgeColor; // 연결선 색상 적용
            }

            statusText.text = BuildStatusLine(); // 하단 상태 문구 적용
        }

        private static bool Contains(List<DungeonMapNode> nodes, int nodeId) // 노드 목록 ID 포함 여부 확인
        {
            for (int index = 0; index < nodes.Count; index++) // 노드 순회
            {
                if (nodes[index] != null && nodes[index].Id == nodeId) // ID 일치 확인
                {
                    return true; // 포함 반환
                }
            }

            return false; // 미포함 반환
        }

        private static string BuildStatusLine() // 하단 상태 문구 구성
        {
            string gold = DungeonRunState.CollectedGold >= 0 ? $"+{DungeonRunState.CollectedGold}" : DungeonRunState.CollectedGold.ToString(); // 획득 골드 부호 문구
            if (DungeonRunState.PendingModifiers.Count == 0) // 다음 전투 효과 존재 확인
            {
                return $"탐험 골드 {gold}   ·   다음 전투 효과 없음"; // 효과 없음 문구 반환
            }

            List<string> labels = new List<string>(); // 효과 문구 목록 생성

            for (int index = 0; index < DungeonRunState.PendingModifiers.Count; index++) // 다음 전투 효과 순회
            {
                DungeonRunModifier modifier = DungeonRunState.PendingModifiers[index]; // 효과 조회
                labels.Add(modifier.IsDebuff ? $"<color=#F07A7A>{modifier.Label}</color>" : $"<color=#7AD7F0>{modifier.Label}</color>"); // 불리 붉은색·유리 하늘색 문구 추가
            }

            return $"탐험 골드 {gold}   ·   다음 전투: {string.Join(", ", labels)}"; // 효과 포함 문구 반환
        }

        private void Update() // 선택 가능 노드 맥동 연출
        {
            float pulse = 1f + (Mathf.Sin(Time.unscaledTime * PulseSpeed) * PulseAmount); // 맥동 배율 계산

            for (int index = 0; index < nodeViews.Count; index++) // 노드 표시 순회
            {
                if (nodeViews[index].Selectable) // 선택 가능 노드 확인
                {
                    nodeViews[index].Rect.localScale = new Vector3(pulse, pulse, 1f); // 맥동 배율 적용
                }
            }
        }

        private void HandleNodeClicked(int nodeId) // 노드 클릭 처리
        {
            if (transitioning || popupRoot.activeSelf) // 씬 전환·팝업 표시 중 확인
            {
                return; // 입력 무시
            }

            DungeonMapNode node = DungeonRunState.SelectNode(nodeId); // 노드 선택 및 이동

            if (node == null) // 선택 실패 확인
            {
                return; // 처리 중단
            }

            switch (node.Kind) // 노드 종류 분기
            {
                case DungeonNodeKind.Battle: // 일반 전투 처리
                case DungeonNodeKind.Elite: // 정예 전투 처리
                case DungeonNodeKind.Boss: // 보스 전투 처리
                    EnterBattle(); // 전투 씬 진입
                    return; // 전투 처리 종료
                case DungeonNodeKind.Treasure: // 보물 처리
                    ResolveTreasure(); // 보물 획득 처리
                    break; // 보물 분기 종료
                case DungeonNodeKind.Trap: // 함정 처리
                    ResolveModifierNode("함정 발동!", DungeonEventCatalog.PickTrap(DungeonRunState.GetRandom()), "다음 전투에 불리한 효과를 받는다."); // 함정 효과 등록
                    break; // 함정 분기 종료
                case DungeonNodeKind.Rest: // 휴식 처리
                    ResolveModifierNode("휴식", DungeonEventCatalog.PickRest(DungeonRunState.GetRandom()), "다음 전투에 유리한 효과를 얻었다."); // 휴식 효과 등록
                    break; // 휴식 분기 종료
                case DungeonNodeKind.Event: // 이벤트 처리
                    ShowEvent(DungeonEventCatalog.PickEvent(DungeonRunState.GetRandom())); // 이벤트 팝업 표시
                    break; // 이벤트 분기 종료
            }

            RefreshStates(); // 이동 결과 반영
        }

        private void EnterBattle() // 전투 노드 전투 씬 진입
        {
            if (GameManager.Instance == null || GameManager.Instance.Scenes == null) // 씬 로더 확인
            {
                ShowMessage("오류", "SceneLoader를 찾을 수 없습니다."); // 오류 표시
                return; // 진입 중단
            }

            transitioning = true; // 중복 입력 잠금
            GameManager.Instance.Scenes.LoadScene(GameScenes.Battle); // 전투 씬 이동 (복귀 시 지도 자동 재오픈)
        }

        private void ResolveTreasure() // 보물 노드 골드 지급
        {
            int baseGold = dungeon == null ? 100 : dungeon.RewardGold; // 던전 기본 골드 조회
            int gold = Mathf.Max(1, Mathf.RoundToInt(baseGold * DungeonEventCatalog.RollTreasureGoldRatio(DungeonRunState.GetRandom()))); // 보물 골드 계산
            GrantGold(gold); // 골드 지급
            ShowMessage("보물 발견!", $"<color=#FFD966>+{gold} 골드</color>를 획득했다."); // 보물 결과 표시
        }

        private void ResolveModifierNode(string title, DungeonRunModifier modifier, string description) // 함정·휴식 다음 전투 효과 등록
        {
            DungeonRunState.AddPendingModifier(modifier); // 다음 전투 효과 등록
            string color = modifier.IsDebuff ? "#F07A7A" : "#7AD7F0"; // 효과 색상 결정
            ShowMessage(title, $"<color={color}>{modifier.Label}</color>\n{description}"); // 결과 표시
        }

        private void ShowEvent(DungeonEventDefinition eventDefinition) // 이벤트 선택지 팝업 표시
        {
            OpenPopup(eventDefinition.Title, eventDefinition.Description); // 팝업 열기

            for (int index = 0; index < popupButtons.Count; index++) // 팝업 버튼 순회
            {
                Button button = popupButtons[index]; // 버튼 조회
                bool hasChoice = index < eventDefinition.Choices.Count; // 선택지 존재 확인
                button.gameObject.SetActive(hasChoice); // 선택지 수만큼 버튼 표시

                if (!hasChoice) // 선택지 없음 확인
                {
                    continue; // 다음 버튼 처리
                }

                DungeonEventChoice choice = eventDefinition.Choices[index]; // 선택지 조회
                SetButtonLabel(button, choice.Label); // 선택지 문구 적용
                LayoutPopupButton(button, index, eventDefinition.Choices.Count); // 버튼 배치
                button.onClick.RemoveAllListeners(); // 기존 이벤트 제거
                button.onClick.AddListener(() => HandleEventChoice(choice)); // 선택 이벤트 연결
            }
        }

        private void HandleEventChoice(DungeonEventChoice choice) // 이벤트 선택지 결과 처리
        {
            int baseGold = dungeon == null ? 100 : dungeon.RewardGold; // 던전 기본 골드 조회
            int required = Mathf.RoundToInt(baseGold * choice.RequiredGoldRatio); // 필요 골드 계산

            if (required > 0 && GetCurrentGold() < required) // 골드 부족 확인
            {
                ShowMessage("골드 부족", $"필요 골드 {required}가 부족하다.\n아무 일도 일어나지 않았다."); // 부족 안내 표시
                return; // 처리 중단
            }

            DungeonOutcome outcome = choice.Roll(DungeonRunState.GetRandom()); // 확률 결과 결정
            string detail = ApplyOutcome(outcome, baseGold); // 결과 적용 및 상세 문구 생성
            ShowMessage(choice.Label, string.IsNullOrEmpty(detail) ? outcome.Message : $"{outcome.Message}\n{detail}"); // 결과 표시
        }

        private string ApplyOutcome(DungeonOutcome outcome, int baseGold) // 이벤트 결과 적용 (상세 문구 반환)
        {
            string detail = string.Empty; // 상세 문구 초기화
            int gold = Mathf.RoundToInt(baseGold * outcome.GoldRatio); // 골드 변동량 계산

            if (outcome.Kind == DungeonOutcomeKind.GainGold && gold > 0) // 골드 획득 확인
            {
                GrantGold(gold); // 골드 지급
                detail = $"<color=#FFD966>+{gold} 골드</color>"; // 획득 문구 설정
            }
            else if (outcome.Kind == DungeonOutcomeKind.SpendGold && gold > 0) // 골드 소모 확인
            {
                SpendGold(gold); // 골드 차감
                detail = $"<color=#F07A7A>-{gold} 골드</color>"; // 소모 문구 설정
            }

            if (outcome.Modifier != null) // 다음 전투 효과 존재 확인
            {
                DungeonRunState.AddPendingModifier(outcome.Modifier); // 다음 전투 효과 등록
                string color = outcome.Modifier.IsDebuff ? "#F07A7A" : "#7AD7F0"; // 효과 색상 결정
                detail = string.IsNullOrEmpty(detail) ? $"<color={color}>{outcome.Modifier.Label}</color>" : $"{detail}\n<color={color}>{outcome.Modifier.Label}</color>"; // 효과 문구 추가
            }

            return detail; // 상세 문구 반환
        }

        private static int GetCurrentGold() // 현재 보유 골드 조회
        {
            SaveData save = GameManager.Instance == null || GameManager.Instance.Save == null ? null : GameManager.Instance.Save.CurrentSave; // 저장 데이터 조회
            return save == null ? 0 : GoldCurrencyService.GetGold(save); // 보유 골드 반환
        }

        private static void GrantGold(int amount) // 골드 지급 및 저장
        {
            SaveData save = GameManager.Instance == null || GameManager.Instance.Save == null ? null : GameManager.Instance.Save.CurrentSave; // 저장 데이터 조회

            if (save == null) // 저장 데이터 확인
            {
                return; // 지급 중단
            }

            GoldCurrencyService.AddGold(save, amount); // 골드 증가
            GameManager.Instance.Save.SaveCurrent(); // 즉시 저장
            DungeonRunState.AddGold(amount); // 탐험 획득 골드 누적
        }

        private static void SpendGold(int amount) // 골드 차감 및 저장
        {
            SaveData save = GameManager.Instance == null || GameManager.Instance.Save == null ? null : GameManager.Instance.Save.CurrentSave; // 저장 데이터 조회

            if (save == null || !GoldCurrencyService.TrySpendGold(save, amount, out _)) // 저장 데이터 및 차감 성공 확인
            {
                return; // 차감 중단
            }

            GameManager.Instance.Save.SaveCurrent(); // 즉시 저장
            DungeonRunState.AddGold(-amount); // 탐험 골드 변동 누적
        }

        private void HandleAbandonClicked() // 탐험 포기 처리
        {
            if (transitioning || popupRoot.activeSelf) // 전환·팝업 중 확인
            {
                return; // 입력 무시
            }

            OpenPopup("탐험 포기", "지금까지 얻은 보상은 유지되지만,\n던전 클리어는 기록되지 않습니다.\n정말 포기할까요?"); // 확인 팝업 열기
            ConfigurePopupButton(0, "포기한다", 2, () => { DungeonRunState.Abandon(); ShowEndSummary(DungeonRunState.ConsumeEndSummary()); }); // 포기 확정 버튼
            ConfigurePopupButton(1, "계속 탐험", 2, ClosePopup); // 취소 버튼
        }

        private void ShowEndSummary(DungeonRunEndKind endKind) // 탐험 종료 요약 표시
        {
            int gold = DungeonRunState.CollectedGold; // 탐험 획득 골드 조회

            switch (endKind) // 종료 종류 분기
            {
                case DungeonRunEndKind.Cleared: // 클리어 처리
                    OpenPopup("던전 클리어!", $"보스를 격파했다.\n탐험 골드 <color=#FFD966>{gold:+#;-#;0}</color>"); // 클리어 요약
                    break; // 클리어 분기 종료
                case DungeonRunEndKind.Failed: // 실패 처리
                    OpenPopup("탐험 실패", $"파티가 쓰러졌다...\n탐험 골드 <color=#FFD966>{gold:+#;-#;0}</color>"); // 실패 요약
                    break; // 실패 분기 종료
                case DungeonRunEndKind.Abandoned: // 포기 처리
                    OpenPopup("탐험 종료", $"던전에서 철수했다.\n탐험 골드 <color=#FFD966>{gold:+#;-#;0}</color>"); // 포기 요약
                    break; // 포기 분기 종료
                default: // 요약 없음 처리
                    Close(); // 오버레이 닫기
                    return; // 처리 종료
            }

            ConfigurePopupButton(0, "확인", 1, Close); // 확인 시 오버레이 닫기
            popupButtons[1].gameObject.SetActive(false); // 두 번째 버튼 숨김
        }

        private void ShowMessage(string title, string body) // 단일 확인 팝업 표시
        {
            OpenPopup(title, body); // 팝업 열기
            ConfigurePopupButton(0, "확인", 1, ClosePopup); // 확인 버튼 구성
            popupButtons[1].gameObject.SetActive(false); // 두 번째 버튼 숨김
        }

        private void OpenPopup(string title, string body) // 팝업 열기
        {
            popupTitle.text = title; // 제목 적용
            popupBody.text = body; // 본문 적용
            popupRoot.transform.SetAsLastSibling(); // 최상단 렌더링
            popupRoot.SetActive(true); // 팝업 표시
        }

        private void ConfigurePopupButton(int index, string label, int buttonCount, UnityEngine.Events.UnityAction action) // 팝업 버튼 구성
        {
            Button button = popupButtons[index]; // 버튼 조회
            button.gameObject.SetActive(true); // 버튼 표시
            SetButtonLabel(button, label); // 문구 적용
            LayoutPopupButton(button, index, buttonCount); // 배치 적용
            button.onClick.RemoveAllListeners(); // 기존 이벤트 제거
            button.onClick.AddListener(action); // 신규 이벤트 연결
        }

        private static void LayoutPopupButton(Button button, int index, int buttonCount) // 버튼 수 기반 팝업 버튼 배치
        {
            RectTransform rect = (RectTransform)button.transform; // 버튼 RectTransform 조회

            if (buttonCount <= 1) // 단일 버튼 확인
            {
                RuntimeUiKit.SetRect(rect, new Vector2(0.32f, 0.07f), new Vector2(0.68f, 0.25f)); // 중앙 배치
                return; // 배치 종료
            }

            float left = index == 0 ? 0.06f : 0.52f; // 좌우 시작 위치 결정
            RuntimeUiKit.SetRect(rect, new Vector2(left, 0.07f), new Vector2(left + 0.42f, 0.25f)); // 좌우 나란히 배치
        }

        private void ClosePopup() // 팝업 닫기
        {
            popupRoot.SetActive(false); // 팝업 숨김
            RefreshStates(); // 지도 상태 갱신
        }

        private void Close() // 오버레이 닫기
        {
            DungeonRunState.ResetAll(); // 종료된 탐험 상태 정리
            Destroy(gameObject); // 오버레이 제거
        }

        private void OnDestroy() // 오버레이 제거 처리
        {
            if (instance == this) // 현재 오버레이 확인
            {
                instance = null; // 참조 해제
            }
        }

        private static void ClearChildren(RectTransform parent) // 자식 객체 전체 제거
        {
            for (int index = parent.childCount - 1; index >= 0; index--) // 자식 역순 순회
            {
                Destroy(parent.GetChild(index).gameObject); // 자식 제거
            }
        }

        private static string GetKindLabel(DungeonNodeKind kind) // 노드 종류 표시 문구 반환
        {
            switch (kind) // 종류 분기
            {
                case DungeonNodeKind.Battle: return "전투"; // 전투 문구
                case DungeonNodeKind.Elite: return "정예"; // 정예 문구
                case DungeonNodeKind.Event: return "이벤트"; // 이벤트 문구
                case DungeonNodeKind.Treasure: return "보물"; // 보물 문구
                case DungeonNodeKind.Trap: return "함정"; // 함정 문구
                case DungeonNodeKind.Rest: return "휴식"; // 휴식 문구
                default: return "보스"; // 보스 문구
            }
        }

        private static Color GetKindColor(DungeonNodeKind kind) // 노드 종류 색상 반환
        {
            switch (kind) // 종류 분기
            {
                case DungeonNodeKind.Battle: return new Color(0.86f, 0.88f, 0.94f, 1f); // 전투 밝은 회백색
                case DungeonNodeKind.Elite: return new Color(0.96f, 0.56f, 0.30f, 1f); // 정예 주황색
                case DungeonNodeKind.Event: return new Color(0.66f, 0.56f, 0.96f, 1f); // 이벤트 보라색
                case DungeonNodeKind.Treasure: return new Color(1f, 0.84f, 0.36f, 1f); // 보물 금색
                case DungeonNodeKind.Trap: return new Color(0.92f, 0.40f, 0.44f, 1f); // 함정 붉은색
                case DungeonNodeKind.Rest: return new Color(0.44f, 0.86f, 0.64f, 1f); // 휴식 초록색
                default: return new Color(0.86f, 0.24f, 0.28f, 1f); // 보스 진홍색
            }
        }

        private static Text CreateText(Transform parent, string name, string value, int fontSize, Color color, TextAnchor alignment) // 공통 텍스트 생성
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline)); // 텍스트 객체 생성
            textObject.transform.SetParent(parent, false); // 부모 연결
            Text text = textObject.GetComponent<Text>(); // Text 조회
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // 기본 폰트 적용
            text.text = value; // 문구 적용
            text.fontSize = fontSize; // 크기 적용
            text.fontStyle = FontStyle.Bold; // 굵기 적용
            text.color = color; // 색상 적용
            text.alignment = alignment; // 정렬 적용
            text.supportRichText = true; // 색상 태그 허용
            text.horizontalOverflow = HorizontalWrapMode.Overflow; // 가로 넘침 허용
            text.verticalOverflow = VerticalWrapMode.Overflow; // 세로 넘침 허용
            text.raycastTarget = false; // 텍스트 입력 비활성화 (버튼 클릭 방해 방지)
            Outline outline = textObject.GetComponent<Outline>(); // 외곽선 조회
            outline.effectColor = new Color(0.02f, 0.03f, 0.06f, 0.85f); // 어두운 외곽선 적용
            outline.effectDistance = new Vector2(1.5f, -1.5f); // 외곽선 두께 적용
            return text; // 텍스트 반환
        }

        private static Button CreateButton(Transform parent, string name, string label, Color color) // 공통 버튼 생성
        {
            Image image = RuntimeUiKit.CreateImage(parent, name, color); // 버튼 배경 생성
            image.raycastTarget = true; // 버튼 클릭 판정 활성화
            Button button = image.gameObject.AddComponent<Button>(); // 버튼 컴포넌트 추가
            button.targetGraphic = image; // 대상 그래픽 연결
            Text text = CreateText(image.transform, "Label", label, 22, Color.white, TextAnchor.MiddleCenter); // 버튼 문구 생성
            RuntimeUiKit.Stretch(text.rectTransform); // 버튼 전체 확장
            return button; // 버튼 반환
        }

        private static void SetButtonLabel(Button button, string label) // 버튼 문구 변경
        {
            Text text = button.GetComponentInChildren<Text>(true); // 버튼 문구 조회

            if (text != null) // 문구 존재 확인
            {
                text.text = label; // 문구 적용
            }
        }
    }
}
