using System; // 콜백 델리게이트 기능
using System.Collections.Generic; // 목록 자료형
using ProjectH.Core; // 환경 설정 기능
using ProjectH.UI; // 공용 UI 생성·그림 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 연출 방지
    public sealed class OutfitBreakView : MonoBehaviour // 의상 파괴 연출 (Day87 신규 — 아군의 체력이 60% · 30% 아래로 내려가는 순간 화면 왼쪽에 그림이 들어왔다 나간다. 전투를 멈추지 않고 입력도 막지 않는다)
    {
        public const int SortingOrder = 30; // 전투 HUD(20) 위 · 보스 연출(450)과 결과창(500) 아래
        public const float ArtScale = 1f; // 틀 안의 그림 크기 — 이 숫자만 바꾸면 구도가 바뀐다 (1 = 전신 · 1.2 = 무릎까지 · 1.4 = 허벅지까지)
        public static readonly Vector2 FrameMin = new Vector2(0f, 0.245f); // 틀 왼쪽 아래 (아래의 HUD 카드 0.225보다 위)
        public static readonly Vector2 FrameMax = new Vector2(0.19f, 0.905f); // 틀 오른쪽 위 (위의 웨이브 표시 0.915보다 아래 · 아군이 서는 0.2보다 왼쪽)
        private const float SlideMargin = 0.01f; // 화면 밖으로 완전히 나가도록 더 미는 폭
        private const float ArtTop = 0.985f; // 창 안에서 그림 위쪽 높이 (머리 위 여백)
        private const float ArtFill = 0.97f; // 배율 1일 때 그림이 창 높이에서 차지하는 비율
        private const float FlashStrength = 0.65f; // 도착 순간 번쩍임 세기
        private static readonly Color FrameColor = new Color(0.10f, 0.11f, 0.16f, 0.94f); // 틀 색 (스킨이 없을 때)
        private static readonly Color PlateColor = new Color(0.12f, 0.14f, 0.25f, 0.95f); // 이름표 색 (스킨이 없을 때)

        private static OutfitBreakView instance; // 이번 전투의 연출 (전투 씬과 함께 사라진다)

        private readonly OutfitBreakTracker tracker = new OutfitBreakTracker(); // 누가 어느 단계까지 나왔는지
        private readonly OutfitBreakQueue queue = new OutfitBreakQueue(); // 차례를 기다리는 그림
        private readonly OutfitBreakTimeline timeline = new OutfitBreakTimeline(); // 시간 진행
        private readonly List<KeyValuePair<BattleStats, Action>> watched = new List<KeyValuePair<BattleStats, Action>>(); // 지켜보는 아군과 체력 변경 수신기
        private CanvasGroup group; // 전체 투명도
        private RectTransform frame; // 틀
        private Image tint; // 그림 뒤 바탕 (캐릭터 색)
        private Image art; // 그림
        private Image flash; // 번쩍임
        private Text label; // 이름표 글자

        public bool IsShowing { get; private set; } // 그림이 떠 있는지
        public OutfitBreakRequest Current { get; private set; } // 지금 떠 있는 그림
        public int PendingCount => queue.Count; // 기다리는 그림 수
        public int WatchedCount => watched.Count; // 지켜보는 아군 수
        public RectTransform Frame => frame; // 틀 (테스트 · 배치 확인용)
        public Image Art => art; // 그림 (테스트용)
        public Text Label => label; // 이름표 글자 (테스트용)

        public static void Watch(BattleStats stats) // 아군 한 명의 체력을 지켜본다 (전투 화면이 아군을 만들 때 부른다)
        {
            if (stats == null || !Application.isPlaying) return; // 대상 없음 · 편집 모드 테스트에서는 화면을 만들지 않는다
            if (instance == null) instance = Create(); // 전투마다 하나
            instance.Add(stats); // 지켜보기 시작
        }

        public static void Stop() // 전투가 끝났을 때 : 지켜보기를 끝내고 떠 있는 그림을 치운다
        {
            if (instance != null) instance.Release(); // 정리
        }

        public static OutfitBreakView Create() // 연출 Canvas 생성 (입력 판정이 없어 전투 조작을 막지 않는다)
        {
            GameObject root = new GameObject("OutfitBreakRuntime", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup)); // 연출 Canvas (GraphicRaycaster 없음 = 클릭 통과)
            Canvas canvas = root.GetComponent<Canvas>(); // Canvas 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 Overlay
            canvas.sortingOrder = SortingOrder; // 정렬 순서
            CanvasScaler scaler = root.GetComponent<CanvasScaler>(); // 스케일러 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기준
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 해상도 (전투 HUD와 같음)
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 중간
            OutfitBreakView view = root.AddComponent<OutfitBreakView>(); // 연출 컴포넌트 추가
            view.group = root.GetComponent<CanvasGroup>(); // 투명도 그룹 저장
            view.group.interactable = false; // 조작 대상 아님
            view.group.blocksRaycasts = false; // 입력 통과
            view.Build(); // 화면 구성
            view.Hide(); // 처음에는 숨김
            return view; // 연출 반환
        }

        public void Add(BattleStats stats) // 지켜볼 아군 추가 (같은 아군은 한 번만)
        {
            if (stats == null) return; // 대상 없음

            foreach (KeyValuePair<BattleStats, Action> pair in watched) // 지켜보는 목록 순회
            {
                if (ReferenceEquals(pair.Key, stats)) return; // 이미 지켜보는 중
            }

            Action handler = () => Report(stats.RuntimeId, stats.CharacterId, stats.DisplayName, stats.HealthRatio); // 체력이 바뀔 때마다 보고
            stats.HealthChanged += handler; // 체력 변경 이벤트 구독
            watched.Add(new KeyValuePair<BattleStats, Action>(stats, handler)); // 목록 등록 (나중에 구독을 끊기 위해 수신기도 함께)
        }

        public bool Report(string runtimeId, string characterId, string displayName, float healthRatio) // 체력 변화 보고 → 그림을 대기열에 올렸으면 true
        {
            int stage = tracker.Report(runtimeId, healthRatio); // 새로 넘은 단계 (설정이 꺼져 있어도 "이미 지나간 단계"는 기록한다 — 전투 도중에 켜도 지난 단계가 다시 나오지 않는다)
            if (stage == OutfitBreakRules.None || !GameSettings.OutfitBreak) return false; // 새 단계 없음 · 설정에서 끔
            queue.Enqueue(new OutfitBreakRequest(characterId, displayName, stage)); // 차례 대기
            if (IsShowing) timeline.Hurry(); // 떠 있는 그림은 조금 일찍 비켜 준다
            return true; // 대기열에 올림
        }

        public void Release() // 지켜보기 종료 + 대기 버림 + 그림 치우기
        {
            foreach (KeyValuePair<BattleStats, Action> pair in watched) // 지켜보는 목록 순회
            {
                if (pair.Key != null) pair.Key.HealthChanged -= pair.Value; // 체력 변경 이벤트 구독 해제
            }

            watched.Clear(); // 목록 비움
            tracker.Clear(); // 단계 기록 비움
            queue.Clear(); // 대기 비움
            Hide(); // 그림 치움
        }

        public void Tick(float deltaTime, bool battleRunning) // 시간 진행 (전투 배속과 무관한 실제 시간 · 전투가 멈춘 동안에는 그대로 기다린다)
        {
            if (!battleRunning) return; // 일시정지 · 궁극기 컷인 · 리듬 판정 중 (그쪽이 끝난 뒤에 이어서 보여 준다)
            if (!IsShowing && !TryBeginNext()) return; // 보여 줄 그림 없음
            timeline.Advance(deltaTime); // 시간 진행
            Apply(); // 화면 반영
            if (timeline.IsDone) Hide(); // 다 보여 줌 (다음 그림은 다음 프레임에 시작)
        }

        private void Update() // 매 프레임 진행
        {
            Tick(Time.unscaledDeltaTime, Time.timeScale > 0f); // 배속을 올려도 그림이 보이는 시간은 같다
        }

        private void OnDestroy() // 전투 씬 종료
        {
            Release(); // 이벤트 구독 해제
            if (instance == this) instance = null; // 참조 해제
        }

        private void Build() // 화면 구성 (틀 + 그림 창 + 이름표)
        {
            Image panel = RuntimeUiKit.CreateImage(transform, "Frame", FrameColor); // 틀
            panel.raycastTarget = false; // 입력 통과
            UiSkinKit.Panel(panel, UiSkin.PanelDark); // 정식 어두운 창
            frame = panel.rectTransform; // 틀 저장
            RuntimeUiKit.SetRect(frame, FrameMin, FrameMax); // 화면 왼쪽

            GameObject windowObject = new GameObject("Window", typeof(RectTransform), typeof(RectMask2D)); // 그림이 보이는 창 (밖으로 나간 부분은 잘린다)
            windowObject.transform.SetParent(frame, false); // 틀 하위
            RectTransform window = (RectTransform)windowObject.transform; // 창 영역
            RuntimeUiKit.SetRect(window, new Vector2(0.045f, 0.022f), new Vector2(0.955f, 0.905f)); // 틀 테두리 안쪽 · 이름표 아래

            tint = RuntimeUiKit.CreateImage(window, "Tint", Color.black); // 그림 뒤 바탕
            tint.raycastTarget = false; // 입력 통과
            RuntimeUiKit.Stretch(tint.rectTransform); // 창 전체

            art = RuntimeUiKit.CreateImage(window, "Art", Color.white); // 그림
            art.raycastTarget = false; // 입력 통과
            art.preserveAspect = true; // 비율 유지 (영역을 넉넉히 잡으면 세로 길이에 맞춰 가운데에 그려진다)
            RuntimeUiKit.SetRect(art.rectTransform, new Vector2(-0.75f, ArtTop - (ArtFill * ArtScale)), new Vector2(1.75f, ArtTop)); // 위쪽은 고정하고 배율만큼 아래로 키운다 (대화 화면의 스탠딩과 같은 방식)

            flash = RuntimeUiKit.CreateImage(window, "Flash", new Color(1f, 1f, 1f, 0f)); // 번쩍임
            flash.raycastTarget = false; // 입력 통과
            RuntimeUiKit.Stretch(flash.rectTransform); // 창 전체

            Image plate = RuntimeUiKit.CreateImage(frame, "Plate", PlateColor); // 이름표
            plate.raycastTarget = false; // 입력 통과
            UiSkinKit.Panel(plate, UiSkin.NamePlate); // 정식 남색 이름표
            RuntimeUiKit.SetRect(plate.rectTransform, new Vector2(0.045f, 0.915f), new Vector2(0.955f, 0.98f)); // 틀 위쪽
            label = RuntimeUiKit.CreateText(plate.transform, "Text", string.Empty, 22, Color.white, FontStyle.Bold).BestFit(13); // 이름표 글자 (긴 이름은 줄여서)
            label.raycastTarget = false; // 입력 통과
            RuntimeUiKit.Stretch(label.rectTransform, 6f); // 이름표 안쪽
        }

        private bool TryBeginNext() // 다음 차례의 그림 시작 (그림이 없는 캐릭터는 건너뛴다)
        {
            while (queue.TryDequeue(out OutfitBreakRequest request)) // 차례대로 꺼냄
            {
                Sprite sprite = OutfitBreakArt.Get(request.CharacterId, request.Stage, out _); // 전용 그림 → 스탠딩 표정
                if (sprite == null) continue; // 그림 없음

                Current = request; // 지금 그림 기록
                art.sprite = sprite; // 그림 적용
                tint.color = Color.Lerp(DialogueArtFactory.GetCharacterTint(request.CharacterId), Color.black, 0.62f); // 캐릭터 색을 어둡게 깐 바탕
                label.text = string.IsNullOrEmpty(request.DisplayName) ? OutfitBreakRules.GetLabel(request.Stage) : $"{request.DisplayName} · {OutfitBreakRules.GetLabel(request.Stage)}"; // 이름 · 단계
                label.color = OutfitBreakRules.GetLabelColor(request.Stage); // 단계 색
                timeline.Start(queue.Count > 0); // 기다리는 그림이 있으면 짧게
                IsShowing = true; // 표시 시작
                ProjectH.Core.AudioService.PlaySfx(ProjectH.Core.AudioCatalog.SfxBreak); // 옷이 찢기는 소리 (Day88)
                return true; // 시작함
            }

            return false; // 보여 줄 그림 없음
        }

        private void Apply() // 타임라인 → 화면 반영
        {
            float shift = -(FrameMax.x - FrameMin.x + SlideMargin) * timeline.Slide; // 왼쪽 화면 밖으로 밀린 폭 (화면 너비 비율)
            frame.anchorMin = new Vector2(FrameMin.x + shift, FrameMin.y); // 틀 왼쪽 아래
            frame.anchorMax = new Vector2(FrameMax.x + shift, FrameMax.y); // 틀 오른쪽 위
            flash.color = new Color(1f, 1f, 1f, timeline.Flash * FlashStrength); // 도착 순간 번쩍임
            group.alpha = 1f - (0.5f * timeline.ExitProgress); // 나가면서 옅어짐
        }

        private void Hide() // 그림 치우기
        {
            IsShowing = false; // 표시 끝
            Current = default; // 기록 비움
            if (group != null) group.alpha = 0f; // 숨김
        }
    }
}
