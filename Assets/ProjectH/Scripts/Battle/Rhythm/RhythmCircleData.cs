using UnityEngine; // Unity 기본 기능

namespace ProjectH.Battle.Rhythm // 프로젝트 전투 리듬 영역 (Day49)
{
    public readonly struct RhythmCircleData // 단일 리듬 원 생성 데이터
    {
        public Vector2 NormalizedPosition { get; } // 화면 정규화 위치 (0~1)
        public float ShrinkSeconds { get; } // 원이 줄어드는 소요 시간(초)
        public float SpawnDelaySeconds { get; } // 챌린지 시작 기준 등장 지연 시간(초, 박자 간격)
        public int OrderNumber { get; } // 화면 표시용 순서 번호 (1부터 시작)

        public RhythmCircleData(Vector2 normalizedPosition, float shrinkSeconds, float spawnDelaySeconds, int orderNumber) // 리듬 원 데이터 생성
        {
            NormalizedPosition = normalizedPosition; // 정규화 위치 저장
            ShrinkSeconds = Mathf.Max(0.1f, shrinkSeconds); // 최소 줄어드는 시간 보정 후 저장
            SpawnDelaySeconds = Mathf.Max(0f, spawnDelaySeconds); // 음수 등장 지연 방지 후 저장
            OrderNumber = Mathf.Max(1, orderNumber); // 최소 순서 번호 보정 후 저장
        }

        public float HitTimeSeconds => SpawnDelaySeconds + ShrinkSeconds; // 챌린지 시작 기준 완벽 판정 시각 반환
    }
}
