using UnityEngine; // Unity 기본 기능

namespace ProjectH.Battle.Rhythm // 프로젝트 전투 리듬 영역 (Day49)
{
    public readonly struct RhythmCircleData // 단일 리듬 원 생성 데이터
    {
        public Vector2 NormalizedPosition { get; } // 화면 정규화 위치 (0~1)
        public float ShrinkSeconds { get; } // 원이 줄어드는 소요 시간(초)

        public RhythmCircleData(Vector2 normalizedPosition, float shrinkSeconds) // 리듬 원 데이터 생성
        {
            NormalizedPosition = normalizedPosition; // 정규화 위치 저장
            ShrinkSeconds = Mathf.Max(0.1f, shrinkSeconds); // 최소 줄어드는 시간 보정 후 저장
        }
    }
}
