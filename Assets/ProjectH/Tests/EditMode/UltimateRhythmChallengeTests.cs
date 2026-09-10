using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle.Rhythm; // 궁극기 리듬 챌린지 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class UltimateRhythmChallengeTests // Day48~49 궁극기 리듬 챌린지 회귀 테스트
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

        [Test] // 생성 원들이 서로 겹치지 않는지 검증
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

        [Test] // 박자 간격 순차 등장 검증 (Day49 추가)
        public void Generate_SpawnDelays_FollowBeatInterval() // 원 등장 시각이 한 박자 간격인지 검증
        {
            System.Random rng = new System.Random(4321); // 고정 시드 난수 생성기
            var circles = UltimateRhythmChallengeGenerator.Generate(rng); // 원 데이터 생성 실행

            for (int index = 0; index < circles.Count; index++) // 생성 원 목록 순회
            {
                float expectedDelay = index * UltimateRhythmChallengeGenerator.SecondsPerBeat; // 기대 등장 지연 계산

                Assert.That(circles[index].SpawnDelaySeconds, Is.EqualTo(expectedDelay).Within(0.0001f)); // 박자 간격 등장 검증
                Assert.That(circles[index].OrderNumber, Is.EqualTo(index + 1)); // 순서 번호 검증
            }
        }

        [Test] // 판정 시각도 한 박자 간격인지 검증 (Day49 추가)
        public void Generate_HitTimes_AreOneBeatApart() // 완벽 판정 순간의 박자 간격 검증
        {
            System.Random rng = new System.Random(555); // 고정 시드 난수 생성기
            var circles = UltimateRhythmChallengeGenerator.Generate(rng); // 원 데이터 생성 실행

            for (int index = 1; index < circles.Count; index++) // 두 번째 원부터 순회
            {
                float gap = circles[index].HitTimeSeconds - circles[index - 1].HitTimeSeconds; // 인접 원 판정 시각 간격 계산

                Assert.That(gap, Is.EqualTo(UltimateRhythmChallengeGenerator.SecondsPerBeat).Within(0.0001f)); // 한 박자 간격 검증
            }
        }

        [Test] // 모든 원의 축소 시간이 동일한지 검증 (Day49 추가 — 박자 일정성 보장)
        public void Generate_ShrinkSeconds_AreUniform() // 공통 축소 시간 검증
        {
            System.Random rng = new System.Random(2024); // 고정 시드 난수 생성기
            var circles = UltimateRhythmChallengeGenerator.Generate(rng); // 원 데이터 생성 실행

            foreach (RhythmCircleData circle in circles) // 생성 원 목록 순회
            {
                Assert.That(circle.ShrinkSeconds, Is.EqualTo(UltimateRhythmChallengeGenerator.ApproachSeconds).Within(0.0001f)); // 공통 축소 시간 검증
            }
        }

        [Test] // Perfect 판정 구간 검증 (Day49 추가)
        public void Evaluate_WithinPerfectWindow_ReturnsPerfect() // Perfect 경계 테스트
        {
            Assert.That(RhythmCircleJudge.Evaluate(1f), Is.EqualTo(RhythmHitResult.Perfect)); // 정확한 타이밍 Perfect 검증
            Assert.That(RhythmCircleJudge.Evaluate(1f - RhythmCircleJudge.PerfectWindow), Is.EqualTo(RhythmHitResult.Perfect)); // 이른 쪽 경계 Perfect 검증
            Assert.That(RhythmCircleJudge.Evaluate(1f + RhythmCircleJudge.PerfectWindow), Is.EqualTo(RhythmHitResult.Perfect)); // 늦은 쪽 경계 Perfect 검증
        }

        [Test] // Good 판정 구간 검증 (Day49 추가)
        public void Evaluate_OutsidePerfectButWithinGood_ReturnsGood() // Good 경계 테스트
        {
            Assert.That(RhythmCircleJudge.Evaluate(1f - RhythmCircleJudge.PerfectWindow - 0.01f), Is.EqualTo(RhythmHitResult.Good)); // Perfect 직전 Good 검증
            Assert.That(RhythmCircleJudge.Evaluate(1f - RhythmCircleJudge.GoodWindow), Is.EqualTo(RhythmHitResult.Good)); // 이른 쪽 경계 Good 검증
            Assert.That(RhythmCircleJudge.Evaluate(1f + RhythmCircleJudge.GoodWindow), Is.EqualTo(RhythmHitResult.Good)); // 늦은 쪽 경계 Good 검증
        }

        [Test] // Miss 판정 구간 검증 (Day49 추가)
        public void Evaluate_OutsideGoodWindow_ReturnsMiss() // Miss 경계 테스트
        {
            Assert.That(RhythmCircleJudge.Evaluate(1f - RhythmCircleJudge.GoodWindow - 0.01f), Is.EqualTo(RhythmHitResult.Miss)); // 너무 이른 클릭 Miss 검증
            Assert.That(RhythmCircleJudge.Evaluate(1f + RhythmCircleJudge.GoodWindow + 0.01f), Is.EqualTo(RhythmHitResult.Miss)); // 너무 늦은 클릭 Miss 검증
            Assert.That(RhythmCircleJudge.Evaluate(0f), Is.EqualTo(RhythmHitResult.Miss)); // 등장 직후 클릭 Miss 검증
        }

        [Test] // 성공 판정 분류 검증 (Day49 추가)
        public void IsHit_ClassifiesPerfectAndGoodAsHit() // Hit 분류 테스트
        {
            Assert.That(RhythmCircleJudge.IsHit(RhythmHitResult.Perfect), Is.True); // Perfect 성공 분류 검증
            Assert.That(RhythmCircleJudge.IsHit(RhythmHitResult.Good), Is.True); // Good 성공 분류 검증
            Assert.That(RhythmCircleJudge.IsHit(RhythmHitResult.Miss), Is.False); // Miss 실패 분류 검증
        }

        [Test] // 종합 결과 집계 검증 (Day49 추가)
        public void ChallengeResult_AggregatesCountsAndAccuracy() // 결과 집계 테스트
        {
            RhythmChallengeResult result = new RhythmChallengeResult(2, 1, 1); // Perfect 2·Good 1·Miss 1 결과 생성

            Assert.That(result.TotalCount, Is.EqualTo(4)); // 전체 개수 검증
            Assert.That(result.HitCount, Is.EqualTo(3)); // 성공 개수 검증
            Assert.That(result.Accuracy, Is.EqualTo(0.625f).Within(0.0001f)); // 정확도(Perfect 1점·Good 0.5점) 검증
        }
    }
}
