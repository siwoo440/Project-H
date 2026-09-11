using ProjectH.Core; // 공통 씬 이름·게임 관리자 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // 씬 이벤트 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class DiarySceneRuntimePatch // 일기장 씬 주입 + 타이틀·로비 [일기장] 버튼 (Day63 신규)
    {
        private const string ControllerName = "DiaryScreenRuntime"; // 일기장 화면 이름
        private const string CameraName = "DiaryCamera"; // 일기장 카메라 이름
        private const string TitleButtonName = "DiaryButton"; // 타이틀 일기장 버튼 이름
        private const string LobbyButtonName = "LobbyDiaryButton"; // 로비 일기장 버튼 이름

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // Runtime 씬 패치 등록
        private static void Register() // 씬 로드 이벤트 등록
        {
            SceneRuntimePatch.Register(GameScenes.Diary, OnDiaryLoaded); // 일기장 씬
            SceneRuntimePatch.Register(GameScenes.Title, OnTitleLoaded); // 타이틀 [일기장]
            SceneRuntimePatch.Register(GameScenes.Lobby, OnLobbyLoaded); // 로비 [일기장]
        }

        private static void OnDiaryLoaded(Scene scene) // 일기장 씬 : 카메라·화면 주입
        {
            if (Camera.allCamerasCount == 0) // 켜진 카메라 확인
            {
                GameObject cameraObject = new GameObject(CameraName, typeof(Camera)); // 카메라 생성
                cameraObject.tag = "MainCamera"; // 메인 카메라 태그
                cameraObject.transform.position = new Vector3(0f, 0f, -10f); // 위치
                Camera camera = cameraObject.GetComponent<Camera>(); // 카메라
                camera.clearFlags = CameraClearFlags.SolidColor; // 단색 배경
                camera.backgroundColor = new Color(0.22f, 0.14f, 0.10f, 1f); // 가죽색
                camera.orthographic = true; // 직교
            }

            if (GameObject.Find(ControllerName) == null) new GameObject(ControllerName, typeof(DiaryScreenController)); // 일기장 화면
        }

        private static void OnTitleLoaded(Scene scene) // 타이틀 : [이어하기] 버튼을 복제해 [종료] 위에 [일기장] 추가
        {
            GameObject continueObject = GameObject.Find("ContinueButton"); // 이어하기 버튼
            GameObject quitObject = GameObject.Find("QuitButton"); // 종료 버튼
            if (continueObject == null || quitObject == null || continueObject.transform.parent.Find(TitleButtonName) != null) return; // 구조 확인·중복 방지
            GameObject diary = Object.Instantiate(continueObject, continueObject.transform.parent); // 같은 모양으로 복제
            diary.name = TitleButtonName; // 이름
            Button button = diary.GetComponent<Button>(); // 버튼
            button.onClick = new Button.ButtonClickedEvent(); // 복제된 [이어하기] 동작 제거
            button.onClick.AddListener(() => OpenDiary(GameScenes.Title)); // 일기장 열기
            Text label = diary.GetComponentInChildren<Text>(true); // 글자
            if (label != null) label.text = "일기장"; // 글자 교체
            bool hasSave = GameManager.Instance != null && GameManager.Instance.Save != null && GameManager.Instance.Save.HasSaveData; // 저장 있음
            button.interactable = hasSave; // 저장이 있어야 열람 가능
            diary.transform.SetSiblingIndex(quitObject.transform.GetSiblingIndex()); // 종료 버튼 바로 위 칸
            PlaceForManualLayout(continueObject, quitObject, diary); // 레이아웃 그룹이 없으면 직접 배치
        }

        private static void PlaceForManualLayout(GameObject continueObject, GameObject quitObject, GameObject diary) // 수동 배치 메뉴일 때 [일기장]을 종료 자리로, 종료는 한 칸 아래로
        {
            if (diary.transform.parent.GetComponent<LayoutGroup>() != null) return; // 레이아웃 그룹이 알아서 배치
            RectTransform continueRect = (RectTransform)continueObject.transform; // 이어하기 영역
            RectTransform quitRect = (RectTransform)quitObject.transform; // 종료 영역
            float step = continueRect.anchoredPosition.y - quitRect.anchoredPosition.y; // 한 칸 간격
            ((RectTransform)diary.transform).anchoredPosition = quitRect.anchoredPosition; // 일기장 = 종료 자리
            quitRect.anchoredPosition -= new Vector2(0f, step); // 종료 한 칸 아래
        }

        private static void OnLobbyLoaded(Scene scene) // 로비 : 상단 [타이틀] 버튼 왼쪽에 [일기장]
        {
            GameObject titleObject = GameObject.Find("TitleButton"); // 로비 타이틀 버튼
            if (titleObject == null || titleObject.transform.parent.Find(LobbyButtonName) != null) return; // 구조 확인·중복 방지
            GameObject diary = Object.Instantiate(titleObject, titleObject.transform.parent); // 같은 모양으로 복제
            diary.name = LobbyButtonName; // 이름
            Button button = diary.GetComponent<Button>(); // 버튼
            button.onClick = new Button.ButtonClickedEvent(); // 복제된 [타이틀] 동작 제거
            button.onClick.AddListener(() => OpenDiary(GameScenes.Lobby)); // 일기장 열기
            Text label = diary.GetComponentInChildren<Text>(true); // 글자
            if (label != null) label.text = "일기장"; // 글자 교체
            RectTransform source = (RectTransform)titleObject.transform; // 타이틀 버튼 영역
            RectTransform rect = (RectTransform)diary.transform; // 일기장 영역
            float width = (source.anchorMax.x - source.anchorMin.x) + 0.012f; // 버튼 너비 + 간격 (앵커 기준)
            rect.anchorMin = source.anchorMin - new Vector2(width, 0f); // 왼쪽 한 칸
            rect.anchorMax = source.anchorMax - new Vector2(width, 0f); // 왼쪽 한 칸
            if (width <= 0.013f) rect.anchoredPosition = source.anchoredPosition - new Vector2(source.rect.width + 16f, 0f); // 앵커가 점이면 픽셀로 이동
        }

        public static void OpenDiary(string returnScene) // 일기장 열기 (타이틀에서는 저장을 먼저 불러옴)
        {
            if (GameManager.Instance == null || GameManager.Instance.Scenes == null || GameManager.Instance.Save == null) return; // 관리자 확인
            if (GameManager.Instance.Save.CurrentSave == null && !GameManager.Instance.Save.LoadCurrent()) return; // 타이틀 : 저장 불러오기
            DiaryScreenController.ReturnScene = returnScene; // 닫으면 돌아갈 곳
            GameManager.Instance.Scenes.LoadScene(GameScenes.Diary); // 일기장 씬
        }
    }
}
