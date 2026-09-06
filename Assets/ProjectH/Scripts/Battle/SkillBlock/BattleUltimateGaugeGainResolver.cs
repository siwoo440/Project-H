using ProjectH.Data; // 스킬 강화도 데이터 기능

namespace ProjectH.Battle.SkillBlock // 스킬 블록 전투 영역
{
    public static class BattleUltimateGaugeGainResolver // 성공한 블록 스킬의 궁극기 충전량 계산 기능
    {
        public static int Resolve(BattleSkillRequest request) // 스킬 요청 강화도 기반 충전량 조회
        {
            if (!request.IsValid || request.Skill == null) // 요청 및 SkillData 유효성 확인
            {
                return 0; // 잘못된 요청 충전량 없음 반환
            }

            SkillEnhancementData enhancement = request.Skill.GetEnhancement(request.EnhancementLevel); // 요청 강화도 실제 데이터 조회

            if (enhancement == null) // 강화도 데이터 존재 확인
            {
                return 0; // 강화도 데이터 없음 충전 차단
            }

            return enhancement.UltimateGaugeGain; // SkillData 강화도별 궁극기 충전량 반환
        }
    }
}
