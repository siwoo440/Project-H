using ProjectH.Core; // 공통 씬 이름·게임 관리자 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // 씬 이벤트 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class DiarySceneRuntimePatch // 일기장 씬 주입 + 타이틀 [일기장] 버튼 (Day63 신규, Day72 — 로비 버튼은 ESC 메뉴로 이동)
    {
        private const string ControllerName = "DiaryScreenRuntime"; // 일기장 화면 이름
        private const string CameraName = "DiaryCamera"; // 일기장 카메라 이름
        private const string TitleButtonName = "DiaryButton"; // 타이틀 일기장 버튼 이름

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // Runtime 씬 패치 등록
        private static void Register() // 씬 로드 이벤트 등록
        {
            SceneRuntimePatch.Register(GameScenes.Diary, OnDiaryLoaded); // 일기장 씬
            SceneRuntimePatch.Register(GameScenes.Title, OnTitleLoaded); // 타이틀 [일기장]
            // 로비 [일기장] 버튼은 Day72에 ESC 메뉴로 옮겨 등록하지 않는다
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
            if (continueObject == null) return; // 구조 확인

            if (continueObject.transform.parent.Find(TitleButtonName) != null) // 이미 추가됨
            {
                TitleMenuRuntimePatch.RelayoutMenu(); // 배치만 다시 맞춤
                return; // 중복 생성 방지
            }
            GameObject diary = Object.Instantiate(continueObject, continueObject.transform.parent); // 같은 모양으로 복제
            diary.name = TitleButtonName; // 이름
            Button button = diary.GetComponent<Button>(); // 버튼
            button.onClick = new Button.ButtonClickedEvent(); // 복제된 [이어하기] 동작 제거
            button.onClick.AddListener(() => OpenDiary(GameScenes.Title)); // 일기장 열기
            Text label = diary.GetComponentInChildren<Text>(true); // 글자
            if (label != null) label.text = "일기장"; // 글자 교체
            bool hasSave = GameManager.Instance != null && GameManager.Instance.Save != null && GameManager.Instance.Save.HasSaveData; // 저장 있음
            button.interactable = hasSave; // 저장이 있어야 열람 가능
            TitleMenuRuntimePatch.RelayoutMenu(); // 메뉴 세로 배치 정리 (Day72 — 설정 버튼과 순서가 섞여도 간격이 일정하다)
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
