using System.Collections.Generic; // 목록 자료형
using ProjectH.Data; // 보스 패턴 데이터 기능
using UnityEngine; // Unity 기본 기능

namespace ProjectH.Battle.Boss // 프로젝트 전투 보스 영역 (Day54)
{
    [DisallowMultipleComponent] // 중복 보스 컨트롤러 방지
    public sealed class BattleBossController : MonoBehaviour // 보스 페이즈 전환 및 특수 패턴 실행 컨트롤러 (Day54 신규)
    {
        private const string PhaseAttackSource = "BOSS_PHASE:ATK"; // 페이즈 공격력 증가 출처 키
        private const string PhaseSpeedSource = "BOSS_PHASE:SPD"; // 페이즈 공격 속도 증가 출처 키
        private const string PhaseGuardSource = "BOSS_PHASE:GUARD"; // 페이즈 전환 무적 출처 키
        private const string FortifySource = "BOSS_PATTERN:FORTIFY"; // 자기 강화 패턴 출처 키
        private const string FocusStunSource = "BOSS_PATTERN:FOCUS_STUN"; // 집중 강타 기절 출처 키

        private readonly List<BattleActor> targetBuffer = new List<BattleActor>(); // 공격 대상 수집 재사용 버퍼
        private BattleEnemyView enemyView; // 보스 적군 뷰
        private BattleActor actor; // 보스 전투 액터
        private BattleEnemyBrain brain; // 보스 타겟 판단 Brain
        private BattleCombatRegistry registry; // 현재 전투 Registry
        private BossPatternData patternData; // 보스 페이즈·패턴 데이터
        private BossPatternScheduler scheduler; // 특수 패턴 스케줄러
        private BattleBossTelegraphView telegraph; // 머리 위 예고 표시
        private float battleStartTime; // 보스 전투 시작 시각
        private bool started; // 스케줄러 초기화 여부

        public int CurrentPhase { get; private set; } = BossPhaseResolver.FirstPhase; // 현재 페이즈 번호 (1부터 시작)

        public void Configure(BattleEnemyView view, BattleEnemyBrain enemyBrain, BattleCombatRegistry combatRegistry, BossPatternData data) // 보스 컨트롤러 참조 설정
        {
            enemyView = view; // 보스 적군 뷰 저장
            actor = view == null ? null : view.Actor; // 보스 전투 액터 저장
            brain = enemyBrain; // 보스 Brain 저장
            registry = combatRegistry; // 전투 Registry 저장
            patternData = data; // 보스 패턴 데이터 저장
        }

        private void Start() // 보스 컨트롤러 시작 (보스 체력바 연결 이후 한 프레임 뒤 실행)
        {
            if (patternData == null || actor == null) // 필수 참조 확인
            {
                enabled = false; // 패턴 데이터 없는 보스는 일반 적으로 동작
                return; // 초기화 중단
            }

            battleStartTime = Time.time; // 전투 시작 시각 기록
            scheduler = new BossPatternScheduler(patternData.Patterns, battleStartTime, patternData.InitialDelaySeconds, patternData.PatternIntervalSeconds); // 특수 패턴 스케줄러 생성
            telegraph = BattleBossTelegraphView.AttachAbove(enemyView.GetWorldAnchorRect()); // 보스 머리 위 예고 표시 부착
            BattleBossPresentationController.EnsureRuntime().SetPhaseMarkers(CollectPhaseThresholds()); // 보스 체력바 페이즈 눈금 표시
            ApplyPhaseModifiers(CurrentPhase); // 1페이즈 스탯 보정 적용
            started = true; // 초기화 완료 기록
        }

        private void Update() // 보스 페이즈·패턴 진행 갱신
        {
            if (!started || actor == null || !actor.IsCombatReady || actor.Stats == null || !actor.Stats.IsAlive) // 보스 전투 가능 상태 확인
            {
                HideTelegraphOnDeath(); // 사망 시 예고 표시 정리
                return; // 갱신 중단
            }

            float now = Time.time; // 현재 전투 시간 조회 (일시정지 시 정지)
            UpdatePhase(now); // 체력·시간 기반 페이즈 전환 확인

            if (BattleSkillRuntimeState.IsInvulnerable(actor.Stats.RuntimeId)) // 페이즈 전환 무적 중 확인
            {
                return; // 전환 연출 중 패턴 진행 중단
            }

            if (scheduler.IsWindingUp) // 예고 진행 여부 확인
            {
                UpdateWindup(now); // 예고 진행 및 저지·발동 처리
                return; // 예고 중 새 패턴 선택 중단
            }

            if (BattleSkillRuntimeState.IsStunned(actor.Stats.RuntimeId)) // 기절·흐트러짐 경직 확인
            {
                return; // 행동 불능 중 새 패턴 시작 중단
            }

            if (scheduler.TryBeginNext(CurrentPhase, now)) // 다음 패턴 예고 시작 시도
            {
                BossPatternDefinition pattern = scheduler.ActivePattern; // 시작 패턴 조회
                Debug.Log($"[Project H][BOSS] {actor.Stats.RuntimeId}, Windup={pattern.DisplayName}, Phase={CurrentPhase}, Seconds={pattern.WindupSeconds:0.0}"); // 패턴 예고 시작 로그
            }
        }

