using ProjectH.Core; // 게임 관리자 및 씬 이름 기능
using ProjectH.SaveSystem; // 저장 및 시간 진행 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // 씬 이벤트 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class LobbyTimeAdvanceRuntimePatch // Lobby 시간 진행 테스트 버튼 Runtime 패치
    {
        private const string TimeButtonName = "TimeAdvanceButton"; // 시간 진행 버튼 객체 이름
        private static bool registered; // 씬 이벤트 등록 상태

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // Runtime 로비 패치 등록
        private static void Register() // 씬 로드 이벤트 등록
        {
            if (registered) // 기존 등록 상태 확인
            {
                return; // 중복 등록 차단
            }

            registered = true; // 등록 상태 저장
            SceneManager.sceneLoaded += OnSceneLoaded; // 씬 로드 완료 이벤트 연결
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) // Lobby 씬 로드 완료 처리
        {
            if (!string.Equals(scene.name, GameScenes.Lobby, System.StringComparison.Ordinal)) // Lobby 씬 여부 확인
            {
                return; // 다른 씬 제외
            }

            EnsureTimeButton(); // 시간 진행 버튼 생성 및 연결
        }

        private static void EnsureTimeButton() // Lobby 시간 진행 버튼 보장
        {
            LobbyScreenController lobby = UnityEngine.Object.FindFirstObjectByType<LobbyScreenController>(); // Lobby 컨트롤러 조회

            if (lobby == null) // Lobby 컨트롤러 존재 확인
            {
                return; // 시간 진행 버튼 생성 중단
            }

            Transform topBar = lobby.transform.Find("TopBar"); // 상단바 루트 조회

            if (topBar == null) // 상단바 존재 확인
            {
                Debug.LogWarning("[Project H] Lobby TopBar was not found for time button."); // 상단바 누락 로그
                return; // 시간 진행 버튼 생성 중단
            }

            Transform existing = topBar.Find(TimeButtonName); // 기존 시간 진행 버튼 조회

            if (existing != null) // 기존 시간 진행 버튼 존재 확인
            {
                Button existingButton = existing.GetComponent<Button>(); // 기존 버튼 컴포넌트 조회

                if (existingButton != null) // 기존 버튼 유효성 확인
                {
                    existingButton.onClick.RemoveListener(AdvanceTime); // 기존 시간 진행 이벤트 제거
                    existingButton.onClick.AddListener(AdvanceTime); // 시간 진행 이벤트 재연결
                }

                return; // 기존 버튼 처리 완료
            }

            Button template = topBar.GetComponentInChildren<Button>(true); // 상단바 버튼 템플릿 조회
            GameObject buttonObject = new GameObject(TimeButtonName, typeof(RectTransform), typeof(Image), typeof(Button)); // 시간 진행 버튼 객체 생성
            buttonObject.transform.SetParent(topBar, false); // 상단바 부모 연결
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

                button.colors = template.colors; // 템플릿 버튼 전환 색상 복사
                button.transition = template.transition; // 템플릿 버튼 전환 방식 복사
            }
            else // 템플릿 없음 처리
            {
                image.color = new Color(0.90f, 0.95f, 1f, 1f); // 기본 버튼 색상 적용
            }

            button.targetGraphic = image; // 버튼 대상 그래픽 연결
            button.onClick.AddListener(AdvanceTime); // 시간 진행 이벤트 연결
            RectTransform rect = buttonObject.GetComponent<RectTransform>(); // 버튼 RectTransform 조회
            rect.anchorMin = new Vector2(0.88f, 0.18f); // 상단바 우측 빈 공간 최소 앵커 적용
            rect.anchorMax = new Vector2(1f, 0.82f); // 상단바 우측 빈 공간 최대 앵커 적용
            rect.offsetMin = Vector2.zero; // 버튼 최소 오프셋 초기화
            rect.offsetMax = Vector2.zero; // 버튼 최대 오프셋 초기화

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text)); // 버튼 라벨 객체 생성
            labelObject.transform.SetParent(buttonObject.transform, false); // 라벨 부모 연결
            Text label = labelObject.GetComponent<Text>(); // 라벨 컴포넌트 조회
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 적용
            label.text = "시간 진행"; // 시간 진행 버튼 문구 설정
            label.fontSize = 14; // 버튼 글자 크기 설정
            label.fontStyle = FontStyle.Bold; // 버튼 글자 굵기 설정
            label.color = new Color(0.18f, 0.27f, 0.40f, 1f); // 버튼 글자 색상 설정
            label.alignment = TextAnchor.MiddleCenter; // 버튼 글자 중앙 정렬
            label.resizeTextForBestFit = true; // 글자 자동 크기 활성화
            label.resizeTextMinSize = 8; // 최소 글자 크기 설정
            label.resizeTextMaxSize = 14; // 최대 글자 크기 설정
            label.raycastTarget = false; // 라벨 입력 비활성화
            RectTransform labelRect = labelObject.GetComponent<RectTransform>(); // 라벨 RectTransform 조회
            labelRect.anchorMin = Vector2.zero; // 라벨 최소 앵커 설정
            labelRect.anchorMax = Vector2.one; // 라벨 최대 앵커 설정
            labelRect.offsetMin = new Vector2(4f, 4f); // 라벨 최소 여백 설정
            labelRect.offsetMax = new Vector2(-4f, -4f); // 라벨 최대 여백 설정
        }

        private static void AdvanceTime() // 시간 진행 버튼 클릭 처리
        {
            if (GameManager.Instance == null || GameManager.Instance.Save == null) // 저장 관리자 확인
            {
                Debug.LogWarning("[Project H] SaveManager was not found for time advance."); // 저장 관리자 누락 로그
                return; // 시간 진행 중단
            }

            SaveManager saveManager = GameManager.Instance.Save; // 저장 관리자 조회
            SaveData saveData = saveManager.CurrentSave; // 현재 저장 데이터 조회

            if (saveData == null) // 현재 저장 데이터 확인
            {
                Debug.LogWarning("[Project H] Current save data is empty for time advance."); // 저장 데이터 누락 로그
                return; // 시간 진행 중단
            }

            GameTimeService.AdvanceTime(saveData); // 시간대 한 단계 진행
            saveManager.SaveCurrent(); // 진행 결과 저장

            LobbyScreenController lobby = UnityEngine.Object.FindFirstObjectByType<LobbyScreenController>(); // Lobby 컨트롤러 조회

            if (lobby != null) // Lobby 컨트롤러 존재 확인
            {
                lobby.Refresh(); // 로비 화면 즉시 갱신
            }

            LobbyVitalityRuntimePatch.RefreshDisplay(); // 다음 일차 진입 시 활력 라벨 즉시 갱신 (Day42)
        }
    }
}
