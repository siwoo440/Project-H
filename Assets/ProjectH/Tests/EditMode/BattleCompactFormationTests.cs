using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 진형 기능
using UnityEngine; // Unity 위치 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleCompactFormationTests // 전투 압축 진형 테스트
    {
        private GameObject rootObject; // 테스트 루트 객체

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 테스트 진형 준비
        {
            rootObject = new GameObject("BattleCompactFormationTests"); // 테스트 루트 생성
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 테스트 진형 정리
        {
            Object.DestroyImmediate(rootObject); // 테스트 루트 제거
        }

        [Test] // 테스트 표시
        public void ApplyPositions_KeepsAllyYSpanWithinPointSixtyFive() // 아군 Y 간격 축소 검증
        {
            BattleFormationAnchors formation = CreateFormation(4, 5); // 테스트 진형 생성
            BattleFormationLayout.ApplyPositions(formation, CreateAllyPositions(), CreateEnemyPositions()); // Runtime 진형 좌표 적용

            float minY = float.PositiveInfinity; // 최소 아군 Y 초기화
            float maxY = float.NegativeInfinity; // 최대 아군 Y 초기화

            for (int index = 0; index < formation.AllyCount; index++) // 아군 진형 순회
            {
                float y = formation.GetAllyAnchor(index).position.y; // 아군 Y 좌표 조회
                minY = Mathf.Min(minY, y); // 최소 아군 Y 갱신
                maxY = Mathf.Max(maxY, y); // 최대 아군 Y 갱신
            }

            Assert.That(maxY - minY, Is.EqualTo(0.65f).Within(0.0001f)); // 아군 Y 범위 축소 검증
        }

        [Test] // 테스트 표시
        public void ApplyPositions_KeepsEnemyYSpanWithinPointSixtyFive() // 적군 Y 간격 축소 검증
        {
            BattleFormationAnchors formation = CreateFormation(4, 5); // 테스트 진형 생성
            BattleFormationLayout.ApplyPositions(formation, CreateAllyPositions(), CreateEnemyPositions()); // Runtime 진형 좌표 적용

            float minY = float.PositiveInfinity; // 최소 적군 Y 초기화
            float maxY = float.NegativeInfinity; // 최대 적군 Y 초기화

            for (int index = 0; index < formation.EnemyCount; index++) // 적군 진형 순회
            {
                float y = formation.GetEnemyAnchor(index).position.y; // 적군 Y 좌표 조회
                minY = Mathf.Min(minY, y); // 최소 적군 Y 갱신
                maxY = Mathf.Max(maxY, y); // 최대 적군 Y 갱신
            }

            Assert.That(maxY - minY, Is.EqualTo(0.65f).Within(0.0001f)); // 적군 Y 범위 축소 검증
        }

        private BattleFormationAnchors CreateFormation(int allyCount, int enemyCount) // 테스트 전투 진형 생성
        {
            BattleFormationAnchors formation = rootObject.AddComponent<BattleFormationAnchors>(); // 전투 진형 컴포넌트 추가
            Transform[] allies = new Transform[allyCount]; // 아군 앵커 배열 생성
            Transform[] enemies = new Transform[enemyCount]; // 적군 앵커 배열 생성

            for (int index = 0; index < allyCount; index++) // 아군 앵커 순회
            {
                GameObject anchor = new GameObject($"AllySlot_{index}"); // 아군 앵커 생성
                anchor.transform.SetParent(rootObject.transform, false); // 테스트 루트 연결
                allies[index] = anchor.transform; // 아군 앵커 배열 등록
            }

            for (int index = 0; index < enemyCount; index++) // 적군 앵커 순회
            {
                GameObject anchor = new GameObject($"EnemySlot_{index}"); // 적군 앵커 생성
                anchor.transform.SetParent(rootObject.transform, false); // 테스트 루트 연결
                enemies[index] = anchor.transform; // 적군 앵커 배열 등록
            }

            formation.Configure(allies, enemies); // 전투 진형 앵커 연결
            return formation; // 전투 진형 반환
        }

        private static Vector3[] CreateAllyPositions() // 아군 압축 좌표 반환
        {
            return new[] // 아군 압축 좌표 배열 반환
            {
                new Vector3(-3.20f, -0.25f, 0f), // 아군 슬롯 0
                new Vector3(-3.50f, -0.05f, 0f), // 아군 슬롯 1
                new Vector3(-3.75f, 0.20f, 0f), // 아군 슬롯 2
                new Vector3(-4.00f, 0.40f, 0f) // 아군 슬롯 3
            }; // 아군 압축 좌표 반환 종료
        }

        private static Vector3[] CreateEnemyPositions() // 적군 압축 좌표 반환
        {
            return new[] // 적군 압축 좌표 배열 반환
            {
                new Vector3(3.10f, -0.15f, 0f), // 적군 슬롯 0
                new Vector3(3.45f, 0.15f, 0f), // 적군 슬롯 1
                new Vector3(3.75f, 0.35f, 0f), // 적군 슬롯 2
                new Vector3(4.05f, 0.50f, 0f), // 적군 슬롯 3
                new Vector3(4.35f, 0.05f, 0f) // 적군 슬롯 4
            }; // 적군 압축 좌표 반환 종료
        }
    }
}
