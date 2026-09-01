using System.Collections.Generic; // 목록 자료형
using ProjectH.Core; // 프로젝트 핵심 기능
using ProjectH.Data; // 프로젝트 데이터 기능
using ProjectH.SaveSystem; // 프로젝트 저장 기능
using ProjectH.UI; // 프로젝트 UI 기능
using UnityEditor; // Unity 에디터 기능
using UnityEditor.Events; // Unity 이벤트 편집 기능
using UnityEditor.SceneManagement; // 에디터 씬 관리
using UnityEngine; // Unity 기본 기능
using UnityEngine.EventSystems; // 이벤트 시스템 기능
using UnityEngine.InputSystem.UI; // 입력 시스템 UI 기능
using UnityEngine.SceneManagement; // 씬 관리 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.EditorTools // 프로젝트 에디터 도구 영역
{
    public static class Phase0Day4Setup // 4일차 자동 설정 도구
    {
        private const string SceneRoot = "Assets/ProjectH/Scenes"; // 씬 루트 경로
        private const string UiRoot = "Assets/ProjectH/UI"; // UI 루트 경로
        private const string ArtRoot = "Assets/ProjectH/UI/Art/Prototype"; // UI 아트 경로
        private const string BootstrapScenePath = "Assets/ProjectH/Scenes/Bootstrap.unity"; // 부트스트랩 씬 경로
        private const string BootstrapRootName = "[ProjectH] Bootstrap"; // 부트스트랩 객체 이름

        private static readonly Color Cream = new Color(0.98f, 0.96f, 0.91f, 0.95f); // 크림색
        private static readonly Color Navy = new Color(0.18f, 0.27f, 0.40f, 1f); // 남색
        private static readonly Color Sky = new Color(0.35f, 0.68f, 0.85f, 1f); // 하늘색
        private static readonly Color Pink = new Color(0.91f, 0.55f, 0.72f, 1f); // 분홍색
        private static readonly Color Gold = new Color(0.87f, 0.69f, 0.35f, 1f); // 금색
        private static readonly Color SoftText = new Color(0.27f, 0.31f, 0.39f, 1f); // 본문색

        [MenuItem("Tools/Project H/Phase 0/4일차 설정 실행")] // 설정 메뉴 등록
        public static void Setup() // 4일차 설정 실행
        {
            EnsureFolders(); // UI 폴더 구성
            ConfigureTextureImports(); // UI 이미지 임포트 설정
            CreateTitleScene(); // 타이틀 씬 생성
            CreateLobbyScene(); // 로비 씬 생성
            CreatePartyScene(); // 파티 씬 생성
            CreateDungeonScene(); // 던전 씬 생성
            CreateBattleScene(); // 전투 씬 생성
            CreateResultScene(); // 결과 씬 생성
            ConfigureBootstrap(); // 부트스트랩 진입 연결
            ConfigureBuildSettings(); // 빌드 씬 순서 설정
            AssetDatabase.SaveAssets(); // 에셋 변경 저장
            AssetDatabase.Refresh(); // 에셋 목록 갱신
            Debug.Log("[Project H] Phase 0 Day 4 UI setup complete."); // 설정 완료 로그
        }

        private static void EnsureFolders() // UI 폴더 구성
        {
            EnsureFolder("Assets/ProjectH", "UI"); // UI 루트 보장
            EnsureFolder(UiRoot, "Art"); // UI 아트 보장
            EnsureFolder(UiRoot + "/Art", "Prototype"); // 프로토타입 아트 보장
        }

        private static void EnsureFolder(string parentPath, string folderName) // 단일 폴더 보장
        {
            string path = parentPath + "/" + folderName; // 전체 폴더 경로 생성

            if (AssetDatabase.IsValidFolder(path)) // 기존 폴더 확인
            {
                return; // 중복 생성 중단
            }

            AssetDatabase.CreateFolder(parentPath, folderName); // 폴더 생성
        }

        private static void ConfigureTextureImports() // UI 텍스처 임포트 설정
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot }); // UI 텍스처 GUID 조회

            foreach (string guid in guids) // 텍스처 목록 순회
            {
                string path = AssetDatabase.GUIDToAssetPath(guid); // 텍스처 경로 조회
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter; // 텍스처 임포터 조회

                if (importer == null) // 임포터 존재 확인
                {
                    continue; // 다음 텍스처 이동
                }

                if (importer.textureType == TextureImporterType.Sprite && importer.alphaIsTransparency) // 기존 설정 확인
                {
                    continue; // 재임포트 생략
                }

                importer.textureType = TextureImporterType.Sprite; // 스프라이트 타입 설정
                importer.spriteImportMode = SpriteImportMode.Single; // 단일 스프라이트 설정
                importer.alphaIsTransparency = true; // 투명도 설정
                importer.mipmapEnabled = false; // 밉맵 비활성화
                importer.SaveAndReimport(); // 임포트 설정 적용
            }
        }

        private static void CreateTitleScene() // 타이틀 씬 생성
        {
            Scene scene = CreateScene(GameScenes.Title, "bg_title.png"); // 타이틀 기본 씬 생성
            Canvas canvas = FindCanvas(scene); // 타이틀 캔버스 조회
            PrototypeScreenController controller = CreateController(canvas, PrototypeScreenKind.Title); // 타이틀 컨트롤러 생성
            Text title = CreateText(canvas.transform, "GameTitle", "PROJECT H", 88, FontStyle.Bold, Navy, TextAnchor.MiddleCenter); // 게임 타이틀 생성
            SetRect(title.rectTransform, new Vector2(0.08f, 0.50f), new Vector2(0.62f, 0.78f), Vector2.zero, Vector2.zero); // 타이틀 위치 설정
            Text subtitle = CreateText(canvas.transform, "Subtitle", "빛과 침식 사이에서 이어지는 소녀들의 여정", 28, FontStyle.Normal, SoftText, TextAnchor.MiddleCenter); // 부제목 생성
            SetRect(subtitle.rectTransform, new Vector2(0.10f, 0.43f), new Vector2(0.60f, 0.54f), Vector2.zero, Vector2.zero); // 부제목 위치 설정
            Image panel = CreateImage(canvas.transform, "TitleMenuPanel", LoadSprite("Frames/frame_panel.png"), Cream); // 메뉴 패널 생성
            SetRect(panel.rectTransform, new Vector2(0.66f, 0.19f), new Vector2(0.93f, 0.77f), Vector2.zero, Vector2.zero); // 메뉴 패널 위치 설정
            Text status = CreateText(panel.transform, "Status", string.Empty, 22, FontStyle.Normal, SoftText, TextAnchor.MiddleCenter); // 저장 상태 텍스트 생성
            SetRect(status.rectTransform, new Vector2(0.08f, 0.77f), new Vector2(0.92f, 0.91f), Vector2.zero, Vector2.zero); // 저장 상태 위치 설정
            Button newGame = CreateButton(panel.transform, "NewGameButton", "새 게임", "button_primary.png", 31); // 새 게임 버튼 생성
            SetRect(newGame.GetComponent<RectTransform>(), new Vector2(0.14f, 0.53f), new Vector2(0.86f, 0.70f), Vector2.zero, Vector2.zero); // 새 게임 위치 설정
            Button continueButton = CreateButton(panel.transform, "ContinueButton", "이어하기", "button_secondary.png", 31); // 이어하기 버튼 생성
            SetRect(continueButton.GetComponent<RectTransform>(), new Vector2(0.14f, 0.31f), new Vector2(0.86f, 0.48f), Vector2.zero, Vector2.zero); // 이어하기 위치 설정
            Button quitButton = CreateButton(panel.transform, "QuitButton", "게임 종료", "button_small.png", 25); // 종료 버튼 생성
            SetRect(quitButton.GetComponent<RectTransform>(), new Vector2(0.24f, 0.11f), new Vector2(0.76f, 0.24f), Vector2.zero, Vector2.zero); // 종료 위치 설정
            UnityEventTools.AddPersistentListener(newGame.onClick, controller.NewGame); // 새 게임 이벤트 연결
            UnityEventTools.AddPersistentListener(continueButton.onClick, controller.ContinueGame); // 이어하기 이벤트 연결
            UnityEventTools.AddPersistentListener(quitButton.onClick, controller.QuitGame); // 종료 이벤트 연결
            controller.Configure(PrototypeScreenKind.Title, status, title, subtitle, continueButton); // 타이틀 참조 설정
            SaveScene(scene, GameScenes.Title); // 타이틀 씬 저장
        }

        private static void CreateLobbyScene() // 로비 씬 생성
        {
            Scene scene = CreateScene(GameScenes.Lobby, "bg_lobby.png"); // 로비 기본 씬 생성
            Canvas canvas = FindCanvas(scene); // 로비 캔버스 조회
            PrototypeScreenController controller = CreateController(canvas, PrototypeScreenKind.Lobby); // 로비 컨트롤러 생성
            Text status = CreateTopBar(canvas.transform, "LOBBY"); // 로비 상단바 생성
            Image heroFrame = CreateImage(canvas.transform, "HeroIllustrationFrame", LoadSprite("Frames/frame_portrait.png"), Color.white); // 캐릭터 일러스트 프레임 생성
            SetRect(heroFrame.rectTransform, new Vector2(0.08f, 0.18f), new Vector2(0.48f, 0.88f), Vector2.zero, Vector2.zero); // 캐릭터 프레임 위치 설정
            Text heroHint = CreateText(heroFrame.transform, "HeroHint", "MAIN CHARACTER\nILLUSTRATION", 28, FontStyle.Bold, new Color(0.38f, 0.48f, 0.60f, 0.65f), TextAnchor.MiddleCenter); // 캐릭터 자리 표시 생성
            Stretch(heroHint.rectTransform, 40f); // 캐릭터 자리 표시 확장
            Image talkPanel = CreateImage(canvas.transform, "TalkPanel", LoadSprite("Frames/frame_panel.png"), Color.white); // 대화 패널 생성
            SetRect(talkPanel.rectTransform, new Vector2(0.52f, 0.38f), new Vector2(0.91f, 0.74f), Vector2.zero, Vector2.zero); // 대화 패널 위치 설정
            Text body = CreateText(talkPanel.transform, "MissionBody", string.Empty, 29, FontStyle.Bold, Navy, TextAnchor.MiddleLeft); // 임무 텍스트 생성
            SetRect(body.rectTransform, new Vector2(0.08f, 0.30f), new Vector2(0.92f, 0.82f), Vector2.zero, Vector2.zero); // 임무 텍스트 위치 설정
            Text auxiliary = CreateText(talkPanel.transform, "SaveState", string.Empty, 19, FontStyle.Normal, SoftText, TextAnchor.MiddleLeft); // 저장 상태 생성
            SetRect(auxiliary.rectTransform, new Vector2(0.08f, 0.10f), new Vector2(0.92f, 0.26f), Vector2.zero, Vector2.zero); // 저장 상태 위치 설정
            Button saveButton = CreateButton(canvas.transform, "SaveButton", "저장", "button_small.png", 24); // 저장 버튼 생성
            SetRect(saveButton.GetComponent<RectTransform>(), new Vector2(0.77f, 0.79f), new Vector2(0.90f, 0.87f), Vector2.zero, Vector2.zero); // 저장 버튼 위치 설정
            UnityEventTools.AddPersistentListener(saveButton.onClick, controller.SaveGame); // 저장 이벤트 연결
            CreateBottomNavigation(canvas.transform, controller, 0); // 로비 하단 메뉴 생성
            controller.Configure(PrototypeScreenKind.Lobby, status, body, auxiliary, null); // 로비 참조 설정
            SaveScene(scene, GameScenes.Lobby); // 로비 씬 저장
        }

        private static void CreatePartyScene() // 파티 씬 생성
        {
            Scene scene = CreateScene(GameScenes.Party, "bg_party.png"); // 파티 기본 씬 생성
            Canvas canvas = FindCanvas(scene); // 파티 캔버스 조회
            PrototypeScreenController controller = CreateController(canvas, PrototypeScreenKind.Party); // 파티 컨트롤러 생성
            Text status = CreateTopBar(canvas.transform, "PARTY"); // 파티 상단바 생성
            Image roster = CreateImage(canvas.transform, "RosterPanel", LoadSprite("Frames/frame_panel.png"), Color.white); // 보유 캐릭터 패널 생성
            SetRect(roster.rectTransform, new Vector2(0.05f, 0.21f), new Vector2(0.40f, 0.83f), Vector2.zero, Vector2.zero); // 캐릭터 패널 위치 설정
            Text rosterTitle = CreateText(roster.transform, "RosterTitle", "CHARACTERS", 25, FontStyle.Bold, Navy, TextAnchor.MiddleLeft); // 캐릭터 제목 생성
            SetRect(rosterTitle.rectTransform, new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.96f), Vector2.zero, Vector2.zero); // 캐릭터 제목 위치 설정
            CreateCharacterMiniCards(roster.transform); // 캐릭터 미니 카드 생성
            Image partyPanel = CreateImage(canvas.transform, "PartyPanel", LoadSprite("Frames/frame_panel.png"), Color.white); // 편성 패널 생성
            SetRect(partyPanel.rectTransform, new Vector2(0.43f, 0.21f), new Vector2(0.95f, 0.83f), Vector2.zero, Vector2.zero); // 편성 패널 위치 설정
            Text body = CreateText(partyPanel.transform, "PartyBody", string.Empty, 29, FontStyle.Bold, Navy, TextAnchor.MiddleLeft); // 파티 내용 생성
            SetRect(body.rectTransform, new Vector2(0.08f, 0.19f), new Vector2(0.92f, 0.83f), Vector2.zero, Vector2.zero); // 파티 내용 위치 설정
            Text hint = CreateText(partyPanel.transform, "PartyHint", "4인 파티 편성 UI · 실제 교체 기능은 Phase 1 연결", 19, FontStyle.Normal, SoftText, TextAnchor.MiddleCenter); // 파티 안내 생성
            SetRect(hint.rectTransform, new Vector2(0.07f, 0.05f), new Vector2(0.93f, 0.16f), Vector2.zero, Vector2.zero); // 파티 안내 위치 설정
            Button dungeonButton = CreateButton(partyPanel.transform, "DungeonButton", "던전 선택", "button_primary.png", 25); // 던전 버튼 생성
            SetRect(dungeonButton.GetComponent<RectTransform>(), new Vector2(0.60f, 0.84f), new Vector2(0.93f, 0.96f), Vector2.zero, Vector2.zero); // 던전 버튼 위치 설정
            UnityEventTools.AddPersistentListener(dungeonButton.onClick, controller.GoDungeonSelect); // 던전 이벤트 연결
            CreateBottomNavigation(canvas.transform, controller, 1); // 파티 하단 메뉴 생성
            controller.Configure(PrototypeScreenKind.Party, status, body, hint, null); // 파티 참조 설정
            SaveScene(scene, GameScenes.Party); // 파티 씬 저장
        }

        private static void CreateDungeonScene() // 던전 선택 씬 생성
        {
            Scene scene = CreateScene(GameScenes.DungeonSelect, "bg_dungeon.png"); // 던전 기본 씬 생성
            Canvas canvas = FindCanvas(scene); // 던전 캔버스 조회
            PrototypeScreenController controller = CreateController(canvas, PrototypeScreenKind.DungeonSelect); // 던전 컨트롤러 생성
            Text status = CreateTopBar(canvas.transform, "ADVENTURE"); // 던전 상단바 생성
            Image chapterCard = CreateImage(canvas.transform, "ChapterCard", LoadSprite("Frames/frame_dungeon_card.png"), Color.white); // 챕터 카드 생성
            SetRect(chapterCard.rectTransform, new Vector2(0.07f, 0.22f), new Vector2(0.43f, 0.78f), Vector2.zero, Vector2.zero); // 챕터 카드 위치 설정
            Text chapterTitle = CreateText(chapterCard.transform, "ChapterTitle", "MAIN ADVENTURE\nLETICIA", 34, FontStyle.Bold, Navy, TextAnchor.MiddleCenter); // 챕터 제목 생성
            SetRect(chapterTitle.rectTransform, new Vector2(0.08f, 0.48f), new Vector2(0.92f, 0.80f), Vector2.zero, Vector2.zero); // 챕터 제목 위치 설정
            Text body = CreateText(chapterCard.transform, "DungeonInfo", string.Empty, 24, FontStyle.Bold, SoftText, TextAnchor.MiddleCenter); // 던전 정보 생성
            SetRect(body.rectTransform, new Vector2(0.08f, 0.13f), new Vector2(0.92f, 0.48f), Vector2.zero, Vector2.zero); // 던전 정보 위치 설정
            Image nodes = CreateImage(canvas.transform, "DungeonNodes", LoadSprite("Frames/frame_panel.png"), Color.white); // 던전 노드 패널 생성
            SetRect(nodes.rectTransform, new Vector2(0.47f, 0.21f), new Vector2(0.94f, 0.80f), Vector2.zero, Vector2.zero); // 던전 노드 위치 설정
            CreateDungeonNodes(nodes.transform, controller); // 던전 노드 생성
            Text auxiliary = CreateText(canvas.transform, "RegionInfo", string.Empty, 21, FontStyle.Bold, Cream, TextAnchor.MiddleRight); // 지역 정보 생성
            SetRect(auxiliary.rectTransform, new Vector2(0.59f, 0.82f), new Vector2(0.91f, 0.88f), Vector2.zero, Vector2.zero); // 지역 정보 위치 설정
            CreateBottomNavigation(canvas.transform, controller, 2); // 던전 하단 메뉴 생성
            controller.Configure(PrototypeScreenKind.DungeonSelect, status, body, auxiliary, null); // 던전 참조 설정
            SaveScene(scene, GameScenes.DungeonSelect); // 던전 씬 저장
        }

        private static void CreateBattleScene() // 전투 씬 생성
        {
            Scene scene = CreateScene(GameScenes.Battle, "bg_battle.png"); // 전투 기본 씬 생성
            Canvas canvas = FindCanvas(scene); // 전투 캔버스 조회
            PrototypeScreenController controller = CreateController(canvas, PrototypeScreenKind.Battle); // 전투 컨트롤러 생성
            Text status = CreateText(canvas.transform, "WaveText", string.Empty, 29, FontStyle.Bold, Navy, TextAnchor.MiddleLeft); // 웨이브 텍스트 생성
            SetRect(status.rectTransform, new Vector2(0.04f, 0.90f), new Vector2(0.25f, 0.98f), Vector2.zero, Vector2.zero); // 웨이브 위치 설정
            Image timerPanel = CreateImage(canvas.transform, "TimerPanel", LoadSprite("Buttons/button_small.png"), Color.white); // 타이머 패널 생성
            SetRect(timerPanel.rectTransform, new Vector2(0.43f, 0.90f), new Vector2(0.57f, 0.98f), Vector2.zero, Vector2.zero); // 타이머 위치 설정
            Text timer = CreateText(timerPanel.transform, "Timer", "1:30", 27, FontStyle.Bold, Navy, TextAnchor.MiddleCenter); // 타이머 텍스트 생성
            Stretch(timer.rectTransform, 4f); // 타이머 텍스트 확장
            Text body = CreateText(canvas.transform, "BattleInfo", string.Empty, 22, FontStyle.Bold, Navy, TextAnchor.MiddleLeft); // 전투 정보 생성
            SetRect(body.rectTransform, new Vector2(0.04f, 0.74f), new Vector2(0.35f, 0.89f), Vector2.zero, Vector2.zero); // 전투 정보 위치 설정
            CreateBattleUnits(canvas.transform); // 전투 유닛 자리 생성
            CreateBattlePortraitBar(canvas.transform); // 전투 하단 카드 생성
            Button autoButton = CreateButton(canvas.transform, "AutoButton", "AUTO", "button_small.png", 23); // 자동 버튼 생성
            SetRect(autoButton.GetComponent<RectTransform>(), new Vector2(0.86f, 0.18f), new Vector2(0.96f, 0.27f), Vector2.zero, Vector2.zero); // 자동 버튼 위치 설정
            Button victoryButton = CreateButton(canvas.transform, "PrototypeVictoryButton", "임시 승리", "button_secondary.png", 21); // 임시 승리 버튼 생성
            SetRect(victoryButton.GetComponent<RectTransform>(), new Vector2(0.83f, 0.90f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero); // 승리 버튼 위치 설정
            UnityEventTools.AddPersistentListener(victoryButton.onClick, controller.GoResult); // 승리 이벤트 연결
            controller.Configure(PrototypeScreenKind.Battle, status, body, null, null); // 전투 참조 설정
            SaveScene(scene, GameScenes.Battle); // 전투 씬 저장
        }

        private static void CreateResultScene() // 결과 씬 생성
        {
            Scene scene = CreateScene(GameScenes.Result, "bg_result.png"); // 결과 기본 씬 생성
            Canvas canvas = FindCanvas(scene); // 결과 캔버스 조회
            PrototypeScreenController controller = CreateController(canvas, PrototypeScreenKind.Result); // 결과 컨트롤러 생성
            Text victory = CreateText(canvas.transform, "VictoryTitle", "VICTORY", 82, FontStyle.Bold, new Color(0.95f, 0.77f, 0.28f, 1f), TextAnchor.MiddleCenter); // 승리 제목 생성
            SetRect(victory.rectTransform, new Vector2(0.18f, 0.74f), new Vector2(0.82f, 0.92f), Vector2.zero, Vector2.zero); // 승리 제목 위치 설정
            Image resultPanel = CreateImage(canvas.transform, "ResultPanel", LoadSprite("Frames/frame_panel.png"), Color.white); // 결과 패널 생성
            SetRect(resultPanel.rectTransform, new Vector2(0.20f, 0.24f), new Vector2(0.80f, 0.74f), Vector2.zero, Vector2.zero); // 결과 패널 위치 설정
            Text status = CreateText(resultPanel.transform, "ResultStatus", string.Empty, 34, FontStyle.Bold, Navy, TextAnchor.MiddleCenter); // 결과 상태 생성
            SetRect(status.rectTransform, new Vector2(0.08f, 0.76f), new Vector2(0.92f, 0.94f), Vector2.zero, Vector2.zero); // 결과 상태 위치 설정
            Text body = CreateText(resultPanel.transform, "RewardBody", string.Empty, 29, FontStyle.Bold, SoftText, TextAnchor.MiddleCenter); // 결과 보상 생성
            SetRect(body.rectTransform, new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.73f), Vector2.zero, Vector2.zero); // 결과 보상 위치 설정
            Text auxiliary = CreateText(resultPanel.transform, "RewardHint", string.Empty, 19, FontStyle.Normal, SoftText, TextAnchor.MiddleCenter); // 결과 안내 생성
            SetRect(auxiliary.rectTransform, new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.34f), Vector2.zero, Vector2.zero); // 결과 안내 위치 설정
            Button lobbyButton = CreateButton(resultPanel.transform, "LobbyButton", "로비로 돌아가기", "button_primary.png", 26); // 로비 버튼 생성
            SetRect(lobbyButton.GetComponent<RectTransform>(), new Vector2(0.28f, 0.05f), new Vector2(0.72f, 0.20f), Vector2.zero, Vector2.zero); // 로비 버튼 위치 설정
            UnityEventTools.AddPersistentListener(lobbyButton.onClick, controller.GoLobby); // 로비 이벤트 연결
            controller.Configure(PrototypeScreenKind.Result, status, body, auxiliary, null); // 결과 참조 설정
            SaveScene(scene, GameScenes.Result); // 결과 씬 저장
        }

        private static Scene CreateScene(string sceneName, string backgroundFile) // 공통 씬 생성
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); // 빈 씬 생성
            GameObject cameraObject = new GameObject("UICamera"); // UI 카메라 객체 생성
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 컴포넌트 추가
            camera.clearFlags = CameraClearFlags.SolidColor; // 단색 클리어 설정
            camera.backgroundColor = new Color(0.15f, 0.23f, 0.33f, 1f); // 카메라 배경색 설정
            Canvas canvas = CreateCanvas(); // 캔버스 생성
            CreateEventSystem(); // 이벤트 시스템 생성
            Image background = CreateImage(canvas.transform, "Background", LoadSprite("Backgrounds/" + backgroundFile), Color.white); // 배경 이미지 생성
            SetRect(background.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); // 배경 전체 확장
            background.transform.SetAsFirstSibling(); // 배경 최하단 이동
            return scene; // 생성 씬 반환
        }

        private static Canvas CreateCanvas() // 공통 캔버스 생성
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 캔버스 객체 생성
            Canvas canvas = canvasObject.GetComponent<Canvas>(); // 캔버스 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 오버레이 렌더 설정
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // 스케일러 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 기준 해상도 스케일 설정
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 해상도 설정
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응 설정
            scaler.matchWidthOrHeight = 0.5f; // 가로세로 균형 설정
            return canvas; // 캔버스 반환
        }

        private static void CreateEventSystem() // 입력 이벤트 시스템 생성
        {
            GameObject eventObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 이벤트 시스템 생성
            InputSystemUIInputModule inputModule = eventObject.GetComponent<InputSystemUIInputModule>(); // 입력 모듈 조회
            inputModule.AssignDefaultActions(); // 기본 UI 입력 액션 설정
        }

        private static PrototypeScreenController CreateController(Canvas canvas, PrototypeScreenKind kind) // 화면 컨트롤러 생성
        {
            PrototypeScreenController controller = canvas.gameObject.AddComponent<PrototypeScreenController>(); // 컨트롤러 추가
            controller.Configure(kind, null, null, null, null); // 기본 화면 종류 설정
            return controller; // 컨트롤러 반환
        }

        private static Text CreateTopBar(Transform parent, string title) // 공통 상단바 생성
        {
            Image bar = CreateImage(parent, "TopBar", LoadSprite("Frames/frame_topbar.png"), Color.white); // 상단바 생성
            SetRect(bar.rectTransform, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.98f), Vector2.zero, Vector2.zero); // 상단바 위치 설정
            Text titleText = CreateText(bar.transform, "ScreenTitle", title, 28, FontStyle.Bold, Navy, TextAnchor.MiddleLeft); // 화면 제목 생성
            SetRect(titleText.rectTransform, new Vector2(0.04f, 0.16f), new Vector2(0.27f, 0.84f), Vector2.zero, Vector2.zero); // 화면 제목 위치 설정
            Text statusText = CreateText(bar.transform, "RuntimeStatus", string.Empty, 20, FontStyle.Bold, SoftText, TextAnchor.MiddleLeft); // 런타임 상태 생성
            SetRect(statusText.rectTransform, new Vector2(0.28f, 0.16f), new Vector2(0.53f, 0.84f), Vector2.zero, Vector2.zero); // 런타임 상태 위치 설정
            CreateResourceChip(bar.transform, "GoldChip", "GOLD  5,548", 0.55f, 0.70f); // 골드 자원칩 생성
            CreateResourceChip(bar.transform, "CrystalChip", "CRYSTAL  120", 0.72f, 0.87f); // 크리스탈 자원칩 생성
            return statusText; // 상태 텍스트 반환
        }

        private static void CreateResourceChip(Transform parent, string name, string value, float minX, float maxX) // 자원칩 생성
        {
            Image chip = CreateImage(parent, name, LoadSprite("Buttons/button_small.png"), new Color(1f, 1f, 1f, 0.95f)); // 자원칩 배경 생성
            SetRect(chip.rectTransform, new Vector2(minX, 0.18f), new Vector2(maxX, 0.82f), Vector2.zero, Vector2.zero); // 자원칩 위치 설정
            Text text = CreateText(chip.transform, "Value", value, 18, FontStyle.Bold, SoftText, TextAnchor.MiddleCenter); // 자원값 텍스트 생성
            Stretch(text.rectTransform, 6f); // 자원값 텍스트 확장
        }

        private static void CreateBottomNavigation(Transform parent, PrototypeScreenController controller, int selectedIndex) // 공통 하단 메뉴 생성
        {
            Image bar = CreateImage(parent, "BottomNavigation", LoadSprite("Frames/frame_bottombar.png"), Color.white); // 하단바 생성
            SetRect(bar.rectTransform, new Vector2(0.12f, 0.02f), new Vector2(0.88f, 0.16f), Vector2.zero, Vector2.zero); // 하단바 위치 설정
            string[] labels = { "로비", "파티", "모험", "캐릭터", "메뉴" }; // 하단 메뉴 라벨
            for (int index = 0; index < labels.Length; index++) // 하단 메뉴 순회
            {
                float minX = 0.02f + index * 0.195f; // 메뉴 시작 위치 계산
                float maxX = minX + 0.18f; // 메뉴 종료 위치 계산
                Button button = CreateButton(bar.transform, "Nav_" + labels[index], labels[index], "button_nav.png", 21); // 메뉴 버튼 생성
                SetRect(button.GetComponent<RectTransform>(), new Vector2(minX, 0.12f), new Vector2(maxX, 0.88f), Vector2.zero, Vector2.zero); // 메뉴 버튼 위치 설정
                ColorBlock colors = button.colors; // 버튼 색상 조회
                colors.normalColor = index == selectedIndex ? new Color(0.90f, 0.95f, 1f, 1f) : Color.white; // 선택 메뉴 색상 설정
                button.colors = colors; // 버튼 색상 적용
                if (index == 0) UnityEventTools.AddPersistentListener(button.onClick, controller.GoLobby); // 로비 메뉴 연결
                if (index == 1) UnityEventTools.AddPersistentListener(button.onClick, controller.GoParty); // 파티 메뉴 연결
                if (index == 2) UnityEventTools.AddPersistentListener(button.onClick, controller.GoDungeonSelect); // 모험 메뉴 연결
            }
        }

        private static void CreateCharacterMiniCards(Transform parent) // 캐릭터 미니 카드 생성
        {
            string[] names = { "세레나", "엘렌", "릴리아", "이브" }; // 캐릭터 이름 목록
            for (int index = 0; index < names.Length; index++) // 캐릭터 카드 순회
            {
                int row = index / 2; // 카드 행 계산
                int column = index % 2; // 카드 열 계산
                float minX = 0.08f + column * 0.45f; // 카드 시작 X 계산
                float maxX = minX + 0.38f; // 카드 종료 X 계산
                float maxY = 0.77f - row * 0.34f; // 카드 종료 Y 계산
                float minY = maxY - 0.28f; // 카드 시작 Y 계산
                Image card = CreateImage(parent, "Character_" + names[index], LoadSprite("Frames/frame_party_slot.png"), Color.white); // 캐릭터 카드 생성
                SetRect(card.rectTransform, new Vector2(minX, minY), new Vector2(maxX, maxY), Vector2.zero, Vector2.zero); // 캐릭터 카드 위치 설정
                Text text = CreateText(card.transform, "Name", names[index], 20, FontStyle.Bold, Navy, TextAnchor.LowerCenter); // 캐릭터 이름 생성
                SetRect(text.rectTransform, new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.28f), Vector2.zero, Vector2.zero); // 캐릭터 이름 위치 설정
            }
        }

        private static void CreateDungeonNodes(Transform parent, PrototypeScreenController controller) // 던전 노드 생성
        {
            for (int index = 0; index < 6; index++) // 던전 노드 순회
            {
                int row = index / 3; // 노드 행 계산
                int column = index % 3; // 노드 열 계산
                float minX = 0.07f + column * 0.31f; // 노드 시작 X 계산
                float maxX = minX + 0.25f; // 노드 종료 X 계산
                float maxY = 0.83f - row * 0.39f; // 노드 종료 Y 계산
                float minY = maxY - 0.29f; // 노드 시작 Y 계산
                string label = index == 0 ? "1-1\n성역의 숲" : $"1-{index + 1}\nLOCK"; // 노드 라벨 결정
                Button node = CreateButton(parent, "DungeonNode_" + index, label, index == 0 ? "button_primary.png" : "button_nav.png", 21); // 던전 노드 생성
                SetRect(node.GetComponent<RectTransform>(), new Vector2(minX, minY), new Vector2(maxX, maxY), Vector2.zero, Vector2.zero); // 던전 노드 위치 설정
                node.interactable = index == 0; // 첫 던전만 활성화
                if (index == 0) UnityEventTools.AddPersistentListener(node.onClick, controller.GoBattle); // 전투 시작 연결
            }
        }

        private static void CreateBattleUnits(Transform parent) // 전투 유닛 자리 생성
        {
            for (int index = 0; index < 4; index++) // 아군 유닛 순회
            {
                Image unit = CreateImage(parent, "PartyUnit_" + index, LoadSprite("Frames/frame_portrait.png"), new Color(1f, 1f, 1f, 0.32f)); // 아군 자리 생성
                float x = 0.16f + index * 0.085f; // 아군 X 위치 계산
                SetRect(unit.rectTransform, new Vector2(x, 0.38f + index * 0.02f), new Vector2(x + 0.075f, 0.65f + index * 0.02f), Vector2.zero, Vector2.zero); // 아군 위치 설정
            }

            for (int index = 0; index < 3; index++) // 적 유닛 순회
            {
                Image enemy = CreateImage(parent, "EnemyUnit_" + index, LoadSprite("Icons/icon_energy.png"), new Color(0.96f, 0.55f, 0.46f, 0.90f)); // 적 자리 생성
                float x = 0.64f + index * 0.10f; // 적 X 위치 계산
                SetRect(enemy.rectTransform, new Vector2(x, 0.43f + index * 0.04f), new Vector2(x + 0.08f, 0.64f + index * 0.04f), Vector2.zero, Vector2.zero); // 적 위치 설정
            }
        }

        private static void CreateBattlePortraitBar(Transform parent) // 전투 하단 캐릭터 카드 생성
        {
            for (int index = 0; index < 4; index++) // 전투 카드 순회
            {
                Image frame = CreateImage(parent, "BattlePortrait_" + index, LoadSprite("Frames/frame_battle_portrait.png"), Color.white); // 전투 초상 프레임 생성
                float minX = 0.22f + index * 0.15f; // 카드 시작 X 계산
                SetRect(frame.rectTransform, new Vector2(minX, 0.03f), new Vector2(minX + 0.13f, 0.25f), Vector2.zero, Vector2.zero); // 카드 위치 설정
                Image hp = CreateImage(frame.transform, "HP", null, new Color(0.34f, 0.79f, 0.45f, 1f)); // HP 바 생성
                SetRect(hp.rectTransform, new Vector2(0.12f, 0.08f), new Vector2(0.88f, 0.14f), Vector2.zero, Vector2.zero); // HP 바 위치 설정
                Image tp = CreateImage(frame.transform, "TP", null, new Color(0.22f, 0.72f, 0.93f, 1f)); // TP 바 생성
                SetRect(tp.rectTransform, new Vector2(0.12f, 0.02f), new Vector2(0.72f, 0.07f), Vector2.zero, Vector2.zero); // TP 바 위치 설정
            }
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite, Color color) // 공통 이미지 생성
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image)); // 이미지 객체 생성
            imageObject.transform.SetParent(parent, false); // 부모 연결
            Image image = imageObject.GetComponent<Image>(); // 이미지 컴포넌트 조회
            image.sprite = sprite; // 이미지 스프라이트 설정
            image.color = color; // 이미지 색상 설정
            image.raycastTarget = false; // 이미지 레이캐스트 비활성화
            return image; // 이미지 반환
        }

        private static Text CreateText(Transform parent, string name, string value, int size, FontStyle style, Color color, TextAnchor alignment) // 공통 텍스트 생성
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow)); // 텍스트 객체 생성
            textObject.transform.SetParent(parent, false); // 부모 연결
            Text text = textObject.GetComponent<Text>(); // 텍스트 컴포넌트 조회
            text.font = GetDefaultFont(); // 기본 폰트 설정
            text.text = value; // 텍스트 값 설정
            text.fontSize = size; // 텍스트 크기 설정
            text.fontStyle = style; // 텍스트 스타일 설정
            text.color = color; // 텍스트 색상 설정
            text.alignment = alignment; // 텍스트 정렬 설정
            text.resizeTextForBestFit = true; // 자동 크기 조절 설정
            text.resizeTextMinSize = 12; // 최소 글자 크기 설정
            text.resizeTextMaxSize = size; // 최대 글자 크기 설정
            text.raycastTarget = false; // 텍스트 레이캐스트 비활성화
            Shadow shadow = textObject.GetComponent<Shadow>(); // 텍스트 그림자 조회
            shadow.effectColor = new Color(1f, 1f, 1f, 0.45f); // 텍스트 그림자 색상 설정
            shadow.effectDistance = new Vector2(1f, -1f); // 텍스트 그림자 거리 설정
            return text; // 텍스트 반환
        }

        private static Button CreateButton(Transform parent, string name, string label, string spriteFile, int fontSize) // 공통 버튼 생성
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); // 버튼 객체 생성
            buttonObject.transform.SetParent(parent, false); // 부모 연결
            Image image = buttonObject.GetComponent<Image>(); // 버튼 이미지 조회
            image.sprite = LoadSprite("Buttons/" + spriteFile); // 버튼 스프라이트 설정
            image.color = Color.white; // 버튼 색상 설정
            Button button = buttonObject.GetComponent<Button>(); // 버튼 컴포넌트 조회
            button.targetGraphic = image; // 버튼 대상 그래픽 설정
            ColorBlock colors = button.colors; // 버튼 색상 블록 조회
            colors.highlightedColor = new Color(1.06f, 1.06f, 1.06f, 1f); // 버튼 강조색 설정
            colors.pressedColor = new Color(0.88f, 0.92f, 0.96f, 1f); // 버튼 눌림색 설정
            colors.disabledColor = new Color(0.64f, 0.66f, 0.70f, 0.65f); // 버튼 비활성색 설정
            button.colors = colors; // 버튼 색상 적용
            Text text = CreateText(buttonObject.transform, "Label", label, fontSize, FontStyle.Bold, Navy, TextAnchor.MiddleCenter); // 버튼 라벨 생성
            Stretch(text.rectTransform, 8f); // 버튼 라벨 확장
            return button; // 버튼 반환
        }

        private static Font GetDefaultFont() // 기본 폰트 조회
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 레거시 폰트 조회
            return font; // 기본 폰트 반환
        }

        private static Sprite LoadSprite(string relativePath) // UI 스프라이트 조회
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "/" + relativePath); // 스프라이트 반환
        }

        private static Canvas FindCanvas(Scene scene) // 씬 캔버스 조회
        {
            foreach (GameObject root in scene.GetRootGameObjects()) // 씬 루트 순회
            {
                Canvas canvas = root.GetComponent<Canvas>(); // 캔버스 컴포넌트 조회
                if (canvas != null) return canvas; // 캔버스 반환
            }

            return null; // 캔버스 없음
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax) // RectTransform 영역 설정
        {
            rect.anchorMin = anchorMin; // 최소 앵커 설정
            rect.anchorMax = anchorMax; // 최대 앵커 설정
            rect.offsetMin = offsetMin; // 최소 오프셋 설정
            rect.offsetMax = offsetMax; // 최대 오프셋 설정
        }

        private static void Stretch(RectTransform rect, float padding) // RectTransform 전체 확장
        {
            SetRect(rect, Vector2.zero, Vector2.one, new Vector2(padding, padding), new Vector2(-padding, -padding)); // 전체 영역 설정
        }

        private static void SaveScene(Scene scene, string sceneName) // 씬 저장
        {
            string path = SceneRoot + "/" + sceneName + ".unity"; // 씬 경로 생성
            EditorSceneManager.SaveScene(scene, path); // 씬 파일 저장
        }

        private static void ConfigureBootstrap() // 부트스트랩 라우터 연결
        {
            Scene bootstrapScene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single); // 부트스트랩 씬 열기
            GameObject root = FindBootstrapRoot(bootstrapScene); // 부트스트랩 루트 조회

            if (root == null) // 부트스트랩 루트 확인
            {
                Debug.LogError("[Project H] Bootstrap root is missing."); // 루트 누락 로그
                return; // 부트스트랩 설정 중단
            }

            if (root.GetComponent<GameManager>() == null) // 게임 관리자 확인
            {
                Debug.LogError("[Project H] GameManager is missing. Run previous setup first."); // 관리자 누락 로그
                return; // 부트스트랩 설정 중단
            }

            if (root.GetComponent<BootstrapStartup>() == null) // 시작 라우터 확인
            {
                root.AddComponent<BootstrapStartup>(); // 시작 라우터 추가
            }

            EditorSceneManager.MarkSceneDirty(bootstrapScene); // 씬 변경 표시
            EditorSceneManager.SaveScene(bootstrapScene, BootstrapScenePath); // 부트스트랩 씬 저장
        }

        private static GameObject FindBootstrapRoot(Scene scene) // 부트스트랩 루트 검색
        {
            foreach (GameObject root in scene.GetRootGameObjects()) // 루트 객체 순회
            {
                if (root.name == BootstrapRootName) return root; // 일치 루트 반환
            }

            return null; // 일치 루트 없음
        }

        private static void ConfigureBuildSettings() // 빌드 씬 순서 구성
        {
            string[] primaryPaths = // 핵심 씬 경로 목록
            {
                SceneRoot + "/" + GameScenes.Bootstrap + ".unity", // 부트스트랩 경로
                SceneRoot + "/" + GameScenes.Title + ".unity", // 타이틀 경로
                SceneRoot + "/" + GameScenes.Lobby + ".unity", // 로비 경로
                SceneRoot + "/" + GameScenes.Party + ".unity", // 파티 경로
                SceneRoot + "/" + GameScenes.DungeonSelect + ".unity", // 던전 경로
                SceneRoot + "/" + GameScenes.Battle + ".unity", // 전투 경로
                SceneRoot + "/" + GameScenes.Result + ".unity" // 결과 경로
            }; // 핵심 씬 경로 종료

            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(); // 새 빌드 목록 생성

            foreach (string path in primaryPaths) // 핵심 씬 순회
            {
                scenes.Add(new EditorBuildSettingsScene(path, true)); // 핵심 씬 활성 등록
            }

            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes) // 기존 씬 순회
            {
                bool duplicate = false; // 중복 여부 초기화
                foreach (string path in primaryPaths) // 핵심 경로 순회
                {
                    if (existing.path == path) duplicate = true; // 핵심 씬 중복 표시
                }

                if (!duplicate) scenes.Add(existing); // 기존 비중복 씬 보존
            }

            EditorBuildSettings.scenes = scenes.ToArray(); // 빌드 씬 목록 적용
        }
    }
}
