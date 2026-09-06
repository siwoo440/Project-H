using ProjectH.Core; // 프로젝트 핵심 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // Unity 씬 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class TitleScreenVisualPatch // 타이틀 화면 시각 요소 패치
    {
        private const string BackgroundResourcePath = "UI/TitleBackground"; // 타이틀 배경 리소스 경로
        private static Sprite backgroundSprite; // 런타임 배경 스프라이트

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // 첫 씬 로드 전 이벤트 구독 지정
        private static void RegisterSceneLoadedHandler() // 씬 로드 이벤트 구독
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded; // 중복 씬 로드 구독 해제
            SceneManager.sceneLoaded += HandleSceneLoaded; // 씬 로드 이벤트 구독
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode _) // 씬 로드 완료 처리
        {
            if (scene.name != GameScenes.Title) // 타이틀 씬 여부 확인
            {
                return; // 다른 씬 수정 중단
            }

            ApplyBackground(scene); // 타이틀 배경 적용
            HideMenuPanel(scene); // 메뉴 패널 배경 제거
            HideStatus(scene); // 저장 상태 문구 제거
            PositionTitleTexts(scene); // 제목과 부제 좌측 상단 배치
            MatchQuitButtonSize(scene); // 종료 버튼 크기 통일
        }

        private static void ApplyBackground(Scene scene) // 타이틀 배경 이미지 적용
        {
            GameObject backgroundObject = FindSceneObject(scene, "Background"); // 배경 오브젝트 조회
            Image backgroundImage = backgroundObject == null ? null : backgroundObject.GetComponent<Image>(); // 배경 이미지 컴포넌트 조회

            if (backgroundImage == null) // 배경 이미지 존재 확인
            {
                return; // 배경 적용 중단
            }

            Sprite sprite = GetBackgroundSprite(); // 타이틀 배경 스프라이트 조회

            if (sprite == null) // 배경 스프라이트 존재 확인
            {
                return; // 배경 적용 중단
            }

            backgroundImage.sprite = sprite; // 업로드 배경 이미지 적용
            backgroundImage.color = Color.white; // 원본 이미지 색상 유지
            backgroundImage.type = Image.Type.Simple; // 단순 이미지 표시 방식 적용
            backgroundImage.preserveAspect = false; // 화면 전체 배경 채우기 적용
            backgroundImage.raycastTarget = false; // 배경 입력 차단 해제
        }

        private static Sprite GetBackgroundSprite() // 런타임 배경 스프라이트 조회
        {
            if (backgroundSprite != null) // 기존 스프라이트 존재 확인
            {
                return backgroundSprite; // 기존 스프라이트 반환
            }

            Texture2D texture = Resources.Load<Texture2D>(BackgroundResourcePath); // 원본 배경 텍스처 로드

            if (texture == null) // 배경 텍스처 존재 확인
            {
                Debug.LogWarning("[Project H][UI] Title background texture was not found."); // 배경 누락 경고 출력
                return null; // 배경 스프라이트 없음 반환
            }

            Rect textureRect = new Rect(0f, 0f, texture.width, texture.height); // 전체 텍스처 영역 생성
            backgroundSprite = Sprite.Create(texture, textureRect, new Vector2(0.5f, 0.5f), 100f); // 런타임 배경 스프라이트 생성
            backgroundSprite.name = "TitleBackgroundRuntime"; // 런타임 스프라이트 이름 지정
            return backgroundSprite; // 생성 배경 스프라이트 반환
        }

        private static void HideMenuPanel(Scene scene) // 메뉴 패널 배경 제거
        {
            GameObject panelObject = FindSceneObject(scene, "TitleMenuPanel"); // 메뉴 패널 오브젝트 조회

            if (panelObject == null) // 메뉴 패널 존재 확인
            {
                return; // 패널 제거 중단
            }

            Image panelImage = panelObject.GetComponent<Image>(); // 메뉴 패널 이미지 조회

            if (panelImage != null) // 패널 이미지 존재 확인
            {
                panelImage.enabled = false; // 패널 배경 이미지 숨김
            }

            Shadow[] panelEffects = panelObject.GetComponents<Shadow>(); // 패널 그림자 효과 조회

            foreach (Shadow panelEffect in panelEffects) // 패널 효과 순회
            {
                panelEffect.enabled = false; // 패널 외곽 효과 숨김
            }
        }

        private static void HideStatus(Scene scene) // 저장 상태 문구 제거
        {
            GameObject statusObject = FindSceneObject(scene, "Status"); // 저장 상태 오브젝트 조회

            if (statusObject == null) // 저장 상태 오브젝트 존재 확인
            {
                return; // 상태 문구 제거 중단
            }

            statusObject.SetActive(false); // 저장 상태 문구 전체 숨김
        }

        private static void PositionTitleTexts(Scene scene) // 제목과 부제 좌측 상단 배치
        {
            GameObject titleObject = FindSceneObject(scene, "GameTitle"); // 게임 제목 오브젝트 조회
            GameObject subtitleObject = FindSceneObject(scene, "Subtitle"); // 부제 오브젝트 조회
            RectTransform titleRect = titleObject == null ? null : titleObject.GetComponent<RectTransform>(); // 게임 제목 사각 영역 조회
            RectTransform subtitleRect = subtitleObject == null ? null : subtitleObject.GetComponent<RectTransform>(); // 부제 사각 영역 조회
            Text titleText = titleObject == null ? null : titleObject.GetComponent<Text>(); // 게임 제목 텍스트 조회
            Text subtitleText = subtitleObject == null ? null : subtitleObject.GetComponent<Text>(); // 부제 텍스트 조회

            if (titleRect != null) // 게임 제목 사각 영역 존재 확인
            {
                titleRect.anchorMin = new Vector2(0f, 1f); // 좌측 상단 최소 앵커 적용
                titleRect.anchorMax = new Vector2(0f, 1f); // 좌측 상단 최대 앵커 적용
                titleRect.pivot = new Vector2(0f, 1f); // 좌측 상단 피벗 적용
                titleRect.anchoredPosition = new Vector2(120f, -90f); // 제목 좌측 상단 위치 적용
                titleRect.sizeDelta = new Vector2(720f, 110f); // 제목 표시 영역 크기 적용
            }

            if (titleText != null) // 게임 제목 텍스트 존재 확인
            {
                titleText.alignment = TextAnchor.MiddleLeft; // 게임 제목 좌측 정렬 적용
            }

            if (subtitleRect != null) // 부제 사각 영역 존재 확인
            {
                subtitleRect.anchorMin = new Vector2(0f, 1f); // 좌측 상단 최소 앵커 적용
                subtitleRect.anchorMax = new Vector2(0f, 1f); // 좌측 상단 최대 앵커 적용
                subtitleRect.pivot = new Vector2(0f, 1f); // 좌측 상단 피벗 적용
                subtitleRect.anchoredPosition = new Vector2(124f, -210f); // 부제 제목 아래 위치 적용
                subtitleRect.sizeDelta = new Vector2(760f, 60f); // 부제 표시 영역 크기 적용
            }

            if (subtitleText != null) // 부제 텍스트 존재 확인
            {
                subtitleText.alignment = TextAnchor.MiddleLeft; // 부제 좌측 정렬 적용
            }
        }

        private static void MatchQuitButtonSize(Scene scene) // 종료 버튼 크기 통일
        {
            GameObject newGameObject = FindSceneObject(scene, "NewGameButton"); // 새 게임 버튼 조회
            GameObject quitObject = FindSceneObject(scene, "QuitButton"); // 종료 버튼 조회
            RectTransform newGameRect = newGameObject == null ? null : newGameObject.GetComponent<RectTransform>(); // 새 게임 버튼 사각 영역 조회
            RectTransform quitRect = quitObject == null ? null : quitObject.GetComponent<RectTransform>(); // 종료 버튼 사각 영역 조회

            if (newGameRect == null || quitRect == null) // 버튼 사각 영역 존재 확인
            {
                return; // 종료 버튼 크기 변경 중단
            }

            quitRect.sizeDelta = newGameRect.sizeDelta; // 새 게임 버튼과 동일 크기 적용
            quitRect.localScale = newGameRect.localScale; // 새 게임 버튼과 동일 배율 적용

            LayoutElement newGameLayout = newGameObject.GetComponent<LayoutElement>(); // 새 게임 레이아웃 값 조회
            LayoutElement quitLayout = quitObject.GetComponent<LayoutElement>(); // 종료 버튼 레이아웃 값 조회

            if (newGameLayout != null && quitLayout != null) // 레이아웃 요소 존재 확인
            {
                quitLayout.minWidth = newGameLayout.minWidth; // 최소 너비 통일
                quitLayout.minHeight = newGameLayout.minHeight; // 최소 높이 통일
                quitLayout.preferredWidth = newGameLayout.preferredWidth; // 선호 너비 통일
                quitLayout.preferredHeight = newGameLayout.preferredHeight; // 선호 높이 통일
                quitLayout.flexibleWidth = newGameLayout.flexibleWidth; // 유동 너비 통일
                quitLayout.flexibleHeight = newGameLayout.flexibleHeight; // 유동 높이 통일
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(quitRect); // 종료 버튼 레이아웃 즉시 갱신
        }

        private static GameObject FindSceneObject(Scene scene, string objectName) // 씬 내부 이름 기반 오브젝트 조회
        {
            GameObject[] roots = scene.GetRootGameObjects(); // 씬 루트 오브젝트 조회

            foreach (GameObject root in roots) // 씬 루트 순회
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true); // 비활성 포함 하위 오브젝트 조회

                foreach (Transform target in transforms) // 하위 오브젝트 순회
                {
                    if (target.name == objectName) // 대상 이름 일치 확인
                    {
                        return target.gameObject; // 대상 오브젝트 반환
                    }
                }
            }

            return null; // 대상 오브젝트 없음 반환
        }
    }
}
