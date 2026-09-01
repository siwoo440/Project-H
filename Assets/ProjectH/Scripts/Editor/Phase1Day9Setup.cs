using ProjectH.Core; // 프로젝트 핵심 기능
using ProjectH.UI; // 프로젝트 UI 기능
using UnityEditor; // Unity 에디터 기능
using UnityEditor.Events; // Unity 이벤트 편집 기능
using UnityEditor.SceneManagement; // 에디터 씬 관리
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // Unity 씬 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.EditorTools // 프로젝트 에디터 도구 영역
{
    public static class Phase1Day9Setup // 9일차 Title Lobby 설정 도구
    {
        private const string TitleScenePath = "Assets/ProjectH/Scenes/Title.unity"; // 타이틀 씬 경로
        private const string LobbyScenePath = "Assets/ProjectH/Scenes/Lobby.unity"; // 로비 씬 경로
        private const string SmallButtonSpritePath = "Assets/ProjectH/UI/Art/Prototype/Buttons/button_small.png"; // 소형 버튼 스프라이트 경로

        [MenuItem("Tools/Project H/Phase 1/9일차 Title-Lobby 설정 실행")] // 9일차 설정 메뉴 등록
        public static void Setup() // 9일차 화면 설정 실행
        {
            ConfigureTitleScene(); // 타이틀 씬 전용 컨트롤러 설정
            ConfigureLobbyScene(); // 로비 씬 전용 컨트롤러 설정
            AssetDatabase.SaveAssets(); // 에셋 변경 저장
            AssetDatabase.Refresh(); // 에셋 목록 갱신
            Debug.Log("[Project H][UI] Phase 1 Day 9 Title/Lobby setup complete."); // 9일차 설정 완료 로그
        }

        private static void ConfigureTitleScene() // 타이틀 씬 설정
        {
            Scene scene = EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single); // 타이틀 씬 열기
            Canvas canvas = FindInScene<Canvas>(scene, "Canvas"); // 타이틀 캔버스 조회
            Text status = FindInScene<Text>(scene, "Status"); // 타이틀 상태 텍스트 조회
            Button newGame = FindInScene<Button>(scene, "NewGameButton"); // 새 게임 버튼 조회
            Button continueButton = FindInScene<Button>(scene, "ContinueButton"); // 이어하기 버튼 조회
            Button quitButton = FindInScene<Button>(scene, "QuitButton"); // 종료 버튼 조회

            if (canvas == null || status == null || newGame == null || continueButton == null || quitButton == null) // 필수 UI 확인
            {
                Debug.LogError("[Project H][UI] Title scene structure is incomplete. Run Phase 0 Day 4 setup first."); // 타이틀 구조 오류 로그
                return; // 타이틀 설정 중단
            }

            PrototypeScreenController prototype = canvas.GetComponent<PrototypeScreenController>(); // 기존 프로토타입 컨트롤러 조회

            if (prototype != null) // 기존 컨트롤러 확인
            {
                Object.DestroyImmediate(prototype, true); // 타이틀 프로토타입 컨트롤러 제거
            }

            TitleScreenController controller = canvas.GetComponent<TitleScreenController>(); // 타이틀 전용 컨트롤러 조회

            if (controller == null) // 타이틀 컨트롤러 존재 확인
            {
                controller = canvas.gameObject.AddComponent<TitleScreenController>(); // 타이틀 전용 컨트롤러 추가
            }

            ResetButton(newGame); // 새 게임 기존 이벤트 제거
            ResetButton(continueButton); // 이어하기 기존 이벤트 제거
            ResetButton(quitButton); // 종료 기존 이벤트 제거
            UnityEventTools.AddPersistentListener(newGame.onClick, controller.NewGame); // 새 게임 이벤트 연결
            UnityEventTools.AddPersistentListener(continueButton.onClick, controller.ContinueGame); // 이어하기 이벤트 연결
            UnityEventTools.AddPersistentListener(quitButton.onClick, controller.QuitGame); // 종료 이벤트 연결
            controller.Configure(status, newGame, continueButton, quitButton); // 타이틀 참조 설정
            EditorUtility.SetDirty(controller); // 타이틀 컨트롤러 변경 표시
            EditorSceneManager.MarkSceneDirty(scene); // 타이틀 씬 변경 표시
            EditorSceneManager.SaveScene(scene, TitleScenePath); // 타이틀 씬 저장
        }

        private static void ConfigureLobbyScene() // 로비 씬 설정
        {
            Scene scene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single); // 로비 씬 열기
            Canvas canvas = FindInScene<Canvas>(scene, "Canvas"); // 로비 캔버스 조회
            Text status = FindInScene<Text>(scene, "RuntimeStatus"); // 로비 진행 상태 조회
            Text body = FindInScene<Text>(scene, "MissionBody"); // 로비 본문 조회
            Text saveState = FindInScene<Text>(scene, "SaveState"); // 로비 저장 상태 조회
            Button saveButton = FindInScene<Button>(scene, "SaveButton"); // 저장 버튼 조회
            Button lobbyButton = FindInScene<Button>(scene, "Nav_로비"); // 로비 메뉴 버튼 조회
            Button partyButton = FindInScene<Button>(scene, "Nav_파티"); // 파티 메뉴 버튼 조회
            Button dungeonButton = FindInScene<Button>(scene, "Nav_모험"); // 모험 메뉴 버튼 조회

            if (canvas == null || status == null || body == null || saveState == null || saveButton == null || lobbyButton == null || partyButton == null || dungeonButton == null) // 필수 UI 확인
            {
                Debug.LogError("[Project H][UI] Lobby scene structure is incomplete. Run Phase 0 Day 4 setup first."); // 로비 구조 오류 로그
                return; // 로비 설정 중단
            }

            PrototypeScreenController prototype = canvas.GetComponent<PrototypeScreenController>(); // 기존 로비 프로토타입 컨트롤러 조회

            if (prototype != null) // 기존 컨트롤러 확인
            {
                Object.DestroyImmediate(prototype, true); // 로비 프로토타입 컨트롤러 제거
            }

            LobbyScreenController controller = canvas.GetComponent<LobbyScreenController>(); // 로비 전용 컨트롤러 조회

            if (controller == null) // 로비 컨트롤러 존재 확인
            {
                controller = canvas.gameObject.AddComponent<LobbyScreenController>(); // 로비 전용 컨트롤러 추가
            }

            Text partyText = EnsurePartySummary(body.transform.parent, body); // 파티 요약 텍스트 보장
            Button titleButton = EnsureTitleButton(canvas.transform, saveButton); // 타이틀 이동 버튼 보장
            RectTransform bodyRect = body.rectTransform; // 본문 RectTransform 조회
            bodyRect.anchorMin = new Vector2(0.08f, 0.57f); // 본문 최소 앵커 조정
            bodyRect.anchorMax = new Vector2(0.92f, 0.82f); // 본문 최대 앵커 조정
            bodyRect.offsetMin = Vector2.zero; // 본문 최소 오프셋 초기화
            bodyRect.offsetMax = Vector2.zero; // 본문 최대 오프셋 초기화
            ResetButton(saveButton); // 저장 버튼 기존 이벤트 제거
            ResetButton(lobbyButton); // 로비 메뉴 기존 이벤트 제거
            ResetButton(partyButton); // 파티 메뉴 기존 이벤트 제거
            ResetButton(dungeonButton); // 모험 메뉴 기존 이벤트 제거
            ResetButton(titleButton); // 타이틀 버튼 기존 이벤트 제거
            UnityEventTools.AddPersistentListener(saveButton.onClick, controller.SaveGame); // 저장 이벤트 연결
            UnityEventTools.AddPersistentListener(lobbyButton.onClick, controller.GoLobby); // 로비 메뉴 이벤트 연결
            UnityEventTools.AddPersistentListener(partyButton.onClick, controller.GoParty); // 파티 메뉴 이벤트 연결
            UnityEventTools.AddPersistentListener(dungeonButton.onClick, controller.GoDungeonSelect); // 모험 메뉴 이벤트 연결
            UnityEventTools.AddPersistentListener(titleButton.onClick, controller.GoTitle); // 타이틀 이동 이벤트 연결
            controller.Configure(status, body, saveState, partyText, saveButton, partyButton, dungeonButton, titleButton); // 로비 참조 설정
            EditorUtility.SetDirty(controller); // 로비 컨트롤러 변경 표시
            EditorSceneManager.MarkSceneDirty(scene); // 로비 씬 변경 표시
            EditorSceneManager.SaveScene(scene, LobbyScenePath); // 로비 씬 저장
        }

        private static Text EnsurePartySummary(Transform parent, Text sourceText) // 파티 요약 텍스트 보장
        {
            Transform existing = FindChildRecursive(parent, "PartySummary"); // 기존 파티 텍스트 검색

            if (existing != null) // 기존 파티 텍스트 확인
            {
                return existing.GetComponent<Text>(); // 기존 파티 텍스트 반환
            }

            GameObject textObject = new GameObject("PartySummary", typeof(RectTransform), typeof(Text), typeof(Shadow)); // 파티 요약 객체 생성
            textObject.transform.SetParent(parent, false); // 대화 패널 부모 연결
            Text text = textObject.GetComponent<Text>(); // 파티 요약 Text 조회
            text.font = sourceText.font; // 기존 폰트 복사
            text.fontSize = 22; // 파티 글자 크기 설정
            text.fontStyle = FontStyle.Bold; // 파티 글자 스타일 설정
            text.color = sourceText.color; // 기존 글자색 복사
            text.alignment = TextAnchor.UpperLeft; // 파티 텍스트 정렬 설정
            text.resizeTextForBestFit = true; // 자동 글자 크기 사용
            text.resizeTextMinSize = 13; // 최소 글자 크기 설정
            text.resizeTextMaxSize = 22; // 최대 글자 크기 설정
            text.raycastTarget = false; // 레이캐스트 비활성화
            Shadow shadow = textObject.GetComponent<Shadow>(); // 그림자 컴포넌트 조회
            shadow.effectColor = new Color(1f, 1f, 1f, 0.45f); // 그림자 색상 설정
            shadow.effectDistance = new Vector2(1f, -1f); // 그림자 거리 설정
            RectTransform rect = text.rectTransform; // 파티 RectTransform 조회
            rect.anchorMin = new Vector2(0.08f, 0.27f); // 파티 최소 앵커 설정
            rect.anchorMax = new Vector2(0.92f, 0.55f); // 파티 최대 앵커 설정
            rect.offsetMin = Vector2.zero; // 파티 최소 오프셋 초기화
            rect.offsetMax = Vector2.zero; // 파티 최대 오프셋 초기화
            return text; // 파티 요약 텍스트 반환
        }

        private static Button EnsureTitleButton(Transform parent, Button sourceButton) // 타이틀 버튼 보장
        {
            Transform existing = FindChildRecursive(parent, "TitleButton"); // 기존 타이틀 버튼 검색

            if (existing != null) // 기존 타이틀 버튼 확인
            {
                return existing.GetComponent<Button>(); // 기존 타이틀 버튼 반환
            }

            GameObject buttonObject = new GameObject("TitleButton", typeof(RectTransform), typeof(Image), typeof(Button)); // 타이틀 버튼 객체 생성
            buttonObject.transform.SetParent(parent, false); // 캔버스 부모 연결
            Image image = buttonObject.GetComponent<Image>(); // 버튼 이미지 조회
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SmallButtonSpritePath); // 소형 버튼 스프라이트 연결
            image.color = Color.white; // 버튼 기본색 설정
            Button button = buttonObject.GetComponent<Button>(); // 버튼 컴포넌트 조회
            button.targetGraphic = image; // 버튼 대상 이미지 설정
            button.colors = sourceButton.colors; // 기존 저장 버튼 색상 복사
            RectTransform rect = buttonObject.GetComponent<RectTransform>(); // 버튼 RectTransform 조회
            rect.anchorMin = new Vector2(0.63f, 0.79f); // 타이틀 버튼 최소 앵커 설정
            rect.anchorMax = new Vector2(0.75f, 0.87f); // 타이틀 버튼 최대 앵커 설정
            rect.offsetMin = Vector2.zero; // 버튼 최소 오프셋 초기화
            rect.offsetMax = Vector2.zero; // 버튼 최대 오프셋 초기화
            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(Shadow)); // 버튼 라벨 객체 생성
            labelObject.transform.SetParent(buttonObject.transform, false); // 버튼 라벨 부모 연결
            Text sourceLabel = sourceButton.GetComponentInChildren<Text>(); // 저장 버튼 라벨 조회
            Text label = labelObject.GetComponent<Text>(); // 타이틀 버튼 라벨 조회
            label.font = sourceLabel == null ? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") : sourceLabel.font; // 버튼 폰트 설정
            label.text = "타이틀"; // 버튼 라벨 설정
            label.fontSize = 22; // 버튼 글자 크기 설정
            label.fontStyle = FontStyle.Bold; // 버튼 글자 스타일 설정
            label.color = sourceLabel == null ? Color.black : sourceLabel.color; // 버튼 글자색 설정
            label.alignment = TextAnchor.MiddleCenter; // 버튼 라벨 정렬 설정
            label.raycastTarget = false; // 라벨 레이캐스트 비활성화
            RectTransform labelRect = label.rectTransform; // 라벨 RectTransform 조회
            labelRect.anchorMin = Vector2.zero; // 라벨 최소 앵커 설정
            labelRect.anchorMax = Vector2.one; // 라벨 최대 앵커 설정
            labelRect.offsetMin = new Vector2(8f, 8f); // 라벨 최소 여백 설정
            labelRect.offsetMax = new Vector2(-8f, -8f); // 라벨 최대 여백 설정
            return button; // 타이틀 버튼 반환
        }

        private static void ResetButton(Button button) // 버튼 이벤트 초기화
        {
            button.onClick = new Button.ButtonClickedEvent(); // 기존 버튼 이벤트 전체 제거
            EditorUtility.SetDirty(button); // 버튼 변경 표시
        }

        private static T FindInScene<T>(Scene scene, string objectName) where T : Component // 씬 이름 기반 컴포넌트 검색
        {
            foreach (GameObject root in scene.GetRootGameObjects()) // 씬 루트 순회
            {
                Transform found = FindChildRecursive(root.transform, objectName); // 대상 이름 검색

                if (found == null) // 검색 결과 확인
                {
                    continue; // 다음 루트 이동
                }

                T component = found.GetComponent<T>(); // 대상 컴포넌트 조회

                if (component != null) // 컴포넌트 존재 확인
                {
                    return component; // 검색 컴포넌트 반환
                }
            }

            return null; // 검색 실패 반환
        }

        private static Transform FindChildRecursive(Transform root, string objectName) // 하위 객체 재귀 검색
        {
            if (root.name == objectName) // 현재 객체 이름 확인
            {
                return root; // 현재 객체 반환
            }

            for (int index = 0; index < root.childCount; index++) // 자식 객체 순회
            {
                Transform found = FindChildRecursive(root.GetChild(index), objectName); // 자식 객체 재귀 검색

                if (found != null) // 자식 검색 결과 확인
                {
                    return found; // 검색 객체 반환
                }
            }

            return null; // 검색 실패 반환
        }
    }
}
