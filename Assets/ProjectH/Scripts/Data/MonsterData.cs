using UnityEngine; // Unity 기본 기능

namespace ProjectH.Data // 프로젝트 데이터 영역
{
    public enum EnemyAIType // 적군 AI 행동 유형
    {
        Normal = 0, // 일반 전선형
        Rush = 1, // 후열 지향 돌격형
        Ranged = 2, // 원거리 전선형
        Defensive = 3, // 방어형 확장 자리
        Magic = 4, // 마법형 확장 자리
        Assassin = 5, // 암살형 확장 자리
        Elite = 6, // 정예형 확장 자리
        Boss = 7 // 보스형 확장 자리
    }

    [CreateAssetMenu(fileName = "MonsterData", menuName = "Project H/Data/Monster")] // 몬스터 에셋 메뉴
    public sealed class MonsterData : ScriptableObject, IDataRecord // 몬스터 데이터 정의
    {
        [SerializeField] private string id; // 몬스터 고유 ID
        [SerializeField] private string displayName; // 몬스터 표시 이름
        [SerializeField] private EnemyAIType aiType = EnemyAIType.Normal; // 몬스터 AI 유형
        [SerializeField, Min(1)] private int maxHp = 100; // 최대 체력
        [SerializeField, Min(0)] private int attack = 10; // 공격력
        [SerializeField, Min(0)] private int defense = 5; // 방어력
        [SerializeField, Min(0)] private int resistance = 5; // 저항력
        [SerializeField, Min(0.01f)] private float attackSpeed = 1f; // 공격속도
        [SerializeField, Min(0f)] private float attackRange = 1.5f; // 공격 사거리
        [SerializeField, Min(0f)] private float moveSpeed = 2f; // 이동속도
        public string Id => id; // 고유 ID 반환
        public string DisplayName => displayName; // 표시 이름 반환
        public EnemyAIType AIType => aiType; // 적군 AI 유형 반환
        public int MaxHp => maxHp; // 최대 체력 반환
        public int Attack => attack; // 공격력 반환
        public int Defense => defense; // 방어력 반환
        public int Resistance => resistance; // 저항력 반환
        public float AttackSpeed => attackSpeed; // 공격속도 반환
        public float AttackRange => attackRange; // 공격 사거리 반환
        public float MoveSpeed => moveSpeed; // 이동속도 반환
    }
}
