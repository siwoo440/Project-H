using System.Collections.Generic; // 목록 자료형
using ProjectH.Core; // 조건부 로그 기능 (Day74 추가)
using UnityEngine; // Unity 수학·로그 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleBondSkills // 결속 5단계 결속 스킬 실행 (Day59 신규 — 기획서 6.11 예시 기반)
    {
        private const string SerenaId = "CH_SERENA"; // 세레나 ID
        private const string EllenId = "CH_ELLEN"; // 엘렌 ID
        private const string LiliaId = "CH_LILIA"; // 릴리아 ID
        private const string EveId = "CH_EVE"; // 이브 ID
        public const float SerenaShieldRatio = 0.20f; // 수호의 기도 보호막 (대상 최대 체력 비율)
        public const float EllenDamageReduction = 0.10f; // 철벽의 맹세 파티 피해 감소
        public const float EllenDuration = 6f; // 철벽의 맹세 지속 시간
        public const float LiliaBurstRatio = 0.50f; // 마력 공명 추가 피해 (공격력 비율)
        public const float EveArrowRatio = 0.40f; // 정령의 화살 추가 피해 (공격력 비율)
        public const float EveArrowCooldown = 3f; // 정령의 화살 쿨타임

        public static bool TrySerenaGuardPrayer(BattleActor serena, BattleActor damagedAlly) // 수호의 기도 : 아군 체력 30% 이하 시 전투당 1회 큰 보호막
        {
            if (serena == null || damagedAlly == null || !BattleBondRuntimeState.HasBondSkill(SerenaId)) // 세레나·결속 5단계 확인
            {
                return false; // 발동 안 함
            }

            if (!BattleBondRuntimeState.TryUseOnce($"BOND_SERENA:{serena.Stats.RuntimeId}")) // 전투당 1회 확인
            {
                return false; // 이미 사용
            }

            int shield = Mathf.Max(1, Mathf.RoundToInt(damagedAlly.Stats.MaxHp * SerenaShieldRatio)); // 최대 체력 20% 보호막
            BattlePassiveRuntimeState.GrantShield(damagedAlly.Stats.RuntimeId, shield); // 보호막 부여
            GameLog.Info($"[Project H][BOND] 수호의 기도 -> {damagedAlly.Stats.RuntimeId}, Shield={shield}"); // 발동 로그
            return true; // 발동 반환
        }

        public static void OnUltimateExecuted(string characterId, BattleActor owner, BattleCombatRegistry registry) // 궁극기 실행 직후 결속 스킬 (엘렌·릴리아)
        {
            if (owner == null || registry == null || !BattleBondRuntimeState.HasBondSkill(characterId)) // 사용자·결속 5단계 확인
            {
                return; // 처리 없음
            }

            if (characterId == EllenId) // 엘렌 철벽의 맹세
            {
                foreach (BattleActor ally in CollectLiving(registry, BattleTeam.Ally)) // 생존 아군 순회
                {
                    BattleSkillRuntimeState.AddModifier(ally.Stats.RuntimeId, BattleRuntimeModifierKind.DamageReductionPercent, EllenDamageReduction, EllenDuration, "BOND_ELLEN_OATH"); // 파티 피해 감소 추가
                }

                GameLog.Info("[Project H][BOND] 철벽의 맹세 발동"); // 발동 로그
            }
            else if (characterId == LiliaId) // 릴리아 마력 공명
            {
                foreach (BattleActor enemy in CollectLiving(registry, BattleTeam.Enemy)) // 생존 적 순회 (궁극기 이후 남은 적)
                {
                    BattleUltimateEffectExecutor.ApplyDamage(owner, enemy, LiliaBurstRatio, BattleDamageType.Magic); // 마력 폭발 추가 피해
                }

                GameLog.Info("[Project H][BOND] 마력 공명 발동"); // 발동 로그
            }
        }

        public static bool TryEveSpiritArrow(BattleActor eve, BattleActor target) // 정령의 화살 : 이브 치명타 시 관통 화살 추가 (쿨타임 3초)
        {
            BattleStats stats = eve == null || !eve.IsCombatReady ? null : eve.Stats as BattleStats; // 이브 스탯 조회

            if (stats == null || stats.CharacterId != EveId || target == null || !target.IsCombatReady || !target.Stats.IsAlive || !BattleBondRuntimeState.HasBondSkill(EveId)) // 대상·결속 5단계 확인
            {
                return false; // 발동 안 함
            }

            string cooldownKey = $"BOND_EVE_ARROW:{stats.RuntimeId}"; // 쿨타임 키

            if (!BattlePassiveRuntimeState.IsCooldownReady(cooldownKey)) // 쿨타임 확인
            {
                return false; // 쿨타임 중
            }

            BattlePassiveRuntimeState.StartCooldown(cooldownKey, EveArrowCooldown); // 쿨타임 시작
            bool hit = BattleUltimateEffectExecutor.ApplyDamage(eve, target, EveArrowRatio, BattleDamageType.Physical); // 관통 화살 추가 피해
            GameLog.Info($"[Project H][BOND] 정령의 화살 -> {target.Stats.RuntimeId}, Hit={hit}"); // 발동 로그
            return hit; // 적중 여부 반환
        }

        private static List<BattleActor> CollectLiving(BattleCombatRegistry registry, BattleTeam team) // 생존 액터 스냅샷 (피해로 목록이 바뀌어도 안전)
        {
            List<BattleActor> result = new List<BattleActor>(); // 결과 목록

            for (int index = 0; index < registry.Actors.Count; index++) // 액터 순회
            {
                BattleActor actor = registry.Actors[index]; // 액터 조회
                if (actor != null && actor.Team == team && actor.IsCombatReady && actor.Stats.IsAlive) result.Add(actor); // 생존 대상 추가
            }

            return result; // 목록 반환
        }
    }
}
