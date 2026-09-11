using System; // 난수 기능
using System.Collections.Generic; // 목록 자료형

namespace ProjectH.Dungeon // 프로젝트 던전 탐험 영역 (Day55)
{
    public static class DungeonMapGenerator // 시드 기반 가로형 던전 지도 생성 기능 (Day55 신규, 순수 로직)
    {
        public const int MinFloorCount = 1; // 최소 층 수 (보스 층만 존재)
        public const int FirstFloorLaneCount = 2; // 첫 층 노드 수
        public const int MinMiddleLaneCount = 2; // 중간 층 최소 노드 수
        public const int MaxMiddleLaneCount = 3; // 중간 층 최대 노드 수
        public const int WeightBattle = 45; // 일반 전투 출현 비중
        public const int WeightEvent = 18; // 이벤트 출현 비중
        public const int WeightTreasure = 12; // 보물 출현 비중
        public const int WeightTrap = 10; // 함정 출현 비중
        public const int WeightRest = 10; // 휴식 출현 비중
        public const int WeightElite = 5; // 정예 출현 비중
        private const double ExtraEdgeChance = 0.5; // 인접 노드 추가 연결 확률

        public static DungeonMap Generate(int seed, int floorCount) // 가로형 던전 지도 생성 (층 수는 보스 층 포함)
        {
            int safeFloorCount = Math.Max(MinFloorCount, floorCount); // 층 수 하한 보정
            Random random = new Random(seed); // 시드 고정 난수 생성기
            DungeonMap map = new DungeonMap(seed, safeFloorCount); // 빈 지도 생성
            List<List<DungeonMapNode>> floors = new List<List<DungeonMapNode>>(); // 층별 노드 목록

            for (int floor = 0; floor < safeFloorCount; floor++) // 왼쪽부터 층 순회
            {
                floors.Add(CreateFloor(map, random, floor, safeFloorCount, floors)); // 층 노드 생성 및 등록
            }

            for (int floor = 0; floor < safeFloorCount - 1; floor++) // 마지막 층 제외 연결 생성
            {
                ConnectFloors(floors[floor], floors[floor + 1], random); // 인접 층 연결
            }

            return map; // 완성 지도 반환
        }

        private static List<DungeonMapNode> CreateFloor(DungeonMap map, Random random, int floor, int floorCount, List<List<DungeonMapNode>> previousFloors) // 단일 층 노드 생성
        {
            List<DungeonMapNode> created = new List<DungeonMapNode>(); // 생성 노드 목록
            bool isBossFloor = floor == floorCount - 1; // 마지막 층 여부 판정

            if (isBossFloor) // 보스 층 확인
            {
                created.Add(map.AddNode(floor, 0, 1, DungeonNodeKind.Boss)); // 보스 노드 1개 생성
                return created; // 보스 층 반환
            }

            bool isFirstFloor = floor == 0; // 첫 층 여부 판정
            int laneCount = isFirstFloor ? FirstFloorLaneCount : random.Next(MinMiddleLaneCount, MaxMiddleLaneCount + 1); // 층 노드 수 결정
            bool previousHasTrap = FloorContains(previousFloors, floor - 1, DungeonNodeKind.Trap); // 직전 층 함정 포함 여부
            bool previousHasRest = FloorContains(previousFloors, floor - 1, DungeonNodeKind.Rest); // 직전 층 휴식 포함 여부

            for (int lane = 0; lane < laneCount; lane++) // 층 내 위치 순회
            {
                DungeonNodeKind kind = isFirstFloor ? DungeonNodeKind.Battle : RollKind(random, previousHasTrap, previousHasRest); // 첫 층은 전투, 이후는 비중 추첨
                created.Add(map.AddNode(floor, lane, laneCount, kind)); // 노드 생성 및 등록
            }

            return created; // 생성 층 반환
        }

        private static bool FloorContains(List<List<DungeonMapNode>> floors, int floor, DungeonNodeKind kind) // 특정 층 노드 종류 포함 여부 확인
        {
            if (floor < 0 || floor >= floors.Count) // 층 범위 확인
            {
                return false; // 범위 밖 미포함 반환
            }

            for (int index = 0; index < floors[floor].Count; index++) // 층 노드 순회
            {
                if (floors[floor][index].Kind == kind) // 종류 일치 확인
                {
                    return true; // 포함 반환
                }
            }

            return false; // 미포함 반환
        }

