using System.Collections.Generic; // 목록 자료형
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 승패 기능
using ProjectH.Dungeon; // 노드형 던전 탐험 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class DungeonMapTests // Day55 노드형 던전 탐험 회귀 테스트
    {
        [SetUp] // 각 테스트 시작 전 처리
        public void SetUp() // 탐험 상태 초기화
        {
            DungeonRunState.ResetAll(); // 이전 테스트 잔여 상태 제거
        }

        [TearDown] // 각 테스트 종료 후 처리
        public void TearDown() // 탐험 상태 정리
        {
            DungeonRunState.ResetAll(); // 다음 테스트 영향 방지
        }

        private static List<string[]> CreateWaves(int count) // 테스트용 웨이브 목록 생성
        {
            List<string[]> waves = new List<string[]>(); // 웨이브 목록 생성

            for (int index = 0; index < count; index++) // 웨이브 수 순회
            {
                waves.Add(new[] { $"W{index}" }); // 식별 가능한 웨이브 추가
            }

            return waves; // 웨이브 목록 반환
        }

        private static DungeonMapNode FindFirst(DungeonMap map, DungeonNodeKind kind) // 특정 종류 첫 노드 조회
        {
            for (int index = 0; index < map.Nodes.Count; index++) // 전체 노드 순회
            {
                if (map.Nodes[index].Kind == kind) // 종류 일치 확인
                {
                    return map.Nodes[index]; // 노드 반환
                }
            }

            return null; // 없음 반환
        }

        [Test] // 같은 시드 동일 지도 검증
        public void Generate_SameSeed_ProducesSameMap() // 결정론 테스트
        {
            DungeonMap first = DungeonMapGenerator.Generate(1234, 4); // 첫 지도 생성
            DungeonMap second = DungeonMapGenerator.Generate(1234, 4); // 같은 시드 두 번째 지도 생성

            Assert.That(second.Nodes.Count, Is.EqualTo(first.Nodes.Count)); // 노드 수 일치 검증

            for (int index = 0; index < first.Nodes.Count; index++) // 노드 순회
            {
                Assert.That(second.Nodes[index].Kind, Is.EqualTo(first.Nodes[index].Kind)); // 노드 종류 일치 검증
                Assert.That(second.Nodes[index].NextNodeIds, Is.EqualTo(first.Nodes[index].NextNodeIds)); // 연결 일치 검증
            }
        }

        [Test] // 1층 던전은 보스 노드 하나 검증 (DG001)
        public void Generate_SingleFloor_IsBossOnly() // 1층 지도 테스트
        {
            DungeonMap map = DungeonMapGenerator.Generate(7, 1); // 1층 지도 생성

            Assert.That(map.FloorCount, Is.EqualTo(1)); // 층 수 검증
            Assert.That(map.Nodes.Count, Is.EqualTo(1)); // 노드 1개 검증
            Assert.That(map.Nodes[0].Kind, Is.EqualTo(DungeonNodeKind.Boss)); // 보스 노드 검증
        }

        [Test] // 층 수 1·2·3·4 구조 검증 (DG001~DG004)
        public void Generate_FloorCounts_HaveBattleStartAndBossEnd([Values(2, 3, 4, 5, 6)] int floorCount) // 층 구조 테스트 (DG001~DG004 = 3·4·5·6층 포함)
        {
            for (int seed = 0; seed < 30; seed++) // 여러 시드 반복
            {
                DungeonMap map = DungeonMapGenerator.Generate(seed, floorCount); // 지도 생성
                List<DungeonMapNode> firstFloor = map.GetFloor(0); // 첫 층 조회
                List<DungeonMapNode> lastFloor = map.GetFloor(floorCount - 1); // 마지막 층 조회

                Assert.That(firstFloor.Count, Is.EqualTo(DungeonMapGenerator.FirstFloorLaneCount)); // 첫 층 노드 수 검증
                Assert.That(firstFloor.TrueForAll(node => node.Kind == DungeonNodeKind.Battle), Is.True); // 첫 층 전부 전투 검증
                Assert.That(lastFloor.Count, Is.EqualTo(1)); // 마지막 층 노드 1개 검증
                Assert.That(lastFloor[0].Kind, Is.EqualTo(DungeonNodeKind.Boss)); // 마지막 층 보스 검증
                Assert.That(map.GetFloor(floorCount).Count, Is.EqualTo(0)); // 지정 층 수 초과 층 없음 검증
            }
        }

        [Test] // 던전 층수 3·4·5·6 노드 수 범위 검증 (Day55 노드 추가 요청)
        public void Generate_DungeonFloorCounts_HaveExpectedNodeRange([Values(3, 4, 5, 6)] int floorCount) // 노드 수 범위 테스트
        {
            int minNodes = DungeonMapGenerator.FirstFloorLaneCount + (DungeonMapGenerator.MinMiddleLaneCount * (floorCount - 2)) + 1; // 최소 노드 수 (첫 층 2 + 중간 층 최소 + 보스 1)
            int maxNodes = DungeonMapGenerator.FirstFloorLaneCount + (DungeonMapGenerator.MaxMiddleLaneCount * (floorCount - 2)) + 1; // 최대 노드 수 (첫 층 2 + 중간 층 최대 + 보스 1)

            for (int seed = 0; seed < 50; seed++) // 여러 시드 반복
            {
                int count = DungeonMapGenerator.Generate(seed, floorCount).Nodes.Count; // 노드 수 조회
                Assert.That(count, Is.InRange(minNodes, maxNodes), $"seed={seed}"); // 노드 수 범위 검증
            }
        }

        [Test] // 연결은 바로 다음 층으로만 향하는지 검증
        public void Generate_EdgesOnlyPointToNextFloor() // 연결 방향 테스트
        {
            for (int seed = 0; seed < 50; seed++) // 여러 시드 반복
            {
                DungeonMap map = DungeonMapGenerator.Generate(seed, 6); // 긴 지도 생성

                foreach (DungeonMapNode node in map.Nodes) // 전체 노드 순회
                {
                    foreach (int nextId in node.NextNodeIds) // 연결 순회
                    {
                        Assert.That(map.GetNode(nextId).Floor, Is.EqualTo(node.Floor + 1)); // 다음 층 연결 검증 (왼쪽→오른쪽)
                    }
                }
            }
        }

        [Test] // 모든 노드에서 보스 도달 가능 검증 (막다른 길 없음)
        public void Generate_EveryNodeCanReachBoss() // 경로 보장 테스트
        {
            for (int seed = 0; seed < 50; seed++) // 여러 시드 반복
            {
                DungeonMap map = DungeonMapGenerator.Generate(seed, 6); // 긴 지도 생성
                int bossId = map.BossNode.Id; // 보스 노드 ID 조회

                foreach (DungeonMapNode start in map.Nodes) // 모든 출발 노드 순회
                {
                    Queue<int> queue = new Queue<int>(); // 탐색 큐 생성
                    HashSet<int> visited = new HashSet<int>(); // 방문 목록 생성
                    queue.Enqueue(start.Id); // 출발 노드 등록
                    bool reached = false; // 도달 여부 초기화

                    while (queue.Count > 0) // 너비 우선 탐색
                    {
                        int current = queue.Dequeue(); // 현재 노드 꺼내기

                        if (current == bossId) // 보스 도달 확인
                        {
                            reached = true; // 도달 기록
                            break; // 탐색 종료
                        }

                        foreach (int next in map.GetNode(current).NextNodeIds) // 연결 순회
                        {
                            if (visited.Add(next)) // 미방문 확인
                            {
                                queue.Enqueue(next); // 탐색 등록
                            }
                        }
                    }

                    Assert.That(reached, Is.True, $"seed={seed}, node={start.Id}"); // 보스 도달 검증
                }
            }
        }

        [Test] // 첫 층 이후 모든 노드가 진입 연결을 가지는지 검증 (고립 노드 없음)
        public void Generate_EveryLaterNodeHasIncomingEdge() // 고립 방지 테스트
        {
            for (int seed = 0; seed < 50; seed++) // 여러 시드 반복
            {
                DungeonMap map = DungeonMapGenerator.Generate(seed, 6); // 긴 지도 생성

                foreach (DungeonMapNode node in map.Nodes) // 전체 노드 순회
                {
                    if (node.Floor == 0) // 첫 층 확인
                    {
                        continue; // 첫 층 제외
                    }

                    bool hasIncoming = map.GetFloor(node.Floor - 1).Exists(previous => previous.ConnectsTo(node.Id)); // 이전 층 진입 연결 확인
                    Assert.That(hasIncoming, Is.True, $"seed={seed}, node={node.Id}"); // 진입 연결 검증
                }
            }
        }

        [Test] // 함정·휴식 연속 층 금지 검증
        public void Generate_TrapAndRest_NotOnConsecutiveFloors() // 연속 배치 금지 테스트
        {
            for (int seed = 0; seed < 100; seed++) // 여러 시드 반복
            {
                DungeonMap map = DungeonMapGenerator.Generate(seed, 10); // 긴 지도 생성

                for (int floor = 1; floor < map.FloorCount; floor++) // 두 번째 층부터 순회
                {
                    List<DungeonMapNode> previous = map.GetFloor(floor - 1); // 이전 층 조회
                    List<DungeonMapNode> current = map.GetFloor(floor); // 현재 층 조회
                    bool previousTrap = previous.Exists(node => node.Kind == DungeonNodeKind.Trap); // 이전 층 함정 여부
                    bool previousRest = previous.Exists(node => node.Kind == DungeonNodeKind.Rest); // 이전 층 휴식 여부

                    Assert.That(previousTrap && current.Exists(node => node.Kind == DungeonNodeKind.Trap), Is.False, $"seed={seed}, floor={floor}"); // 함정 연속 금지 검증
                    Assert.That(previousRest && current.Exists(node => node.Kind == DungeonNodeKind.Rest), Is.False, $"seed={seed}, floor={floor}"); // 휴식 연속 금지 검증
                }
            }
        }

        [Test] // 노드 종류 비중 검증
        public void RollKind_DistributionMatchesWeights() // 비중 분포 테스트
        {
            System.Random random = new System.Random(99); // 고정 시드 난수 생성기
            Dictionary<DungeonNodeKind, int> counts = new Dictionary<DungeonNodeKind, int>(); // 종류별 횟수
            const int samples = 20000; // 표본 수

            for (int index = 0; index < samples; index++) // 표본 추출
            {
                DungeonNodeKind kind = DungeonMapGenerator.RollKind(random, false, false); // 제한 없는 추첨
                counts[kind] = counts.TryGetValue(kind, out int count) ? count + 1 : 1; // 횟수 누적
            }

            Assert.That(counts[DungeonNodeKind.Battle] / (float)samples, Is.EqualTo(0.45f).Within(0.02f)); // 전투 45% 검증
            Assert.That(counts[DungeonNodeKind.Event] / (float)samples, Is.EqualTo(0.18f).Within(0.02f)); // 이벤트 18% 검증
            Assert.That(counts[DungeonNodeKind.Elite] / (float)samples, Is.EqualTo(0.05f).Within(0.015f)); // 정예 5% 검증
            Assert.That(counts.ContainsKey(DungeonNodeKind.Boss), Is.False); // 중간 층 보스 미출현 검증
        }

        [Test] // 탐험 시작 시 첫 층만 선택 가능 검증
        public void Run_Start_OnlyFirstFloorSelectable() // 시작 선택 테스트
        {
            DungeonRunState.Begin("DG004", 4, 42); // 4층 탐험 시작
            List<DungeonMapNode> selectable = DungeonRunState.GetSelectableNodes(); // 선택 가능 노드 조회

            Assert.That(selectable.Count, Is.EqualTo(2)); // 첫 층 2개 검증
            Assert.That(selectable.TrueForAll(node => node.Floor == 0), Is.True); // 첫 층만 검증
            Assert.That(DungeonRunState.CanSelect(DungeonRunState.Map.BossNode.Id), Is.False); // 보스 즉시 선택 불가 검증
        }

        [Test] // 전투 노드 승리 후 다음 층 이동 검증
        public void Run_BattleVictory_AdvancesToNextFloor() // 전투 진행 테스트
        {
            DungeonRunState.Begin("DG003", 3, 5); // 3층 탐험 시작
            DungeonMapNode first = DungeonRunState.GetSelectableNodes()[0]; // 첫 전투 노드 조회
            DungeonRunState.SelectNode(first.Id); // 전투 노드 선택

            Assert.That(DungeonRunState.HasPendingBattle, Is.True); // 전투 결과 대기 검증
            Assert.That(DungeonRunState.IsCleared(first.Id), Is.False); // 결과 전 미완료 검증

            DungeonRunState.ReportBattleOutcome(BattleOutcome.Victory, 50); // 승리 보고

            Assert.That(DungeonRunState.IsCleared(first.Id), Is.True); // 완료 처리 검증
            Assert.That(DungeonRunState.CollectedGold, Is.EqualTo(50)); // 획득 골드 누적 검증
            Assert.That(DungeonRunState.GetSelectableNodes().TrueForAll(node => node.Floor == 1), Is.True); // 다음 층 선택 가능 검증
        }

        [Test] // 전투 이탈 시 해당 노드만 재도전 가능 검증
        public void Run_UnresolvedBattle_OnlyRetrySelectable() // 건너뛰기 방지 테스트
        {
            DungeonRunState.Begin("DG003", 3, 5); // 3층 탐험 시작
            DungeonMapNode first = DungeonRunState.GetSelectableNodes()[0]; // 첫 전투 노드 조회
            DungeonRunState.SelectNode(first.Id); // 전투 노드 선택 (결과 보고 없이 복귀 상황)
            List<DungeonMapNode> selectable = DungeonRunState.GetSelectableNodes(); // 선택 가능 노드 조회

            Assert.That(selectable.Count, Is.EqualTo(1)); // 재도전 노드 하나 검증
            Assert.That(selectable[0].Id, Is.EqualTo(first.Id)); // 같은 전투 노드 검증
        }

        [Test] // 패배 시 탐험 실패 종료 검증
        public void Run_Defeat_EndsAsFailed() // 패배 종료 테스트
        {
            DungeonRunState.Begin("DG002", 2, 5); // 2층 탐험 시작
            DungeonRunState.SelectNode(DungeonRunState.GetSelectableNodes()[0].Id); // 전투 노드 선택
            DungeonRunState.ReportBattleOutcome(BattleOutcome.Defeat, 0); // 패배 보고

            Assert.That(DungeonRunState.IsActive, Is.False); // 탐험 종료 검증
            Assert.That(DungeonRunState.ConsumeEndSummary(), Is.EqualTo(DungeonRunEndKind.Failed)); // 실패 요약 검증
            Assert.That(DungeonRunState.ConsumeEndSummary(), Is.EqualTo(DungeonRunEndKind.None)); // 요약 1회 소비 검증
        }

        [Test] // 보스 격파 시 탐험 클리어 검증
        public void Run_BossVictory_EndsAsCleared() // 보스 클리어 테스트
        {
            DungeonRunState.Begin("DG001", 1, 5); // 1층(보스만) 탐험 시작
            DungeonMapNode boss = DungeonRunState.GetSelectableNodes()[0]; // 보스 노드 조회

            Assert.That(boss.Kind, Is.EqualTo(DungeonNodeKind.Boss)); // 1층 던전 즉시 보스 검증
            DungeonRunState.SelectNode(boss.Id); // 보스 선택
            Assert.That(DungeonRunState.PendingBattleCountsAsClear, Is.True); // 보스 전투 클리어 기록 대상 검증
            DungeonRunState.ReportBattleOutcome(BattleOutcome.Victory, 120); // 보스 승리 보고

            Assert.That(DungeonRunState.IsActive, Is.False); // 탐험 종료 검증
            Assert.That(DungeonRunState.LastEndKind, Is.EqualTo(DungeonRunEndKind.Cleared)); // 클리어 종료 검증
        }

        [Test] // 노드 종류별 웨이브 구성 검증
        public void Run_ResolveBattleWaves_ByNodeKind() // 웨이브 구성 테스트
        {
            List<string[]> waves = CreateWaves(4); // 웨이브 4개 (마지막이 보스급)
            Assert.That(DungeonRunState.ResolveBattleWaves(waves), Is.SameAs(waves)); // 탐험 외 전투 원본 유지 검증

            DungeonRunState.Begin("DG004", 4, 11); // 4층 탐험 시작
            DungeonRunState.SelectNode(DungeonRunState.GetSelectableNodes()[0].Id); // 첫 층 일반 전투 선택
            List<string[]> battleWaves = DungeonRunState.ResolveBattleWaves(waves); // 일반 전투 웨이브 구성

            Assert.That(battleWaves.Count, Is.EqualTo(1)); // 일반 전투 1웨이브 검증
            Assert.That(battleWaves[0][0], Is.Not.EqualTo("W3")); // 보스급 마지막 웨이브 제외 검증
        }

        [Test] // 보스 노드 전체 웨이브 검증
        public void Run_BossNode_UsesAllWaves() // 보스 웨이브 테스트
        {
            List<string[]> waves = CreateWaves(4); // 웨이브 4개
            DungeonRunState.Begin("DG001", 1, 3); // 보스만 있는 탐험 시작
            DungeonRunState.SelectNode(DungeonRunState.Map.BossNode.Id); // 보스 선택

            Assert.That(DungeonRunState.ResolveBattleWaves(waves).Count, Is.EqualTo(4)); // 전체 웨이브 검증
        }

        [Test] // 보상 배율과 클리어·드롭 규칙 검증
        public void Run_RewardScaleAndClearRules() // 보상 규칙 테스트
        {
            Assert.That(DungeonRunState.GetPendingRewardScale(), Is.EqualTo(1f).Within(0.0001f)); // 탐험 외 전투 100% 검증
            Assert.That(DungeonRunState.PendingBattleCountsAsClear, Is.True); // 탐험 외 전투 클리어 기록 검증
            Assert.That(DungeonRunState.PendingBattleGrantsDrops, Is.True); // 탐험 외 전투 드롭 검증

            DungeonRunState.Begin("DG004", 4, 11); // 4층 탐험 시작
            DungeonRunState.SelectNode(DungeonRunState.GetSelectableNodes()[0].Id); // 일반 전투 선택

            Assert.That(DungeonRunState.GetPendingRewardScale(), Is.EqualTo(0.4f).Within(0.0001f)); // 일반 전투 40% 검증
            Assert.That(DungeonRunState.PendingBattleCountsAsClear, Is.False); // 일반 전투 클리어 미기록 검증
            Assert.That(DungeonRunState.PendingBattleGrantsDrops, Is.False); // 일반 전투 드롭 없음 검증
        }

        [Test] // 다음 전투 효과 누적 및 1회 소비 검증
        public void Run_PendingModifiers_ConsumedOnce() // 다음 전투 효과 테스트
        {
            DungeonRunState.Begin("DG003", 3, 1); // 탐험 시작
            DungeonRunState.AddPendingModifier(DungeonEventCatalog.PickTrap(new System.Random(1))); // 함정 효과 등록
            DungeonRunState.AddPendingModifier(DungeonEventCatalog.PickRest(new System.Random(1))); // 휴식 효과 등록

            Assert.That(DungeonRunState.PendingModifiers.Count, Is.EqualTo(2)); // 효과 2개 누적 검증
            Assert.That(DungeonRunState.ConsumePendingModifiers().Count, Is.EqualTo(2)); // 효과 꺼내기 검증
            Assert.That(DungeonRunState.PendingModifiers.Count, Is.EqualTo(0)); // 1회 소비 후 비움 검증
        }

        [Test] // 이벤트 카탈로그 구성 검증
        public void EventCatalog_HasFourEventsWithTwoChoices() // 이벤트 구성 테스트
        {
            Assert.That(DungeonEventCatalog.AllEvents.Count, Is.EqualTo(4)); // 이벤트 4종 검증

            foreach (DungeonEventDefinition eventDefinition in DungeonEventCatalog.AllEvents) // 이벤트 순회
            {
                Assert.That(eventDefinition.Choices.Count, Is.EqualTo(2)); // 선택지 2개 검증
                Assert.That(eventDefinition.Title, Is.Not.Empty); // 제목 정의 검증
            }
        }

        [Test] // 이벤트 선택지 확률 결과 검증
        public void EventChoice_Roll_UsesSuccessChance() // 확률 결과 테스트
        {
            DungeonOutcome success = new DungeonOutcome(DungeonOutcomeKind.GainGold, 0.5f, null, "성공"); // 성공 결과
            DungeonOutcome failure = new DungeonOutcome(DungeonOutcomeKind.Nothing, 0f, null, "실패"); // 실패 결과
            DungeonEventChoice always = new DungeonEventChoice("항상", 1f, success, failure); // 100% 선택지
            DungeonEventChoice never = new DungeonEventChoice("절대", 0f, success, failure); // 0% 선택지
            System.Random random = new System.Random(3); // 고정 시드 난수 생성기

            Assert.That(always.Roll(random), Is.SameAs(success)); // 100% 성공 검증
            Assert.That(never.Roll(random), Is.SameAs(failure)); // 0% 실패 검증
        }
    }
}
