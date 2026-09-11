using System.Collections.Generic; // 목록 자료형
using UnityEngine; // Unity 시간 및 수학 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static partial class BattleSkillRuntimeState // 스킬 기반 전투 Runtime 상태 저장소 — 상태이상 스냅샷 수집 (Day51) (최적화 분리)
    {
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
    }
}
