using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 런타임 기능
using ProjectH.Data; // 데이터 관리자 기능
using ProjectH.SaveSystem; // 저장 데이터 기능
using ProjectH.UI; // 파티 편집 상태 기능
using UnityEditor; // Unity 에디터 기능
using UnityEngine; // Unity 게임 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class PartyPresetBattleRuntimeTests // 파티 편성과 전투 런타임 통합 테스트
    {
        private const string CatalogPath = "Assets/ProjectH/Data/Database/ProjectHDataCatalog.asset"; // 데이터 카탈로그 경로
        private GameObject dataObject; // 테스트 데이터 객체
        private DataManager dataManager; // 테스트 데이터 관리자

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 데이터 관리자 준비
        {
            dataObject = new GameObject("PartyPresetBattleRuntimeTests"); // 테스트 객체 생성
            dataManager = dataObject.AddComponent<DataManager>(); // 데이터 관리자 추가
            ProjectHDataCatalog catalog = AssetDatabase.LoadAssetAtPath<ProjectHDataCatalog>(CatalogPath); // 데이터 카탈로그 로드
            SerializedObject serialized = new SerializedObject(dataManager); // 데이터 관리자 직렬화 객체 생성
            serialized.FindProperty("catalog").objectReferenceValue = catalog; // 카탈로그 참조 연결
            serialized.ApplyModifiedPropertiesWithoutUndo(); // 카탈로그 참조 적용
            dataManager.Initialize(); // 데이터 관리자 초기화
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 테스트 객체 정리
        {
            Object.DestroyImmediate(dataObject); // 테스트 객체 제거
        }

        [Test] // 테스트 표시
        public void CommittedPartyOrder_CreatesSameBattleRuntimeOrder() // 편성 순서 전투 연동 검증
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA", "CH_ELLEN", "CH_LILIA", "CH_EVE", "CH_NATASHA", "CH_CLAIRE" }); // 6인 저장 데이터 생성
            PartyEditState state = PartyEditState.Create(saveData); // 파티 편집 상태 생성
            state.TryAssignCharacter(0, "CH_NATASHA", out string firstError); // 첫 슬롯 나타샤 교체
            state.TryAssignCharacter(1, "CH_CLAIRE", out string secondError); // 두 번째 슬롯 클레어 교체
            state.CommitTo(saveData, out string commitError); // 파티 편집 저장 반영
            bool created = BattlePartyRuntime.TryCreate(dataManager, saveData, out BattlePartyRuntime party, out string runtimeError); // 전투 런타임 파티 생성
            Assert.That(firstError, Is.Empty); // 첫 교체 오류 없음 검증
            Assert.That(secondError, Is.Empty); // 두 번째 교체 오류 없음 검증
            Assert.That(commitError, Is.Empty); // 저장 반영 오류 없음 검증
            Assert.That(created, Is.True, runtimeError); // 전투 파티 생성 성공 검증
            Assert.That(party[0].CharacterId, Is.EqualTo("CH_NATASHA")); // 첫 전투 슬롯 검증
            Assert.That(party[1].CharacterId, Is.EqualTo("CH_CLAIRE")); // 두 번째 전투 슬롯 검증
            Assert.That(party[2].CharacterId, Is.EqualTo("CH_LILIA")); // 세 번째 전투 슬롯 검증
            Assert.That(party[3].CharacterId, Is.EqualTo("CH_EVE")); // 네 번째 전투 슬롯 검증
        }
    }
}
