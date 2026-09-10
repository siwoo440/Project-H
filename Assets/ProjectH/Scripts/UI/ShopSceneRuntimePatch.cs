using ProjectH.Core; // 공통 씬 이름 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // 씬 이벤트 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class ShopSceneRuntimePatch // 상점 씬 Runtime 연결 패치
    {
        private const string ShopControllerName = "ShopScreenRuntime"; // 상점 화면 컨트롤러 이름
        private const string ShopCameraName = "ShopCamera"; // 상점 화면 카메라 이름
        private static bool registered; // 씬 이벤트 등록 상태

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // Runtime 씬 패치 등록
        private static void Register() // 씬 로드 이벤트 등록
        {
            if (registered) // 기존 등록 상태 확인
            {
                return; // 중복 등록 차단
            }

            registered = true; // 등록 상태 저장
            SceneManager.sceneLoaded += OnSceneLoaded; // 씬 로드 완료 이벤트 연결
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) // 씬 로드 완료 처리
        {
            if (!string.Equals(scene.name, GameScenes.Shop, System.StringComparison.Ordinal)) // 상점 씬 여부 확인
            {
                return; // 다른 씬 제외
            }

            EnsureShopCamera(); // 상점 카메라 보장
            EnsureShopScreen(); // 상점 화면 보장
        }

        private static void EnsureShopCamera() // 상점 기본 카메라 보장
        {
            if (UnityEngine.Object.FindFirstObjectByType<Camera>() != null) // 기존 카메라 확인
            {
                return; // 기존 카메라 유지
            }

            GameObject cameraObject = new GameObject(ShopCameraName, typeof(Camera)); // 상점 카메라 생성
            cameraObject.tag = "MainCamera"; // 메인 카메라 태그 설정
            cameraObject.transform.position = new Vector3(0f, 0f, -10f); // 카메라 위치 설정
            Camera shopCamera = cameraObject.GetComponent<Camera>(); // 카메라 컴포넌트 조회
            shopCamera.clearFlags = CameraClearFlags.SolidColor; // 단색 배경 설정
            shopCamera.backgroundColor = new Color(0.93f, 0.93f, 0.93f, 1f); // 상점 배경색 설정
            shopCamera.orthographic = true; // 직교 카메라 설정
            shopCamera.orthographicSize = 5f; // 직교 크기 설정
        }

        private static void EnsureShopScreen() // 상점 화면 보장
        {
            if (GameObject.Find(ShopControllerName) != null) // 기존 상점 화면 확인
            {
                return; // 중복 화면 생성 차단
            }

            GameObject controllerObject = new GameObject(ShopControllerName, typeof(ShopScreenController)); // 상점 화면 컨트롤러 생성
            controllerObject.name = ShopControllerName; // 컨트롤러 이름 확정
        }
    }
}
