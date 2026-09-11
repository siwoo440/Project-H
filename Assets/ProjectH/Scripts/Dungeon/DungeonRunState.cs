using System; // 난수 기능
using System.Collections.Generic; // 목록 자료형
using ProjectH.Battle; // 전투 승패 기능

namespace ProjectH.Dungeon // 프로젝트 던전 탐험 영역 (Day55)
{
    public enum DungeonRunEndKind // 탐험 종료 종류 (Day55 신규)
    {
        None = 0, // 종료되지 않음
        Cleared = 1, // 보스 격파 클리어
        Failed = 2, // 전투 패배
        Abandoned = 3 // 탐험 포기
    }

    public static class DungeonRunState // 노드형 던전 탐험 진행 상태 (Day55 신규, 씬 전환 간 유지되는 static 상태)
    {
        public const float BattleRewardScale = 0.4f; // 일반 전투 노드 보상 배율
        public const float EliteRewardScale = 0.7f; // 정예 노드 보상 배율
        public const float BossRewardScale = 1.0f; // 보스 노드 보상 배율

        private static readonly HashSet<int> clearedNodeIds = new HashSet<int>(); // 완료 노드 ID 목록
        private static readonly List<DungeonRunModifier> pendingModifiers = new List<DungeonRunModifier>(); // 다음 전투 효과 목록
        private static Random random; // 탐험 진행 난수 생성기

        public static bool IsActive { get; private set; } // 탐험 진행 여부
        public static string DungeonId { get; private set; } = string.Empty; // 탐험 던전 ID
        public static DungeonMap Map { get; private set; } // 탐험 지도
        public static int CurrentNodeId { get; private set; } = -1; // 현재 위치 노드 ID (-1이면 입장 전)
        public static int PendingBattleNodeId { get; private set; } = -1; // 결과 대기 중인 전투 노드 ID
        public static int CollectedGold { get; private set; } // 이번 탐험 획득 골드 합계
        public static DungeonRunEndKind LastEndKind { get; private set; } // 마지막 종료 종류 (종료 요약 표시용)
        public static IReadOnlyList<DungeonRunModifier> PendingModifiers => pendingModifiers; // 다음 전투 효과 목록 반환
        public static bool HasPendingBattle => IsActive && PendingBattleNodeId >= 0; // 전투 결과 대기 여부 반환
        public static DungeonMapNode PendingBattleNode => HasPendingBattle ? Map.GetNode(PendingBattleNodeId) : null; // 결과 대기 전투 노드 반환

        public static void Begin(string dungeonId, int floorCount, int seed) // 신규 탐험 시작
        {
            ResetAll(); // 이전 탐험 상태 초기화
            IsActive = true; // 탐험 진행 상태 적용
            DungeonId = dungeonId ?? string.Empty; // 던전 ID 저장
            Map = DungeonMapGenerator.Generate(seed, floorCount); // 가로형 지도 생성
            random = new Random(seed ^ 0x5F3759DF); // 지도와 분리된 진행 난수 생성기 생성
        }

        public static void ResetAll() // 탐험 상태 전체 초기화
        {
            IsActive = false; // 탐험 진행 해제
            DungeonId = string.Empty; // 던전 ID 초기화
            Map = null; // 지도 해제
            CurrentNodeId = -1; // 현재 위치 초기화
            PendingBattleNodeId = -1; // 전투 대기 초기화
            CollectedGold = 0; // 획득 골드 초기화
            LastEndKind = DungeonRunEndKind.None; // 종료 종류 초기화
            clearedNodeIds.Clear(); // 완료 노드 초기화
            pendingModifiers.Clear(); // 다음 전투 효과 초기화
            random = null; // 난수 생성기 해제
        }

        public static bool IsCleared(int nodeId) // 노드 완료 여부 확인
        {
            return clearedNodeIds.Contains(nodeId); // 완료 여부 반환
        }

