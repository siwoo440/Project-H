using UnityEngine; // Unity 기본 기능

namespace ProjectH.Data // 프로젝트 데이터 영역
{
    public enum SkillTargetType // 스킬 대상 종류
    {
        Self = 0, // 자기 자신 대상
        NearestEnemy = 1, // 가장 가까운 적 대상
        LowestHpAlly = 2, // 체력이 가장 낮은 아군 대상
        AllEnemies = 3, // 모든 적 대상
        AllAllies = 4, // 모든 아군 대상
        LineEnemies = 5, // 전방 직선 적 전체 대상
        NearbyEnemiesFromPrimary = 6 // 주 대상 위치 주변 적 대상
    }

    public enum SkillEffectType // 스킬 대표 효과 종류
    {
        None = 0, // 효과 미연결 상태
        Damage = 1, // 피해 효과
        Heal = 2, // 회복 효과
        Buff = 3, // 버프 효과
        Debuff = 4, // 디버프 효과
        Shield = 5, // 보호막 효과
        Special = 6 // 특수 효과
    }

    public enum SkillDamageType // 데이터 계층 스킬 피해 종류
    {
        Physical = 0, // 물리 피해
        Magic = 1, // 마법 피해
        True = 2 // 방어 무시 피해
    }

    public enum SkillEffectKind // 실제 데이터 기반 스킬 효과 종류
    {
        None = 0, // 효과 없음
        HealMaxHpPercent = 1, // 최대 체력 비율 회복
        CleanseDebuff = 2, // 약화 효과 제거
        DefensePercent = 3, // 방어력 비율 증가
        DamageReductionPercent = 4, // 최종 피해 감소
        HealingReceivedPercent = 5, // 받는 회복량 증가
        Taunt = 6, // 적군 강제 타겟 지정
        CounterChance = 7, // 피격 시 반격 확률
        DamageAttackRatio = 8, // 공격력 계수 직접 피해
        PeriodicDamageAttackRatio = 9, // 공격력 계수 주기 피해
        ResistanceReductionPercent = 10, // 마법 저항 비율 감소
        AccuracyReductionPercent = 11, // 명중률 비율 감소
        Stun = 12 // 행동 불가 기절
    }

    [System.Serializable] // 스킬 효과 정의 직렬화
    public sealed class SkillEffectDefinition // Inspector에서 수정 가능한 단일 스킬 효과 데이터
    {
        [SerializeField] private SkillEffectKind kind; // 실제 효과 종류
        [SerializeField] private SkillTargetType targetType = SkillTargetType.Self; // 효과 대상 종류
        [SerializeField] private SkillDamageType damageType = SkillDamageType.Physical; // 피해 계열 효과의 피해 종류
        [SerializeField] private ElementType element = ElementType.None; // 효과 공격 속성 (Day52 추가, None이면 시전자 속성 상속)
        [SerializeField] private float value; // 효과 비율 또는 확률
        [SerializeField, Min(0f)] private float duration; // 효과 지속시간
        [SerializeField, Min(0)] private int count; // Hit 수·Tick 수·정화 수
        [SerializeField, Range(0f, 1f)] private float chance = 1f; // 확률 효과 발동 확률
        [SerializeField, Min(0f)] private float interval = 1f; // 주기 피해 Tick 간격
        [SerializeField, Min(0f)] private float radius; // 주변 대상 판정 반경
        [SerializeField] private bool excludePrimary; // 주변 대상에서 주 대상 제외 여부
        [SerializeField] private bool requirePreviousSuccess; // 앞선 효과 성공 요구 여부
        public SkillEffectKind Kind => kind; // 효과 종류 반환
        public SkillTargetType TargetType => targetType; // 효과 대상 종류 반환
        public SkillDamageType DamageType => damageType; // 피해 종류 반환
        public ElementType Element => element; // 효과 공격 속성 반환 (Day52 추가)
        public float Value => value; // 효과 수치 반환
        public float Duration => duration; // 효과 지속시간 반환
        public int Count => count; // 정수 효과값 반환
        public float Chance => chance; // 효과 발동 확률 반환
        public float Interval => interval; // 주기 피해 간격 반환
        public float Radius => radius; // 주변 대상 판정 반경 반환
        public bool ExcludePrimary => excludePrimary; // 주 대상 제외 여부 반환
        public bool RequirePreviousSuccess => requirePreviousSuccess; // 앞선 효과 성공 요구 여부 반환

        public SkillEffectDefinition(SkillEffectKind effectKind, SkillTargetType effectTargetType, float effectValue, float effectDuration, int effectCount, bool needsPreviousSuccess) // 18일차 호환 스킬 효과 데이터 생성
            : this(effectKind, effectTargetType, SkillDamageType.Physical, effectValue, effectDuration, effectCount, 1f, 1f, 0f, false, needsPreviousSuccess) // 기존 생성 규칙을 확장 생성자로 전달
        {
        }

