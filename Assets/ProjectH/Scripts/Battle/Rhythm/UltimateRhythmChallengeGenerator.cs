using System.Collections.Generic; // 목록 자료형
using UnityEngine; // Unity 기본 기능

namespace ProjectH.Battle.Rhythm // 프로젝트 전투 리듬 영역 (Day49)
{
    public static class UltimateRhythmChallengeGenerator // 궁극기 리듬 챌린지 원 생성 기능
    {
        public const int MinCircleCount = 3; // 최소 생성 원 개수
        public const int MaxCircleCount = 4; // 최대 생성 원 개수
        public const float BeatsPerMinute = 80f; // 챌린지 박자 속도(BPM)
        public const float SecondsPerBeat = 60f / BeatsPerMinute; // 한 박자 길이(초)
        public const float ApproachSeconds = 3f; // 원이 목표 크기까지 줄어드는 시간(초) — 모든 원 공통이라 박자가 일정하게 유지됨
        public const float CircleSizePixels = 130f; // 원 기준 크기(px), 기준 해상도(1920x1080) 대비 값
        public const float ReferenceWidth = 1920f; // 원 배치 계산 기준 해상도 가로
        public const float ReferenceHeight = 1080f; // 원 배치 계산 기준 해상도 세로
        private const float MinCenterDistancePixels = CircleSizePixels * 1.15f; // 원 사이 최소 중심 거리 (겹침 방지 여유 포함)
        private const int MaxPlacementAttempts = 40; // 겹치지 않는 위치 탐색 최대 시도 횟수
        private static readonly Vector2 SafeAreaMin = new Vector2(0.14f, 0.30f); // 안전 영역 최소 좌표 (기존 HUD·패널과 겹치지 않는 범위)
        private static readonly Vector2 SafeAreaMax = new Vector2(0.86f, 0.78f); // 안전 영역 최대 좌표

        public static List<RhythmCircleData> Generate(System.Random random = null) // 궁극기 리듬 챌린지 원 목록 생성
        {
            System.Random rng = random ?? new System.Random(); // 난수 발생기 준비 (테스트 시 시드 주입 가능)
            int count = rng.Next(MinCircleCount, MaxCircleCount + 1); // 3~4개 사이 랜덤 원 개수 결정
            List<RhythmCircleData> circles = new List<RhythmCircleData>(count); // 생성 원 목록 준비

            for (int index = 0; index < count; index++) // 결정된 개수만큼 원 생성
            {
                Vector2 position = FindNonOverlappingPosition(rng, circles); // 기존 원과 겹치지 않는 위치 탐색
                float spawnDelay = index * SecondsPerBeat; // 한 박자 간격 등장 지연 계산 (판정 순간도 정확히 한 박자 간격이 됨)
                circles.Add(new RhythmCircleData(position, ApproachSeconds, spawnDelay, index + 1)); // 생성 원 데이터 추가
            }

            return circles; // 생성 원 목록 반환
        }

        public static float GetChallengeDurationSeconds(IReadOnlyList<RhythmCircleData> circles) // 챌린지 전체 진행 시간 계산
        {
            float longest = 0f; // 최장 판정 시각 초기화

            if (circles == null) // 원 목록 존재 확인
            {
                return longest; // 빈 목록 0초 반환
            }

            for (int index = 0; index < circles.Count; index++) // 원 목록 순회
            {
                longest = Mathf.Max(longest, circles[index].HitTimeSeconds); // 가장 늦은 판정 시각 갱신
            }

            return longest; // 최장 판정 시각 반환
        }

        public static bool IsWithinSafeArea(Vector2 normalizedPosition) // 위치의 안전 영역 포함 여부 확인
        {
            return normalizedPosition.x >= SafeAreaMin.x && normalizedPosition.x <= SafeAreaMax.x
                && normalizedPosition.y >= SafeAreaMin.y && normalizedPosition.y <= SafeAreaMax.y; // 안전 영역 범위 포함 결과 반환
        }

        private static Vector2 FindNonOverlappingPosition(System.Random rng, List<RhythmCircleData> existing) // 기존 원과 겹치지 않는 랜덤 위치 탐색
        {
            Vector2 candidate = Vector2.zero; // 후보 위치 초기화

            for (int attempt = 0; attempt < MaxPlacementAttempts; attempt++) // 최대 시도 횟수만큼 반복
            {
                float x = Mathf.Lerp(SafeAreaMin.x, SafeAreaMax.x, (float)rng.NextDouble()); // 안전 영역 내 랜덤 X 좌표 계산
                float y = Mathf.Lerp(SafeAreaMin.y, SafeAreaMax.y, (float)rng.NextDouble()); // 안전 영역 내 랜덤 Y 좌표 계산
                candidate = new Vector2(x, y); // 후보 위치 갱신

                if (!OverlapsAny(candidate, existing)) // 기존 원과 겹침 여부 확인
                {
                    return candidate; // 겹치지 않는 위치 즉시 반환
                }
            }

            return candidate; // 시도 초과 시 마지막 후보 위치 반환 (안전 영역이 좁은 극단적 경우 대비)
        }

        private static bool OverlapsAny(Vector2 candidate, List<RhythmCircleData> existing) // 후보 위치의 기존 원 겹침 여부 확인
        {
            for (int index = 0; index < existing.Count; index++) // 기존 생성 원 순회
            {
                float dx = (candidate.x - existing[index].NormalizedPosition.x) * ReferenceWidth; // 기준 해상도 가로 픽셀 거리 계산
                float dy = (candidate.y - existing[index].NormalizedPosition.y) * ReferenceHeight; // 기준 해상도 세로 픽셀 거리 계산
                float distance = Mathf.Sqrt((dx * dx) + (dy * dy)); // 두 원 중심 사이 픽셀 거리 계산

                if (distance < MinCenterDistancePixels) // 최소 중심 거리 미만 여부 확인
                {
                    return true; // 겹침 판정 반환
                }
            }

            return false; // 겹침 없음 반환
        }
    }
}
