using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 17일차 HUD 배치 기능
using UnityEngine; // Unity UI 좌표 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleDay17HudLayoutTests // 17일차 HUD 분할 배치 테스트
    {
        [Test] // 테스트 표시
        public void ProfileCards_StayInBottomLeftWithoutOverlap() // 프로필 4개 왼쪽 하단 비중첩 검증
        {
            Rect previous = default; // 이전 카드 Rect 초기화

            for (int index = 0; index < BattleDay17HudLayout.ProfileCardCount; index++) // 4개 프로필 카드 순회
            {
                Rect current = BattleDay17HudLayout.GetProfileCardAnchorRect(index); // 현재 카드 앵커 Rect 조회
                Assert.That(current.xMin, Is.GreaterThanOrEqualTo(0f)); // 카드 화면 왼쪽 범위 검증
                Assert.That(current.xMax, Is.LessThanOrEqualTo(BattleDay17HudLayout.ProfileAreaMaxX)); // 카드 왼쪽 영역 내부 검증
                Assert.That(current.yMin, Is.GreaterThanOrEqualTo(0f)); // 카드 하단 범위 검증
                Assert.That(current.yMax, Is.LessThanOrEqualTo(0.25f)); // 카드 하단 영역 높이 검증

                if (index > 0) // 둘째 이후 카드 확인
                {
                    Assert.That(current.xMin, Is.GreaterThan(previous.xMax)); // 이전 카드와 가로 비중첩 검증
                }

                previous = current; // 현재 카드를 다음 비교 기준으로 저장
            }
        }

        [Test] // 테스트 표시
        public void SkillPanel_StaysInBottomRightAndDoesNotOverlapProfiles() // 스킬 패널 오른쪽 하단 분리 검증
        {
            Rect skillRect = BattleDay17HudLayout.SkillPanelAnchorRect; // 스킬 패널 앵커 Rect 조회
            Rect lastProfile = BattleDay17HudLayout.GetProfileCardAnchorRect(BattleDay17HudLayout.ProfileCardCount - 1); // 마지막 프로필 카드 Rect 조회

            Assert.That(skillRect.xMin, Is.GreaterThan(lastProfile.xMax)); // 프로필 영역과 스킬 패널 비중첩 검증
            Assert.That(skillRect.xMin, Is.GreaterThanOrEqualTo(BattleDay17HudLayout.SkillAreaMinX)); // 스킬 패널 오른쪽 영역 시작 검증
            Assert.That(skillRect.xMax, Is.LessThanOrEqualTo(1f)); // 스킬 패널 화면 오른쪽 범위 검증
            Assert.That(skillRect.yMin, Is.GreaterThanOrEqualTo(0f)); // 스킬 패널 화면 하단 범위 검증
            Assert.That(skillRect.yMax, Is.LessThanOrEqualTo(0.25f)); // 스킬 패널 하단 영역 높이 검증
        }
    }
}
