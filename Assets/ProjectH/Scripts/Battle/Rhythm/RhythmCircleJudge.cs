using UnityEngine; // Unity 수학 기능

namespace ProjectH.Battle.Rhythm // 프로젝트 전투 리듬 영역 (Day49)
{
    public enum RhythmHitResult // 리듬 원 판정 결과 (Day49 기본 Hit/Miss, Perfect/Good 세분화는 후속 Day)
    {
        Hit = 0, // 판정 성공
        Miss = 1 // 판정 실패
    }

    public static class RhythmCircleJudge // 리듬 원 기본 Hit/Miss 판정 기능
    {
        public const float HitWindowStart = 0.7f; // Hit 판정 시작 진행률 (원이 목표 크기에 충분히 가까워진 시점)

        public static RhythmHitResult Evaluate(float shrinkProgressAtClick) // 클릭 시점 진행률 기반 판정
        {
            float clamped = Mathf.Clamp01(shrinkProgressAtClick); // 진행률 범위 보정
            return clamped >= HitWindowStart ? RhythmHitResult.Hit : RhythmHitResult.Miss; // Hit 구간 진입 여부 기반 판정 반환
        }
    }
}
