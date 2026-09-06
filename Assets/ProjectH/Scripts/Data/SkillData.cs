using UnityEngine; // Unity 기본 기능

namespace ProjectH.Data // 프로젝트 데이터 영역
{
    public enum SkillTargetType // 스킬 대상 종류
    {
        Self = 0, // 자기 자신 대상
        NearestEnemy = 1, // 가장 가까운 적 대상
        LowestHpAlly = 2, // 체력이 가장 낮은 아군 대상
        AllEnemies = 3, // 모든 적 대상
        AllAllies = 4 // 모든 아군 대상
    }

    public enum SkillEffectType // 스킬 효과 종류
    {
        None = 0, // 17일차 효과 미연결 상태
        Damage = 1, // 피해 효과
        Heal = 2, // 회복 효과
        Buff = 3, // 버프 효과
        Debuff = 4, // 디버프 효과
        Shield = 5, // 보호막 효과
        Special = 6 // 특수 효과
    }

    [System.Serializable] // 강화도 데이터 직렬화
    public sealed class SkillEnhancementData // 스킬 강화도별 수치 데이터
    {
        [SerializeField, Min(0f)] private float powerRatio = 1f; // 강화도별 계수
        [SerializeField] private int flatValue; // 강화도별 고정 수치
        [SerializeField, Min(0)] private int ultimateGaugeGain; // 강화도별 궁극기 게이지 획득량
        public float PowerRatio => powerRatio; // 강화도 계수 반환
        public int FlatValue => flatValue; // 강화도 고정 수치 반환
        public int UltimateGaugeGain => ultimateGaugeGain; // 궁극기 게이지 획득량 반환
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
        [SerializeField] private SkillTargetType targetType = SkillTargetType.NearestEnemy; // 스킬 대상 종류
        [SerializeField] private SkillEffectType effectType = SkillEffectType.None; // 스킬 효과 종류
        [SerializeField] private SkillEnhancementData[] enhancements = new SkillEnhancementData[MaxEnhancementLevel]; // 강화도 1~3 데이터
        public string Id => id; // 스킬 ID 반환
        public string OwnerCharacterId => ownerCharacterId; // 소유 캐릭터 ID 반환
        public int SkillSlot => skillSlot; // 스킬 슬롯 번호 반환
        public string DisplayName => displayName; // 스킬 표시 이름 반환
        public string Description => description; // 스킬 설명 반환
        public SkillTargetType TargetType => targetType; // 스킬 대상 종류 반환
        public SkillEffectType EffectType => effectType; // 스킬 효과 종류 반환
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
