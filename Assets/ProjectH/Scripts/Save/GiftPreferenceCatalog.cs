using System; // 문자열 비교 기능
using System.Collections.Generic; // 읽기 전용 목록 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    public enum GiftPreference // 선물 취향 단계 (Day57 신규)
    {
        Dislike = 0, // 별로
        Normal = 1, // 보통 (표에 없는 조합 기본값)
        Like = 2, // 좋아함
        Love = 3 // 아주 좋아함
    }

    public readonly struct GiftPreferenceEntry // 캐릭터·선물 취향 한 줄 (Day57 신규)
    {
        public string CharacterId { get; } // 캐릭터 ID
        public string ItemId { get; } // 선물 아이템 ID
        public GiftPreference Preference { get; } // 취향 단계

        public GiftPreferenceEntry(string characterId, string itemId, GiftPreference preference) // 취향 항목 생성
        {
            CharacterId = characterId ?? string.Empty; // 캐릭터 ID 저장
            ItemId = itemId ?? string.Empty; // 선물 ID 저장
            Preference = preference; // 취향 저장
        }
    }

    public static class GiftPreferenceCatalog // 선물 목록·취향 표 (Day57 신규 — 수치와 취향은 이 파일에서 조정)
    {
        public const string Flower = "IT_GIFT_FLOWER"; // 들꽃 꽃다발
        public const string Candle = "IT_GIFT_CANDLE"; // 성수 향초
        public const string Brooch = "IT_GIFT_BROOCH"; // 기사단 문장 브로치
        public const string StarMap = "IT_GIFT_STARMAP"; // 별자리 지도
        public const string Shell = "IT_GIFT_SHELL"; // 정령의 조개
        public const string Sweets = "IT_GIFT_SWEETS"; // 달콤한 과자

        private static readonly string[] GiftIds = { Flower, Candle, Brooch, StarMap, Shell, Sweets }; // 선물 창 표시 순서

        private static readonly GiftPreferenceEntry[] Entries = // 취향 표 (여기에 없는 조합은 보통)
        {
            new GiftPreferenceEntry("CH_SERENA", Candle, GiftPreference.Love), // 세레나 : 성수 향초 아주 좋아함
            new GiftPreferenceEntry("CH_SERENA", Flower, GiftPreference.Like), // 세레나 : 들꽃 꽃다발 좋아함
            new GiftPreferenceEntry("CH_SERENA", Brooch, GiftPreference.Dislike), // 세레나 : 브로치 별로
            new GiftPreferenceEntry("CH_ELLEN", Brooch, GiftPreference.Love), // 엘렌 : 기사단 문장 브로치 아주 좋아함
            new GiftPreferenceEntry("CH_ELLEN", Sweets, GiftPreference.Like), // 엘렌 : 달콤한 과자 좋아함
            new GiftPreferenceEntry("CH_ELLEN", Flower, GiftPreference.Dislike), // 엘렌 : 들꽃 꽃다발 별로
            new GiftPreferenceEntry("CH_LILIA", StarMap, GiftPreference.Love), // 릴리아 : 별자리 지도 아주 좋아함
            new GiftPreferenceEntry("CH_LILIA", Candle, GiftPreference.Like), // 릴리아 : 성수 향초 좋아함
            new GiftPreferenceEntry("CH_LILIA", Shell, GiftPreference.Dislike), // 릴리아 : 정령의 조개 별로 (불 속성)
            new GiftPreferenceEntry("CH_EVE", Shell, GiftPreference.Love), // 이브 : 정령의 조개 아주 좋아함
            new GiftPreferenceEntry("CH_EVE", Sweets, GiftPreference.Like), // 이브 : 달콤한 과자 좋아함
            new GiftPreferenceEntry("CH_EVE", Candle, GiftPreference.Dislike), // 이브 : 성수 향초 별로 (물 속성)
            new GiftPreferenceEntry("CH_NATASHA", Sweets, GiftPreference.Love), // 나타샤 : 달콤한 과자 아주 좋아함 (Day64 추가 — 겉은 장난스러움)
            new GiftPreferenceEntry("CH_NATASHA", Brooch, GiftPreference.Like), // 나타샤 : 브로치 좋아함 (반짝이는 것)
            new GiftPreferenceEntry("CH_NATASHA", Candle, GiftPreference.Dislike), // 나타샤 : 성수 향초 별로 (교단 냄새)
            new GiftPreferenceEntry("CH_CLAIRE", Shell, GiftPreference.Love), // 클레어 : 정령의 조개 아주 좋아함 (연금 재료)
            new GiftPreferenceEntry("CH_CLAIRE", StarMap, GiftPreference.Like), // 클레어 : 별자리 지도 좋아함
            new GiftPreferenceEntry("CH_CLAIRE", Flower, GiftPreference.Dislike), // 클레어 : 들꽃 꽃다발 별로 (금방 시듦)
            new GiftPreferenceEntry("CH_LUCIA", Brooch, GiftPreference.Love), // 루시아 : 브로치 아주 좋아함 (값나가는 물건)
            new GiftPreferenceEntry("CH_LUCIA", StarMap, GiftPreference.Like), // 루시아 : 별자리 지도 좋아함 (길잡이)
            new GiftPreferenceEntry("CH_LUCIA", Sweets, GiftPreference.Dislike), // 루시아 : 달콤한 과자 별로
            new GiftPreferenceEntry("CH_PYRA", Sweets, GiftPreference.Love), // 파이라 : 달콤한 과자 아주 좋아함 (먹성)
            new GiftPreferenceEntry("CH_PYRA", Brooch, GiftPreference.Like), // 파이라 : 기사단 브로치 좋아함
            new GiftPreferenceEntry("CH_PYRA", Shell, GiftPreference.Dislike), // 파이라 : 정령의 조개 별로 (불 속성)
            new GiftPreferenceEntry("CH_TYRIA", Flower, GiftPreference.Love), // 티리아 : 들꽃 꽃다발 아주 좋아함 (잃은 마을의 꽃)
            new GiftPreferenceEntry("CH_TYRIA", Candle, GiftPreference.Like), // 티리아 : 성수 향초 좋아함 (추모)
            new GiftPreferenceEntry("CH_TYRIA", Sweets, GiftPreference.Dislike), // 티리아 : 달콤한 과자 별로
            new GiftPreferenceEntry("CH_MERCIA", Candle, GiftPreference.Love), // 메르시아 : 성수 향초 아주 좋아함 (수행)
            new GiftPreferenceEntry("CH_MERCIA", Sweets, GiftPreference.Like), // 메르시아 : 달콤한 과자 좋아함 (처음 먹어 봄)
            new GiftPreferenceEntry("CH_MERCIA", Brooch, GiftPreference.Dislike), // 메르시아 : 브로치 별로 (사치)
            new GiftPreferenceEntry("CH_NOEL", StarMap, GiftPreference.Love), // 노엘 : 별자리 지도 아주 좋아함 (탐험가)
            new GiftPreferenceEntry("CH_NOEL", Shell, GiftPreference.Like), // 노엘 : 정령의 조개 좋아함 (수집)
            new GiftPreferenceEntry("CH_NOEL", Candle, GiftPreference.Dislike), // 노엘 : 성수 향초 별로
            new GiftPreferenceEntry("CH_SEPHIRA", Candle, GiftPreference.Love), // 세피라 : 성수 향초 아주 좋아함 (기도)
            new GiftPreferenceEntry("CH_SEPHIRA", Flower, GiftPreference.Like), // 세피라 : 들꽃 꽃다발 좋아함 (처음 받아 본 선물)
            new GiftPreferenceEntry("CH_SEPHIRA", StarMap, GiftPreference.Dislike) // 세피라 : 별자리 지도 별로 (신의 뜻 밖의 길)
        };

        public static IReadOnlyList<string> AllGiftIds => GiftIds; // 선물 ID 목록 반환
        public static IReadOnlyList<GiftPreferenceEntry> AllEntries => Entries; // 취향 표 반환

        public static GiftPreference GetPreference(string characterId, string itemId) // 캐릭터·선물 취향 조회
        {
            for (int index = 0; index < Entries.Length; index++) // 취향 표 순회
            {
                GiftPreferenceEntry entry = Entries[index]; // 항목 조회

                if (string.Equals(entry.CharacterId, characterId, StringComparison.Ordinal) && string.Equals(entry.ItemId, itemId, StringComparison.Ordinal)) // 조합 일치 확인
                {
                    return entry.Preference; // 표의 취향 반환
                }
            }

            return GiftPreference.Normal; // 표에 없는 조합은 보통
        }

        public static int GetAffinityGain(GiftPreference preference) // 취향별 호감도 증가량 반환
        {
            switch (preference) // 취향 분기
            {
                case GiftPreference.Love: // 아주 좋아함 처리
                    return 15; // +15
                case GiftPreference.Like: // 좋아함 처리
                    return 8; // +8
                case GiftPreference.Dislike: // 별로 처리
                    return 1; // +1
                default: // 보통 처리
                    return 4; // +4
            }
        }

        public static string GetLabel(GiftPreference preference) // 취향 한글 라벨 반환
        {
            switch (preference) // 취향 분기
            {
                case GiftPreference.Love: // 아주 좋아함 처리
                    return "아주 좋아함"; // 아주 좋아함 라벨 반환
                case GiftPreference.Like: // 좋아함 처리
                    return "좋아함"; // 좋아함 라벨 반환
                case GiftPreference.Dislike: // 별로 처리
                    return "별로"; // 별로 라벨 반환
                default: // 보통 처리
                    return "보통"; // 보통 라벨 반환
            }
        }

        public static bool IsGiftId(string itemId) // 선물 목록 포함 여부 확인
        {
            return Array.IndexOf(GiftIds, itemId) >= 0; // 선물 ID 포함 여부 반환
        }
    }
}
