using System.Collections.Generic; // 목록 자료형
using UnityEngine; // Unity 시간 및 수학 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static partial class BattleSkillRuntimeState // 스킬 기반 전투 Runtime 상태 저장소 — 정화·Debuff 제거 (최적화 분리)
    {
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
    }
}
