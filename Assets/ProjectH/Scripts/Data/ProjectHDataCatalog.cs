using System.Collections.Generic; // 목록 자료형
using UnityEngine; // Unity 기본 기능

namespace ProjectH.Data // 프로젝트 데이터 영역
{
    [CreateAssetMenu(fileName = "ProjectHDataCatalog", menuName = "Project H/Data/Catalog")] // 카탈로그 에셋 메뉴
    public sealed class ProjectHDataCatalog : ScriptableObject // 전체 데이터 카탈로그
    {
        [SerializeField] private List<CharacterData> characters = new List<CharacterData>(); // 캐릭터 목록
        [SerializeField] private List<MonsterData> monsters = new List<MonsterData>(); // 몬스터 목록
        [SerializeField] private List<DungeonData> dungeons = new List<DungeonData>(); // 던전 목록
        [SerializeField] private List<ItemData> items = new List<ItemData>(); // 아이템 목록

        public IReadOnlyList<CharacterData> Characters => characters; // 캐릭터 목록 반환
        public IReadOnlyList<MonsterData> Monsters => monsters; // 몬스터 목록 반환
        public IReadOnlyList<DungeonData> Dungeons => dungeons; // 던전 목록 반환
        public IReadOnlyList<ItemData> Items => items; // 아이템 목록 반환
    }
}
