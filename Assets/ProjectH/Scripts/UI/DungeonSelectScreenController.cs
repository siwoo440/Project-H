using System.Collections.Generic; // 목록 자료형
using ProjectH.Battle; // 던전 진행 상태 기능
using ProjectH.Battle.Rhythm; // 원형 스프라이트 기능
using ProjectH.Core; // 프로젝트 핵심 기능
using ProjectH.Data; // 프로젝트 데이터 기능
using ProjectH.Dialogue; // 첫 방문 대사 파일 기능
using ProjectH.Dungeon; // 던전 입장 비용 · 모험 지역 기능
using ProjectH.SaveSystem; // 저장 및 지역 침식도 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // Unity 씬 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 중복 컨트롤러 방지
    public sealed class DungeonSelectScreenController : MonoBehaviour // 모험 지도 화면 (Day26 던전 선택 → Day63 목업 10번 세계 지도로 교체 : 지역 → 던전 → 탐험 시작)
    {
        private static readonly Color BarColor = new Color(0.05f, 0.07f, 0.10f, 0.90f); // 상단 띠
        private static readonly Color PanelColor = new Color(0.06f, 0.07f, 0.10f, 0.92f); // 오른쪽 패널
        private static readonly Color CardColor = new Color(0.15f, 0.20f, 0.28f, 1f); // 던전 카드
        private static readonly Color SelectedCardColor = new Color(0.26f, 0.36f, 0.50f, 1f); // 선택 카드
        private static readonly Color LockedCardColor = new Color(0.16f, 0.17f, 0.19f, 1f); // 잠긴 카드
        private static readonly Color GoldColor = new Color(0.96f, 0.76f, 0.30f, 1f); // 강조 금색
        private static readonly Color HintColor = new Color(0.80f, 0.83f, 0.88f, 1f); // 안내 글자
        private static readonly Color ActionColor = new Color(0.78f, 0.50f, 0.18f, 1f); // 실행 버튼 주황
        private const float MapRight = 0.655f; // 지도 영역 오른쪽 끝
        private const float MapTop = 0.915f; // 지도 영역 위쪽 끝

        private sealed class RegionMarker // 지도 위 지역 표시
        {
            public AdventureRegion Region; // 지역
            public Image Disc; // 원
            public Outline Ring; // 선택 테두리
            public Text Label; // 이름
        }

        private readonly List<RegionMarker> markers = new List<RegionMarker>(); // 지역 표시 목록
        private readonly List<GameObject> panelItems = new List<GameObject>(); // 오른쪽 패널 동적 요소
        private AdventureRegion selectedRegion; // 선택 지역
        private Text timeText; // 일차·활력
        private RectTransform panelContent; // 오른쪽 패널 내용
        private Text statusText; // 안내
        private Button actionButton; // 탐험 시작 · 마을로 가기
        private Text actionLabel; // 실행 버튼 글자

        public string SelectedDungeonId => DungeonSelectionRuntimeState.SelectedDungeonId; // 현재 선택 던전 ID 반환
        public string SelectedRegionId => selectedRegion == null ? string.Empty : selectedRegion.Id; // 현재 선택 지역 ID 반환 (Day63 추가)

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // 첫 씬 로드 전 이벤트 구독 지정
        private static void RegisterSceneLoadedHandler() // 씬 로드 이벤트 구독
        {
            SceneRuntimePatch.Register(GameScenes.DungeonSelect, HandleSceneLoaded); // DungeonSelect 씬 로드 시 주입 등록 (최적화 — 공통 등록기 사용)
        }

        private static void HandleSceneLoaded(Scene scene) // 씬 로드 완료 처리
        {
            if (Object.FindFirstObjectByType<DungeonSelectScreenController>() != null) return; // 중복 설치 차단
            new GameObject("DungeonSelectScreenRuntime", typeof(DungeonSelectScreenController)); // 런타임 컨트롤러 객체 생성
        }

        private void Awake() // 런타임 화면 초기화
        {
            DungeonSelectionRuntimeState.SetUnlockEvaluator(IsDungeonUnlocked); // 순차 해금 규칙 연결 (Day63 — 이전 진행 UI 패치 통합)
            DungeonSelectionRuntimeState.SelectionChanged += HandleSelectionChanged; // 선택 변경 이벤트 구독
            EnsureRegionErosionInitialized(); // 지역별 침식도 랜덤 초기화 (Day44)
            BuildUi(); // 지도 화면 생성
            AdventureRegion start = AdventureRegionCatalog.FindByDungeon(DungeonSelectionRuntimeState.SelectedDungeonId) ?? AdventureRegionCatalog.All[0]; // 전투에서 돌아오면 그 지역, 아니면 숲
            SelectRegion(start); // 첫 지역 선택
        }

        private void OnDestroy() // 런타임 화면 해제
        {
            DungeonSelectionRuntimeState.SelectionChanged -= HandleSelectionChanged; // 선택 변경 이벤트 구독 해제
            DungeonSelectionRuntimeState.SetUnlockEvaluator(null); // 해금 규칙 해제
        }

        private void BuildUi() // 전체 화면 : 왼쪽 세계 지도 + 지역 표시 · 오른쪽 지역 패널 · 상단 바
        {
            GameObject canvasObject = new GameObject("AdventureMapCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 런타임 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // 컨트롤러 하위 배치
            Canvas canvas = canvasObject.GetComponent<Canvas>(); // Canvas 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // Overlay 렌더링
            canvas.sortingOrder = 400; // 씬 기존 프로토타입 UI 위
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // CanvasScaler 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 스케일
            scaler.referenceResolution = new Vector2(1600f, 900f); // 기준 해상도 (다른 화면과 통일)
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 대응 방식
            scaler.matchWidthOrHeight = 0.5f; // 가로 세로 중간
            Transform root = canvasObject.transform; // 루트
            Image backdrop = CreateImage(root, "Backdrop", new Color(0.04f, 0.07f, 0.12f, 1f)); // 뒤 바탕
            Stretch(backdrop.rectTransform); // 전체
            Image map = CreateImage(root, "WorldMap", Color.white); // 세계 지도
            map.sprite = AdventureMapArt.Get(); // 정식 지도 우선
            map.raycastTarget = false; // 입력 통과
            SetRect(map.rectTransform, Vector2.zero, new Vector2(MapRight, MapTop)); // 왼쪽 큰 영역
            BuildMarkers(root); // 지역 표시
            BuildPanel(root); // 오른쪽 패널
            BuildTopBar(root); // 상단 바
        }

        private void BuildTopBar(Transform root) // 상단 바 : 로비 · 제목 · 일차/활력
        {
            Image bar = CreateImage(root, "TopBar", BarColor); // 상단 띠
            SetRect(bar.rectTransform, new Vector2(0f, MapTop), Vector2.one); // 위
            Button back = CreateButton(bar.transform, "BackButton", "◀  로비", new Color(0.24f, 0.26f, 0.32f, 1f)); // 로비 복귀
            SetRect(back.GetComponent<RectTransform>(), new Vector2(0.01f, 0.14f), new Vector2(0.11f, 0.86f)); // 왼쪽
            back.onClick.AddListener(() => LoadScene(GameScenes.Lobby)); // 로비 이동
            Text title = CreateText(bar.transform, "Title", "모험", 30, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft); // 제목
            SetRect(title.rectTransform, new Vector2(0.125f, 0f), new Vector2(0.40f, 1f)); // 제목 배치
            timeText = CreateText(bar.transform, "Time", string.Empty, 20, GoldColor, FontStyle.Bold, TextAnchor.MiddleRight); // 일차·활력
            SetRect(timeText.rectTransform, new Vector2(0.45f, 0f), new Vector2(0.985f, 1f)); // 오른쪽
        }

        private void BuildMarkers(Transform root) // 지도 위 지역 원 + 이름
        {
            foreach (AdventureRegion region in AdventureRegionCatalog.All) // 지역 순회
            {
                AdventureRegion captured = region; // 클릭용 복사
                Vector2 anchor = new Vector2(region.Position.x * MapRight, region.Position.y * MapTop); // 지도 영역 안 좌표
                Button button = RuntimeUiKit.CreateButton(root, "Region_" + region.Id, Color.white); // 원 버튼
                Image disc = button.GetComponent<Image>(); // 원 이미지
                disc.sprite = RhythmCircleSpriteFactory.GetDiscSprite(); // 원 모양
                RectTransform rect = (RectTransform)button.transform; // 영역
                rect.anchorMin = anchor; // 기준점
                rect.anchorMax = anchor; // 기준점
                rect.sizeDelta = new Vector2(62f, 62f); // 원 크기
                Outline ring = button.gameObject.AddComponent<Outline>(); // 선택 테두리
                ring.effectDistance = new Vector2(4f, -4f); // 테두리 두께
                Text glyph = CreateText(button.transform, "Glyph", region.Kind == AdventureRegionKind.Village ? "집" : region.Kind == AdventureRegionKind.Locked ? "✕" : region.Name.Substring(0, 1), 24, Color.white, FontStyle.Bold).Outlined(new Color(0f, 0f, 0f, 0.7f), new Vector2(1f, -1f)); // 원 안 글자
                Stretch(glyph.rectTransform); // 원 채움
                Text label = CreateText(root, "Label_" + region.Id, region.Name, 22, Color.white, FontStyle.Bold).Outlined(new Color(0f, 0f, 0f, 0.85f), new Vector2(2f, -2f)); // 이름
                label.raycastTarget = false; // 입력 통과
                RectTransform labelRect = label.rectTransform; // 이름 영역
                labelRect.anchorMin = anchor; // 기준점
                labelRect.anchorMax = anchor; // 기준점
                labelRect.anchoredPosition = new Vector2(0f, -50f); // 원 아래
                labelRect.sizeDelta = new Vector2(200f, 32f); // 크기
                button.onClick.AddListener(() => SelectRegion(captured)); // 지역 선택
                markers.Add(new RegionMarker { Region = region, Disc = disc, Ring = ring, Label = label }); // 목록 등록
            }
        }

        private void BuildPanel(Transform root) // 오른쪽 지역 패널 (내용은 선택할 때마다 다시 그림)
        {
            Image panel = CreateImage(root, "RegionPanel", PanelColor); // 패널
            SetRect(panel.rectTransform, new Vector2(MapRight + 0.01f, 0.015f), new Vector2(0.99f, MapTop - 0.012f)); // 오른쪽
            GameObject content = new GameObject("Content", typeof(RectTransform)); // 동적 내용
            content.transform.SetParent(panel.transform, false); // 패널 하위
            panelContent = (RectTransform)content.transform; // 저장
            SetRect(panelContent, new Vector2(0.05f, 0.24f), new Vector2(0.95f, 0.98f)); // 위쪽 영역
            statusText = CreateText(panel.transform, "Status", string.Empty, 16, HintColor, FontStyle.Normal, TextAnchor.MiddleLeft).Wrap(); // 안내
            SetRect(statusText.rectTransform, new Vector2(0.05f, 0.135f), new Vector2(0.95f, 0.225f)); // 버튼 위
            actionButton = CreateButton(panel.transform, "EnterButton", "탐험 시작", ActionColor); // 실행 버튼
            SetRect(actionButton.GetComponent<RectTransform>(), new Vector2(0.05f, 0.035f), new Vector2(0.95f, 0.125f)); // 아래
            actionLabel = actionButton.GetComponentInChildren<Text>(); // 버튼 글자
            actionLabel.fontSize = 24; // 큰 글자
            actionButton.onClick.AddListener(RunAction); // 실행
            Button minus = CreateButton(panel.transform, "ErosionMinusDebug", "침식 -10", new Color(0.30f, 0.22f, 0.20f, 1f)); // 침식도 감소 (개발용)
            SetRect(minus.GetComponent<RectTransform>(), new Vector2(0.05f, 0.23f), new Vector2(0.30f, 0.27f)); // 패널 안
            minus.onClick.AddListener(() => ApplyErosionDebugDelta(-10)); // 감소
            DevelopmentFeatures.HideInRelease(minus); // 출시 빌드에서는 숨김
            Button plus = CreateButton(panel.transform, "ErosionPlusDebug", "침식 +10", new Color(0.30f, 0.22f, 0.20f, 1f)); // 침식도 증가 (개발용)
            SetRect(plus.GetComponent<RectTransform>(), new Vector2(0.32f, 0.23f), new Vector2(0.57f, 0.27f)); // 패널 안
            plus.onClick.AddListener(() => ApplyErosionDebugDelta(10)); // 증가
            DevelopmentFeatures.HideInRelease(plus); // 출시 빌드에서는 숨김
        }

        private void SelectRegion(AdventureRegion region) // 지역 선택 (던전 지역이면 입장 가능한 첫 던전 선택)
        {
            selectedRegion = region; // 지역 저장

            if (region.Kind == AdventureRegionKind.Dungeon && AdventureRegionCatalog.FindByDungeon(DungeonSelectionRuntimeState.SelectedDungeonId) != region) // 다른 지역의 던전이 선택된 상태
            {
                foreach (string dungeonId in region.DungeonIds) // 지역 던전 순회
                {
                    if (DungeonSelectionRuntimeState.TrySelect(dungeonId, HasDungeonData)) break; // 입장 가능한 첫 던전
                }
            }

            RefreshAll(); // 갱신
        }

        private void SelectDungeon(string dungeonId) // 던전 카드 선택
        {
            DungeonSelectionRuntimeState.TrySelect(dungeonId, HasDungeonData); // 선택 (잠김이면 무시)
            RefreshAll(); // 갱신
        }

        private void HandleSelectionChanged(string dungeonId) // 선택 변경 이벤트 처리
        {
            RefreshAll(); // 갱신
        }

        private void RefreshAll() // 상단 · 지역 표시 · 오른쪽 패널 갱신
        {
            SaveData saveData = GetSave(); // 현재 저장
            timeText.text = saveData == null ? "Bootstrap 씬부터 실행해 주세요." : $"DAY {GameTimeService.GetCurrentDay(saveData)} · {GameTimeService.GetPhaseLabel(GameTimeService.GetCurrentPhase(saveData))}    활력 {VitalityService.GetVitality(saveData)}/{SaveData.MaxVitality}"; // 일차·활력

            foreach (RegionMarker marker in markers) // 지역 표시 순회
            {
                bool selected = marker.Region == selectedRegion; // 선택 여부
                marker.Disc.color = GetRegionColor(saveData, marker.Region); // 상태 색
                marker.Ring.effectColor = selected ? GoldColor : new Color(0f, 0f, 0f, 0.6f); // 선택 금테
                marker.Disc.rectTransform.localScale = Vector3.one * (selected ? 1.18f : 1f); // 선택 확대
                marker.Label.color = selected ? GoldColor : Color.white; // 이름 강조
            }

            if (selectedRegion != null) RefreshPanel(saveData); // 패널
        }

        private void RefreshPanel(SaveData saveData) // 오른쪽 패널 다시 그리기
        {
            foreach (GameObject item in panelItems) Destroy(item); // 이전 요소 제거
            panelItems.Clear(); // 목록 비움
            AdventureRegion region = selectedRegion; // 선택 지역
            float y = 1f; // 위에서부터
            y = AddLabel(region.Name, 32, Color.white, FontStyle.Bold, y, 0.075f); // 지역 이름
            y = AddLabel(region.LoreName, 17, GoldColor, FontStyle.Bold, y, 0.045f); // 세계관 지명
            y = AddLabel(region.Description, 16, HintColor, FontStyle.Normal, y, 0.11f); // 설명

            if (region.Kind == AdventureRegionKind.Locked) // 잠긴 지역
            {
                AddLabel("잠김 · " + region.LockedHint, 18, new Color(1f, 0.55f, 0.45f, 1f), FontStyle.Bold, y - 0.02f, 0.06f); // 안내
                SetAction(false, "아직 갈 수 없음"); // 비활성
                statusText.text = string.Empty; // 안내 없음
                return; // 종료
            }

            if (region.Kind == AdventureRegionKind.Village) // 마을
            {
                SetAction(true, "마을로 가기"); // 마을 이동
                statusText.text = "마을에서는 동료들과 시간을 보내거나 온천·여관에서 쉴 수 있어요."; // 안내
                return; // 종료
            }

            if (saveData != null && region.DungeonIds.Count > 0) // 지역 침식도 (첫 던전의 지역 기준)
            {
                DungeonData first = GetDungeon(region.DungeonIds[0]); // 첫 던전
                if (first != null) y = AddLabel(BuildErosionLine(saveData, first.RegionId), 16, new Color(1f, 0.66f, 0.46f, 1f), FontStyle.Bold, y, 0.045f); // 침식도
            }

            BattleRegionTraitKind trait = region.DungeonIds.Count > 0 ? BattleRegionTraitCatalog.GetKind(region.DungeonIds[0]) : BattleRegionTraitKind.None; // 지역 특징 (Day65)
            if (trait != BattleRegionTraitKind.None) y = AddLabel($"지역 특징 · {BattleRegionTraitCatalog.GetName(trait)} — {BattleRegionTraitCatalog.GetDescription(trait)}", 15, new Color(0.62f, 0.82f, 1f, 1f), FontStyle.Normal, y, 0.075f); // 특징 안내

            y -= 0.015f; // 간격

            foreach (string dungeonId in region.DungeonIds) // 던전 카드
            {
                y = AddDungeonCard(saveData, dungeonId, y); // 카드 추가
            }

            DungeonData selected = GetDungeon(DungeonSelectionRuntimeState.SelectedDungeonId); // 선택 던전
            bool canEnter = selected != null && AdventureRegionCatalog.FindByDungeon(selected.Id) == region && DungeonSelectionRuntimeState.CanEnter(HasDungeonData); // 이 지역 던전이 선택되어 입장 가능
            statusText.text = canEnter ? $"{selected.DisplayName} · 입장 활력 {selected.VitalityCost} (보유 {VitalityService.GetVitality(saveData)})" : "입장할 던전을 고르세요."; // 안내
            SetAction(canEnter, canEnter ? "탐험 시작" : "던전 선택 필요"); // 실행 버튼
        }

        private float AddDungeonCard(SaveData saveData, string dungeonId, float top) // 던전 카드 (이름 · 권장 레벨 · 활력 · 보상 · 진행 상태)
        {
            DungeonData dungeon = GetDungeon(dungeonId); // 던전
            DungeonProgressState state = DungeonProgressionPolicy.GetState(saveData, dungeonId); // 진행 상태
            bool selected = DungeonSelectionRuntimeState.SelectedDungeonId == dungeonId; // 선택 여부
            bool locked = dungeon == null || saveData == null || state == DungeonProgressState.Locked; // 잠김
            Button card = CreateButton(panelContent, "DungeonCard_" + dungeonId, string.Empty, locked ? LockedCardColor : selected ? SelectedCardColor : CardColor); // 카드
            SetRect(card.GetComponent<RectTransform>(), new Vector2(0f, top - 0.17f), new Vector2(1f, top)); // 배치
            if (selected) card.gameObject.AddComponent<Outline>().effectColor = GoldColor; // 선택 금테
            card.onClick.AddListener(() => SelectDungeon(dungeonId)); // 선택
            card.interactable = !locked; // 잠김이면 비활성
            Text name = CreateText(card.transform, "Name", dungeon == null ? dungeonId : dungeon.DisplayName, 22, Color.white, FontStyle.Bold, TextAnchor.UpperLeft).BestFit(14); // 이름
            SetRect(name.rectTransform, new Vector2(0.04f, 0.52f), new Vector2(0.96f, 0.92f)); // 위
            string info = dungeon == null ? "데이터 없음" : $"권장 Lv.{dungeon.RecommendedLevel}   활력 {dungeon.VitalityCost}   ● {dungeon.RewardGold}G"; // 정보
            Text detail = CreateText(card.transform, "Info", info, 15, HintColor, FontStyle.Normal, TextAnchor.MiddleLeft).BestFit(10); // 정보 글자
            SetRect(detail.rectTransform, new Vector2(0.04f, 0.28f), new Vector2(0.96f, 0.52f)); // 가운데
            string stateText = state == DungeonProgressState.Cleared ? $"클리어 {Stars(DungeonProgressSaveAdapter.GetBestStars(saveData, dungeonId))}" : locked ? DungeonProgressionPolicy.GetLockReason(dungeonId) : "진입 가능"; // 진행 상태
            Text status = CreateText(card.transform, "Status", stateText, 15, state == DungeonProgressState.Cleared ? new Color(0.50f, 0.90f, 0.62f, 1f) : locked ? new Color(0.62f, 0.64f, 0.68f, 1f) : GoldColor, FontStyle.Bold, TextAnchor.MiddleLeft).BestFit(10); // 상태 글자
            SetRect(status.rectTransform, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.28f)); // 아래
            panelItems.Add(card.gameObject); // 목록 등록
            return top - 0.185f; // 다음 위치
        }

        private float AddLabel(string text, int size, Color color, FontStyle style, float top, float height) // 패널 글자 한 줄
        {
            Text label = CreateText(panelContent, "Label", text, size, color, style, TextAnchor.UpperLeft).Wrap(); // 글자
            SetRect(label.rectTransform, new Vector2(0f, top - height), new Vector2(1f, top)); // 배치
            panelItems.Add(label.gameObject); // 목록 등록
            return top - height - 0.01f; // 다음 위치
        }

        private void SetAction(bool interactable, string label) // 실행 버튼 상태
        {
            actionButton.interactable = interactable; // 활성 여부
            actionLabel.text = label; // 글자
        }

        private void RunAction() // 실행 : 마을 이동 또는 탐험 시작
        {
            if (selectedRegion == null) return; // 선택 없음

            if (selectedRegion.Kind == AdventureRegionKind.Village) // 마을
            {
                LoadScene(GameScenes.Village); // 마을 이동
                return; // 종료
            }

            EnterDungeon(); // 탐험 시작
        }

        private void EnterDungeon() // 선택 던전 탐험 시작 (활력 소비 → 노드형 탐험 지도)
        {
            if (!DungeonSelectionRuntimeState.CanEnter(HasDungeonData)) // 유효 선택 여부 확인
            {
                RefreshAll(); // 진입 차단 상태 갱신
                return; // 진입 중단
            }

            if (GameManager.Instance == null || GameManager.Instance.Scenes == null) // 씬 로더 준비 확인
            {
                statusText.text = "SceneLoader를 찾을 수 없습니다."; // 씬 로더 누락 표시
                return; // 진입 중단
            }

            DungeonData entryDungeon = GetDungeon(DungeonSelectionRuntimeState.SelectedDungeonId); // 입장 던전 (Day61)
            SaveData entrySave = GetSave(); // 현재 저장 (Day61)

            if (!DungeonEntryService.TryPayEntry(entrySave, entryDungeon, out string entryError)) // 입장 활력 소비 (Day61 경제 밸런스)
            {
                statusText.text = entryError; // 활력 부족 안내
                return; // 입장 중단
            }

            GameManager.Instance.Save.SaveCurrent(); // 활력 소비 저장
            string dungeonId = DungeonSelectionRuntimeState.SelectedDungeonId; // 입장 던전 ID
            RegionArrivalDefinition arrival = RegionVisitService.GetPendingArrival(entrySave, dungeonId); // 지역 첫 방문 이야기 (Day65)

            if (arrival != null) // 처음 온 지역
            {
                DialogueOverlayView view = DialogueOverlayView.Open(DialogueLibrary.Load(arrival.ScriptId), runner => // 고향 동료의 소개 이야기
                {
                    RegionVisitService.CompleteArrival(GetSave(), arrival, runner); // 방문 기록 · 호감도
                    GameManager.Instance.Save.SaveCurrent(); // 저장
                    DungeonMapOverlayView.StartRun(dungeonId); // 이야기가 끝나면 탐험 시작
                });

                if (view != null) return; // 이야기 진행 중
                RegionVisitService.CompleteArrival(entrySave, arrival, null); // 대사 파일이 없으면 방문만 기록
            }

            DungeonMapOverlayView.StartRun(dungeonId); // 노드형 탐험 지도 시작 (Day55, 전투 노드 선택 시 전투 씬 이동)
        }

        private Color GetRegionColor(SaveData saveData, AdventureRegion region) // 지역 원 색 (잠김 회색 · 마을 금색 · 던전은 침식도 색)
        {
            if (region.Kind == AdventureRegionKind.Locked) return new Color(0.34f, 0.35f, 0.38f, 1f); // 잠김
            if (region.Kind == AdventureRegionKind.Village) return new Color(0.92f, 0.72f, 0.32f, 1f); // 마을
            DungeonData first = region.DungeonIds.Count > 0 ? GetDungeon(region.DungeonIds[0]) : null; // 첫 던전
            if (saveData == null || first == null) return new Color(0.40f, 0.52f, 0.70f, 1f); // 정보 없음

            switch (RegionErosionService.GetErosionTier(saveData, first.RegionId)) // 침식도 등급
            {
                case RegionErosionTier.Stable: return new Color(0.36f, 0.72f, 0.46f, 1f); // 안정 초록
                case RegionErosionTier.Cracked: return new Color(0.62f, 0.72f, 0.36f, 1f); // 균열 연두
                case RegionErosionTier.Eroded: return new Color(0.86f, 0.66f, 0.28f, 1f); // 침식 주황
                case RegionErosionTier.Dangerous: return new Color(0.88f, 0.40f, 0.26f, 1f); // 위험 빨강
                default: return new Color(0.58f, 0.20f, 0.46f, 1f); // 붕괴 자주
            }
        }

        private static string BuildErosionLine(SaveData saveData, string regionId) // 침식도 한 줄
        {
            int erosion = RegionErosionService.GetOrInitializeErosion(saveData, regionId); // 침식도
            int bonus = RegionErosionService.GetEnemyStatBonusPercent(saveData, regionId); // 적 강화
            return $"침식도 {erosion}/{RegionErosionSaveData.MaxErosion} · {GetErosionTierLabel(RegionErosionService.GetErosionTier(saveData, regionId))} · 적 능력치 +{bonus}%"; // 문구
        }

        private static string GetErosionTierLabel(RegionErosionTier tier) // 침식도 등급 한글 라벨 변환 (Day44)
        {
            switch (tier) // 등급별 분기
            {
                case RegionErosionTier.Stable: return "안정"; // 안정
                case RegionErosionTier.Cracked: return "균열"; // 균열
                case RegionErosionTier.Eroded: return "침식"; // 침식
                case RegionErosionTier.Dangerous: return "위험"; // 위험
                default: return "붕괴"; // 붕괴
            }
        }

        private static string Stars(int stars) => new string('★', Mathf.Clamp(stars, 0, 3)) + new string('☆', 3 - Mathf.Clamp(stars, 0, 3)); // 별점 표시 (Day47)

        private void EnsureRegionErosionInitialized() // 지원 던전 소속 지역 침식도 랜덤 초기화 (Day44 추가 작업)
        {
            SaveData saveData = GetSave(); // 현재 저장
            if (saveData == null) return; // 저장 없음
            bool changed = false; // 신규 초기화 여부

            foreach (string dungeonId in DungeonSelectionRuntimeState.SupportedDungeonIds) // 지원 던전 순회
            {
                DungeonData dungeon = GetDungeon(dungeonId); // 던전
                if (dungeon != null && !string.IsNullOrWhiteSpace(dungeon.RegionId) && RegionErosionService.TryInitializeRandomErosion(saveData, dungeon.RegionId, out _)) changed = true; // 미등록 지역 초기화
            }

            if (changed) GameManager.Instance.Save.SaveCurrent(); // 초기화 결과 저장
        }

        private void ApplyErosionDebugDelta(int delta) // 선택 지역 침식도 디버그 증감 (Day44, 개발 빌드 전용 버튼)
        {
            SaveData saveData = GetSave(); // 현재 저장
            DungeonData dungeon = selectedRegion == null || selectedRegion.DungeonIds.Count == 0 ? null : GetDungeon(selectedRegion.DungeonIds[0]); // 지역 첫 던전

            if (saveData == null || dungeon == null) // 대상 확인
            {
                statusText.text = "던전이 있는 지역을 먼저 선택해 주세요."; // 안내
                return; // 중단
            }

            RegionErosionService.AddErosion(saveData, dungeon.RegionId, delta); // 침식도 증감
            GameManager.Instance.Save.SaveCurrent(); // 저장
            RefreshAll(); // 갱신
        }

        private bool IsDungeonUnlocked(string dungeonId) // 현재 저장 기준 던전 해금 여부 (순차 해금)
        {
            SaveData saveData = GetSave(); // 현재 저장
            return saveData != null && DungeonProgressionPolicy.IsUnlocked(saveData, dungeonId); // 결과
        }

        private bool HasDungeonData(string dungeonId) => GetDungeon(dungeonId) != null; // 던전 데이터 존재 확인

        private static DungeonData GetDungeon(string dungeonId) // 던전 데이터 안전 조회
        {
            if (string.IsNullOrWhiteSpace(dungeonId) || GameManager.Instance == null || GameManager.Instance.Data == null || !GameManager.Instance.Data.IsInitialized) return null; // 준비 확인
            return GameManager.Instance.Data.GetDungeon(dungeonId); // 조회
        }

        private static SaveData GetSave() => GameManager.Instance == null || GameManager.Instance.Save == null ? null : GameManager.Instance.Save.CurrentSave; // 현재 저장 조회

        private void LoadScene(string sceneName) // 씬 이동
        {
            if (GameManager.Instance == null || GameManager.Instance.Scenes == null) // 씬 로더 확인
            {
                statusText.text = "SceneLoader를 찾을 수 없습니다."; // 안내
                return; // 중단
            }

            GameManager.Instance.Scenes.LoadScene(sceneName); // 씬 로드
        }

        private static Image CreateImage(Transform parent, string name, Color color) => RuntimeUiKit.CreateImage(parent, name, color); // 공통 UI 이미지 생성 (RuntimeUiKit 위임)

        private static Text CreateText(Transform parent, string name, string value, int size, Color color, FontStyle style = FontStyle.Bold, TextAnchor alignment = TextAnchor.MiddleCenter) // 공통 UI 텍스트 생성 (RuntimeUiKit 위임)
        {
            return RuntimeUiKit.CreateText(parent, name, value, size, color, style, alignment); // 텍스트 반환
        }

        private static Button CreateButton(Transform parent, string name, string label, Color color) // 공통 UI 버튼 생성 (RuntimeUiKit 위임)
        {
            Button button = RuntimeUiKit.CreateButton(parent, name, color); // 버튼 생성
            Text text = CreateText(button.transform, "Label", label, 19, Color.white).BestFit(10); // 라벨
            Stretch(text.rectTransform, 6f); // 라벨 확장
            return button; // 버튼 반환
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax) => RuntimeUiKit.SetRect(rect, anchorMin, anchorMax); // 정규화 UI 영역 배치 (RuntimeUiKit 위임)

        private static void Stretch(RectTransform rect, float padding = 0f) => RuntimeUiKit.Stretch(rect, padding); // 부모 전체 영역 확장 (RuntimeUiKit 위임)
    }
}
