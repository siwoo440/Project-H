using System.Collections.Generic; // 목록 자료형
using UnityEngine; // Unity 시간 및 수학 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static partial class BattleSkillRuntimeState // 스킬 기반 전투 Runtime 상태 저장소 — 주기 피해·반격 (최적화 분리)
    {
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
    }
}
