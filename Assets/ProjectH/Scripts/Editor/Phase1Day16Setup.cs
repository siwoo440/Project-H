using ProjectH.Battle; // 전투 HUD 기능
using UnityEditor; // Unity 에디터 기능
using UnityEditor.SceneManagement; // 에디터 Scene 관리
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // Unity Scene 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.EditorTools // 프로젝트 에디터 도구 영역
{
    public static class Phase1Day16Setup // 16일차 Battle HUD 설정 도구
    {
        private const string BattleScenePath = "Assets/ProjectH/Scenes/Battle.unity"; // 전투 Scene 경로
        private static readonly Color Navy = new Color(0.12f, 0.20f, 0.34f, 1f); // 공통 HUD 남색
        private static readonly Color Cream = new Color(0.98f, 0.96f, 0.90f, 0.98f); // 공통 HUD 크림색
        private static readonly Color DarkPanel = new Color(0.05f, 0.08f, 0.13f, 0.92f); // Debug 패널 배경색
        private static readonly Color GaugeBlue = new Color(0.26f, 0.68f, 0.92f, 1f); // 궁극기 게이지 임시색

        [MenuItem("Tools/Project H/Phase 1/16일차 Battle HUD 설정 실행")] // 16일차 HUD 설정 메뉴 등록
        public static void Setup() // 기존 Battle Scene HUD 업그레이드
        {
            Scene scene = EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Single); // 기존 Battle Scene 열기
            Transform controllerTransform = FindRequiredTransform(scene, "BattleController"); // BattleController 조회
            BattleScreenController screenController = FindRequiredComponent<BattleScreenController>(scene, "BattleController"); // 전투 화면 컨트롤러 조회
            Text battleStatus = FindRequiredComponent<Text>(scene, "BattleStatus"); // 기존 개발 상태 텍스트 조회
            Canvas hudCanvas = battleStatus.GetComponentInParent<Canvas>(); // 기존 HUD Canvas 조회
            RectTransform timePanel = FindRequiredTransform(scene, "TimePanel").GetComponent<RectTransform>(); // 기존 전투 시간 패널 조회

            if (timePanel == null) // TimePanel RectTransform 존재 확인
            {
                throw new System.InvalidOperationException("TimePanel에 RectTransform이 없습니다."); // TimePanel 구조 오류 발생
            }

            BattleTopHudLayout.ApplyTimePanel(timePanel); // TimePanel을 상단 동일 높이의 화면 중앙으로 이동

            if (hudCanvas == null) // HUD Canvas 존재 확인
            {
                throw new System.InvalidOperationException("BattleStatus의 HUD Canvas를 찾을 수 없습니다."); // HUD Canvas 누락 예외 발생
            }

            Button menuStyleSource = FindRequiredComponent<Button>(scene, "MenuButton"); // 기존 MENU 버튼 스타일 원본 조회
            Button speedButton = EnsureTopSpeedButton(hudCanvas.transform, menuStyleSource, out Text speedText); // 상단 전투 속도 버튼 확보
            BattleTimeController timeController = EnsureComponent<BattleTimeController>(controllerTransform.gameObject); // 전투 시간 컨트롤러 확보
            timeController.Configure(speedButton, speedText); // 전투 시간 UI 참조 연결
            GameObject debugPanelRoot = EnsureDebugPanel(hudCanvas.transform, battleStatus); // 전투 Debug 패널 확보
            Transform menuWindow = FindRequiredTransform(scene, "MenuWindow"); // 전투 메뉴 창 조회
            Button debugToggle = EnsureButton(menuWindow, "DebugToggleButton", "DEBUG", new Vector2(0.37f, 0.44f), new Vector2(0.63f, 0.56f)); // Debug 표시 버튼 확보
            Text debugToggleText = debugToggle.GetComponentInChildren<Text>(true); // Debug 버튼 문구 조회
            MoveExistingDebugControls(scene, debugPanelRoot.transform); // 기존 Debug UI를 전용 패널로 이동
            BattleDebugPanel debugPanel = EnsureComponent<BattleDebugPanel>(controllerTransform.gameObject); // 전투 Debug 패널 컨트롤러 확보
            debugPanel.Configure(debugPanelRoot, debugToggle, debugToggleText); // 전투 Debug UI 참조 연결
            UpgradeHudCards(scene); // 하단 4인 HUD 카드 확장
            UpgradeWorldDebugVisibility(scene); // 전장 Runtime 개발 정보 기본 숨김
            UpgradeMenuLabels(scene); // 전투 메뉴 문구 정리
            screenController.ConfigureDay16(timeController, debugPanel); // 전투 화면 Day16 Runtime 참조 연결
            EditorUtility.SetDirty(timeController); // 전투 시간 컨트롤러 변경 표시
            EditorUtility.SetDirty(debugPanel); // 전투 Debug 컨트롤러 변경 표시
            EditorUtility.SetDirty(screenController); // 전투 화면 컨트롤러 변경 표시
            EditorSceneManager.MarkSceneDirty(scene); // Battle Scene 변경 표시
            EditorSceneManager.SaveScene(scene, BattleScenePath); // Battle Scene 저장
            AssetDatabase.SaveAssets(); // 에셋 및 Scene 변경 저장
            AssetDatabase.Refresh(); // 에셋 목록 갱신
            Debug.Log("[Project H][BATTLE] Phase 1 Day 16 battle HUD setup complete."); // 16일차 HUD 설정 완료 로그
        }

        private static Button EnsureTopSpeedButton(Transform hudRoot, Button menuStyleSource, out Text speedText) // 상단 전투 속도 버튼 생성 또는 조회
        {
            Button button = EnsureButton(hudRoot, "SpeedButton", "×1", new Vector2(0.805f, 0.92f), new Vector2(0.885f, 0.985f)); // 상단 전투 속도 버튼 확보
            BattleHudButtonStyle.Copy(menuStyleSource, button); // 기존 MENU 버튼 시각 스타일 복사
            speedText = BattleHudButtonLabelUtility.EnsureSingleLabel(button, "×1"); // 상단 전투 속도 단일 라벨 정리 및 조회
            Text menuText = menuStyleSource == null ? null : menuStyleSource.GetComponentInChildren<Text>(true); // 기존 MENU 버튼 텍스트 조회
            BattleHudButtonStyle.CopyLabel(menuText, speedText); // 기존 MENU 버튼 텍스트 스타일 복사

            if (speedText != null) // 전투 속도 텍스트 확인
            {
                speedText.text = "×1"; // 초기 전투 속도 문구 적용
                speedText.alignment = TextAnchor.MiddleCenter; // 전투 속도 글자 중앙 정렬
                speedText.raycastTarget = false; // 전투 속도 텍스트 입력 비활성화
            }

            return button; // 상단 전투 속도 버튼 반환
        }

        private static GameObject EnsureDebugPanel(Transform hudRoot, Text battleStatus) // 전투 Debug 전용 패널 생성 또는 조회
        {
            Transform existing = FindOptionalChild(hudRoot, "BattleDebugPanel"); // 기존 Debug 패널 조회
            GameObject panelObject; // Debug 패널 객체 변수 선언

            if (existing != null) // 기존 Debug 패널 존재 확인
            {
                panelObject = existing.gameObject; // 기존 Debug 패널 객체 사용
            }
            else // 신규 Debug 패널 생성 처리
            {
                panelObject = new GameObject("BattleDebugPanel", typeof(RectTransform), typeof(Image)); // Debug 패널 객체 생성
                panelObject.transform.SetParent(hudRoot, false); // HUD Canvas 연결
                Image image = panelObject.GetComponent<Image>(); // Debug 패널 Image 조회
                image.color = DarkPanel; // Debug 패널 배경색 적용
                image.raycastTarget = false; // Debug 패널 배경 입력 통과 설정
            }

            RectTransform panelRect = panelObject.GetComponent<RectTransform>(); // Debug 패널 RectTransform 조회
            SetRect(panelRect, new Vector2(0.20f, 0.68f), new Vector2(0.80f, 0.90f)); // Debug 패널 상단 중앙 배치
            Text title = EnsureText(panelObject.transform, "DebugTitle", "BATTLE DEBUG", 18, FontStyle.Bold, Color.white); // Debug 패널 제목 확보
            SetRect(title.rectTransform, new Vector2(0.03f, 0.76f), new Vector2(0.30f, 0.96f)); // Debug 패널 제목 배치
            battleStatus.transform.SetParent(panelObject.transform, false); // 기존 BattleStatus를 Debug 패널로 이동
            battleStatus.fontSize = 15; // Debug 상태 텍스트 글자 크기 조정
            battleStatus.alignment = TextAnchor.MiddleLeft; // Debug 상태 텍스트 왼쪽 정렬
            SetRect(battleStatus.rectTransform, new Vector2(0.31f, 0.74f), new Vector2(0.97f, 0.97f)); // Debug 상태 텍스트 배치
            panelObject.SetActive(false); // 기본 Debug 패널 숨김
            return panelObject; // Debug 패널 객체 반환
        }

        private static void MoveExistingDebugControls(Scene scene, Transform debugPanel) // 기존 Debug 컨트롤 전용 패널 이동
        {
            string[] controlNames = // 기존 Debug 버튼 이름 목록
            {
                "DebugAttackButton", // 공격 표시 Debug 버튼
                "DebugSkillButton", // 스킬 표시 Debug 버튼
                "DebugUltimateButton", // 궁극기 표시 Debug 버튼
                "DebugHealButton" // 회복 Debug 버튼
            }; // 기존 Debug 버튼 이름 목록 종료

            for (int index = 0; index < controlNames.Length; index++) // 기존 Debug 버튼 이름 순회
            {
                Transform control = FindOptionalTransform(scene, controlNames[index]); // 기존 Debug 버튼 조회

                if (control == null) // 기존 Debug 버튼 존재 확인
                {
                    continue; // 없는 Debug 버튼 제외
                }

                control.SetParent(debugPanel, false); // Debug 전용 패널로 버튼 이동
                float minX = 0.03f + (index * 0.24f); // Debug 버튼 가로 시작 위치 계산
                float maxX = minX + 0.22f; // Debug 버튼 가로 종료 위치 계산
                RectTransform rect = control.GetComponent<RectTransform>(); // Debug 버튼 RectTransform 조회

                if (rect != null) // Debug 버튼 RectTransform 확인
                {
                    SetRect(rect, new Vector2(minX, 0.12f), new Vector2(maxX, 0.55f)); // Debug 버튼 한 줄 배치
                }
            }
        }

        private static void UpgradeHudCards(Scene scene) // 하단 4인 HUD 카드 확장
        {
            const int cardCount = 4; // HUD 카드 최대 개수
            float cardWidth = 0.155f; // 확장 HUD 카드 너비
            float gap = 0.012f; // 확장 HUD 카드 간격
            float groupWidth = (cardWidth * cardCount) + (gap * (cardCount - 1)); // HUD 카드 그룹 너비 계산
            float startX = (1f - groupWidth) * 0.5f; // HUD 카드 그룹 중앙 시작점 계산

            for (int index = 0; index < cardCount; index++) // HUD 카드 순회
            {
                Transform cardTransform = FindRequiredTransform(scene, $"BattleHudCard_{index}"); // 현재 HUD 카드 조회
                BattleHudCardView cardView = cardTransform.GetComponent<BattleHudCardView>(); // HUD 카드 View 조회

                if (cardView == null) // HUD 카드 View 존재 확인
                {
                    throw new System.InvalidOperationException($"BattleHudCard_{index}에 BattleHudCardView가 없습니다."); // HUD 카드 View 누락 예외 발생
                }

                RectTransform cardRect = cardTransform.GetComponent<RectTransform>(); // HUD 카드 RectTransform 조회
                float minX = startX + (index * (cardWidth + gap)); // HUD 카드 X 시작점 계산
                SetRect(cardRect, new Vector2(minX, 0.012f), new Vector2(minX + cardWidth, 0.265f)); // 확장 HUD 카드 배치
                Text nameText = FindRequiredChild(cardTransform, "NameText").GetComponent<Text>(); // HUD 이름 텍스트 조회
                Text levelText = FindRequiredChild(cardTransform, "LevelText").GetComponent<Text>(); // HUD 레벨 텍스트 조회
                Text portraitText = FindRequiredChild(cardTransform, "PortraitText").GetComponent<Text>(); // HUD 임시 초상화 텍스트 조회
                Text hpText = FindRequiredChild(cardTransform, "HpText").GetComponent<Text>(); // HUD HP 텍스트 조회
                Image hpFill = FindRequiredChild(cardTransform, "HpFill").GetComponent<Image>(); // HUD HP 게이지 조회
                Transform gaugeBack = FindRequiredChild(cardTransform, "GaugeBack"); // HUD 궁극기 게이지 배경 조회
                Image gaugeFill = FindRequiredChild(gaugeBack, "GaugeFill").GetComponent<Image>(); // HUD 궁극기 게이지 Fill 조회
                Transform portrait = FindRequiredChild(cardTransform, "Portrait"); // HUD 임시 초상화 영역 조회
                SetRect(portrait.GetComponent<RectTransform>(), new Vector2(0.07f, 0.48f), new Vector2(0.93f, 0.96f)); // HUD 임시 초상화 영역 확대
                SetRect(levelText.rectTransform, new Vector2(0.06f, 0.39f), new Vector2(0.31f, 0.48f)); // HUD 레벨 배치
                SetRect(nameText.rectTransform, new Vector2(0.30f, 0.39f), new Vector2(0.94f, 0.48f)); // HUD 이름 배치
                Transform hpBack = FindRequiredChild(cardTransform, "HpBack"); // HUD HP 배경 조회
                SetRect(hpBack.GetComponent<RectTransform>(), new Vector2(0.07f, 0.25f), new Vector2(0.93f, 0.32f)); // HUD HP Bar 배치
                SetRect(hpText.rectTransform, new Vector2(0.07f, 0.235f), new Vector2(0.93f, 0.335f)); // HUD HP 수치 배치
                SetRect(gaugeBack.GetComponent<RectTransform>(), new Vector2(0.07f, 0.145f), new Vector2(0.93f, 0.215f)); // HUD 궁극기 Gauge 배치
                gaugeFill.color = GaugeBlue; // HUD 궁극기 Gauge 임시색 적용
                Text healthState = EnsureText(cardTransform, "HealthStateText", "HP OK", 13, FontStyle.Bold, Navy); // 체력 상태 문구 확보
                SetRect(healthState.rectTransform, new Vector2(0.07f, 0.32f), new Vector2(0.93f, 0.39f)); // 체력 상태 문구 배치
                Text ultimateText = EnsureText(gaugeBack, "UltimateText", "ULT 0%", 12, FontStyle.Bold, Color.white); // 궁극기 Gauge 문구 확보
                Stretch(ultimateText.rectTransform, 2f); // 궁극기 Gauge 문구 전체 확장
                Button skillButton = EnsureButton(cardTransform, "SkillButton", "SKILL\nLOCKED", new Vector2(0.20f, 0.025f), new Vector2(0.80f, 0.125f)); // 스킬 자리 버튼 확보
                Text skillText = skillButton.GetComponentInChildren<Text>(true); // 스킬 자리 버튼 문구 조회
                skillButton.interactable = false; // 17일차 전까지 스킬 입력 잠금
                cardView.ConfigureExtended(nameText, levelText, portraitText, hpText, hpFill, gaugeFill, healthState, skillButton, skillText, ultimateText); // 16일차 HUD 카드 참조 연결
                EditorUtility.SetDirty(cardView); // HUD 카드 View 변경 표시
            }
        }

        private static void UpgradeWorldDebugVisibility(Scene scene) // 전장 Runtime 개발 정보 기본 숨김 설정
        {
            Transform allyTemplate = FindOptionalTransform(scene, "BattleUnitTemplate"); // 아군 전투 템플릿 조회

            if (allyTemplate != null) // 아군 전투 템플릿 존재 확인
            {
                BattleUnitView allyView = allyTemplate.GetComponent<BattleUnitView>(); // 아군 전투 View 조회
                allyView?.SetDebugInfoVisible(false); // 아군 Runtime ID 기본 숨김
                EditorUtility.SetDirty(allyView); // 아군 전투 View 변경 표시
            }

            Transform enemyTemplate = FindOptionalTransform(scene, "BattleEnemyTemplate"); // 적군 전투 템플릿 조회

            if (enemyTemplate != null) // 적군 전투 템플릿 존재 확인
            {
                BattleEnemyView enemyView = enemyTemplate.GetComponent<BattleEnemyView>(); // 적군 전투 View 조회
                enemyView?.SetDebugInfoVisible(false); // 적군 Runtime ID 및 AI 유형 기본 숨김
                EditorUtility.SetDirty(enemyView); // 적군 전투 View 변경 표시
            }
        }

        private static void UpgradeMenuLabels(Scene scene) // 전투 메뉴 버튼 문구 정리
        {
            Transform closeTransform = FindOptionalTransform(scene, "CloseMenuButton"); // 메뉴 닫기 버튼 조회

            if (closeTransform != null) // 메뉴 닫기 버튼 존재 확인
            {
                Text closeText = closeTransform.GetComponentInChildren<Text>(true); // 메뉴 닫기 버튼 문구 조회

                if (closeText != null) // 메뉴 닫기 버튼 문구 확인
                {
                    closeText.text = "계속하기"; // 메뉴 닫기 버튼을 전투 계속 문구로 변경
                }
            }

            Transform returnTransform = FindOptionalTransform(scene, "ReturnDungeonButton"); // 던전 복귀 버튼 조회

            if (returnTransform != null) // 던전 복귀 버튼 존재 확인
            {
                Text returnText = returnTransform.GetComponentInChildren<Text>(true); // 던전 복귀 버튼 문구 조회

                if (returnText != null) // 던전 복귀 버튼 문구 확인
                {
                    returnText.text = "던전 선택으로"; // 던전 복귀 버튼 문구 정리
                }
            }
        }

        private static Button EnsureButton(Transform parent, string name, string label, Vector2 min, Vector2 max) // 공통 HUD 버튼 생성 또는 조회
        {
            Transform existing = FindOptionalChild(parent, name); // 기존 HUD 버튼 조회
            Button button; // HUD 버튼 변수 선언

            if (existing != null) // 기존 HUD 버튼 존재 확인
            {
                button = existing.GetComponent<Button>(); // 기존 HUD Button 컴포넌트 조회

                if (button == null) // 기존 HUD Button 컴포넌트 확인
                {
                    button = existing.gameObject.AddComponent<Button>(); // 기존 HUD 객체에 Button 추가
                }

                Image existingImage = existing.GetComponent<Image>(); // 기존 HUD 버튼 Image 조회

                if (existingImage == null) // 기존 HUD 버튼 Image 존재 확인
                {
                    existingImage = existing.gameObject.AddComponent<Image>(); // 기존 HUD 버튼 Image 추가
                    existingImage.color = Cream; // 신규 HUD 버튼 배경색 적용
                }

                button.targetGraphic = existingImage; // HUD 버튼 대상 그래픽 연결
            }
            else // 신규 HUD 버튼 생성 처리
            {
                GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); // HUD 버튼 객체 생성
                buttonObject.transform.SetParent(parent, false); // HUD 버튼 부모 연결
                Image image = buttonObject.GetComponent<Image>(); // HUD 버튼 Image 조회
                image.color = Cream; // HUD 버튼 배경색 적용
                button = buttonObject.GetComponent<Button>(); // HUD Button 컴포넌트 조회
                button.targetGraphic = image; // HUD Button 대상 그래픽 연결
            }

            RectTransform rect = button.GetComponent<RectTransform>(); // HUD 버튼 RectTransform 조회
            SetRect(rect, min, max); // HUD 버튼 배치
            Text text = button.GetComponentInChildren<Text>(true); // 기존 HUD 버튼 문구 조회

            if (text == null) // HUD 버튼 문구 존재 확인
            {
                text = EnsureText(button.transform, "Label", label, 16, FontStyle.Bold, Navy); // HUD 버튼 문구 생성
                Stretch(text.rectTransform, 4f); // HUD 버튼 문구 전체 확장
            }

            text.text = label; // HUD 버튼 문구 적용
            text.alignment = TextAnchor.MiddleCenter; // HUD 버튼 문구 중앙 정렬
            text.raycastTarget = false; // HUD 버튼 문구 입력 비활성화
            return button; // HUD 버튼 반환
        }

        private static Text EnsureText(Transform parent, string name, string value, int size, FontStyle style, Color color) // 공통 HUD Text 생성 또는 조회
        {
            Transform existing = FindOptionalChild(parent, name); // 기존 HUD Text 조회
            Text text; // HUD Text 변수 선언

            if (existing != null) // 기존 HUD Text 존재 확인
            {
                text = existing.GetComponent<Text>(); // 기존 HUD Text 컴포넌트 조회

                if (text == null) // 기존 HUD Text 컴포넌트 존재 확인
                {
                    text = existing.gameObject.AddComponent<Text>(); // 기존 HUD 객체에 Text 추가
                }
            }
            else // 신규 HUD Text 생성 처리
            {
                GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // HUD Text 객체 생성
                textObject.transform.SetParent(parent, false); // HUD Text 부모 연결
                text = textObject.GetComponent<Text>(); // 신규 HUD Text 컴포넌트 조회
            }

            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 적용
            text.text = value; // HUD Text 초기 문구 적용
            text.fontSize = size; // HUD Text 글자 크기 적용
            text.fontStyle = style; // HUD Text 글자 스타일 적용
            text.color = color; // HUD Text 글자색 적용
            text.alignment = TextAnchor.MiddleCenter; // HUD Text 중앙 정렬
            text.alignByGeometry = true; // HUD Text 글리프 정렬 적용
            text.resizeTextForBestFit = true; // HUD Text 자동 크기 적용
            text.resizeTextMinSize = 10; // HUD Text 최소 크기 적용
            text.resizeTextMaxSize = size; // HUD Text 최대 크기 적용
            text.raycastTarget = false; // HUD Text 입력 비활성화
            return text; // HUD Text 반환
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component // 컴포넌트 생성 또는 조회
        {
            T component = target.GetComponent<T>(); // 기존 컴포넌트 조회

            if (component == null) // 기존 컴포넌트 존재 확인
            {
                component = target.AddComponent<T>(); // 신규 컴포넌트 추가
            }

            return component; // 컴포넌트 반환
        }

        private static T FindRequiredComponent<T>(Scene scene, string objectName) where T : Component // Scene 필수 컴포넌트 조회
        {
            Transform target = FindRequiredTransform(scene, objectName); // Scene 필수 객체 조회
            T component = target.GetComponent<T>(); // Scene 필수 컴포넌트 조회

            if (component == null) // Scene 필수 컴포넌트 존재 확인
            {
                throw new System.InvalidOperationException($"{objectName}에 {typeof(T).Name} 컴포넌트가 없습니다."); // Scene 필수 컴포넌트 누락 예외 발생
            }

            return component; // Scene 필수 컴포넌트 반환
        }

        private static Transform FindRequiredTransform(Scene scene, string objectName) // Scene 필수 Transform 조회
        {
            Transform found = FindOptionalTransform(scene, objectName); // Scene 객체 검색

            if (found == null) // Scene 객체 존재 확인
            {
                throw new System.InvalidOperationException($"Battle Scene object not found: {objectName}."); // Scene 필수 객체 누락 예외 발생
            }

            return found; // Scene 필수 Transform 반환
        }

        private static Transform FindOptionalTransform(Scene scene, string objectName) // Scene Transform 검색
        {
            foreach (GameObject root in scene.GetRootGameObjects()) // Scene 루트 객체 순회
            {
                Transform found = FindOptionalChild(root.transform, objectName); // 루트 하위 객체 검색

                if (found != null) // 검색 결과 확인
                {
                    return found; // 검색 Transform 반환
                }
            }

            return null; // Scene 검색 실패 반환
        }

        private static Transform FindRequiredChild(Transform root, string objectName) // 하위 필수 Transform 조회
        {
            Transform found = FindOptionalChild(root, objectName); // 하위 객체 검색

            if (found == null) // 하위 객체 존재 확인
            {
                throw new System.InvalidOperationException($"Child object not found: {objectName}."); // 하위 필수 객체 누락 예외 발생
            }

            return found; // 하위 필수 Transform 반환
        }

        private static Transform FindOptionalChild(Transform root, string objectName) // 하위 Transform 재귀 검색
        {
            if (root.name == objectName) // 현재 객체 이름 확인
            {
                return root; // 현재 Transform 반환
            }

            for (int index = 0; index < root.childCount; index++) // 자식 객체 순회
            {
                Transform found = FindOptionalChild(root.GetChild(index), objectName); // 자식 객체 재귀 검색

                if (found != null) // 자식 검색 결과 확인
                {
                    return found; // 검색 Transform 반환
                }
            }

            return null; // 하위 객체 검색 실패 반환
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max) // RectTransform 앵커 배치
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
