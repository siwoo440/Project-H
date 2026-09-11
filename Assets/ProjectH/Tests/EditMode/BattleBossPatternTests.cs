using System.Collections.Generic; // 목록 자료형
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 흐트러짐 게이지 기능
using ProjectH.Battle.Boss; // 보스 페이즈·패턴 기능
using ProjectH.Data; // 보스 패턴 데이터 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleBossPatternTests // Day54 보스 페이즈·패턴 회귀 테스트
    {
        private static List<BossPhaseDefinition> CreatePhases() // 3페이즈 테스트 정의 생성 (100% · 60% · 25%)
        {
            return new List<BossPhaseDefinition> // 페이즈 목록 생성
            {
                new BossPhaseDefinition("수호", 1f, 0f, 0f, 0f), // 1페이즈 정의
                new BossPhaseDefinition("분노", 0.6f, 0f, 0.2f, 0.15f), // 2페이즈 정의
                new BossPhaseDefinition("광폭", 0.25f, 180f, 0.35f, 0.35f) // 3페이즈 정의 (180초 시간 강제 전환)
            };
        }

        private static List<BossPatternDefinition> CreatePatterns() // 테스트 패턴 목록 생성
        {
            return new List<BossPatternDefinition> // 패턴 목록 생성
            {
                new BossPatternDefinition("파동", BossPatternKind.AreaStrike, 1.4f, 2f, 10f, 1, true), // 0번 1페이즈 전체 공격
                new BossPatternDefinition("갑주", BossPatternKind.SelfFortify, 0f, 1f, 10f, 1, true), // 1번 1페이즈 자기 강화
                new BossPatternDefinition("강타", BossPatternKind.FocusStrike, 3f, 3f, 10f, 2, true), // 2번 2페이즈 집중 강타
                new BossPatternDefinition("종말", BossPatternKind.AreaStrike, 2.6f, 4f, 10f, 3, false) // 3번 3페이즈 저지 불가 필살
            };
        }

        [Test] // 체력 비율별 페이즈 판정 검증
        public void ResolvePhase_ByHealthRatio_ReturnsExpectedPhase() // 체력 기반 페이즈 테스트
        {
            List<BossPhaseDefinition> phases = CreatePhases(); // 페이즈 정의 생성

            Assert.That(BossPhaseResolver.ResolvePhase(phases, 1f, 0f, 1), Is.EqualTo(1)); // 만피 1페이즈 검증
            Assert.That(BossPhaseResolver.ResolvePhase(phases, 0.61f, 0f, 1), Is.EqualTo(1)); // 60% 직전 1페이즈 검증
            Assert.That(BossPhaseResolver.ResolvePhase(phases, 0.6f, 0f, 1), Is.EqualTo(2)); // 60% 경계 2페이즈 진입 검증
            Assert.That(BossPhaseResolver.ResolvePhase(phases, 0.3f, 0f, 1), Is.EqualTo(2)); // 30% 2페이즈 유지 검증
            Assert.That(BossPhaseResolver.ResolvePhase(phases, 0.25f, 0f, 1), Is.EqualTo(3)); // 25% 경계 3페이즈 진입 검증
            Assert.That(BossPhaseResolver.ResolvePhase(phases, 0.1f, 0f, 1), Is.EqualTo(3)); // 한 번에 크게 깎여도 3페이즈 직행 검증
        }

        [Test] // 한번 올라간 페이즈 유지 검증
        public void ResolvePhase_AfterHealing_DoesNotGoBack() // 페이즈 역행 방지 테스트
        {
            List<BossPhaseDefinition> phases = CreatePhases(); // 페이즈 정의 생성

            Assert.That(BossPhaseResolver.ResolvePhase(phases, 0.9f, 0f, 2), Is.EqualTo(2)); // 회복해도 2페이즈 유지 검증
            Assert.That(BossPhaseResolver.ResolvePhase(phases, 1f, 0f, 3), Is.EqualTo(3)); // 만피 회복해도 3페이즈 유지 검증
        }

        [Test] // 시간 기반 강제 페이즈 전환 검증
        public void ResolvePhase_AfterForceSeconds_EntersPhase() // 시간 강제 전환 테스트
        {
            List<BossPhaseDefinition> phases = CreatePhases(); // 페이즈 정의 생성

            Assert.That(BossPhaseResolver.ResolvePhase(phases, 0.9f, 179f, 1), Is.EqualTo(1)); // 180초 직전 1페이즈 유지 검증
            Assert.That(BossPhaseResolver.ResolvePhase(phases, 0.9f, 180f, 1), Is.EqualTo(3)); // 180초 도달 3페이즈 강제 전환 검증
        }

        [Test] // 페이즈 정의 없음 안전 처리 검증
        public void ResolvePhase_WithoutPhases_ReturnsFirstPhase() // 빈 정의 테스트
        {
            Assert.That(BossPhaseResolver.ResolvePhase(null, 0.1f, 999f, 1), Is.EqualTo(1)); // null 정의 1페이즈 검증
            Assert.That(BossPhaseResolver.ResolvePhase(new List<BossPhaseDefinition>(), 0.1f, 999f, 1), Is.EqualTo(1)); // 빈 정의 1페이즈 검증
            Assert.That(BossPhaseResolver.GetPhase(CreatePhases(), 2).DisplayName, Is.EqualTo("분노")); // 페이즈 번호 조회 검증
        }

        [Test] // 첫 패턴 대기 시간 검증
        public void Scheduler_RespectsInitialDelay() // 초기 대기 테스트
        {
            BossPatternScheduler scheduler = new BossPatternScheduler(CreatePatterns(), 0f, 5f, 4f); // 5초 대기 스케줄러 생성

            Assert.That(scheduler.TryBeginNext(1, 4.9f), Is.False); // 5초 이전 패턴 시작 불가 검증
            Assert.That(scheduler.TryBeginNext(1, 5f), Is.True); // 5초 도달 패턴 시작 검증
            Assert.That(scheduler.ActivePattern.DisplayName, Is.EqualTo("파동")); // 첫 패턴 순서 검증
        }

        [Test] // 페이즈별 패턴 해금 검증
        public void Scheduler_OnlySelectsUnlockedPatterns() // 페이즈 해금 테스트
        {
            BossPatternScheduler scheduler = new BossPatternScheduler(CreatePatterns(), 0f, 0f, 0f); // 대기 없는 스케줄러 생성
            HashSet<string> used = new HashSet<string>(); // 사용 패턴 이름 집합 생성

            for (int step = 0; step < 6; step++) // 1페이즈에서 여러 번 선택
            {
                float now = step * 20f; // 쿨다운이 끝나도록 충분한 간격
                Assert.That(scheduler.TryBeginNext(1, now), Is.True); // 패턴 선택 성공 검증
                used.Add(scheduler.ActivePattern.DisplayName); // 사용 패턴 기록
                scheduler.CompleteActive(now); // 패턴 완료 처리
            }

            Assert.That(used, Is.EquivalentTo(new[] { "파동", "갑주" })); // 1페이즈 해금 패턴만 사용 검증
        }

        [Test] // 쿨다운 중 패턴 제외 검증
        public void Scheduler_SkipsPatternsOnCooldown() // 쿨다운 테스트
        {
            List<BossPatternDefinition> single = new List<BossPatternDefinition> { new BossPatternDefinition("파동", BossPatternKind.AreaStrike, 1f, 0f, 10f, 1, true) }; // 쿨다운 10초 단일 패턴 목록
            BossPatternScheduler scheduler = new BossPatternScheduler(single, 0f, 0f, 0f); // 대기·간격 없는 스케줄러 생성

            Assert.That(scheduler.TryBeginNext(1, 0f), Is.True); // 첫 사용 성공 검증
            scheduler.CompleteActive(0f); // 0초 시점 완료 (10초 쿨다운 시작)

            Assert.That(scheduler.TryBeginNext(1, 9.9f), Is.False); // 쿨다운 중 재사용 불가 검증
            Assert.That(scheduler.TryBeginNext(1, 10f), Is.True); // 쿨다운 종료 후 재사용 검증 (유일 후보는 연속 허용)
        }

        [Test] // 직전 패턴 연속 사용 방지 검증
        public void Scheduler_AvoidsRepeatingLastPattern() // 연속 사용 방지 테스트
        {
            List<BossPatternDefinition> twoPatterns = new List<BossPatternDefinition> // 쿨다운 없는 두 패턴 목록
            {
                new BossPatternDefinition("A", BossPatternKind.AreaStrike, 1f, 0f, 0f, 1, true), // A 패턴
                new BossPatternDefinition("B", BossPatternKind.SelfFortify, 0f, 0f, 0f, 1, true) // B 패턴
            };
            BossPatternScheduler scheduler = new BossPatternScheduler(twoPatterns, 0f, 0f, 0f); // 대기·간격 없는 스케줄러 생성
            string previous = null; // 직전 패턴 이름 초기화

            for (int step = 0; step < 6; step++) // 여러 번 연속 선택
            {
                Assert.That(scheduler.TryBeginNext(1, step), Is.True); // 패턴 선택 성공 검증
                string current = scheduler.ActivePattern.DisplayName; // 현재 패턴 이름 조회
                Assert.That(current, Is.Not.EqualTo(previous)); // 직전과 다른 패턴 검증
                previous = current; // 직전 패턴 갱신
                scheduler.CompleteActive(step); // 패턴 완료 처리
            }
        }

        [Test] // 예고 진행 및 완료 검증
        public void Scheduler_WindupCompletesAfterDuration() // 예고 진행 테스트
        {
            BossPatternScheduler scheduler = new BossPatternScheduler(CreatePatterns(), 0f, 0f, 0f); // 대기 없는 스케줄러 생성
            scheduler.TryBeginNext(1, 0f); // 2초 예고 패턴 시작

            Assert.That(scheduler.IsWindingUp, Is.True); // 예고 진행 상태 검증
            Assert.That(scheduler.TickWindup(1f), Is.False); // 1초 경과 미완료 검증
            Assert.That(scheduler.WindupProgress, Is.EqualTo(0.5f).Within(0.0001f)); // 진행률 50% 검증
            Assert.That(scheduler.TickWindup(1f), Is.True); // 2초 경과 완료 검증
            Assert.That(scheduler.CompleteActive(2f).DisplayName, Is.EqualTo("파동")); // 완료 패턴 반환 검증
            Assert.That(scheduler.IsWindingUp, Is.False); // 예고 상태 해제 검증
        }

        [Test] // 예고 취소 시 쿨다운 소모 검증 (흐트러짐 저지 보상)
        public void Scheduler_CancelActive_AppliesCooldown() // 예고 취소 테스트
        {
            BossPatternScheduler scheduler = new BossPatternScheduler(CreatePatterns(), 0f, 0f, 0f); // 대기 없는 스케줄러 생성
            scheduler.TryBeginNext(1, 0f); // 0번 패턴 예고 시작
            BossPatternDefinition cancelled = scheduler.CancelActive(1f); // 1초 시점 취소

            Assert.That(cancelled.DisplayName, Is.EqualTo("파동")); // 취소 패턴 반환 검증
            Assert.That(scheduler.IsWindingUp, Is.False); // 예고 상태 해제 검증
            Assert.That(scheduler.GetReadyAt(0), Is.EqualTo(11f).Within(0.0001f)); // 취소해도 10초 쿨다운 적용 검증
        }

        [Test] // 패턴 간격 및 지연 검증
        public void Scheduler_IntervalAndPostpone_DelayNextPattern() // 패턴 간격 테스트
        {
            BossPatternScheduler scheduler = new BossPatternScheduler(CreatePatterns(), 0f, 0f, 4f); // 4초 간격 스케줄러 생성
            scheduler.TryBeginNext(1, 0f); // 첫 패턴 시작
            scheduler.CompleteActive(0f); // 0초 완료

            Assert.That(scheduler.TryBeginNext(1, 3.9f), Is.False); // 4초 간격 이전 시작 불가 검증
            scheduler.Postpone(4f, 5f); // 페이즈 전환 등으로 4초 시점부터 5초 지연 (9초까지)

            Assert.That(scheduler.TryBeginNext(1, 8.5f), Is.False); // 지연 기간 중 시작 불가 검증
            Assert.That(scheduler.TryBeginNext(1, 9f), Is.True); // 지연 종료 후 시작 검증
        }

        [Test] // 저지 불가 패턴 정의 검증
        public void PatternDefinition_Uninterruptible_IsPreserved() // 저지 불가 속성 테스트
        {
            List<BossPatternDefinition> patterns = CreatePatterns(); // 패턴 정의 생성

            Assert.That(patterns[0].Interruptible, Is.True); // 일반 패턴 저지 가능 검증
            Assert.That(patterns[3].Interruptible, Is.False); // 필살 패턴 저지 불가 검증
            Assert.That(patterns[3].MinPhase, Is.EqualTo(3)); // 필살 패턴 3페이즈 해금 검증
        }

        [Test] // 흐트러짐 게이지 회복 API 검증
        public void RecoverDisarray_ReducesGaugeButNotWhileDisarrayed() // 흐트러짐 회복 테스트
        {
            BattleDisarrayRuntimeState.ResetAll(); // 흐트러짐 상태 초기화
            BattleDisarrayRuntimeState.Register("BOSS_0", 800); // 최대치 100 보스 등록
            BattleDisarrayRuntimeState.AddDisarray("BOSS_0", 80, 10f); // 80 누적

            Assert.That(BattleDisarrayRuntimeState.RecoverDisarray("BOSS_0", 0.5f, 10f), Is.EqualTo(40)); // 절반 회복 감소량 검증
            Assert.That(BattleDisarrayRuntimeState.GetCurrentGauge("BOSS_0"), Is.EqualTo(40)); // 회복 후 누적 수치 검증

            BattleDisarrayRuntimeState.AddDisarray("BOSS_0", 60, 10f); // 흐트러짐 발생
            Assert.That(BattleDisarrayRuntimeState.RecoverDisarray("BOSS_0", 0.5f, 11f), Is.EqualTo(0)); // 흐트러진 동안 회복 불가 검증
            BattleDisarrayRuntimeState.ResetAll(); // 다음 테스트 영향 방지
        }

        [Test] // 무적·가속 Modifier 검증
        public void InvulnerableAndHasteModifiers_AreQueryable() // 무적·가속 테스트
        {
            BattleSkillRuntimeState.ResetAll(); // 스킬 Runtime 상태 초기화
            Assert.That(BattleSkillRuntimeState.IsInvulnerable("BOSS_0", 10f), Is.False); // 무적 미적용 검증
            BattleSkillRuntimeState.AddModifier("BOSS_0", BattleRuntimeModifierKind.Invulnerable, 1f, 1.5f, "GUARD", 10f); // 1.5초 무적 등록
            BattleSkillRuntimeState.AddModifier("BOSS_0", BattleRuntimeModifierKind.AttackSpeedPercent, 0.35f, float.PositiveInfinity, "SPD", 10f); // 영구 가속 등록

            Assert.That(BattleSkillRuntimeState.IsInvulnerable("BOSS_0", 11f), Is.True); // 무적 지속 검증
            Assert.That(BattleSkillRuntimeState.IsInvulnerable("BOSS_0", 11.6f), Is.False); // 무적 만료 검증
            Assert.That(BattleSkillRuntimeState.GetAttackSpeedMultiplier("BOSS_0", 20f), Is.EqualTo(1.35f).Within(0.0001f)); // 가속 배율 검증
            BattleSkillRuntimeState.ResetAll(); // 다음 테스트 영향 방지
        }
    }
}
