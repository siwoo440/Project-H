using UnityEngine; // Unity 컴포넌트 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 Runtime Driver 방지
    public sealed class BattleSkillRuntimeDriver : MonoBehaviour // 주기 효과 및 궁극기 게이지 Runtime 갱신기
    {
        private BattleCombatRegistry registry; // 전투 객체 Registry
        private readonly BattleUltimateGaugeTickAccumulator ultimateGaugeAccumulator = new BattleUltimateGaugeTickAccumulator(); // 궁극기 게이지 시간 누적기

        private void Awake() // Runtime Driver 초기화
        {
            registry = GetComponent<BattleCombatRegistry>(); // 동일 객체 전투 Registry 연결
        }

        private void Update() // 전투 Runtime 매 프레임 갱신
        {
            BattleSkillRuntimeState.TickPeriodicEffects(Time.time); // 현재 전투 시간 기준 주기 피해 Tick 처리
            TickUltimateGauge(Time.deltaTime); // 전투 경과 시간 기준 궁극기 게이지 충전 처리
        }

        public void TickUltimateGauge(float deltaTime) // 생존 아군 초 단위 궁극기 게이지 충전
        {
            if (registry == null) // 전투 Registry 연결 상태 확인
            {
                registry = GetComponent<BattleCombatRegistry>(); // 동일 객체 전투 Registry 재탐색
            }

            if (registry == null) // 전투 Registry 존재 확인
            {
                return; // 시간 충전 처리 중단
            }

            int tickCount = ultimateGaugeAccumulator.Advance(deltaTime); // 경과 시간 기준 완료 Tick 수 계산

            if (tickCount <= 0) // 완료 Tick 존재 확인
            {
                return; // 충전 대상 처리 중단
            }

            ChargeLivingAllies(tickCount); // 완료된 초 수만큼 생존 아군 게이지 충전
        }

        private void ChargeLivingAllies(int amount) // 생존 아군 전체 궁극기 게이지 충전
        {
            for (int index = 0; index < registry.Actors.Count; index++) // 전체 전투 객체 순회
            {
                BattleActor actor = registry.Actors[index]; // 현재 전투 액터 조회

                if (actor == null || actor.Team != BattleTeam.Ally || !actor.IsCombatReady || !actor.Stats.IsAlive) // 생존 아군 여부 확인
                {
                    continue; // 시간 충전 대상 제외
                }

                BattleStats stats = actor.Stats as BattleStats; // 아군 캐릭터 전투 스탯 변환

                if (stats == null || string.IsNullOrWhiteSpace(stats.CharacterId)) // 캐릭터 스탯과 ID 확인
                {
                    continue; // 잘못된 충전 대상 제외
                }

                BattleUltimateGaugeRuntimeState.AddGauge(stats.CharacterId, amount); // 전투 시간 기준 캐릭터 게이지 충전
            }
        }

        private void OnDestroy() // 전투 Runtime Driver 제거 처리
        {
            ultimateGaugeAccumulator.Reset(); // 궁극기 게이지 시간 누적값 초기화
            BattleUltimateGaugeRuntimeState.ResetAll(); // 전투 종료 시 궁극기 게이지 Runtime 상태 초기화
            BattleSkillRuntimeState.ResetAll(); // 전투 종료 시 스킬 Runtime 상태 초기화
            BattleDisarrayRuntimeState.ResetAll(); // 전투 종료 시 흐트러짐 상태 초기화 (Day53 추가)
            BattleElementRuntimeState.ResetAll(); // 전투 종료 시 속성 등록 초기화 (Day52 추가 — 전투 시작 시점에 초기화하면 스탯 생성 순서와 경합해 등록이 지워질 수 있음)
            BattleSkillRuntimeState.SetRegistry(null); // 전투 종료 시 Registry 연결 해제
            registry = null; // 전투 Registry 참조 해제
        }
    }
}
