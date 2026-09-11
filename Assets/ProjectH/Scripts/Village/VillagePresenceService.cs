using System; // 난수 기능
using System.Collections.Generic; // 목록 자료형
using ProjectH.SaveSystem; // 저장·시간 기능

namespace ProjectH.Village // 프로젝트 마을 영역
{
    public static class VillagePresenceService // 시간대별 캐릭터 무작위 배치 (Day62 신규 — 같은 일차·시간대면 항상 같은 배치)
    {
        public const int AbsentWeight = 2; // "외출 중" 가중치 (구역 가중치 합과 비교)

        private static readonly Dictionary<string, int[]> Weights = new Dictionary<string, int[]>(StringComparer.Ordinal) // 캐릭터별 구역 선호 {광장, 시장, 온천, 여관, 길드} (12인 — 64일차 합류 캐릭터도 미리 등록)
        {
            { "CH_SERENA", new[] { 5, 2, 2, 4, 1 } }, // 세레나 : 광장 산책·여관 기도
            { "CH_ELLEN", new[] { 2, 1, 2, 2, 5 } }, // 엘렌 : 길드 훈련장
            { "CH_LILIA", new[] { 1, 5, 1, 4, 2 } }, // 릴리아 : 시장 재료·여관 연구
            { "CH_EVE", new[] { 4, 1, 5, 1, 1 } }, // 이브 : 온천·광장 나무 그늘
            { "CH_NATASHA", new[] { 1, 3, 1, 4, 3 } }, // 나타샤 : 여관 구석·길드 정보
            { "CH_CLAIRE", new[] { 1, 5, 2, 2, 2 } }, // 클레어 : 시장 부품
            { "CH_LUCIA", new[] { 1, 2, 1, 3, 5 } }, // 루시아 : 길드 의뢰
            { "CH_PYRA", new[] { 3, 1, 2, 2, 4 } }, // 파이라 : 길드·광장
            { "CH_TYRIA", new[] { 4, 2, 2, 2, 3 } }, // 티리아 : 광장 순찰
            { "CH_MERCIA", new[] { 3, 1, 4, 2, 1 } }, // 메르시아 : 온천 명상
            { "CH_NOEL", new[] { 2, 4, 1, 2, 3 } }, // 노엘 : 시장 구경
            { "CH_SEPHIRA", new[] { 4, 1, 3, 3, 1 } } // 세피라 : 광장 설교
        };

        private static readonly int[] DefaultWeights = { 3, 2, 2, 2, 2 }; // 등록되지 않은 캐릭터

        public static int[] GetWeights(string characterId) => Weights.TryGetValue(characterId ?? string.Empty, out int[] weights) ? weights : DefaultWeights; // 선호 가중치 조회

        public static VillageZone? GetZone(SaveData saveData, string characterId) // 현재 시간대의 캐릭터 위치 (null = 외출 중)
        {
            if (saveData == null || saveData.FindCharacter(characterId) == null) return null; // 보유하지 않은 캐릭터
            return PickZone(characterId, GameTimeService.GetCurrentDay(saveData), GameTimeService.GetCurrentPhase(saveData)); // 일차·시간대 배치
        }

        public static VillageZone? PickZone(string characterId, int day, SaveTimeOfDay phase) // 일차·시간대 기반 결정적 배치
        {
            int[] weights = GetWeights(characterId); // 선호 가중치
            int total = AbsentWeight; // 가중치 합 (외출 포함)

            for (int index = 0; index < weights.Length; index++) // 구역 순회
            {
                if (VillageZoneCatalog.IsOpen((VillageZone)index, phase)) total += weights[index]; // 열린 구역만 합산
            }

            Random random = new Random(StableHash($"{characterId}|{day}|{(int)phase}")); // 결정적 난수 (키 전체를 해시해 이웃 일차끼리 값이 비슷해지지 않게)
            int roll = random.Next(total); // 뽑기

            for (int index = 0; index < weights.Length; index++) // 구역 순회
            {
                if (!VillageZoneCatalog.IsOpen((VillageZone)index, phase)) continue; // 닫힌 구역 제외
                if (roll < weights[index]) return (VillageZone)index; // 당첨 구역
                roll -= weights[index]; // 다음 구역으로
            }

            return null; // 외출 중
        }

        public static List<string> GetCharactersIn(SaveData saveData, VillageZone zone) // 구역에 있는 보유 캐릭터 목록 (저장 순서)
        {
            List<string> result = new List<string>(); // 결과
            if (saveData == null) return result; // 저장 없음

            foreach (CharacterSaveData character in saveData.Characters) // 보유 캐릭터 순회
            {
                if (character != null && GetZone(saveData, character.CharacterId) == zone) result.Add(character.CharacterId); // 같은 구역
            }

            return result; // 결과 반환
        }

        private static int StableHash(string value) // 실행 환경과 무관한 문자열 해시 (FNV-1a)
        {
            unchecked // 오버플로 허용
            {
                int hash = (int)2166136261; // 초기값
                foreach (char c in value ?? string.Empty) hash = (hash ^ c) * 16777619; // 문자 누적
                return hash & 0x7FFFFFFF; // 양수 반환
            }
        }
    }
}
