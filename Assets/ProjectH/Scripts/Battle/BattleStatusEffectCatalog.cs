using UnityEngine; // Unity 색상 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public enum BattleStatusEffectId // 전투 상태이상 종류 (Day51 신규, 흩어진 Runtime 효과를 하나의 개념으로 통합)
    {
        None = 0, // 상태이상 없음
        Burn = 1, // 화상 (마법 계열 지속 피해)
        Poison = 2, // 중독 (물리 계열 지속 피해)
        Stun = 3, // 기절 (행동 불가)
        Silence = 4, // 침묵 (스킬 사용 불가)
        Slow = 5, // 둔화 (공격 속도 감소)
        ResistDown = 6, // 저항 감소
        BlindDown = 7, // 실명 (명중률 감소)
        Taunt = 8, // 도발 (대상 강제)
        DefenseUp = 9, // 방어력 증가
        DamageShield = 10, // 받는 피해 감소
        HealUp = 11, // 받는 회복량 증가
        CounterUp = 12, // 반격 확률 증가
        AttackUp = 13, // 공격력 증가
        Haste = 14, // 공격 속도 증가 가속 (Day54 추가)
        Invulnerable = 15 // 무적 (Day54 추가)
    }

    public readonly struct BattleStatusEffectSnapshot // 현재 활성 상태이상 표시 정보 (Day51 신규)
    {
        public BattleStatusEffectId Id { get; } // 상태이상 종류
        public float RemainingSeconds { get; } // 남은 지속시간 (무한 지속은 float.PositiveInfinity)
        public int StackCount { get; } // 동일 상태이상 중첩 수

        public BattleStatusEffectSnapshot(BattleStatusEffectId id, float remainingSeconds, int stackCount) // 상태이상 표시 정보 생성
        {
            Id = id; // 상태이상 종류 저장
            RemainingSeconds = remainingSeconds; // 남은 지속시간 저장
            StackCount = Mathf.Max(1, stackCount); // 중첩 수 최소 1 보정
        }

        public bool IsPermanent => float.IsPositiveInfinity(RemainingSeconds); // 전투 종료까지 유지되는 효과 여부 반환
    }

    public static class BattleStatusEffectCatalog // 상태이상 표시 정보 조회 기능 (Day51 신규, 순수 조회 전용)
    {
        private static readonly Color DebuffColor = new Color(0.86f, 0.30f, 0.36f, 0.92f); // 일반 디버프 표시 색상
        private static readonly Color ControlColor = new Color(0.78f, 0.42f, 0.90f, 0.92f); // 행동 제한 디버프 표시 색상
        private static readonly Color DotColor = new Color(0.95f, 0.55f, 0.24f, 0.92f); // 지속 피해 디버프 표시 색상
        private static readonly Color BuffColor = new Color(0.34f, 0.74f, 0.96f, 0.92f); // 일반 버프 표시 색상
        private static readonly Color GuardColor = new Color(0.40f, 0.86f, 0.70f, 0.92f); // 방어 계열 버프 표시 색상
        private static readonly Color UnknownColor = new Color(0.70f, 0.72f, 0.78f, 0.92f); // 미분류 상태이상 표시 색상

        public static string GetLabel(BattleStatusEffectId id) // 상태이상 표시 문구 반환
        {
            switch (id) // 상태이상 종류 분기
            {
                case BattleStatusEffectId.Burn: // 화상 처리
                    return "화상"; // 화상 문구 반환
                case BattleStatusEffectId.Poison: // 중독 처리
                    return "중독"; // 중독 문구 반환
                case BattleStatusEffectId.Stun: // 기절 처리
                    return "기절"; // 기절 문구 반환
                case BattleStatusEffectId.Silence: // 침묵 처리
                    return "침묵"; // 침묵 문구 반환
                case BattleStatusEffectId.Slow: // 둔화 처리
                    return "둔화"; // 둔화 문구 반환
                case BattleStatusEffectId.ResistDown: // 저항 감소 처리
                    return "저항 감소"; // 저항 감소 문구 반환
                case BattleStatusEffectId.BlindDown: // 실명 처리
                    return "실명"; // 실명 문구 반환
                case BattleStatusEffectId.Taunt: // 도발 처리
                    return "도발"; // 도발 문구 반환
                case BattleStatusEffectId.DefenseUp: // 방어 증가 처리
                    return "방어 증가"; // 방어 증가 문구 반환
                case BattleStatusEffectId.DamageShield: // 피해 감소 처리
                    return "피해 감소"; // 피해 감소 문구 반환
                case BattleStatusEffectId.HealUp: // 회복 증가 처리
                    return "회복 증가"; // 회복 증가 문구 반환
                case BattleStatusEffectId.CounterUp: // 반격 증가 처리
                    return "반격 증가"; // 반격 증가 문구 반환
                case BattleStatusEffectId.AttackUp: // 공격 증가 처리
                    return "공격 증가"; // 공격 증가 문구 반환
                case BattleStatusEffectId.Haste: // 가속 처리 (Day54 추가)
                    return "가속"; // 가속 문구 반환
                case BattleStatusEffectId.Invulnerable: // 무적 처리 (Day54 추가)
                    return "무적"; // 무적 문구 반환
                default: // 미분류 상태이상 처리
                    return "-"; // 미분류 표시 문구 반환
            }
        }

        public static string GetShortLabel(BattleStatusEffectId id) // 상태이상 칩 표시용 축약 문구 반환
        {
            switch (id) // 상태이상 종류 분기
            {
                case BattleStatusEffectId.Burn: // 화상 처리
                    return "화"; // 화상 축약 문구 반환
                case BattleStatusEffectId.Poison: // 중독 처리
                    return "독"; // 중독 축약 문구 반환
                case BattleStatusEffectId.Stun: // 기절 처리
                    return "기"; // 기절 축약 문구 반환
                case BattleStatusEffectId.Silence: // 침묵 처리
                    return "침"; // 침묵 축약 문구 반환
                case BattleStatusEffectId.Slow: // 둔화 처리
                    return "둔"; // 둔화 축약 문구 반환
                case BattleStatusEffectId.ResistDown: // 저항 감소 처리
                    return "저"; // 저항 감소 축약 문구 반환
                case BattleStatusEffectId.BlindDown: // 실명 처리
                    return "맹"; // 실명 축약 문구 반환
                case BattleStatusEffectId.Taunt: // 도발 처리
                    return "도"; // 도발 축약 문구 반환
                case BattleStatusEffectId.DefenseUp: // 방어 증가 처리
                    return "방"; // 방어 증가 축약 문구 반환
                case BattleStatusEffectId.DamageShield: // 피해 감소 처리
                    return "막"; // 피해 감소 축약 문구 반환
                case BattleStatusEffectId.HealUp: // 회복 증가 처리
                    return "치"; // 회복 증가 축약 문구 반환
                case BattleStatusEffectId.CounterUp: // 반격 증가 처리
                    return "반"; // 반격 증가 축약 문구 반환
                case BattleStatusEffectId.AttackUp: // 공격 증가 처리
                    return "공"; // 공격 증가 축약 문구 반환
                case BattleStatusEffectId.Haste: // 가속 처리 (Day54 추가)
                    return "속"; // 가속 축약 문구 반환
                case BattleStatusEffectId.Invulnerable: // 무적 처리 (Day54 추가)
                    return "무"; // 무적 축약 문구 반환
                default: // 미분류 상태이상 처리
                    return "?"; // 미분류 축약 문구 반환
            }
        }

        public static bool IsDebuff(BattleStatusEffectId id) // 정화 대상 디버프 여부 반환
        {
            switch (id) // 상태이상 종류 분기
            {
                case BattleStatusEffectId.Burn: // 화상 처리
                case BattleStatusEffectId.Poison: // 중독 처리
                case BattleStatusEffectId.Stun: // 기절 처리
                case BattleStatusEffectId.Silence: // 침묵 처리
                case BattleStatusEffectId.Slow: // 둔화 처리
                case BattleStatusEffectId.ResistDown: // 저항 감소 처리
                case BattleStatusEffectId.BlindDown: // 실명 처리
                case BattleStatusEffectId.Taunt: // 도발 처리
                    return true; // 디버프 분류 반환
                default: // 버프 및 미분류 처리
                    return false; // 비디버프 분류 반환
            }
        }

        public static bool IsBuff(BattleStatusEffectId id) // 버프 여부 반환
        {
            return id != BattleStatusEffectId.None && !IsDebuff(id); // 유효하면서 디버프가 아닌 상태이상 여부 반환
        }

        public static Color GetColor(BattleStatusEffectId id) // 상태이상 칩 표시 색상 반환
        {
            switch (id) // 상태이상 종류 분기
            {
                case BattleStatusEffectId.Burn: // 화상 처리
                case BattleStatusEffectId.Poison: // 중독 처리
                    return DotColor; // 지속 피해 색상 반환
                case BattleStatusEffectId.Stun: // 기절 처리
                case BattleStatusEffectId.Silence: // 침묵 처리
                case BattleStatusEffectId.Taunt: // 도발 처리
                    return ControlColor; // 행동 제한 색상 반환
                case BattleStatusEffectId.Slow: // 둔화 처리
                case BattleStatusEffectId.ResistDown: // 저항 감소 처리
                case BattleStatusEffectId.BlindDown: // 실명 처리
                    return DebuffColor; // 일반 디버프 색상 반환
                case BattleStatusEffectId.DefenseUp: // 방어 증가 처리
                case BattleStatusEffectId.DamageShield: // 피해 감소 처리
                case BattleStatusEffectId.Invulnerable: // 무적 처리 (Day54 추가)
                    return GuardColor; // 방어 계열 버프 색상 반환
                case BattleStatusEffectId.HealUp: // 회복 증가 처리
                case BattleStatusEffectId.CounterUp: // 반격 증가 처리
                case BattleStatusEffectId.AttackUp: // 공격 증가 처리
                case BattleStatusEffectId.Haste: // 가속 처리 (Day54 추가)
                    return BuffColor; // 일반 버프 색상 반환
                default: // 미분류 상태이상 처리
                    return UnknownColor; // 미분류 색상 반환
            }
        }

        public static BattleStatusEffectId FromModifierKind(BattleRuntimeModifierKind kind) // Runtime Modifier 종류를 상태이상 종류로 변환
        {
            switch (kind) // Modifier 종류 분기
            {
                case BattleRuntimeModifierKind.DefensePercent: // 방어력 증가 처리
                    return BattleStatusEffectId.DefenseUp; // 방어 증가 상태이상 반환
                case BattleRuntimeModifierKind.DamageReductionPercent: // 피해 감소 처리
                    return BattleStatusEffectId.DamageShield; // 피해 감소 상태이상 반환
                case BattleRuntimeModifierKind.HealingReceivedPercent: // 회복량 증가 처리
                    return BattleStatusEffectId.HealUp; // 회복 증가 상태이상 반환
                case BattleRuntimeModifierKind.CounterChance: // 반격 확률 증가 처리
                    return BattleStatusEffectId.CounterUp; // 반격 증가 상태이상 반환
                case BattleRuntimeModifierKind.ResistanceReductionPercent: // 마법 저항 감소 처리
                    return BattleStatusEffectId.ResistDown; // 저항 감소 상태이상 반환
                case BattleRuntimeModifierKind.AccuracyReductionPercent: // 명중률 감소 처리
                    return BattleStatusEffectId.BlindDown; // 실명 상태이상 반환
                case BattleRuntimeModifierKind.Stun: // 기절 처리
                    return BattleStatusEffectId.Stun; // 기절 상태이상 반환
                case BattleRuntimeModifierKind.Silence: // 침묵 처리
                    return BattleStatusEffectId.Silence; // 침묵 상태이상 반환
                case BattleRuntimeModifierKind.AttackSpeedReductionPercent: // 공격 속도 감소 처리
                    return BattleStatusEffectId.Slow; // 둔화 상태이상 반환
                case BattleRuntimeModifierKind.AttackPercent: // 공격력 증가 처리
                    return BattleStatusEffectId.AttackUp; // 공격 증가 상태이상 반환
                case BattleRuntimeModifierKind.AttackSpeedPercent: // 공격 속도 증가 처리 (Day54 추가)
                    return BattleStatusEffectId.Haste; // 가속 상태이상 반환
                case BattleRuntimeModifierKind.Invulnerable: // 무적 처리 (Day54 추가)
                    return BattleStatusEffectId.Invulnerable; // 무적 상태이상 반환
                default: // 미분류 Modifier 처리
                    return BattleStatusEffectId.None; // 상태이상 없음 반환
            }
        }

        public static BattleStatusEffectId FromPeriodicDamageType(BattleDamageType damageType) // 주기 피해 종류를 지속 피해 상태이상으로 변환
        {
            return damageType == BattleDamageType.Magic ? BattleStatusEffectId.Burn : BattleStatusEffectId.Poison; // 마법 계열은 화상, 그 외는 중독 반환
        }
    }
}
