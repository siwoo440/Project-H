using System.Collections.Generic; // 목록 자료형
using ProjectH.Battle; // 아이템 지급 기능
using ProjectH.Data; // 데이터 관리자 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    public enum AffinityRewardState // 호감도 단계 보상 버튼 상태 (Day56 신규)
    {
        Locked = 0, // 아직 단계 미도달
        Claimable = 1, // 도달했지만 미수령
        Claimed = 2 // 수령 완료
    }

    public static class AffinityRewardService // 호감도 단계 보상 수령 기능 (Day56 신규 — 버튼으로 직접 수령)
    {
        public static AffinityRewardState GetState(SaveData saveData, string characterId, AffinityTier tier) // 단계 보상 상태 조회
        {
            CharacterSaveData character = saveData == null ? null : saveData.FindCharacter(characterId); // 캐릭터 진행 조회

            if (character == null || AffinityRewardCatalog.Get(tier) == null) // 캐릭터 및 보상 존재 확인
            {
                return AffinityRewardState.Locked; // 잠김 반환
            }

            if (character.IsAffinityRewardClaimed(tier)) // 수령 여부 확인
            {
                return AffinityRewardState.Claimed; // 수령 완료 반환 (호감도가 떨어져도 유지)
            }

            return AffinityService.ResolveTier(character.Affinity) >= tier ? AffinityRewardState.Claimable : AffinityRewardState.Locked; // 도달 여부 기반 상태 반환
        }

        public static bool TryClaim(SaveData saveData, DataManager dataManager, string characterId, AffinityTier tier, out string message) // 단계 보상 수령 시도
        {
            AffinityTierReward reward = AffinityRewardCatalog.Get(tier); // 단계 보상 조회
            AffinityRewardState state = GetState(saveData, characterId, tier); // 현재 상태 조회

            if (reward == null || state == AffinityRewardState.Locked) // 수령 가능 여부 확인
            {
                message = $"{AffinityService.GetTierLabel(tier)} 단계에 도달해야 받을 수 있습니다."; // 잠김 안내
                return false; // 수령 실패 반환
            }

            if (state == AffinityRewardState.Claimed) // 중복 수령 확인
            {
                message = "이미 받은 보상입니다."; // 중복 안내
                return false; // 수령 실패 반환
            }

            List<DungeonDropResult> items = new List<DungeonDropResult>(); // 지급 아이템 목록

            if (!string.IsNullOrEmpty(reward.ItemId) && reward.ItemQuantity > 0 && dataManager != null) // 아이템 보상 및 데이터 준비 확인 (데이터 미준비 테스트 환경은 아이템 생략)
            {
                items.Add(new DungeonDropResult(reward.ItemId, reward.ItemQuantity)); // 아이템 지급 목록 추가

                if (!DungeonDropGrantService.CanGrantAll(saveData, dataManager, items, out string error)) // 아이템 지급 가능 여부 확인
                {
                    message = $"보상 아이템을 지급할 수 없습니다. {error}"; // 지급 불가 안내
                    return false; // 수령 실패 반환 (상태 변경 없음)
                }
            }

            if (reward.Gold > 0) // 골드 보상 확인
            {
                GoldCurrencyService.AddGold(saveData, reward.Gold); // 골드 지급
            }

            if (items.Count > 0) // 아이템 지급 대상 확인
            {
                DungeonDropGrantService.GrantAll(saveData, dataManager, items); // 아이템 지급 (Day45 드롭 지급 재사용)
            }

            saveData.FindCharacter(characterId).MarkAffinityRewardClaimed(tier); // 수령 기록
            message = $"{AffinityService.GetTierLabel(tier)} 보상 획득! {reward.Summary}"; // 수령 결과 안내
            return true; // 수령 성공 반환
        }
    }
}
