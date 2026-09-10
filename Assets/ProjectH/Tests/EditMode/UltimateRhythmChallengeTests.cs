using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle.Rhythm; // 궁극기 리듬 챌린지 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class UltimateRhythmChallengeTests // Day49 궁극기 리듬 챌린지 회귀 테스트
    {
        [Test] // 생성 원 개수 범위 검증
        public void Generate_CircleCount_IsAlwaysThreeOrFour() // 3~4개 범위 생성 검증
        {
            System.Random rng = new System.Random(12345); // 고정 시드 난수 생성기

            for (int trial = 0; trial < 50; trial++) // 50회 반복 검증
            {
                var circles = UltimateRhythmChallengeGenerator.Generate(rng); // 원 데이터 생성 실행

                Assert.That(circles.Count, Is.GreaterThanOrEqualTo(UltimateRhythmChallengeGenerator.MinCircleCount)); // 최소 개수 검증
                Assert.That(circles.Count, Is.LessThanOrEqualTo(UltimateRhythmChallengeGenerator.MaxCircleCount)); // 최대 개수 검증
            }
        }

        [Test] // 생성 위치 안전 영역 포함 검증
        public void Generate_CirclePositions_StayWithinSafeArea() // 안전 영역 범위 검증
        {
            System.Random rng = new System.Random(777); // 고정 시드 난수 생성기
            var circles = UltimateRhythmChallengeGenerator.Generate(rng); // 원 데이터 생성 실행

            foreach (RhythmCircleData circle in circles) // 생성 원 목록 순회
            {
                Assert.That(UltimateRhythmChallengeGenerator.IsWithinSafeArea(circle.NormalizedPosition), Is.True); // 안전 영역 포함 검증
            }
        }

        [Test] // 생성 원들이 서로 겹치지 않는지 검증 (겹침 방지 추가 요청 대응)
        public void Generate_Circles_DoNotOverlapEachOther() // 원 간 최소 거리 검증
        {
            System.Random rng = new System.Random(999); // 고정 시드 난수 생성기

            for (int trial = 0; trial < 50; trial++) // 50회 반복 검증
            {
                var circles = UltimateRhythmChallengeGenerator.Generate(rng); // 원 데이터 생성 실행

                for (int i = 0; i < circles.Count; i++) // 원 목록 이중 순회 (겹침 쌍 확인)
                {
                    for (int j = i + 1; j < circles.Count; j++) // 이후 원과만 비교하여 중복 비교 방지
                    {
                        float dx = (circles[i].NormalizedPosition.x - circles[j].NormalizedPosition.x) * UltimateRhythmChallengeGenerator.ReferenceWidth; // 가로 픽셀 거리 계산
                        float dy = (circles[i].NormalizedPosition.y - circles[j].NormalizedPosition.y) * UltimateRhythmChallengeGenerator.ReferenceHeight; // 세로 픽셀 거리 계산
                        float distance = UnityEngine.Mathf.Sqrt((dx * dx) + (dy * dy)); // 두 원 중심 거리 계산

                        Assert.That(distance, Is.GreaterThanOrEqualTo(UltimateRhythmChallengeGenerator.CircleSizePixels), $"Trial={trial}, Pair=({i},{j}), Distance={distance}"); // 최소 비겹침 거리 검증
                    }
                }
            }
        }

        [Test] // 생성 줄어드는 시간 범위 검증
        public void Generate_ShrinkSeconds_StayWithinConfiguredRange() // 줄어드는 시간 범위 검증
        {
            System.Random rng = new System.Random(2024); // 고정 시드 난수 생성기
            var circles = UltimateRhythmChallengeGenerator.Generate(rng); // 원 데이터 생성 실행

            foreach (RhythmCircleData circle in circles) // 생성 원 목록 순회
            {
                Assert.That(circle.ShrinkSeconds, Is.GreaterThanOrEqualTo(UltimateRhythmChallengeGenerator.MinShrinkSeconds)); // 최소 시간 검증
                Assert.That(circle.ShrinkSeconds, Is.LessThanOrEqualTo(UltimateRhythmChallengeGenerator.MaxShrinkSeconds)); // 최대 시간 검증
            }
        }

        [Test] // Hit 판정 경계값 검증
        public void Evaluate_AtOrAboveHitWindow_ReturnsHit() // Hit 판정 경계 테스트
        {
            Assert.That(RhythmCircleJudge.Evaluate(RhythmCircleJudge.HitWindowStart), Is.EqualTo(RhythmHitResult.Hit)); // 경계값 Hit 검증
            Assert.That(RhythmCircleJudge.Evaluate(1f), Is.EqualTo(RhythmHitResult.Hit)); // 최대 진행률 Hit 검증
        }

        [Test] // Miss 판정 경계값 검증
        public void Evaluate_BelowHitWindow_ReturnsMiss() // Miss 판정 경계 테스트
        {
            Assert.That(RhythmCircleJudge.Evaluate(RhythmCircleJudge.HitWindowStart - 0.01f), Is.EqualTo(RhythmHitResult.Miss)); // 경계 직전 Miss 검증
            Assert.That(RhythmCircleJudge.Evaluate(0f), Is.EqualTo(RhythmHitResult.Miss)); // 최소 진행률 Miss 검증
        }

        [Test] // 진행률 범위 초과 입력 보정 검증
        public void Evaluate_OutOfRangeProgress_ClampsSafely() // 범위 밖 입력 보정 테스트
        {
            Assert.That(RhythmCircleJudge.Evaluate(-1f), Is.EqualTo(RhythmHitResult.Miss)); // 음수 입력 Miss 검증
            Assert.That(RhythmCircleJudge.Evaluate(2f), Is.EqualTo(RhythmHitResult.Hit)); // 초과 입력 Hit 검증 (1로 보정)
        }
    }
}
