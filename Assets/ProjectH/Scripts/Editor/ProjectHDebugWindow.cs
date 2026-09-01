using System.Collections.Generic; // 목록 자료형
using ProjectH.Core; // 프로젝트 핵심 기능
using ProjectH.Events; // 이벤트 기능
using ProjectH.SaveSystem; // 저장 기능
using UnityEditor; // Unity 에디터 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // 씬 관리 기능

namespace ProjectH.EditorTools // 프로젝트 에디터 도구 영역
{
    public sealed class ProjectHDebugWindow : EditorWindow // 프로젝트 상태 디버그 창
    {
        private readonly List<EventDefinition> availableEvents = new List<EventDefinition>(); // 사용 가능 이벤트 목록
        private Vector2 scrollPosition; // 스크롤 위치
        private string flagInput = "STORY_SERENA_JOINED"; // 플래그 입력값

        [MenuItem("Tools/Project H/Debug/State Monitor")] // 디버그 창 메뉴 등록
        public static void Open() // 디버그 창 열기
        {
            ProjectHDebugWindow window = GetWindow<ProjectHDebugWindow>("Project H Debug"); // 디버그 창 생성
            window.minSize = new Vector2(520f, 620f); // 최소 창 크기 설정
            window.Show(); // 디버그 창 표시
        }

        private void OnGUI() // 디버그 화면 그리기
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition); // 스크롤 영역 시작
            DrawHeader(); // 헤더 출력

            if (!Application.isPlaying) // 플레이 모드 확인
            {
                EditorGUILayout.HelpBox("Bootstrap 씬을 Play한 뒤 상태를 확인할 수 있습니다.", MessageType.Info); // 플레이 모드 안내
                EditorGUILayout.EndScrollView(); // 스크롤 영역 종료
                return; // 화면 출력 종료
            }

            GameManager game = GameManager.Instance; // 게임 관리자 조회

            if (game == null) // 게임 관리자 확인
            {
                EditorGUILayout.HelpBox("GameManager.Instance가 없습니다.", MessageType.Error); // 관리자 누락 안내
                EditorGUILayout.EndScrollView(); // 스크롤 영역 종료
                return; // 화면 출력 종료
            }

