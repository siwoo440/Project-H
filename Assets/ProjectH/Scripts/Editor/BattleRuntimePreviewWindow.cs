using ProjectH.Battle; // 전투 런타임 기능
using ProjectH.Data; // 캐릭터 데이터 기능
using UnityEditor; // Unity 에디터 기능
using UnityEngine; // Unity 화면 기능

namespace ProjectH.EditorTools // 프로젝트 에디터 도구 영역
{
    public sealed class BattleRuntimePreviewWindow : EditorWindow // 전투 스탯 미리보기 창
    {
        private const string CatalogPath = "Assets/ProjectH/Data/Database/ProjectHDataCatalog.asset"; // 데이터 카탈로그 경로
        private Vector2 scrollPosition; // 스크롤 위치
        private int previewLevel = 1; // 미리보기 레벨

        [MenuItem("Tools/Project H/Phase 1/8일차 Runtime Stats Preview")] // 미리보기 메뉴 등록
        public static void Open() // 미리보기 창 열기
        {
            BattleRuntimePreviewWindow window = GetWindow<BattleRuntimePreviewWindow>("Battle Runtime"); // 미리보기 창 생성
            window.minSize = new Vector2(560f, 500f); // 최소 창 크기 설정
            window.Show(); // 미리보기 창 표시
        }

        private void OnGUI() // 미리보기 화면 출력
        {
            ProjectHDataCatalog catalog = AssetDatabase.LoadAssetAtPath<ProjectHDataCatalog>(CatalogPath); // 데이터 카탈로그 로드
            EditorGUILayout.LabelField("PHASE 1 DAY 8 · BATTLE RUNTIME PREVIEW", EditorStyles.boldLabel); // 창 제목 출력
            previewLevel = EditorGUILayout.IntSlider("Preview Level", previewLevel, 1, 100); // 미리보기 레벨 입력

            if (catalog == null) // 카탈로그 존재 확인
            {
                EditorGUILayout.HelpBox("ProjectHDataCatalog을 찾을 수 없습니다.", MessageType.Error); // 카탈로그 누락 안내
                return; // 화면 출력 중단
            }

            EditorGUILayout.LabelField($"Characters : {catalog.Characters.Count}"); // 캐릭터 수 출력
            EditorGUILayout.Space(6f); // 구분 여백 추가
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition); // 스크롤 영역 시작

            for (int index = 0; index < catalog.Characters.Count; index++) // 캐릭터 목록 순회
            {
                CharacterData character = catalog.Characters[index]; // 현재 캐릭터 조회

                if (character == null) // 캐릭터 존재 확인
                {
                    continue; // null 캐릭터 제외
                }

                BattleStats stats = BattleStatsFactory.CreateCharacter(character, previewLevel, $"PREVIEW_{index}"); // 미리보기 런타임 스탯 생성
                EditorGUILayout.BeginVertical(EditorStyles.helpBox); // 캐릭터 정보 상자 시작
                EditorGUILayout.LabelField($"{stats.DisplayName} · {stats.CharacterId}", EditorStyles.boldLabel); // 캐릭터 이름 출력
                EditorGUILayout.LabelField($"Position : {stats.Position}    Level : {stats.Level}"); // 포지션과 레벨 출력
                EditorGUILayout.LabelField($"HP : {stats.MaxHp}    ATK : {stats.Attack}    DEF : {stats.Defense}"); // 주요 스탯 출력
                EditorGUILayout.LabelField($"ASPD : {stats.AttackSpeed:0.00}    ACC : {stats.Accuracy:P0}    CRIT : {stats.CriticalRate:P0}"); // 보조 스탯 출력
                EditorGUILayout.EndVertical(); // 캐릭터 정보 상자 종료
            }

            EditorGUILayout.EndScrollView(); // 스크롤 영역 종료
        }
    }
}
