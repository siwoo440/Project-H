using ProjectH.Battle; // 전투 Scene 기능
using UnityEditor; // Unity 에디터 기능
using UnityEditor.SceneManagement; // 에디터 씬 관리
using UnityEngine; // Unity 기본 기능
using UnityEngine.EventSystems; // UI 이벤트 시스템 기능
using UnityEngine.InputSystem.UI; // 입력 시스템 UI 기능
using UnityEngine.SceneManagement; // Unity 씬 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.EditorTools // 프로젝트 에디터 도구 영역
{
    public static class Phase1Day11Setup // 11일차 전투 Scene 설정 도구
    {
        private const string BattleScenePath = "Assets/ProjectH/Scenes/Battle.unity"; // 전투 씬 경로
        private const string ArtRoot = "Assets/ProjectH/UI/Art/Prototype"; // 프로토타입 UI 아트 경로
        private static readonly Color Navy = new Color(0.12f, 0.20f, 0.34f, 1f); // HUD 남색
        private static readonly Color Cream = new Color(0.98f, 0.96f, 0.90f, 0.96f); // HUD 크림색
        private static readonly Color HpGreen = new Color(0.30f, 0.82f, 0.38f, 1f); // HP 게이지 초록색
        private static readonly Color GaugeBlue = new Color(0.26f, 0.68f, 0.92f, 1f); // 스킬 게이지 파랑색

        [MenuItem("Tools/Project H/Phase 1/11일차 Battle Scene 재구성")] // 전투 Scene 재구성 메뉴 등록
        public static void Setup() // 11일차 전투 Scene 구축
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); // 빈 전투 씬 생성
            Camera mainCamera = CreateMainCamera(); // 전투 메인 카메라 생성
            CreateEventSystem(); // UI 이벤트 시스템 생성
            CreateBackground(mainCamera); // 전투 배경 생성
            GameObject battleWorld = new GameObject("BattleWorld"); // 전투 월드 루트 생성
            BattleFormationAnchors formation = CreateFormation(battleWorld.transform); // 아군 적군 배치 앵커 생성
            Transform allyRoot = CreateChild(battleWorld.transform, "SpawnedAllies"); // 생성 아군 루트 생성
            BattleUnitView unitTemplate = CreateUnitTemplate(allyRoot, mainCamera); // 아군 전투 유닛 템플릿 생성
            CreateEnemyPreviews(battleWorld.transform, formation, mainCamera); // 임시 적군 표시 생성
            Canvas hudCanvas = CreateHudCanvas(mainCamera); // 전투 HUD Canvas 생성
            CreateTopHud(hudCanvas.transform, out Text waveText, out Text timeText, out Text statusText, out Button menuButton); // 상단 전투 HUD 생성
            BattleHudCardView[] hudCards = CreateBottomHud(hudCanvas.transform, out Button autoButton, out Text autoButtonText); // 하단 4인 HUD 생성
            CreateMenuPanel(hudCanvas.transform, out GameObject menuPanel, out Button returnDungeonButton, out Button closeMenuButton); // 전투 메뉴 패널 생성
            GameObject controllerObject = new GameObject("BattleController"); // 전투 컨트롤러 객체 생성
            BattleScreenController controller = controllerObject.AddComponent<BattleScreenController>(); // 전투 화면 컨트롤러 추가
            controller.Configure(formation, allyRoot, unitTemplate, hudCards, waveText, timeText, statusText, autoButtonText, menuPanel, menuButton, autoButton, returnDungeonButton, closeMenuButton); // 전투 컨트롤러 참조 연결
            EditorUtility.SetDirty(controller); // 전투 컨트롤러 변경 표시
            EditorSceneManager.MarkSceneDirty(scene); // 전투 씬 변경 표시
            EditorSceneManager.SaveScene(scene, BattleScenePath); // 전투 씬 저장
            AssetDatabase.SaveAssets(); // 에셋 변경 저장
            AssetDatabase.Refresh(); // 에셋 목록 갱신
            Debug.Log("[Project H][BATTLE] Phase 1 Day 11 Battle scene rebuild complete."); // 전투 Scene 구축 완료 로그
        }

        private static Camera CreateMainCamera() // 전투 메인 카메라 생성
        {
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); // 메인 카메라 객체 생성
            cameraObject.tag = "MainCamera"; // 메인 카메라 태그 설정
            cameraObject.transform.position = new Vector3(0f, 0f, -10f); // 2D 카메라 위치 설정
            Camera camera = cameraObject.GetComponent<Camera>(); // 카메라 컴포넌트 조회
            camera.orthographic = true; // 직교 카메라 설정
            camera.orthographicSize = 5f; // 전장 표시 크기 설정
            camera.clearFlags = CameraClearFlags.SolidColor; // 카메라 배경 초기화 방식 설정
            camera.backgroundColor = new Color(0.88f, 0.92f, 0.90f, 1f); // 카메라 기본 배경색 설정
            return camera; // 메인 카메라 반환
        }

        private static void CreateEventSystem() // UI EventSystem 생성
        {
            GameObject eventObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); // EventSystem 객체 생성
            InputSystemUIInputModule inputModule = eventObject.GetComponent<InputSystemUIInputModule>(); // 입력 모듈 조회
            inputModule.AssignDefaultActions(); // 기본 UI 입력 액션 연결
        }

        private static void CreateBackground(Camera camera) // 전투 배경 Canvas 생성
        {
            GameObject canvasObject = new GameObject("BackgroundCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler)); // 배경 Canvas 객체 생성
            Canvas canvas = canvasObject.GetComponent<Canvas>(); // 배경 Canvas 조회
            canvas.renderMode = RenderMode.ScreenSpaceCamera; // 카메라 공간 Canvas 설정
            canvas.worldCamera = camera; // 배경 카메라 연결
            canvas.planeDistance = 20f; // 전투 월드 뒤 배경 거리 설정
            canvas.sortingOrder = -20; // 배경 정렬 순서 설정
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // 배경 CanvasScaler 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 스케일 적용
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 해상도 설정
            Image background = CreateImage(canvas.transform, "BattleBackground", LoadSprite("Backgrounds/bg_battle.png"), Color.white); // 전투 배경 이미지 생성
            SetRect(background.rectTransform, Vector2.zero, Vector2.one); // 전투 배경 전체 화면 확장
            background.preserveAspect = false; // 전투 배경 화면 채우기 설정
        }

        private static BattleFormationAnchors CreateFormation(Transform parent) // 전투 진형 앵커 생성
        {
            GameObject formationObject = new GameObject("BattleFormation"); // 전투 진형 객체 생성
            formationObject.transform.SetParent(parent, false); // 전투 월드 연결
            BattleFormationAnchors formation = formationObject.AddComponent<BattleFormationAnchors>(); // 진형 앵커 컴포넌트 추가
            Vector3[] allyPositions = // 아군 배치 좌표 정의
            {
                new Vector3(-3.00f, 0.05f, 0f), // 아군 슬롯 0 좌표
                new Vector3(-4.05f, -1.10f, 0f), // 아군 슬롯 1 좌표
                new Vector3(-4.25f, 1.10f, 0f), // 아군 슬롯 2 좌표
                new Vector3(-3.35f, 1.75f, 0f) // 아군 슬롯 3 좌표
            }; // 아군 좌표 정의 종료
            Vector3[] enemyPositions = // 적군 배치 좌표 정의
            {
                new Vector3(3.00f, 0.05f, 0f), // 적군 슬롯 0 좌표
                new Vector3(4.05f, 1.20f, 0f), // 적군 슬롯 1 좌표
                new Vector3(4.15f, -1.10f, 0f), // 적군 슬롯 2 좌표
                new Vector3(3.15f, 1.85f, 0f), // 적군 슬롯 3 좌표
                new Vector3(5.00f, 0.10f, 0f) // 적군 슬롯 4 좌표
            }; // 적군 좌표 정의 종료
            Transform[] allies = CreateAnchorArray(formationObject.transform, "AllySlot", allyPositions); // 아군 앵커 배열 생성
            Transform[] enemies = CreateAnchorArray(formationObject.transform, "EnemySlot", enemyPositions); // 적군 앵커 배열 생성
            formation.Configure(allies, enemies); // 진형 앵커 배열 연결
            return formation; // 진형 컴포넌트 반환
        }

        private static Transform[] CreateAnchorArray(Transform parent, string prefix, Vector3[] positions) // 배치 앵커 배열 생성
        {
            Transform[] result = new Transform[positions.Length]; // 앵커 결과 배열 생성

            for (int index = 0; index < positions.Length; index++) // 배치 좌표 순회
            {
                GameObject anchorObject = new GameObject($"{prefix}_{index}"); // 배치 앵커 객체 생성
                anchorObject.transform.SetParent(parent, false); // 진형 객체 연결
                anchorObject.transform.position = positions[index]; // 월드 배치 좌표 적용
                result[index] = anchorObject.transform; // 앵커 배열 등록
            }

            return result; // 앵커 배열 반환
        }

        private static BattleUnitView CreateUnitTemplate(Transform parent, Camera camera) // 아군 전투 유닛 템플릿 생성
        {
            GameObject root = new GameObject("BattleUnitTemplate", typeof(BattleUnitView)); // 전투 유닛 템플릿 루트 생성
            root.transform.SetParent(parent, false); // 아군 생성 루트 연결
            BattleUnitView view = root.GetComponent<BattleUnitView>(); // 전투 유닛 뷰 조회
            Canvas canvas = CreateWorldCanvas(root.transform, "UnitCanvas", camera, new Vector2(190f, 285f), 0.006f, 5); // 유닛 월드 Canvas 생성
            Image body = CreateImage(canvas.transform, "Body", null, new Color(0.55f, 0.62f, 0.72f, 0.95f)); // 임시 캐릭터 바디 생성
            SetRect(body.rectTransform, new Vector2(0.17f, 0.25f), new Vector2(0.83f, 0.80f)); // 캐릭터 바디 영역 배치
            Text characterText = CreateText(body.transform, "CharacterText", "CHARACTER", 28, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter); // 캐릭터 이름 자리 생성
            Stretch(characterText.rectTransform, 8f); // 캐릭터 이름 영역 확장
            Text roleText = CreateText(canvas.transform, "RoleText", "ROLE", 16, FontStyle.Bold, Navy, TextAnchor.MiddleCenter); // 역할 텍스트 생성
            SetRect(roleText.rectTransform, new Vector2(0.25f, 0.82f), new Vector2(0.75f, 0.94f)); // 역할 텍스트 배치
            Text runtimeText = CreateText(canvas.transform, "RuntimeIdText", "ALLY_0", 13, FontStyle.Normal, Navy, TextAnchor.MiddleCenter); // 런타임 ID 텍스트 생성
            SetRect(runtimeText.rectTransform, new Vector2(0.25f, 0.93f), new Vector2(0.75f, 1.00f)); // 런타임 ID 텍스트 배치
            Image hpBack = CreateImage(canvas.transform, "HpBack", null, new Color(0.12f, 0.17f, 0.19f, 0.92f)); // 유닛 HP 배경 생성
            SetRect(hpBack.rectTransform, new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.20f)); // 유닛 HP 배경 배치
            Image hpFill = CreateImage(hpBack.transform, "HpFill", null, HpGreen); // 유닛 HP 게이지 생성
            SetRect(hpFill.rectTransform, Vector2.zero, Vector2.one); // 유닛 HP 게이지 전체 채우기
            Text hpText = CreateText(canvas.transform, "HpText", "0 / 0", 14, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter); // 유닛 HP 수치 생성
            SetRect(hpText.rectTransform, new Vector2(0.10f, 0.10f), new Vector2(0.90f, 0.22f)); // 유닛 HP 수치 배치
            view.Configure(canvas, body, characterText, runtimeText, roleText, hpText, hpFill); // 전투 유닛 뷰 참조 연결
            root.SetActive(false); // 전투 유닛 템플릿 초기 숨김
            return view; // 전투 유닛 템플릿 반환
        }

        private static void CreateEnemyPreviews(Transform parent, BattleFormationAnchors formation, Camera camera) // 임시 적군 표시 생성
        {
            GameObject previewRoot = new GameObject("EnemyPreviews"); // 적군 미리보기 루트 생성
            previewRoot.transform.SetParent(parent, false); // 전투 월드 연결

            for (int index = 0; index < 3; index++) // 초기 적군 3개 표시
            {
                Transform anchor = formation.GetEnemyAnchor(index); // 적군 배치 앵커 조회
                GameObject enemy = new GameObject($"ENEMY_{index}_PREVIEW"); // 임시 적군 객체 생성
                enemy.transform.SetParent(previewRoot.transform, false); // 적군 미리보기 루트 연결
                enemy.transform.position = anchor.position; // 적군 앵커 위치 적용
                Canvas canvas = CreateWorldCanvas(enemy.transform, "EnemyCanvas", camera, new Vector2(180f, 250f), 0.006f, 4); // 적군 월드 Canvas 생성
                Image body = CreateImage(canvas.transform, "EnemyBody", null, new Color(0.55f, 0.27f, 0.20f, 0.96f)); // 적군 임시 바디 생성
                SetRect(body.rectTransform, new Vector2(0.16f, 0.22f), new Vector2(0.84f, 0.83f)); // 적군 바디 배치
                Text bodyText = CreateText(body.transform, "EnemyText", $"ENEMY {index + 1}", 24, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter); // 적군 표시 텍스트 생성
                Stretch(bodyText.rectTransform, 8f); // 적군 텍스트 영역 확장
                Text status = CreateText(canvas.transform, "EnemyStatus", "MONSTER PREVIEW", 13, FontStyle.Bold, Navy, TextAnchor.MiddleCenter); // 적군 임시 상태 생성
                SetRect(status.rectTransform, new Vector2(0.15f, 0.08f), new Vector2(0.85f, 0.18f)); // 적군 상태 배치
            }
        }

        private static Canvas CreateHudCanvas(Camera camera) // 전투 HUD Canvas 생성
        {
            GameObject canvasObject = new GameObject("BattleHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // HUD Canvas 객체 생성
            Canvas canvas = canvasObject.GetComponent<Canvas>(); // HUD Canvas 조회
            canvas.renderMode = RenderMode.ScreenSpaceCamera; // HUD 카메라 공간 설정
            canvas.worldCamera = camera; // HUD 카메라 연결
            canvas.planeDistance = 5f; // HUD 카메라 거리 설정
            canvas.sortingOrder = 20; // HUD 정렬 순서 설정
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // HUD CanvasScaler 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 스케일 적용
            scaler.referenceResolution = new Vector2(1920f, 1080f); // HUD 기준 해상도 설정
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 해상도 대응 방식 설정
            scaler.matchWidthOrHeight = 0.5f; // 가로세로 대응 비율 설정
            return canvas; // HUD Canvas 반환
        }

        private static void CreateTopHud(Transform parent, out Text waveText, out Text timeText, out Text statusText, out Button menuButton) // 상단 전투 HUD 생성
        {
            Image wavePanel = CreateImage(parent, "WavePanel", LoadSprite("Buttons/button_small.png"), Cream); // 웨이브 패널 생성
            SetRect(wavePanel.rectTransform, new Vector2(0.018f, 0.915f), new Vector2(0.135f, 0.985f)); // 웨이브 패널 배치
            waveText = CreateText(wavePanel.transform, "WaveText", "WAVE 1 / 3", 24, FontStyle.Bold, Navy, TextAnchor.MiddleCenter); // 웨이브 텍스트 생성
            Stretch(waveText.rectTransform, 5f); // 웨이브 텍스트 확장
            Image timePanel = CreateImage(parent, "TimePanel", LoadSprite("Buttons/button_small.png"), Cream); // 전투 시간 패널 생성
            SetRect(timePanel.rectTransform, new Vector2(0.80f, 0.925f), new Vector2(0.885f, 0.982f)); // 전투 시간 패널 배치
            timeText = CreateText(timePanel.transform, "TimeText", "00:00", 22, FontStyle.Bold, Navy, TextAnchor.MiddleCenter); // 전투 시간 텍스트 생성
            Stretch(timeText.rectTransform, 5f); // 전투 시간 텍스트 확장
            menuButton = CreateButton(parent, "MenuButton", "MENU", "button_small.png", 21); // 전투 메뉴 버튼 생성
            SetRect(menuButton.GetComponent<RectTransform>(), new Vector2(0.895f, 0.92f), new Vector2(0.982f, 0.985f)); // 전투 메뉴 버튼 배치
            statusText = CreateText(parent, "BattleStatus", "전투 배치 준비 중", 17, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter); // 개발 상태 텍스트 생성
            SetRect(statusText.rectTransform, new Vector2(0.28f, 0.91f), new Vector2(0.72f, 0.965f)); // 개발 상태 텍스트 배치
        }

        private static BattleHudCardView[] CreateBottomHud(Transform parent, out Button autoButton, out Text autoButtonText) // 하단 캐릭터 HUD 생성
        {
            BattleHudCardView[] cards = new BattleHudCardView[4]; // HUD 카드 배열 생성
            float cardWidth = 0.14f; // HUD 카드 너비 설정
            float gap = 0.015f; // HUD 카드 간격 설정
            float groupWidth = (cardWidth * cards.Length) + (gap * (cards.Length - 1)); // HUD 카드 그룹 너비 계산
            float startX = (1f - groupWidth) * 0.5f; // HUD 카드 그룹 중앙 시작점 계산

            for (int index = 0; index < cards.Length; index++) // HUD 카드 순회
            {
                float minX = startX + index * (cardWidth + gap); // HUD 카드 X 위치 계산
                GameObject cardObject = new GameObject($"BattleHudCard_{index}", typeof(RectTransform), typeof(Image), typeof(BattleHudCardView)); // HUD 카드 객체 생성
                cardObject.transform.SetParent(parent, false); // HUD Canvas 연결
                Image frame = cardObject.GetComponent<Image>(); // HUD 카드 프레임 조회
                frame.sprite = LoadSprite("Frames/frame_party_slot.png"); // 파티 슬롯 프레임 적용
                frame.color = Color.white; // HUD 카드 프레임 색상 적용
                SetRect(cardObject.GetComponent<RectTransform>(), new Vector2(minX, 0.015f), new Vector2(minX + cardWidth, 0.235f)); // HUD 카드 배치
                Image portrait = CreateImage(cardObject.transform, "Portrait", LoadSprite("Frames/frame_portrait.png"), new Color(0.92f, 0.95f, 0.98f, 0.95f)); // HUD 초상화 영역 생성
                SetRect(portrait.rectTransform, new Vector2(0.08f, 0.39f), new Vector2(0.92f, 0.94f)); // HUD 초상화 배치
                Text portraitText = CreateText(portrait.transform, "PortraitText", "CHARACTER", 18, FontStyle.Bold, Navy, TextAnchor.MiddleCenter); // 임시 초상화 이름 생성
                Stretch(portraitText.rectTransform, 7f); // 초상화 이름 영역 확장
                Text nameText = CreateText(cardObject.transform, "NameText", "이름", 17, FontStyle.Bold, Navy, TextAnchor.MiddleCenter); // HUD 캐릭터 이름 생성
                SetRect(nameText.rectTransform, new Vector2(0.26f, 0.29f), new Vector2(0.94f, 0.40f)); // HUD 이름 배치
                Text levelText = CreateText(cardObject.transform, "LevelText", "Lv.1", 15, FontStyle.Bold, Navy, TextAnchor.MiddleLeft); // HUD 레벨 생성
                SetRect(levelText.rectTransform, new Vector2(0.07f, 0.29f), new Vector2(0.28f, 0.40f)); // HUD 레벨 배치
                Image hpBack = CreateImage(cardObject.transform, "HpBack", null, new Color(0.16f, 0.19f, 0.20f, 0.90f)); // HUD HP 배경 생성
                SetRect(hpBack.rectTransform, new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.25f)); // HUD HP 배경 배치
                Image hpFill = CreateImage(hpBack.transform, "HpFill", null, HpGreen); // HUD HP 게이지 생성
                SetRect(hpFill.rectTransform, Vector2.zero, Vector2.one); // HUD HP 게이지 채우기
                Text hpText = CreateText(cardObject.transform, "HpText", "0/0", 12, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter); // HUD HP 수치 생성
                SetRect(hpText.rectTransform, new Vector2(0.08f, 0.16f), new Vector2(0.92f, 0.27f)); // HUD HP 수치 배치
                Image gaugeBack = CreateImage(cardObject.transform, "GaugeBack", null, new Color(0.12f, 0.17f, 0.22f, 0.88f)); // HUD 스킬 게이지 배경 생성
                SetRect(gaugeBack.rectTransform, new Vector2(0.08f, 0.07f), new Vector2(0.92f, 0.13f)); // HUD 스킬 게이지 배경 배치
                Image gaugeFill = CreateImage(gaugeBack.transform, "GaugeFill", null, GaugeBlue); // HUD 스킬 게이지 생성
                SetRect(gaugeFill.rectTransform, Vector2.zero, Vector2.one); // HUD 스킬 게이지 초기 채우기
                BattleHudCardView cardView = cardObject.GetComponent<BattleHudCardView>(); // HUD 카드 뷰 조회
                cardView.Configure(nameText, levelText, portraitText, hpText, hpFill, gaugeFill); // HUD 카드 참조 연결
                cards[index] = cardView; // HUD 카드 배열 등록
            }

            autoButton = CreateButton(parent, "AutoButton", "AUTO ON", "button_primary.png", 20); // AUTO 버튼 생성
            SetRect(autoButton.GetComponent<RectTransform>(), new Vector2(0.90f, 0.11f), new Vector2(0.982f, 0.20f)); // AUTO 버튼 배치
            autoButtonText = autoButton.GetComponentInChildren<Text>(); // AUTO 버튼 텍스트 조회
            return cards; // HUD 카드 배열 반환
        }

        private static void CreateMenuPanel(Transform parent, out GameObject menuPanel, out Button returnDungeonButton, out Button closeMenuButton) // 전투 메뉴 패널 생성
        {
            Image overlay = CreateImage(parent, "BattleMenuPanel", null, new Color(0.04f, 0.07f, 0.12f, 0.66f)); // 전투 메뉴 오버레이 생성
            SetRect(overlay.rectTransform, Vector2.zero, Vector2.one); // 메뉴 오버레이 전체 확장
            overlay.raycastTarget = true; // 메뉴 외부 입력 차단
            Image panel = CreateImage(overlay.transform, "MenuWindow", LoadSprite("Frames/frame_panel.png"), Cream); // 전투 메뉴 창 생성
            SetRect(panel.rectTransform, new Vector2(0.35f, 0.30f), new Vector2(0.65f, 0.70f)); // 전투 메뉴 창 배치
            Text title = CreateText(panel.transform, "MenuTitle", "전투 메뉴", 32, FontStyle.Bold, Navy, TextAnchor.MiddleCenter); // 전투 메뉴 제목 생성
            SetRect(title.rectTransform, new Vector2(0.12f, 0.73f), new Vector2(0.88f, 0.92f)); // 전투 메뉴 제목 배치
            Text guide = CreateText(panel.transform, "MenuGuide", "11일차는 배치 확인 단계입니다.\n실제 전투 일시정지/설정은 이후 연결됩니다.", 19, FontStyle.Normal, Navy, TextAnchor.MiddleCenter); // 전투 메뉴 안내 생성
            SetRect(guide.rectTransform, new Vector2(0.12f, 0.47f), new Vector2(0.88f, 0.70f)); // 전투 메뉴 안내 배치
            returnDungeonButton = CreateButton(panel.transform, "ReturnDungeonButton", "던전 선택으로", "button_secondary.png", 21); // 던전 선택 복귀 버튼 생성
            SetRect(returnDungeonButton.GetComponent<RectTransform>(), new Vector2(0.16f, 0.20f), new Vector2(0.84f, 0.38f)); // 던전 복귀 버튼 배치
            closeMenuButton = CreateButton(panel.transform, "CloseMenuButton", "닫기", "button_small.png", 20); // 전투 메뉴 닫기 버튼 생성
            SetRect(closeMenuButton.GetComponent<RectTransform>(), new Vector2(0.28f, 0.06f), new Vector2(0.72f, 0.18f)); // 전투 메뉴 닫기 버튼 배치
            menuPanel = overlay.gameObject; // 전투 메뉴 패널 반환
            menuPanel.SetActive(false); // 전투 메뉴 초기 숨김
        }

        private static Canvas CreateWorldCanvas(Transform parent, string name, Camera camera, Vector2 size, float scale, int sortingOrder) // 월드 공간 Canvas 생성
        {
            GameObject canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas)); // 월드 Canvas 객체 생성
            canvasObject.transform.SetParent(parent, false); // 월드 객체 연결
            Canvas canvas = canvasObject.GetComponent<Canvas>(); // 월드 Canvas 조회
            canvas.renderMode = RenderMode.WorldSpace; // 월드 공간 렌더링 설정
            canvas.worldCamera = camera; // 월드 UI 카메라 연결
            canvas.sortingOrder = sortingOrder; // 월드 UI 정렬 순서 설정
            RectTransform rect = canvasObject.GetComponent<RectTransform>(); // 월드 Canvas RectTransform 조회
            rect.sizeDelta = size; // 월드 Canvas 기준 크기 설정
            rect.localScale = Vector3.one * scale; // 월드 Canvas 실제 크기 설정
            return canvas; // 월드 Canvas 반환
        }

        private static Transform CreateChild(Transform parent, string name) // 빈 하위 Transform 생성
        {
            GameObject child = new GameObject(name); // 하위 객체 생성
            child.transform.SetParent(parent, false); // 부모 객체 연결
            return child.transform; // 하위 Transform 반환
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite, Color color) // 공통 이미지 생성
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image)); // 이미지 객체 생성
            imageObject.transform.SetParent(parent, false); // 부모 객체 연결
            Image image = imageObject.GetComponent<Image>(); // Image 컴포넌트 조회
            image.sprite = sprite; // 이미지 Sprite 설정
            image.color = color; // 이미지 색상 설정
            image.raycastTarget = false; // 기본 이미지 입력 비활성화
            return image; // Image 반환
        }

        private static Text CreateText(Transform parent, string name, string value, int size, FontStyle style, Color color, TextAnchor alignment) // 공통 텍스트 생성
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow)); // 텍스트 객체 생성
            textObject.transform.SetParent(parent, false); // 부모 객체 연결
            Text text = textObject.GetComponent<Text>(); // Text 컴포넌트 조회
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 설정
            text.text = value; // 텍스트 내용 설정
            text.fontSize = size; // 텍스트 크기 설정
            text.fontStyle = style; // 텍스트 스타일 설정
            text.color = color; // 텍스트 색상 설정
            text.alignment = alignment; // 텍스트 정렬 설정
            text.alignByGeometry = true; // 글리프 기준 중앙 정렬 사용
            text.resizeTextForBestFit = true; // 자동 텍스트 크기 사용
            text.resizeTextMinSize = 10; // 최소 텍스트 크기 설정
            text.resizeTextMaxSize = size; // 최대 텍스트 크기 설정
            text.raycastTarget = false; // 텍스트 입력 비활성화
            return text; // Text 반환
        }

        private static Button CreateButton(Transform parent, string name, string label, string spriteFile, int fontSize) // 공통 버튼 생성
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); // 버튼 객체 생성
            buttonObject.transform.SetParent(parent, false); // 부모 객체 연결
            Image image = buttonObject.GetComponent<Image>(); // 버튼 이미지 조회
            image.sprite = LoadSprite("Buttons/" + spriteFile); // 버튼 Sprite 설정
            image.color = Color.white; // 버튼 이미지 기본색 설정
            Button button = buttonObject.GetComponent<Button>(); // Button 컴포넌트 조회
            button.targetGraphic = image; // 버튼 대상 그래픽 설정
            Text text = CreateText(buttonObject.transform, "Label", label, fontSize, FontStyle.Bold, Navy, TextAnchor.MiddleCenter); // 버튼 라벨 생성
            Stretch(text.rectTransform, 5f); // 버튼 라벨 영역 확장
            return button; // Button 반환
        }

        private static Sprite LoadSprite(string relativePath) // 프로토타입 UI Sprite 로드
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/{relativePath}"); // UI Sprite 반환
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max) // RectTransform 앵커 설정
        {
            rect.anchorMin = min; // 최소 앵커 설정
            rect.anchorMax = max; // 최대 앵커 설정
            rect.offsetMin = Vector2.zero; // 최소 오프셋 초기화
            rect.offsetMax = Vector2.zero; // 최대 오프셋 초기화
        }

        private static void Stretch(RectTransform rect, float padding) // RectTransform 전체 확장
        {
            rect.anchorMin = Vector2.zero; // 최소 앵커 전체 설정
            rect.anchorMax = Vector2.one; // 최대 앵커 전체 설정
            rect.offsetMin = new Vector2(padding, padding); // 최소 내부 여백 설정
            rect.offsetMax = new Vector2(-padding, -padding); // 최대 내부 여백 설정
        }
    }
}
