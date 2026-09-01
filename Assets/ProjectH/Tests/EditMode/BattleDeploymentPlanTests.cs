using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 런타임 기능
using ProjectH.Data; // 데이터 관리자 기능
using ProjectH.SaveSystem; // 저장 데이터 기능
using UnityEditor; // Unity 에디터 기능
using UnityEngine; // Unity 게임 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleDeploymentPlanTests // 전투 배치 계획 테스트
    {
        private const string CatalogPath = "Assets/ProjectH/Data/Database/ProjectHDataCatalog.asset"; // 데이터 카탈로그 경로
        private GameObject rootObject; // 테스트 루트 객체
        private DataManager dataManager; // 테스트 데이터 관리자

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 테스트 환경 준비
        {
            rootObject = new GameObject("BattleDeploymentPlanTests"); // 테스트 루트 생성
            dataManager = rootObject.AddComponent<DataManager>(); // 데이터 관리자 추가
            ProjectHDataCatalog catalog = AssetDatabase.LoadAssetAtPath<ProjectHDataCatalog>(CatalogPath); // 데이터 카탈로그 로드
            SerializedObject serialized = new SerializedObject(dataManager); // 데이터 관리자 직렬화 객체 생성
            serialized.FindProperty("catalog").objectReferenceValue = catalog; // 카탈로그 참조 연결
            serialized.ApplyModifiedPropertiesWithoutUndo(); // 카탈로그 참조 적용
            dataManager.Initialize(); // 데이터 관리자 초기화
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 테스트 환경 정리
        {
            Object.DestroyImmediate(rootObject); // 테스트 루트 제거
        }

        [Test] // 테스트 표시
        public void TryCreate_UsesPartyOrderForAllySlots() // 파티 순서와 배치 슬롯 연동 검증
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_ELLEN", "CH_SERENA", "CH_EVE", "CH_LILIA" }); // 순서 변경 파티 저장 생성
            bool runtimeCreated = BattlePartyRuntime.TryCreate(dataManager, saveData, out BattlePartyRuntime party, out string runtimeError); // 전투 파티 런타임 생성
            BattleFormationAnchors anchors = CreateFormation(4); // 4인 배치 앵커 생성
            bool planCreated = BattleDeploymentPlan.TryCreate(party, anchors, out BattleDeploymentPlan plan, out string planError); // 배치 계획 생성

            Assert.That(runtimeCreated, Is.True, runtimeError); // 전투 파티 생성 성공 검증
            Assert.That(planCreated, Is.True, planError); // 배치 계획 생성 성공 검증
            Assert.That(plan.Count, Is.EqualTo(4)); // 배치 인원 검증
            Assert.That(plan[0].Stats.CharacterId, Is.EqualTo("CH_ELLEN")); // 첫 캐릭터 순서 검증
            Assert.That(plan[0].Stats.RuntimeId, Is.EqualTo("ALLY_0")); // 첫 런타임 ID 검증
            Assert.That(plan[0].Anchor.name, Is.EqualTo("AllySlot_0")); // 첫 배치 슬롯 검증
            Assert.That(plan[3].Stats.CharacterId, Is.EqualTo("CH_LILIA")); // 마지막 캐릭터 순서 검증
            Assert.That(plan[3].Anchor.name, Is.EqualTo("AllySlot_3")); // 마지막 배치 슬롯 검증
        }

        [Test] // 테스트 표시
        public void TryCreate_WhenAnchorsAreInsufficient_FailsClearly() // 앵커 부족 실패 검증
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA", "CH_ELLEN", "CH_LILIA", "CH_EVE" }); // 4인 저장 데이터 생성
            BattlePartyRuntime.TryCreate(dataManager, saveData, out BattlePartyRuntime party, out string runtimeError); // 전투 파티 런타임 생성
            BattleFormationAnchors anchors = CreateFormation(3); // 3개 아군 앵커 생성

            bool planCreated = BattleDeploymentPlan.TryCreate(party, anchors, out BattleDeploymentPlan plan, out string error); // 배치 계획 생성 시도

            Assert.That(runtimeError, Is.Empty); // 전투 파티 오류 없음 검증
            Assert.That(planCreated, Is.False); // 배치 계획 생성 실패 검증
            Assert.That(plan, Is.Null); // 실패 계획 null 검증
            Assert.That(error, Does.Contain("anchor")); // 앵커 부족 오류 검증
        }

        private BattleFormationAnchors CreateFormation(int allyCount) // 테스트 전투 배치 생성
        {
            GameObject formationObject = new GameObject("Formation"); // 배치 객체 생성
            formationObject.transform.SetParent(rootObject.transform, false); // 테스트 루트 연결
            BattleFormationAnchors anchors = formationObject.AddComponent<BattleFormationAnchors>(); // 배치 앵커 컴포넌트 추가
            Transform[] allies = new Transform[allyCount]; // 아군 앵커 배열 생성
            Transform[] enemies = new Transform[5]; // 적군 앵커 배열 생성

            for (int index = 0; index < allies.Length; index++) // 아군 앵커 순회
            {
                GameObject anchor = new GameObject($"AllySlot_{index}"); // 아군 앵커 생성
                anchor.transform.SetParent(formationObject.transform, false); // 배치 객체 연결
                allies[index] = anchor.transform; // 아군 배열 등록
            }

            for (int index = 0; index < enemies.Length; index++) // 적군 앵커 순회
            {
                GameObject anchor = new GameObject($"EnemySlot_{index}"); // 적군 앵커 생성
                anchor.transform.SetParent(formationObject.transform, false); // 배치 객체 연결
                enemies[index] = anchor.transform; // 적군 배열 등록
            }

            anchors.Configure(allies, enemies); // 배치 앵커 연결
            return anchors; // 배치 컴포넌트 반환
        }
    }
}