        private void UpdatePhase(float now) // 체력·시간 기반 페이즈 전환 처리
        {
            int resolved = BossPhaseResolver.ResolvePhase(patternData.Phases, GetHealthRatio(), now - battleStartTime, CurrentPhase); // 현재 도달 페이즈 판정

            if (resolved <= CurrentPhase) // 페이즈 상승 여부 확인
            {
                return; // 페이즈 유지
            }

            CurrentPhase = resolved; // 신규 페이즈 저장
            BossPhaseDefinition phase = BossPhaseResolver.GetPhase(patternData.Phases, CurrentPhase); // 신규 페이즈 정의 조회

            if (scheduler.IsWindingUp) // 전환 시점 예고 진행 여부 확인
            {
                scheduler.CancelActive(now); // 전환으로 기존 예고 정리
            }

            telegraph?.Hide(); // 예고 표시 정리
            ApplyPhaseModifiers(CurrentPhase); // 신규 페이즈 스탯 보정 적용
            float guardSeconds = patternData.PhaseTransitionInvulnerableSeconds; // 전환 무적 시간 조회

            if (guardSeconds > 0f) // 전환 무적 사용 여부 확인
            {
                BattleSkillRuntimeState.AddModifier(actor.Stats.RuntimeId, BattleRuntimeModifierKind.Invulnerable, 1f, guardSeconds, PhaseGuardSource); // 페이즈 전환 무적 적용
            }

            scheduler.Postpone(now, guardSeconds + patternData.PatternIntervalSeconds); // 전환 직후 패턴 즉시 사용 방지
            string phaseName = phase == null ? string.Empty : phase.DisplayName; // 페이즈 표시 이름 조회
            BattleBossPresentationController.EnsureRuntime().AnnouncePhase(CurrentPhase, phaseName); // 페이즈 전환 중앙 연출 실행
            Debug.Log($"[Project H][BOSS] {actor.Stats.RuntimeId}, Phase={CurrentPhase} ({phaseName}), Hp={GetHealthRatio():P0}"); // 페이즈 전환 로그
        }

        private void ApplyPhaseModifiers(int phaseNumber) // 페이즈별 공격력·공격 속도 보정 적용
        {
            BossPhaseDefinition phase = BossPhaseResolver.GetPhase(patternData.Phases, phaseNumber); // 페이즈 정의 조회

            if (phase == null) // 페이즈 정의 존재 확인
            {
                return; // 보정 적용 중단
            }

            if (phase.AttackBonus > 0f) // 공격력 보정 존재 확인
            {
                BattleSkillRuntimeState.AddModifier(actor.Stats.RuntimeId, BattleRuntimeModifierKind.AttackPercent, phase.AttackBonus, float.PositiveInfinity, PhaseAttackSource); // 같은 출처 덮어쓰기로 페이즈 공격력 보정 적용
            }

            if (phase.AttackSpeedBonus > 0f) // 공격 속도 보정 존재 확인
            {
                BattleSkillRuntimeState.AddModifier(actor.Stats.RuntimeId, BattleRuntimeModifierKind.AttackSpeedPercent, phase.AttackSpeedBonus, float.PositiveInfinity, PhaseSpeedSource); // 같은 출처 덮어쓰기로 페이즈 가속 적용
            }
        }

