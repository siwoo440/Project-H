using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 전진 기능
using UnityEngine; // Unity 게임 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleForwardAdvanceTests // 전선 전진 이동 테스트
    {
        private GameObject rootObject; // 테스트 루트 객체

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 테스트 루트 준비
        {
            rootObject = new GameObject("BattleForwardAdvanceTests"); // 테스트 루트 생성
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 테스트 루트 정리
        {
            Object.DestroyImmediate(rootObject); // 테스트 루트 제거
        }

        [Test] // 테스트 표시
        public void AllyMoveForwardToward_StopsAtAttackRangeAndPreservesLane() // 아군 전진 정지선 검증
        {
            BattleActor ally = CreateActor("ALLY_0", BattleTeam.Ally, new Vector3(0f, 1.25f, 0f), 1f, 20f); // 빠른 아군 생성
            BattleActor enemy = CreateActor("ENEMY_0", BattleTeam.Enemy, new Vector3(3f, -1f, 0f), 1f, 20f); // 적군 생성
            ally.MoveForwardToward(enemy, 1f); // 아군 전방 이동 실행

            Assert.That(ally.transform.position.x, Is.EqualTo(2f).Within(0.001f)); // 적 공격 사거리 앞 정지 검증
            Assert.That(ally.transform.position.x, Is.LessThan(enemy.transform.position.x)); // 적 통과 방지 검증
            Assert.That(ally.transform.position.y, Is.EqualTo(1.25f).Within(0.001f)); // 기존 세로 Lane 유지 검증
        }

        [Test] // 테스트 표시
        public void EnemyMoveForwardToward_StopsAtAttackRangeWithoutPassingAlly() // 적군 전진 정지선 검증
        {
            BattleActor ally = CreateActor("ALLY_0", BattleTeam.Ally, new Vector3(0f, 0f, 0f), 1f, 20f); // 아군 생성
            BattleActor enemy = CreateActor("ENEMY_0", BattleTeam.Enemy, new Vector3(4f, 0.8f, 0f), 1f, 20f); // 빠른 적군 생성
            enemy.MoveForwardToward(ally, 1f); // 적군 전방 이동 실행

            Assert.That(enemy.transform.position.x, Is.EqualTo(1f).Within(0.001f)); // 아군 공격 사거리 앞 정지 검증
            Assert.That(enemy.transform.position.x, Is.GreaterThan(ally.transform.position.x)); // 아군 통과 방지 검증
            Assert.That(enemy.transform.position.y, Is.EqualTo(0.8f).Within(0.001f)); // 기존 세로 Lane 유지 검증
        }

        [Test] // 테스트 표시
        public void HorizontalRange_IgnoresVisualLaneOffset() // 횡스크롤 가로 사거리 판정 검증
        {
            BattleActor ally = CreateActor("ALLY_0", BattleTeam.Ally, new Vector3(0f, 2f, 0f), 1.5f, 2f); // 아군 생성
            BattleActor enemy = CreateActor("ENEMY_0", BattleTeam.Enemy, new Vector3(1.4f, -2f, 0f), 1.5f, 2f); // 다른 세로 Lane 적군 생성

            Assert.That(ally.IsWithinAttackRange(enemy), Is.True); // 세로 연출 위치와 무관한 공격 사거리 검증
        }

        private BattleActor CreateActor(string runtimeId, BattleTeam team, Vector3 position, float attackRange, float moveSpeed) // 테스트 전투 객체 생성
        {
            GameObject actorObject = new GameObject(runtimeId); // 테스트 전투 객체 생성
            actorObject.transform.SetParent(rootObject.transform, false); // 테스트 루트 연결
            actorObject.transform.position = position; // 테스트 시작 위치 적용
            BattleActor actor = actorObject.AddComponent<BattleActor>(); // 전투 액터 추가
            actor.Initialize(team, new TestCombatantStats(runtimeId, attackRange, moveSpeed), position); // 테스트 전투 액터 초기화
            return actor; // 테스트 전투 액터 반환
        }

        private sealed class TestCombatantStats : IBattleCombatantStats // 테스트 전투 스탯
        {
            public string RuntimeId { get; } // 런타임 ID 반환
            public string DisplayName => RuntimeId; // 표시 이름 반환
            public int MaxHp => 100; // 최대 체력 반환
            public int CurrentHp => 100; // 현재 체력 반환
            public int Attack => 10; // 공격력 반환
            public int Defense => 5; // 방어력 반환
            public float AttackSpeed => 1f; // 공격속도 반환
            public float AttackRange { get; } // 공격 사거리 반환
            public float MoveSpeed { get; } // 이동속도 반환
            public bool IsAlive => true; // 생존 상태 반환

            public TestCombatantStats(string runtimeId, float attackRange, float moveSpeed) // 테스트 전투 스탯 생성
            {
                RuntimeId = runtimeId; // 런타임 ID 저장
                AttackRange = attackRange; // 공격 사거리 저장
                MoveSpeed = moveSpeed; // 이동속도 저장
            }
        }
    }
}
