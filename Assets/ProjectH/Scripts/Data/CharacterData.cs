using UnityEngine; // Unity 기본 기능

namespace ProjectH.Data // 프로젝트 데이터 영역
{
    [CreateAssetMenu(fileName = "CharacterData", menuName = "Project H/Data/Character")] // 캐릭터 에셋 메뉴
    public sealed class CharacterData : ScriptableObject, IDataRecord // 캐릭터 데이터 정의
    {
        [SerializeField] private string id; // 캐릭터 고유 ID
        [SerializeField] private string displayName; // 캐릭터 표시 이름
        [SerializeField] private CharacterJob job; // 캐릭터 직군
        [SerializeField] private BattlePosition position = BattlePosition.Dealer; // 단순 전투 포지션
        [SerializeField, Min(1)] private int baseHp = 100; // 기본 체력
        [SerializeField, Min(0)] private int baseAttack = 10; // 기본 공격력
        [SerializeField, Min(0)] private int baseDefense = 5; // 기본 방어력
        [SerializeField, Min(0.01f)] private float attackSpeed = 1f; // 기본 공격속도
        [SerializeField, Min(0.2f)] private float attackRange = 1.6f; // 기본 공격 사거리
        [SerializeField, Min(0.01f)] private float moveSpeed = 2f; // 기본 전투 이동속도
        [SerializeField, Range(0f, 1f)] private float accuracy = 0.95f; // 기본 명중률
        [SerializeField, Min(0)] private int baseMagic; // 임시 마력 수치
        [SerializeField, Min(0)] private int baseResistance; // 임시 저항력 수치
        [SerializeField, Range(0f, 1f)] private float criticalRate = 0.05f; // 임시 치명타율
        public string Id => id; // 고유 ID 반환
        public string DisplayName => displayName; // 표시 이름 반환
        public CharacterJob Job => job; // 직군 반환
        public BattlePosition Position => position; // 포지션 반환
        public CharacterRole Role => (CharacterRole)position; // 이전 역할 호환 반환
        public int BaseHp => baseHp; // 체력 반환
        public int BaseAttack => baseAttack; // 공격력 반환
        public int BaseDefense => baseDefense; // 방어력 반환
        public float AttackSpeed => attackSpeed; // 공격속도 반환
        public float AttackRange => attackRange; // 기본 공격 사거리 반환
        public float MoveSpeed => moveSpeed; // 전투 이동속도 반환
        public float Accuracy => accuracy; // 명중률 반환
        public int BaseMagic => baseMagic; // 임시 마력 반환
        public int BaseResistance => baseResistance; // 임시 저항력 반환
        public float CriticalRate => criticalRate; // 임시 치명타율 반환
    }
}