        public static DungeonNodeKind RollKind(Random random, bool blockTrap, bool blockRest) // 비중 기반 노드 종류 추첨 (연속 층 함정·휴식 금지)
        {
            int trapWeight = blockTrap ? 0 : WeightTrap; // 직전 층 함정 존재 시 함정 제외
            int restWeight = blockRest ? 0 : WeightRest; // 직전 층 휴식 존재 시 휴식 제외
            int total = WeightBattle + WeightEvent + WeightTreasure + trapWeight + restWeight + WeightElite; // 전체 비중 합계
            int roll = random.Next(total); // 비중 구간 추첨

            if ((roll -= WeightBattle) < 0) return DungeonNodeKind.Battle; // 일반 전투 구간 반환
            if ((roll -= WeightEvent) < 0) return DungeonNodeKind.Event; // 이벤트 구간 반환
            if ((roll -= WeightTreasure) < 0) return DungeonNodeKind.Treasure; // 보물 구간 반환
            if ((roll -= trapWeight) < 0) return DungeonNodeKind.Trap; // 함정 구간 반환
            if ((roll -= restWeight) < 0) return DungeonNodeKind.Rest; // 휴식 구간 반환
            return DungeonNodeKind.Elite; // 정예 구간 반환
        }

        private static void ConnectFloors(List<DungeonMapNode> current, List<DungeonMapNode> next, Random random) // 인접 두 층 연결 (막다른 길·고립 노드 없음 보장)
        {
            for (int index = 0; index < current.Count; index++) // 현재 층 노드 순회
            {
                DungeonMapNode from = current[index]; // 출발 노드 조회
                int nearest = FindNearestLane(from.NormalizedLane, next); // 세로 위치가 가장 가까운 다음 층 노드 탐색
                from.AddNext(next[nearest].Id); // 기본 연결 추가 (모든 노드 출구 보장)

                if (next.Count > 1 && random.NextDouble() < ExtraEdgeChance) // 추가 연결 여부 추첨
                {
                    int neighbor = from.NormalizedLane <= next[nearest].NormalizedLane ? nearest - 1 : nearest + 1; // 출발 위치 쪽 인접 노드 선택 (교차 최소화)

                    if (neighbor < 0 || neighbor >= next.Count) // 인접 범위 확인
                    {
                        neighbor = nearest == 0 ? 1 : nearest - 1; // 반대쪽 인접 노드 대체
                    }

                    from.AddNext(next[neighbor].Id); // 추가 연결 반영
                }
            }

            for (int index = 0; index < next.Count; index++) // 다음 층 노드 순회
            {
                DungeonMapNode to = next[index]; // 도착 노드 조회

                if (HasIncoming(current, to.Id)) // 진입 연결 존재 확인
                {
                    continue; // 연결 보유 노드 제외
                }

                int nearest = FindNearestLane(to.NormalizedLane, current); // 가장 가까운 현재 층 노드 탐색
                current[nearest].AddNext(to.Id); // 고립 노드 진입 연결 보장
            }
        }

        private static int FindNearestLane(float normalizedLane, List<DungeonMapNode> candidates) // 세로 위치 기준 최근접 노드 인덱스 탐색
        {
            int best = 0; // 최근접 인덱스 초기화
            float bestDistance = float.MaxValue; // 최근접 거리 초기화

            for (int index = 0; index < candidates.Count; index++) // 후보 노드 순회
            {
                float distance = Math.Abs(candidates[index].NormalizedLane - normalizedLane); // 세로 위치 거리 계산

                if (distance < bestDistance) // 최근접 갱신 확인
                {
                    bestDistance = distance; // 최근접 거리 갱신
                    best = index; // 최근접 인덱스 갱신
                }
            }

            return best; // 최근접 인덱스 반환
        }

        private static bool HasIncoming(List<DungeonMapNode> sources, int targetId) // 진입 연결 존재 여부 확인
        {
            for (int index = 0; index < sources.Count; index++) // 출발 후보 순회
            {
                if (sources[index].ConnectsTo(targetId)) // 연결 포함 확인
                {
                    return true; // 진입 연결 존재 반환
                }
            }

            return false; // 진입 연결 없음 반환
        }
    }
}
