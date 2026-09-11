using System.Collections.Generic; // 읽기 전용 목록 기능
using ProjectH.Data; // 보스 페이즈 데이터 기능
using UnityEngine; // Unity 수학 기능

namespace ProjectH.Battle.Boss // 프로젝트 전투 보스 영역 (Day54)
{
    public static class BossPhaseResolver // 보스 페이즈 판정 기능 (Day54 신규, 순수 계산 전용)
    {
        public const int FirstPhase = 1; // 첫 페이즈 번호

        public static int ResolvePhase(IReadOnlyList<BossPhaseDefinition> phases, float healthRatio, float elapsedSeconds, int currentPhase) // 체력 비율과 경과 시간 기반 현재 페이즈 판정 (1부터 시작)
        {
            if (phases == null || phases.Count == 0) // 페이즈 정의 존재 확인
            {
                return FirstPhase; // 페이즈 정의 없음 첫 페이즈 반환
            }

            int reached = FirstPhase; // 도달한 최고 페이즈 초기화

            for (int index = 1; index < phases.Count; index++) // 2페이즈 이후 정의 순회 (1페이즈는 항상 도달)
            {
                BossPhaseDefinition phase = phases[index]; // 현재 페이즈 정의 조회

                if (phase == null) // 페이즈 정의 유효성 확인
                {
                    continue; // 잘못된 정의 제외
                }

                bool healthReached = healthRatio <= phase.EnterHealthRatio; // 체력 조건 도달 여부 판정
                bool timeReached = phase.ForceAfterSeconds > 0f && elapsedSeconds >= phase.ForceAfterSeconds; // 시간 조건 도달 여부 판정

                if (healthReached || timeReached) // 두 조건 중 하나라도 도달 확인
                {
                    reached = index + 1; // 도달 페이즈 번호 갱신
                }
            }

            int clampedCurrent = Mathf.Clamp(currentPhase, FirstPhase, phases.Count); // 현재 페이즈 범위 보정
            return Mathf.Max(clampedCurrent, reached); // 한번 올라간 페이즈는 회복해도 내려가지 않도록 최고값 반환
        }

        public static BossPhaseDefinition GetPhase(IReadOnlyList<BossPhaseDefinition> phases, int phaseNumber) // 페이즈 번호 기반 정의 조회
        {
            if (phases == null || phases.Count == 0) // 페이즈 정의 존재 확인
            {
                return null; // 페이즈 정의 없음 반환
            }

            int index = Mathf.Clamp(phaseNumber, FirstPhase, phases.Count) - 1; // 페이즈 번호를 배열 인덱스로 변환
            return phases[index]; // 페이즈 정의 반환
        }
    }
}
