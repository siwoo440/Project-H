using System.Collections.Generic; // 읽기 전용 목록 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    public sealed class AffinityTierReward // 호감도 단계별 보상 정의 (Day56 신규)
    {
        public AffinityTier Tier { get; } // 보상 단계
        public int Gold { get; } // 골드 보상
        public string ItemId { get; } // 아이템 보상 ID (없으면 빈 문자열)
        public int ItemQuantity { get; } // 아이템 보상 수량
        public string ItemLabel { get; } // 아이템 표시 이름
        public float HpBonus { get; } // 전투 최대 체력 증가율 (수령 후 적용)
        public float AttackBonus { get; } // 전투 공격력 증가율 (수령 후 적용)
        public int StartUltimateGauge { get; } // 전투 시작 궁극기 게이지 (수령 후 적용)

        public AffinityTierReward(AffinityTier tier, int gold, string itemId, int itemQuantity, string itemLabel, float hpBonus, float attackBonus, int startUltimateGauge) // 단계 보상 생성
        {
            Tier = tier; // 단계 저장
            Gold = gold; // 골드 저장
            ItemId = itemId ?? string.Empty; // 아이템 ID 저장
            ItemQuantity = itemQuantity; // 아이템 수량 저장
            ItemLabel = itemLabel ?? string.Empty; // 아이템 표시 이름 저장
            HpBonus = hpBonus; // 체력 증가율 저장
            AttackBonus = attackBonus; // 공격력 증가율 저장
            StartUltimateGauge = startUltimateGauge; // 시작 게이지 저장
        }

        public string Summary // 보상 요약 문구 반환
        {
            get
            {
                List<string> parts = new List<string>(); // 요약 조각 목록

                if (Gold > 0) parts.Add($"골드 {Gold}"); // 골드 요약 추가
                if (!string.IsNullOrEmpty(ItemId) && ItemQuantity > 0) parts.Add($"{ItemLabel} ×{ItemQuantity}"); // 아이템 요약 추가
                if (HpBonus > 0f) parts.Add($"체력 +{HpBonus * 100f:0}%"); // 체력 보너스 요약 추가
                if (AttackBonus > 0f) parts.Add($"공격력 +{AttackBonus * 100f:0}%"); // 공격력 보너스 요약 추가
                if (StartUltimateGauge > 0) parts.Add($"궁극기 {StartUltimateGauge}으로 시작"); // 시작 게이지 요약 추가
                return string.Join(" · ", parts); // 요약 문구 반환
            }
        }
    }

    public readonly struct AffinityBattleBonus // 수령한 호감도 보상의 누적 전투 보너스 (Day56 신규)
    {
        public static AffinityBattleBonus Empty => new AffinityBattleBonus(0f, 0f, 0); // 보너스 없음

        public float HpPercent { get; } // 최대 체력 증가율 합계
        public float AttackPercent { get; } // 공격력 증가율 합계
        public int StartUltimateGauge { get; } // 전투 시작 궁극기 게이지

        public AffinityBattleBonus(float hpPercent, float attackPercent, int startUltimateGauge) // 누적 보너스 생성
        {
            HpPercent = hpPercent; // 체력 증가율 저장
            AttackPercent = attackPercent; // 공격력 증가율 저장
            StartUltimateGauge = startUltimateGauge; // 시작 게이지 저장
        }

        public bool IsEmpty => HpPercent <= 0f && AttackPercent <= 0f && StartUltimateGauge <= 0; // 보너스 없음 여부 반환

        public string Summary // 누적 보너스 요약 문구 반환
        {
            get
            {
                if (IsEmpty) // 보너스 없음 확인
                {
                    return "없음"; // 없음 문구 반환
                }

                List<string> parts = new List<string>(); // 요약 조각 목록
                if (HpPercent > 0f) parts.Add($"체력 +{HpPercent * 100f:0}%"); // 체력 요약 추가
                if (AttackPercent > 0f) parts.Add($"공격력 +{AttackPercent * 100f:0}%"); // 공격력 요약 추가
                if (StartUltimateGauge > 0) parts.Add($"궁극기 {StartUltimateGauge}으로 시작"); // 시작 게이지 요약 추가
                return string.Join(" · ", parts); // 요약 문구 반환
            }
        }
    }

    public static class AffinityRewardCatalog // 호감도 단계 보상 공통 목록 (Day56 신규 — 수치는 이 파일에서 조정)
    {
        private static readonly AffinityTierReward[] Rewards = // 단계별 보상 (낯섦은 보상 없음)
        {
            new AffinityTierReward(AffinityTier.Acquaintance, 100, "IT_MATERIAL_001", 2, "재료", 0.02f, 0f, 0), // 안면 (20)
            new AffinityTierReward(AffinityTier.Friendly, 200, "IT_POTION_SMALL", 1, "회복 물약", 0f, 0.02f, 0), // 호감 (40)
            new AffinityTierReward(AffinityTier.Trusted, 300, "IT_POTION_SMALL", 2, "회복 물약", 0.02f, 0.02f, 0), // 신뢰 (60)
            new AffinityTierReward(AffinityTier.Bonded, 500, "EQ_WEAPON_IRON", 1, "철제 무기", 0f, 0f, 25) // 유대 (80)
        };

        public static IReadOnlyList<AffinityTierReward> All => Rewards; // 전체 단계 보상 반환

        public static AffinityTierReward Get(AffinityTier tier) // 단계 보상 조회
        {
            for (int index = 0; index < Rewards.Length; index++) // 보상 순회
            {
                if (Rewards[index].Tier == tier) // 단계 일치 확인
                {
                    return Rewards[index]; // 보상 반환
                }
            }

            return null; // 보상 없는 단계 반환
        }

        public static AffinityBattleBonus GetClaimedBonus(CharacterSaveData character) // 수령한 단계 보상의 누적 전투 보너스 계산
        {
            if (character == null) // 캐릭터 확인
            {
                return AffinityBattleBonus.Empty; // 보너스 없음 반환
            }

            float hp = 0f; // 체력 증가율 합계
            float attack = 0f; // 공격력 증가율 합계
            int gauge = 0; // 시작 게이지 최대값

            for (int index = 0; index < Rewards.Length; index++) // 보상 순회
            {
                AffinityTierReward reward = Rewards[index]; // 보상 조회

                if (!character.IsAffinityRewardClaimed(reward.Tier)) // 수령 여부 확인
                {
                    continue; // 미수령 단계 제외 (받아야 효과 적용)
                }

                hp += reward.HpBonus; // 체력 증가율 누적
                attack += reward.AttackBonus; // 공격력 증가율 누적
                gauge = System.Math.Max(gauge, reward.StartUltimateGauge); // 시작 게이지는 최대값 사용
            }

            return new AffinityBattleBonus(hp, attack, gauge); // 누적 보너스 반환
        }
    }
}
