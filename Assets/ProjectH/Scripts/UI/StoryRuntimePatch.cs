using ProjectH.Core; // 게임 관리자 및 씬 이름 기능
using ProjectH.Dialogue; // 대사 파일 기능
using ProjectH.SaveSystem; // 저장·이름 기능
using ProjectH.Story; // 챕터 진행 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // 씬 이벤트 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class StoryRuntimePatch // 로비에서 메인 스토리 진행 (Day68 신규 — 이름 입력 → 챕터 이야기 자동 재생)
    {
        private static bool busy; // 이름 입력·이야기 진행 중

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // 첫 씬 로드 전 등록
        private static void Register() // 씬 로드 이벤트 등록
        {
            SceneRuntimePatch.Register(GameScenes.Lobby, OnLobbyLoaded); // 로비에서 스토리 진행
        }

        private static void OnLobbyLoaded(Scene scene) // 로비 로드 완료 처리
        {
            busy = false; // 상태 초기화 (씬 재진입)
            Step(); // 이름 → 이야기 순서로 진행
        }

        public static void Step() // 다음 할 일 진행 (이름 입력 → 자동 재생 이야기)
        {
            if (busy) return; // 진행 중
            SaveData saveData = GetSave(); // 현재 저장
            if (saveData == null) return; // 저장 없음

            if (HeroNameService.NeedsInput(saveData)) // 아직 이름을 정하지 않음
            {
                busy = true; // 진행 중
                HeroNameInputView.Open(name => // 이름 입력 화면
                {
                    HeroNameService.TrySetName(GetSave(), name); // 이름 저장
                    Save(); // 즉시 저장
                    busy = false; // 완료
                    Step(); // 이어서 이야기
                });

                return; // 이름 입력 대기
            }

            ChapterService.Refresh(saveData); // 진행 상황 정리 (이미 깬 던전 단계 통과)
            string scriptId = ChapterService.GetPendingDialogueId(saveData); // 자동 재생할 이야기
            if (string.IsNullOrEmpty(scriptId)) { Save(); return; } // 재생할 이야기 없음
            DialogueScript script = DialogueLibrary.Load(scriptId); // 대사 파일

            if (script == null) // 대사 파일 없음
            {
                Debug.LogWarning($"[Project H][STORY] 대사 파일을 찾을 수 없습니다. ({scriptId})"); // 안내
                return; // 중단
            }

            busy = true; // 진행 중
            DialogueOverlayView.Open(script, runner => // 이야기 재생
            {
                if (runner != null && runner.IsFinished) ChapterService.CompleteDialogue(GetSave(), scriptId, runner); // 끝까지 봤으면 다음 단계 (선택지 플래그 포함, Day71)
                Save(); // 진행 저장
                busy = false; // 완료
                RefreshLobby(); // 로비 표시 갱신
                Step(); // 연속된 이야기 이어서
            });
        }

        private static void RefreshLobby() // 로비 화면 갱신 (챕터·목표 문구 반영)
        {
            LobbyScreenController lobby = Object.FindFirstObjectByType<LobbyScreenController>(); // 로비 컨트롤러
            if (lobby != null) lobby.Refresh(); // 갱신
        }

        private static void Save() // 즉시 저장
        {
            if (GameManager.Instance != null && GameManager.Instance.Save != null) GameManager.Instance.Save.SaveCurrent(); // 저장
        }

        private static SaveData GetSave() => GameManager.Instance == null || GameManager.Instance.Save == null ? null : GameManager.Instance.Save.CurrentSave; // 현재 저장 조회
    }
}
