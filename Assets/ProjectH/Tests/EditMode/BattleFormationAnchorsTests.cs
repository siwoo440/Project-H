using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 배치 기능
using UnityEngine; // Unity 게임 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleFormationAnchorsTests // 전투 배치 앵커 테스트
    {
        private GameObject rootObject; // 테스트 루트 객체

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 테스트 루트 준비
        {
            rootObject = new GameObject("BattleFormationAnchorsTests"); // 테스트 루트 생성
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 테스트 루트 정리
        {
            Object.DestroyImmediate(rootObject); // 테스트 루트 제거
        }

        [Test] // 테스트 표시
        public void Configure_PreservesAllyAndEnemySlotOrder() // 아군 적군 앵커 순서 검증
        {
            BattleFormationAnchors anchors = rootObject.AddComponent<BattleFormationAnchors>(); // 배치 앵커 컴포넌트 생성
            Transform[] allies = CreateAnchors("ALLY", 4); // 아군 앵커 생성
            Transform[] enemies = CreateAnchors("ENEMY", 5); // 적군 앵커 생성
            anchors.Configure(allies, enemies); // 앵커 배열 연결

            Assert.That(anchors.AllyCount, Is.EqualTo(4)); // 아군 앵커 수 검증
            Assert.That(anchors.EnemyCount, Is.EqualTo(5)); // 적군 앵커 수 검증
            Assert.That(anchors.GetAllyAnchor(0), Is.SameAs(allies[0])); // 첫 아군 앵커 검증
            Assert.That(anchors.GetAllyAnchor(3), Is.SameAs(allies[3])); // 마지막 아군 앵커 검증
            Assert.That(anchors.GetEnemyAnchor(0), Is.SameAs(enemies[0])); // 첫 적군 앵커 검증
            Assert.That(anchors.GetEnemyAnchor(4), Is.SameAs(enemies[4])); // 마지막 적군 앵커 검증
        }

        [Test] // 테스트 표시
        public void GetAnchor_InvalidIndex_ReturnsNull() // 잘못된 앵커 번호 처리 검증
        {
            BattleFormationAnchors anchors = rootObject.AddComponent<BattleFormationAnchors>(); // 배치 앵커 컴포넌트 생성
            anchors.Configure(CreateAnchors("ALLY", 4), CreateAnchors("ENEMY", 5)); // 앵커 배열 연결

            Assert.That(anchors.GetAllyAnchor(-1), Is.Null); // 음수 아군 슬롯 검증
            Assert.That(anchors.GetAllyAnchor(4), Is.Null); // 초과 아군 슬롯 검증
            Assert.That(anchors.GetEnemyAnchor(-1), Is.Null); // 음수 적군 슬롯 검증
            Assert.That(anchors.GetEnemyAnchor(5), Is.Null); // 초과 적군 슬롯 검증
        }

        private Transform[] CreateAnchors(string prefix, int count) // 테스트 앵커 배열 생성
        {
            Transform[] result = new Transform[count]; // 앵커 결과 배열 생성

            for (int index = 0; index < count; index++) // 앵커 개수 순회
            {
                GameObject anchor = new GameObject($"{prefix}_{index}"); // 테스트 앵커 객체 생성
                anchor.transform.SetParent(rootObject.transform, false); // 테스트 루트 연결
                result[index] = anchor.transform; // 앵커 배열 등록
            }

            return result; // 앵커 배열 반환
        }
    }
}
