using System.Collections.Generic; // 사전 자료형

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleElementRuntimeState // Runtime ID 기반 전투 속성 저장소 (Day52 신규)
    {
        private static readonly Dictionary<string, BattleElement> elements = new Dictionary<string, BattleElement>(); // Runtime ID별 속성 목록

        public static void ResetAll() // 전투 속성 등록 전체 초기화
        {
            elements.Clear(); // 등록 속성 전체 제거
        }

        public static void Register(string runtimeId, BattleElement element) // 전투 참가자 속성 등록
        {
            if (string.IsNullOrWhiteSpace(runtimeId)) // Runtime ID 확인
            {
                return; // 잘못된 속성 등록 중단
            }

            elements[runtimeId] = element; // Runtime ID 기준 속성 저장 또는 갱신
        }

        public static void Unregister(string runtimeId) // 전투 참가자 속성 등록 해제 (Day55 추가, 새 스탯 생성 시 이전 잔여 속성 제거)
        {
            if (!string.IsNullOrWhiteSpace(runtimeId)) // Runtime ID 확인
            {
                elements.Remove(runtimeId); // 등록 속성 제거
            }
        }

        public static BattleElement GetElement(string runtimeId) // 전투 참가자 속성 조회
        {
            if (string.IsNullOrWhiteSpace(runtimeId)) // Runtime ID 확인
            {
                return BattleElement.None; // 빈 Runtime ID 무속성 반환
            }

            return elements.TryGetValue(runtimeId, out BattleElement element) ? element : BattleElement.None; // 미등록 대상은 무속성 반환
        }

        public static BattleElementAffinity EvaluateAgainst(BattleElement attackElement, string defenderRuntimeId) // 방어 대상 Runtime ID 기준 상성 판정
        {
            return BattleElementAffinityTable.Evaluate(attackElement, GetElement(defenderRuntimeId)); // 등록 속성 기반 상성 판정 반환
        }

        public static BattleElement ResolveAttackElement(BattleElement requestedElement, string attackerRuntimeId) // 공격 속성 결정 (미지정 시 시전자 속성 상속)
        {
            return requestedElement != BattleElement.None ? requestedElement : GetElement(attackerRuntimeId); // 지정 속성 우선, 없으면 시전자 속성 반환
        }
    }
}
