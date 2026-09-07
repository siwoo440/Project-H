using System.Collections.Generic; // 목록 자료형
using System.Reflection; // 비정상 저장 데이터 테스트 주입 기능
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
        public void TryCreate_RebuildsRuntimeStatsFromUpdatedSavedLevel() // 저장 레벨 변경 후 런타임 스탯 재계산 검증
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA", "CH_ELLEN", "CH_LILIA", "CH_EVE" }); // 초기 4인 저장 데이터 생성
            CharacterSaveData serenaSave = saveData.FindCharacter("CH_SERENA"); // 세레나 저장 데이터 조회

            bool firstCreated = BattlePartyRuntime.TryCreate(dataManager, saveData, out BattlePartyRuntime firstParty, out string firstError); // 1레벨 런타임 파티 생성
            serenaSave.SetLevel(5); // 저장 레벨 5 설정
            bool secondCreated = BattlePartyRuntime.TryCreate(dataManager, saveData, out BattlePartyRuntime secondParty, out string secondError); // 5레벨 런타임 파티 재생성

            Assert.That(firstCreated, Is.True, firstError); // 첫 파티 생성 성공 검증
            Assert.That(secondCreated, Is.True, secondError); // 두 번째 파티 생성 성공 검증
            Assert.That(firstParty[0].Level, Is.EqualTo(1)); // 첫 런타임 레벨 검증
            Assert.That(firstParty[0].MaxHp, Is.EqualTo(2200)); // 첫 런타임 체력 검증
            Assert.That(secondParty[0].Level, Is.EqualTo(5)); // 재생성 런타임 레벨 검증
            Assert.That(secondParty[0].MaxHp, Is.EqualTo(2640)); // 재계산 런타임 체력 검증
            Assert.That(secondParty[0].Attack, Is.EqualTo(216)); // 재계산 런타임 공격력 검증
            Assert.That(secondParty[0].Defense, Is.EqualTo(144)); // 재계산 런타임 방어력 검증
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
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA", "CH_ELLEN", "CH_LILIA", "CH_EVE", "CH_NATASHA" }); // 보유 캐릭터 5명 저장 데이터 생성
            ForceActiveParty(saveData, new[] { "CH_SERENA", "CH_ELLEN", "CH_LILIA", "CH_EVE", "CH_NATASHA" }); // 손상 저장을 가정한 5인 활성 파티 강제 주입

            bool created = BattlePartyRuntime.TryCreate(dataManager, saveData, out BattlePartyRuntime party, out string error); // 런타임 파티 생성 시도

            Assert.That(created, Is.False); // 파티 생성 실패 검증
            Assert.That(party, Is.Null); // 실패 파티 null 검증
            Assert.That(error, Does.Contain("4")); // 최대 인원 오류 검증
        }

        private static void ForceActiveParty(SaveData saveData, IEnumerable<string> characterIds) // 비정상 저장 데이터 활성 파티 강제 주입
        {
            FieldInfo partyField = typeof(SaveData).GetField("partyCharacterIds", BindingFlags.Instance | BindingFlags.NonPublic); // 비공개 활성 파티 필드 조회
            Assert.That(partyField, Is.Not.Null); // 활성 파티 필드 존재 검증
            partyField.SetValue(saveData, new List<string>(characterIds)); // 정규화 전 비정상 활성 파티 데이터 주입
        }
    }
}
