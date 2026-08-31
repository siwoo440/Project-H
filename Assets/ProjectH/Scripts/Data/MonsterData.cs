using UnityEngine; // Unity 기본 기능

namespace ProjectH.Data // 프로젝트 데이터 영역
{
    [CreateAssetMenu(fileName = "MonsterData", menuName = "Project H/Data/Monster")] // 몬스터 에셋 메뉴
    public sealed class MonsterData : ScriptableObject, IDataRecord // 몬스터 데이터 정의
    {
        [SerializeField] private string id; // 몬스터 고유 ID
        [SerializeField] private string displayName; // 몬스터 표시 이름
        [SerializeField, Min(1)] private int maxHp = 100; // 최대 체력
        [SerializeField, Min(0)] private int attack = 10; // 공격력
        [SerializeField, Min(0)] private int defense = 5; // 방어력
        [SerializeField, Min(0)] private int resistance = 5; // 저항력
        [SerializeField, Min(0.01f)] private float attackSpeed = 1f; // 공격속도
        [SerializeField, Min(0f)] private float attackRange = 1.5f; // 공격 사거리
        [SerializeField, Min(0f)] private float moveSpeed = 2f; // 이동속도

        public string Id => id; // 고유 ID 반환
        public string DisplayName => displayName; // 표시 이름 반환
        public int MaxHp => maxHp; // 최대 체력 반환
        public int Attack => attack; // 공격력 반환
        public int Defense => defense; // 방어력 반환
        public int Resistance => resistance; // 저항력 반환
        public float AttackSpeed => attackSpeed; // 공격속도 반환
        public float AttackRange => attackRange; // 공격 사거리 반환
        public float MoveSpeed => moveSpeed; // 이동속도 반환
    }
}
