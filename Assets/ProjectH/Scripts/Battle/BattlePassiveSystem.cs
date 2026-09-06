using ProjectH.Battle.SkillBlock; // 스킬 요청 기능
using ProjectH.Data; // 스킬 대상 데이터 기능
using UnityEngine; // Unity 수학 및 로그 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattlePassiveSystem // 초기 캐릭터 패시브 공통 실행 시스템
    {
        private const string SerenaCharacterId = "CH_SERENA"; // 세레나 캐릭터 ID
        private const string EllenCharacterId = "CH_ELLEN"; // 엘렌 캐릭터 ID
        private const string LiliaCharacterId = "CH_LILIA"; // 릴리아 캐릭터 ID
        private const string EveCharacterId = "CH_EVE"; // 이브 캐릭터 ID
        private const string EveHitCounterKey = "EVE_BASIC_HIT"; // 이브 연속 적중 카운터 키
        private const float CriticalDamageMultiplier = 1.5f; // 프로토타입 기본 치명타 피해 배율
        private static BattleCombatRegistry registry; // 현재 전투 Registry

        public static void Initialize(BattleCombatRegistry combatRegistry) // 패시브 시스템 전투 초기화
        {
            registry = combatRegistry; // 현재 전투 Registry 저장
            BattlePassiveRuntimeState.ResetAll(); // 신규 전투 패시브 Runtime 초기화
        }

        public static void Shutdown(BattleCombatRegistry combatRegistry) // 패시브 시스템 전투 종료
        {
            if (registry != combatRegistry) // 현재 Registry 일치 확인
            {
                return; // 다른 전투 종료 요청 무시
            }

            BattlePassiveRuntimeState.ResetAll(); // 전투 종료 패시브 Runtime 제거
            registry = null; // 현재 전투 Registry 연결 해제
        }

        public static void Handle(BattlePassiveEventContext context) // 패시브 Trigger 공통 처리
        {
            if (registry == null) // 현재 전투 Registry 확인
            {
                return; // 패시브 시스템 미초기화 시 처리 중단
            }

            switch (context.TriggerType) // 패시브 Trigger 종류 분기
            {
                case BattlePassiveTriggerType.BasicAttackHit: // 기본 공격 적중 Trigger 처리
                    HandleBasicAttackHit(context.Actor); // 이브 연속 적중 패시브 처리
                    break; // 기본 공격 적중 처리 종료
                case BattlePassiveTriggerType.BasicAttackMiss: // 기본 공격 빗나감 Trigger 처리
                    HandleBasicAttackMiss(context.Actor); // 이브 연속 적중 초기화 처리
                    break; // 기본 공격 빗나감 처리 종료
                case BattlePassiveTriggerType.DamageTaken: // 피해 수신 Trigger 처리
                    HandleDamageTaken(context.Actor, context.Amount); // 세레나 및 엘렌 패시브 처리
                    break; // 피해 수신 처리 종료
                case BattlePassiveTriggerType.SkillUsed: // 일반 스킬 사용 Trigger 처리
                case BattlePassiveTriggerType.SkillEnhancementUsed: // 강화 스킬 사용 Trigger 처리
                    HandleSkillUsed(context.Actor, context.SkillRequest); // 릴리아 광역 스킬 패시브 처리
                    break; // 스킬 Trigger 처리 종료
            }
        }

        public static float GetBasicAttackCriticalChance(BattleActor actor) // 기본 공격 최종 치명타율 조회
        {
            if (actor == null || !actor.IsCombatReady) // 전투 액터 상태 확인
            {
                return 0f; // 잘못된 액터 치명타율 0 반환
            }

            float baseChance = actor.Stats is BattleStats stats ? stats.CriticalRate : 0f; // 캐릭터 기본 치명타율 조회
            float passiveBonus = BattlePassiveRuntimeState.GetCriticalChanceBonus(actor.Stats.RuntimeId); // 패시브 치명타율 증가 조회
            return Mathf.Clamp01(baseChance + passiveBonus); // 최종 치명타율 반환
        }

        public static float GetBasicAttackCriticalDamageMultiplier() // 기본 공격 치명타 피해 배율 조회
        {
            return CriticalDamageMultiplier; // 프로토타입 치명타 피해 배율 반환
        }

        public static bool IsAreaSkill(BattleSkillRequest request) // 광역 스킬 여부 판정
        {
            if (!request.IsValid || request.Skill == null) // 스킬 요청 확인
            {
                return false; // 잘못된 스킬 광역 판정 실패
            }

            if (IsAreaTargetType(request.Skill.TargetType)) // 대표 TargetType 광역 여부 확인
            {
                return true; // 대표 TargetType 광역 스킬 반환
            }

            SkillEnhancementData enhancement = request.Skill.GetEnhancement(request.EnhancementLevel); // 현재 강화도 데이터 조회

            if (enhancement == null || enhancement.Effects == null) // 강화도 효과 목록 확인
            {
                return false; // 실제 효과 없는 스킬 광역 판정 실패
            }

            for (int index = 0; index < enhancement.Effects.Length; index++) // 실제 효과 목록 순회
            {
                SkillEffectDefinition effect = enhancement.Effects[index]; // 현재 스킬 효과 조회

                if (effect != null && IsAreaTargetType(effect.TargetType)) // 실제 효과 TargetType 광역 여부 확인
                {
                    return true; // 광역 효과 포함 스킬 반환
                }
            }

            return false; // 광역 효과 없음 반환
        }

        private static void HandleDamageTaken(BattleActor damagedActor, int appliedDamage) // 피해 기반 패시브 처리
        {
            if (damagedActor == null || !damagedActor.IsCombatReady || damagedActor.Team != BattleTeam.Ally || !damagedActor.Stats.IsAlive || appliedDamage <= 0) // 피해 대상 상태 확인
            {
                return; // 피해 기반 패시브 처리 중단
            }

            TrySerenaShield(damagedActor); // 세레나 저체력 아군 보호막 처리
            TryEllenDefense(damagedActor); // 엘렌 저체력 방어 증가 처리
        }

        private static void TrySerenaShield(BattleActor damagedAlly) // 세레나 저체력 아군 보호막 처리
        {
            if (GetHealthRatio(damagedAlly.Stats) > 0.30f) // 아군 체력 30퍼센트 이하 조건 확인
            {
                return; // 세레나 보호막 조건 미충족
            }

            BattleActor serena = FindLivingCharacter(SerenaCharacterId); // 생존 세레나 조회

            if (serena == null) // 세레나 파티 포함 및 생존 확인
            {
                return; // 세레나 패시브 처리 중단
            }

            string cooldownKey = $"PASSIVE_SERENA_SHIELD:{serena.Stats.RuntimeId}"; // 세레나 패시브 쿨다운 키 생성

            if (!BattlePassiveRuntimeState.IsCooldownReady(cooldownKey)) // 세레나 패시브 쿨다운 확인
            {
                return; // 세레나 패시브 쿨다운 중단
            }

            int shieldAmount = Mathf.Max(1, Mathf.RoundToInt(damagedAlly.Stats.MaxHp * 0.06f)); // 대상 최대 체력 6퍼센트 보호막 계산
            BattlePassiveRuntimeState.GrantShield(damagedAlly.Stats.RuntimeId, shieldAmount); // 저체력 아군 보호막 부여
            BattlePassiveRuntimeState.StartCooldown(cooldownKey, 12f); // 세레나 패시브 12초 쿨다운 시작
            Debug.Log($"[Project H][PASSIVE] Serena shield -> {damagedAlly.Stats.RuntimeId}, Shield={shieldAmount}"); // 세레나 패시브 로그
        }

        private static void TryEllenDefense(BattleActor damagedActor) // 엘렌 저체력 방어 증가 처리
        {
            if (!IsCharacter(damagedActor, EllenCharacterId) || GetHealthRatio(damagedActor.Stats) > 0.40f) // 엘렌 및 체력 40퍼센트 이하 조건 확인
            {
                return; // 엘렌 패시브 조건 미충족
            }

            string cooldownKey = $"PASSIVE_ELLEN_DEFENSE:{damagedActor.Stats.RuntimeId}"; // 엘렌 패시브 쿨다운 키 생성

            if (!BattlePassiveRuntimeState.IsCooldownReady(cooldownKey)) // 엘렌 패시브 쿨다운 확인
            {
                return; // 엘렌 패시브 쿨다운 중단
            }

            BattleSkillRuntimeState.AddModifier(damagedActor.Stats.RuntimeId, BattleRuntimeModifierKind.DefensePercent, 0.20f, 10f, "PASSIVE_ELLEN_DEFENSE"); // 엘렌 방어력 20퍼센트 증가 적용
            BattlePassiveRuntimeState.StartCooldown(cooldownKey, 20f); // 엘렌 패시브 20초 쿨다운 시작
            Debug.Log($"[Project H][PASSIVE] Ellen defense -> {damagedActor.Stats.RuntimeId}"); // 엘렌 패시브 로그
        }

        private static void HandleSkillUsed(BattleActor owner, BattleSkillRequest request) // 스킬 사용 기반 패시브 처리
        {
            if (!IsCharacter(owner, LiliaCharacterId) || !IsAreaSkill(request)) // 릴리아 및 광역 스킬 조건 확인
            {
                return; // 릴리아 패시브 조건 미충족
            }

            for (int index = 0; index < registry.Actors.Count; index++) // 전체 전투 액터 순회
            {
                BattleActor target = registry.Actors[index]; // 현재 전투 액터 조회

                if (target == null || target.Team != BattleTeam.Enemy || !target.IsCombatReady || !target.Stats.IsAlive) // 생존 적군 여부 확인
                {
                    continue; // 릴리아 방어 감소 대상 제외
                }

                BattlePassiveRuntimeState.SetDefenseReduction(target.Stats.RuntimeId, 0.05f, 6f); // 적군 방어력 5퍼센트 감소 6초 적용
            }

            Debug.Log($"[Project H][PASSIVE] Lilia area defense reduction -> Skill={request.SkillId}"); // 릴리아 패시브 로그
        }

        private static void HandleBasicAttackHit(BattleActor attacker) // 기본 공격 적중 기반 패시브 처리
        {
            if (!IsCharacter(attacker, EveCharacterId)) // 이브 기본 공격 여부 확인
            {
                return; // 이브 패시브 처리 중단
            }

            int hitCount = BattlePassiveRuntimeState.IncrementCounter(attacker.Stats.RuntimeId, EveHitCounterKey); // 이브 연속 적중 카운터 증가

            if (hitCount < 5) // 5회 연속 적중 여부 확인
            {
                return; // 이브 패시브 발동 대기
            }

            BattlePassiveRuntimeState.ResetCounter(attacker.Stats.RuntimeId, EveHitCounterKey); // 이브 연속 적중 카운터 초기화
            BattlePassiveRuntimeState.SetCriticalChanceBonus(attacker.Stats.RuntimeId, 0.04f, 10f); // 이브 치명타율 4퍼센트포인트 증가 10초 적용
            Debug.Log($"[Project H][PASSIVE] Eve critical chance +4% -> {attacker.Stats.RuntimeId}"); // 이브 패시브 로그
        }

        private static void HandleBasicAttackMiss(BattleActor attacker) // 기본 공격 빗나감 기반 패시브 처리
        {
            if (!IsCharacter(attacker, EveCharacterId)) // 이브 기본 공격 여부 확인
            {
                return; // 이브 패시브 처리 중단
            }

            BattlePassiveRuntimeState.ResetCounter(attacker.Stats.RuntimeId, EveHitCounterKey); // 이브 연속 적중 카운터 초기화
        }

        private static BattleActor FindLivingCharacter(string characterId) // 캐릭터 ID 기반 생존 아군 조회
        {
            if (registry == null || string.IsNullOrWhiteSpace(characterId)) // Registry 및 캐릭터 ID 확인
            {
                return null; // 캐릭터 조회 실패 반환
            }

            for (int index = 0; index < registry.Actors.Count; index++) // 전체 전투 액터 순회
            {
                BattleActor actor = registry.Actors[index]; // 현재 전투 액터 조회

                if (actor == null || actor.Team != BattleTeam.Ally || !actor.IsCombatReady || !actor.Stats.IsAlive) // 생존 아군 여부 확인
                {
                    continue; // 캐릭터 후보 제외
                }

                if (IsCharacter(actor, characterId)) // 캐릭터 ID 일치 확인
                {
                    return actor; // 생존 캐릭터 반환
                }
            }

            return null; // 생존 캐릭터 없음 반환
        }

        private static bool IsCharacter(BattleActor actor, string characterId) // 전투 액터 캐릭터 ID 일치 확인
        {
            BattleStats stats = actor != null && actor.IsCombatReady ? actor.Stats as BattleStats : null; // 캐릭터 전투 스탯 변환
            return stats != null && stats.CharacterId == characterId; // 캐릭터 ID 일치 여부 반환
        }

        private static float GetHealthRatio(IBattleCombatantStats stats) // 전투 스탯 체력 비율 계산
        {
            if (stats == null || stats.MaxHp <= 0) // 전투 스탯 및 최대 체력 확인
            {
                return 0f; // 잘못된 체력 비율 0 반환
            }

            return Mathf.Clamp01((float)stats.CurrentHp / stats.MaxHp); // 현재 체력 비율 반환
        }

        private static bool IsAreaTargetType(SkillTargetType targetType) // 광역 TargetType 여부 확인
        {
            return targetType == SkillTargetType.AllEnemies || targetType == SkillTargetType.AllAllies || targetType == SkillTargetType.LineEnemies || targetType == SkillTargetType.NearbyEnemiesFromPrimary; // 광역 대상 종류 판정 반환
        }
    }
}
