using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 타겟팅 기능
using UnityEngine; // Unity 게임 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleTargetSelectorTests // 전투 타겟 선택 테스트
    {
        private GameObject rootObject; // 테스트 루트 객체

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 테스트 루트 준비
        {
            rootObject = new GameObject("BattleTargetSelectorTests"); // 테스트 루트 생성
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 테스트 루트 정리
        {
            Object.DestroyImmediate(rootObject); // 테스트 루트 제거
        }

        [Test] // 테스트 표시
        public void SelectNearest_ReturnsClosestLivingOpponentAhead() // 가장 가까운 생존 전방 적 선택 검증
        {
            BattleActor source = CreateActor("ALLY_0", BattleTeam.Ally, new Vector3(-3f, 0f, 0f), true); // 아군 전투 객체 생성
            BattleActor farEnemy = CreateActor("ENEMY_0", BattleTeam.Enemy, new Vector3(4f, 0f, 0f), true); // 먼 적군 생성
            BattleActor nearEnemy = CreateActor("ENEMY_1", BattleTeam.Enemy, new Vector3(1f, 2f, 0f), true); // 가까운 전방 적군 생성
            BattleActor ally = CreateActor("ALLY_1", BattleTeam.Ally, new Vector3(-2f, 0f, 0f), true); // 같은 팀 전투 객체 생성
            BattleActor deadEnemy = CreateActor("ENEMY_2", BattleTeam.Enemy, new Vector3(-2.5f, 0f, 0f), false); // 사망 적군 생성
            BattleActor selected = BattleTargetSelector.SelectNearest(source, new[] { farEnemy, nearEnemy, ally, deadEnemy }); // 가장 가까운 전방 적 선택

            Assert.That(selected, Is.SameAs(nearEnemy)); // 가까운 생존 전방 적 선택 검증
        }

        [Test] // 테스트 표시
        public void SelectNearest_PrefersOpponentAheadOverOpponentBehind() // 전방 상대 우선 선택 검증
        {
            BattleActor source = CreateActor("ALLY_0", BattleTeam.Ally, Vector3.zero, true); // 아군 전투 객체 생성
            BattleActor behindEnemy = CreateActor("ENEMY_BEHIND", BattleTeam.Enemy, new Vector3(-0.2f, 0f, 0f), true); // 비정상 뒤쪽 적군 생성
            BattleActor aheadEnemy = CreateActor("ENEMY_AHEAD", BattleTeam.Enemy, new Vector3(1.5f, 0f, 0f), true); // 정상 전방 적군 생성
            BattleActor selected = BattleTargetSelector.SelectNearest(source, new[] { behindEnemy, aheadEnemy }); // 전방 우선 타겟 선택

            Assert.That(selected, Is.SameAs(aheadEnemy)); // 전방 적군 우선 선택 검증
        }

        [Test] // 테스트 표시
        public void SelectNearest_WhenNoLivingOpponent_ReturnsNull() // 생존 적 없음 처리 검증
        {
            BattleActor source = CreateActor("ALLY_0", BattleTeam.Ally, Vector3.zero, true); // 아군 전투 객체 생성
            BattleActor ally = CreateActor("ALLY_1", BattleTeam.Ally, Vector3.right, true); // 같은 팀 객체 생성
            BattleActor deadEnemy = CreateActor("ENEMY_0", BattleTeam.Enemy, Vector3.left, false); // 사망 적군 객체 생성
            BattleActor selected = BattleTargetSelector.SelectNearest(source, new[] { ally, deadEnemy }); // 타겟 선택 시도

            Assert.That(selected, Is.Null); // 타겟 없음 검증
        }

        private BattleActor CreateActor(string runtimeId, BattleTeam team, Vector3 position, bool alive) // 테스트 전투 객체 생성
        {
            GameObject actorObject = new GameObject(runtimeId); // 전투 객체 생성
            actorObject.transform.SetParent(rootObject.transform, false); // 테스트 루트 연결
            actorObject.transform.position = position; // 테스트 위치 설정
            BattleActor actor = actorObject.AddComponent<BattleActor>(); // 전투 액터 추가
            TestCombatantStats stats = new TestCombatantStats(runtimeId, alive); // 테스트 전투 스탯 생성
            actor.Initialize(team, stats, position); // 전투 액터 초기화
            return actor; // 전투 액터 반환
        }

        private sealed class TestCombatantStats : IBattleCombatantStats // 테스트 전투 스탯
        {
            public string RuntimeId { get; } // 런타임 ID 반환
            public string DisplayName => RuntimeId; // 표시 이름 반환
            public int MaxHp => 100; // 최대 체력 반환
            public int CurrentHp { get; private set; } // 현재 체력 반환
            public int Attack => 10; // 공격력 반환
            public int Defense => 5; // 방어력 반환
            public float AttackSpeed => 1f; // 공격속도 반환
            public float AttackRange => 1.5f; // 공격 사거리 반환
            public float MoveSpeed => 2f; // 이동속도 반환
            public bool IsAlive => CurrentHp > 0; // 생존 여부 반환

            public TestCombatantStats(string runtimeId, bool alive) // 테스트 전투 스탯 생성
            {
                RuntimeId = runtimeId; // 런타임 ID 저장
                CurrentHp = alive ? MaxHp : 0; // 테스트 생존 상태 설정
            }
        }
    }
}
