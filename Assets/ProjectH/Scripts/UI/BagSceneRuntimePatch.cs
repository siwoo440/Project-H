using ProjectH.Core; // 게임 관리자 및 씬 이름 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // Unity 씬 이벤트 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class BagSceneRuntimePatch // 가방 씬 Runtime 연결 패치
    {
        private const string BagControllerName = "BagScreenRuntime"; // 가방 화면 컨트롤러 이름
        private const string BagCameraName = "BagCamera"; // 가방 화면 카메라 이름

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // Runtime 씬 패치 등록
        private static void Register() // 씬 로드 이벤트 등록
        {
            SceneRuntimePatch.Register(GameScenes.Bag, OnSceneLoaded); // Bag 씬 로드 시 주입 등록 (최적화 — 공통 등록기 사용)
        }

        private static void OnSceneLoaded(Scene scene) // 씬 로드 완료 처리
        {
            EnsureBagCamera(); // 가방 씬 카메라 보장
            EnsureBagScreen(); // 가방 화면 생성
        }

        private static void EnsureBagCamera() // 가방 씬 기본 카메라 보장
        {
            if (UnityEngine.Object.FindFirstObjectByType<Camera>() != null) // 기존 카메라 존재 확인
            {
                return; // 기존 카메라 유지
            }

            GameObject cameraObject = new GameObject(BagCameraName, typeof(Camera)); // 가방 씬 기본 카메라 생성
            cameraObject.tag = "MainCamera"; // 메인 카메라 태그 적용
            cameraObject.transform.position = new Vector3(0f, 0f, -10f); // 기본 카메라 위치 설정
            Camera bagCamera = cameraObject.GetComponent<Camera>(); // 생성 카메라 컴포넌트 조회
            bagCamera.clearFlags = CameraClearFlags.SolidColor; // 단색 배경 렌더링 설정
            bagCamera.backgroundColor = new Color(0.93f, 0.93f, 0.93f, 1f); // 가방 화면 기본 배경색 설정
            bagCamera.orthographic = true; // UI 화면용 직교 카메라 설정
            bagCamera.orthographicSize = 5f; // 기본 카메라 직교 크기 설정
        }

        private static void EnsureBagScreen() // 가방 화면 보장
        {
            if (GameObject.Find(BagControllerName) != null) // 기존 가방 화면 확인
            {
                return; // 중복 가방 화면 생성 차단
            }

            GameObject controllerObject = new GameObject(BagControllerName, typeof(BagScreenController)); // 가방 화면 컨트롤러 생성
            controllerObject.name = BagControllerName; // 가방 화면 객체 이름 확정
        }
    }
}
