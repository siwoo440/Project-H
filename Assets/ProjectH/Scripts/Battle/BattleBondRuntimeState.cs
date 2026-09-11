using System; // 문자열 비교·수학 기능
using System.Collections.Generic; // 사전·집합 자료형
using ProjectH.SaveSystem; // 결속 수치 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleBondRuntimeState // 전투 중 결속 효과 상태 (Day59 신규 — 등록형: 스탯 생성 시 단계 등록, 초기화는 BattleRuntimeStates)
    {
        private static readonly Dictionary<string, int> levels = new Dictionary<string, int>(StringComparer.Ordinal); // 캐릭터 ID별 결속 단계
        private static readonly Dictionary<string, float> gaugeRemainders = new Dictionary<string, float>(StringComparer.Ordinal); // 게이지 소수점 누적 (1씩 충전해도 +10%가 쌓이도록)
        private static readonly HashSet<string> usedOnce = new HashSet<string>(StringComparer.Ordinal); // 전투당 1회 결속 스킬 사용 기록
        private static bool roundTable; // 결속의 원탁 시너지 활성 여부

        public static bool RoundTableActive => roundTable; // 결속의 원탁 활성 여부 반환

        public static void Register(string characterId, int level) // 결속 단계 등록 (BattleStatsFactory에서 호출)
        {
            if (!string.IsNullOrWhiteSpace(characterId)) levels[characterId] = Math.Max(0, Math.Min(BondCatalog.MaxLevel, level)); // 범위 보정 후 저장
        }

        public static int GetLevel(string characterId) // 결속 단계 조회 (미등록 0)
        {
            return characterId != null && levels.TryGetValue(characterId, out int level) ? level : 0; // 단계 반환
        }

        public static void SetRoundTable(bool active) // 결속의 원탁 시너지 설정
        {
            roundTable = active; // 활성 여부 저장
        }

        public static float GetGaugeGainMultiplier(string characterId) // 궁극기 게이지 충전 배율 (2단계 +10%, 결속의 원탁 +10%)
        {
            return 1f + BondCatalog.GetGaugeGainBonus(GetLevel(characterId)) + (roundTable ? BondCatalog.RoundTableGaugeBonus : 0f); // 배율 반환
        }

        public static int ScaleGaugeGain(string characterId, int amount) // 충전량에 결속 배율 적용 (소수점은 다음 충전으로 이월)
        {
            float multiplier = GetGaugeGainMultiplier(characterId); // 배율 조회

            if (amount <= 0 || multiplier <= 1f || string.IsNullOrEmpty(characterId)) // 적용 대상 확인
            {
                return amount; // 결속 없으면 기존 충전량 그대로
            }

            gaugeRemainders.TryGetValue(characterId, out float remainder); // 이월 소수점 조회
            float total = (amount * multiplier) + remainder + 0.0001f; // 배율 적용 합계 (부동소수 오차 보정)
            int whole = (int)Math.Floor(total); // 정수 충전량
            gaugeRemainders[characterId] = Math.Max(0f, total - whole - 0.0001f); // 남은 소수점 이월
            return whole; // 충전량 반환
        }

        public static float GetPerfectBonusPerHit(string characterId) // 리듬 Perfect 1개당 추가 위력 (3단계)
        {
            return BondCatalog.GetPerfectBonusPerHit(GetLevel(characterId)); // 추가 위력 반환
        }

        public static bool HasPassiveBoost(string characterId) => BondCatalog.HasPassiveBoost(GetLevel(characterId)); // 4단계 패시브 강화 여부
        public static bool HasBondSkill(string characterId) => BondCatalog.HasBondSkill(GetLevel(characterId)); // 5단계 결속 스킬 여부

        public static bool TryUseOnce(string key) // 전투당 1회 결속 스킬 사용 시도
        {
            return usedOnce.Add(key ?? string.Empty); // 처음이면 true
        }

        public static void ResetBattleEffects() // 전투 시작 초기화 (등록된 단계는 유지 — Day52 등록형 규칙)
        {
            gaugeRemainders.Clear(); // 게이지 소수점 초기화
            usedOnce.Clear(); // 1회 사용 기록 초기화
            roundTable = false; // 시너지 초기화
        }

        public static void ResetAll() // 전체 초기화 (전투 종료·테스트)
        {
            ResetBattleEffects(); // 전투 효과 초기화
            levels.Clear(); // 등록 단계 초기화
        }
    }
}
