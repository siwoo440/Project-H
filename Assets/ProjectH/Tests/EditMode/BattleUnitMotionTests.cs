using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 유닛 움직임 기능
using UnityEngine; // 캔버스·좌표 기능
using UnityEngine.UI; // Image 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleUnitMotionTests // Day79 전투 유닛 움직임 계산 테스트
    {
        [Test] // 한 번 튀는 움직임 : 0에서 시작해 정점 1을 찍고 0으로 돌아온다
        public void Pulse_RisesToOneAndReturnsToZero() // 튀는 값 테스트
        {
            float outSeconds = BattleUnitMotionMath.LungeOutSeconds; // 나가는 시간
            float backSeconds = BattleUnitMotionMath.LungeBackSeconds; // 돌아오는 시간
            Assert.That(BattleUnitMotionMath.Pulse(-1f, outSeconds, backSeconds), Is.EqualTo(0f)); // 시작 전
            Assert.That(BattleUnitMotionMath.Pulse(0f, outSeconds, backSeconds), Is.EqualTo(0f)); // 시작
            Assert.That(BattleUnitMotionMath.Pulse(outSeconds * 0.5f, outSeconds, backSeconds), Is.EqualTo(0.5f).Within(0.0001f)); // 나가는 중간
            Assert.That(BattleUnitMotionMath.Pulse(outSeconds, outSeconds, backSeconds), Is.EqualTo(1f).Within(0.0001f)); // 정점
            Assert.That(BattleUnitMotionMath.Pulse(outSeconds + (backSeconds * 0.5f), outSeconds, backSeconds), Is.EqualTo(0.5f).Within(0.0001f)); // 돌아오는 중간
            Assert.That(BattleUnitMotionMath.Pulse(outSeconds + backSeconds, outSeconds, backSeconds), Is.EqualTo(0f).Within(0.0001f)); // 복귀
            Assert.That(BattleUnitMotionMath.Pulse(10f, outSeconds, backSeconds), Is.EqualTo(0f)); // 한참 뒤

            float previous = 1f; // 직전 값

            for (int step = 1; step <= 10; step++) // 돌아오는 구간을 10등분
            {
                float value = BattleUnitMotionMath.Pulse(outSeconds + (backSeconds * step / 10f), outSeconds, backSeconds); // 현재 값
                Assert.That(value, Is.LessThanOrEqualTo(previous + 0.0001f), "돌아오는 중에 다시 튀어나감"); // 줄어들기만 함
                previous = value; // 갱신
            }

            float total = outSeconds + backSeconds; // 전체 시간
            Assert.That(BattleUnitMotionMath.IsPulseDone(total + 0.001f, outSeconds, backSeconds), Is.True); // 끝난 직후 (정확한 경계값은 소수 오차에 따라 달라져 비교하지 않는다)
            Assert.That(BattleUnitMotionMath.IsPulseDone(total - 0.001f, outSeconds, backSeconds), Is.False); // 끝나기 직전
            Assert.That(BattleUnitMotionMath.IsPulseDone(outSeconds, outSeconds, backSeconds), Is.False); // 정점
            Assert.That(BattleUnitMotionMath.IsPulseDone(-1f, outSeconds, backSeconds), Is.False); // 시작 전
        }

        [Test] // 대기 오르내림은 정해 둔 폭을 넘지 않고, 실제로 위아래로 움직인다
        public void Idle_StaysWithinAmplitude() // 대기 움직임 테스트
        {
            float min = float.MaxValue; // 가장 낮은 값
            float max = float.MinValue; // 가장 높은 값

            for (int step = 0; step < 400; step++) // 4초를 0.01초 간격으로
            {
                float value = BattleUnitMotionMath.Idle(step * 0.01f, 1.3f); // 오르내림
                min = Mathf.Min(min, value); // 최저
                max = Mathf.Max(max, value); // 최고
            }

            Assert.That(max, Is.LessThanOrEqualTo(BattleUnitMotionMath.IdleAmplitude + 0.0001f)); // 폭 이내
            Assert.That(min, Is.GreaterThanOrEqualTo(-BattleUnitMotionMath.IdleAmplitude - 0.0001f)); // 폭 이내
            Assert.That(max - min, Is.GreaterThan(BattleUnitMotionMath.IdleAmplitude)); // 실제로 움직임
        }

        [Test] // 유닛마다 박자가 다르고, 같은 유닛은 늘 같은 박자다
        public void Phase_IsStablePerUnitAndDiffersBetweenUnits() // 박자 테스트
        {
            float first = BattleUnitMotionMath.GetPhase("ALLY_0"); // 첫 유닛
            float second = BattleUnitMotionMath.GetPhase("ALLY_1"); // 둘째 유닛
            Assert.That(BattleUnitMotionMath.GetPhase("ALLY_0"), Is.EqualTo(first)); // 같은 유닛은 같은 값
            Assert.That(second, Is.Not.EqualTo(first)); // 다른 유닛은 다른 값
            Assert.That(first, Is.InRange(0f, 6.29f)); // 한 바퀴 안
            Assert.That(second, Is.InRange(0f, 6.29f)); // 한 바퀴 안
            Assert.That(BattleUnitMotionMath.GetPhase(null), Is.EqualTo(0f)); // null 안전
        }

        [Test] // 쓰러짐 진행률과 설정 배율
        public void DefeatProgressAndMotionScale() // 쓰러짐 · 배율 테스트
        {
            Assert.That(BattleUnitMotionMath.GetDefeatProgress(-1f), Is.EqualTo(0f)); // 시작 전
            Assert.That(BattleUnitMotionMath.GetDefeatProgress(BattleUnitMotionMath.DefeatSeconds * 0.5f), Is.EqualTo(0.5f).Within(0.0001f)); // 중간
            Assert.That(BattleUnitMotionMath.GetDefeatProgress(99f), Is.EqualTo(1f)); // 끝
            Assert.That(BattleUnitMotionMath.DefeatSeconds, Is.LessThanOrEqualTo(0.35f)); // 전장에서 사라지기 전에 끝남
            Assert.That(BattleUnitMotionMath.GetMotionScale(false), Is.EqualTo(1f)); // 보통
            Assert.That(BattleUnitMotionMath.GetMotionScale(true), Is.InRange(0f, 0.99f)); // 흔들림 줄이기
        }

        [Test] // 겹침 순서 : 화면 아래쪽 유닛이 앞이고, 전투 배경(-20)과 HUD(20) 사이를 벗어나지 않는다
        public void SortingOrder_PutsLowerUnitsInFront() // 겹침 순서 테스트
        {
            Assert.That(BattleUnitMotionMath.GetSortingOrder(-1.1f), Is.GreaterThan(BattleUnitMotionMath.GetSortingOrder(0.05f))); // 아래쪽이 앞
            Assert.That(BattleUnitMotionMath.GetSortingOrder(0.05f), Is.GreaterThan(BattleUnitMotionMath.GetSortingOrder(1.85f))); // 위쪽이 뒤

            foreach (float worldY in new[] { -50f, -5f, -1.1f, 0f, 1.85f, 5f, 50f }) // 화면 안팎의 높이
            {
                int order = BattleUnitMotionMath.GetSortingOrder(worldY); // 겹침 순서
                Assert.That(order, Is.InRange(BattleUnitMotionMath.MinSortingOrder, BattleUnitMotionMath.MaxSortingOrder)); // 범위 안
                Assert.That(order, Is.GreaterThan(-20).And.LessThan(20)); // 배경 위 · HUD 아래
            }
        }

        [Test] // 움직임 컴포넌트 : 연결하면 겹침 순서가 정해지고, 쓰러진 뒤에는 공격 움직임을 받지 않으며, 초기화하면 자세가 돌아온다
        public void Motion_ConfigureDefeatAndReset() // 움직임 컴포넌트 테스트
        {
            GameObject unit = new GameObject("Unit", typeof(BattleUnitMotion)); // 유닛 객체
            GameObject canvasObject = new GameObject("UnitCanvas", typeof(RectTransform), typeof(Canvas)); // 유닛 캔버스
            GameObject bodyObject = new GameObject("Body", typeof(RectTransform), typeof(Image)); // 바디
            canvasObject.transform.SetParent(unit.transform, false); // 유닛 하위
            bodyObject.transform.SetParent(canvasObject.transform, false); // 캔버스 하위

            try // 정리 보장
            {
                unit.transform.position = new Vector3(-3f, -1.1f, 0f); // 화면 아래쪽 자리
                BattleUnitMotion motion = unit.GetComponent<BattleUnitMotion>(); // 움직임
                RectTransform body = (RectTransform)bodyObject.transform; // 바디 영역
                motion.Configure(body, 1f, "ALLY_1"); // 연결
                Assert.That(canvasObject.GetComponent<Canvas>().sortingOrder, Is.EqualTo(BattleUnitMotionMath.GetSortingOrder(-1.1f))); // 겹침 순서 적용
                Assert.That(motion.IsDefeatPlaying, Is.False); // 서 있음

                motion.PlayDefeat(); // 쓰러짐
                Assert.That(motion.IsDefeatPlaying, Is.True); // 쓰러지는 중
                Assert.That(() => motion.PlayLunge(), Throws.Nothing); // 쓰러진 뒤 공격 신호는 무시
                Assert.That(() => motion.PlayRecoil(), Throws.Nothing); // 쓰러진 뒤 피격 신호도 무시

                body.localRotation = Quaternion.Euler(0f, 0f, 20f); // 기울어진 상태를 흉내
                motion.Configure(body, 1f, "ALLY_1"); // 다시 연결 (부활)
                Assert.That(motion.IsDefeatPlaying, Is.False); // 다시 서 있음
                Assert.That(Quaternion.Angle(body.localRotation, Quaternion.identity), Is.LessThan(0.01f)); // 기울임 없음
                Assert.That(body.anchoredPosition, Is.EqualTo(Vector2.zero)); // 원래 자리
                Assert.That(() => motion.Configure(null, -1f, null), Throws.Nothing); // null 안전
            }
            finally // 임시 객체 정리
            {
                Object.DestroyImmediate(unit); // 유닛과 하위 제거
            }
        }
    }
}
