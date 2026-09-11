using ProjectH.Core; // 게임 관리자 및 씬 이름 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // 씬 이벤트 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class LobbyShopRuntimePatch // Lobby 상점 진입 버튼 Runtime 패치
    {
        private const string ShopButtonName = "ShopSceneEntryButton"; // 상점 버튼 객체 이름

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // Runtime 로비 패치 등록
        private static void Register() // 씬 로드 이벤트 등록
        {
            SceneRuntimePatch.Register(GameScenes.Lobby, OnSceneLoaded); // Lobby 씬 로드 시 주입 등록 (최적화 — 공통 등록기 사용)
        }

        private static void OnSceneLoaded(Scene scene) // Lobby 씬 로드 완료 처리
        {
            EnsureShopButton(); // 상점 버튼 생성 및 연결
        }

        private static void EnsureShopButton() // Lobby 상점 버튼 보장
        {
            LobbyScreenController lobby = UnityEngine.Object.FindFirstObjectByType<LobbyScreenController>(); // Lobby 컨트롤러 조회

            if (lobby == null) // Lobby 컨트롤러 존재 확인
            {
                return; // 상점 버튼 생성 중단
            }

            Transform navigationRoot = lobby.transform.Find("BottomNavigation"); // 하단 내비게이션 조회

            if (navigationRoot == null) // 하단 내비게이션 존재 확인
            {
                Debug.LogWarning("[Project H] Lobby BottomNavigation was not found for Shop button."); // 내비게이션 누락 로그
                return; // 상점 버튼 생성 중단
            }

            Transform existing = navigationRoot.Find(ShopButtonName); // 기존 상점 버튼 조회

            if (existing != null) // 기존 상점 버튼 존재 확인
            {
                Button existingButton = existing.GetComponent<Button>(); // 기존 버튼 컴포넌트 조회

                if (existingButton != null) // 기존 버튼 유효성 확인
                {
                    existingButton.onClick.RemoveListener(GoShop); // 기존 상점 이벤트 제거
                    existingButton.onClick.AddListener(GoShop); // 상점 이동 이벤트 재연결
                }

                NormalizeBottomNavigation(navigationRoot); // 하단 버튼 균등 배치
                return; // 기존 버튼 처리 완료
            }

            Button template = FindTemplateButton(navigationRoot); // 기존 하단 버튼 템플릿 조회
            GameObject buttonObject = new GameObject(ShopButtonName, typeof(RectTransform), typeof(Image), typeof(Button)); // 상점 버튼 객체 생성
            buttonObject.transform.SetParent(navigationRoot, false); // 하단 내비게이션 부모 연결
            Image image = buttonObject.GetComponent<Image>(); // 버튼 이미지 조회
            Button button = buttonObject.GetComponent<Button>(); // 버튼 컴포넌트 조회

            if (template != null) // 템플릿 버튼 존재 확인
            {
                Image templateImage = template.GetComponent<Image>(); // 템플릿 이미지 조회

                if (templateImage != null) // 템플릿 이미지 존재 확인
                {
                    image.sprite = templateImage.sprite; // 템플릿 스프라이트 복사
                    image.type = templateImage.type; // 템플릿 이미지 유형 복사
                    image.color = templateImage.color; // 템플릿 색상 복사
                }

                button.colors = template.colors; // 템플릿 버튼 색상 전환 복사
                button.transition = template.transition; // 템플릿 버튼 전환 방식 복사
            }
            else // 템플릿 없음 처리
            {
                image.color = new Color(0.90f, 0.95f, 1f, 1f); // 기본 버튼 색상 적용
            }

            button.targetGraphic = image; // 버튼 대상 그래픽 연결
            button.onClick.AddListener(GoShop); // 상점 이동 이벤트 연결
            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text)); // 상점 버튼 라벨 생성
            labelObject.transform.SetParent(buttonObject.transform, false); // 버튼 라벨 부모 연결
            Text label = labelObject.GetComponent<Text>(); // 버튼 라벨 조회
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 적용
            label.text = "상점"; // 상점 버튼 문구 설정
            label.fontSize = 21; // 상점 버튼 글자 크기 설정
            label.fontStyle = FontStyle.Bold; // 상점 버튼 굵기 설정
            label.color = new Color(0.18f, 0.27f, 0.40f, 1f); // 상점 버튼 글자색 설정
            label.alignment = TextAnchor.MiddleCenter; // 상점 버튼 중앙 정렬
            label.resizeTextForBestFit = true; // 글자 자동 크기 활성화
            label.resizeTextMinSize = 12; // 최소 글자 크기 설정
            label.resizeTextMaxSize = 21; // 최대 글자 크기 설정
            label.raycastTarget = false; // 라벨 입력 비활성화
            RectTransform labelRect = labelObject.GetComponent<RectTransform>(); // 라벨 RectTransform 조회
            labelRect.anchorMin = Vector2.zero; // 라벨 최소 앵커 설정
            labelRect.anchorMax = Vector2.one; // 라벨 최대 앵커 설정
            labelRect.offsetMin = new Vector2(8f, 8f); // 라벨 최소 여백 설정
            labelRect.offsetMax = new Vector2(-8f, -8f); // 라벨 최대 여백 설정
            NormalizeBottomNavigation(navigationRoot); // 하단 버튼 균등 배치
        }

        private static Button FindTemplateButton(Transform navigationRoot) // 하단 버튼 템플릿 조회
        {
            for (int index = 0; index < navigationRoot.childCount; index++) // 하단 자식 순회
            {
                Button button = navigationRoot.GetChild(index).GetComponent<Button>(); // 현재 버튼 조회

                if (button != null) // 버튼 존재 확인
                {
                    return button; // 첫 버튼 템플릿 반환
                }
            }

            return null; // 템플릿 없음 반환
        }

        private static void NormalizeBottomNavigation(Transform navigationRoot) // 하단 버튼 균등 배치
        {
            System.Collections.Generic.List<Button> buttons = new System.Collections.Generic.List<Button>(); // 활성 버튼 목록 생성

            for (int index = 0; index < navigationRoot.childCount; index++) // 하단 자식 순회
            {
                Transform child = navigationRoot.GetChild(index); // 현재 자식 조회
                Button button = child.GetComponent<Button>(); // 현재 버튼 조회

                if (button != null && child.gameObject.activeSelf) // 활성 버튼 여부 확인
                {
                    buttons.Add(button); // 배치 대상 추가
                }
            }

            if (buttons.Count == 0) // 버튼 개수 확인
            {
                return; // 배치 중단
            }

            const float margin = 0.02f; // 좌우 여백 설정
            const float gap = 0.012f; // 버튼 사이 간격 설정
            float usableWidth = 1f - (margin * 2f) - (gap * (buttons.Count - 1)); // 사용 가능 너비 계산
            float buttonWidth = usableWidth / buttons.Count; // 버튼별 너비 계산

            for (int index = 0; index < buttons.Count; index++) // 버튼 목록 순회
            {
                RectTransform rect = buttons[index].GetComponent<RectTransform>(); // 현재 버튼 RectTransform 조회
                float minX = margin + (index * (buttonWidth + gap)); // 현재 버튼 최소 X 계산
                rect.anchorMin = new Vector2(minX, 0.12f); // 버튼 최소 앵커 적용
                rect.anchorMax = new Vector2(minX + buttonWidth, 0.88f); // 버튼 최대 앵커 적용
                rect.offsetMin = Vector2.zero; // 최소 오프셋 초기화
                rect.offsetMax = Vector2.zero; // 최대 오프셋 초기화
            }
        }

        private static void GoShop() // 상점 씬 이동
        {
            if (GameManager.Instance == null || GameManager.Instance.Scenes == null) // 씬 로더 확인
            {
                Debug.LogWarning("[Project H] SceneLoader was not found for Shop."); // 씬 로더 누락 로그
                return; // 상점 이동 중단
            }

            GameManager.Instance.Scenes.LoadScene(GameScenes.Shop); // 상점 씬 로드
        }
    }
}