        private void UpdateWindup(float now) // 예고 진행 및 저지·발동 처리
        {
            BossPatternDefinition pattern = scheduler.ActivePattern; // 예고 패턴 조회

            if (pattern.Interruptible && BattleDisarrayRuntimeState.IsDisarrayed(actor.Stats.RuntimeId)) // 저지 가능 패턴의 흐트러짐 발생 확인
            {
                scheduler.CancelActive(now); // 예고 패턴 취소 (쿨다운은 동일 소모)
                telegraph?.ShowInterrupted(pattern.DisplayName); // 저지 성공 표시
                Debug.Log($"[Project H][BOSS] {actor.Stats.RuntimeId}, Interrupted={pattern.DisplayName}"); // 패턴 저지 로그
                return; // 예고 처리 종료
            }

            if (BattleSkillRuntimeState.IsStunned(actor.Stats.RuntimeId)) // 일반 기절 또는 저지 불가 패턴의 경직 확인
            {
                telegraph?.ShowWindup(pattern.DisplayName, scheduler.WindupProgress, pattern.Interruptible); // 예고 표시 유지 (진행은 정지)
                return; // 기절 중 예고 진행 정지
            }

            if (!scheduler.TickWindup(Time.deltaTime)) // 예고 시간 진행 및 완료 여부 확인
            {
                telegraph?.ShowWindup(pattern.DisplayName, scheduler.WindupProgress, pattern.Interruptible); // 예고 진행 표시 갱신
                return; // 예고 진행 유지
            }

            scheduler.CompleteActive(now); // 예고 완료 확정 (쿨다운 적용)
            ExecutePattern(pattern); // 패턴 효과 실행
            telegraph?.ShowExecuted(pattern.DisplayName); // 패턴 발동 표시
        }

        private void ExecutePattern(BossPatternDefinition pattern) // 보스 특수 패턴 효과 실행 (기존 전투 도구 조합)
        {
            actor.ShowAction(BattleActionKind.Skill); // 보스 머리 위 행동 표시
            int affected = 0; // 영향 대상 수 초기화

            switch (pattern.Kind) // 패턴 종류 분기
            {
                case BossPatternKind.AreaStrike: // 전체 공격 처리
                    affected = ExecuteAreaStrike(pattern); // 아군 전체 피해 적용
                    break; // 전체 공격 분기 종료
                case BossPatternKind.FocusStrike: // 집중 강타 처리
                    affected = ExecuteFocusStrike(pattern); // 단일 대상 피해 및 기절 적용
                    break; // 집중 강타 분기 종료
                case BossPatternKind.SelfFortify: // 자기 강화 처리
                    BattleSkillRuntimeState.AddModifier(actor.Stats.RuntimeId, BattleRuntimeModifierKind.DefensePercent, pattern.EffectValue, pattern.EffectDuration, FortifySource); // 보스 방어력 증가 적용 (상태이상 칩에 자동 표시)
                    affected = 1; // 보스 자신 영향 기록
                    break; // 자기 강화 분기 종료
                case BossPatternKind.DisarrayRecover: // 흐트러짐 회복 처리
                    affected = BattleDisarrayRuntimeState.RecoverDisarray(actor.Stats.RuntimeId, pattern.EffectValue) > 0 ? 1 : 0; // 보스 흐트러짐 누적 비율 회복
                    break; // 흐트러짐 회복 분기 종료
            }

            Debug.Log($"[Project H][BOSS] {actor.Stats.RuntimeId}, Execute={pattern.DisplayName}, Kind={pattern.Kind}, Affected={affected}"); // 패턴 발동 로그
        }

        private int ExecuteAreaStrike(BossPatternDefinition pattern) // 아군 전체 공격 실행
        {
            CollectLivingAllies(); // 생존 아군 스냅샷 수집 (사망 시 Registry 변경 대비)
            int hit = 0; // 적중 대상 수 초기화

            for (int index = 0; index < targetBuffer.Count; index++) // 생존 아군 순회
            {
                if (ApplyPatternDamage(targetBuffer[index], pattern)) // 패턴 피해 적용 확인
                {
                    hit++; // 적중 대상 수 증가
                }
            }

            return hit; // 적중 대상 수 반환
        }

        private int ExecuteFocusStrike(BossPatternDefinition pattern) // 단일 대상 강타 및 기절 실행
        {
            BattleActor target = brain != null && IsLivingAlly(brain.CurrentTarget) ? brain.CurrentTarget : FindFirstLivingAlly(); // Brain 현재 타겟 우선, 없으면 첫 생존 아군 선택

            if (!ApplyPatternDamage(target, pattern)) // 강타 피해 적용 확인
            {
                return 0; // 강타 실패 반환
            }

            if (pattern.EffectDuration > 0f && IsLivingAlly(target)) // 기절 지속시간 및 생존 확인
            {
                BattleSkillRuntimeState.AddModifier(target.Stats.RuntimeId, BattleRuntimeModifierKind.Stun, 1f, pattern.EffectDuration, FocusStunSource); // 강타 대상 기절 적용
                BattleSkillRuntimeState.RegisterRemovableDebuff(target.Stats.RuntimeId, FocusStunSource); // 세레나 정화 대상 등록
            }

            return 1; // 강타 대상 1명 반환
        }

