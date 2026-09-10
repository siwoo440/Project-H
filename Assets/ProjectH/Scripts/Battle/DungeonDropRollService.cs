using System; // 난수 기능
using System.Collections.Generic; // 목록 및 사전 자료형
using ProjectH.Data; // 던전 드롭 데이터 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class DungeonDropRollService // 던전 드롭 확률 계산 기능
    {
        public static List<DungeonDropResult> Roll(IReadOnlyList<DungeonDropEntry> entries, string seedSource) // 지정 드롭 테이블 계산
        {
            List<DungeonDropResult> results = new List<DungeonDropResult>(); // 최종 드롭 결과 생성

            if (entries == null || entries.Count == 0) // 드롭 테이블 존재 확인
            {
                return results; // 빈 드롭 결과 반환
            }

            Random random = new Random(CreateStableSeed(seedSource)); // 결과 ID 기반 결정적 난수 생성
            Dictionary<string, int> quantities = new Dictionary<string, int>(StringComparer.Ordinal); // 동일 아이템 수량 합산 저장소 생성
            List<string> order = new List<string>(); // 최초 드롭 순서 저장소 생성

            for (int index = 0; index < entries.Count; index++) // 드롭 항목 순회
            {
                DungeonDropEntry entry = entries[index]; // 현재 드롭 항목 조회

                if (entry == null || string.IsNullOrWhiteSpace(entry.ItemId)) // 드롭 항목 유효성 확인
                {
                    continue; // 잘못된 드롭 항목 제외
                }

                double roll = random.NextDouble(); // 현재 드롭 확률 난수 생성

                if (roll >= entry.DropChance) // 드롭 성공 여부 확인
                {
                    continue; // 드롭 실패 항목 제외
                }

                int minimum = Math.Max(1, entry.MinQuantity); // 최소 드롭 수량 보정
                int maximum = Math.Max(minimum, entry.MaxQuantity); // 최대 드롭 수량 보정
                int quantity = random.Next(minimum, maximum + 1); // 실제 드롭 수량 계산

                if (!quantities.ContainsKey(entry.ItemId)) // 최초 동일 아이템 드롭 여부 확인
                {
                    quantities.Add(entry.ItemId, 0); // 동일 아이템 합산 항목 생성
                    order.Add(entry.ItemId); // 최초 드롭 순서 저장
                }

                quantities[entry.ItemId] += quantity; // 동일 아이템 드롭 수량 합산
            }

            for (int index = 0; index < order.Count; index++) // 합산 드롭 순서 순회
            {
                string itemId = order[index]; // 현재 합산 아이템 ID 조회
                results.Add(new DungeonDropResult(itemId, quantities[itemId])); // 최종 드롭 결과 추가
            }

            return results; // 완성 드롭 결과 반환
        }

        private static int CreateStableSeed(string value) // 문자열 기반 안정 난수 시드 생성
        {
            unchecked // 정수 오버플로 허용
            {
                uint hash = 2166136261u; // FNV-1a 초기 해시 설정
                string source = value ?? string.Empty; // null 문자열 보정

                for (int index = 0; index < source.Length; index++) // 시드 문자열 문자 순회
                {
                    hash ^= source[index]; // 현재 문자 해시에 반영
                    hash *= 16777619u; // FNV-1a 소수 곱 적용
                }

                return (int)hash; // 안정 시드 정수 반환
            }
        }
    }
}
