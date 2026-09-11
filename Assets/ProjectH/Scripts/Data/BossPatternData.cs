using System.Collections.Generic; // 읽기 전용 목록 기능
using UnityEngine; // Unity 기본 기능

namespace ProjectH.Data // 프로젝트 데이터 영역
{
    public enum BossPatternKind // 보스 특수 패턴 종류 (Day54 신규)
    {
        AreaStrike = 0, // 아군 전체 공격
        FocusStrike = 1, // 단일 대상 강타 및 기절
        SelfFortify = 2, // 보스 자신 방어력 강화
        DisarrayRecover = 3 // 보스 흐트러짐 게이지 회복
    }

    [System.Serializable] // Inspector 직렬화 허용
    public sealed class BossPhaseDefinition // 보스 단일 페이즈 정의 (Day54 신규)
    {
        [SerializeField] private string displayName; // 페이즈 표시 이름
        [SerializeField, Range(0f, 1f)] private float enterHealthRatio = 1f; // 페이즈 진입 체력 비율 (이 비율 이하가 되면 진입)
        [SerializeField, Min(0f)] private float forceAfterSeconds; // 시간 경과 강제 진입 초 (0이면 미사용)
        [SerializeField, Min(0f)] private float attackBonus; // 페이즈 공격력 증가 비율
        [SerializeField, Min(0f)] private float attackSpeedBonus; // 페이즈 공격 속도 증가 비율
        public string DisplayName => displayName; // 페이즈 표시 이름 반환
        public float EnterHealthRatio => enterHealthRatio; // 진입 체력 비율 반환
        public float ForceAfterSeconds => forceAfterSeconds; // 시간 경과 강제 진입 초 반환
        public float AttackBonus => attackBonus; // 공격력 증가 비율 반환
        public float AttackSpeedBonus => attackSpeedBonus; // 공격 속도 증가 비율 반환

        public BossPhaseDefinition(string phaseName, float enterRatio, float forceSeconds, float attackIncrease, float attackSpeedIncrease) // 보스 페이즈 정의 생성 (테스트 및 코드 생성용)
        {
            displayName = phaseName ?? string.Empty; // 표시 이름 저장
            enterHealthRatio = Mathf.Clamp01(enterRatio); // 진입 체력 비율 범위 보정
            forceAfterSeconds = Mathf.Max(0f, forceSeconds); // 강제 진입 초 음수 방지
            attackBonus = Mathf.Max(0f, attackIncrease); // 공격력 증가 음수 방지
            attackSpeedBonus = Mathf.Max(0f, attackSpeedIncrease); // 공격 속도 증가 음수 방지
        }
    }

    [System.Serializable] // Inspector 직렬화 허용
    public sealed class BossPatternDefinition // 보스 단일 특수 패턴 정의 (Day54 신규)
    {
        [SerializeField] private string displayName; // 패턴 표시 이름
        [SerializeField] private BossPatternKind kind; // 패턴 종류
        [SerializeField] private SkillDamageType damageType = SkillDamageType.Physical; // 공격 패턴 피해 종류
        [SerializeField, Min(0f)] private float powerRatio = 1f; // 공격 패턴 공격력 계수
        [SerializeField, Min(0f)] private float effectValue; // 부가 효과 수치 (방어 증가율·게이지 회복률 등)
        [SerializeField, Min(0f)] private float effectDuration; // 부가 효과 지속시간 (기절·강화 등)
        [SerializeField, Min(0f)] private float windupSeconds; // 예고 시간 (0이면 즉시 발동)
        [SerializeField, Min(0f)] private float cooldownSeconds = 10f; // 재사용 대기 시간
        [SerializeField, Min(1)] private int minPhase = 1; // 해금 페이즈 (1부터 시작)
        [SerializeField] private bool interruptible = true; // 흐트러짐으로 취소 가능 여부
        public string DisplayName => displayName; // 패턴 표시 이름 반환
        public BossPatternKind Kind => kind; // 패턴 종류 반환
        public SkillDamageType DamageType => damageType; // 피해 종류 반환
        public float PowerRatio => powerRatio; // 공격력 계수 반환
        public float EffectValue => effectValue; // 부가 효과 수치 반환
        public float EffectDuration => effectDuration; // 부가 효과 지속시간 반환
        public float WindupSeconds => windupSeconds; // 예고 시간 반환
        public float CooldownSeconds => cooldownSeconds; // 재사용 대기 시간 반환
        public int MinPhase => minPhase; // 해금 페이즈 반환
        public bool Interruptible => interruptible; // 흐트러짐 취소 가능 여부 반환

        public BossPatternDefinition(string patternName, BossPatternKind patternKind, float power, float windup, float cooldown, int unlockPhase, bool canInterrupt) // 보스 패턴 정의 생성 (테스트 및 코드 생성용)
            : this(patternName, patternKind, SkillDamageType.Physical, power, 0f, 0f, windup, cooldown, unlockPhase, canInterrupt) // 부가 효과 없는 기본 생성 규칙 위임
        {
        }

        public BossPatternDefinition(string patternName, BossPatternKind patternKind, SkillDamageType patternDamageType, float power, float value, float duration, float windup, float cooldown, int unlockPhase, bool canInterrupt) // 보스 패턴 정의 전체 생성
        {
            displayName = patternName ?? string.Empty; // 표시 이름 저장
            kind = patternKind; // 패턴 종류 저장
            damageType = patternDamageType; // 피해 종류 저장
            powerRatio = Mathf.Max(0f, power); // 공격력 계수 음수 방지
            effectValue = Mathf.Max(0f, value); // 부가 효과 수치 음수 방지
            effectDuration = Mathf.Max(0f, duration); // 부가 효과 지속시간 음수 방지
            windupSeconds = Mathf.Max(0f, windup); // 예고 시간 음수 방지
            cooldownSeconds = Mathf.Max(0f, cooldown); // 재사용 대기 음수 방지
            minPhase = Mathf.Max(1, unlockPhase); // 해금 페이즈 최소 1 보정
            interruptible = canInterrupt; // 취소 가능 여부 저장
        }
    }

    [CreateAssetMenu(fileName = "BossPatternData", menuName = "Project H/Data/Boss Pattern")] // 보스 패턴 에셋 메뉴
    public sealed class BossPatternData : ScriptableObject // 보스 페이즈 및 특수 패턴 데이터 (Day54 신규)
    {
        [SerializeField] private BossPhaseDefinition[] phases = new BossPhaseDefinition[0]; // 페이즈 목록 (첫 항목이 1페이즈)
        [SerializeField] private BossPatternDefinition[] patterns = new BossPatternDefinition[0]; // 특수 패턴 목록
        [SerializeField, Min(0f)] private float initialDelaySeconds = 5f; // 전투 시작 후 첫 패턴까지 대기 시간
        [SerializeField, Min(0f)] private float patternIntervalSeconds = 4f; // 패턴 사이 최소 간격
        [SerializeField, Min(0f)] private float phaseTransitionInvulnerableSeconds = 1.5f; // 페이즈 전환 무적 시간
        public IReadOnlyList<BossPhaseDefinition> Phases => phases; // 페이즈 목록 반환
        public IReadOnlyList<BossPatternDefinition> Patterns => patterns; // 특수 패턴 목록 반환
        public float InitialDelaySeconds => initialDelaySeconds; // 첫 패턴 대기 시간 반환
        public float PatternIntervalSeconds => patternIntervalSeconds; // 패턴 사이 최소 간격 반환
        public float PhaseTransitionInvulnerableSeconds => phaseTransitionInvulnerableSeconds; // 페이즈 전환 무적 시간 반환
    }
}
