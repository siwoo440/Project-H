using UnityEngine; // Unity 색상 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public enum BattleElement // 전투 공격·방어 속성 (Day52 신규)
    {
        None = 0, // 무속성 (빛·어둠에 약점)
        Fire = 1, // 불 속성
        Water = 2, // 물 속성
        Grass = 3, // 풀 속성
        Light = 4, // 빛 속성
        Dark = 5 // 어둠 속성
    }

    public enum BattleElementAffinity // 속성 상성 판정 결과 (Day52 신규)
    {
        Neutral = 0, // 상성 없음
        Weak = 1, // 약점 (피해 증가)
        Resist = 2 // 저항 (피해 감소)
    }

    public static class BattleElementAffinityTable // 속성 상성 계산 기능 (Day52 신규, 순수 계산 전용)
    {
        public const float WeakMultiplier = 1.50f; // 약점 적중 피해 배율
        public const float ResistMultiplier = 0.75f; // 저항 적중 피해 배율
        public const float NeutralMultiplier = 1.00f; // 상성 없음 피해 배율
        private static readonly Color WeakColor = new Color(1f, 0.82f, 0.26f, 1f); // 약점 표시 색상
        private static readonly Color ResistColor = new Color(0.62f, 0.66f, 0.72f, 1f); // 저항 표시 색상

        public static BattleElementAffinity Evaluate(BattleElement attackElement, BattleElement defenderElement) // 공격 속성과 방어 속성 기반 상성 판정
        {
            if (attackElement == BattleElement.None) // 무속성 공격 여부 확인
            {
                return BattleElementAffinity.Neutral; // 무속성 공격은 항상 상성 없음 반환 (Day52 이전 전투 결과 불변 보장)
            }

            if (defenderElement == BattleElement.None) // 무속성 방어 대상 여부 확인
            {
                return IsLightOrDark(attackElement) ? BattleElementAffinity.Weak : BattleElementAffinity.Neutral; // 무속성은 빛·어둠 공격에만 약점 반환
            }

            if (GetStrongTarget(attackElement) == defenderElement) // 공격 속성이 우위인 대상 여부 확인
            {
                return BattleElementAffinity.Weak; // 약점 적중 반환
            }

            if (GetStrongTarget(defenderElement) == attackElement) // 방어 속성이 공격 속성에 우위인지 확인
            {
                return BattleElementAffinity.Resist; // 역상성 저항 반환
            }

            return BattleElementAffinity.Neutral; // 상성 없음 반환
        }

        public static float GetMultiplier(BattleElementAffinity affinity) // 상성 판정 기반 피해 배율 반환
        {
            switch (affinity) // 상성 판정 분기
            {
                case BattleElementAffinity.Weak: // 약점 처리
                    return WeakMultiplier; // 약점 배율 반환
                case BattleElementAffinity.Resist: // 저항 처리
                    return ResistMultiplier; // 저항 배율 반환
                default: // 상성 없음 처리
                    return NeutralMultiplier; // 기본 배율 반환
            }
        }

        public static float GetMultiplier(BattleElement attackElement, BattleElement defenderElement) // 공격·방어 속성 기반 피해 배율 직접 반환
        {
            return GetMultiplier(Evaluate(attackElement, defenderElement)); // 상성 판정 후 배율 반환
        }

        private static BattleElement GetStrongTarget(BattleElement element) // 해당 속성이 우위를 가지는 대상 속성 반환
        {
            switch (element) // 공격 속성 분기
            {
                case BattleElement.Fire: // 불 처리
                    return BattleElement.Grass; // 불은 풀에 우위 반환
                case BattleElement.Grass: // 풀 처리
                    return BattleElement.Water; // 풀은 물에 우위 반환
                case BattleElement.Water: // 물 처리
                    return BattleElement.Fire; // 물은 불에 우위 반환
                case BattleElement.Light: // 빛 처리
                    return BattleElement.Dark; // 빛은 어둠에 우위 반환 (일방향 상성)
                default: // 어둠 및 무속성 처리
                    return BattleElement.None; // 우위 대상 없음 반환
            }
        }

        private static bool IsLightOrDark(BattleElement element) // 빛 또는 어둠 속성 여부 확인
        {
            return element == BattleElement.Light || element == BattleElement.Dark; // 빛·어둠 여부 반환
        }

        public static string GetLabel(BattleElement element) // 속성 표시 문구 반환
        {
            switch (element) // 속성 분기
            {
                case BattleElement.Fire: // 불 처리
                    return "불"; // 불 문구 반환
                case BattleElement.Water: // 물 처리
                    return "물"; // 물 문구 반환
                case BattleElement.Grass: // 풀 처리
                    return "풀"; // 풀 문구 반환
                case BattleElement.Light: // 빛 처리
                    return "빛"; // 빛 문구 반환
                case BattleElement.Dark: // 어둠 처리
                    return "어둠"; // 어둠 문구 반환
                default: // 무속성 처리
                    return "무"; // 무속성 문구 반환
            }
        }

        public static string GetShortLabel(BattleElement element) // 속성 칩 표시용 축약 문구 반환
        {
            switch (element) // 속성 분기
            {
                case BattleElement.Fire: // 불 처리
                    return "불"; // 불 축약 문구 반환
                case BattleElement.Water: // 물 처리
                    return "물"; // 물 축약 문구 반환
                case BattleElement.Grass: // 풀 처리
                    return "풀"; // 풀 축약 문구 반환
                case BattleElement.Light: // 빛 처리
                    return "빛"; // 빛 축약 문구 반환
                case BattleElement.Dark: // 어둠 처리
                    return "암"; // 어둠 축약 문구 반환
                default: // 무속성 처리
                    return "무"; // 무속성 축약 문구 반환
            }
        }

        public static Color GetColor(BattleElement element) // 속성 표시 색상 반환
        {
            switch (element) // 속성 분기
            {
                case BattleElement.Fire: // 불 처리
                    return new Color(0.94f, 0.42f, 0.26f, 0.92f); // 불 주황색 반환
                case BattleElement.Water: // 물 처리
                    return new Color(0.30f, 0.62f, 0.94f, 0.92f); // 물 파란색 반환
                case BattleElement.Grass: // 풀 처리
                    return new Color(0.42f, 0.80f, 0.42f, 0.92f); // 풀 초록색 반환
                case BattleElement.Light: // 빛 처리
                    return new Color(0.98f, 0.92f, 0.58f, 0.92f); // 빛 연노란색 반환
                case BattleElement.Dark: // 어둠 처리
                    return new Color(0.52f, 0.38f, 0.72f, 0.92f); // 어둠 보라색 반환
                default: // 무속성 처리
                    return new Color(0.70f, 0.72f, 0.78f, 0.92f); // 무속성 회색 반환
            }
        }

        public static string GetAffinityLabel(BattleElementAffinity affinity) // 상성 판정 표시 문구 반환
        {
            switch (affinity) // 상성 판정 분기
            {
                case BattleElementAffinity.Weak: // 약점 처리
                    return "약점!"; // 약점 문구 반환
                case BattleElementAffinity.Resist: // 저항 처리
                    return "저항"; // 저항 문구 반환
                default: // 상성 없음 처리
                    return string.Empty; // 표시 문구 없음 반환
            }
        }

        public static Color GetAffinityColor(BattleElementAffinity affinity, Color defaultColor) // 상성 판정 기반 피해 숫자 색상 반환
        {
            switch (affinity) // 상성 판정 분기
            {
                case BattleElementAffinity.Weak: // 약점 처리
                    return WeakColor; // 약점 강조 색상 반환
                case BattleElementAffinity.Resist: // 저항 처리
                    return ResistColor; // 저항 흐린 색상 반환
                default: // 상성 없음 처리
                    return defaultColor; // 기본 피해 색상 반환
            }
        }
    }
}