            DrawSystemState(game); // 시스템 상태 출력
            DrawSaveState(game); // 저장 상태 출력
            DrawFlagTools(game); // 플래그 도구 출력
            DrawEventState(game); // 이벤트 상태 출력
            DrawDataState(game); // 데이터 상태 출력
            EditorGUILayout.EndScrollView(); // 스크롤 영역 종료
            Repaint(); // 런타임 상태 자동 갱신
        }

        private static void DrawHeader() // 디버그 헤더 출력
        {
            EditorGUILayout.Space(8f); // 상단 여백 추가
            EditorGUILayout.LabelField("PROJECT H · DEVELOPMENT STATE", EditorStyles.boldLabel); // 디버그 제목 출력
            EditorGUILayout.LabelField($"Scene : {SceneManager.GetActiveScene().name}"); // 현재 씬 출력
            EditorGUILayout.Space(6f); // 구분 여백 추가
        }

        private static void DrawSystemState(GameManager game) // 시스템 상태 출력
        {
            EditorGUILayout.LabelField("SYSTEM", EditorStyles.boldLabel); // 시스템 섹션 제목
            EditorGUILayout.Toggle("GameManager", game.IsInitialized); // 게임 관리자 상태 출력
            EditorGUILayout.Toggle("DataManager", game.Data != null && game.Data.IsInitialized); // 데이터 관리자 상태 출력
            EditorGUILayout.Toggle("SaveManager", game.Save != null && game.Save.IsInitialized); // 저장 관리자 상태 출력
            EditorGUILayout.Toggle("EventManager", game.Events != null && game.Events.IsInitialized); // 이벤트 관리자 상태 출력
            EditorGUILayout.Space(8f); // 구분 여백 추가
        }

        private static void DrawSaveState(GameManager game) // 저장 상태 출력
        {
            EditorGUILayout.LabelField("SAVE", EditorStyles.boldLabel); // 저장 섹션 제목
            SaveManager save = game.Save; // 저장 관리자 조회

            if (save == null) // 저장 관리자 확인
            {
                EditorGUILayout.HelpBox("SaveManager가 없습니다.", MessageType.Error); // 저장 관리자 누락 안내
                return; // 저장 상태 출력 중단
            }

            EditorGUILayout.Toggle("Has Save File", save.HasSaveData); // 저장 파일 상태 출력
            EditorGUILayout.TextField("Save Path", save.SavePath ?? string.Empty); // 저장 경로 출력

            if (save.CurrentSave != null) // 현재 저장 데이터 확인
            {
                EditorGUILayout.IntField("Day", save.CurrentSave.CurrentDay); // 현재 일차 출력
                EditorGUILayout.EnumPopup("Time", save.CurrentSave.CurrentTime); // 현재 시간대 출력
                EditorGUILayout.TextField("Chapter", save.CurrentSave.CurrentChapter); // 현재 챕터 출력
                EditorGUILayout.TextField("Main Quest", save.CurrentSave.CurrentMainQuest); // 현재 목표 출력
                EditorGUILayout.IntField("Story Flags", save.CurrentSave.StoryFlags.Count); // 플래그 개수 출력
            }
            else // 현재 저장 없음 처리
            {
                EditorGUILayout.HelpBox("CurrentSave가 없습니다. 새 게임 또는 Load를 실행하세요.", MessageType.Warning); // 현재 저장 없음 안내
            }

            EditorGUILayout.BeginHorizontal(); // 저장 버튼 행 시작
            if (GUILayout.Button("Save")) save.SaveCurrent(); // 현재 저장 실행
            if (GUILayout.Button("Load")) save.LoadCurrent(); // 현재 불러오기 실행
            if (GUILayout.Button("Log State")) save.LogCurrentState(); // 현재 상태 로그 출력
            EditorGUILayout.EndHorizontal(); // 저장 버튼 행 종료
            EditorGUILayout.Space(8f); // 구분 여백 추가
        }

        private void DrawFlagTools(GameManager game) // 플래그 도구 출력
        {
            EditorGUILayout.LabelField("STORY FLAGS", EditorStyles.boldLabel); // 플래그 섹션 제목

            if (game.Events == null || game.Save == null || game.Save.CurrentSave == null) // 이벤트 및 저장 상태 확인
            {
                EditorGUILayout.HelpBox("EventManager와 CurrentSave가 필요합니다.", MessageType.Info); // 플래그 도구 안내
                EditorGUILayout.Space(8f); // 구분 여백 추가
                return; // 플래그 도구 출력 중단
            }

            flagInput = EditorGUILayout.TextField("Flag ID", flagInput); // 플래그 입력 출력
            EditorGUILayout.BeginHorizontal(); // 플래그 버튼 행 시작
            if (GUILayout.Button("ON")) game.Events.SetStoryFlag(flagInput); // 플래그 활성화
            if (GUILayout.Button("OFF")) game.Events.RemoveStoryFlag(flagInput); // 플래그 비활성화
            EditorGUILayout.EndHorizontal(); // 플래그 버튼 행 종료

            string flagToRemove = null; // 삭제 예정 플래그 초기화

            foreach (string flagId in game.Save.CurrentSave.StoryFlags) // 활성 플래그 순회
            {
                EditorGUILayout.BeginHorizontal(); // 플래그 행 시작
                EditorGUILayout.LabelField("✓ " + flagId); // 활성 플래그 출력

                if (GUILayout.Button("OFF", GUILayout.Width(60f))) // 플래그 해제 요청 확인
                {
                    flagToRemove = flagId; // 삭제 예정 플래그 저장
                }

                EditorGUILayout.EndHorizontal(); // 플래그 행 종료
            }

            if (!string.IsNullOrWhiteSpace(flagToRemove)) // 삭제 예정 플래그 확인
            {
                game.Events.RemoveStoryFlag(flagToRemove); // 반복 종료 후 플래그 해제
            }

            EditorGUILayout.Space(8f); // 구분 여백 추가
        }

        private void DrawEventState(GameManager game) // 이벤트 상태 출력
        {
            EditorGUILayout.LabelField("EVENT CONDITIONS", EditorStyles.boldLabel); // 이벤트 섹션 제목

            if (game.Events == null) // 이벤트 관리자 확인
            {
                EditorGUILayout.HelpBox("EventManager가 없습니다.", MessageType.Error); // 이벤트 관리자 누락 안내
                EditorGUILayout.Space(8f); // 구분 여백 추가
                return; // 이벤트 상태 출력 중단
            }

            game.Events.GetAvailableEvents(availableEvents); // 사용 가능 이벤트 갱신
            EditorGUILayout.LabelField($"Definitions : {game.Events.DefinitionCount}"); // 등록 이벤트 수 출력
            EditorGUILayout.LabelField($"Available : {availableEvents.Count}"); // 활성 이벤트 수 출력

            foreach (EventDefinition definition in game.Events.Definitions) // 이벤트 정의 순회
            {
                if (definition == null) // 이벤트 정의 확인
                {
                    continue; // null 이벤트 제외
                }

                bool available = game.Events.IsEventAvailable(definition, out string reason); // 이벤트 조건 평가
                EditorGUILayout.BeginVertical(EditorStyles.helpBox); // 이벤트 정보 상자 시작
                EditorGUILayout.LabelField($"{(available ? "✓" : "×")} {definition.Id}", EditorStyles.boldLabel); // 이벤트 ID 출력
                EditorGUILayout.LabelField(definition.DisplayName ?? string.Empty); // 이벤트 이름 출력
                EditorGUILayout.LabelField(reason, EditorStyles.wordWrappedMiniLabel); // 이벤트 평가 사유 출력
                EditorGUILayout.EndVertical(); // 이벤트 정보 상자 종료
            }

            EditorGUILayout.Space(8f); // 구분 여백 추가
        }

        private static void DrawDataState(GameManager game) // 데이터 상태 출력
        {
            EditorGUILayout.LabelField("DATA VALIDATION", EditorStyles.boldLabel); // 데이터 섹션 제목

            if (game.Data == null) // 데이터 관리자 확인
            {
                EditorGUILayout.HelpBox("DataManager가 없습니다.", MessageType.Error); // 데이터 관리자 누락 안내
                return; // 데이터 상태 출력 중단
            }

            EditorGUILayout.LabelField($"Characters : {game.Data.CharacterCount}"); // 캐릭터 수 출력
            EditorGUILayout.LabelField($"Monsters : {game.Data.MonsterCount}"); // 몬스터 수 출력
            EditorGUILayout.LabelField($"Dungeons : {game.Data.DungeonCount}"); // 던전 수 출력
            EditorGUILayout.LabelField($"Items : {game.Data.ItemCount}"); // 아이템 수 출력
            EditorGUILayout.LabelField($"Data Errors : {game.Data.ValidationErrors.Count}"); // 데이터 오류 수 출력
            EditorGUILayout.LabelField($"Event Errors : {(game.Events == null ? 0 : game.Events.ValidationErrors.Count)}"); // 이벤트 오류 수 출력
        }
    }
}
