using System.Collections.Generic; // 목록 자료형
using ProjectH.Battle.Rhythm; // 원형 스프라이트 생성 기능
using ProjectH.Core; // 씬 로더 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // 현재 씬 조회 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public enum LoadingScreenMode // 로딩창 연출 모드 (Day55)
    {
        Corner = 0, // 우하단 러너 + 페이드 (타이틀 → 로비, 전투 → 던전 선택)
        Tunnel = 1 // 중앙 4인 러너 + 터널 출구 걷힘 (던전 선택 → 전투)
    }

    [DisallowMultipleComponent] // 중복 로딩창 방지
    public sealed class LoadingScreenView : MonoBehaviour // 씬 전환 로딩창 (Day55 신규 — 우하단 러너 모드 / 전투 진입 터널 모드)
    {
        private const float MinDisplaySeconds = 0.7f; // 최소 표시 시간 (빠른 로딩에서도 연출이 한 번은 보이도록)
        private const float BarFillSpeed = 1.4f; // 로딩바 초당 최대 채움 속도
        private const float FadeOutSeconds = 0.3f; // 우하단 모드 사라짐 시간
        private const float RevealSeconds = 0.95f; // 터널 모드 검은 막 걷힘 시간
        private const float HudFadeSeconds = 0.18f; // 걷힘 시작 시 로딩바·문구 사라짐 시간
        private const float RunnerFadeSeconds = 0.14f; // 경계선이 지나간 러너 사라짐 시간
        private const float RunCycleSeconds = 0.42f; // 달리기 한 주기 시간
        private const float SprintCycleSeconds = 0.30f; // 터널 출구 전력질주 주기 시간
        private const float LegSwingDegrees = 38f; // 다리 흔들림 각도
        private const float ArmSwingDegrees = 30f; // 팔 흔들림 각도
        private const float BobHeight = 6f; // 달릴 때 몸 상하 흔들림(px)
        private const float EdgeSoftnessPixels = 240f; // 검은 막 경계 흐림 폭(px)
        private const float ExitGlowWidthPixels = 380f; // 출구 빛 폭(px)
        private const float GroundDashSpacing = 96f; // 바닥선 점선 간격(px)
        private const float GroundScrollSpeed = 520f; // 바닥선 흐름 속도(px/s)
        private const float AsyncReadyProgress = 0.9f; // 씬 활성화 대기 시 Unity 비동기 진행률 상한
        private static readonly Color SilhouetteColor = Color.white; // 실루엣 색상
        private static readonly Color BarBackColor = new Color(0.22f, 0.22f, 0.24f, 1f); // 로딩바 배경 색상
        private static readonly Color SubTextColor = new Color(0.80f, 0.82f, 0.86f, 1f); // 보조 문구 색상
        private static readonly Color ExitLightColor = new Color(1f, 0.96f, 0.86f, 1f); // 터널 출구 빛 색상

        private enum LoadState { Idle, Loading, Activating, FadingOut, Revealing } // 로딩창 진행 상태
        private enum HairStyle { Curly, Long, Bob, Ponytail, TwinTails } // 실루엣 머리 모양

        private sealed class RunnerRig // 달리는 실루엣 관절 참조
        {
            public RectTransform Body; // 상하 흔들림 몸통 루트
            public RectTransform FrontLeg; // 앞다리
            public RectTransform BackLeg; // 뒷다리
            public RectTransform FrontArm; // 앞팔
            public RectTransform BackArm; // 뒷팔
            public RectTransform Shadow; // 발밑 그림자
            public CanvasGroup Group; // 개별 페이드 그룹
            public float PhaseOffset; // 달리기 위상 차이
            public float NormalizedX; // 화면 가로 위치 비율 (터널 경계 통과 판정용)
        }

        private sealed class HudRefs // 로딩바·문구 참조 묶음
        {
            public RectTransform BarFill; // 로딩바 채움
            public Text Loading; // NOW LOADING 문구
            public Text Percent; // 진행률 문구
            public CanvasGroup Group; // 묶음 페이드 그룹
        }

        private static LoadingScreenView instance; // 전역 로딩창 (씬 전환 간 유지)
        private static bool registered; // 씬 로더 이벤트 구독 여부
        private static Sprite horizontalFadeSprite; // 가로 알파 그라데이션 스프라이트 (왼쪽 불투명 → 오른쪽 투명)
        private readonly List<RunnerRig> tunnelRunners = new List<RunnerRig>(); // 터널 모드 4인 러너
        private readonly List<RectTransform> groundDashes = new List<RectTransform>(); // 터널 모드 바닥선 점선
        private Canvas canvas; // 로딩창 Canvas
        private CanvasGroup group; // 전체 페이드 그룹
        private RectTransform rootRect; // 로딩창 루트 (화면 크기 조회용)
        private RectTransform blackCurtain; // 검은 막 (터널 모드에서 오른쪽부터 걷힘)
        private RectTransform curtainEdge; // 검은 막 흐린 경계
        private Image exitGlow; // 오른쪽 끝 터널 출구 빛
        private Image revealGlow; // 걷히는 경계의 빛 번짐
        private GameObject cornerGroup; // 우하단 모드 묶음
        private GameObject tunnelGroup; // 터널 모드 묶음
        private HudRefs cornerHud; // 우하단 모드 로딩바·문구
        private HudRefs tunnelHud; // 터널 모드 로딩바·문구
        private HudRefs activeHud; // 현재 모드 로딩바·문구
        private RunnerRig cornerRunner; // 우하단 곱슬머리 러너
        private RectTransform companionBody; // 따라오는 곰인형 몸통 루트
        private RectTransform companionFrontLeg; // 곰인형 앞다리
        private RectTransform companionBackLeg; // 곰인형 뒷다리
        private Text tipText; // Tip 문구
        private CanvasGroup tipGroup; // Tip 페이드 그룹
        private AsyncOperation operation; // 진행 중인 씬 전환 작업
        private LoadState state = LoadState.Idle; // 현재 진행 상태
        private LoadingScreenMode mode = LoadingScreenMode.Corner; // 현재 연출 모드
        private float shownAt; // 표시 시작 시각
        private float displayedProgress; // 화면 표시 진행률
        private float fadeTimer; // 사라짐·걷힘 경과 시간

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // 첫 씬 로드 전 구독 등록
        private static void Register() // 씬 로더 이벤트 구독
        {
            if (registered) // 기존 구독 확인
            {
                return; // 중복 구독 차단
            }

            registered = true; // 구독 상태 저장
            SceneLoader.LoadStarted += HandleLoadStarted; // 씬 전환 시작 이벤트 연결
        }

        private static void HandleLoadStarted(AsyncOperation loadOperation, string sceneName) // 씬 전환 시작 처리
        {
            string fromScene = SceneManager.GetActiveScene().name; // 출발 씬 이름 조회

            if (!ShouldShow(fromScene, sceneName)) // 무거운 전환 여부 확인
            {
                return; // 메뉴 이동 등 가벼운 전환은 로딩창 없이 바로 전환
            }

            EnsureInstance().Begin(loadOperation, ResolveMode(fromScene, sceneName)); // 전환 종류별 모드로 로딩창 표시 시작
        }

        public static bool ShouldShow(string fromScene, string toScene) // 로딩창 표시 대상 전환 판정 (전투 진입·복귀, 타이틀에서 출발)
        {
            return toScene == GameScenes.Battle || fromScene == GameScenes.Battle || fromScene == GameScenes.Title; // 전투 진입·전투 복귀·게임 시작만 표시
        }

        public static LoadingScreenMode ResolveMode(string fromScene, string toScene) // 전환 종류별 연출 모드 판정
        {
            return toScene == GameScenes.Battle ? LoadingScreenMode.Tunnel : LoadingScreenMode.Corner; // 전투 진입만 터널 모드
        }

        private static LoadingScreenView EnsureInstance() // 전역 로딩창 확보
        {
            if (instance != null) // 기존 로딩창 확인
            {
                return instance; // 기존 로딩창 반환
            }

            GameObject root = new GameObject("LoadingScreenRuntime", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(LoadingScreenView)); // 로딩창 Canvas 생성
            DontDestroyOnLoad(root); // 씬 전환 간 유지
            instance = root.GetComponent<LoadingScreenView>(); // 컴포넌트 조회
            instance.Build(); // 구성 요소 생성
            return instance; // 로딩창 반환
        }

        private void Build() // 로딩창 구성 요소 생성
        {
            canvas = GetComponent<Canvas>(); // Canvas 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 Overlay 렌더링 설정
            canvas.sortingOrder = 1000; // 모든 UI 위 표시 설정
            CanvasScaler scaler = GetComponent<CanvasScaler>(); // CanvasScaler 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 스케일 설정
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 해상도 설정
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응 방식 설정
            scaler.matchWidthOrHeight = 0.5f; // 가로 세로 중간 스케일 적용
            group = GetComponent<CanvasGroup>(); // 페이드 그룹 조회
            rootRect = (RectTransform)transform; // 루트 RectTransform 저장

            Image curtain = RuntimeUiKit.CreateImage(transform, "BlackCurtain", Color.black); // 검은 막 생성
            curtain.raycastTarget = true; // 로딩 중 입력 차단
            blackCurtain = curtain.rectTransform; // 검은 막 저장
            Image edge = RuntimeUiKit.CreateImage(transform, "CurtainEdge", Color.black); // 검은 막 흐린 경계 생성
            edge.sprite = GetHorizontalFadeSprite(); // 왼쪽 불투명 → 오른쪽 투명 그라데이션 적용
            edge.raycastTarget = false; // 입력 비활성화
            curtainEdge = edge.rectTransform; // 경계 저장

            exitGlow = CreateGlow("ExitGlow", ExitGlowWidthPixels); // 오른쪽 끝 출구 빛 생성
            RectTransform exitRect = exitGlow.rectTransform; // 출구 빛 RectTransform 조회
            exitRect.anchorMin = new Vector2(1f, 0f); // 화면 오른쪽 끝 하단 앵커
            exitRect.anchorMax = new Vector2(1f, 1f); // 화면 오른쪽 끝 상단 앵커
            exitRect.pivot = new Vector2(1f, 0.5f); // 오른쪽 기준 피벗
            exitRect.localScale = new Vector3(-1f, 1f, 1f); // 좌우 반전 (오른쪽이 가장 밝게)
            exitRect.anchoredPosition = new Vector2(-ExitGlowWidthPixels, 0f); // 반전 보정 위치 적용
            revealGlow = CreateGlow("RevealGlow", 200f); // 걷히는 경계 빛 번짐 생성

            cornerGroup = BuildCornerGroup(); // 우하단 모드 묶음 생성
            tunnelGroup = BuildTunnelGroup(); // 터널 모드 묶음 생성

            tipText = CreateText(transform, "TipText", string.Empty, 24, SubTextColor, TextAnchor.MiddleCenter); // Tip 문구 생성
            tipGroup = tipText.gameObject.AddComponent<CanvasGroup>(); // Tip 페이드 그룹 추가
            RectTransform tipRect = tipText.rectTransform; // Tip RectTransform 조회
            tipRect.anchorMin = new Vector2(0.5f, 0f); // 중앙 하단 앵커 설정
            tipRect.anchorMax = new Vector2(0.5f, 0f); // 중앙 하단 앵커 설정
            tipRect.pivot = new Vector2(0.5f, 0f); // 하단 기준 피벗 설정
            tipRect.sizeDelta = new Vector2(1080f, 40f); // Tip 영역 크기 설정
            tipRect.anchoredPosition = new Vector2(0f, 72f); // 하단 여백 적용

            canvas.enabled = false; // 초기 숨김
            group.alpha = 0f; // 초기 투명 적용
            group.blocksRaycasts = false; // 숨김 중 입력 통과
        }

        private Image CreateGlow(string name, float width) // 가로 그라데이션 빛 생성 (왼쪽 밝음 → 오른쪽 투명)
        {
            Image glow = RuntimeUiKit.CreateImage(transform, name, new Color(ExitLightColor.r, ExitLightColor.g, ExitLightColor.b, 0f)); // 빛 이미지 생성 (초기 투명)
            glow.sprite = GetHorizontalFadeSprite(); // 그라데이션 적용
            glow.raycastTarget = false; // 입력 비활성화
            RectTransform rect = glow.rectTransform; // RectTransform 조회
            rect.anchorMin = new Vector2(0f, 0f); // 하단 앵커 설정
            rect.anchorMax = new Vector2(0f, 1f); // 상단 앵커 설정 (화면 높이 전체)
            rect.pivot = new Vector2(0f, 0.5f); // 왼쪽 기준 피벗
            rect.sizeDelta = new Vector2(width, 0f); // 폭 적용
            return glow; // 빛 반환
        }

        private GameObject BuildCornerGroup() // 우하단 모드: 곱슬머리 러너 + 곰인형 + 로딩바
        {
            RectTransform corner = CreateRect(transform, "CornerGroup", new Vector2(1f, 0f), new Vector2(-70f, 64f), new Vector2(320f, 250f)); // 우하단 묶음 영역 생성
            cornerRunner = CreateRunner(corner, "Runner", new Vector2(40f, 62f), 1f, HairStyle.Curly, 0f); // 곱슬머리 러너 생성

            RectTransform companionRoot = CreateRect(corner, "Companion", new Vector2(0.5f, 0f), new Vector2(-62f, 62f), new Vector2(60f, 80f)); // 곰인형 루트 생성
            companionBody = CreateRect(companionRoot, "Body", new Vector2(0.5f, 0f), Vector2.zero, new Vector2(60f, 80f)); // 곰인형 흔들림 루트 생성
            companionBackLeg = CreateLimb(companionBody, "BackLeg", new Vector2(-5f, 16f), new Vector2(10f, 18f)); // 곰인형 뒷다리 생성
            CreateDisc(companionBody, "Belly", new Vector2(0f, 28f), new Vector2(30f, 34f), SilhouetteColor); // 곰인형 몸통 생성
            companionFrontLeg = CreateLimb(companionBody, "FrontLeg", new Vector2(5f, 16f), new Vector2(10f, 18f)); // 곰인형 앞다리 생성
            CreateDisc(companionBody, "EarBack", new Vector2(-8f, 66f), new Vector2(13f, 13f), SilhouetteColor); // 곰인형 뒷귀 생성
            CreateDisc(companionBody, "EarFront", new Vector2(12f, 66f), new Vector2(13f, 13f), SilhouetteColor); // 곰인형 앞귀 생성
            CreateDisc(companionBody, "Head", new Vector2(3f, 54f), new Vector2(30f, 28f), SilhouetteColor); // 곰인형 머리 생성
            CreateDisc(companionBody, "Snout", new Vector2(16f, 51f), new Vector2(11f, 9f), SilhouetteColor); // 곰인형 주둥이 생성

            cornerHud = CreateHud(corner, 280f, 40f); // 로딩바·문구 생성
            return corner.gameObject; // 묶음 반환
        }

        private GameObject BuildTunnelGroup() // 터널 모드: 화면 중앙 4인 러너 + 흐르는 바닥선 + 로딩바
        {
            RectTransform tunnel = CreateRect(transform, "TunnelGroup", new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(900f, 300f)); // 화면 중앙 묶음 영역 생성

            for (int index = 0; index < 11; index++) // 바닥선 점선 생성
            {
                Image dash = RuntimeUiKit.CreateImage(tunnel, $"GroundDash_{index}", new Color(1f, 1f, 1f, 0.22f)); // 점선 이미지 생성
                dash.raycastTarget = false; // 입력 비활성화
                RectTransform dashRect = dash.rectTransform; // 점선 RectTransform 조회
                dashRect.anchorMin = new Vector2(0.5f, 0f); // 하단 중앙 앵커 설정
                dashRect.anchorMax = new Vector2(0.5f, 0f); // 하단 중앙 앵커 설정
                dashRect.pivot = new Vector2(0.5f, 0.5f); // 중심 피벗 설정
                dashRect.sizeDelta = new Vector2(46f, 4f); // 점선 크기 설정
                groundDashes.Add(dashRect); // 점선 목록 등록
            }

            HairStyle[] styles = { HairStyle.TwinTails, HairStyle.Ponytail, HairStyle.Bob, HairStyle.Long }; // 뒤에서 앞 순서 머리 모양 (4인 파티)
            float[] scales = { 0.90f, 0.96f, 1.06f, 1.00f }; // 러너별 키 차이
            float[] offsets = { -270f, -90f, 90f, 270f }; // 러너별 가로 위치 (앞사람이 오른쪽)

            for (int index = 0; index < styles.Length; index++) // 4인 러너 생성
            {
                RunnerRig rig = CreateRunner(tunnel, $"PartyRunner_{index}", new Vector2(offsets[index], 92f), scales[index], styles[index], index * 0.9f); // 위상 차이 러너 생성
                rig.NormalizedX = 0.5f + (offsets[index] / 1920f); // 화면 가로 위치 비율 기록 (기준 해상도)
                tunnelRunners.Add(rig); // 러너 목록 등록
            }

            tunnelHud = CreateHud(tunnel, 460f, 34f); // 로딩바·문구 생성
            return tunnel.gameObject; // 묶음 반환
        }

        private HudRefs CreateHud(RectTransform parent, float barWidth, float barY) // 로딩바와 NOW LOADING·진행률 문구 생성
        {
            GameObject hudObject = new GameObject("Hud", typeof(RectTransform), typeof(CanvasGroup)); // 묶음 루트 생성
            hudObject.transform.SetParent(parent, false); // 부모 연결
            RuntimeUiKit.Stretch((RectTransform)hudObject.transform); // 부모 전체 확장

            RectTransform barBack = CreateRect(hudObject.transform, "BarBack", new Vector2(0.5f, 0f), new Vector2(0f, barY), new Vector2(barWidth, 10f)); // 로딩바 배경 영역 생성
            Image barBackImage = barBack.gameObject.AddComponent<Image>(); // 배경 이미지 추가
            barBackImage.color = BarBackColor; // 배경 색상 적용
            barBackImage.raycastTarget = false; // 입력 비활성화
            Image fill = RuntimeUiKit.CreateImage(barBack, "BarFill", Color.white); // 채움 생성
            fill.raycastTarget = false; // 입력 비활성화
            RectTransform fillRect = fill.rectTransform; // 채움 RectTransform 조회
            fillRect.anchorMin = Vector2.zero; // 채움 최소 앵커 설정
            fillRect.anchorMax = new Vector2(0f, 1f); // 채움 0% 시작 (Filled 타입 대신 앵커 확장 방식)
            fillRect.offsetMin = Vector2.zero; // 최소 오프셋 초기화
            fillRect.offsetMax = Vector2.zero; // 최대 오프셋 초기화

            Text loading = CreateText(hudObject.transform, "LoadingText", "NOW LOADING", 20, Color.white, TextAnchor.MiddleLeft); // NOW LOADING 문구 생성
            PlaceUnderBar(loading.rectTransform, barWidth, barY); // 로딩바 아래 배치
            Text percent = CreateText(hudObject.transform, "PercentText", "0%", 20, SubTextColor, TextAnchor.MiddleRight); // 진행률 문구 생성
            PlaceUnderBar(percent.rectTransform, barWidth, barY); // 로딩바 아래 오른쪽 정렬 배치

            return new HudRefs { BarFill = fillRect, Loading = loading, Percent = percent, Group = hudObject.GetComponent<CanvasGroup>() }; // 참조 묶음 반환
        }

        private static void PlaceUnderBar(RectTransform rect, float width, float barY) // 로딩바 바로 아래 문구 배치
        {
            rect.anchorMin = new Vector2(0.5f, 0f); // 하단 중앙 앵커 설정
            rect.anchorMax = new Vector2(0.5f, 0f); // 하단 중앙 앵커 설정
            rect.pivot = new Vector2(0.5f, 1f); // 상단 기준 피벗 설정
            rect.sizeDelta = new Vector2(width, 28f); // 문구 영역 크기 설정
            rect.anchoredPosition = new Vector2(0f, barY - 4f); // 로딩바 아래 여백 적용
        }

        private RunnerRig CreateRunner(RectTransform parent, string name, Vector2 footPosition, float scale, HairStyle hair, float phaseOffset) // 원형 조합 달리는 사람 실루엣 생성 (오른쪽을 향함)
        {
            RectTransform shadow = CreateDisc(parent, $"{name}_Shadow", footPosition + new Vector2(0f, -4f), new Vector2(120f * scale, 11f), new Color(1f, 1f, 1f, 0.16f)); // 발밑 그림자 생성
            RectTransform root = CreateRect(parent, name, new Vector2(0.5f, 0f), footPosition, new Vector2(120f, 170f)); // 러너 루트 생성
            root.localScale = new Vector3(scale, scale, 1f); // 키 차이 배율 적용
            CanvasGroup runnerGroup = root.gameObject.AddComponent<CanvasGroup>(); // 개별 페이드 그룹 추가
            RectTransform body = CreateRect(root, "Body", new Vector2(0.5f, 0f), Vector2.zero, new Vector2(120f, 170f)); // 흔들림 몸통 루트 생성

            if (hair == HairStyle.Long) // 긴 머리 확인
            {
                CreateDisc(body, "HairBack", new Vector2(-8f, 104f), new Vector2(34f, 62f), SilhouetteColor); // 등 뒤로 흐르는 긴 머리 (몸통 뒤 렌더링)
            }

            RectTransform backArm = CreateLimb(body, "BackArm", new Vector2(-4f, 92f), new Vector2(13f, 40f)); // 뒷팔 생성
            RectTransform backLeg = CreateLimb(body, "BackLeg", new Vector2(-4f, 50f), new Vector2(17f, 52f)); // 뒷다리 생성
            CreateDisc(body, "Torso", new Vector2(0f, 72f), new Vector2(42f, 58f), SilhouetteColor).localRotation = Quaternion.Euler(0f, 0f, -10f); // 앞으로 기운 몸통 생성
            RectTransform frontLeg = CreateLimb(body, "FrontLeg", new Vector2(4f, 50f), new Vector2(17f, 52f)); // 앞다리 생성
            CreateDisc(body, "Head", new Vector2(8f, 118f), new Vector2(46f, 46f), SilhouetteColor); // 머리 생성
            BuildHair(body, hair); // 머리 모양 생성
            RectTransform frontArm = CreateLimb(body, "FrontArm", new Vector2(6f, 92f), new Vector2(13f, 40f)); // 앞팔 생성

            return new RunnerRig { Body = body, FrontLeg = frontLeg, BackLeg = backLeg, FrontArm = frontArm, BackArm = backArm, Shadow = shadow, Group = runnerGroup, PhaseOffset = phaseOffset }; // 관절 참조 반환
        }

        private static void BuildHair(RectTransform body, HairStyle hair) // 머리 모양별 원 조합 생성
        {
            switch (hair) // 머리 모양 분기
            {
                case HairStyle.Curly: // 곱슬머리 처리
                    Vector2[] curls = { new Vector2(-10f, 132f), new Vector2(4f, 142f), new Vector2(20f, 138f), new Vector2(28f, 124f), new Vector2(-16f, 118f), new Vector2(12f, 130f), new Vector2(-4f, 124f) }; // 곱슬 뭉치 위치
                    float[] sizes = { 30f, 32f, 28f, 24f, 24f, 26f, 26f }; // 곱슬 뭉치 크기

                    for (int index = 0; index < curls.Length; index++) // 곱슬 뭉치 순회
                    {
                        CreateDisc(body, $"Curl_{index}", curls[index], new Vector2(sizes[index], sizes[index]), SilhouetteColor); // 곱슬 원 생성
                    }

                    break; // 곱슬머리 분기 종료
                case HairStyle.Long: // 긴 머리 처리
                    CreateDisc(body, "HairTop", new Vector2(4f, 128f), new Vector2(50f, 32f), SilhouetteColor); // 정수리 머리 생성
                    break; // 긴 머리 분기 종료
                case HairStyle.Bob: // 단발 처리
                    CreateDisc(body, "HairTop", new Vector2(6f, 126f), new Vector2(54f, 40f), SilhouetteColor); // 둥근 단발 윗부분 생성
                    CreateDisc(body, "HairBack", new Vector2(-10f, 112f), new Vector2(26f, 32f), SilhouetteColor); // 단발 뒷부분 생성
                    break; // 단발 분기 종료
                case HairStyle.Ponytail: // 포니테일 처리
                    CreateDisc(body, "HairTop", new Vector2(6f, 128f), new Vector2(50f, 30f), SilhouetteColor); // 정수리 머리 생성
                    CreateDisc(body, "Tail", new Vector2(-22f, 118f), new Vector2(16f, 40f), SilhouetteColor).localRotation = Quaternion.Euler(0f, 0f, 35f); // 뒤로 날리는 포니테일 생성
                    break; // 포니테일 분기 종료
                case HairStyle.TwinTails: // 양갈래 처리
                    CreateDisc(body, "HairTop", new Vector2(6f, 128f), new Vector2(50f, 30f), SilhouetteColor); // 정수리 머리 생성
                    CreateDisc(body, "TailBack", new Vector2(-18f, 108f), new Vector2(13f, 32f), SilhouetteColor).localRotation = Quaternion.Euler(0f, 0f, 20f); // 뒤쪽 양갈래 생성
                    CreateDisc(body, "TailFront", new Vector2(26f, 108f), new Vector2(13f, 30f), SilhouetteColor).localRotation = Quaternion.Euler(0f, 0f, 10f); // 앞쪽 양갈래 생성
                    break; // 양갈래 분기 종료
            }
        }

        private void Begin(AsyncOperation loadOperation, LoadingScreenMode loadingMode) // 로딩 표시 시작
        {
            operation = loadOperation; // 전환 작업 저장
            mode = loadingMode; // 연출 모드 저장

            if (operation != null) // 전환 작업 확인
            {
                operation.allowSceneActivation = false; // 최소 표시 시간과 로딩바 완료 전까지 새 씬 활성화 보류
            }

            state = LoadState.Loading; // 로딩 상태 적용
            shownAt = Time.unscaledTime; // 표시 시작 시각 기록
            displayedProgress = 0f; // 표시 진행률 초기화
            fadeTimer = 0f; // 경과 시간 초기화
            cornerGroup.SetActive(mode == LoadingScreenMode.Corner); // 우하단 묶음 표시 여부 적용
            tunnelGroup.SetActive(mode == LoadingScreenMode.Tunnel); // 터널 묶음 표시 여부 적용
            activeHud = mode == LoadingScreenMode.Tunnel ? tunnelHud : cornerHud; // 현재 모드 로딩바 선택
            activeHud.Group.alpha = 1f; // 로딩바 표시
            tipGroup.alpha = 1f; // Tip 표시

            for (int index = 0; index < tunnelRunners.Count; index++) // 터널 러너 순회
            {
                tunnelRunners[index].Group.alpha = 1f; // 러너 표시 초기화
            }

            SetCurtain(1f); // 검은 막 전체 덮기
            SetGlow(exitGlow, mode == LoadingScreenMode.Tunnel ? 0.06f : 0f); // 터널 모드만 희미한 출구 빛
            SetGlow(revealGlow, 0f); // 경계 빛 숨김
            tipText.text = $"<color=#FFD966>Tip :</color>  {LoadingTipCatalog.PickNext()}"; // 무작위 Tip 적용
            SetBar(0f); // 로딩바 초기화
            canvas.enabled = true; // Canvas 표시
            group.alpha = 1f; // 즉시 불투명 표시
            group.blocksRaycasts = true; // 로딩 중 입력 차단
        }

        private void Update() // 로딩 진행 및 연출 갱신
        {
            if (state == LoadState.Idle) // 대기 상태 확인
            {
                return; // 갱신 중단
            }

            float time = Time.unscaledTime; // 일시정지 무관 시간 조회
            AnimateRunners(time); // 달리기 연출 갱신

            switch (state) // 진행 상태 분기
            {
                case LoadState.Loading: // 로딩 진행 처리
                    UpdateLoading(); // 로딩바 진행
                    break; // 로딩 분기 종료
                case LoadState.Activating: // 씬 활성화 대기 처리
                    if (operation == null || operation.isDone) // 새 씬 활성화 완료 확인
                    {
                        state = mode == LoadingScreenMode.Tunnel ? LoadState.Revealing : LoadState.FadingOut; // 모드별 퇴장 연출 선택
                        fadeTimer = 0f; // 경과 시간 초기화
                        group.blocksRaycasts = false; // 새 씬 입력 허용
                    }
                    break; // 활성화 분기 종료
                case LoadState.FadingOut: // 우하단 모드 사라짐 처리
                    fadeTimer += Time.unscaledDeltaTime; // 경과 시간 누적
                    group.alpha = 1f - Mathf.Clamp01(fadeTimer / FadeOutSeconds); // 전체 알파 감소

                    if (fadeTimer >= FadeOutSeconds) // 사라짐 완료 확인
                    {
                        Finish(); // 로딩창 종료
                    }
                    break; // 사라짐 분기 종료
                case LoadState.Revealing: // 터널 모드 걷힘 처리
                    UpdateReveal(); // 오른쪽부터 검은 막 걷기
                    break; // 걷힘 분기 종료
            }
        }

        private void UpdateLoading() // 실제 진행률과 최소 표시 시간 기반 로딩바 진행
        {
            float realProgress = operation == null ? 1f : Mathf.Clamp01(operation.progress / AsyncReadyProgress); // 실제 로딩 진행률 (활성화 보류 시 0.9가 완료)
            float timeProgress = Mathf.Clamp01((Time.unscaledTime - shownAt) / MinDisplaySeconds); // 최소 표시 시간 진행률
            float target = Mathf.Min(realProgress, timeProgress); // 두 진행률 중 느린 쪽을 목표로 사용
            displayedProgress = Mathf.MoveTowards(displayedProgress, target, BarFillSpeed * Time.unscaledDeltaTime); // 부드러운 채움
            SetBar(displayedProgress); // 로딩바 적용

            if (mode == LoadingScreenMode.Tunnel) // 터널 모드 확인
            {
                float pulse = 0.03f * Mathf.Sin(Time.unscaledTime * 3f); // 출구 빛 은은한 맥동
                SetGlow(exitGlow, 0.06f + (0.30f * displayedProgress) + pulse); // 진행할수록 출구 빛이 밝아짐 (터널 끝 접근)
            }

            if (displayedProgress < 1f || realProgress < 1f) // 로딩바 완료 및 실제 준비 완료 확인
            {
                return; // 진행 유지
            }

            if (operation != null) // 전환 작업 확인
            {
                operation.allowSceneActivation = true; // 새 씬 활성화 허용
            }

            state = LoadState.Activating; // 활성화 대기 상태 전환
        }

        private void UpdateReveal() // 터널 출구: 검은 막이 오른쪽부터 왼쪽으로 걷히며 전투 화면 공개
        {
            fadeTimer += Time.unscaledDeltaTime; // 경과 시간 누적
            float t = Mathf.Clamp01(fadeTimer / RevealSeconds); // 걷힘 진행률
            float eased = t * t * (3f - (2f * t)); // 부드러운 가감속 적용
            float curtainRight = 1f - eased; // 검은 막 오른쪽 경계 위치 (1 → 0)
            SetCurtain(curtainRight); // 검은 막 적용

            float hudAlpha = 1f - Mathf.Clamp01(fadeTimer / HudFadeSeconds); // 로딩바·Tip 빠른 사라짐
            activeHud.Group.alpha = hudAlpha; // 로딩바 알파 적용
            tipGroup.alpha = hudAlpha; // Tip 알파 적용
            SetGlow(exitGlow, Mathf.Lerp(0.36f, 0f, t * 2f)); // 출구 빛은 경계 빛으로 넘겨주며 소멸
            SetGlow(revealGlow, 0.55f * (1f - t)); // 경계 빛 번짐 (출구를 막 빠져나오는 순간이 가장 밝음)

            for (int index = 0; index < tunnelRunners.Count; index++) // 러너 순회
            {
                RunnerRig rig = tunnelRunners[index]; // 러너 조회

                if (curtainRight < rig.NormalizedX + 0.02f) // 경계선이 러너를 지나갔는지 확인
                {
                    rig.Group.alpha = Mathf.MoveTowards(rig.Group.alpha, 0f, Time.unscaledDeltaTime / RunnerFadeSeconds); // 빛 속으로 사라짐
                }
            }

            if (t >= 1f) // 걷힘 완료 확인
            {
                Finish(); // 로딩창 종료
            }
        }

        private void Finish() // 로딩창 종료 및 초기 상태 복구
        {
            canvas.enabled = false; // Canvas 숨김
            state = LoadState.Idle; // 대기 상태 복귀
            operation = null; // 작업 참조 해제
            SetCurtain(1f); // 다음 표시를 위해 검은 막 복구
        }

        private void SetCurtain(float rightEdge) // 검은 막 오른쪽 경계 설정 (0~1, 흐린 경계 포함)
        {
            float clamped = Mathf.Clamp01(rightEdge); // 경계 범위 보정
            blackCurtain.anchorMin = Vector2.zero; // 왼쪽 하단 앵커
            blackCurtain.anchorMax = new Vector2(clamped, 1f); // 오른쪽 경계까지 덮기
            blackCurtain.offsetMin = Vector2.zero; // 최소 오프셋 초기화
            blackCurtain.offsetMax = Vector2.zero; // 최대 오프셋 초기화
            curtainEdge.anchorMin = new Vector2(clamped, 0f); // 경계 하단 앵커
            curtainEdge.anchorMax = new Vector2(clamped, 1f); // 경계 상단 앵커
            curtainEdge.pivot = new Vector2(0f, 0.5f); // 왼쪽 기준 피벗
            curtainEdge.sizeDelta = new Vector2(clamped >= 1f ? 0f : EdgeSoftnessPixels, 0f); // 전부 덮을 때는 경계 숨김
            curtainEdge.anchoredPosition = Vector2.zero; // 경계 위치 적용
            RectTransform glowRect = revealGlow.rectTransform; // 경계 빛 RectTransform 조회
            glowRect.anchorMin = new Vector2(clamped, 0f); // 경계 빛 하단 앵커
            glowRect.anchorMax = new Vector2(clamped, 1f); // 경계 빛 상단 앵커
            glowRect.anchoredPosition = Vector2.zero; // 경계 빛 위치 적용
        }

        private static void SetGlow(Image glow, float alpha) // 빛 알파 적용
        {
            Color color = glow.color; // 현재 색상 조회
            glow.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha)); // 알파 적용
        }

        private void SetBar(float progress) // 로딩바·문구 갱신
        {
            if (activeHud == null) // 로딩바 선택 확인
            {
                return; // 갱신 중단
            }

            activeHud.BarFill.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f); // 앵커 확장 방식 채움 적용
            activeHud.Percent.text = $"{Mathf.RoundToInt(progress * 100f)}%"; // 진행률 문구 적용
            int dots = Mathf.FloorToInt(Time.unscaledTime * 3f) % 4; // 0~3개 점 애니메이션 계산
            activeHud.Loading.text = "NOW LOADING" + new string('.', dots); // 점 애니메이션 적용
        }

        private void AnimateRunners(float time) // 모드별 달리기 연출 갱신
        {
            if (mode == LoadingScreenMode.Corner) // 우하단 모드 확인
            {
                AnimateRig(cornerRunner, time, RunCycleSeconds); // 곱슬머리 러너 갱신
                AnimateCompanion(time); // 곰인형 갱신
                return; // 우하단 모드 종료
            }

            float cycle = state == LoadState.Revealing ? SprintCycleSeconds : RunCycleSeconds; // 출구에서는 전력질주
            float speed = state == LoadState.Revealing ? GroundScrollSpeed * 1.6f : GroundScrollSpeed; // 출구에서는 바닥 흐름도 가속

            for (int index = 0; index < tunnelRunners.Count; index++) // 4인 러너 순회
            {
                AnimateRig(tunnelRunners[index], time, cycle); // 러너 갱신
            }

            float span = GroundDashSpacing * groundDashes.Count; // 점선 전체 반복 길이
            float scroll = Mathf.Repeat(time * speed, GroundDashSpacing); // 점선 흐름 오프셋 계산

            for (int index = 0; index < groundDashes.Count; index++) // 점선 순회
            {
                float x = (index * GroundDashSpacing) - scroll - (span * 0.5f); // 왼쪽으로 흐르는 점선 위치 계산 (달리는 방향의 반대)
                groundDashes[index].anchoredPosition = new Vector2(x, 84f); // 발밑 높이 점선 위치 적용
            }
        }

        private static void AnimateRig(RunnerRig rig, float time, float cycleSeconds) // 단일 러너 팔다리 교차 흔들림과 상하 반동
        {
            float phase = ((time / cycleSeconds) * Mathf.PI * 2f) + rig.PhaseOffset; // 달리기 위상 계산
            float swing = Mathf.Sin(phase); // 팔다리 흔들림 계수
            float bob = Mathf.Abs(Mathf.Sin(phase)) * BobHeight; // 상하 반동 높이
            rig.Body.anchoredPosition = new Vector2(0f, bob); // 몸통 반동 적용
            rig.FrontLeg.localRotation = Quaternion.Euler(0f, 0f, swing * LegSwingDegrees); // 앞다리 흔들림
            rig.BackLeg.localRotation = Quaternion.Euler(0f, 0f, -swing * LegSwingDegrees); // 뒷다리 반대 흔들림
            rig.FrontArm.localRotation = Quaternion.Euler(0f, 0f, -swing * ArmSwingDegrees); // 앞팔 다리 반대 흔들림
            rig.BackArm.localRotation = Quaternion.Euler(0f, 0f, swing * ArmSwingDegrees); // 뒷팔 반대 흔들림
            float shadowScale = 1f - (bob / BobHeight) * 0.12f; // 떠오를수록 그림자 축소
            rig.Shadow.localScale = new Vector3(shadowScale, 1f, 1f); // 그림자 크기 적용
        }

        private void AnimateCompanion(float time) // 곰인형 종종걸음 연출
        {
            float phase = ((time / RunCycleSeconds) * Mathf.PI * 2f * 1.35f) + 1.2f; // 더 빠른 종종걸음 위상
            float swing = Mathf.Sin(phase); // 다리 흔들림 계수
            companionBody.anchoredPosition = new Vector2(0f, Mathf.Abs(swing) * (BobHeight * 0.8f)); // 반동 적용
            companionFrontLeg.localRotation = Quaternion.Euler(0f, 0f, swing * 45f); // 앞다리 흔들림
            companionBackLeg.localRotation = Quaternion.Euler(0f, 0f, -swing * 45f); // 뒷다리 흔들림
        }

        private static Sprite GetHorizontalFadeSprite() // 가로 알파 그라데이션 스프라이트 (왼쪽 불투명 → 오른쪽 투명, 부드러운 곡선)
        {
            if (horizontalFadeSprite != null) // 캐시 확인
            {
                return horizontalFadeSprite; // 캐시 반환
            }

            const int width = 256; // 텍스처 폭
            Texture2D texture = new Texture2D(width, 4, TextureFormat.RGBA32, false); // 텍스처 생성
            texture.wrapMode = TextureWrapMode.Clamp; // 가장자리 반복 방지
            texture.hideFlags = HideFlags.HideAndDontSave; // Scene 저장 대상 제외

            for (int x = 0; x < width; x++) // 가로 픽셀 순회
            {
                float t = x / (float)(width - 1); // 가로 진행률
                float alpha = 1f - (t * t * (3f - (2f * t))); // 부드러운 감쇠 곡선
                Color color = new Color(1f, 1f, 1f, alpha); // 흰색 알파 픽셀 (색상은 Image.color로 지정)

                for (int y = 0; y < 4; y++) // 세로 픽셀 순회
                {
                    texture.SetPixel(x, y, color); // 픽셀 적용
                }
            }

            texture.Apply(false, true); // 텍스처 확정 (읽기 불필요)
            horizontalFadeSprite = Sprite.Create(texture, new Rect(0f, 0f, width, 4f), new Vector2(0.5f, 0.5f)); // 스프라이트 생성
            horizontalFadeSprite.hideFlags = HideFlags.HideAndDontSave; // Scene 저장 대상 제외
            return horizontalFadeSprite; // 스프라이트 반환
        }

        private static RectTransform CreateRect(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size) // 점 앵커 빈 RectTransform 생성 (가로는 앵커 기준, 세로는 하단 기준 피벗)
        {
            GameObject rectObject = new GameObject(name, typeof(RectTransform)); // 객체 생성
            rectObject.transform.SetParent(parent, false); // 부모 연결
            RectTransform rect = rectObject.GetComponent<RectTransform>(); // RectTransform 조회
            rect.anchorMin = anchor; // 최소 앵커 적용
            rect.anchorMax = anchor; // 최대 앵커 적용
            rect.pivot = new Vector2(anchor.x, 0f); // 피벗 적용
            rect.sizeDelta = size; // 크기 적용
            rect.anchoredPosition = position; // 위치 적용
            return rect; // 생성 RectTransform 반환
        }

        private static RectTransform CreateDisc(RectTransform parent, string name, Vector2 center, Vector2 size, Color color) // 원·타원 실루엣 조각 생성 (부모 하단 중앙 기준 좌표)
        {
            Image image = RuntimeUiKit.CreateImage(parent, name, color); // 이미지 생성
            image.sprite = RhythmCircleSpriteFactory.GetDiscSprite(); // Day49 원형 스프라이트 재사용
            image.raycastTarget = false; // 입력 비활성화
            RectTransform rect = image.rectTransform; // RectTransform 조회
            rect.anchorMin = new Vector2(0.5f, 0f); // 하단 중앙 앵커 설정
            rect.anchorMax = new Vector2(0.5f, 0f); // 하단 중앙 앵커 설정
            rect.pivot = new Vector2(0.5f, 0.5f); // 중심 피벗 설정
            rect.sizeDelta = size; // 크기 적용
            rect.anchoredPosition = center; // 중심 위치 적용
            return rect; // 조각 반환
        }

        private static RectTransform CreateLimb(RectTransform parent, string name, Vector2 joint, Vector2 size) // 관절에서 매달린 팔다리 생성 (관절 기준 회전)
        {
            RectTransform limb = CreateDisc(parent, name, joint, size, SilhouetteColor); // 긴 타원 조각 생성
            limb.pivot = new Vector2(0.5f, 1f); // 위쪽 끝(관절) 기준 피벗으로 변경
            limb.anchoredPosition = joint; // 관절 위치 적용
            return limb; // 팔다리 반환
        }

        private static Text CreateText(Transform parent, string name, string value, int fontSize, Color color, TextAnchor alignment) // 공통 텍스트 생성
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // 텍스트 객체 생성
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
            text.raycastTarget = false; // 입력 비활성화
            return text; // 텍스트 반환
        }

        private void OnDestroy() // 로딩창 제거 처리
        {
            if (instance == this) // 현재 로딩창 확인
            {
                instance = null; // 참조 해제
            }
        }
    }
}
