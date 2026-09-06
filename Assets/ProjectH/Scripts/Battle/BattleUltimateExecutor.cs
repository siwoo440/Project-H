using UnityEngine; // Unity 객체 조회와 로그 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public readonly struct BattleUltimateExecutionResult // 궁극기 실행 결과
    {
        public bool Succeeded { get; } // 궁극기 실행 성공 여부
        public string CharacterId { get; } // 궁극기 사용자 캐릭터 ID
        public string UltimateName { get; } // 궁극기 표시 이름
        public int AppliedEffectCount { get; } // 실제 적용 효과 종류 수
        public int AffectedTargetCount { get; } // 실제 영향 대상 누적 수
        public string Message { get; } // 실행 결과 메시지

        public BattleUltimateExecutionResult(bool succeeded, string characterId, string ultimateName, int appliedEffectCount, int affectedTargetCount, string message) // 궁극기 실행 결과 생성
        {
            Succeeded = succeeded; // 실행 성공 여부 저장
            CharacterId = characterId ?? string.Empty; // 캐릭터 ID 저장
            UltimateName = ultimateName ?? string.Empty; // 궁극기 이름 저장
            AppliedEffectCount = Mathf.Max(0, appliedEffectCount); // 적용 효과 수 음수 방지
            AffectedTargetCount = Mathf.Max(0, affectedTargetCount); // 영향 대상 수 음수 방지
            Message = message ?? string.Empty; // 실행 결과 메시지 저장
        }

        public static BattleUltimateExecutionResult Failure(string characterId, string message) // 궁극기 실행 실패 결과 생성
        {
            return new BattleUltimateExecutionResult(false, characterId, BattleUltimateEffectExecutor.GetUltimateName(characterId), 0, 0, message); // 실패 결과 반환
        }
    }

    public static class BattleUltimateExecutor // 궁극기 전용 실행 관리자
    {
        public static BattleUltimateExecutionResult TryExecute(string characterId) // 현재 전투 Registry 기반 궁극기 실행 시도
        {
            BattleCombatRegistry registry = Object.FindFirstObjectByType<BattleCombatRegistry>(); // 현재 Scene 전투 Registry 조회
            return TryExecute(characterId, registry); // 조회 Registry 기반 궁극기 실행 결과 반환
        }

        public static BattleUltimateExecutionResult TryExecute(string characterId, BattleCombatRegistry registry) // 지정 Registry 기반 궁극기 실행 시도
        {
            if (string.IsNullOrWhiteSpace(characterId)) // 캐릭터 ID 유효성 확인
            {
                return BattleUltimateExecutionResult.Failure(characterId, "궁극기 캐릭터 ID가 없습니다."); // 잘못된 캐릭터 ID 실패 반환
            }

            if (registry == null) // 전투 Registry 존재 확인
            {
                return BattleUltimateExecutionResult.Failure(characterId, "전투 Registry를 찾을 수 없습니다."); // Registry 누락 실패 반환
            }

            if (!BattleUltimateEffectExecutor.IsSupported(characterId)) // 초기 4인 궁극기 지원 여부 확인
            {
                return BattleUltimateExecutionResult.Failure(characterId, "아직 구현되지 않은 궁극기입니다."); // 미지원 궁극기 실패 반환
            }

            if (registry.CountLiving(BattleTeam.Ally) <= 0 || registry.CountLiving(BattleTeam.Enemy) <= 0) // 실제 전투 진행 상태 확인
            {
                return BattleUltimateExecutionResult.Failure(characterId, "전투 진행 중에만 궁극기를 사용할 수 있습니다."); // 전투 종료 상태 실패 반환
            }

            BattleActor owner = FindLivingOwner(characterId, registry); // 궁극기 사용자 생존 액터 조회

            if (owner == null || owner.Stats == null) // 궁극기 사용자 유효성 확인
            {
                return BattleUltimateExecutionResult.Failure(characterId, "생존한 궁극기 사용자를 찾을 수 없습니다."); // 사용자 없음 실패 반환
            }

            if (!BattleUltimateGaugeRuntimeState.CanUseUltimate(characterId, owner.Stats.IsAlive)) // 생존 및 완충 게이지 사용 가능 여부 확인
            {
                return BattleUltimateExecutionResult.Failure(characterId, "궁극기 게이지가 준비되지 않았습니다."); // Ready 미충족 실패 반환
            }

            if (!BattleUltimateGaugeRuntimeState.TryConsumeGauge(characterId, owner.Stats.IsAlive)) // 궁극기 게이지 100 소비 시도
            {
                return BattleUltimateExecutionResult.Failure(characterId, "궁극기 게이지 소비에 실패했습니다."); // 게이지 소비 실패 반환
            }

            owner.ShowAction(BattleActionKind.Ultimate); // 궁극기 전용 행동 표시 실행
            BattleUltimateEffectExecutionResult effectResult = BattleUltimateEffectExecutor.Execute(characterId, owner, registry); // 캐릭터 고유 궁극기 효과 1회 실행
            string ultimateName = BattleUltimateEffectExecutor.GetUltimateName(characterId); // 궁극기 표시 이름 조회
            Debug.Log($"[Project H][ULTIMATE] {owner.Stats.RuntimeId}, Character={characterId}, Name={ultimateName}, Effects={effectResult.AppliedEffectCount}, Targets={effectResult.AffectedTargetCount}"); // 궁극기 실행 로그 출력
            return new BattleUltimateExecutionResult(true, characterId, ultimateName, effectResult.AppliedEffectCount, effectResult.AffectedTargetCount, $"{ultimateName} 발동"); // 궁극기 실행 성공 결과 반환
        }

        private static BattleActor FindLivingOwner(string characterId, BattleCombatRegistry registry) // 캐릭터 ID 기반 생존 궁극기 사용자 조회
        {
            if (registry == null) // Registry 존재 확인
            {
                return null; // Registry 없음 반환
            }

            for (int index = 0; index < registry.Actors.Count; index++) // 등록 전투 액터 순회
            {
                BattleActor actor = registry.Actors[index]; // 현재 전투 액터 조회

                if (actor == null || actor.Team != BattleTeam.Ally || !actor.IsCombatReady || !actor.Stats.IsAlive) // 아군 생존 전투 상태 확인
                {
                    continue; // 궁극기 사용자 후보 제외
                }

                BattleStats stats = actor.Stats as BattleStats; // 아군 전투 스탯 변환

                if (stats != null && stats.CharacterId == characterId) // 캐릭터 ID 일치 여부 확인
                {
                    return actor; // 일치 생존 사용자 반환
                }
            }

            return null; // 궁극기 사용자 조회 실패 반환
        }
    }
}
