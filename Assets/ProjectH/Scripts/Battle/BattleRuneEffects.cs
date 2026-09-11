using ProjectH.SaveSystem; // 룬 종류 기능
using UnityEngine; // Unity 수학·난수·로그 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleRuneEffects // 발동형 룬 효과 (Day60 신규 — 광기·흡혈·가시·보호막·재생·민첩·부활·전투)
    {
        public const float BarrierInterval = 5f; // 보호막 룬 주기 (기획서 5초)
        public const float RegenInterval = 1f; // 재생 룬 주기 (기획서 매초)
        public const float FrenzyCheckInterval = 0.25f; // 광기 체력 확인 주기
        public const float FrenzyThreshold = 0.5f; // 광기 발동 체력 비율 (50% 이하)
        private static bool reflecting; // 가시 반사 중 (반사 피해가 다시 반사되는 것 방지)

        public static void Tick(BattleCombatRegistry registry, float deltaTime) // 주기형 룬 처리 (BattleSkillRuntimeDriver.Update에서 호출)
        {
            if (registry == null || deltaTime <= 0f) // 입력 확인
            {
                return; // 처리 없음
            }

            BattleRuneRuntimeState.FrenzyTimer += deltaTime; // 광기 누적
            BattleRuneRuntimeState.RegenTimer += deltaTime; // 재생 누적
            BattleRuneRuntimeState.BarrierTimer += deltaTime; // 보호막 누적
            bool frenzy = BattleRuneRuntimeState.FrenzyTimer >= FrenzyCheckInterval; // 광기 확인 시점
            bool regen = BattleRuneRuntimeState.RegenTimer >= RegenInterval; // 재생 시점
            bool barrier = BattleRuneRuntimeState.BarrierTimer >= BarrierInterval; // 보호막 시점
            if (frenzy) BattleRuneRuntimeState.FrenzyTimer -= FrenzyCheckInterval; // 광기 누적 차감
            if (regen) BattleRuneRuntimeState.RegenTimer -= RegenInterval; // 재생 누적 차감
            if (barrier) BattleRuneRuntimeState.BarrierTimer -= BarrierInterval; // 보호막 누적 차감

            if (!frenzy && !regen && !barrier) // 처리 시점 확인
            {
                return; // 이번 프레임 처리 없음
            }

            for (int index = 0; index < registry.Actors.Count; index++) // 전투 액터 순회
            {
                BattleActor actor = registry.Actors[index]; // 액터 조회

                if (actor == null || actor.Team != BattleTeam.Ally || !actor.IsCombatReady || !actor.Stats.IsAlive) // 생존 아군 확인
                {
                    continue; // 대상 제외
                }

                string id = actor.Stats.RuntimeId; // Runtime ID

                if (frenzy && BattleRuneRuntimeState.Get(id, RuneKind.Frenzy) > 0f && actor.Stats.CurrentHp <= actor.Stats.MaxHp * FrenzyThreshold) // 광기 : 체력 50% 이하
                {
                    BattleSkillRuntimeState.AddModifier(id, BattleRuntimeModifierKind.AttackPercent, BattleRuneRuntimeState.Get(id, RuneKind.Frenzy), FrenzyCheckInterval * 2f, "RUNE_FRENZY"); // 짧게 갱신 (체력이 회복되면 자연히 사라짐)
                }

                if (regen && BattleRuneRuntimeState.Get(id, RuneKind.Regen) > 0f) // 재생 : 매초 최대 체력 비율 회복
                {
                    actor.ApplyHealing(BattleHealingResolver.Resolve(actor.Stats, Mathf.Max(1, Mathf.RoundToInt(actor.Stats.MaxHp * BattleRuneRuntimeState.Get(id, RuneKind.Regen))))); // 회복 적용
                }

                if (barrier && BattleRuneRuntimeState.Get(id, RuneKind.Barrier) > 0f) // 보호막 : 5초마다 최대 체력 비율 보호막 (겹치지 않고 큰 값 유지)
                {
                    BattlePassiveRuntimeState.GrantShield(id, Mathf.Max(1, Mathf.RoundToInt(actor.Stats.MaxHp * BattleRuneRuntimeState.Get(id, RuneKind.Barrier)))); // 보호막 부여
                }
            }
        }

        public static bool TryEvade(BattleActor target, BattleDamageResult result) // 민첩 : 공격자가 있는 피해 회피 (BattleActor.ApplyDamage 시작 시)
        {
            float chance = target == null || string.IsNullOrWhiteSpace(result.AttackerRuntimeId) ? 0f : BattleRuneRuntimeState.Get(target.Stats.RuntimeId, RuneKind.Agility); // 회피 확률 (지속 피해 제외)

            if (chance <= 0f || Random.value > chance) // 회피 판정
            {
                return false; // 명중
            }

            Debug.Log($"[Project H][RUNE] 민첩 회피 -> {target.Stats.RuntimeId}"); // 회피 로그
            return true; // 회피
        }

        public static int ScaleDisarray(string attackerRuntimeId, int amount) // 전투 : 흐트러짐 누적 증가 (공격자 룬 기준)
        {
            float bonus = BattleRuneRuntimeState.Get(attackerRuntimeId, RuneKind.Battle); // 증가율
            return bonus <= 0f ? amount : Mathf.RoundToInt(amount * (1f + bonus)); // 누적량 반환 (룬 없으면 그대로)
        }

        public static void OnDamageApplied(BattleActor target, BattleDamageResult result, int applied) // 피해 적용 직후 : 흡혈(공격자)·가시(피해자)·부활(피해자)
        {
            if (target == null || applied <= 0) // 입력 확인
            {
                return; // 처리 없음
            }

            BattleCombatRegistry registry = BattleSkillRuntimeState.CurrentRegistry; // 현재 전투 Registry
            BattleActor attacker = registry == null || string.IsNullOrWhiteSpace(result.AttackerRuntimeId) ? null : registry.FindByRuntimeId(result.AttackerRuntimeId); // 공격자 조회

            if (attacker != null && attacker.IsCombatReady && attacker.Stats.IsAlive) // 공격자 생존 확인
            {
                float vampire = BattleRuneRuntimeState.Get(attacker.Stats.RuntimeId, RuneKind.Vampire); // 흡혈 비율
                if (vampire > 0f) attacker.ApplyHealing(BattleHealingResolver.Resolve(attacker.Stats, Mathf.Max(1, Mathf.RoundToInt(applied * vampire)))); // 준 피해 비율 회복

                float thorns = BattleRuneRuntimeState.Get(target.Stats.RuntimeId, RuneKind.Thorns); // 가시 비율

                if (thorns > 0f && !reflecting && target.Team != attacker.Team) // 반사 조건 (중첩 반사 방지)
                {
                    reflecting = true; // 반사 시작

                    try // 반사 중 예외와 무관하게 플래그 해제 보장
                    {
                        int power = Mathf.Max(1, Mathf.RoundToInt(target.Stats.Attack * thorns)); // 공격력 비율 반사 위력
                        attacker.ApplyDamage(BattleDamageResolver.Resolve(new BattleDamageRequest(target.Stats, attacker.Stats, BattleDamageType.Physical, power))); // 반사 피해
                    }
                    finally // 마무리
                    {
                        reflecting = false; // 반사 종료
                    }
                }
            }

            TryRevive(target); // 쓰러졌으면 부활 룬 확인
        }

        private static void TryRevive(BattleActor target) // 부활 : 전투 중 쓰러지면 1회 체력 비율로 부활
        {
            if (target.Stats.IsAlive || target.Team != BattleTeam.Ally) // 쓰러진 아군 확인
            {
                return; // 처리 없음
            }

            float ratio = BattleRuneRuntimeState.Get(target.Stats.RuntimeId, RuneKind.Revive); // 부활 체력 비율
            BattleDeathHandler handler = target.GetComponent<BattleDeathHandler>(); // 사망 처리기 (Day15)

            if (ratio <= 0f || handler == null || !handler.CanRevive || !BattleRuneRuntimeState.TryConsumeRevive(target.Stats.RuntimeId)) // 부활 가능·1회 확인
            {
                return; // 부활 없음
            }

            bool revived = handler.TryRevive(Mathf.Max(1, Mathf.RoundToInt(target.Stats.MaxHp * ratio))); // 세레나 궁극기와 같은 부활 경로
            Debug.Log($"[Project H][RUNE] 부활의 룬 -> {target.Stats.RuntimeId}, Revived={revived}"); // 부활 로그
        }
    }
}
