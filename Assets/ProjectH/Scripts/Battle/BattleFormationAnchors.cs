using System; // 배열 기본값 기능
using UnityEngine; // Unity Transform 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 배치 컴포넌트 방지
    public sealed class BattleFormationAnchors : MonoBehaviour // 전투 진형 앵커 모음
    {
        [SerializeField] private Transform[] allyAnchors = Array.Empty<Transform>(); // 아군 배치 앵커
        [SerializeField] private Transform[] enemyAnchors = Array.Empty<Transform>(); // 적군 배치 앵커
        public int AllyCount => allyAnchors == null ? 0 : allyAnchors.Length; // 아군 앵커 수 반환
        public int EnemyCount => enemyAnchors == null ? 0 : enemyAnchors.Length; // 적군 앵커 수 반환

        public void Configure(Transform[] allies, Transform[] enemies) // 에디터 배치 앵커 설정
        {
            allyAnchors = allies ?? Array.Empty<Transform>(); // 아군 앵커 저장
            enemyAnchors = enemies ?? Array.Empty<Transform>(); // 적군 앵커 저장
        }

        public Transform GetAllyAnchor(int slotIndex) // 아군 슬롯 앵커 조회
        {
            if (allyAnchors == null || slotIndex < 0 || slotIndex >= allyAnchors.Length) // 아군 슬롯 범위 확인
            {
                return null; // 잘못된 아군 슬롯 반환
            }

            return allyAnchors[slotIndex]; // 아군 슬롯 앵커 반환
        }

        public Transform GetEnemyAnchor(int slotIndex) // 적군 슬롯 앵커 조회
        {
            if (enemyAnchors == null || slotIndex < 0 || slotIndex >= enemyAnchors.Length) // 적군 슬롯 범위 확인
            {
                return null; // 잘못된 적군 슬롯 반환
            }

            return enemyAnchors[slotIndex]; // 적군 슬롯 앵커 반환
        }
    }
}
