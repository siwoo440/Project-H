using ProjectH.Core; // 공통 씬 이름 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // 씬 이벤트 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class VillageSceneRuntimePatch // 마을 씬 Runtime 연결 패치 (Day62 신규 — 빈 씬에 카메라·화면 주입, Unity 재등록을 위해 메타 재생성)
    {
        private const string ControllerName = "VillageScreenRuntime"; // 마을 화면 컨트롤러 이름
        private const string CameraName = "VillageCamera"; // 마을 카메라 이름

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // Runtime 씬 패치 등록
        private static void Register() // 씬 로드 이벤트 등록
        {
            SceneRuntimePatch.Register(GameScenes.Village, OnSceneLoaded); // Village 씬 로드 시 주입 등록
        }

        private static void OnSceneLoaded(Scene scene) // 씬 로드 완료 처리
        {
            EnsureCamera(); // 카메라 보장
            EnsureScreen(); // 마을 화면 보장
        }

        private static void EnsureCamera() // 켜진 카메라가 없으면 생성 (꺼진 카메라 컴포넌트만 남아 있어도 "No cameras rendering"이 되지 않게)
        {
            if (Camera.allCamerasCount > 0) // 켜진 카메라 확인 (allCamerasCount는 활성·사용 중인 카메라만 셈)
            {
                return; // 기존 카메라 유지
            }

            GameObject cameraObject = new GameObject(CameraName, typeof(Camera)); // 카메라 생성
            cameraObject.tag = "MainCamera"; // 메인 카메라 태그
            cameraObject.transform.position = new Vector3(0f, 0f, -10f); // 카메라 위치
            Camera camera = cameraObject.GetComponent<Camera>(); // 카메라 조회
            camera.clearFlags = CameraClearFlags.SolidColor; // 단색 배경
            camera.backgroundColor = new Color(0.52f, 0.66f, 0.50f, 1f); // 들판 초록 배경
            camera.orthographic = true; // 직교 카메라
        }

        private static void EnsureScreen() // 마을 화면 보장
        {
            if (GameObject.Find(ControllerName) != null) // 중복 화면 확인
            {
                return; // 중복 생성 차단
            }

            new GameObject(ControllerName, typeof(VillageScreenController)); // 마을 화면 생성
        }
    }
}
