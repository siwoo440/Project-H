using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 공통 사망 처리 기능
using ProjectH.Data; // 전투 포지션 기능
using UnityEngine; // Unity 게임 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleDeathHandlerTests // 공통 전투 사망 처리 테스트
    {
        private GameObject rootObject; // 테스트 루트 객체
        private BattleCombatRegistry registry; // 테스트 전투 레지스트리

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 테스트 사망 처리 구조 준비
        {
            rootObject = new GameObject("BattleDeathHandlerTests"); // 테스트 루트 생성
            registry = rootObject.AddComponent<BattleCombatRegistry>(); // 테스트 레지스트리 추가
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 테스트 사망 처리 구조 정리
        {
            Object.DestroyImmediate(rootObject); // 테스트 루트 제거
        }

        [Test] // 테스트 표시
        public void AllyDeath_UnregistersStopsAttackAndHidesWorldObject() // 아군 사망 공통 처리 검증
        {
            GameObject allyObject = new GameObject("ALLY_0"); // 테스트 아군 객체 생성
            allyObject.transform.SetParent(rootObject.transform, false); // 테스트 루트 연결
            BattleActor actor = allyObject.AddComponent<BattleActor>(); // 아군 전투 액터 추가
            BattleBasicAttackController attackController = allyObject.AddComponent<BattleBasicAttackController>(); // 아군 공격 컨트롤러 추가
            BattleDeathHandler deathHandler = allyObject.AddComponent<BattleDeathHandler>(); // 공통 사망 처리기 추가
            BattleStats stats = new BattleStats("ALLY_0", "CH_TEST", "TEST", BattlePosition.Dealer, 1, 10, 5, 1, 1f, 1f, 0f); // 테스트 아군 전투 스탯 생성
            actor.Initialize(BattleTeam.Ally, stats, Vector3.zero); // 아군 전투 액터 초기화
            registry.Register(actor); // 아군 전투 레지스트리 등록
            attackController.Configure(actor, registry); // 아군 기본 공격 참조 연결
            deathHandler.Configure(actor, registry, attackController, null, null, null); // 공통 사망 처리 참조 연결
            stats.TakeDamage(999); // 아군 전투 불능 처리

            Assert.That(stats.IsAlive, Is.False); // 아군 전투 불능 상태 검증
            Assert.That(registry.Contains(actor), Is.False); // 사망 아군 Registry 제외 검증
            Assert.That(attackController.enabled, Is.False); // 사망 아군 공격 중지 검증
            Assert.That(allyObject.activeSelf, Is.False); // EditMode 사망 아군 즉시 숨김 검증
        }
    }
}
