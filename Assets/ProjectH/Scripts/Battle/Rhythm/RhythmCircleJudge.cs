using UnityEngine; // Unity 수학 기능

namespace ProjectH.Battle.Rhythm // 프로젝트 전투 리듬 영역 (Day49)
{
    public enum RhythmHitResult // 리듬 원 판정 결과 (Day49 Perfect/Good/Miss 3단계 확장)
    {
        Perfect = 0, // 완벽 타이밍 판정
        Good = 1, // 근접 타이밍 판정
        Miss = 2 // 실패 판정
    }

    public static class RhythmCircleJudge // 리듬 원 타이밍 판정 기능
    {
        public const float PerfectWindow = 0.05f; // Perfect 허용 진행률 오차 (1.0 기준 ±)
        public const float GoodWindow = 0.15f; // Good 허용 진행률 오차 (1.0 기준 ±)
        public const float LateWindow = GoodWindow; // 판정 순간 이후 클릭을 허용하는 진행률 여유
        private const float BoundaryEpsilon = 0.0001f; // 경계값 부동소수점 오차 허용치 (경계 진행률이 한 단계 낮게 밀리는 것 방지)

        public static RhythmHitResult Evaluate(float shrinkProgressAtClick) // 클릭 시점 진행률 기반 판정
        {
            float offset = Mathf.Abs(shrinkProgressAtClick - 1f); // 완벽 타이밍(진행률 1.0)과의 오차 계산

            if (offset <= PerfectWindow + BoundaryEpsilon) // Perfect 구간 포함 여부 확인
            {
                return RhythmHitResult.Perfect; // Perfect 판정 반환
            }

            if (offset <= GoodWindow + BoundaryEpsilon) // Good 구간 포함 여부 확인
            {
                return RhythmHitResult.Good; // Good 판정 반환
            }

            return RhythmHitResult.Miss; // 두 구간 모두 벗어난 Miss 판정 반환
        }

        public static bool IsHit(RhythmHitResult result) // 성공 판정 여부 확인
        {
            return result == RhythmHitResult.Perfect || result == RhythmHitResult.Good; // Perfect 또는 Good 여부 반환
        }

        public static string GetLabel(RhythmHitResult result) // 판정 표시 문구 반환
        {
            switch (result) // 판정 결과 분기
            {
                case RhythmHitResult.Perfect: // Perfect 처리
                    return "PERFECT"; // Perfect 문구 반환
                case RhythmHitResult.Good: // Good 처리
                    return "GOOD"; // Good 문구 반환
                default: // Miss 처리
                    return "MISS"; // Miss 문구 반환
            }
        }

        public static Color GetColor(RhythmHitResult result) // 판정 표시 색상 반환
        {
            switch (result) // 판정 결과 분기
            {
                case RhythmHitResult.Perfect: // Perfect 처리
                    return new Color(1f, 0.86f, 0.28f, 1f); // Perfect 금색 반환
                case RhythmHitResult.Good: // Good 처리
                    return new Color(0.36f, 0.90f, 0.52f, 1f); // Good 초록색 반환
                default: // Miss 처리
                    return new Color(0.90f, 0.30f, 0.32f, 1f); // Miss 붉은색 반환
            }
        }
    }
}
