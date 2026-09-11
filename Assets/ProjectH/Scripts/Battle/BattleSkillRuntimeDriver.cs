using System.Collections.Generic; // 목록 자료형 (Day59 추가)
using ProjectH.SaveSystem; // 결속 시너지 판정 기능 (Day59 추가)
using UnityEngine; // Unity 컴포넌트 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 Runtime Driver 방지
    public sealed class BattleSkillRuntimeDriver : MonoBehaviour // 주기 효과 및 궁극기 게이지 Runtime 갱신기
    {
        private BattleCombatRegistry registry; // 전투 객체 Registry
        private bool affinityGaugeApplied; // 호감도 시작 게이지 적용 완료 여부 (Day56 추가)
        private bool bondSynergyApplied; // 결속 조합 시너지 적용 완료 여부 (Day59 추가)
        private readonly BattleUltimateGaugeTickAccumulator ultimateGaugeAccumulator = new BattleUltimateGaugeTickAccumulator(); // 궁극기 게이지 시간 누적기
        private readonly BattleRegionTraitTicker regionTraits = new BattleRegionTraitTicker(); // 지역 특징 발동기 (Day65 추가 — 전투마다 새로 생성)

        private void Awake() // Runtime Driver 초기화
        {
            registry = GetComponent<BattleCombatRegistry>(); // 동일 객체 전투 Registry 연결
        }

        private void Update() // 전투 Runtime 매 프레임 갱신
        {
            TryApplyDungeonRunModifiers(); // 노드형 탐험 다음 전투 효과 1회 적용 (Day55 추가)
            TryApplyBondSynergy(); // 결속 조합 시너지 1회 적용 (Day59 추가, 시작 게이지보다 먼저 — 결속의 원탁 배율 반영)
            TryApplyAffinityStartGauge(); // 호감도 유대 보상 궁극기 시작 게이지 1회 적용 (Day56 추가)
            BattleSkillRuntimeState.TickPeriodicEffects(Time.time); // 현재 전투 시간 기준 주기 피해 Tick 처리
            BattleRuneEffects.Tick(registry, Time.deltaTime); // 광기·재생·보호막의 룬 주기 처리 (Day60 추가)
            TickUltimateGauge(Time.deltaTime); // 전투 경과 시간 기준 궁극기 게이지 충전 처리
            regionTraits.Tick(registry, BattleRegionTraitCatalog.GetKind(BattleContextRuntimeState.CurrentDungeonId), Time.deltaTime); // 마나 폭주 · 정령의 가호 (Day65 추가)
        }

        private void TryApplyAffinityStartGauge() // 수령한 유대 보상의 궁극기 시작 게이지 적용 (Day56 추가, 게이지 초기화 이후 생성되는 Driver에서 1회)
        {
            if (affinityGaugeApplied || registry == null || registry.CountLiving(BattleTeam.Ally) <= 0) // 적용 완료·아군 생성 확인
            {
                return; // 적용 대기 또는 완료
            }

            affinityGaugeApplied = true; // 1회 적용 기록
            ProjectH.SaveSystem.SaveData saveData = ProjectH.Core.GameManager.Instance == null || ProjectH.Core.GameManager.Instance.Save == null ? null : ProjectH.Core.GameManager.Instance.Save.CurrentSave; // 저장 데이터 조회

            if (saveData == null) // 저장 데이터 확인
            {
                return; // 적용 중단
            }

            for (int index = 0; index < registry.Actors.Count; index++) // 전체 전투 객체 순회
            {
                BattleActor actor = registry.Actors[index]; // 현재 전투 액터 조회
                BattleStats stats = actor == null || actor.Team != BattleTeam.Ally ? null : actor.Stats as BattleStats; // 아군 캐릭터 스탯 조회

                if (stats == null) // 아군 캐릭터 확인
                {
                    continue; // 대상 제외
                }

                int gauge = ProjectH.SaveSystem.AffinityRewardCatalog.GetClaimedBonus(saveData.FindCharacter(stats.CharacterId)).StartUltimateGauge; // 수령 보상 시작 게이지 조회

                if (gauge > 0) // 시작 게이지 존재 확인
                {
                    BattleUltimateGaugeRuntimeState.AddGauge(stats.CharacterId, gauge); // 궁극기 게이지 선충전 (HUD 게이지 자동 갱신)
                }
            }
        }

        private void TryApplyBondSynergy() // 파티 조합 시너지 적용 (Day59 추가 — 균형 파티·결속의 원탁)
        {
            if (bondSynergyApplied || registry == null || registry.CountLiving(BattleTeam.Ally) <= 0) // 적용 완료·아군 생성 확인
            {
                return; // 적용 대기 또는 완료
            }

            bondSynergyApplied = true; // 1회 적용 기록
            List<BattleActor> allies = new List<BattleActor>(); // 아군 목록
            List<string> ids = new List<string>(); // 아군 캐릭터 ID
            List<int> levels = new List<int>(); // 아군 결속 단계

            for (int index = 0; index < registry.Actors.Count; index++) // 전체 전투 객체 순회
            {
                BattleActor actor = registry.Actors[index]; // 현재 전투 액터 조회
                BattleStats stats = actor == null || actor.Team != BattleTeam.Ally ? null : actor.Stats as BattleStats; // 아군 캐릭터 스탯 조회

                if (stats == null) // 아군 캐릭터 확인
                {
                    continue; // 대상 제외
                }

                allies.Add(actor); // 아군 등록
                ids.Add(stats.CharacterId); // ID 등록
                levels.Add(BattleBondRuntimeState.GetLevel(stats.CharacterId)); // 결속 단계 등록
            }

            float skillRune = 0f; // 파티 최대 스킬의 룬 수치 (Day60 추가)

            for (int index = 0; index < allies.Count; index++) // 아군 순회
            {
                skillRune = Mathf.Max(skillRune, BattleRuneRuntimeState.Get(allies[index].Stats.RuntimeId, RuneKind.Skill)); // 가장 높은 값 사용 (파티 공용 블록 생성기)
            }

            if (skillRune > 0f) // 스킬의 룬 확인
            {
                SkillBlock.BattleSkillBlockController blocks = FindFirstObjectByType<SkillBlock.BattleSkillBlockController>(); // 스킬 블록 생성기 조회
                if (blocks != null) blocks.SetIntervalReduction(skillRune); // 생성 주기 단축
            }

            BondSynergyResult synergy = BondCatalog.EvaluateSynergy(ids, levels); // 시너지 판정
            BattleBondRuntimeState.SetRoundTable(synergy.RoundTable); // 결속의 원탁 게이지 보너스 설정

            if (synergy.BalancedParty) // 균형 파티 확인
            {
                for (int index = 0; index < allies.Count; index++) // 아군 순회
                {
                    string runtimeId = allies[index].Stats.RuntimeId; // 대상 Runtime ID
                    BattleSkillRuntimeState.AddModifier(runtimeId, BattleRuntimeModifierKind.AttackPercent, BondCatalog.BalancedPartyBonus, float.PositiveInfinity, "SYNERGY_BALANCED_ATK"); // 공격력 +3%
                    BattleSkillRuntimeState.AddModifier(runtimeId, BattleRuntimeModifierKind.DefensePercent, BondCatalog.BalancedPartyBonus, float.PositiveInfinity, "SYNERGY_BALANCED_DEF"); // 방어력 +3%
                    BattleSkillRuntimeState.AddModifier(runtimeId, BattleRuntimeModifierKind.HealingReceivedPercent, BondCatalog.BalancedPartyBonus, float.PositiveInfinity, "SYNERGY_BALANCED_HEAL"); // 받는 회복량 +3%
                }
            }

            Debug.Log($"[Project H][BOND] 시너지 · 균형 파티={synergy.BalancedParty}, 결속의 원탁={synergy.RoundTable}"); // 시너지 로그
        }

        private void TryApplyDungeonRunModifiers() // 함정·휴식·이벤트가 쌓은 다음 전투 효과를 아군 전원에게 적용 (Day55 추가)
        {
            if (!ProjectH.Dungeon.DungeonRunState.HasPendingBattle || ProjectH.Dungeon.DungeonRunState.PendingModifiers.Count == 0 || registry == null) // 탐험 전투·대기 효과·Registry 확인
            {
                return; // 적용 대상 없음
            }

            if (registry.CountLiving(BattleTeam.Ally) <= 0) // 아군 생성 완료 확인 (Driver는 스킬 상태 초기화 이후 생성되므로 초기화에 지워지지 않음)
            {
                return; // 아군 생성 전 대기
            }

            var modifiers = ProjectH.Dungeon.DungeonRunState.ConsumePendingModifiers(); // 다음 전투 효과 꺼내기 (1회)

            for (int actorIndex = 0; actorIndex < registry.Actors.Count; actorIndex++) // 전체 전투 객체 순회
            {
                BattleActor actor = registry.Actors[actorIndex]; // 현재 전투 액터 조회

                if (actor == null || actor.Team != BattleTeam.Ally || !actor.IsCombatReady || !actor.Stats.IsAlive) // 생존 아군 확인
                {
                    continue; // 적용 대상 제외
                }

                for (int modifierIndex = 0; modifierIndex < modifiers.Count; modifierIndex++) // 다음 전투 효과 순회
                {
                    var modifier = modifiers[modifierIndex]; // 현재 효과 조회
                    string sourceKey = $"RUN:{modifierIndex}:{modifier.Kind}"; // 효과별 출처 키 생성
                    BattleSkillRuntimeState.AddModifier(actor.Stats.RuntimeId, modifier.Kind, modifier.Value, modifier.Duration, sourceKey); // 아군 효과 적용 (Day51 상태이상 칩에 자동 표시)

                    if (modifier.IsDebuff) // 불리 효과 확인
                    {
                        BattleSkillRuntimeState.RegisterRemovableDebuff(actor.Stats.RuntimeId, sourceKey); // 세레나 정화 대상 등록
                    }
                }
            }

            Debug.Log($"[Project H][DUNGEON RUN] 다음 전투 효과 {modifiers.Count}개 적용"); // 효과 적용 로그
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
            BattleRuntimeStates.EndBattle(); // 전투 종료 정적 상태 전체 초기화 (통합 창구)
            registry = null; // 전투 Registry 참조 해제
        }
    }
}
