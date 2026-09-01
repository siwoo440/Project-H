using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 승패 컨트롤러 기능
using UnityEngine; // Unity 게임 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleOutcomeControllerTests // 전투 승패 컨트롤러 테스트
    {
        private GameObject rootObject; // 테스트 루트 객체
        private BattleCombatRegistry registry; // 테스트 전투 레지스트리
        private BattleOutcomeController controller; // 테스트 승패 컨트롤러

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 테스트 전투 구조 준비
        {
            rootObject = new GameObject("BattleOutcomeControllerTests"); // 테스트 루트 생성
            registry = rootObject.AddComponent<BattleCombatRegistry>(); // 테스트 레지스트리 추가
            controller = rootObject.AddComponent<BattleOutcomeController>(); // 테스트 승패 컨트롤러 추가
            controller.Configure(registry, null); // UI 없는 승패 컨트롤러 설정
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 테스트 전투 구조 정리
        {
            Object.DestroyImmediate(rootObject); // 테스트 루트 제거
        }

        [Test] // 테스트 표시
        public void LastEnemyUnregister_ChangesOutcomeToVictoryOnce() // 마지막 적 제외 승리 1회 처리 검증
        {
            BattleActor ally = CreateActor("ALLY_0", BattleTeam.Ally); // 아군 전투 객체 생성
            BattleActor enemy = CreateActor("ENEMY_0", BattleTeam.Enemy); // 적군 전투 객체 생성
            registry.Register(ally); // 아군 레지스트리 등록
            registry.Register(enemy); // 적군 레지스트리 등록
            int changedCount = 0; // 승패 변경 횟수 초기화
            controller.OutcomeChanged += _ => changedCount++; // 승패 변경 이벤트 구독
            controller.BeginBattle(); // 전투 승패 감시 시작
            registry.Unregister(enemy); // 마지막 적군 제외
            registry.Unregister(enemy); // 동일 적군 중복 제외 시도

            Assert.That(controller.CurrentOutcome, Is.EqualTo(BattleOutcome.Victory)); // 승리 상태 검증
            Assert.That(changedCount, Is.EqualTo(1)); // 승리 중복 처리 방지 검증
        }

        [Test] // 테스트 표시
        public void LastAllyUnregister_ChangesOutcomeToDefeat() // 마지막 아군 제외 패배 검증
        {
            BattleActor ally = CreateActor("ALLY_0", BattleTeam.Ally); // 아군 전투 객체 생성
            BattleActor enemy = CreateActor("ENEMY_0", BattleTeam.Enemy); // 적군 전투 객체 생성
            registry.Register(ally); // 아군 레지스트리 등록
            registry.Register(enemy); // 적군 레지스트리 등록
            controller.BeginBattle(); // 전투 승패 감시 시작
            registry.Unregister(ally); // 마지막 아군 제외

            Assert.That(controller.CurrentOutcome, Is.EqualTo(BattleOutcome.Defeat)); // 패배 상태 검증
        }

        private BattleActor CreateActor(string runtimeId, BattleTeam team) // 테스트 전투 객체 생성
        {
            GameObject actorObject = new GameObject(runtimeId); // 테스트 전투 객체 생성
            actorObject.transform.SetParent(rootObject.transform, false); // 테스트 루트 연결
            BattleActor actor = actorObject.AddComponent<BattleActor>(); // 전투 액터 추가
            actor.Initialize(team, new TestStats(runtimeId), Vector3.zero); // 테스트 전투 액터 초기화
            return actor; // 테스트 전투 액터 반환
        }

        private sealed class TestStats : IBattleCombatantStats // 테스트 조회 전투 스탯
        {
            public string RuntimeId { get; } // 런타임 ID 반환
            public string DisplayName => RuntimeId; // 표시 이름 반환
            public int MaxHp => 100; // 최대 체력 반환
            public int CurrentHp => 100; // 현재 체력 반환
            public int Attack => 10; // 공격력 반환
            public int Defense => 5; // 방어력 반환
            public float AttackSpeed => 1f; // 공격속도 반환
            public float AttackRange => 1.5f; // 공격 사거리 반환
            public float MoveSpeed => 2f; // 이동속도 반환
            public bool IsAlive => true; // 생존 상태 반환

            public TestStats(string runtimeId) // 테스트 조회 스탯 생성
            {
                RuntimeId = runtimeId; // 런타임 ID 저장
            }
        }
    }
}
