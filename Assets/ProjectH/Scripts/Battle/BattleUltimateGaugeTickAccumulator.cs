using UnityEngine; // Unity 수학 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public sealed class BattleUltimateGaugeTickAccumulator // 궁극기 게이지 초 단위 시간 누적기
    {
        private const float TickInterval = 1f; // 게이지 충전 간격
        private float elapsedTime; // 누적 전투 시간

        public int Advance(float deltaTime) // 경과 시간 기반 충전 Tick 계산
        {
            if (deltaTime <= 0f) // 유효 경과 시간 확인
            {
                return 0; // 충전 Tick 없음 반환
            }

            elapsedTime += deltaTime; // 전투 경과 시간 누적
            int tickCount = Mathf.FloorToInt(elapsedTime / TickInterval); // 완료된 1초 단위 Tick 수 계산

            if (tickCount <= 0) // 완료 Tick 존재 확인
            {
                return 0; // 충전 Tick 없음 반환
            }

            elapsedTime -= tickCount * TickInterval; // 처리 완료 시간 차감
            return tickCount; // 완료된 충전 Tick 수 반환
        }

        public void Reset() // 누적 시간 초기화
        {
            elapsedTime = 0f; // 전투 시간 누적값 초기화
        }
    }
}
