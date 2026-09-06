using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 HUD와 궁극기 게이지 기능
using ProjectH.Data; // 캐릭터 포지션 기능
using UnityEngine; // Unity 게임 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleUltimateGaugeHudTests // 궁극기 게이지 HUD Runtime 연동 테스트
    {
        private GameObject rootObject; // 테스트 HUD 객체
        private BattleHudCardView cardView; // 테스트 HUD 카드
        private BattleStats stats; // 테스트 전투 스탯

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 궁극기 HUD 테스트 준비
        {
            BattleUltimateGaugeRuntimeState.ResetAll(); // 이전 테스트 궁극기 게이지 초기화
            rootObject = new GameObject("BattleUltimateGaugeHudTests"); // 테스트 HUD 객체 생성
            cardView = rootObject.AddComponent<BattleHudCardView>(); // HUD 카드 View 추가
            stats = new BattleStats("ALLY_0", "CH_TEST", "TEST", BattlePosition.Dealer, 1, 100, 10, 5, 1f, 1f, 0f); // 테스트 전투 스탯 생성
            cardView.Bind(stats); // HUD 카드 전투 스탯 연결
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 궁극기 HUD 테스트 정리
        {
            Object.DestroyImmediate(rootObject); // 테스트 HUD 객체 제거
            BattleUltimateGaugeRuntimeState.ResetAll(); // 테스트 종료 궁극기 게이지 초기화
        }

        [Test] // 테스트 표시
        public void GaugeChanged_RefreshesCardRatioAndReadyState() // Runtime 게이지 HUD 갱신 검증
        {
            BattleUltimateGaugeRuntimeState.AddGauge("CH_TEST", 100); // 연결 캐릭터 게이지 완충

            Assert.That(cardView.UltimateRatio, Is.EqualTo(1f)); // HUD 게이지 100퍼센트 검증
            Assert.That(cardView.IsUltimateReady, Is.True); // 생존 캐릭터 Ready 상태 검증
        }

        [Test] // 테스트 표시
        public void ResetAll_RefreshesBoundCardToZero() // 전투 재진입 초기화 HUD 반영 검증
        {
            BattleUltimateGaugeRuntimeState.AddGauge("CH_TEST", 70); // 연결 캐릭터 게이지 충전
            BattleUltimateGaugeRuntimeState.ResetAll(); // 신규 전투 전체 게이지 초기화

            Assert.That(cardView.UltimateRatio, Is.EqualTo(0f)); // HUD 게이지 초기화 반영 검증
            Assert.That(cardView.IsUltimateReady, Is.False); // 초기화 후 Ready 비활성 검증
        }

        [Test] // 테스트 표시
        public void DeadCharacter_IsNotReadyEvenWithFullGauge() // 사망 캐릭터 Ready 비활성 검증
        {
            BattleUltimateGaugeRuntimeState.AddGauge("CH_TEST", 100); // 연결 캐릭터 게이지 완충
            stats.SetCurrentHp(0); // 연결 캐릭터 전투 불능 처리

            Assert.That(cardView.UltimateRatio, Is.EqualTo(1f)); // 사망 후 게이지 값 유지 검증
            Assert.That(cardView.IsUltimateReady, Is.False); // 사망 후 Ready 비활성 검증
        }
    }
}
