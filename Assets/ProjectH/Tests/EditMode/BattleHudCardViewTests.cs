using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 HUD 카드 기능
using ProjectH.Data; // 캐릭터 포지션 기능
using UnityEngine; // Unity 게임 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleHudCardViewTests // HUD 카드 Runtime 상태 테스트
    {
        private GameObject rootObject; // 테스트 HUD 객체
        private BattleHudCardView cardView; // 테스트 HUD 카드
        private BattleStats stats; // 테스트 전투 스탯

        [SetUp] // 테스트 준비 표시
        public void SetUp() // HUD 카드 테스트 준비
        {
            rootObject = new GameObject("BattleHudCardViewTests"); // 테스트 HUD 객체 생성
            cardView = rootObject.AddComponent<BattleHudCardView>(); // HUD 카드 View 추가
            stats = new BattleStats("ALLY_0", "CH_TEST", "TEST", BattlePosition.Dealer, 1, 100, 10, 5, 1f, 1f, 0f); // 테스트 전투 스탯 생성
            cardView.Bind(stats); // HUD 카드 전투 스탯 연결
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // HUD 카드 테스트 정리
        {
            Object.DestroyImmediate(rootObject); // 테스트 HUD 객체 제거
        }

        [Test] // 테스트 표시
        public void Bind_StartsWithZeroUltimateAndNormalHealth() // 초기 HUD 상태 검증
        {
            Assert.That(cardView.UltimateRatio, Is.EqualTo(0f)); // 초기 궁극기 게이지 0 검증
            Assert.That(cardView.HealthState, Is.EqualTo(BattleHudHealthState.Normal)); // 초기 정상 체력 상태 검증
        }

        [Test] // 테스트 표시
        public void SetUltimatePreview_ClampsRatio() // 궁극기 게이지 범위 보정 검증
        {
            cardView.SetUltimatePreview(1.5f); // 최대 초과 궁극기 게이지 적용
            Assert.That(cardView.UltimateRatio, Is.EqualTo(1f)); // 궁극기 최대 1 보정 검증
            cardView.SetUltimatePreview(-1f); // 최소 미만 궁극기 게이지 적용
            Assert.That(cardView.UltimateRatio, Is.EqualTo(0f)); // 궁극기 최소 0 보정 검증
        }

        [Test] // 테스트 표시
        public void HealthChanged_UpdatesHealthState() // 체력 변경 HUD 상태 갱신 검증
        {
            stats.SetCurrentHp(20); // 위험 체력 적용
            Assert.That(cardView.HealthState, Is.EqualTo(BattleHudHealthState.Danger)); // 위험 체력 상태 검증
            stats.SetCurrentHp(0); // 전투 불능 체력 적용
            Assert.That(cardView.HealthState, Is.EqualTo(BattleHudHealthState.Down)); // 전투 불능 상태 검증
        }
    }
}
