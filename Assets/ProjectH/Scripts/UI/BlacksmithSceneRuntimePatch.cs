using ProjectH.Core; // 공통 씬 이름 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // 씬 이벤트 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class BlacksmithSceneRuntimePatch // 대장간 씬 Runtime 연결 패치 (Day61 신규 — 빈 씬에 카메라·화면 주입)
    {
        private const string ControllerName = "BlacksmithScreenRuntime"; // 대장간 화면 컨트롤러 이름
        private const string CameraName = "BlacksmithCamera"; // 대장간 카메라 이름

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // Runtime 씬 패치 등록
        private static void Register() // 씬 로드 이벤트 등록
        {
            SceneRuntimePatch.Register(GameScenes.Blacksmith, OnSceneLoaded); // Blacksmith 씬 로드 시 주입 등록
        }

        private static void OnSceneLoaded(Scene scene) // 씬 로드 완료 처리
        {
            if (Object.FindFirstObjectByType<Camera>() == null) // 기존 카메라 확인
            {
                GameObject cameraObject = new GameObject(CameraName, typeof(Camera)); // 카메라 생성
                cameraObject.tag = "MainCamera"; // 메인 카메라 태그
                cameraObject.transform.position = new Vector3(0f, 0f, -10f); // 카메라 위치
                Camera camera = cameraObject.GetComponent<Camera>(); // 카메라 조회
                camera.clearFlags = CameraClearFlags.SolidColor; // 단색 배경
                camera.backgroundColor = new Color(0.08f, 0.06f, 0.06f, 1f); // 화로 어두운 배경
                camera.orthographic = true; // 직교 카메라
            }

            if (GameObject.Find(ControllerName) == null) // 중복 화면 확인
            {
                new GameObject(ControllerName, typeof(BlacksmithScreenController)); // 대장간 화면 생성
            }
        }
    }
}
