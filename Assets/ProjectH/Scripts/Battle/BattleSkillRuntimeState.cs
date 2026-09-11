using System.Collections.Generic; // 목록 자료형
using UnityEngine; // Unity 시간 및 수학 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public enum BattleRuntimeModifierKind // 전투 중 시간 제한 Modifier 종류
    {
        DefensePercent = 0, // 방어력 비율 증가
        DamageReductionPercent = 1, // 최종 피해 감소
        HealingReceivedPercent = 2, // 받는 회복량 증가
        CounterChance = 3, // 반격 확률 증가
        ResistanceReductionPercent = 4, // 마법 저항 비율 감소
        AccuracyReductionPercent = 5, // 명중률 비율 감소
        Stun = 6, // 행동 불가 기절
        Silence = 7, // 스킬 사용 불가 침묵 (Day51 추가)
        AttackSpeedReductionPercent = 8, // 공격 속도 비율 감소 둔화 (Day51 추가)
        AttackPercent = 9, // 공격력 비율 증가 (Day51 추가)
        AttackSpeedPercent = 10, // 공격 속도 비율 증가 가속 (Day54 추가)
        Invulnerable = 11, // 모든 피해 무시 무적 (Day54 추가)
        DefenseReductionPercent = 12, // 방어력 비율 감소 (Day64 추가)
        AttackReductionPercent = 13 // 공격력 비율 감소 (Day64 추가)
    }

    public static partial class BattleSkillRuntimeState // 스킬 기반 전투 Runtime 상태 저장소 (최적화 — 역할별 partial 파일로 분리: 본 파일은 Modifier·스탯 조회)
    {
        private sealed class TimedModifier // 시간 제한 Modifier 데이터
        {
            public string RuntimeId; // 효과 대상 Runtime ID
            public BattleRuntimeModifierKind Kind; // Modifier 종류
            public float Value; // Modifier 수치
            public float ExpiresAt; // 만료 전투 시간
            public string SourceKey; // 같은 스킬 효과 재사용 시 갱신용 출처 키
        }

        private sealed class TauntEntry // 적군 도발 Runtime 데이터
        {
            public string EnemyRuntimeId; // 도발 대상 적 Runtime ID
            public string ForcedTargetRuntimeId; // 강제 공격 대상 Runtime ID
            public float ExpiresAt; // 도발 만료 시간
            public string SourceKey; // 도발 출처 키
        }

        private sealed class PeriodicDamageEntry // 주기 피해 Runtime 데이터
        {
            public IBattleCombatantStats Attacker; // 주기 피해 공격자 스탯
            public string TargetRuntimeId; // 주기 피해 대상 Runtime ID
            public BattleDamageType DamageType; // 주기 피해 종류
            public int Power; // Tick당 원본 위력
            public float TickInterval; // Tick 간격
            public int RemainingTicks; // 남은 Tick 수
            public float NextTickAt; // 다음 Tick 전투 시간
            public string SourceKey; // 주기 피해 출처 키
        }

        private static readonly List<TimedModifier> modifiers = new List<TimedModifier>(); // 전체 Runtime Modifier 목록
        private static readonly List<TauntEntry> taunts = new List<TauntEntry>(); // 전체 도발 목록
        private static readonly List<PeriodicDamageEntry> periodicDamages = new List<PeriodicDamageEntry>(); // 전체 주기 피해 목록
        private static readonly Dictionary<string, List<string>> removableDebuffs = new Dictionary<string, List<string>>(); // 제거 가능 Debuff 출처 목록
        private static BattleCombatRegistry registry; // 현재 전투 Registry
        public static BattleCombatRegistry CurrentRegistry => registry; // 현재 전투 Registry 반환 (Day60 추가, 룬 가시 반사 대상 조회)
        private static bool resolvingCounter; // 반격 재귀 처리 방지 상태
        private static float nextExpiryAt = float.PositiveInfinity; // 가장 이른 만료 시각 (이 시각 전에는 만료 정리 생략 — 최적화)
        private const float MaxAttackSpeedReduction = 0.70f; // 둔화 공격 속도 감소 상한 (Day51 추가, 완전 정지 방지)

        public static void SetRegistry(BattleCombatRegistry combatRegistry) // 현재 전투 Registry 연결
        {
            registry = combatRegistry; // 현재 전투 Registry 저장
        }

        public static void ResetAll() // 스킬 Runtime 상태 전체 초기화
        {
            modifiers.Clear(); // 전체 Modifier 제거
            nextExpiryAt = float.PositiveInfinity; // 만료 예정 시각 초기화
            taunts.Clear(); // 전체 도발 제거
            periodicDamages.Clear(); // 전체 주기 피해 제거
            removableDebuffs.Clear(); // 전체 제거 가능 Debuff 제거
            resolvingCounter = false; // 반격 처리 상태 초기화
        }

        public static void AddModifier(string runtimeId, BattleRuntimeModifierKind kind, float value, float duration, float nowSeconds = -1f) // 시간 제한 Modifier 추가
        {
            AddModifier(runtimeId, kind, value, duration, string.Empty, nowSeconds); // 출처 없는 누적 가능 Modifier 추가
        }

        public static void AddModifier(string runtimeId, BattleRuntimeModifierKind kind, float value, float duration, string sourceKey, float nowSeconds = -1f) // 출처 기반 시간 제한 Modifier 추가 또는 갱신
        {
            if (string.IsNullOrWhiteSpace(runtimeId) || value <= 0f || duration <= 0f) // Modifier 입력 유효성 확인
            {
                return; // 잘못된 Modifier 추가 중단
            }

            float now = ResolveNow(nowSeconds); // 현재 전투 시간 계산
            CleanupExpired(now); // 만료 Runtime 효과 정리

            if (!string.IsNullOrWhiteSpace(sourceKey)) // 동일 스킬 효과 갱신용 출처 키 존재 확인
            {
                for (int index = 0; index < modifiers.Count; index++) // 기존 Modifier 목록 순회
                {
                    TimedModifier existing = modifiers[index]; // 현재 Modifier 조회

                    if (existing.RuntimeId == runtimeId && existing.Kind == kind && existing.SourceKey == sourceKey) // 같은 대상·종류·출처 Modifier 확인
                    {
                        existing.Value = value; // 같은 스킬 효과 최신 수치로 갱신
                        existing.ExpiresAt = now + duration; // 같은 스킬 효과 지속시간 갱신
                        NoteExpiry(existing.ExpiresAt); // 만료 예정 시각 갱신
                        return; // Modifier 갱신 완료
                    }
                }
            }

            modifiers.Add(new TimedModifier // 신규 Modifier 생성
            {
                RuntimeId = runtimeId, // Modifier 대상 저장
                Kind = kind, // Modifier 종류 저장
                Value = value, // Modifier 수치 저장
                ExpiresAt = now + duration, // Modifier 만료 시간 저장
                SourceKey = sourceKey ?? string.Empty // Modifier 출처 키 저장
            }); // 신규 Modifier 추가 완료
            NoteExpiry(now + duration); // 만료 예정 시각 기록
        }

        public static float GetModifierTotal(string runtimeId, BattleRuntimeModifierKind kind, float nowSeconds = -1f) // 현재 활성 Modifier 합계 조회
        {
            if (string.IsNullOrWhiteSpace(runtimeId)) // Runtime ID 확인
            {
                return 0f; // 빈 Runtime ID Modifier 합계 0 반환
            }

            float now = ResolveNow(nowSeconds); // 현재 전투 시간 계산
            CleanupExpired(now); // 만료 Runtime 효과 정리
            float total = 0f; // Modifier 합계 초기화

            for (int index = 0; index < modifiers.Count; index++) // 전체 Modifier 순회
            {
                TimedModifier modifier = modifiers[index]; // 현재 Modifier 조회

                if (modifier.RuntimeId == runtimeId && modifier.Kind == kind) // 대상 및 종류 일치 확인
                {
                    total += modifier.Value; // Modifier 수치 누적
                }
            }

            return Mathf.Max(0f, total); // 음수 방지 Modifier 합계 반환
        }

        public static float GetModifierValue(string runtimeId, BattleRuntimeModifierKind kind, string sourceKey, float nowSeconds = -1f) // 특정 출처 Modifier 현재 수치 (없거나 정화되면 0, Day66 추가 — 혹한 중첩 확인)
        {
            if (string.IsNullOrWhiteSpace(runtimeId) || string.IsNullOrEmpty(sourceKey)) return 0f; // 입력 확인
            CleanupExpired(ResolveNow(nowSeconds)); // 만료 정리

            for (int index = 0; index < modifiers.Count; index++) // Modifier 순회
            {
                TimedModifier modifier = modifiers[index]; // 현재 Modifier
                if (modifier.RuntimeId == runtimeId && modifier.Kind == kind && modifier.SourceKey == sourceKey) return modifier.Value; // 같은 출처
            }

            return 0f; // 없음
        }

        public static int GetEffectiveDefense(IBattleCombatantStats target, float nowSeconds = -1f) // 방어력 증가 반영 최종 방어력 계산
        {
            if (target == null) // 전투 대상 확인
            {
                return 0; // 대상 없음 방어력 0 반환
            }

            float bonus = GetModifierTotal(target.RuntimeId, BattleRuntimeModifierKind.DefensePercent, nowSeconds); // 대상 방어 증가율 조회
            float reduction = Mathf.Clamp(GetModifierTotal(target.RuntimeId, BattleRuntimeModifierKind.DefenseReductionPercent, nowSeconds), 0f, 0.90f); // 방어 감소율 (Day64 추가, 상한 90%)
            return Mathf.Max(0, Mathf.RoundToInt(target.Defense * (1f + bonus) * (1f - reduction))); // 방어 증가·감소 적용 최종 방어력 반환
        }

        public static int GetEffectiveResistance(IBattleCombatantStats target, float nowSeconds = -1f) // 마법 저항 감소 반영 최종 저항력 계산
        {
            if (target == null) // 전투 대상 확인
            {
                return 0; // 대상 없음 저항력 0 반환
            }

            int baseResistance = target is IBattleResistanceStats resistanceStats ? Mathf.Max(0, resistanceStats.Resistance) : Mathf.Max(0, target.Defense); // 기본 저항력 또는 방어 대체값 조회
            float reduction = Mathf.Clamp(GetModifierTotal(target.RuntimeId, BattleRuntimeModifierKind.ResistanceReductionPercent, nowSeconds), 0f, 0.90f); // 마법 저항 감소율 상한 적용
            return Mathf.Max(0, Mathf.RoundToInt(baseResistance * (1f - reduction))); // 마법 저항 감소 반영 최종 저항력 반환
        }

        public static float GetEffectiveAccuracy(IBattleCombatantStats attacker, float nowSeconds = -1f) // 명중 감소 반영 최종 명중률 계산
        {
            if (attacker == null) // 공격자 확인
            {
                return 0f; // 공격자 없음 명중률 0 반환
            }

            float baseAccuracy = attacker is IBattleAccuracyStats accuracyStats ? Mathf.Clamp01(accuracyStats.Accuracy) : 1f; // 기본 명중률 또는 100퍼센트 대체값 조회
            float reduction = Mathf.Clamp(GetModifierTotal(attacker.RuntimeId, BattleRuntimeModifierKind.AccuracyReductionPercent, nowSeconds), 0f, 0.95f); // 명중 감소율 상한 적용
            return Mathf.Clamp01(baseAccuracy * (1f - reduction)); // 명중 감소 반영 최종 명중률 반환
        }

        public static bool IsStunned(string runtimeId, float nowSeconds = -1f) // 현재 기절 상태 확인
        {
            return GetModifierTotal(runtimeId, BattleRuntimeModifierKind.Stun, nowSeconds) > 0f; // 활성 기절 Modifier 존재 여부 반환
        }

        public static bool IsSilenced(string runtimeId, float nowSeconds = -1f) // 현재 침묵 상태 확인 (Day51 추가)
        {
            return GetModifierTotal(runtimeId, BattleRuntimeModifierKind.Silence, nowSeconds) > 0f; // 침묵 Modifier 존재 여부 반환
        }

        public static float GetAttackSpeedReduction(string runtimeId, float nowSeconds = -1f) // 공격 속도 감소율 조회 (Day51 추가)
        {
            return Mathf.Clamp(GetModifierTotal(runtimeId, BattleRuntimeModifierKind.AttackSpeedReductionPercent, nowSeconds), 0f, MaxAttackSpeedReduction); // 상한 보정 공격 속도 감소율 반환
        }

        public static float GetAttackSpeedMultiplier(string runtimeId, float nowSeconds = -1f) // 공격 속도 증가 배율 조회 (Day54 추가)
        {
            return 1f + GetModifierTotal(runtimeId, BattleRuntimeModifierKind.AttackSpeedPercent, nowSeconds); // 가속 반영 배율 반환
        }

        public static bool IsInvulnerable(string runtimeId, float nowSeconds = -1f) // 현재 무적 상태 확인 (Day54 추가)
        {
            return GetModifierTotal(runtimeId, BattleRuntimeModifierKind.Invulnerable, nowSeconds) > 0f; // 무적 Modifier 존재 여부 반환
        }

        public static float GetAttackMultiplier(string runtimeId, float nowSeconds = -1f) // 공격력 증가 배율 조회 (Day51 추가)
        {
            float reduction = Mathf.Clamp(GetModifierTotal(runtimeId, BattleRuntimeModifierKind.AttackReductionPercent, nowSeconds), 0f, 0.90f); // 공격력 감소율 (Day64 추가, 상한 90%)
            return (1f + GetModifierTotal(runtimeId, BattleRuntimeModifierKind.AttackPercent, nowSeconds)) * (1f - reduction); // 공격력 증가·감소 반영 배율 반환
        }

        public static float GetDamageReduction(string runtimeId, float nowSeconds = -1f) // 최종 피해 감소율 조회
        {
            float reduction = GetModifierTotal(runtimeId, BattleRuntimeModifierKind.DamageReductionPercent, nowSeconds); // 대상 피해 감소율 합계 조회
            return Mathf.Clamp(reduction, 0f, 0.90f); // 완전 무적 방지 90퍼센트 상한 적용
        }

        public static float GetHealingReceivedMultiplier(string runtimeId, float nowSeconds = -1f) // 받는 회복량 배율 조회
        {
            float bonus = GetModifierTotal(runtimeId, BattleRuntimeModifierKind.HealingReceivedPercent, nowSeconds); // 받는 회복량 증가율 조회
            return Mathf.Max(0f, 1f + bonus); // 받는 회복량 최종 배율 반환
        }

        private static float ResolveNow(float nowSeconds) // 테스트 또는 Runtime 현재 시간 계산
        {
            return nowSeconds >= 0f ? nowSeconds : Time.time; // 명시 시간 또는 Unity 전투 시간 반환
        }

        private static void NoteExpiry(float expiresAt) // 만료 예정 시각 기록 (가장 이른 시각 유지)
        {
            if (expiresAt < nextExpiryAt) // 더 이른 만료 확인
            {
                nextExpiryAt = expiresAt; // 가장 이른 만료 시각 갱신
            }
        }

        private static void CleanupExpired(float now) // 만료 Runtime 효과 정리 (최적화 — 가장 이른 만료 전에는 목록 순회 생략)
        {
            if (now < nextExpiryAt) // 아직 만료된 효과가 없는지 확인
            {
                return; // 지울 효과 없음 (결과는 기존 전체 순회와 동일)
            }

            float earliest = float.PositiveInfinity; // 남은 효과 중 가장 이른 만료 시각

            for (int index = modifiers.Count - 1; index >= 0; index--) // Modifier 목록 역순 순회
            {
                if (modifiers[index].ExpiresAt <= now) // Modifier 만료 여부 확인
                {
                    modifiers.RemoveAt(index); // 만료 Modifier 제거
                }
                else if (modifiers[index].ExpiresAt < earliest) // 남은 효과 만료 시각 비교
                {
                    earliest = modifiers[index].ExpiresAt; // 가장 이른 만료 갱신
                }
            }

            for (int index = taunts.Count - 1; index >= 0; index--) // 도발 목록 역순 순회
            {
                if (taunts[index].ExpiresAt <= now) // 도발 만료 여부 확인
                {
                    taunts.RemoveAt(index); // 만료 도발 제거
                }
                else if (taunts[index].ExpiresAt < earliest) // 남은 도발 만료 시각 비교
                {
                    earliest = taunts[index].ExpiresAt; // 가장 이른 만료 갱신
                }
            }

            nextExpiryAt = earliest; // 다음 정리 시각 확정
        }
    }
}