        public static List<DungeonMapNode> GetSelectableNodes() // 현재 선택 가능한 노드 목록 조회
        {
            List<DungeonMapNode> result = new List<DungeonMapNode>(); // 선택 가능 노드 목록 생성

            if (!IsActive || Map == null) // 탐험 진행 확인
            {
                return result; // 빈 목록 반환
            }

            if (PendingBattleNodeId >= 0) // 결과 없이 돌아온 전투 확인 (전투 중 이탈)
            {
                result.Add(Map.GetNode(PendingBattleNodeId)); // 해당 전투만 다시 선택 가능 (건너뛰기 방지)
                return result; // 재도전 전용 목록 반환
            }

            if (CurrentNodeId < 0) // 입장 직후 확인
            {
                return Map.GetFloor(0); // 첫 층 전체 반환
            }

            DungeonMapNode current = Map.GetNode(CurrentNodeId); // 현재 노드 조회

            for (int index = 0; current != null && index < current.NextNodeIds.Count; index++) // 연결 노드 순회
            {
                result.Add(Map.GetNode(current.NextNodeIds[index])); // 다음 층 연결 노드 추가
            }

            return result; // 선택 가능 노드 목록 반환
        }

        public static bool CanSelect(int nodeId) // 노드 선택 가능 여부 확인
        {
            List<DungeonMapNode> selectable = GetSelectableNodes(); // 선택 가능 노드 조회

            for (int index = 0; index < selectable.Count; index++) // 선택 가능 노드 순회
            {
                if (selectable[index].Id == nodeId) // ID 일치 확인
                {
                    return true; // 선택 가능 반환
                }
            }

            return false; // 선택 불가 반환
        }

        public static DungeonMapNode SelectNode(int nodeId) // 노드 선택 및 이동 (전투 노드는 결과 대기, 그 외는 즉시 완료)
        {
            if (!CanSelect(nodeId)) // 선택 가능 여부 확인
            {
                return null; // 선택 실패 반환
            }

            DungeonMapNode node = Map.GetNode(nodeId); // 선택 노드 조회
            CurrentNodeId = nodeId; // 현재 위치 이동

            if (node.IsBattle) // 전투 노드 확인
            {
                PendingBattleNodeId = nodeId; // 전투 결과 대기 설정
            }
            else // 비전투 노드 처리
            {
                clearedNodeIds.Add(nodeId); // 즉시 완료 처리
            }

            return node; // 선택 노드 반환
        }

        public static List<string[]> ResolveBattleWaves(List<string[]> allWaves) // 대기 전투 노드 종류 기반 웨이브 구성
        {
            if (!HasPendingBattle || allWaves == null || allWaves.Count == 0) // 노드 전투 및 웨이브 존재 확인
            {
                return allWaves; // 탐험 전투가 아니면 원본 그대로 반환
            }

            DungeonMapNode node = PendingBattleNode; // 대기 전투 노드 조회

            if (node.Kind == DungeonNodeKind.Boss || allWaves.Count == 1) // 보스 또는 단일 웨이브 확인
            {
                return allWaves; // 전체 웨이브 반환
            }

            int regularCount = allWaves.Count - 1; // 마지막(보스급) 웨이브 제외 일반 웨이브 수
            int seedOffset = ((Map.Seed % regularCount) + regularCount) % regularCount; // 음수 시드 대비 양수 나머지 계산 (정수 오버플로 방지)
            int start = (seedOffset + node.Id) % regularCount; // 노드별 고정 시작 웨이브 선택
            int take = node.Kind == DungeonNodeKind.Elite ? Math.Min(2, regularCount) : 1; // 정예 2개, 일반 1개
            List<string[]> result = new List<string[]>(); // 구성 웨이브 목록 생성

            for (int index = 0; index < take; index++) // 필요 웨이브 수 순회
            {
                result.Add(allWaves[(start + index) % regularCount]); // 일반 웨이브 순환 추가
            }

            return result; // 노드 웨이브 반환
        }

