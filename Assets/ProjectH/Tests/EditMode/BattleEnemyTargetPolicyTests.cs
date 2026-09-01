using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 적 AI 기능
using ProjectH.Data; // 몬스터 AI 유형
using UnityEngine; // Unity 게임 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleEnemyTargetPolicyTests // 적 AI 타겟 정책 테스트
    {
        private GameObject rootObject; // 테스트 루트 객체
        private BattleCombatRegistry registry; // 테스트 전투 레지스트리

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 테스트 전투 구조 준비
        {
            rootObject = new GameObject("BattleEnemyTargetPolicyTests"); // 테스트 루트 생성
            registry = rootObject.AddComponent<BattleCombatRegistry>(); // 테스트 레지스트리 추가
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 테스트 전투 구조 정리
        {
            Object.DestroyImmediate(rootObject); // 테스트 루트 제거
        }

        [Test] // 테스트 표시
        public void Normal_SelectsNearestLivingAlly() // 일반형 가장 가까운 아군 선택 검증
        {
            BattleActor enemy = CreateActor("ENEMY_0", BattleTeam.Enemy, new Vector3(4f, 0f, 0f)); // 일반형 적군 생성
            BattleActor front = CreateActor("ALLY_FRONT", BattleTeam.Ally, new Vector3(-1f, 0f, 0f)); // 전방 아군 생성
            CreateActor("ALLY_REAR", BattleTeam.Ally, new Vector3(-3f, 0f, 0f)); // 후열 아군 생성
            BattleActor selected = BattleEnemyTargetPolicy.SelectDesiredTarget(enemy, registry.Actors, EnemyAIType.Normal); // 일반형 희망 타겟 선택

            Assert.That(selected, Is.SameAs(front)); // 가장 가까운 아군 선택 검증
        }

        [Test] // 테스트 표시
        public void Rush_DesiresRearButFrontBlockerBecomesActualTarget() // 돌격형 후열 목표 및 전방 차단 검증
        {
            BattleActor enemy = CreateActor("ENEMY_1", BattleTeam.Enemy, new Vector3(4f, 0f, 0f)); // 돌격형 적군 생성
            BattleActor front = CreateActor("ALLY_FRONT", BattleTeam.Ally, new Vector3(-1f, 0f, 0f)); // 전방 차단 아군 생성
            BattleActor middle = CreateActor("ALLY_MIDDLE", BattleTeam.Ally, new Vector3(-2f, 0f, 0f)); // 중간 아군 생성
            BattleActor rear = CreateActor("ALLY_REAR", BattleTeam.Ally, new Vector3(-3f, 0f, 0f)); // 후열 아군 생성
            BattleActor desired = BattleEnemyTargetPolicy.SelectDesiredTarget(enemy, registry.Actors, EnemyAIType.Rush); // 돌격형 희망 타겟 선택
            BattleActor actual = BattleFrontBlockerResolver.Resolve(enemy, desired, registry.Actors); // 실제 전방 차단 타겟 계산

            Assert.That(desired, Is.SameAs(rear)); // 돌격형 후열 목표 검증
            Assert.That(actual, Is.SameAs(front)); // 전방 아군 통과 방지 검증
            Assert.That(actual, Is.Not.SameAs(middle)); // 중간 아군 우선 아님 검증
        }

        [Test] // 테스트 표시
        public void Ranged_UsesNearestTargetAndLetsAttackRangeControlDistance() // 원거리형 가까운 타겟 선택 검증
        {
            BattleActor enemy = CreateActor("ENEMY_2", BattleTeam.Enemy, new Vector3(4f, 0f, 0f)); // 원거리형 적군 생성
            BattleActor front = CreateActor("ALLY_FRONT", BattleTeam.Ally, new Vector3(-1f, 0f, 0f)); // 전방 아군 생성
            CreateActor("ALLY_REAR", BattleTeam.Ally, new Vector3(-3f, 0f, 0f)); // 후열 아군 생성
            BattleActor selected = BattleEnemyTargetPolicy.SelectDesiredTarget(enemy, registry.Actors, EnemyAIType.Ranged); // 원거리형 희망 타겟 선택

            Assert.That(selected, Is.SameAs(front)); // 원거리형 전선 타겟 선택 검증
        }

        private BattleActor CreateActor(string runtimeId, BattleTeam team, Vector3 position) // 테스트 전투 객체 생성
        {
            GameObject actorObject = new GameObject(runtimeId); // 전투 객체 생성
            actorObject.transform.SetParent(rootObject.transform, false); // 테스트 루트 연결
            actorObject.transform.position = position; // 전투 객체 위치 적용
            BattleActor actor = actorObject.AddComponent<BattleActor>(); // 전투 액터 추가
            actor.Initialize(team, new TestStats(runtimeId), position); // 테스트 전투 액터 초기화
            registry.Register(actor); // 테스트 전투 레지스트리 등록
            return actor; // 전투 액터 반환
        }

        private sealed class TestStats : IBattleMutableCombatantStats // 테스트 전투 스탯
        {
            public string RuntimeId { get; } // 런타임 ID 반환
            public string DisplayName => RuntimeId; // 표시 이름 반환
            public int MaxHp => 100; // 최대 체력 반환
            public int CurrentHp { get; private set; } = 100; // 현재 체력 반환
            public int Attack => 10; // 공격력 반환
            public int Defense => 5; // 방어력 반환
            public float AttackSpeed => 1f; // 공격속도 반환
            public float AttackRange => 1.5f; // 공격 사거리 반환
            public float MoveSpeed => 2f; // 이동속도 반환
            public bool IsAlive => CurrentHp > 0; // 생존 상태 반환

            public TestStats(string runtimeId) // 테스트 전투 스탯 생성
            {
                RuntimeId = runtimeId; // 런타임 ID 저장
            }

            public int TakeDamage(int amount) // 테스트 피해 적용
            {
                int before = CurrentHp; // 피해 전 체력 저장
                CurrentHp = Mathf.Max(0, CurrentHp - Mathf.Max(0, amount)); // 테스트 체력 감소
                return before - CurrentHp; // 실제 피해량 반환
            }

            public int Heal(int amount) // 테스트 회복 적용
            {
                int before = CurrentHp; // 회복 전 체력 저장
                CurrentHp = Mathf.Min(MaxHp, CurrentHp + Mathf.Max(0, amount)); // 테스트 체력 회복
                return CurrentHp - before; // 실제 회복량 반환
            }
        }
    }
}
