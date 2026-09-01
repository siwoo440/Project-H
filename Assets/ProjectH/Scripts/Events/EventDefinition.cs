using System.Collections.Generic; // 목록 자료형
using ProjectH.Data; // 데이터 공통 규약
using UnityEngine; // Unity 기본 기능

namespace ProjectH.Events // 프로젝트 이벤트 영역
{
    [CreateAssetMenu(fileName = "EventDefinition", menuName = "Project H/Event/Event Definition")] // 이벤트 에셋 메뉴
    public sealed class EventDefinition : ScriptableObject, IDataRecord // 이벤트 정의 데이터
    {
        [SerializeField] private string id; // 이벤트 고유 ID
        [SerializeField] private string displayName; // 이벤트 표시 이름
        [SerializeField] private EventConditionGroupMode groupMode = EventConditionGroupMode.All; // 조건 그룹 방식
        [SerializeField] private List<EventCondition> conditions = new List<EventCondition>(); // 이벤트 조건 목록

        public string Id => id; // 이벤트 ID 반환
        public string DisplayName => displayName; // 표시 이름 반환
        public EventConditionGroupMode GroupMode => groupMode; // 조건 그룹 반환
        public IReadOnlyList<EventCondition> Conditions => conditions; // 조건 목록 반환
    }
}
