using System.Collections.Generic; // 목록 자료형
using ProjectH.Data; // 보스 패턴 데이터 기능
using UnityEngine; // Unity 수학 기능

namespace ProjectH.Battle.Boss // 프로젝트 전투 보스 영역 (Day54)
{
    public sealed class BossPatternScheduler // 보스 특수 패턴 순서·쿨다운·예고 관리 (Day54 신규, MonoBehaviour 비의존 순수 클래스)
    {
        private readonly List<BossPatternDefinition> patterns = new List<BossPatternDefinition>(); // 스케줄 대상 패턴 목록
        private readonly float[] readyAt; // 패턴별 재사용 가능 시각
        private readonly float intervalSeconds; // 패턴 사이 최소 간격
        private float nextAllowedAt; // 다음 패턴 시작 가능 시각
        private int cursor; // 순환 선택 시작 위치
        private int lastIndex = -1; // 직전 사용 패턴 인덱스
        private int activeIndex = -1; // 현재 예고 중인 패턴 인덱스
        private float windupTotal; // 현재 패턴 전체 예고 시간
        private float windupRemaining; // 현재 패턴 남은 예고 시간

        public bool IsWindingUp => activeIndex >= 0; // 예고 진행 여부 반환
        public BossPatternDefinition ActivePattern => activeIndex >= 0 ? patterns[activeIndex] : null; // 현재 예고 중인 패턴 반환
        public float WindupProgress => windupTotal <= 0f ? 1f : Mathf.Clamp01(1f - (windupRemaining / windupTotal)); // 예고 진행률 반환 (0 시작 → 1 발동)
        public float WindupRemaining => Mathf.Max(0f, windupRemaining); // 남은 예고 시간 반환
        public int LastPatternIndex => lastIndex; // 직전 사용 패턴 인덱스 반환

        public BossPatternScheduler(IReadOnlyList<BossPatternDefinition> source, float startTime, float initialDelaySeconds, float patternIntervalSeconds) // 패턴 스케줄러 생성
        {
            if (source != null) // 원본 패턴 목록 확인
            {
                for (int index = 0; index < source.Count; index++) // 원본 패턴 순회
                {
                    if (source[index] != null) // 유효 패턴 확인
                    {
                        patterns.Add(source[index]); // 스케줄 대상 패턴 등록
                    }
                }
            }

            readyAt = new float[patterns.Count]; // 패턴별 재사용 시각 배열 생성
            intervalSeconds = Mathf.Max(0f, patternIntervalSeconds); // 패턴 간격 음수 방지
            nextAllowedAt = startTime + Mathf.Max(0f, initialDelaySeconds); // 첫 패턴 시작 가능 시각 설정
        }

        public bool TryBeginNext(int currentPhase, float now) // 현재 페이즈에서 사용 가능한 다음 패턴 선택 및 예고 시작
        {
            if (IsWindingUp || patterns.Count == 0 || now < nextAllowedAt) // 예고 중·패턴 없음·간격 미도달 확인
            {
                return false; // 새 패턴 시작 불가 반환
            }

            int selected = SelectCandidate(currentPhase, now, true); // 직전 패턴 제외 후보 선택

            if (selected < 0) // 후보 없음 확인
            {
                selected = SelectCandidate(currentPhase, now, false); // 유일 후보라면 직전 패턴 허용 재선택
            }

            if (selected < 0) // 최종 후보 존재 확인
            {
                return false; // 사용 가능 패턴 없음 반환
            }

            activeIndex = selected; // 예고 패턴 저장
            windupTotal = patterns[selected].WindupSeconds; // 전체 예고 시간 저장
            windupRemaining = windupTotal; // 남은 예고 시간 초기화
            cursor = (selected + 1) % patterns.Count; // 다음 순환 시작 위치 이동
            return true; // 예고 시작 성공 반환
        }

        private int SelectCandidate(int currentPhase, float now, bool excludeLast) // 조건 충족 패턴 순환 탐색
        {
            for (int offset = 0; offset < patterns.Count; offset++) // 전체 패턴 1회 순회
            {
                int index = (cursor + offset) % patterns.Count; // 순환 인덱스 계산
                BossPatternDefinition pattern = patterns[index]; // 후보 패턴 조회

                if (pattern.MinPhase > currentPhase || now < readyAt[index]) // 페이즈 해금 및 쿨다운 확인
                {
                    continue; // 미해금 또는 쿨다운 패턴 제외
                }

                if (excludeLast && index == lastIndex) // 직전 패턴 연속 사용 제외 확인
                {
                    continue; // 직전 패턴 제외
                }

                return index; // 조건 충족 패턴 반환
            }

            return -1; // 후보 없음 반환
        }

        public bool TickWindup(float deltaSeconds) // 예고 시간 진행 (완료 시 true)
        {
            if (!IsWindingUp) // 예고 진행 여부 확인
            {
                return false; // 예고 없음 반환
            }

            windupRemaining -= Mathf.Max(0f, deltaSeconds); // 남은 예고 시간 감소
            return windupRemaining <= 0f; // 예고 완료 여부 반환
        }

        public BossPatternDefinition CompleteActive(float now) // 예고 완료 패턴 확정 (쿨다운 적용)
        {
            return FinishActive(now); // 공통 종료 처리 결과 반환
        }

        public BossPatternDefinition CancelActive(float now) // 예고 중 패턴 취소 (흐트러짐 저지 — 쿨다운은 동일하게 소모)
        {
            return FinishActive(now); // 공통 종료 처리 결과 반환 (취소해도 재사용 대기가 걸려 저지 보상이 됨)
        }

        private BossPatternDefinition FinishActive(float now) // 예고 패턴 공통 종료 처리
        {
            if (!IsWindingUp) // 예고 진행 여부 확인
            {
                return null; // 종료할 패턴 없음 반환
            }

            BossPatternDefinition pattern = patterns[activeIndex]; // 종료 패턴 조회
            readyAt[activeIndex] = now + pattern.CooldownSeconds; // 패턴 재사용 시각 설정
            lastIndex = activeIndex; // 직전 사용 패턴 기록
            activeIndex = -1; // 예고 상태 해제
            windupTotal = 0f; // 전체 예고 시간 초기화
            windupRemaining = 0f; // 남은 예고 시간 초기화
            nextAllowedAt = Mathf.Max(nextAllowedAt, now + intervalSeconds); // 다음 패턴 최소 간격 적용
            return pattern; // 종료 패턴 반환
        }

        public void Postpone(float now, float seconds) // 다음 패턴 시작 지연 (페이즈 전환 등)
        {
            nextAllowedAt = Mathf.Max(nextAllowedAt, now + Mathf.Max(0f, seconds)); // 지연 시각 적용
        }

        public float GetReadyAt(int index) // 패턴별 재사용 가능 시각 조회 (테스트·디버그용)
        {
            return index >= 0 && index < readyAt.Length ? readyAt[index] : 0f; // 범위 내 재사용 시각 반환
        }
    }
}
