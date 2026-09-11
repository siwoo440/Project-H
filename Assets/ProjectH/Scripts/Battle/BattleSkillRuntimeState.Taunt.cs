using System.Collections.Generic; // 목록 자료형
using UnityEngine; // Unity 시간 및 수학 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static partial class BattleSkillRuntimeState // 스킬 기반 전투 Runtime 상태 저장소 — 도발 (최적화 분리)
    {
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
                    NoteExpiry(existing.ExpiresAt); // 만료 예정 시각 갱신 (최적화)
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
            NoteExpiry(now + duration); // 만료 예정 시각 기록 (최적화)
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
    }
}