        private bool ApplyPatternDamage(BattleActor target, BossPatternDefinition pattern) // 패턴 공격력 계수 피해 공통 적용
        {
            if (!IsLivingAlly(target)) // 피해 대상 유효성 확인
            {
                return false; // 잘못된 대상 실패 반환
            }

            int power = Mathf.RoundToInt(actor.Stats.Attack * pattern.PowerRatio); // 공격력 계수 기반 원본 위력 계산

            if (power <= 0) // 원본 위력 확인
            {
                return false; // 0 위력 실패 반환
            }

            BattleDamageResult result = BattleDamageResolver.Resolve(new BattleDamageRequest(actor.Stats, target.Stats, ConvertDamageType(pattern.DamageType), power)); // 공통 피해 계산 (버프·속성 배율 자동 적용)
            return target.ApplyDamage(result) > 0; // 실제 피해 적용 결과 반환
        }

        private void CollectLivingAllies() // 생존 아군 스냅샷 수집
        {
            targetBuffer.Clear(); // 이전 수집 결과 초기화

            if (registry == null) // Registry 존재 확인
            {
                return; // 수집 중단
            }

            for (int index = 0; index < registry.Actors.Count; index++) // 등록 전투 액터 순회
            {
                BattleActor candidate = registry.Actors[index]; // 후보 액터 조회

                if (IsLivingAlly(candidate)) // 생존 아군 확인
                {
                    targetBuffer.Add(candidate); // 공격 대상 추가
                }
            }
        }

        private BattleActor FindFirstLivingAlly() // 첫 생존 아군 조회
        {
            CollectLivingAllies(); // 생존 아군 수집
            return targetBuffer.Count > 0 ? targetBuffer[0] : null; // 첫 대상 또는 null 반환
        }

        private static bool IsLivingAlly(BattleActor candidate) // 생존 아군 여부 확인
        {
            return candidate != null && candidate.Team == BattleTeam.Ally && candidate.IsCombatReady && candidate.Stats != null && candidate.Stats.IsAlive; // 아군·전투 가능·생존 여부 반환
        }

        private static BattleDamageType ConvertDamageType(SkillDamageType damageType) // 데이터 피해 종류를 전투 피해 종류로 변환
        {
            switch (damageType) // 데이터 피해 종류 분기
            {
                case SkillDamageType.Magic: // 마법 피해 처리
                    return BattleDamageType.Magic; // 전투 마법 피해 반환
                case SkillDamageType.True: // 방어 무시 피해 처리
                    return BattleDamageType.True; // 전투 방어 무시 피해 반환
                default: // 물리 피해 처리
                    return BattleDamageType.Physical; // 전투 물리 피해 반환
            }
        }

        private List<float> CollectPhaseThresholds() // 2페이즈 이후 진입 체력 비율 목록 수집 (체력바 눈금용)
        {
            List<float> thresholds = new List<float>(); // 눈금 비율 목록 생성

            for (int index = 1; index < patternData.Phases.Count; index++) // 2페이즈 이후 정의 순회
            {
                BossPhaseDefinition phase = patternData.Phases[index]; // 페이즈 정의 조회

                if (phase != null && phase.EnterHealthRatio > 0f && phase.EnterHealthRatio < 1f) // 유효 체력 비율 확인
                {
                    thresholds.Add(phase.EnterHealthRatio); // 눈금 비율 추가
                }
            }

            return thresholds; // 눈금 비율 목록 반환
        }

        private float GetHealthRatio() // 보스 현재 체력 비율 계산 (공통 스탯 계약에 비율 속성이 없어 직접 계산)
        {
            return actor.Stats.MaxHp <= 0 ? 0f : Mathf.Clamp01(actor.Stats.CurrentHp / (float)actor.Stats.MaxHp); // 현재 체력 비율 반환
        }

        private void HideTelegraphOnDeath() // 보스 사망 시 예고 표시 정리
        {
            if (started && telegraph != null && telegraph.gameObject.activeSelf && (actor == null || actor.Stats == null || !actor.Stats.IsAlive)) // 사망 후 예고 표시 잔존 확인
            {
                telegraph.Hide(); // 예고 표시 숨김
            }
        }
    }
}
