using UnityEngine; // Unity 수학 기능

namespace ProjectH.Battle.Rhythm // 프로젝트 전투 리듬 영역 (Day49)
{
    public readonly struct RhythmChallengeResult // 궁극기 리듬 챌린지 종합 결과
    {
        public int PerfectCount { get; } // Perfect 판정 수
        public int GoodCount { get; } // Good 판정 수
        public int MissCount { get; } // Miss 판정 수
        public int MaxCombo { get; } // 이번 챌린지 최고 콤보 수 (Day50 추가)
        public int TotalCount => PerfectCount + GoodCount + MissCount; // 전체 원 개수 반환
        public int HitCount => PerfectCount + GoodCount; // 성공 판정 수 반환
        public float Accuracy => TotalCount <= 0 ? 0f : ((PerfectCount * 1f) + (GoodCount * 0.5f)) / TotalCount; // Perfect 1점·Good 0.5점 기준 정확도 반환
        public bool IsFullCombo => TotalCount > 0 && MissCount == 0; // 전 판정 성공 풀콤보 여부 반환 (Day50 추가)

        public RhythmChallengeResult(int perfectCount, int goodCount, int missCount) // 리듬 챌린지 결과 생성 (콤보 미지정 호환 생성자)
            : this(perfectCount, goodCount, missCount, perfectCount + goodCount) // 최고 콤보를 성공 판정 수로 간주해 위임
        {
        }

        public RhythmChallengeResult(int perfectCount, int goodCount, int missCount, int maxCombo) // 리듬 챌린지 결과 생성 (Day50 콤보 포함)
        {
            PerfectCount = Mathf.Max(0, perfectCount); // Perfect 수 음수 방지
            GoodCount = Mathf.Max(0, goodCount); // Good 수 음수 방지
            MissCount = Mathf.Max(0, missCount); // Miss 수 음수 방지
            MaxCombo = Mathf.Max(0, maxCombo); // 최고 콤보 수 음수 방지
        }

        public override string ToString() // 결과 요약 문구 반환
        {
            return $"PERFECT {PerfectCount} · GOOD {GoodCount} · MISS {MissCount}"; // 등급별 집계 문구 반환
        }
    }
}