        public static float GetPendingRewardScale() // 대기 전투 보상 배율 조회
        {
            if (!HasPendingBattle) // 노드 전투 여부 확인
            {
                return 1f; // 탐험 외 전투 기존 보상 반환
            }

            switch (PendingBattleNode.Kind) // 노드 종류 분기
            {
                case DungeonNodeKind.Elite: // 정예 처리
                    return EliteRewardScale; // 정예 배율 반환
                case DungeonNodeKind.Boss: // 보스 처리
                    return BossRewardScale; // 보스 배율 반환
                default: // 일반 전투 처리
                    return BattleRewardScale; // 일반 배율 반환
            }
        }

        public static bool PendingBattleCountsAsClear => !HasPendingBattle || PendingBattleNode.Kind == DungeonNodeKind.Boss; // 던전 클리어 기록 여부 (탐험 외 전투 또는 보스)
        public static bool PendingBattleGrantsDrops => !HasPendingBattle || PendingBattleNode.Kind != DungeonNodeKind.Battle; // 드롭 지급 여부 (일반 전투 노드만 제외)

        public static void ReportBattleOutcome(BattleOutcome outcome, int gainedGold) // 전투 결과 반영
        {
            if (!HasPendingBattle) // 노드 전투 여부 확인
            {
                return; // 탐험 외 전투 무시
            }

            DungeonMapNode node = PendingBattleNode; // 대기 전투 노드 조회
            PendingBattleNodeId = -1; // 전투 대기 해제

            if (outcome != BattleOutcome.Victory) // 패배 확인
            {
                End(DungeonRunEndKind.Failed); // 탐험 실패 종료
                return; // 결과 반영 종료
            }

            clearedNodeIds.Add(node.Id); // 전투 노드 완료 처리
            CollectedGold += Math.Max(0, gainedGold); // 획득 골드 누적

            if (node.Kind == DungeonNodeKind.Boss) // 보스 격파 확인
            {
                End(DungeonRunEndKind.Cleared); // 탐험 클리어 종료
            }
        }

        public static void AddGold(int amount) // 비전투 노드 골드 변동 누적 (결과 요약용)
        {
            CollectedGold += amount; // 골드 변동 누적
        }

        public static void AddPendingModifier(DungeonRunModifier modifier) // 다음 전투 효과 추가
        {
            if (modifier != null) // 효과 유효성 확인
            {
                pendingModifiers.Add(modifier); // 다음 전투 효과 등록
            }
        }

        public static List<DungeonRunModifier> ConsumePendingModifiers() // 다음 전투 효과 꺼내기 (전투 시작 시 1회)
        {
            List<DungeonRunModifier> consumed = new List<DungeonRunModifier>(pendingModifiers); // 효과 복사
            pendingModifiers.Clear(); // 효과 목록 비움
            return consumed; // 꺼낸 효과 반환
        }

        public static Random GetRandom() // 탐험 진행 난수 생성기 조회
        {
            return random ?? (random = new Random()); // 없으면 임시 생성 후 반환
        }

        public static void Abandon() // 탐험 포기
        {
            End(DungeonRunEndKind.Abandoned); // 포기 종료
        }

        private static void End(DungeonRunEndKind endKind) // 탐험 종료 공통 처리 (요약 표시를 위해 종료 종류·획득 골드 유지)
        {
            IsActive = false; // 탐험 진행 해제
            PendingBattleNodeId = -1; // 전투 대기 해제
            pendingModifiers.Clear(); // 남은 다음 전투 효과 폐기
            LastEndKind = endKind; // 종료 종류 기록
        }

        public static DungeonRunEndKind ConsumeEndSummary() // 종료 요약 1회 조회 후 초기화
        {
            DungeonRunEndKind kind = LastEndKind; // 종료 종류 조회
            LastEndKind = DungeonRunEndKind.None; // 요약 표시 완료 처리
            return kind; // 종료 종류 반환
        }
    }
}
