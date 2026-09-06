using System; // 이벤트 기능
using System.Collections.Generic; // 캐릭터별 게이지 저장 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleUltimateGaugeRuntimeState // 전투 한정 캐릭터별 궁극기 게이지 Runtime 상태
    {
        public const int MaxGauge = 100; // 궁극기 게이지 최대치
        private static readonly Dictionary<string, int> gauges = new Dictionary<string, int>(StringComparer.Ordinal); // 캐릭터별 현재 게이지 저장소
        public static event Action<string, int> GaugeChanged; // 캐릭터별 게이지 변경 이벤트

        public static int GetGauge(string characterId) // 캐릭터 현재 궁극기 게이지 조회
        {
            if (string.IsNullOrWhiteSpace(characterId)) // 캐릭터 ID 유효성 확인
            {
                return 0; // 잘못된 ID 기본 게이지 반환
            }

            return gauges.TryGetValue(characterId, out int value) ? value : 0; // 저장 게이지 또는 기본값 반환
        }

        public static float GetGaugeRatio(string characterId) // 캐릭터 궁극기 게이지 비율 조회
        {
            return (float)GetGauge(characterId) / MaxGauge; // 0~1 게이지 비율 반환
        }

        public static bool IsReady(string characterId) // 캐릭터 궁극기 완충 상태 확인
        {
            return GetGauge(characterId) >= MaxGauge; // 최대 게이지 도달 여부 반환
        }

        public static bool CanUseUltimate(string characterId, bool isAlive) // 캐릭터 궁극기 사용 가능 상태 확인
        {
            return isAlive && IsReady(characterId); // 생존과 완충 조건 동시 확인
        }

        public static int AddGauge(string characterId, int amount) // 캐릭터 궁극기 게이지 충전
        {
            if (string.IsNullOrWhiteSpace(characterId)) // 캐릭터 ID 유효성 확인
            {
                return 0; // 잘못된 ID 충전 차단
            }

            int currentGauge = GetGauge(characterId); // 기존 궁극기 게이지 조회

            if (amount <= 0 || currentGauge >= MaxGauge) // 유효 충전량과 완충 상태 확인
            {
                return currentGauge; // 변경 없는 현재 게이지 반환
            }

            int nextGauge = Math.Min(MaxGauge, currentGauge + amount); // 최대 100 기준 신규 게이지 계산
            gauges[characterId] = nextGauge; // 캐릭터별 신규 게이지 저장
            GaugeChanged?.Invoke(characterId, nextGauge); // HUD 등 게이지 변경 알림
            return nextGauge; // 충전 후 게이지 반환
        }

        public static bool TryConsumeGauge(string characterId, bool isAlive) // 궁극기 사용 전 게이지 소비 시도
        {
            if (!CanUseUltimate(characterId, isAlive)) // 생존 및 Ready 조건 확인
            {
                return false; // 사용 불가 상태 소비 차단
            }

            ResetGauge(characterId); // 완충 게이지 전체 소비
            return true; // 게이지 소비 성공 반환
        }

        public static void ResetGauge(string characterId) // 캐릭터 개별 궁극기 게이지 초기화
        {
            if (string.IsNullOrWhiteSpace(characterId)) // 캐릭터 ID 유효성 확인
            {
                return; // 잘못된 ID 초기화 중단
            }

            if (!gauges.Remove(characterId)) // 저장 게이지 존재 여부 확인
            {
                return; // 기존 게이지 없음 처리 중단
            }

            GaugeChanged?.Invoke(characterId, 0); // 개별 게이지 초기화 알림
        }

        public static void ResetAll() // 신규 전투용 전체 궁극기 게이지 초기화
        {
            if (gauges.Count == 0) // 기존 게이지 존재 확인
            {
                return; // 초기화 대상 없음 처리 중단
            }

            string[] characterIds = new string[gauges.Count]; // 초기화 알림용 캐릭터 ID 배열 생성
            gauges.Keys.CopyTo(characterIds, 0); // 기존 게이지 캐릭터 ID 복사
            gauges.Clear(); // 전투 Runtime 전체 게이지 제거

            for (int index = 0; index < characterIds.Length; index++) // 초기화 대상 캐릭터 순회
            {
                GaugeChanged?.Invoke(characterIds[index], 0); // UI 포함 캐릭터별 0 게이지 알림
            }
        }
    }
}
