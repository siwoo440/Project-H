using System.Collections.Generic; // 목록 자료형

namespace ProjectH.Dungeon // 프로젝트 던전 탐험 영역 (Day55)
{
    public enum DungeonNodeKind // 던전 지도 노드 종류 (Day55 신규)
    {
        Battle = 0, // 일반 전투
        Elite = 1, // 정예 전투
        Event = 2, // 선택지 이벤트
        Treasure = 3, // 보물
        Trap = 4, // 함정 (다음 전투 불리 효과)
        Rest = 5, // 휴식 (다음 전투 유리 효과)
        Boss = 6 // 보스 전투 (마지막 층)
    }

    public sealed class DungeonMapNode // 던전 지도 단일 노드 (Day55 신규)
    {
        private readonly List<int> nextNodeIds = new List<int>(); // 다음 층 연결 노드 ID 목록

        public int Id { get; } // 노드 고유 ID (지도 내 순번)
        public int Floor { get; } // 층 번호 (0부터 시작, 왼쪽→오른쪽)
        public int Lane { get; } // 층 내 세로 위치 (0부터 시작, 위→아래)
        public int LaneCount { get; } // 같은 층 노드 수
        public DungeonNodeKind Kind { get; } // 노드 종류
        public IReadOnlyList<int> NextNodeIds => nextNodeIds; // 다음 층 연결 노드 ID 목록 반환
        public bool IsBattle => Kind == DungeonNodeKind.Battle || Kind == DungeonNodeKind.Elite || Kind == DungeonNodeKind.Boss; // 전투 씬 진입 노드 여부 반환

        public DungeonMapNode(int id, int floor, int lane, int laneCount, DungeonNodeKind kind) // 던전 지도 노드 생성
        {
            Id = id; // 노드 ID 저장
            Floor = floor; // 층 번호 저장
            Lane = lane; // 층 내 위치 저장
            LaneCount = laneCount; // 같은 층 노드 수 저장
            Kind = kind; // 노드 종류 저장
        }

        internal void AddNext(int nodeId) // 다음 층 노드 연결 추가 (생성기 전용)
        {
            if (!nextNodeIds.Contains(nodeId)) // 중복 연결 확인
            {
                nextNodeIds.Add(nodeId); // 연결 추가
            }
        }

        public bool ConnectsTo(int nodeId) => nextNodeIds.Contains(nodeId); // 다음 층 특정 노드 연결 여부 반환

        public float NormalizedLane => LaneCount <= 0 ? 0.5f : (Lane + 0.5f) / LaneCount; // 층 내 세로 위치 비율 반환 (0~1)
    }

    public sealed class DungeonMap // 던전 지도 전체 (Day55 신규)
    {
        private readonly List<DungeonMapNode> nodes = new List<DungeonMapNode>(); // 전체 노드 목록 (ID 순)

        public int Seed { get; } // 생성 시드
        public int FloorCount { get; } // 전체 층 수 (보스 층 포함)
        public IReadOnlyList<DungeonMapNode> Nodes => nodes; // 전체 노드 목록 반환

        public DungeonMap(int seed, int floorCount) // 던전 지도 생성
        {
            Seed = seed; // 시드 저장
            FloorCount = floorCount; // 층 수 저장
        }

        internal DungeonMapNode AddNode(int floor, int lane, int laneCount, DungeonNodeKind kind) // 노드 추가 (생성기 전용)
        {
            DungeonMapNode node = new DungeonMapNode(nodes.Count, floor, lane, laneCount, kind); // 순번 ID 노드 생성
            nodes.Add(node); // 노드 목록 등록
            return node; // 생성 노드 반환
        }

        public DungeonMapNode GetNode(int nodeId) // ID 기반 노드 조회
        {
            return nodeId >= 0 && nodeId < nodes.Count ? nodes[nodeId] : null; // 범위 내 노드 반환
        }

        public List<DungeonMapNode> GetFloor(int floor) // 층 기반 노드 목록 조회
        {
            List<DungeonMapNode> result = new List<DungeonMapNode>(); // 층 노드 목록 생성

            for (int index = 0; index < nodes.Count; index++) // 전체 노드 순회
            {
                if (nodes[index].Floor == floor) // 층 일치 확인
                {
                    result.Add(nodes[index]); // 층 노드 추가
                }
            }

            return result; // 층 노드 목록 반환
        }

        public DungeonMapNode BossNode // 보스 노드 반환
        {
            get
            {
                for (int index = nodes.Count - 1; index >= 0; index--) // 뒤에서부터 순회
                {
                    if (nodes[index].Kind == DungeonNodeKind.Boss) // 보스 노드 확인
                    {
                        return nodes[index]; // 보스 노드 반환
                    }
                }

                return null; // 보스 노드 없음 반환
            }
        }
    }
}