        public SkillEffectDefinition(SkillEffectKind effectKind, SkillTargetType effectTargetType, SkillDamageType effectDamageType, float effectValue, float effectDuration, int effectCount, float effectChance, float effectInterval, float effectRadius, bool shouldExcludePrimary, bool needsPreviousSuccess) // 19일차 확장 스킬 효과 데이터 생성
        {
            kind = effectKind; // 효과 종류 저장
            targetType = effectTargetType; // 효과 대상 저장
            damageType = effectDamageType; // 피해 종류 저장
            value = effectValue; // 효과 수치 저장
            duration = Mathf.Max(0f, effectDuration); // 지속시간 음수 방지
            count = Mathf.Max(0, effectCount); // 정수 효과값 음수 방지
            chance = Mathf.Clamp01(effectChance); // 발동 확률 범위 보정
            interval = Mathf.Max(0f, effectInterval); // Tick 간격 음수 방지
            radius = Mathf.Max(0f, effectRadius); // 주변 반경 음수 방지
            excludePrimary = shouldExcludePrimary; // 주 대상 제외 여부 저장
            requirePreviousSuccess = needsPreviousSuccess; // 앞선 효과 성공 요구 저장
        }
    }

    [System.Serializable] // 강화도 데이터 직렬화
    public sealed class SkillEnhancementData // 스킬 강화도별 수치 데이터
    {
        [SerializeField, Min(0f)] private float powerRatio = 1f; // 강화도별 범용 계수
        [SerializeField] private int flatValue; // 강화도별 범용 고정 수치
        [SerializeField, Min(0)] private int ultimateGaugeGain; // 강화도별 궁극기 게이지 획득량
        [SerializeField] private SkillEffectDefinition[] effects = new SkillEffectDefinition[0]; // 강화도별 실제 효과 목록
        public float PowerRatio => powerRatio; // 강화도 계수 반환
        public int FlatValue => flatValue; // 강화도 고정 수치 반환
        public int UltimateGaugeGain => ultimateGaugeGain; // 궁극기 게이지 획득량 반환
        public SkillEffectDefinition[] Effects => effects; // 강화도별 실제 효과 목록 반환
    }

    [CreateAssetMenu(fileName = "SkillData", menuName = "Project H/Data/Skill")] // 스킬 에셋 메뉴
    public sealed class SkillData : ScriptableObject // 캐릭터 개별 스킬 데이터
    {
        public const int MaxEnhancementLevel = 3; // 최대 스킬 강화도
        [SerializeField] private string id; // 스킬 고유 ID
        [SerializeField] private string ownerCharacterId; // 스킬 소유 캐릭터 ID
        [SerializeField, Range(1, 3)] private int skillSlot = 1; // 캐릭터 스킬 슬롯 번호
        [SerializeField] private string displayName; // 스킬 표시 이름
        [SerializeField, TextArea(2, 5)] private string description; // 스킬 설명
        [SerializeField] private SkillTargetType targetType = SkillTargetType.NearestEnemy; // 대표 스킬 대상 종류
        [SerializeField] private SkillEffectType effectType = SkillEffectType.None; // 대표 스킬 효과 종류
        [SerializeField] private SkillEnhancementData[] enhancements = new SkillEnhancementData[MaxEnhancementLevel]; // 강화도 1~3 데이터
        public string Id => id; // 스킬 ID 반환
        public string OwnerCharacterId => ownerCharacterId; // 소유 캐릭터 ID 반환
        public int SkillSlot => skillSlot; // 스킬 슬롯 번호 반환
        public string DisplayName => displayName; // 스킬 표시 이름 반환
        public string Description => description; // 스킬 설명 반환
        public SkillTargetType TargetType => targetType; // 대표 스킬 대상 종류 반환
        public SkillEffectType EffectType => effectType; // 대표 스킬 효과 종류 반환
        public int EnhancementCount => enhancements == null ? 0 : enhancements.Length; // 강화도 데이터 개수 반환

        public SkillEnhancementData GetEnhancement(int enhancementLevel) // 지정 강화도 데이터 조회
        {
            if (enhancements == null || enhancements.Length == 0) // 강화도 데이터 존재 확인
            {
                return null; // 강화도 데이터 없음 반환
            }

            int safeLevel = Mathf.Clamp(enhancementLevel, 1, Mathf.Min(MaxEnhancementLevel, enhancements.Length)); // 강화도 범위 보정
            return enhancements[safeLevel - 1]; // 보정된 강화도 데이터 반환
        }
    }
}
