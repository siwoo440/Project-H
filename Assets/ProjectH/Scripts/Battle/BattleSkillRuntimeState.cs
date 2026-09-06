using System.Collections.Generic; // 목록 자료형
using UnityEngine; // Unity 시간 및 수학 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public enum BattleRuntimeModifierKind // 전투 중 시간 제한 Modifier 종류
    {
        DefensePercent = 0, // 방어력 비율 증가
        DamageReductionPercent = 1, // 최종 피해 감소
        HealingReceivedPercent = 2, // 받는 회복량 증가
        CounterChance = 3 // 반격 확률 증가
    }

    public static class BattleSkillRuntimeState // 스킬 기반 전투 Runtime 상태 저장소
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
        }

        private static readonly List<TimedModifier> modifiers = new List<TimedModifier>(); // 전체 Runtime Modifier 목록
        private static readonly List<TauntEntry> taunts = new List<TauntEntry>(); // 전체 도발 목록
        private static readonly Dictionary<string, List<string>> removableDebuffs = new Dictionary<string, List<string>>(); // 향후 상태이상 연결용 제거 가능 Debuff 목록
        private static BattleCombatRegistry registry; // 현재 전투 Registry
        private static bool resolvingCounter; // 반격 재귀 처리 방지 상태

        public static void SetRegistry(BattleCombatRegistry combatRegistry) // 현재 전투 Registry 연결
        {
            registry = combatRegistry; // 현재 전투 Registry 저장
        }

        public static void ResetAll() // 스킬 Runtime 상태 전체 초기화
        {
            modifiers.Clear(); // 전체 Modifier 제거
            taunts.Clear(); // 전체 도발 제거
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

        public static int GetEffectiveDefense(IBattleCombatantStats target, float nowSeconds = -1f) // 방어력 증가 반영 최종 방어력 계산
        {
            if (target == null) // 전투 대상 확인
            {
                return 0; // 대상 없음 방어력 0 반환
            }

            float bonus = GetModifierTotal(target.RuntimeId, BattleRuntimeModifierKind.DefensePercent, nowSeconds); // 대상 방어 증가율 조회
            return Mathf.Max(0, Mathf.RoundToInt(target.Defense * (1f + bonus))); // 방어 증가 적용 최종 방어력 반환
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

        public static void ApplyTaunt(string enemyRuntimeId, string forcedTargetRuntimeId, float duration, float nowSeconds = -1f) // 적군 도발 적용
        {
            if (string.IsNullOrWhiteSpace(enemyRuntimeId) || string.IsNullOrWhiteSpace(forcedTargetRuntimeId) || duration <= 0f) // 도발 입력 유효성 확인
            {
                return; // 잘못된 도발 적용 중단
            }

            float now = ResolveNow(nowSeconds); // 현재 전투 시간 계산
            CleanupExpired(now); // 만료 Runtime 효과 정리

            for (int index = 0; index < taunts.Count; index++) // 기존 도발 목록 순회
            {
                TauntEntry existing = taunts[index]; // 현재 도발 항목 조회

                if (existing.EnemyRuntimeId == enemyRuntimeId) // 같은 적 기존 도발 확인
                {
                    existing.ForcedTargetRuntimeId = forcedTargetRuntimeId; // 최신 도발 대상 교체
                    existing.ExpiresAt = now + duration; // 최신 도발 지속시간 갱신
                    return; // 도발 갱신 완료
                }
            }

            taunts.Add(new TauntEntry // 신규 도발 Runtime 생성
            {
                EnemyRuntimeId = enemyRuntimeId, // 도발 대상 적 저장
                ForcedTargetRuntimeId = forcedTargetRuntimeId, // 강제 공격 대상 저장
                ExpiresAt = now + duration // 도발 만료 시간 저장
            }); // 신규 도발 추가 완료
        }

        public static bool TryGetTauntTargetRuntimeId(string enemyRuntimeId, float nowSeconds, out string forcedTargetRuntimeId) // 테스트 및 Runtime 공용 도발 대상 ID 조회
        {
            forcedTargetRuntimeId = string.Empty; // 강제 타겟 기본값 초기화
            float now = ResolveNow(nowSeconds); // 현재 전투 시간 계산
            CleanupExpired(now); // 만료 Runtime 효과 정리

            for (int index = 0; index < taunts.Count; index++) // 전체 도발 목록 순회
            {
                TauntEntry taunt = taunts[index]; // 현재 도발 항목 조회

                if (taunt.EnemyRuntimeId == enemyRuntimeId) // 적 Runtime ID 일치 확인
                {
                    forcedTargetRuntimeId = taunt.ForcedTargetRuntimeId; // 강제 공격 대상 ID 반환
                    return true; // 활성 도발 존재 반환
                }
            }

            return false; // 활성 도발 없음 반환
        }

        public static bool TryGetTauntTarget(BattleActor enemy, out BattleActor forcedTarget) // 적군 AI용 활성 도발 타겟 조회
        {
            forcedTarget = null; // 강제 타겟 기본값 초기화

            if (enemy == null || !enemy.IsCombatReady || registry == null) // 적군 및 Registry 확인
            {
                return false; // 도발 타겟 조회 실패 반환
            }

            if (!TryGetTauntTargetRuntimeId(enemy.Stats.RuntimeId, Time.time, out string runtimeId)) // 적군 활성 도발 ID 조회
            {
                return false; // 활성 도발 없음 반환
            }

            BattleActor target = registry.FindByRuntimeId(runtimeId); // Registry에서 강제 타겟 조회

            if (target == null || !target.IsCombatReady || !target.Stats.IsAlive || target.Team == enemy.Team) // 강제 타겟 유효성 확인
            {
                return false; // 잘못되거나 사망한 강제 타겟 차단
            }

            forcedTarget = target; // 활성 도발 타겟 반환
            return true; // 활성 도발 타겟 조회 성공
        }

        public static void RegisterRemovableDebuff(string runtimeId, string effectId) // 향후 상태이상 시스템용 제거 가능 Debuff 등록
        {
            if (string.IsNullOrWhiteSpace(runtimeId) || string.IsNullOrWhiteSpace(effectId)) // Debuff 등록 입력 확인
            {
                return; // 잘못된 Debuff 등록 중단
            }

            if (!removableDebuffs.TryGetValue(runtimeId, out List<string> effects)) // 대상 Debuff 목록 조회
            {
                effects = new List<string>(); // 신규 Debuff 목록 생성
                removableDebuffs.Add(runtimeId, effects); // 대상별 Debuff 목록 등록
            }

            effects.Add(effectId); // 제거 가능 Debuff 등록
        }

        public static int RemoveDebuffs(string runtimeId, int count) // 지정 개수 제거 가능 Debuff 제거
        {
            if (string.IsNullOrWhiteSpace(runtimeId) || count <= 0) // 제거 요청 유효성 확인
            {
                return 0; // 잘못된 제거 요청 0 반환
            }

            if (!removableDebuffs.TryGetValue(runtimeId, out List<string> effects) || effects.Count == 0) // 대상 Debuff 존재 확인
            {
                return 0; // 제거할 Debuff 없음 반환
            }

            int removeCount = Mathf.Min(count, effects.Count); // 실제 제거 가능 개수 계산
            effects.RemoveRange(0, removeCount); // 앞쪽 Debuff부터 제거
            return removeCount; // 실제 제거 개수 반환
        }

        public static void TryCounter(BattleActor defender, BattleDamageResult incomingResult) // 피격 후 반격 시도
        {
            if (resolvingCounter || defender == null || !defender.IsCombatReady || !defender.Stats.IsAlive || registry == null) // 반격 실행 가능 상태 확인
            {
                return; // 반격 시도 중단
            }

            float chance = Mathf.Clamp01(GetModifierTotal(defender.Stats.RuntimeId, BattleRuntimeModifierKind.CounterChance)); // 현재 반격 확률 조회

            if (chance <= 0f || Random.value > chance) // 반격 확률 판정
            {
                return; // 반격 미발동 처리
            }

            BattleActor attacker = registry.FindByRuntimeId(incomingResult.AttackerRuntimeId); // 원 공격자 Registry 조회

            if (attacker == null || !attacker.IsCombatReady || !attacker.Stats.IsAlive || attacker.Team == defender.Team) // 반격 대상 유효성 확인
            {
                return; // 반격 대상 없음 처리
            }

            resolvingCounter = true; // 반격 재귀 방지 활성화
            BattleDamageResult counterResult = BattleDamageResolver.Resolve(new BattleDamageRequest(defender.Stats, attacker.Stats, BattleDamageType.Physical, defender.Stats.Attack)); // 방어자의 공격력 100퍼센트 반격 피해 계산
            attacker.ApplyDamage(counterResult); // 반격 피해 실제 적용
            resolvingCounter = false; // 반격 재귀 방지 해제
        }

        private static float ResolveNow(float nowSeconds) // 테스트 또는 Runtime 현재 시간 계산
        {
            return nowSeconds >= 0f ? nowSeconds : Time.time; // 명시 시간 또는 Unity 전투 시간 반환
        }

        private static void CleanupExpired(float now) // 만료 Runtime 효과 정리
        {
            for (int index = modifiers.Count - 1; index >= 0; index--) // Modifier 목록 역순 순회
            {
                if (modifiers[index].ExpiresAt <= now) // Modifier 만료 여부 확인
                {
                    modifiers.RemoveAt(index); // 만료 Modifier 제거
                }
            }

            for (int index = taunts.Count - 1; index >= 0; index--) // 도발 목록 역순 순회
            {
                if (taunts[index].ExpiresAt <= now) // 도발 만료 여부 확인
                {
                    taunts.RemoveAt(index); // 만료 도발 제거
                }
            }
        }
    }
}
