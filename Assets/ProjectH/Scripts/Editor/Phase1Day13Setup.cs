using ProjectH.Battle; // 전투 피해 회복 기능
using UnityEditor; // Unity 에디터 기능
using UnityEditor.SceneManagement; // 에디터 씬 관리
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // Unity 씬 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.EditorTools // 프로젝트 에디터 도구 영역
{
    public static class Phase1Day13Setup // 13일차 피해 회복 시스템 설정 도구
    {
        private const string BattleScenePath = "Assets/ProjectH/Scenes/Battle.unity"; // 전투 씬 경로
        private static readonly Color Navy = new Color(0.12f, 0.20f, 0.34f, 1f); // 공통 남색
        private static readonly Color Cream = new Color(0.98f, 0.96f, 0.90f, 0.98f); // 공통 크림색

        [MenuItem("Tools/Project H/Phase 1/13일차 피해-회복 시스템 설정 실행")] // 13일차 설정 메뉴 등록
        public static void Setup() // 13일차 피해 회복 Scene 구조 적용
        {
            Scene scene = EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Single); // 기존 Battle Scene 열기
            Transform allyTemplate = FindRequiredTransform(scene, "BattleUnitTemplate"); // 아군 전투 템플릿 조회
            Transform enemyTemplate = FindRequiredTransform(scene, "BattleEnemyTemplate"); // 적군 전투 템플릿 조회
            Transform battleControllerTransform = FindRequiredTransform(scene, "BattleController"); // 전투 컨트롤러 조회
            Transform menuWindow = FindRequiredTransform(scene, "MenuWindow"); // 전투 메뉴 창 조회
            Text statusText = FindRequiredComponent<Text>(scene, "BattleStatus"); // 전투 상태 텍스트 조회
            BattleCombatRegistry registry = FindRequiredComponent<BattleCombatRegistry>(scene, "BattleController"); // 전투 레지스트리 조회
            UpgradeActorTemplate(allyTemplate, "UnitCanvas"); // 아군 피해 회복 숫자 표시 추가
            UpgradeActorTemplate(enemyTemplate, "EnemyCanvas"); // 적군 피해 회복 숫자 표시 추가
            Button healButton = EnsureHealButton(menuWindow); // 회복 디버그 버튼 생성
            BattleHealthDebugController healthDebug = EnsureComponent<BattleHealthDebugController>(battleControllerTransform.gameObject); // 체력 디버그 컨트롤러 확보
            healthDebug.Configure(registry, healButton, statusText); // 체력 디버그 참조 연결
            EditorUtility.SetDirty(healthDebug); // 체력 디버그 변경 표시
            EditorSceneManager.MarkSceneDirty(scene); // Battle Scene 변경 표시
            EditorSceneManager.SaveScene(scene, BattleScenePath); // Battle Scene 저장
            AssetDatabase.SaveAssets(); // 에셋 변경 저장
            AssetDatabase.Refresh(); // 에셋 목록 갱신
            Debug.Log("[Project H][BATTLE] Phase 1 Day 13 damage/healing setup complete."); // 13일차 설정 완료 로그
        }

        private static void UpgradeActorTemplate(Transform templateRoot, string canvasName) // 전투 템플릿 체력 변화 표시 확장
        {
            BattleActor actor = EnsureComponent<BattleActor>(templateRoot.gameObject); // 전투 액터 확보
            Transform canvasTransform = FindRequiredChild(templateRoot, canvasName); // 전투 월드 Canvas 조회
            Image bodyImage = FindRequiredChild(templateRoot, "Body").GetComponent<Image>(); // 전투 바디 이미지 조회
            BattleActionDebugText actionDebug = FindRequiredChild(templateRoot, "ActionDebugText").GetComponent<BattleActionDebugText>(); // 행동 디버그 텍스트 조회
            BattleFloatingValueText floatingValue = EnsureFloatingValueText(canvasTransform); // 체력 변화 숫자 텍스트 확보

            if (bodyImage == null || actionDebug == null) // 기존 전투 시각 참조 확인
            {
                throw new System.InvalidOperationException($"{templateRoot.name} 전투 시각 참조가 올바르지 않습니다."); // 전투 템플릿 구조 오류 발생
            }

            actor.ConfigureVisuals(bodyImage, actionDebug, floatingValue); // 전투 액터 체력 변화 표시 참조 연결
            EditorUtility.SetDirty(actor); // 전투 액터 변경 표시
        }

        private static BattleFloatingValueText EnsureFloatingValueText(Transform canvasTransform) // 피해 회복 숫자 텍스트 생성 또는 조회
        {
            Transform existing = FindOptionalChild(canvasTransform, "FloatingValueText"); // 기존 체력 변화 텍스트 조회

            if (existing != null) // 기존 체력 변화 텍스트 존재 확인
            {
                Text existingText = existing.GetComponent<Text>(); // 기존 Text 컴포넌트 조회
                BattleFloatingValueText existingFloating = EnsureComponent<BattleFloatingValueText>(existing.gameObject); // 기존 체력 변화 컴포넌트 확보
                existingFloating.Configure(existingText); // 기존 Text 참조 연결
                EditorUtility.SetDirty(existingFloating); // 기존 체력 변화 컴포넌트 변경 표시
                return existingFloating; // 기존 체력 변화 컴포넌트 반환
            }

            GameObject textObject = new GameObject("FloatingValueText", typeof(RectTransform), typeof(Text), typeof(Outline), typeof(BattleFloatingValueText)); // 체력 변화 숫자 객체 생성
            textObject.transform.SetParent(canvasTransform, false); // 전투 월드 Canvas 연결
            RectTransform rect = textObject.GetComponent<RectTransform>(); // 체력 변화 RectTransform 조회
            rect.anchorMin = new Vector2(0.16f, 0.69f); // 체력 변화 최소 앵커 설정
            rect.anchorMax = new Vector2(0.84f, 0.84f); // 체력 변화 최대 앵커 설정
            rect.offsetMin = Vector2.zero; // 체력 변화 최소 오프셋 초기화
            rect.offsetMax = Vector2.zero; // 체력 변화 최대 오프셋 초기화
            Text text = textObject.GetComponent<Text>(); // 체력 변화 Text 조회
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 적용
            text.text = "-0"; // 체력 변화 초기 문구 설정
            text.fontSize = 28; // 체력 변화 폰트 크기 설정
            text.fontStyle = FontStyle.Bold; // 체력 변화 굵은 글씨 설정
            text.alignment = TextAnchor.MiddleCenter; // 체력 변화 중앙 정렬
            text.alignByGeometry = true; // 글리프 기준 정렬 적용
            text.resizeTextForBestFit = true; // 자동 텍스트 크기 적용
            text.resizeTextMinSize = 12; // 최소 텍스트 크기 설정
            text.resizeTextMaxSize = 28; // 최대 텍스트 크기 설정
            text.raycastTarget = false; // 체력 변화 입력 비활성화
            Outline outline = textObject.GetComponent<Outline>(); // 체력 변화 외곽선 조회
            outline.effectColor = new Color(0.05f, 0.06f, 0.08f, 0.96f); // 체력 변화 외곽선 색상 설정
            outline.effectDistance = new Vector2(2f, -2f); // 체력 변화 외곽선 거리 설정
            BattleFloatingValueText floating = textObject.GetComponent<BattleFloatingValueText>(); // 체력 변화 컴포넌트 조회
            floating.Configure(text); // 체력 변화 Text 참조 연결
            EditorUtility.SetDirty(floating); // 신규 체력 변화 컴포넌트 변경 표시
            return floating; // 신규 체력 변화 컴포넌트 반환
        }

        private static Button EnsureHealButton(Transform menuWindow) // 회복 디버그 버튼 생성 또는 조회
        {
            Transform existing = FindOptionalChild(menuWindow, "DebugHealButton"); // 기존 회복 디버그 버튼 조회
            Button button; // 회복 디버그 버튼 변수 선언

            if (existing != null) // 기존 회복 버튼 존재 확인
            {
                button = existing.GetComponent<Button>(); // 기존 회복 버튼 조회
            }
            else // 신규 회복 버튼 생성 처리
            {
                GameObject buttonObject = new GameObject("DebugHealButton", typeof(RectTransform), typeof(Image), typeof(Button)); // 회복 디버그 버튼 객체 생성
                buttonObject.transform.SetParent(menuWindow, false); // 전투 메뉴 창 연결
                Image image = buttonObject.GetComponent<Image>(); // 회복 버튼 이미지 조회
                image.color = Cream; // 회복 버튼 크림색 적용
                button = buttonObject.GetComponent<Button>(); // 회복 Button 컴포넌트 조회
                button.targetGraphic = image; // 회복 버튼 대상 그래픽 연결
                GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text)); // 회복 버튼 라벨 객체 생성
                labelObject.transform.SetParent(buttonObject.transform, false); // 회복 버튼 라벨 연결
                Text label = labelObject.GetComponent<Text>(); // 회복 버튼 라벨 조회
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 적용
                label.fontSize = 17; // 회복 버튼 글자 크기 설정
                label.fontStyle = FontStyle.Bold; // 회복 버튼 굵은 글씨 설정
                label.color = Navy; // 회복 버튼 글자 색상 설정
                label.alignment = TextAnchor.MiddleCenter; // 회복 버튼 글자 중앙 정렬
                label.raycastTarget = false; // 회복 버튼 라벨 입력 비활성화
                Stretch(label.rectTransform, 4f); // 회복 버튼 라벨 전체 확장
            }

            Text buttonLabel = button.GetComponentInChildren<Text>(true); // 회복 버튼 라벨 조회

            if (buttonLabel != null) // 회복 버튼 라벨 확인
            {
                buttonLabel.text = "아군 +25 회복"; // 회복 디버그 버튼 문구 적용
                buttonLabel.alignment = TextAnchor.MiddleCenter; // 회복 버튼 글자 중앙 정렬
            }

            RectTransform rect = button.GetComponent<RectTransform>(); // 회복 버튼 RectTransform 조회
            rect.anchorMin = new Vector2(0.30f, 0.35f); // 회복 버튼 최소 앵커 설정
            rect.anchorMax = new Vector2(0.70f, 0.42f); // 회복 버튼 최대 앵커 설정
            rect.offsetMin = Vector2.zero; // 회복 버튼 최소 오프셋 초기화
            rect.offsetMax = Vector2.zero; // 회복 버튼 최대 오프셋 초기화
            return button; // 회복 디버그 버튼 반환
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

        private static T FindRequiredComponent<T>(Scene scene, string objectName) where T : Component // 씬 필수 컴포넌트 조회
        {
            Transform transform = FindRequiredTransform(scene, objectName); // 대상 Transform 조회
            T component = transform.GetComponent<T>(); // 대상 컴포넌트 조회

            if (component == null) // 대상 컴포넌트 존재 확인
            {
                throw new System.InvalidOperationException($"{objectName}에 {typeof(T).Name} 컴포넌트가 없습니다."); // 필수 컴포넌트 누락 예외 발생
            }

            return component; // 필수 컴포넌트 반환
        }

        private static Transform FindRequiredTransform(Scene scene, string objectName) // 씬 필수 Transform 조회
        {
            foreach (GameObject root in scene.GetRootGameObjects()) // 씬 루트 객체 순회
            {
                Transform found = FindOptionalChild(root.transform, objectName); // 루트 하위 객체 검색

                if (found != null) // 검색 결과 확인
                {
                    return found; // 필수 Transform 반환
                }
            }

            throw new System.InvalidOperationException($"Battle Scene object not found: {objectName}."); // 필수 객체 누락 예외 발생
        }

        private static Transform FindRequiredChild(Transform root, string objectName) // 하위 필수 Transform 조회
        {
            Transform found = FindOptionalChild(root, objectName); // 하위 객체 검색

            if (found == null) // 하위 객체 존재 확인
            {
                throw new System.InvalidOperationException($"Child object not found: {objectName}."); // 하위 객체 누락 예외 발생
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

            return null; // 검색 실패 반환
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
