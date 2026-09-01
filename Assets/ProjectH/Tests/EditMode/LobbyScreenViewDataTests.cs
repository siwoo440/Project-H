using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Data; // 프로젝트 데이터 기능
using ProjectH.SaveSystem; // 프로젝트 저장 기능
using ProjectH.UI; // 프로젝트 UI 기능
using UnityEditor; // Unity 에디터 기능
using UnityEngine; // Unity 게임 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class LobbyScreenViewDataTests // 로비 화면 데이터 테스트
    {
        private const string CatalogPath = "Assets/ProjectH/Data/Database/ProjectHDataCatalog.asset"; // 데이터 카탈로그 경로
        private GameObject dataObject; // 테스트 데이터 객체
        private DataManager dataManager; // 테스트 데이터 관리자

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 데이터 관리자 준비
        {
            dataObject = new GameObject("LobbyScreenViewDataTests"); // 테스트 객체 생성
            dataManager = dataObject.AddComponent<DataManager>(); // 데이터 관리자 추가
            ProjectHDataCatalog catalog = AssetDatabase.LoadAssetAtPath<ProjectHDataCatalog>(CatalogPath); // 데이터 카탈로그 로드
            SerializedObject serialized = new SerializedObject(dataManager); // 데이터 관리자 직렬화 객체 생성
            serialized.FindProperty("catalog").objectReferenceValue = catalog; // 카탈로그 참조 연결
            serialized.ApplyModifiedPropertiesWithoutUndo(); // 카탈로그 연결 적용
            dataManager.Initialize(); // 데이터 관리자 초기화
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 테스트 객체 정리
        {
            Object.DestroyImmediate(dataObject); // 테스트 객체 제거
        }

        [Test] // 테스트 표시
        public void Build_WithoutSave_ReturnsLockedLobby() // 저장 없음 로비 검증
        {
            LobbyScreenViewData state = LobbyScreenViewData.Build(dataManager, null, false); // 저장 없음 화면 데이터 생성

            Assert.That(state.CanNavigate, Is.False); // 진행 버튼 잠금 검증
            Assert.That(state.StatusText, Is.EqualTo("진행 데이터 없음")); // 진행 없음 상태 검증
            Assert.That(state.BodyText, Does.Contain("타이틀")); // 타이틀 이동 안내 검증
        }

        [Test] // 테스트 표시
        public void Build_WithInitialParty_ShowsNamesAndLevels() // 초기 파티 표시 검증
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA", "CH_ELLEN", "CH_LILIA", "CH_EVE" }); // 초기 저장 데이터 생성
            CharacterSaveData serena = saveData.FindCharacter("CH_SERENA"); // 세레나 진행 조회
            serena.SetLevel(5); // 세레나 테스트 레벨 적용
            saveData.SetCurrentDay(7); // 테스트 일차 적용

            LobbyScreenViewData state = LobbyScreenViewData.Build(dataManager, saveData, true); // 로비 화면 데이터 생성

            Assert.That(state.CanNavigate, Is.True); // 진행 버튼 활성 검증
            Assert.That(state.StatusText, Does.Contain("DAY 7")); // 일차 표시 검증
            Assert.That(state.PartyText, Does.Contain("1. 세레나  Lv.5")); // 세레나 표시 검증
            Assert.That(state.PartyText, Does.Contain("4. 이브  Lv.1")); // 이브 표시 검증
            Assert.That(state.SaveStateText, Is.EqualTo("SAVE DATA · ONLINE")); // 저장 상태 표시 검증
        }
    }
}
