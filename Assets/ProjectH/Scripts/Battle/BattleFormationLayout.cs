using System; // 예외 기능
using UnityEngine; // Unity 위치 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleFormationLayout // 전투 진형 좌표 적용 기능
    {
        public static void ApplyPositions(BattleFormationAnchors formation, Vector3[] allyPositions, Vector3[] enemyPositions) // 전투 진형 좌표 적용
        {
            if (formation == null) // 전투 진형 컴포넌트 확인
            {
                throw new ArgumentNullException(nameof(formation)); // 진형 누락 예외 발생
            }

            if (allyPositions == null || allyPositions.Length < formation.AllyCount) // 아군 좌표 개수 확인
            {
                throw new ArgumentException("아군 진형 좌표가 부족합니다.", nameof(allyPositions)); // 아군 좌표 부족 예외 발생
            }

            if (enemyPositions == null || enemyPositions.Length < formation.EnemyCount) // 적군 좌표 개수 확인
            {
                throw new ArgumentException("적군 진형 좌표가 부족합니다.", nameof(enemyPositions)); // 적군 좌표 부족 예외 발생
            }

            for (int index = 0; index < formation.AllyCount; index++) // 아군 진형 슬롯 순회
            {
                Transform anchor = formation.GetAllyAnchor(index); // 아군 진형 앵커 조회

                if (anchor == null) // 아군 진형 앵커 확인
                {
                    throw new InvalidOperationException($"AllySlot_{index} 앵커가 없습니다."); // 아군 앵커 누락 예외 발생
                }

                anchor.position = allyPositions[index]; // 아군 압축 좌표 적용
            }

            for (int index = 0; index < formation.EnemyCount; index++) // 적군 진형 슬롯 순회
            {
                Transform anchor = formation.GetEnemyAnchor(index); // 적군 진형 앵커 조회

                if (anchor == null) // 적군 진형 앵커 확인
                {
                    throw new InvalidOperationException($"EnemySlot_{index} 앵커가 없습니다."); // 적군 앵커 누락 예외 발생
                }

                anchor.position = enemyPositions[index]; // 적군 압축 좌표 적용
            }
        }
    }
}
