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
        AttackPercent = 9 // 공격력 비율 증가 (Day51 추가)
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
        private static bool resolvingCounter; // 반격 재귀 처리 방지 상태
        private const float MaxAttackSpeedReduction = 0.70f; // 둔화 공격 속도 감소 상한 (Day51 추가, 완전 정지 방지)

        public static void SetRegistry(BattleCombatRegistry combatRegistry) // 현재 전투 Registry 연결
        {
            registry = combatRegistry; // 현재 전투 Registry 저장
        }

        public static void ResetAll() // 스킬 Runtime 상태 전체 초기화
        {
            modifiers.Clear(); // 전체 Modifier 제거
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

        public static float GetAttackMultiplier(string runtimeId, float nowSeconds = -1f) // 공격력 증가 배율 조회 (Day51 추가)
        {
            return 1f + GetModifierTotal(runtimeId, BattleRuntimeModifierKind.AttackPercent, nowSeconds); // 공격력 증가율 반영 배율 반환
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

        public static void ApplyTaunt(string enemyRuntimeId, string forcedTargetRuntimeId, float duration, float nowSeconds = -1f) // 18일차 호환 적군 도발 적용
        {
            ApplyTaunt(enemyRuntimeId, forcedTargetRuntimeId, duration, string.Empty, nowSeconds); // 출처 없는 도발 적용
        }

        public static void ApplyTaunt(string enemyRuntimeId, string forcedTargetRuntimeId, float duration, string sourceKey, float nowSeconds = -1f) // 출처 기반 적군 도발 적용
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
                    existing.SourceKey = sourceKey ?? string.Empty; // 최신 도발 출처 갱신
                    return; // 도발 갱신 완료
                }
            }

            taunts.Add(new TauntEntry // 신규 도발 Runtime 생성
            {
                EnemyRuntimeId = enemyRuntimeId, // 도발 대상 적 저장
                ForcedTargetRuntimeId = forcedTargetRuntimeId, // 강제 공격 대상 저장
                ExpiresAt = now + duration, // 도발 만료 시간 저장
                SourceKey = sourceKey ?? string.Empty // 도발 출처 저장
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

        public static void CollectStatusEffects(string runtimeId, List<BattleStatusEffectSnapshot> buffer, float nowSeconds = -1f) // 현재 활성 상태이상 스냅샷 수집 (Day51 추가, UI 매 갱신 호출용 버퍼 재사용 방식)
        {
            if (buffer == null) // 수집 버퍼 확인
            {
                return; // 상태이상 수집 중단
            }

            buffer.Clear(); // 이전 수집 결과 초기화

            if (string.IsNullOrWhiteSpace(runtimeId)) // Runtime ID 확인
            {
                return; // 빈 Runtime ID 수집 중단
            }

            float now = ResolveNow(nowSeconds); // 현재 전투 시간 계산
            CleanupExpired(now); // 만료 Runtime 효과 정리

            for (int index = 0; index < modifiers.Count; index++) // 전체 Modifier 순회
            {
                TimedModifier modifier = modifiers[index]; // 현재 Modifier 조회

                if (modifier.RuntimeId != runtimeId) // 대상 일치 확인
                {
                    continue; // 다른 대상 Modifier 제외
                }

                BattleStatusEffectId id = BattleStatusEffectCatalog.FromModifierKind(modifier.Kind); // Modifier 종류를 상태이상 종류로 변환

                if (id == BattleStatusEffectId.None) // 표시 대상 상태이상 여부 확인
                {
                    continue; // 미분류 Modifier 제외
                }

                AccumulateSnapshot(buffer, id, modifier.ExpiresAt - now); // 동일 종류 병합 후 스냅샷 누적
            }

            for (int index = 0; index < periodicDamages.Count; index++) // 전체 주기 피해 순회
            {
                PeriodicDamageEntry entry = periodicDamages[index]; // 현재 주기 피해 조회

                if (entry.TargetRuntimeId != runtimeId || entry.RemainingTicks <= 0) // 대상 일치 및 잔여 Tick 확인
                {
                    continue; // 비대상 또는 완료 주기 피해 제외
                }

                float remaining = Mathf.Max(0f, entry.NextTickAt - now) + (Mathf.Max(0, entry.RemainingTicks - 1) * entry.TickInterval); // 남은 전체 지속 피해 시간 계산
                AccumulateSnapshot(buffer, BattleStatusEffectCatalog.FromPeriodicDamageType(entry.DamageType), remaining); // 지속 피해 스냅샷 누적
            }

            for (int index = 0; index < taunts.Count; index++) // 전체 도발 순회
            {
                TauntEntry taunt = taunts[index]; // 현재 도발 조회

                if (taunt.EnemyRuntimeId != runtimeId) // 도발 대상 일치 확인
                {
                    continue; // 비대상 도발 제외
                }

                AccumulateSnapshot(buffer, BattleStatusEffectId.Taunt, taunt.ExpiresAt - now); // 도발 스냅샷 누적
            }
        }

        private static void AccumulateSnapshot(List<BattleStatusEffectSnapshot> buffer, BattleStatusEffectId id, float remainingSeconds) // 동일 종류 상태이상 병합 누적 (Day51 추가)
        {
            for (int index = 0; index < buffer.Count; index++) // 기존 수집 결과 순회
            {
                BattleStatusEffectSnapshot existing = buffer[index]; // 현재 스냅샷 조회

                if (existing.Id != id) // 동일 상태이상 종류 확인
                {
                    continue; // 다른 종류 스냅샷 제외
                }

                float longest = Mathf.Max(existing.RemainingSeconds, remainingSeconds); // 가장 늦게 끝나는 지속시간 선택
                buffer[index] = new BattleStatusEffectSnapshot(id, longest, existing.StackCount + 1); // 중첩 수 증가 스냅샷 갱신
                return; // 병합 완료
            }

            buffer.Add(new BattleStatusEffectSnapshot(id, remainingSeconds, 1)); // 신규 상태이상 스냅샷 추가
        }

        public static void RegisterRemovableDebuff(string runtimeId, string effectId) // 제거 가능한 Debuff 등록
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

            if (!effects.Contains(effectId)) // 같은 Debuff 출처 중복 여부 확인
            {
                effects.Add(effectId); // 제거 가능 Debuff 출처 등록
            }
        }

        public static int RemoveDebuffs(string runtimeId, int count) // 지정 개수 제거 가능 Debuff 제거
        {
            if (string.IsNullOrWhiteSpace(runtimeId) || count <= 0) // 제거 요청 유효성 확인
            {
                return 0; // 잘못된 제거 요청 0 반환
            }

            int removed = 0; // 실제 제거 개수 초기화

            if (removableDebuffs.TryGetValue(runtimeId, out List<string> effects) && effects.Count > 0) // 등록된 제거 가능 Debuff 존재 확인
            {
                int removeCount = Mathf.Min(count, effects.Count); // 실제 제거 가능 개수 계산

                for (int index = 0; index < removeCount; index++) // 제거 대상 Debuff 순회
                {
                    string sourceKey = effects[0]; // 가장 먼저 등록된 Debuff 출처 조회
                    effects.RemoveAt(0); // Debuff 출처 목록에서 제거
                    RemoveRuntimeEffectBySource(runtimeId, sourceKey); // 실제 Runtime 효과 제거
                }

                removed += removeCount; // 등록 기반 제거 개수 누적

                if (effects.Count == 0) // 대상 Debuff 목록 소진 여부 확인
                {
                    removableDebuffs.Remove(runtimeId); // 빈 대상 Debuff 목록 제거
                }
            }

            removed += RemoveUnregisteredDebuffs(runtimeId, count - removed); // 출처 미등록 디버프를 카탈로그 분류 기준으로 추가 제거 (Day51 추가)
            return removed; // 실제 제거 개수 반환
        }

        private static int RemoveUnregisteredDebuffs(string runtimeId, int count) // 카탈로그 분류 기준 미등록 디버프 제거 (Day51 추가, RegisterRemovableDebuff 누락 방어)
        {
            if (count <= 0) // 잔여 제거 요청 수 확인
            {
                return 0; // 추가 제거 없음 반환
            }

            int removed = 0; // 추가 제거 개수 초기화

            for (int index = modifiers.Count - 1; index >= 0 && removed < count; index--) // Modifier 목록 역순 순회
            {
                TimedModifier modifier = modifiers[index]; // 현재 Modifier 조회

                if (modifier.RuntimeId != runtimeId || !BattleStatusEffectCatalog.IsDebuff(BattleStatusEffectCatalog.FromModifierKind(modifier.Kind))) // 대상 및 디버프 분류 확인
                {
                    continue; // 비대상 또는 버프 Modifier 제외
                }

                modifiers.RemoveAt(index); // 분류 기준 디버프 Modifier 제거
                removed++; // 추가 제거 개수 증가
            }

            for (int index = periodicDamages.Count - 1; index >= 0 && removed < count; index--) // 주기 피해 목록 역순 순회
            {
                if (periodicDamages[index].TargetRuntimeId != runtimeId) // 대상 일치 확인
                {
                    continue; // 비대상 주기 피해 제외
                }

                periodicDamages.RemoveAt(index); // 지속 피해 디버프 제거
                removed++; // 추가 제거 개수 증가
            }

            for (int index = taunts.Count - 1; index >= 0 && removed < count; index--) // 도발 목록 역순 순회
            {
                if (taunts[index].EnemyRuntimeId != runtimeId) // 도발 대상 일치 확인
                {
                    continue; // 비대상 도발 제외
                }

                taunts.RemoveAt(index); // 도발 디버프 제거
                removed++; // 추가 제거 개수 증가
            }

            return removed; // 추가 제거 개수 반환
        }

        public static void AddPeriodicDamage(IBattleCombatantStats attacker, string targetRuntimeId, BattleDamageType damageType, int power, float tickInterval, int tickCount, string sourceKey, float nowSeconds = -1f) // 주기 피해 등록 또는 갱신
        {
            if (attacker == null || string.IsNullOrWhiteSpace(targetRuntimeId) || power <= 0 || tickInterval <= 0f || tickCount <= 0) // 주기 피해 입력 유효성 확인
            {
                return; // 잘못된 주기 피해 등록 중단
            }

            float now = ResolveNow(nowSeconds); // 현재 전투 시간 계산
            string safeSourceKey = sourceKey ?? string.Empty; // 주기 피해 출처 키 보정

            if (!string.IsNullOrWhiteSpace(safeSourceKey)) // 주기 피해 재사용 갱신 가능 여부 확인
            {
                for (int index = 0; index < periodicDamages.Count; index++) // 기존 주기 피해 목록 순회
                {
                    PeriodicDamageEntry existing = periodicDamages[index]; // 현재 주기 피해 조회

                    if (existing.TargetRuntimeId == targetRuntimeId && existing.SourceKey == safeSourceKey) // 같은 대상·출처 주기 피해 확인
                    {
                        existing.Attacker = attacker; // 최신 공격자 스탯 갱신
                        existing.DamageType = damageType; // 최신 피해 종류 갱신
                        existing.Power = power; // 최신 Tick 위력 갱신
                        existing.TickInterval = tickInterval; // 최신 Tick 간격 갱신
                        existing.RemainingTicks = tickCount; // 최신 남은 Tick 수 갱신
                        existing.NextTickAt = now + tickInterval; // 주기 피해 시작 시간 갱신
                        RegisterRemovableDebuff(targetRuntimeId, safeSourceKey); // 갱신된 DoT 정화 대상 등록
                        return; // 주기 피해 갱신 완료
                    }
                }
            }

            periodicDamages.Add(new PeriodicDamageEntry // 신규 주기 피해 생성
            {
                Attacker = attacker, // 주기 피해 공격자 저장
                TargetRuntimeId = targetRuntimeId, // 주기 피해 대상 저장
                DamageType = damageType, // 주기 피해 종류 저장
                Power = power, // Tick 위력 저장
                TickInterval = tickInterval, // Tick 간격 저장
                RemainingTicks = tickCount, // 남은 Tick 수 저장
                NextTickAt = now + tickInterval, // 첫 Tick 시간 저장
                SourceKey = safeSourceKey // 주기 피해 출처 저장
            }); // 신규 주기 피해 등록 완료
            RegisterRemovableDebuff(targetRuntimeId, safeSourceKey); // 신규 DoT 정화 대상 등록
        }

        public static void TickPeriodicEffects(float nowSeconds = -1f) // 현재 시간까지 주기 피해 갱신
        {
            float now = ResolveNow(nowSeconds); // 현재 전투 시간 계산
            CleanupExpired(now); // 만료 Modifier 및 도발 정리

            for (int index = periodicDamages.Count - 1; index >= 0; index--) // 주기 피해 목록 역순 순회
            {
                PeriodicDamageEntry entry = periodicDamages[index]; // 현재 주기 피해 조회

                if (entry.RemainingTicks <= 0) // 남은 Tick 수 확인
                {
                    periodicDamages.RemoveAt(index); // 완료된 주기 피해 제거
                    continue; // 다음 주기 피해 처리
                }

                if (now < entry.NextTickAt) // 다음 Tick 도달 여부 확인
                {
                    continue; // 아직 Tick 이전 처리
                }

                BattleActor target = registry == null ? null : registry.FindByRuntimeId(entry.TargetRuntimeId); // Registry에서 주기 피해 대상 조회

                if (target == null || !target.IsCombatReady || !target.Stats.IsAlive || entry.Attacker == null) // 주기 피해 대상 및 공격자 유효성 확인
                {
                    periodicDamages.RemoveAt(index); // 유효하지 않은 주기 피해 제거
                    continue; // 다음 주기 피해 처리
                }

                while (entry.RemainingTicks > 0 && now >= entry.NextTickAt) // 누적된 Tick 실행
                {
                    if (target == null || !target.IsCombatReady || !target.Stats.IsAlive) // Tick 중 대상 제거 또는 사망 여부 확인
                    {
                        break; // 제거된 대상의 남은 Tick 중단
                    }

                    BattleDamageResult result = BattleDamageResolver.Resolve(new BattleDamageRequest(entry.Attacker, target.Stats, entry.DamageType, entry.Power)); // Tick 피해 계산
                    target.ApplyDamage(result); // Tick 피해 실제 적용
                    entry.RemainingTicks--; // 남은 Tick 수 감소
                    entry.NextTickAt += entry.TickInterval; // 다음 Tick 시간 이동
                }

                if (entry.RemainingTicks <= 0 || target == null || !target.IsCombatReady || !target.Stats.IsAlive) // 주기 피해 완료 또는 대상 제거·사망 확인
                {
                    periodicDamages.RemoveAt(index); // 완료된 주기 피해 제거
                }
            }
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

        private static void RemoveRuntimeEffectBySource(string runtimeId, string sourceKey) // Debuff 출처 기반 Runtime 효과 제거
        {
            if (string.IsNullOrWhiteSpace(sourceKey)) // Debuff 출처 키 확인
            {
                return; // 출처 없는 Runtime 효과 제거 중단
            }

            for (int index = modifiers.Count - 1; index >= 0; index--) // Modifier 목록 역순 순회
            {
                TimedModifier modifier = modifiers[index]; // 현재 Modifier 조회

                if (modifier.RuntimeId == runtimeId && modifier.SourceKey == sourceKey) // 대상 및 출처 일치 확인
                {
                    modifiers.RemoveAt(index); // 일치 Modifier 제거
                }
            }

            for (int index = periodicDamages.Count - 1; index >= 0; index--) // 주기 피해 목록 역순 순회
            {
                PeriodicDamageEntry entry = periodicDamages[index]; // 현재 주기 피해 조회

                if (entry.TargetRuntimeId == runtimeId && entry.SourceKey == sourceKey) // 대상 및 출처 일치 확인
                {
                    periodicDamages.RemoveAt(index); // 일치 주기 피해 제거
                }
            }

            for (int index = taunts.Count - 1; index >= 0; index--) // 도발 목록 역순 순회
            {
                TauntEntry taunt = taunts[index]; // 현재 도발 조회

                if (taunt.EnemyRuntimeId == runtimeId && taunt.SourceKey == sourceKey) // 대상 및 출처 일치 확인
                {
                    taunts.RemoveAt(index); // 일치 도발 제거
                }
            }
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
