using UnityEngine; // Unity 기본 기능

namespace ProjectH.Data // 프로젝트 데이터 영역
{
    [CreateAssetMenu(fileName = "DungeonData", menuName = "Project H/Data/Dungeon")] // 던전 에셋 메뉴
    public sealed class DungeonData : ScriptableObject, IDataRecord // 던전 데이터 정의
    {
        [SerializeField] private string id; // 던전 고유 ID
        [SerializeField] private string displayName; // 던전 표시 이름
        [SerializeField] private string regionId; // 지역 참조 ID
        [SerializeField, Min(1)] private int recommendedLevel = 1; // 권장 레벨
        [SerializeField, Min(0)] private int rewardGold = 100; // 기본 골드 보상
        [SerializeField, Min(0)] private int rewardExp = 100; // 기본 경험치 보상

        public string Id => id; // 고유 ID 반환
        public string DisplayName => displayName; // 표시 이름 반환
        public string RegionId => regionId; // 지역 ID 반환
        public int RecommendedLevel => recommendedLevel; // 권장 레벨 반환
        public int RewardGold => rewardGold; // 골드 보상 반환
        public int RewardExp => rewardExp; // 경험치 보상 반환
    }
}
