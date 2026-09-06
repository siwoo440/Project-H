using System.Collections.Generic; // 테스트 객체 목록 기능
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 수동 이동 기능
using ProjectH.Data; // 캐릭터 포지션 기능
using UnityEngine; // Unity 게임 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleManualMoveControllerTests // 숫자키 선택 및 우클릭 수동 이동 테스트
    {
        private readonly List<GameObject> spawnedObjects = new List<GameObject>(); // 테스트 생성 객체 목록
        private GameObject registryObject; // 테스트 Registry 객체
        private BattleCombatRegistry registry; // 테스트 전투 Registry

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 수동 이동 테스트 준비
        {
            BattleSkillRuntimeState.ResetAll(); // 이전 스킬 Runtime 상태 초기화
            registryObject = new GameObject("BattleManualMoveControllerTests.Registry"); // 테스트 Registry 객체 생성
            spawnedObjects.Add(registryObject); // 테스트 정리 목록 등록
            registry = registryObject.AddComponent<BattleCombatRegistry>(); // 테스트 Registry 컴포넌트 생성
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 수동 이동 테스트 객체 정리
        {
            BattleSkillRuntimeState.ResetAll(); // 테스트 스킬 Runtime 상태 초기화

            for (int index = spawnedObjects.Count - 1; index >= 0; index--) // 생성 객체 역순 순회
            {
                if (spawnedObjects[index] != null) // 현재 테스트 객체 존재 확인
                {
                    Object.DestroyImmediate(spawnedObjects[index]); // 현재 테스트 객체 즉시 제거
                }
            }

            spawnedObjects.Clear(); // 테스트 객체 목록 초기화
        }

        [Test] // 테스트 표시
        public void RegisterAllies_NumberKeysFollowPartyCreationOrder() // 파티 생성 순서 숫자 슬롯 등록 검증
        {
            BattleActor first = CreateAlly("ALLY_0", "CH_SERENA"); // 1번 아군 생성
            BattleActor second = CreateAlly("ALLY_1", "CH_ELLEN"); // 2번 아군 생성
            BattleActor third = CreateAlly("ALLY_2", "CH_LILIA"); // 3번 아군 생성
            BattleActor fourth = CreateAlly("ALLY_3", "CH_EVE"); // 4번 아군 생성
            BattleManualMoveController manual = registryObject.GetComponent<BattleManualMoveController>(); // 자동 생성 수동 이동 컨트롤러 조회

            Assert.That(manual, Is.Not.Null); // 수동 이동 컨트롤러 자동 생성 검증
            Assert.That(manual.TrySelectSlot(3), Is.True); // 3번 슬롯 선택 성공 검증
            Assert.That(manual.SelectedSlotNumber, Is.EqualTo(3)); // 선택 슬롯 번호 검증
            Assert.That(manual.SelectedActor, Is.SameAs(third)); // 3번 파티 캐릭터 연결 검증
            Assert.That(first, Is.Not.Null); // 1번 생성 유지 검증
            Assert.That(second, Is.Not.Null); // 2번 생성 유지 검증
            Assert.That(fourth, Is.Not.Null); // 4번 생성 유지 검증
        }

        [Test] // 테스트 표시
        public void ManualMove_ShowsDestinationArrowUntilArrival() // 수동 이동 목적지 화살표 표시와 도착 후 숨김 검증
        {
            BattleActor actor = CreateAlly("ALLY_0", "CH_SERENA"); // 화살표 테스트 아군 생성
            BattleManualMoveController manual = registryObject.GetComponent<BattleManualMoveController>(); // 수동 이동 컨트롤러 조회
            Vector3 target = new Vector3(3f, 4f, actor.transform.position.z); // 화살표 표시 목적지 생성

            Assert.That(manual.TrySelectSlot(1), Is.True); // 1번 캐릭터 선택 검증
            Assert.That(manual.TryIssueMove(target), Is.True); // 목적지 이동 명령 검증
            Assert.That(manual.IsDestinationMarkerVisible(actor), Is.True); // 이동 중 목적지 화살표 표시 검증
            manual.TickManualMoves(10f); // 목적지까지 충분한 이동 시간 처리

            Assert.That(manual.IsDestinationMarkerVisible(actor), Is.False); // 목적지 도착 후 화살표 숨김 검증
        }

        [Test] // 테스트 표시
        public void ManualMove_ReachesTwoDimensionalDestinationAndResumesAuto() // X/Y 수동 이동 후 자동 전투 복귀 검증
        {
            BattleActor actor = CreateAlly("ALLY_0", "CH_SERENA"); // 수동 이동 아군 생성
            BattleBasicAttackController attackController = actor.GetComponent<BattleBasicAttackController>(); // 자동 공격 컨트롤러 조회
            BattleManualMoveController manual = registryObject.GetComponent<BattleManualMoveController>(); // 수동 이동 컨트롤러 조회
            Vector3 target = new Vector3(3f, 4f, actor.transform.position.z); // 2D 수동 이동 목적지 생성

            Assert.That(manual.TrySelectSlot(1), Is.True); // 1번 캐릭터 선택 검증
            Assert.That(manual.TryIssueMove(target), Is.True); // 우클릭 대체 이동 명령 검증
            Assert.That(attackController.IsManualMoveSuspended, Is.True); // 이동 중 자동 전투 정지 검증
            manual.TickManualMoves(10f); // 충분한 시간 수동 이동 처리

            Assert.That(actor.transform.position.x, Is.EqualTo(target.x).Within(0.001f)); // 목적지 X 도착 검증
            Assert.That(actor.transform.position.y, Is.EqualTo(target.y).Within(0.001f)); // 목적지 Y 도착 검증
            Assert.That(manual.IsMoving(actor), Is.False); // 도착 후 수동 이동 종료 검증
            Assert.That(attackController.IsManualMoveSuspended, Is.False); // 도착 후 자동 전투 복귀 검증
            Assert.That(attackController.State, Is.EqualTo(BattleAttackState.Idle)); // 복귀 후 새 타겟 탐색 대기 상태 검증
        }

        [Test] // 테스트 표시
        public void ManualMove_MultipleAlliesKeepIndependentDestinations() // 여러 아군 독립 수동 이동 검증
        {
            BattleActor first = CreateAlly("ALLY_0", "CH_SERENA"); // 첫 번째 수동 이동 아군 생성
            BattleActor second = CreateAlly("ALLY_1", "CH_ELLEN"); // 두 번째 수동 이동 아군 생성
            BattleManualMoveController manual = registryObject.GetComponent<BattleManualMoveController>(); // 수동 이동 컨트롤러 조회
            Vector3 firstTarget = new Vector3(-2f, 1f, first.transform.position.z); // 첫 번째 아군 목적지 생성
            Vector3 secondTarget = new Vector3(2f, -1f, second.transform.position.z); // 두 번째 아군 목적지 생성

            Assert.That(manual.TrySelectSlot(1), Is.True); // 1번 아군 선택 검증
            Assert.That(manual.TryIssueMove(firstTarget), Is.True); // 1번 아군 이동 명령 검증
            Assert.That(manual.TrySelectSlot(2), Is.True); // 2번 아군 선택 검증
            Assert.That(manual.TryIssueMove(secondTarget), Is.True); // 2번 아군 이동 명령 검증
            Assert.That(manual.IsMoving(first), Is.True); // 1번 이동 유지 검증
            Assert.That(manual.IsMoving(second), Is.True); // 2번 이동 유지 검증
            Assert.That(manual.IsDestinationMarkerVisible(first), Is.True); // 1번 목적지 화살표 표시 검증
            Assert.That(manual.IsDestinationMarkerVisible(second), Is.True); // 2번 목적지 화살표 표시 검증
            manual.TickManualMoves(10f); // 전체 수동 이동 충분 시간 처리

            Assert.That(first.transform.position.x, Is.EqualTo(firstTarget.x).Within(0.001f)); // 1번 목적지 X 검증
            Assert.That(first.transform.position.y, Is.EqualTo(firstTarget.y).Within(0.001f)); // 1번 목적지 Y 검증
            Assert.That(second.transform.position.x, Is.EqualTo(secondTarget.x).Within(0.001f)); // 2번 목적지 X 검증
            Assert.That(second.transform.position.y, Is.EqualTo(secondTarget.y).Within(0.001f)); // 2번 목적지 Y 검증
        }

        [Test] // 테스트 표시
        public void ManualMove_DeadActorCancelsMovementAndUnlocksAutoController() // 이동 중 사망 시 수동 이동 취소 검증
        {
            BattleActor actor = CreateAlly("ALLY_0", "CH_EVE"); // 사망 취소 테스트 아군 생성
            BattleStats stats = actor.Stats as BattleStats; // 테스트 아군 스탯 변환
            BattleBasicAttackController attackController = actor.GetComponent<BattleBasicAttackController>(); // 자동 공격 컨트롤러 조회
            BattleManualMoveController manual = registryObject.GetComponent<BattleManualMoveController>(); // 수동 이동 컨트롤러 조회

            Assert.That(manual.TrySelectSlot(1), Is.True); // 1번 아군 선택 검증
            Assert.That(manual.TryIssueMove(new Vector3(5f, 0f, 0f)), Is.True); // 수동 이동 시작 검증
            stats.SetCurrentHp(0); // 이동 중 전투 불능 처리
            manual.TickManualMoves(0.1f); // 사망 상태 수동 이동 갱신

            Assert.That(manual.IsMoving(actor), Is.False); // 사망 후 수동 이동 취소 검증
            Assert.That(manual.IsDestinationMarkerVisible(actor), Is.False); // 사망 취소 후 목적지 화살표 숨김 검증
            Assert.That(attackController.IsManualMoveSuspended, Is.False); // 사망 후 자동 컨트롤러 잠금 해제 검증
            Assert.That(manual.TrySelectSlot(1), Is.False); // 사망 캐릭터 재선택 차단 검증
        }

        private BattleActor CreateAlly(string runtimeId, string characterId) // 테스트 아군 생성
        {
            GameObject targetObject = new GameObject(runtimeId); // 테스트 아군 GameObject 생성
            spawnedObjects.Add(targetObject); // 테스트 정리 목록 등록
            BattleActor actor = targetObject.AddComponent<BattleActor>(); // 테스트 전투 액터 추가
            BattleStats stats = new BattleStats(runtimeId, characterId, characterId, BattlePosition.Dealer, 1, 1000, 100, 50, 1f, 1f, 0f); // 테스트 전투 스탯 생성
            actor.Initialize(BattleTeam.Ally, stats, Vector3.zero); // 테스트 아군 액터 초기화
            BattleBasicAttackController attackController = targetObject.AddComponent<BattleBasicAttackController>(); // 테스트 자동 공격 컨트롤러 추가
            attackController.Configure(actor, registry); // 테스트 자동 공격 및 수동 이동 연결
            registry.Register(actor); // 테스트 아군 Registry 등록
            return actor; // 생성 테스트 아군 반환
        }
    }
}
