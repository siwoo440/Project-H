using UnityEngine; // Unity 컴포넌트 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 패시브 Driver 방지
    public sealed class BattlePassiveDriver : MonoBehaviour // 전투 패시브 Runtime 초기화 Driver
    {
        [SerializeField] private BattleCombatRegistry registry; // 전투 객체 Registry

        public void Configure(BattleCombatRegistry combatRegistry) // 패시브 Driver 참조 설정
        {
            registry = combatRegistry; // 전투 Registry 연결
        }

        private void Awake() // 전투 Scene Runtime 시작
        {
            BattlePassiveSystem.Initialize(registry); // 신규 전투 패시브 시스템 초기화
        }

        private void Start() // 전투 시작 Trigger 처리
        {
            BattlePassiveSystem.Handle(BattlePassiveEventContext.CreateBattleStart()); // 전투 시작 패시브 Trigger 발생
        }

        private void OnDestroy() // 전투 Scene Runtime 종료
        {
            BattlePassiveSystem.Shutdown(registry); // 전투 패시브 시스템 종료
        }
    }
}
