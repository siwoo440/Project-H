using ProjectH.Battle; // 전투 행동 기능
using ProjectH.Data; // 캐릭터 데이터 기능
using UnityEditor; // Unity 에디터 기능
using UnityEditor.SceneManagement; // 에디터 씬 관리
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // Unity 씬 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.EditorTools // 프로젝트 에디터 도구 영역
{
    public static class Phase1Day12Setup // 12일차 타겟팅 및 기본 공격 설정 도구
    {
        private const string BattleScenePath = "Assets/ProjectH/Scenes/Battle.unity"; // 전투 씬 경로
        private const string CharacterDataRoot = "Assets/ProjectH/Data/Characters"; // 캐릭터 데이터 경로
        private const string ArtRoot = "Assets/ProjectH/UI/Art/Prototype"; // 프로토타입 UI 아트 경로
        private static readonly string[] DefaultEnemyIds = // 12일차 테스트 적군 ID 목록
        {
            "MON_CORRUPTED_SOLDIER", // 칠식 병사 테스트 적군
            "MON_CORRUPTED_WOLF", // 칠식 늑대 테스트 적군
            "MON_POLLUTED_PLANT" // 오염 식물 테스트 적군
        }; // 테스트 적군 ID 목록 종료
        private static readonly Color Navy = new Color(0.12f, 0.20f, 0.34f, 1f); // 공통 남색
        private static readonly Color Cream = new Color(0.98f, 0.96f, 0.90f, 0.96f); // 공통 크림색
        private static readonly Color HpGreen = new Color(0.30f, 0.82f, 0.38f, 1f); // HP 게이지 초록색

        [MenuItem("Tools/Project H/Phase 1/12일차 타겟팅-기본 공격 설정 실행")] // 12일차 설정 메뉴 등록
        public static void Setup() // 12일차 전투 행동 구조 적용
        {
            BackfillCharacterCombatMobility(); // 캐릭터 신규 전투 이동 수치 보정
            Scene scene = EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Single); // 기존 11일차 전투 씬 열기
            Transform battleWorld = FindRequiredTransform(scene, "BattleWorld"); // 전투 월드 조회
            Transform spawnedAllies = FindRequiredTransform(scene, "SpawnedAllies"); // 아군 생성 루트 조회
            Transform unitTemplateTransform = FindRequiredTransform(scene, "BattleUnitTemplate"); // 아군 템플릿 조회
            Transform controllerTransform = FindRequiredTransform(scene, "BattleController"); // 전투 컨트롤러 조회
            Transform menuWindow = FindRequiredTransform(scene, "MenuWindow"); // 전투 메뉴 창 조회
            Camera mainCamera = FindRequiredComponent<Camera>(scene, "Main Camera"); // 전투 메인 카메라 조회
            BattleFormationAnchors formation = FindRequiredComponent<BattleFormationAnchors>(scene, "BattleFormation"); // 전투 진형 앵커 조회
            BattleScreenController controller = FindRequiredComponent<BattleScreenController>(scene, "BattleController"); // 전투 화면 컨트롤러 조회
            BattleCombatRegistry registry = EnsureComponent<BattleCombatRegistry>(controllerTransform.gameObject); // 전투 객체 레지스트리 확보
            RemoveEnemyPreviewRoot(scene); // 11일차 임시 적군 미리보기 제거
            Transform enemyRoot = EnsureChild(battleWorld, "SpawnedEnemies"); // 실제 적군 생성 루트 확보
            BattleUnitView unitTemplate = UpgradeAllyTemplate(unitTemplateTransform, mainCamera); // 아군 템플릿 행동 구조 확장
            BattleEnemyView enemyTemplate = EnsureEnemyTemplate(enemyRoot, mainCamera); // 적군 전투 템플릿 생성
            CreateDebugButtons(menuWindow, out Button attackButton, out Button skillButton, out Button ultimateButton); // 행동 디버그 버튼 생성
            controller.ConfigureCombat(registry, enemyRoot, enemyTemplate, DefaultEnemyIds, attackButton, skillButton, ultimateButton); // 12일차 전투 행동 참조 연결
            EditorUtility.SetDirty(controller); // 전투 컨트롤러 변경 표시
            EditorUtility.SetDirty(unitTemplate); // 아군 템플릿 변경 표시
            EditorUtility.SetDirty(enemyTemplate); // 적군 템플릿 변경 표시
            EditorSceneManager.MarkSceneDirty(scene); // 전투 씬 변경 표시
            EditorSceneManager.SaveScene(scene, BattleScenePath); // 전투 씬 저장
            AssetDatabase.SaveAssets(); // 캐릭터 데이터 및 씬 변경 저장
            AssetDatabase.Refresh(); // 에셋 목록 갱신
            Debug.Log("[Project H][BATTLE] Phase 1 Day 12 targeting/basic attack setup complete."); // 12일차 설정 완료 로그
        }

        private static void BackfillCharacterCombatMobility() // 신규 캐릭터 전투 이동 수치 보정
        {
            string[] guids = AssetDatabase.FindAssets("t:CharacterData", new[] { CharacterDataRoot }); // 전체 캐릭터 데이터 GUID 조회

            foreach (string guid in guids) // 캐릭터 데이터 GUID 순회
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid); // 캐릭터 데이터 경로 변환
                CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>(assetPath); // 캐릭터 데이터 로드

                if (character == null) // 캐릭터 데이터 존재 확인
                {
                    continue; // 잘못된 캐릭터 데이터 제외
                }

                SerializedObject serialized = new SerializedObject(character); // 캐릭터 직렬화 객체 생성
                SerializedProperty attackRange = serialized.FindProperty("attackRange"); // 공격 사거리 프로퍼티 조회
                SerializedProperty moveSpeed = serialized.FindProperty("moveSpeed"); // 이동속도 프로퍼티 조회
                bool changed = false; // 캐릭터 데이터 변경 여부 초기화

                if (attackRange != null && attackRange.floatValue < 0.2f) // 공격 사거리 신규 필드 보정 필요 확인
                {
                    attackRange.floatValue = 1.6f; // 기본 공격 사거리 적용
                    changed = true; // 캐릭터 데이터 변경 기록
                }

                if (moveSpeed != null && moveSpeed.floatValue < 0.01f) // 이동속도 신규 필드 보정 필요 확인
                {
                    moveSpeed.floatValue = 2f; // 기본 전투 이동속도 적용
                    changed = true; // 캐릭터 데이터 변경 기록
                }

                if (!changed) // 캐릭터 데이터 변경 여부 확인
                {
                    continue; // 저장 불필요 캐릭터 제외
                }

                serialized.ApplyModifiedPropertiesWithoutUndo(); // 캐릭터 이동 수치 적용
                EditorUtility.SetDirty(character); // 캐릭터 에셋 변경 표시
            }
        }

        private static BattleUnitView UpgradeAllyTemplate(Transform templateTransform, Camera mainCamera) // 아군 템플릿 행동 구조 확장
        {
            BattleUnitView view = templateTransform.GetComponent<BattleUnitView>(); // 아군 전투 View 조회

            if (view == null) // 아군 전투 View 존재 확인
            {
                throw new System.InvalidOperationException("BattleUnitTemplate에 BattleUnitView가 없습니다."); // 아군 템플릿 구조 오류 발생
            }

            Canvas canvas = FindRequiredComponentInChildren<Canvas>(templateTransform, "UnitCanvas"); // 아군 월드 Canvas 조회
            Image body = FindRequiredComponentInChildren<Image>(templateTransform, "Body"); // 아군 임시 바디 조회
            Text characterText = FindRequiredComponentInChildren<Text>(templateTransform, "CharacterText"); // 아군 캐릭터 이름 조회
            Text runtimeText = FindRequiredComponentInChildren<Text>(templateTransform, "RuntimeIdText"); // 아군 런타임 ID 조회
            Text roleText = FindRequiredComponentInChildren<Text>(templateTransform, "RoleText"); // 아군 역할 텍스트 조회
            Text hpText = FindRequiredComponentInChildren<Text>(templateTransform, "HpText"); // 아군 HP 텍스트 조회
            Image hpFill = FindRequiredComponentInChildren<Image>(templateTransform, "HpFill"); // 아군 HP 게이지 조회
            BattleActor actor = EnsureComponent<BattleActor>(templateTransform.gameObject); // 아군 공통 전투 액터 확보
            BattleActionDebugText debugText = EnsureActionDebugText(canvas.transform); // 아군 머리 위 행동 텍스트 확보
            canvas.worldCamera = mainCamera; // 아군 월드 Canvas 카메라 연결
            view.Configure(canvas, body, characterText, runtimeText, roleText, hpText, hpFill, actor, debugText); // 아군 전투 View 12일차 참조 연결
            return view; // 아군 전투 View 반환
        }

        private static BattleEnemyView EnsureEnemyTemplate(Transform enemyRoot, Camera mainCamera) // 적군 전투 템플릿 생성 또는 갱신
        {
            Transform existing = FindChildRecursive(enemyRoot, "BattleEnemyTemplate"); // 기존 적군 템플릿 조회

            if (existing != null) // 기존 적군 템플릿 존재 확인
            {
                return ConfigureExistingEnemyTemplate(existing, mainCamera); // 기존 적군 템플릿 갱신 반환
            }

            GameObject root = new GameObject("BattleEnemyTemplate", typeof(BattleEnemyView), typeof(BattleActor)); // 적군 템플릿 루트 생성
            root.transform.SetParent(enemyRoot, false); // 적군 생성 루트 연결
            BattleEnemyView view = root.GetComponent<BattleEnemyView>(); // 적군 View 조회
            BattleActor actor = root.GetComponent<BattleActor>(); // 적군 전투 액터 조회
            Canvas canvas = CreateWorldCanvas(root.transform, "EnemyCanvas", mainCamera, new Vector2(190f, 285f), 0.006f, 5); // 적군 월드 Canvas 생성
            Image body = CreateImage(canvas.transform, "Body", null, new Color(0.58f, 0.28f, 0.20f, 0.96f)); // 적군 임시 바디 생성
            SetRect(body.rectTransform, new Vector2(0.17f, 0.25f), new Vector2(0.83f, 0.80f)); // 적군 바디 배치
            Text nameText = CreateText(body.transform, "CharacterText", "ENEMY", 25, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter); // 적군 이름 텍스트 생성
            Stretch(nameText.rectTransform, 8f); // 적군 이름 영역 확장
            Text runtimeText = CreateText(canvas.transform, "RuntimeIdText", "ENEMY_0", 13, FontStyle.Normal, Navy, TextAnchor.MiddleCenter); // 적군 런타임 ID 텍스트 생성
            SetRect(runtimeText.rectTransform, new Vector2(0.25f, 0.91f), new Vector2(0.75f, 0.98f)); // 적군 런타임 ID 배치
            Image hpBack = CreateImage(canvas.transform, "HpBack", null, new Color(0.12f, 0.17f, 0.19f, 0.92f)); // 적군 HP 배경 생성
            SetRect(hpBack.rectTransform, new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.20f)); // 적군 HP 배경 배치
            Image hpFill = CreateImage(hpBack.transform, "HpFill", null, HpGreen); // 적군 HP 게이지 생성
            SetRect(hpFill.rectTransform, Vector2.zero, Vector2.one); // 적군 HP 게이지 전체 채우기
            Text hpText = CreateText(canvas.transform, "HpText", "0 / 0", 14, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter); // 적군 HP 수치 생성
            SetRect(hpText.rectTransform, new Vector2(0.10f, 0.10f), new Vector2(0.90f, 0.22f)); // 적군 HP 수치 배치
            BattleActionDebugText debugText = EnsureActionDebugText(canvas.transform); // 적군 행동 디버그 텍스트 생성
            view.Configure(canvas, body, nameText, runtimeText, hpText, hpFill, actor, debugText); // 적군 전투 View 참조 연결
            root.SetActive(false); // 적군 템플릿 초기 숨김
            return view; // 적군 전투 템플릿 반환
        }

        private static BattleEnemyView ConfigureExistingEnemyTemplate(Transform templateTransform, Camera mainCamera) // 기존 적군 템플릿 참조 갱신
        {
            BattleEnemyView view = EnsureComponent<BattleEnemyView>(templateTransform.gameObject); // 적군 View 확보
            BattleActor actor = EnsureComponent<BattleActor>(templateTransform.gameObject); // 적군 액터 확보
            Canvas canvas = FindRequiredComponentInChildren<Canvas>(templateTransform, "EnemyCanvas"); // 적군 Canvas 조회
            Image body = FindRequiredComponentInChildren<Image>(templateTransform, "Body"); // 적군 바디 조회
            Text nameText = FindRequiredComponentInChildren<Text>(templateTransform, "CharacterText"); // 적군 이름 조회
            Text runtimeText = FindRequiredComponentInChildren<Text>(templateTransform, "RuntimeIdText"); // 적군 런타임 ID 조회
            Text hpText = FindRequiredComponentInChildren<Text>(templateTransform, "HpText"); // 적군 HP 수치 조회
            Image hpFill = FindRequiredComponentInChildren<Image>(templateTransform, "HpFill"); // 적군 HP 게이지 조회
            BattleActionDebugText debugText = EnsureActionDebugText(canvas.transform); // 적군 행동 텍스트 확보
            canvas.worldCamera = mainCamera; // 적군 월드 Canvas 카메라 연결
            view.Configure(canvas, body, nameText, runtimeText, hpText, hpFill, actor, debugText); // 기존 적군 View 참조 재연결
            templateTransform.gameObject.SetActive(false); // 적군 템플릿 숨김 유지
            return view; // 기존 적군 View 반환
        }

        private static BattleActionDebugText EnsureActionDebugText(Transform canvasTransform) // 머리 위 행동 디버그 텍스트 생성 또는 조회
        {
            Transform existing = FindChildRecursive(canvasTransform, "ActionDebugText"); // 기존 행동 텍스트 조회

            if (existing != null) // 기존 행동 텍스트 존재 확인
            {
                Text existingText = existing.GetComponent<Text>(); // 기존 행동 Text 조회
                BattleActionDebugText existingDebug = EnsureComponent<BattleActionDebugText>(existing.gameObject); // 기존 행동 디버그 컴포넌트 확보
                existingDebug.Configure(existingText); // 기존 행동 텍스트 참조 연결
                return existingDebug; // 기존 행동 디버그 컴포넌트 반환
            }

            Text text = CreateText(canvasTransform, "ActionDebugText", "공격!", 30, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter); // 머리 위 행동 텍스트 생성
            SetRect(text.rectTransform, new Vector2(0.05f, 0.84f), new Vector2(0.95f, 1.06f)); // 행동 텍스트 머리 위 배치
            Outline outline = text.gameObject.AddComponent<Outline>(); // 행동 텍스트 외곽선 추가
            outline.effectColor = new Color(0.08f, 0.10f, 0.14f, 0.95f); // 행동 텍스트 외곽선 색상 설정
            outline.effectDistance = new Vector2(2f, -2f); // 행동 텍스트 외곽선 거리 설정
            BattleActionDebugText debugText = text.gameObject.AddComponent<BattleActionDebugText>(); // 행동 디버그 컴포넌트 추가
            debugText.Configure(text); // 행동 디버그 Text 참조 연결
            return debugText; // 행동 디버그 컴포넌트 반환
        }

        private static void CreateDebugButtons(Transform menuWindow, out Button attackButton, out Button skillButton, out Button ultimateButton) // 행동 디버그 버튼 생성
        {
            Text guide = FindOptionalComponentInChildren<Text>(menuWindow, "MenuGuide"); // 기존 메뉴 안내 텍스트 조회

            if (guide != null) // 메뉴 안내 텍스트 확인
            {
                guide.text = "12일차 전투 행동 디버그\n기본 공격은 자동 실행되며 아래 버튼으로 머리 위 행동 텍스트를 확인할 수 있습니다."; // 12일차 메뉴 안내 문구 적용
                SetRect(guide.rectTransform, new Vector2(0.08f, 0.60f), new Vector2(0.92f, 0.76f)); // 메뉴 안내 영역 상단 배치
            }

            attackButton = EnsureButton(menuWindow, "DebugAttackButton", "공격! 표시", 0.10f, 0.44f, 0.36f, 0.56f); // 공격 디버그 버튼 생성
            skillButton = EnsureButton(menuWindow, "DebugSkillButton", "스킬! 표시", 0.37f, 0.44f, 0.63f, 0.56f); // 스킬 디버그 버튼 생성
            ultimateButton = EnsureButton(menuWindow, "DebugUltimateButton", "궁극기! 표시", 0.64f, 0.44f, 0.90f, 0.56f); // 궁극기 디버그 버튼 생성
            Button returnButton = FindOptionalComponentInChildren<Button>(menuWindow, "ReturnDungeonButton"); // 던전 복귀 버튼 조회
            Button closeButton = FindOptionalComponentInChildren<Button>(menuWindow, "CloseMenuButton"); // 메뉴 닫기 버튼 조회

            if (returnButton != null) // 던전 복귀 버튼 확인
            {
                SetRect(returnButton.GetComponent<RectTransform>(), new Vector2(0.16f, 0.20f), new Vector2(0.84f, 0.34f)); // 던전 복귀 버튼 하단 재배치
            }

            if (closeButton != null) // 메뉴 닫기 버튼 확인
            {
                SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(0.28f, 0.06f), new Vector2(0.72f, 0.17f)); // 메뉴 닫기 버튼 하단 재배치
            }
        }

        private static Button EnsureButton(Transform parent, string name, string label, float minX, float minY, float maxX, float maxY) // 메뉴 버튼 생성 또는 갱신
        {
            Transform existing = FindChildRecursive(parent, name); // 기존 버튼 객체 조회
            Button button; // 대상 버튼 변수 선언

            if (existing != null) // 기존 버튼 존재 확인
            {
                button = existing.GetComponent<Button>(); // 기존 버튼 컴포넌트 조회
            }
            else // 신규 버튼 생성 처리
            {
                GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); // 신규 버튼 객체 생성
                buttonObject.transform.SetParent(parent, false); // 메뉴 창 연결
                Image image = buttonObject.GetComponent<Image>(); // 신규 버튼 이미지 조회
                image.sprite = LoadSprite("Buttons/button_small.png"); // 공통 작은 버튼 Sprite 적용
                image.color = Color.white; // 신규 버튼 기본색 적용
                button = buttonObject.GetComponent<Button>(); // 신규 버튼 컴포넌트 조회
                button.targetGraphic = image; // 버튼 대상 그래픽 연결
                Text text = CreateText(buttonObject.transform, "Label", label, 18, FontStyle.Bold, Navy, TextAnchor.MiddleCenter); // 신규 버튼 라벨 생성
                Stretch(text.rectTransform, 4f); // 신규 버튼 라벨 확장
            }

            Text labelText = button.GetComponentInChildren<Text>(true); // 버튼 라벨 Text 조회

            if (labelText != null) // 버튼 라벨 존재 확인
            {
                labelText.text = label; // 버튼 라벨 문구 갱신
                labelText.alignment = TextAnchor.MiddleCenter; // 버튼 라벨 중앙 정렬
            }

            SetRect(button.GetComponent<RectTransform>(), new Vector2(minX, minY), new Vector2(maxX, maxY)); // 메뉴 버튼 지정 위치 배치
            return button; // 메뉴 버튼 반환
        }

        private static void RemoveEnemyPreviewRoot(Scene scene) // 11일차 임시 적군 루트 제거
        {
            Transform previewRoot = FindOptionalTransform(scene, "EnemyPreviews"); // 임시 적군 루트 조회

            if (previewRoot == null) // 임시 적군 루트 존재 확인
            {
                return; // 임시 적군 제거 불필요
            }

            UnityEngine.Object.DestroyImmediate(previewRoot.gameObject); // 11일차 임시 적군 미리보기 제거
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
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // 텍스트 객체 생성
            textObject.transform.SetParent(parent, false); // 부모 객체 연결
            Text text = textObject.GetComponent<Text>(); // Text 컴포넌트 조회
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 설정
            text.text = value; // 텍스트 내용 설정
            text.fontSize = size; // 텍스트 크기 설정
            text.fontStyle = style; // 텍스트 스타일 설정
            text.color = color; // 텍스트 색상 설정
            text.alignment = alignment; // 텍스트 정렬 설정
            text.alignByGeometry = true; // 글리프 기준 정렬 적용
            text.resizeTextForBestFit = true; // 자동 텍스트 크기 사용
            text.resizeTextMinSize = 10; // 최소 텍스트 크기 설정
            text.resizeTextMaxSize = size; // 최대 텍스트 크기 설정
            text.raycastTarget = false; // 텍스트 입력 비활성화
            return text; // Text 반환
        }

        private static Sprite LoadSprite(string relativePath) // 프로토타입 UI Sprite 로드
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/{relativePath}"); // UI Sprite 반환
        }

        private static Transform EnsureChild(Transform parent, string name) // 하위 객체 생성 또는 조회
        {
            Transform existing = FindChildRecursive(parent, name); // 기존 하위 객체 조회

            if (existing != null) // 기존 하위 객체 존재 확인
            {
                return existing; // 기존 하위 객체 반환
            }

            GameObject child = new GameObject(name); // 신규 하위 객체 생성
            child.transform.SetParent(parent, false); // 부모 객체 연결
            return child.transform; // 신규 하위 Transform 반환
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

        private static Transform FindRequiredTransform(Scene scene, string objectName) // 씬 필수 Transform 조회
        {
            Transform result = FindOptionalTransform(scene, objectName); // 씬 Transform 검색

            if (result == null) // 필수 Transform 존재 확인
            {
                throw new System.InvalidOperationException($"Battle Scene object not found: {objectName}. Day 11 Scene을 먼저 구성해 주세요."); // 필수 객체 누락 예외 발생
            }

            return result; // 필수 Transform 반환
        }

        private static Transform FindOptionalTransform(Scene scene, string objectName) // 씬 선택 Transform 조회
        {
            foreach (GameObject root in scene.GetRootGameObjects()) // 씬 루트 객체 순회
            {
                Transform found = FindChildRecursive(root.transform, objectName); // 루트 하위 이름 검색

                if (found != null) // 검색 결과 확인
                {
                    return found; // 일치 Transform 반환
                }
            }

            return null; // 검색 실패 반환
        }

        private static T FindRequiredComponent<T>(Scene scene, string objectName) where T : Component // 씬 필수 컴포넌트 조회
        {
            Transform transform = FindRequiredTransform(scene, objectName); // 대상 Transform 조회
            T component = transform.GetComponent<T>(); // 대상 컴포넌트 조회

            if (component == null) // 대상 컴포넌트 존재 확인
            {
                throw new System.InvalidOperationException($"Required component {typeof(T).Name} not found on {objectName}."); // 필수 컴포넌트 누락 예외 발생
            }

            return component; // 필수 컴포넌트 반환
        }

        private static T FindRequiredComponentInChildren<T>(Transform root, string objectName) where T : Component // 하위 필수 컴포넌트 조회
        {
            Transform child = FindChildRecursive(root, objectName); // 하위 객체 이름 검색

            if (child == null) // 하위 객체 존재 확인
            {
                throw new System.InvalidOperationException($"Child object not found: {objectName}."); // 하위 객체 누락 예외 발생
            }

            T component = child.GetComponent<T>(); // 하위 컴포넌트 조회

            if (component == null) // 하위 컴포넌트 존재 확인
            {
                throw new System.InvalidOperationException($"Required component {typeof(T).Name} not found on {objectName}."); // 하위 컴포넌트 누락 예외 발생
            }

            return component; // 하위 필수 컴포넌트 반환
        }

        private static T FindOptionalComponentInChildren<T>(Transform root, string objectName) where T : Component // 하위 선택 컴포넌트 조회
        {
            Transform child = FindChildRecursive(root, objectName); // 하위 객체 이름 검색
            return child == null ? null : child.GetComponent<T>(); // 하위 컴포넌트 또는 null 반환
        }

        private static Transform FindChildRecursive(Transform root, string objectName) // 하위 Transform 재귀 검색
        {
            if (root.name == objectName) // 현재 객체 이름 확인
            {
                return root; // 현재 Transform 반환
            }

            for (int index = 0; index < root.childCount; index++) // 자식 객체 순회
            {
                Transform found = FindChildRecursive(root.GetChild(index), objectName); // 자식 객체 재귀 검색

                if (found != null) // 자식 검색 결과 확인
                {
                    return found; // 검색된 Transform 반환
                }
            }

            return null; // 검색 실패 반환
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
