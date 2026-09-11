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

    public readonly struct BattleUltimateBeginResult // 궁극기 선행 단계(검증·게이지 소비) 결과 (Day50 신규)
    {
        public bool Succeeded { get; } // 선행 단계 성공 여부
        public string CharacterId { get; } // 궁극기 사용자 캐릭터 ID
        public string UltimateName { get; } // 궁극기 표시 이름
        public BattleActor Owner { get; } // 궁극기 사용자 전투 액터
        public BattleCombatRegistry Registry { get; } // 효과 실행 대상 전투 Registry
        public string Message { get; } // 선행 단계 결과 메시지

        public BattleUltimateBeginResult(bool succeeded, string characterId, string ultimateName, BattleActor owner, BattleCombatRegistry registry, string message) // 궁극기 선행 단계 결과 생성
        {
            Succeeded = succeeded; // 선행 단계 성공 여부 저장
            CharacterId = characterId ?? string.Empty; // 캐릭터 ID 저장
            UltimateName = ultimateName ?? string.Empty; // 궁극기 이름 저장
            Owner = owner; // 궁극기 사용자 액터 저장
            Registry = registry; // 전투 Registry 저장
            Message = message ?? string.Empty; // 선행 단계 결과 메시지 저장
        }

        public static BattleUltimateBeginResult Failure(string characterId, string message) // 궁극기 선행 단계 실패 결과 생성
        {
            return new BattleUltimateBeginResult(false, characterId, BattleUltimateEffectExecutor.GetUltimateName(characterId), null, null, message); // 선행 단계 실패 결과 반환
        }
    }

    public static class BattleUltimateExecutor // 궁극기 전용 실행 관리자
    {
        public static BattleUltimateExecutionResult TryExecute(string characterId) // 현재 전투 Registry 기반 궁극기 즉시 실행 시도 (배율 1.0 호환 경로)
        {
            BattleCombatRegistry registry = Object.FindFirstObjectByType<BattleCombatRegistry>(); // 현재 Scene 전투 Registry 조회
            return TryExecute(characterId, registry); // 조회 Registry 기반 궁극기 실행 결과 반환
        }

        public static BattleUltimateExecutionResult TryExecute(string characterId, BattleCombatRegistry registry) // 지정 Registry 기반 궁극기 즉시 실행 시도 (배율 1.0 호환 경로)
        {
            BattleUltimateBeginResult begin = TryBeginUltimate(characterId, registry); // 궁극기 선행 단계 실행

            if (!begin.Succeeded) // 선행 단계 실패 여부 확인
            {
                return BattleUltimateExecutionResult.Failure(characterId, begin.Message); // 선행 단계 실패 결과 반환
            }

            return ExecuteUltimateEffect(begin, 1f); // 기본 위력 배율 1.0 효과 실행 결과 반환
        }

        public static BattleUltimateBeginResult TryBeginUltimate(string characterId) // 현재 전투 Registry 기반 궁극기 선행 단계 시도 (Day50 신규)
        {
            BattleCombatRegistry registry = Object.FindFirstObjectByType<BattleCombatRegistry>(); // 현재 Scene 전투 Registry 조회
            return TryBeginUltimate(characterId, registry); // 조회 Registry 기반 선행 단계 결과 반환
        }

        public static BattleUltimateBeginResult TryBeginUltimate(string characterId, BattleCombatRegistry registry) // 지정 Registry 기반 궁극기 선행 단계 시도 (검증 후 게이지만 소비하고 효과는 미실행)
        {
            if (string.IsNullOrWhiteSpace(characterId)) // 캐릭터 ID 유효성 확인
            {
                return BattleUltimateBeginResult.Failure(characterId, "궁극기 캐릭터 ID가 없습니다."); // 잘못된 캐릭터 ID 실패 반환
            }

            if (registry == null) // 전투 Registry 존재 확인
            {
                return BattleUltimateBeginResult.Failure(characterId, "전투 Registry를 찾을 수 없습니다."); // Registry 누락 실패 반환
            }

            if (!BattleUltimateEffectExecutor.IsSupported(characterId)) // 초기 4인 궁극기 지원 여부 확인
            {
                return BattleUltimateBeginResult.Failure(characterId, "아직 구현되지 않은 궁극기입니다."); // 미지원 궁극기 실패 반환
            }

            if (registry.CountLiving(BattleTeam.Ally) <= 0 || registry.CountLiving(BattleTeam.Enemy) <= 0) // 실제 전투 진행 상태 확인
            {
                return BattleUltimateBeginResult.Failure(characterId, "전투 진행 중에만 궁극기를 사용할 수 있습니다."); // 전투 종료 상태 실패 반환
            }

            BattleActor owner = FindLivingOwner(characterId, registry); // 궁극기 사용자 생존 액터 조회

            if (owner == null || owner.Stats == null) // 궁극기 사용자 유효성 확인
            {
                return BattleUltimateBeginResult.Failure(characterId, "생존한 궁극기 사용자를 찾을 수 없습니다."); // 사용자 없음 실패 반환
            }

            if (!BattleUltimateGaugeRuntimeState.CanUseUltimate(characterId, owner.Stats.IsAlive)) // 생존 및 완충 게이지 사용 가능 여부 확인
            {
                return BattleUltimateBeginResult.Failure(characterId, "궁극기 게이지가 준비되지 않았습니다."); // Ready 미충족 실패 반환
            }

            if (!BattleUltimateGaugeRuntimeState.TryConsumeGauge(characterId, owner.Stats.IsAlive)) // 궁극기 게이지 100 소비 시도
            {
                return BattleUltimateBeginResult.Failure(characterId, "궁극기 게이지 소비에 실패했습니다."); // 게이지 소비 실패 반환
            }

            string ultimateName = BattleUltimateEffectExecutor.GetUltimateName(characterId); // 궁극기 표시 이름 조회
            return new BattleUltimateBeginResult(true, characterId, ultimateName, owner, registry, ultimateName + " 준비"); // 선행 단계 성공 결과 반환
        }

        public static BattleUltimateExecutionResult ExecuteUltimateEffect(BattleUltimateBeginResult begin, float powerMultiplier) // 선행 단계 결과 기반 궁극기 효과 실행 (Day50 신규, 리듬 성적 배율 적용)
        {
            if (!begin.Succeeded) // 선행 단계 성공 여부 확인
            {
                return BattleUltimateExecutionResult.Failure(begin.CharacterId, "궁극기 선행 단계가 완료되지 않았습니다."); // 잘못된 실행 요청 실패 반환
            }

            BattleActor owner = begin.Owner; // 궁극기 사용자 액터 조회

            if (owner == null || owner.Stats == null || !owner.IsCombatReady || !owner.Stats.IsAlive) // 챌린지 진행 중 사용자 상태 변화 확인
            {
                return BattleUltimateExecutionResult.Failure(begin.CharacterId, "궁극기 사용자가 더 이상 전투 상태가 아닙니다."); // 사용자 이탈 실패 반환
            }

            if (begin.Registry == null || begin.Registry.CountLiving(BattleTeam.Enemy) <= 0) // 챌린지 진행 중 전투 종료 여부 확인
            {
                return BattleUltimateExecutionResult.Failure(begin.CharacterId, "전투가 이미 종료되어 궁극기 효과를 적용하지 않았습니다."); // 전투 종료 실패 반환
            }

            float safeMultiplier = Mathf.Max(0f, powerMultiplier); // 위력 배율 음수 방지
            owner.ShowAction(BattleActionKind.Ultimate); // 궁극기 전용 행동 표시 실행
            BattleUltimateEffectExecutionResult effectResult = BattleUltimateEffectExecutor.Execute(begin.CharacterId, owner, begin.Registry, safeMultiplier); // 캐릭터 고유 궁극기 효과 1회 실행
            BattleBondSkills.OnUltimateExecuted(begin.CharacterId, owner, begin.Registry); // 결속 5단계 결속 스킬 (엘렌 철벽의 맹세·릴리아 마력 공명, Day59 추가)
            Debug.Log($"[Project H][ULTIMATE] {owner.Stats.RuntimeId}, Character={begin.CharacterId}, Name={begin.UltimateName}, Power={safeMultiplier:0.00}, Effects={effectResult.AppliedEffectCount}, Targets={effectResult.AffectedTargetCount}"); // 궁극기 실행 로그 출력
            return new BattleUltimateExecutionResult(true, begin.CharacterId, begin.UltimateName, effectResult.AppliedEffectCount, effectResult.AffectedTargetCount, begin.UltimateName + " 발동"); // 궁극기 실행 성공 결과 반환
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
