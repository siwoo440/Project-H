using UnityEngine; // Unity 기본 기능

namespace ProjectH.Data // 프로젝트 데이터 영역
{
    [CreateAssetMenu(fileName = "CharacterData", menuName = "Project H/Data/Character")] // 캐릭터 에셋 메뉴
    public sealed class CharacterData : ScriptableObject, IDataRecord // 캐릭터 데이터 정의
    {
        [SerializeField] private string id; // 캐릭터 고유 ID
        [SerializeField] private string displayName; // 캐릭터 표시 이름
        [SerializeField] private CharacterJob job; // 캐릭터 직군
        [SerializeField] private BattlePosition position; // 전투 위치
        [SerializeField] private CharacterRole role; // 전투 역할
        [SerializeField, Min(1)] private int baseHp = 100; // 기본 체력
        [SerializeField, Min(0)] private int baseAttack = 10; // 기본 공격력
        [SerializeField, Min(0)] private int baseMagic = 10; // 기본 마력
        [SerializeField, Min(0)] private int baseDefense = 5; // 기본 방어력
        [SerializeField, Min(0)] private int baseResistance = 5; // 기본 저항력
        [SerializeField, Min(0.01f)] private float attackSpeed = 1f; // 기본 공격속도
        [SerializeField, Range(0f, 1f)] private float criticalRate = 0.05f; // 기본 치명타율

        public string Id => id; // 고유 ID 반환
        public string DisplayName => displayName; // 표시 이름 반환
        public CharacterJob Job => job; // 직군 반환
        public BattlePosition Position => position; // 위치 반환
        public CharacterRole Role => role; // 역할 반환
        public int BaseHp => baseHp; // 체력 반환
        public int BaseAttack => baseAttack; // 공격력 반환
        public int BaseMagic => baseMagic; // 마력 반환
        public int BaseDefense => baseDefense; // 방어력 반환
        public int BaseResistance => baseResistance; // 저항력 반환
        public float AttackSpeed => attackSpeed; // 공격속도 반환
        public float CriticalRate => criticalRate; // 치명타율 반환
    }
}
