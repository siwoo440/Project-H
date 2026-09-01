using System.Collections.Generic; // 목록 자료형
using UnityEngine; // Unity 기본 기능

namespace ProjectH.Events // 프로젝트 이벤트 영역
{
    [CreateAssetMenu(fileName = "ProjectHEventCatalog", menuName = "Project H/Event/Event Catalog")] // 이벤트 카탈로그 메뉴
    public sealed class ProjectHEventCatalog : ScriptableObject // 전체 이벤트 카탈로그
    {
        [SerializeField] private List<EventDefinition> events = new List<EventDefinition>(); // 이벤트 정의 목록

        public IReadOnlyList<EventDefinition> Events => events; // 이벤트 목록 반환
    }
}
