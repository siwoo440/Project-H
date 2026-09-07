using ProjectH.Core; // 게임 관리자 및 씬 이름 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // Unity 씬 이벤트 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class CharacterSceneRuntimePatch // 캐릭터 씬 Runtime 연결 패치
    {
        private const string CharacterControllerName = "CharacterEquipmentScreenRuntime"; // 캐릭터 화면 컨트롤러 이름
        private const string CharacterCameraName = "CharacterCamera"; // 캐릭터 화면 카메라 이름
        private static bool registered; // 씬 이벤트 등록 상태

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // Runtime 씬 패치 등록
        private static void Register() // 씬 로드 이벤트 등록
        {
            if (registered) // 기존 등록 상태 확인
            {
                return; // 중복 등록 차단
            }

            registered = true; // 씬 이벤트 등록 상태 저장
            SceneManager.sceneLoaded += OnSceneLoaded; // 씬 로드 완료 이벤트 연결
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) // 씬 로드 완료 처리
        {
            if (!string.Equals(scene.name, GameScenes.Character, System.StringComparison.Ordinal)) // 캐릭터 씬 여부 확인
            {
                return; // 다른 씬 처리 생략
            }

            EnsureCharacterCamera(); // 캐릭터 씬 카메라 보장
            EnsureCharacterScreen(); // 캐릭터 장비 화면 생성
        }

        private static void EnsureCharacterCamera() // 캐릭터 씬 기본 카메라 보장
        {
            if (UnityEngine.Object.FindFirstObjectByType<Camera>() != null) // 기존 카메라 존재 확인
            {
                return; // 기존 카메라 유지
            }

            GameObject cameraObject = new GameObject(CharacterCameraName, typeof(Camera)); // 캐릭터 씬 기본 카메라 생성
            cameraObject.tag = "MainCamera"; // 메인 카메라 태그 적용
            cameraObject.transform.position = new Vector3(0f, 0f, -10f); // 기본 카메라 위치 설정
            Camera characterCamera = cameraObject.GetComponent<Camera>(); // 생성 카메라 컴포넌트 조회
            characterCamera.clearFlags = CameraClearFlags.SolidColor; // 단색 배경 렌더링 설정
            characterCamera.backgroundColor = new Color(0.93f, 0.93f, 0.93f, 1f); // 캐릭터 화면 기본 배경색 설정
            characterCamera.orthographic = true; // 임시 UI 화면용 직교 카메라 설정
            characterCamera.orthographicSize = 5f; // 임시 카메라 직교 크기 설정
        }

        private static void EnsureCharacterScreen() // 캐릭터 장비 화면 보장
        {
            if (GameObject.Find(CharacterControllerName) != null) // 기존 캐릭터 화면 확인
            {
                return; // 중복 캐릭터 화면 생성 차단
            }

            GameObject controllerObject = new GameObject(CharacterControllerName, typeof(CharacterEquipmentScreenController)); // 캐릭터 장비 화면 컨트롤러 생성
            controllerObject.name = CharacterControllerName; // 캐릭터 화면 객체 이름 확정
        }
    }
}
