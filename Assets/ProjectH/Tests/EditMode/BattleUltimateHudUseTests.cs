using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 궁극기 HUD 기능
using ProjectH.Data; // 전투 포지션 기능
using UnityEngine; // Unity 게임 오브젝트 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleUltimateHudUseTests // 궁극기 HUD 입력 연동 테스트
    {
        private GameObject registryObject; // 테스트 Registry 객체
        private GameObject cardObject; // 테스트 HUD 카드 객체
        private GameObject ultimateTextObject; // 테스트 궁극기 텍스트 객체
        private BattleCombatRegistry registry; // 테스트 전투 Registry
        private BattleHudCardView cardView; // 테스트 HUD 카드 View
        private BattleActor owner; // 테스트 궁극기 사용자
        private BattleActor enemy; // 전투 진행용 적군

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 궁극기 HUD 입력 테스트 준비
        {
            BattleRuntimeStates.ResetAll(); // 전투 정적 상태 전체 초기화 (최적화 통합 창구)
            registryObject = new GameObject("BattleUltimateHudUseTests.Registry"); // 테스트 Registry 객체 생성
            registry = registryObject.AddComponent<BattleCombatRegistry>(); // 테스트 Registry 생성
            BattleSkillRuntimeState.SetRegistry(registry); // 테스트 스킬 Runtime Registry 연결
            BattlePassiveSystem.Initialize(registry); // 테스트 패시브 시스템 Registry 연결
            owner = CreateAlly("ALLY_0", "CH_EVE"); // 이브 테스트 사용자 생성
            enemy = CreateEnemy("ENEMY_0"); // 전투 진행용 적 생성
            cardObject = new GameObject("BattleUltimateHudUseTests.Card"); // 테스트 HUD 카드 객체 생성
            cardView = cardObject.AddComponent<BattleHudCardView>(); // 테스트 HUD 카드 View 추가
            ultimateTextObject = new GameObject("UltimateText"); // 궁극기 텍스트 객체 생성
            ultimateTextObject.transform.SetParent(cardObject.transform); // 궁극기 텍스트 HUD 하위 배치
            Text ultimateText = ultimateTextObject.AddComponent<Text>(); // 궁극기 텍스트 컴포넌트 추가
            cardView.ConfigureExtended(null, null, null, null, null, null, null, null, null, ultimateText); // 궁극기 텍스트 참조 연결
            cardView.Bind(owner.Stats as BattleStats); // 이브 전투 스탯 HUD 연결
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 궁극기 HUD 입력 테스트 정리
        {
            Object.DestroyImmediate(cardObject); // 테스트 HUD 카드 객체 제거
            Object.DestroyImmediate(owner.gameObject); // 테스트 사용자 객체 제거
            Object.DestroyImmediate(enemy.gameObject); // 테스트 적군 객체 제거
            BattleRuntimeStates.ResetAll(); // 전투 정적 상태 전체 초기화 (최적화 통합 창구)
            BattlePassiveSystem.Shutdown(registry); // 테스트 패시브 시스템 연결 해제
            BattlePassiveRuntimeState.ResetAll(); // 테스트 패시브 Runtime 초기화
            Object.DestroyImmediate(registryObject); // 테스트 Registry 객체 제거
        }

        [Test] // 테스트 표시
        public void FullGauge_EnablesUltimateInputAndConsumesGaugeOnUse() // Ready 입력 활성과 게이지 소비 검증
        {
            BattleUltimateGaugeRuntimeState.AddGauge("CH_EVE", 100); // 이브 게이지 완충

            bool used = cardView.TryUseUltimate(); // HUD 궁극기 사용 실행

            Assert.That(cardView.IsUltimateButtonInteractable, Is.False); // 사용 후 궁극기 버튼 비활성 검증
            Assert.That(used, Is.True); // HUD 궁극기 실행 성공 검증
            Assert.That(BattleUltimateGaugeRuntimeState.GetGauge("CH_EVE"), Is.EqualTo(0)); // HUD 사용 후 게이지 0 검증
        }

        private BattleActor CreateAlly(string runtimeId, string characterId) // 아군 테스트 액터 생성
        {
            GameObject targetObject = new GameObject(runtimeId); // 아군 테스트 객체 생성
            BattleActor actor = targetObject.AddComponent<BattleActor>(); // 아군 전투 액터 추가
            BattleStats stats = new BattleStats(runtimeId, characterId, characterId, BattlePosition.Dealer, 1, 1000, 100, 10, 1f, 1f, 0f); // 아군 전투 스탯 생성
            actor.Initialize(BattleTeam.Ally, stats, Vector3.zero); // 아군 전투 액터 초기화
            registry.Register(actor); // 아군 Registry 등록
            return actor; // 생성 아군 액터 반환
        }

        private BattleActor CreateEnemy(string runtimeId) // 적군 테스트 액터 생성
        {
            GameObject targetObject = new GameObject(runtimeId); // 적군 테스트 객체 생성
            BattleActor actor = targetObject.AddComponent<BattleActor>(); // 적군 전투 액터 추가
            BattleEnemyStats stats = new BattleEnemyStats(runtimeId, "MON_TEST", runtimeId, 1000, 10, 0, 0, 1f, 1f, 1f); // 적군 전투 스탯 생성
            actor.Initialize(BattleTeam.Enemy, stats, Vector3.right); // 적군 전투 액터 초기화
            registry.Register(actor); // 적군 Registry 등록
            return actor; // 생성 적군 액터 반환
        }
    }
}
