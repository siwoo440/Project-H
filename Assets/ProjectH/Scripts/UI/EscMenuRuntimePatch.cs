using System.Collections.Generic; // 집합 자료형
using ProjectH.Core; // 씬 이름 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.InputSystem; // 신규 Input System 키 입력 기능 (구형 Input 클래스는 이 프로젝트에서 사용할 수 없음)
using UnityEngine.SceneManagement; // 씬 이벤트 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class EscMenuRuntimePatch // ESC 메뉴 등록 + 로비 [저장] 버튼 정리 (Day72 신규)
    {
        private const string HotkeyObjectName = "EscMenuHotkey"; // 입력 감시 오브젝트 이름

        private static readonly HashSet<string> AllowedScenes = new HashSet<string> // ESC 메뉴를 쓸 수 있는 씬 (타이틀·전투·결과에서는 열리지 않는다)
        {
            GameScenes.Lobby, // 로비
            GameScenes.Village, // 마을
            GameScenes.Shop, // 상점
            GameScenes.Blacksmith, // 대장간
            GameScenes.DungeonSelect, // 모험 지도
            GameScenes.Character, // 캐릭터
            GameScenes.Bag, // 가방
            GameScenes.Party // 파티
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // 첫 씬 로드 전 등록
        private static void Register() // 씬 로드 이벤트 등록
        {
            SceneRuntimePatch.Register(GameScenes.Lobby, OnLobbyLoaded); // 로비 [저장] 버튼 정리
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // 첫 씬 로드 후 감시 시작
        private static void CreateHotkey() // ESC 입력 감시 오브젝트 만들기 (씬이 바뀌어도 유지)
        {
            if (GameObject.Find(HotkeyObjectName) != null) return; // 중복 방지
            GameObject hotkey = new GameObject(HotkeyObjectName, typeof(EscMenuHotkey)); // 감시 오브젝트
            Object.DontDestroyOnLoad(hotkey); // 씬 전환에도 유지
        }

        public static bool IsAllowedScene(string sceneName) => AllowedScenes.Contains(sceneName); // ESC 메뉴 사용 가능 씬인지

        private static void OnLobbyLoaded(Scene scene) // 로비 : 씬에 있던 [저장]·[타이틀] 버튼 감추기 (ESC 메뉴로 옮겼다)
        {
            HideSceneButton("SaveButton"); // 저장 → ESC 메뉴 [저장하기]
            HideSceneButton("TitleButton"); // 타이틀 → ESC 메뉴 [타이틀로 나가기]
        }

        private static void HideSceneButton(string objectName) // 씬에 있는 버튼 하나 감추기
        {
            GameObject target = GameObject.Find(objectName); // 버튼 조회
            if (target != null) target.SetActive(false); // 화면에서 제거
        }
    }

    [DisallowMultipleComponent] // 중복 방지
    public sealed class EscMenuHotkey : MonoBehaviour // ESC 키 감시 (Day72 신규)
    {
        private void Update() // 매 프레임 입력 확인
        {
            Keyboard keyboard = Keyboard.current; // 현재 키보드
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return; // ESC를 누른 프레임만
            if (!EscMenuRuntimePatch.IsAllowedScene(SceneManager.GetActiveScene().name)) return; // 허용된 씬만
            if (!EscMenuView.IsOpen && IsBlockedByOtherScreen()) return; // 대사·이름 입력·놀이판이 떠 있으면 열지 않음
            EscMenuView.Toggle(); // 메뉴 열기·닫기
        }

        private static bool IsBlockedByOtherScreen() // 먼저 끝내야 하는 화면이 떠 있는지
        {
            if (Object.FindFirstObjectByType<DialogueOverlayView>() != null) return true; // 대사 진행 중
            if (Object.FindFirstObjectByType<HeroNameInputView>() != null) return true; // 이름 입력 중
            return Object.FindFirstObjectByType<MinigameView>() != null; // 놀이판 진행 중
        }
    }
}
