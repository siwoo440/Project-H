using ProjectH.Core; // 게임 관리자 및 씬 이름 기능
using ProjectH.SaveSystem; // 저장 및 활력 진행 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // 씬 이벤트 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class LobbyVitalityRuntimePatch // Lobby 활력 테스트 UI Runtime 패치 (Day42)
    {
        private const string PanelName = "VitalityTestPanel"; // 활력 테스트 패널 객체 이름
        private const string LabelName = "VitalityLabel"; // 활력 표시 라벨 이름
        private const string MinusButtonName = "VitalityMinusButton"; // 활력 감소 버튼 이름
        private const string PlusButtonName = "VitalityPlusButton"; // 활력 증가 버튼 이름
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

            EnsureVitalityPanel(); // 활력 테스트 패널 생성 및 연결
        }

        private static void EnsureVitalityPanel() // Lobby 활력 테스트 패널 보장
        {
            LobbyScreenController lobby = UnityEngine.Object.FindFirstObjectByType<LobbyScreenController>(); // Lobby 컨트롤러 조회

            if (lobby == null) // Lobby 컨트롤러 존재 확인
            {
                return; // 활력 패널 생성 중단
            }

            Transform existingPanel = lobby.transform.Find(PanelName); // 기존 활력 패널 조회

            if (existingPanel != null) // 기존 활력 패널 존재 확인
            {
                RebindButton(existingPanel, MinusButtonName, () => ApplyDelta(-1)); // 감소 버튼 이벤트 재연결
                RebindButton(existingPanel, PlusButtonName, () => ApplyDelta(1)); // 증가 버튼 이벤트 재연결
                RefreshLabel(existingPanel); // 활력 라벨 갱신
                return; // 기존 패널 처리 완료
            }

            GameObject panelObject = new GameObject(PanelName, typeof(RectTransform)); // 활력 패널 객체 생성
            panelObject.transform.SetParent(lobby.transform, false); // Lobby 루트 부모 연결
            RectTransform panelRect = panelObject.GetComponent<RectTransform>(); // 패널 RectTransform 조회
            panelRect.anchorMin = new Vector2(0.52f, 0.745f); // 패널 최소 앵커 적용 (PartySummary와 Title/Save 사이 빈 공간, 초상화 프레임과 겹치지 않음)
            panelRect.anchorMax = new Vector2(0.705f, 0.785f); // 패널 최대 앵커 적용 (Gold/Crystal 칩과 동일한 컴팩트 크기)
            panelRect.offsetMin = Vector2.zero; // 패널 최소 오프셋 초기화
            panelRect.offsetMax = Vector2.zero; // 패널 최대 오프셋 초기화

            Image background = panelObject.AddComponent<Image>(); // 활력 패널 배경 이미지 추가
            Image chipTemplate = lobby.transform.Find("TopBar/GoldChip")?.GetComponent<Image>(); // GoldChip 스프라이트 템플릿 조회 (디자인 통일)

            if (chipTemplate != null) // GoldChip 템플릿 존재 확인
            {
                background.sprite = chipTemplate.sprite; // GoldChip 스프라이트 복사
                background.type = chipTemplate.type; // GoldChip 이미지 유형 복사
                background.color = new Color(1f, 1f, 1f, 0.95f); // 활력 칩 배경 색상 적용 (Gold/Crystal과 동일 톤)
            }
            else // 템플릿 없음 처리
            {
                background.color = new Color(0.90f, 0.95f, 1f, 0.95f); // 기본 활력 패널 배경 색상 적용
            }

            background.raycastTarget = false; // 활력 패널 배경 입력 비활성화

            CreateLabel(panelObject.transform); // 활력 표시 라벨 생성
            CreateButton(panelObject.transform, MinusButtonName, "-1", new Vector2(0.60f, 0.12f), new Vector2(0.795f, 0.88f), () => ApplyDelta(-1)); // 활력 감소 버튼 생성
            CreateButton(panelObject.transform, PlusButtonName, "+1", new Vector2(0.805f, 0.12f), new Vector2(1f, 0.88f), () => ApplyDelta(1)); // 활력 증가 버튼 생성
            RefreshLabel(panelObject.transform); // 활력 라벨 초기값 표시
        }

        private static void CreateLabel(Transform parent) // 활력 표시 라벨 생성
        {
            GameObject labelObject = new GameObject(LabelName, typeof(RectTransform), typeof(Text)); // 라벨 객체 생성
            labelObject.transform.SetParent(parent, false); // 패널 부모 연결
            Text label = labelObject.GetComponent<Text>(); // 라벨 컴포넌트 조회
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 적용
            label.fontSize = 14; // 라벨 글자 크기 설정
            label.fontStyle = FontStyle.Bold; // 라벨 글자 굵기 설정
            label.color = new Color(0.18f, 0.27f, 0.40f, 1f); // 라벨 글자 색상 설정 (Gold/Crystal 칩과 동일한 남색)
            label.alignment = TextAnchor.MiddleLeft; // 라벨 좌측 정렬
            label.resizeTextForBestFit = true; // 글자 자동 크기 활성화
            label.resizeTextMinSize = 7; // 최소 글자 크기 설정
            label.resizeTextMaxSize = 14; // 최대 글자 크기 설정
            label.raycastTarget = false; // 라벨 입력 비활성화
            RectTransform labelRect = labelObject.GetComponent<RectTransform>(); // 라벨 RectTransform 조회
            labelRect.anchorMin = new Vector2(0f, 0f); // 라벨 최소 앵커 설정
            labelRect.anchorMax = new Vector2(0.58f, 1f); // 라벨 최대 앵커 설정
            labelRect.offsetMin = new Vector2(6f, 0f); // 라벨 좌측 내부 여백 적용
            labelRect.offsetMax = Vector2.zero; // 라벨 최대 오프셋 초기화
        }

        private static void CreateButton(Transform parent, string buttonName, string labelText, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction onClick) // 활력 버튼 공통 생성
        {
            GameObject buttonObject = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button)); // 버튼 객체 생성
            buttonObject.transform.SetParent(parent, false); // 패널 부모 연결
            Image image = buttonObject.GetComponent<Image>(); // 버튼 이미지 조회
            image.color = new Color(0.90f, 0.95f, 1f, 1f); // 기본 버튼 색상 적용
            Button button = buttonObject.GetComponent<Button>(); // 버튼 컴포넌트 조회
            button.targetGraphic = image; // 버튼 대상 그래픽 연결
            button.onClick.AddListener(onClick); // 버튼 클릭 이벤트 연결
            RectTransform rect = buttonObject.GetComponent<RectTransform>(); // 버튼 RectTransform 조회
            rect.anchorMin = anchorMin; // 버튼 최소 앵커 적용
            rect.anchorMax = anchorMax; // 버튼 최대 앵커 적용
            rect.offsetMin = Vector2.zero; // 버튼 최소 오프셋 초기화
            rect.offsetMax = Vector2.zero; // 버튼 최대 오프셋 초기화

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text)); // 버튼 라벨 객체 생성
            labelObject.transform.SetParent(buttonObject.transform, false); // 버튼 라벨 부모 연결
            Text label = labelObject.GetComponent<Text>(); // 버튼 라벨 컴포넌트 조회
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 적용
            label.text = labelText; // 버튼 라벨 문구 설정
            label.fontSize = 14; // 버튼 라벨 글자 크기 설정
            label.fontStyle = FontStyle.Bold; // 버튼 라벨 글자 굵기 설정
            label.color = new Color(0.18f, 0.27f, 0.40f, 1f); // 버튼 라벨 글자 색상 설정
            label.alignment = TextAnchor.MiddleCenter; // 버튼 라벨 중앙 정렬
            label.raycastTarget = false; // 버튼 라벨 입력 비활성화
            RectTransform labelRect = labelObject.GetComponent<RectTransform>(); // 버튼 라벨 RectTransform 조회
            labelRect.anchorMin = Vector2.zero; // 버튼 라벨 최소 앵커 설정
            labelRect.anchorMax = Vector2.one; // 버튼 라벨 최대 앵커 설정
            labelRect.offsetMin = Vector2.zero; // 버튼 라벨 최소 오프셋 초기화
            labelRect.offsetMax = Vector2.zero; // 버튼 라벨 최대 오프셋 초기화
        }

        private static void RebindButton(Transform panel, string buttonName, UnityEngine.Events.UnityAction onClick) // 기존 버튼 이벤트 재연결
        {
            Transform buttonTransform = panel.Find(buttonName); // 대상 버튼 조회
            Button button = buttonTransform == null ? null : buttonTransform.GetComponent<Button>(); // 버튼 컴포넌트 조회

            if (button == null) // 버튼 존재 확인
            {
                return; // 재연결 중단
            }

            button.onClick.RemoveAllListeners(); // 기존 클릭 이벤트 전체 제거
            button.onClick.AddListener(onClick); // 클릭 이벤트 재연결
        }

        public static void RefreshDisplay() // 외부(시간 진행 등)에서 활력 라벨 갱신 요청 처리
        {
            LobbyScreenController lobby = UnityEngine.Object.FindFirstObjectByType<LobbyScreenController>(); // Lobby 컨트롤러 조회
            Transform panel = lobby == null ? null : lobby.transform.Find(PanelName); // 활력 패널 조회
            RefreshLabel(panel); // 활력 라벨 갱신
        }

        private static void ApplyDelta(int amount) // 활력 테스트 증감 처리
        {
            if (GameManager.Instance == null || GameManager.Instance.Save == null) // 저장 관리자 확인
            {
                Debug.LogWarning("[Project H] SaveManager was not found for vitality test."); // 저장 관리자 누락 로그
                return; // 활력 변경 중단
            }

            SaveManager saveManager = GameManager.Instance.Save; // 저장 관리자 조회
            SaveData saveData = saveManager.CurrentSave; // 현재 저장 데이터 조회

            if (saveData == null) // 현재 저장 데이터 확인
            {
                Debug.LogWarning("[Project H] Current save data is empty for vitality test."); // 저장 데이터 누락 로그
                return; // 활력 변경 중단
            }

            if (amount > 0) // 증가 요청 확인
            {
                VitalityService.AddVitality(saveData, amount); // 활력 증가 처리
            }
            else // 감소 요청 처리
            {
                VitalityService.TrySpendVitality(saveData, -amount, out _); // 활력 감소 처리 (부족 시 실패 무시하고 유지)
            }

            saveManager.SaveCurrent(); // 변경 결과 저장

            LobbyScreenController lobby = UnityEngine.Object.FindFirstObjectByType<LobbyScreenController>(); // Lobby 컨트롤러 조회
            Transform panel = lobby == null ? null : lobby.transform.Find(PanelName); // 활력 패널 조회
            RefreshLabel(panel); // 활력 라벨 갱신
        }

        private static void RefreshLabel(Transform panel) // 활력 라벨 문구 갱신
        {
            if (panel == null) // 패널 존재 확인
            {
                return; // 라벨 갱신 중단
            }

            Transform labelTransform = panel.Find(LabelName); // 라벨 오브젝트 조회
            Text label = labelTransform == null ? null : labelTransform.GetComponent<Text>(); // 라벨 컴포넌트 조회

            if (label == null) // 라벨 존재 확인
            {
                return; // 라벨 갱신 중단
            }

            if (GameManager.Instance == null || GameManager.Instance.Save == null || GameManager.Instance.Save.CurrentSave == null) // 저장 데이터 확인
            {
                label.text = "VIT -"; // 저장 데이터 없음 표시
                return; // 라벨 갱신 종료
            }

            int vitality = VitalityService.GetVitality(GameManager.Instance.Save.CurrentSave); // 현재 활력 조회
            label.text = $"VIT {vitality}/{SaveData.MaxVitality}"; // 활력 문구 표시
        }
    }
}
