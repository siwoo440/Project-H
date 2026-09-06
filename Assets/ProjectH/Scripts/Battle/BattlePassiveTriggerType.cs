namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public enum BattlePassiveTriggerType // 패시브 발동 Trigger 종류
    {
        BattleStart = 0, // 전투 시작 Trigger
        BasicAttackHit = 1, // 기본 공격 적중 Trigger
        BasicAttackMiss = 2, // 기본 공격 빗나감 Trigger
        DamageTaken = 3, // 피해 수신 Trigger
        Death = 4, // 전투 불능 Trigger
        SkillUsed = 5, // 스킬 사용 Trigger
        SkillEnhancementUsed = 6 // 강화 스킬 사용 Trigger
    }
}
