using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 런타임 기능
using ProjectH.Data; // 데이터 관리자 기능
using ProjectH.SaveSystem; // 저장 데이터 기능
using UnityEditor; // Unity 에디터 기능
using UnityEngine; // Unity 게임 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattlePartyRuntimeTests // 전투 파티 런타임 테스트
    {
        private const string CatalogPath = "Assets/ProjectH/Data/Database/ProjectHDataCatalog.asset"; // 데이터 카탈로그 경로
        private GameObject dataObject; // 테스트 데이터 객체
        private DataManager dataManager; // 테스트 데이터 관리자

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 테스트 데이터 관리자 준비
        {
            dataObject = new GameObject("BattlePartyRuntimeTests"); // 테스트 게임 오브젝트 생성
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
            Object.DestroyImmediate(dataObject); // 테스트 게임 오브젝트 제거
        }

        [Test] // 테스트 표시
        public void TryCreate_BuildsInitialFourMemberParty() // 초기 4인 파티 생성 검증
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA", "CH_ELLEN", "CH_LILIA", "CH_EVE" }); // 초기 4인 저장 데이터 생성

            bool created = BattlePartyRuntime.TryCreate(dataManager, saveData, out BattlePartyRuntime party, out string error); // 런타임 파티 생성

            Assert.That(created, Is.True, error); // 파티 생성 성공 검증
            Assert.That(party, Is.Not.Null); // 파티 객체 존재 검증
            Assert.That(party.Count, Is.EqualTo(4)); // 파티 인원 검증
            Assert.That(party[0].RuntimeId, Is.EqualTo("ALLY_0")); // 첫 슬롯 런타임 ID 검증
            Assert.That(party[0].CharacterId, Is.EqualTo("CH_SERENA")); // 첫 슬롯 캐릭터 검증
            Assert.That(party[3].CharacterId, Is.EqualTo("CH_EVE")); // 마지막 슬롯 캐릭터 검증
        }

        [Test] // 테스트 표시
        public void TryCreate_FailsForUnknownCharacterId() // 알 수 없는 캐릭터 실패 검증
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_UNKNOWN" }); // 잘못된 저장 데이터 생성

            bool created = BattlePartyRuntime.TryCreate(dataManager, saveData, out BattlePartyRuntime party, out string error); // 런타임 파티 생성 시도

            Assert.That(created, Is.False); // 파티 생성 실패 검증
            Assert.That(party, Is.Null); // 실패 파티 null 검증
            Assert.That(error, Does.Contain("CH_UNKNOWN")); // 실패 원인 ID 검증
        }

        [Test] // 테스트 표시
        public void TryCreate_FailsWhenPartyExceedsFourMembers() // 최대 파티 인원 검증
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA", "CH_ELLEN", "CH_LILIA", "CH_EVE", "CH_NATASHA" }); // 5인 저장 데이터 생성

            bool created = BattlePartyRuntime.TryCreate(dataManager, saveData, out BattlePartyRuntime party, out string error); // 런타임 파티 생성 시도

            Assert.That(created, Is.False); // 파티 생성 실패 검증
            Assert.That(party, Is.Null); // 실패 파티 null 검증
            Assert.That(error, Does.Contain("4")); // 최대 인원 오류 검증
        }
    }
}
