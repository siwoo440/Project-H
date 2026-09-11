using System; // 빈 배열 기능
using System.Collections.Generic; // 목록 자료형
using ProjectH.Data; // 아이템 데이터 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    public sealed class GiftResult // 선물 1회 결과 (Day57 신규)
    {
        public bool Success { get; } // 선물 성공 여부
        public string Message { get; } // 화면 안내 문구
        public GiftPreference Preference { get; } // 적용된 취향
        public int AffinityBefore { get; } // 선물 전 호감도
        public int AffinityAfter { get; } // 선물 후 호감도
        public int AffinityGain => AffinityAfter - AffinityBefore; // 실제 증가량 (100 초과분 제외)
        public int GiftsGivenToday { get; } // 오늘 선물 횟수
        public bool DiscoveredPreference { get; } // 이번 선물로 취향을 처음 알게 됐는지
        public IReadOnlyList<AffinityTier> NewTiers { get; } // 이번 선물로 새로 도달한 단계 목록

        private GiftResult(bool success, string message, GiftPreference preference, int before, int after, int giftsToday, bool discovered, IReadOnlyList<AffinityTier> newTiers) // 결과 생성
        {
            Success = success; // 성공 여부 저장
            Message = message ?? string.Empty; // 안내 문구 저장
            Preference = preference; // 취향 저장
            AffinityBefore = before; // 선물 전 호감도 저장
            AffinityAfter = after; // 선물 후 호감도 저장
            GiftsGivenToday = giftsToday; // 오늘 횟수 저장
            DiscoveredPreference = discovered; // 취향 발견 여부 저장
            NewTiers = newTiers ?? Array.Empty<AffinityTier>(); // 새 단계 목록 저장
        }

        internal static GiftResult Failed(string message, int affinity, int giftsToday) // 실패 결과 생성 (상태 변경 없음)
        {
            return new GiftResult(false, message, GiftPreference.Normal, affinity, affinity, giftsToday, false, null); // 실패 결과 반환
        }

        internal static GiftResult Succeeded(string message, GiftPreference preference, int before, int after, int giftsToday, bool discovered, IReadOnlyList<AffinityTier> newTiers) // 성공 결과 생성
        {
            return new GiftResult(true, message, preference, before, after, giftsToday, discovered, newTiers); // 성공 결과 반환
        }
    }

    public static class GiftService // 캐릭터 선물 기능 (Day57 신규)
    {
        public const int DailyGiftLimit = 3; // 캐릭터당 하루 선물 횟수
        public const string KnownFlagPrefix = "SYS_GIFT_KNOWN:"; // 취향 발견 스토리 플래그 접두사

        public static string BuildKnownFlag(string characterId, string itemId) // 취향 발견 플래그 ID 생성
        {
            return $"{KnownFlagPrefix}{characterId}:{itemId}"; // SYS_GIFT_KNOWN:캐릭터:선물 반환
        }

        public static bool IsPreferenceKnown(SaveData saveData, string characterId, string itemId) // 취향 발견 여부 확인
        {
            return saveData != null && saveData.HasStoryFlag(BuildKnownFlag(characterId, itemId)); // 플래그 보유 여부 반환
        }

        public static int GetGiftsGivenToday(SaveData saveData, string characterId) // 오늘 선물 횟수 조회
        {
            CharacterSaveData character = saveData == null ? null : saveData.FindCharacter(characterId); // 캐릭터 진행 조회
            return character == null ? 0 : character.GetGiftsGivenToday(GameTimeService.GetCurrentDay(saveData)); // 오늘 기준 횟수 반환
        }

        public static int GetRemainingToday(SaveData saveData, string characterId) // 오늘 남은 선물 횟수 조회
        {
            return Math.Max(0, DailyGiftLimit - GetGiftsGivenToday(saveData, characterId)); // 남은 횟수 반환
        }

        public static bool TryGive(SaveData saveData, DataManager dataManager, string characterId, string itemId, out GiftResult result) // 선물하기 시도
        {
            CharacterSaveData character = saveData == null ? null : saveData.FindCharacter(characterId); // 캐릭터 진행 조회

            if (character == null) // 저장 및 캐릭터 확인
            {
                result = GiftResult.Failed("선물할 캐릭터를 찾을 수 없습니다.", 0, 0); // 캐릭터 누락 결과
                return false; // 선물 실패 반환
            }

            int currentDay = GameTimeService.GetCurrentDay(saveData); // 현재 일차 조회
            int affinity = character.Affinity; // 현재 호감도 조회
            int giftsToday = character.GetGiftsGivenToday(currentDay); // 오늘 선물 횟수 조회
            ItemData item = dataManager == null || !dataManager.IsInitialized ? null : dataManager.GetItem(itemId); // 선물 아이템 원본 조회

            if (item == null || item.Type != ItemType.Gift) // 선물 아이템 여부 확인
            {
                result = GiftResult.Failed("선물할 수 없는 아이템입니다.", affinity, giftsToday); // 선물 아님 결과
                return false; // 선물 실패 반환
            }

            if (affinity >= CharacterSaveData.MaxAffinity) // 호감도 최대 확인
            {
                result = GiftResult.Failed("호감도가 이미 최대입니다. 선물은 아껴 두세요.", affinity, giftsToday); // 최대 호감도 거부 (선물 소모 없음)
                return false; // 선물 실패 반환
            }

            if (giftsToday >= DailyGiftLimit) // 하루 제한 확인
            {
                result = GiftResult.Failed($"오늘은 더 이상 선물할 수 없습니다. ({giftsToday}/{DailyGiftLimit}) 날짜가 바뀌면 다시 줄 수 있어요.", affinity, giftsToday); // 제한 초과 결과
                return false; // 선물 실패 반환
            }

            if (!ItemInventoryService.TryRemove(saveData, dataManager, itemId, 1, out string removeError)) // 가방에서 1개 차감
            {
                string reason = saveData.GetItemCount(itemId) <= 0 ? $"가방에 {item.DisplayName}이(가) 없습니다." : removeError; // 실패 사유 결정
                result = GiftResult.Failed(reason, affinity, giftsToday); // 차감 실패 결과
                return false; // 선물 실패 반환
            }

            GiftPreference preference = GiftPreferenceCatalog.GetPreference(characterId, itemId); // 취향 조회
            AffinityTier beforeTier = AffinityService.ResolveTier(affinity); // 선물 전 단계
            int after = AffinityService.AddAffinity(saveData, characterId, GiftPreferenceCatalog.GetAffinityGain(preference)); // 호감도 증가 (100에서 멈춤)
            AffinityTier afterTier = AffinityService.ResolveTier(after); // 선물 후 단계
            int giftsAfter = character.RecordGift(currentDay); // 오늘 선물 횟수 기록
            bool discovered = saveData.SetStoryFlag(BuildKnownFlag(characterId, itemId)); // 취향 발견 플래그 기록 (처음일 때만 true)
            List<AffinityTier> newTiers = new List<AffinityTier>(); // 새로 도달한 단계 목록

            for (int tier = (int)beforeTier + 1; tier <= (int)afterTier; tier++) // 넘어선 단계 순회
            {
                newTiers.Add((AffinityTier)tier); // 새 단계 추가
            }

            string message = BuildSuccessMessage(item.DisplayName, preference, after - affinity, after, giftsAfter, discovered, newTiers); // 성공 안내 문구 생성
            result = GiftResult.Succeeded(message, preference, affinity, after, giftsAfter, discovered, newTiers); // 성공 결과 생성
            return true; // 선물 성공 반환
        }

        private static string BuildSuccessMessage(string itemName, GiftPreference preference, int gain, int after, int giftsToday, bool discovered, List<AffinityTier> newTiers) // 성공 안내 문구 조립
        {
            string discoveredText = discovered ? " (새 취향 발견!)" : string.Empty; // 취향 발견 문구
            string message = $"{itemName} 선물 · {GiftPreferenceCatalog.GetLabel(preference)}{discoveredText} · 호감도 +{gain} ({after}/{CharacterSaveData.MaxAffinity}) · 오늘 {giftsToday}/{DailyGiftLimit}"; // 기본 결과 문구

            if (newTiers.Count > 0) // 새 단계 도달 확인
            {
                message += $"\n{AffinityService.GetTierLabel(newTiers[newTiers.Count - 1])} 단계 도달! [호감도 보상]에서 보상을 받으세요."; // Day56 호감도 보상 안내
            }

            return message; // 안내 문구 반환
        }
    }
}
