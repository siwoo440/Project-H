using UnityEngine; // Unity 컴포넌트 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 Runtime Driver 방지
    public sealed class BattleSkillRuntimeDriver : MonoBehaviour // 주기 피해 및 시간 기반 스킬 Runtime 갱신기
    {
        private void Update() // 스킬 Runtime 매 프레임 갱신
        {
            BattleSkillRuntimeState.TickPeriodicEffects(Time.time); // 현재 전투 시간 기준 주기 피해 Tick 처리
        }

        private void OnDestroy() // 전투 Runtime Driver 제거 처리
        {
            BattleSkillRuntimeState.ResetAll(); // 전투 종료 시 스킬 Runtime 상태 초기화
            BattleSkillRuntimeState.SetRegistry(null); // 전투 종료 시 Registry 연결 해제
        }
    }
}
