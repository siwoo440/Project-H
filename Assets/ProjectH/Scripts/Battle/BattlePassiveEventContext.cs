using ProjectH.Battle.SkillBlock; // 스킬 사용 요청 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public readonly struct BattlePassiveEventContext // 패시브 Trigger 실행 Context
    {
        public BattlePassiveTriggerType TriggerType { get; } // Trigger 종류
        public BattleActor Actor { get; } // Trigger 주체 액터
        public BattleActor Target { get; } // Trigger 상대 액터
        public BattleSkillRequest SkillRequest { get; } // 스킬 사용 요청
        public int Amount { get; } // 피해 등 수치 정보

        public BattlePassiveEventContext(BattlePassiveTriggerType triggerType, BattleActor actor, BattleActor target, BattleSkillRequest skillRequest, int amount) // 패시브 Context 생성
        {
            TriggerType = triggerType; // Trigger 종류 저장
            Actor = actor; // Trigger 주체 저장
            Target = target; // Trigger 상대 저장
            SkillRequest = skillRequest; // 스킬 요청 저장
            Amount = amount; // 수치 정보 저장
        }

        public static BattlePassiveEventContext CreateBattleStart() // 전투 시작 Context 생성
        {
            return new BattlePassiveEventContext(BattlePassiveTriggerType.BattleStart, null, null, default, 0); // 전투 시작 Context 반환
        }

        public static BattlePassiveEventContext CreateBasicAttackHit(BattleActor attacker, BattleActor target, int damage) // 기본 공격 적중 Context 생성
        {
            return new BattlePassiveEventContext(BattlePassiveTriggerType.BasicAttackHit, attacker, target, default, damage); // 기본 공격 적중 Context 반환
        }

        public static BattlePassiveEventContext CreateBasicAttackMiss(BattleActor attacker, BattleActor target) // 기본 공격 빗나감 Context 생성
        {
            return new BattlePassiveEventContext(BattlePassiveTriggerType.BasicAttackMiss, attacker, target, default, 0); // 기본 공격 빗나감 Context 반환
        }

        public static BattlePassiveEventContext CreateDamageTaken(BattleActor target, int damage) // 피해 수신 Context 생성
        {
            return new BattlePassiveEventContext(BattlePassiveTriggerType.DamageTaken, target, null, default, damage); // 피해 수신 Context 반환
        }

        public static BattlePassiveEventContext CreateSkillUsed(BattleActor owner, BattleSkillRequest request) // 스킬 사용 Context 생성
        {
            BattlePassiveTriggerType trigger = request.EnhancementLevel > 1 ? BattlePassiveTriggerType.SkillEnhancementUsed : BattlePassiveTriggerType.SkillUsed; // 강화도 기반 Trigger 선택
            return new BattlePassiveEventContext(trigger, owner, null, request, 0); // 스킬 사용 Context 반환
        }
    }
}
