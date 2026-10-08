using UnityEngine; // 수학 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleUnitMotionMath // 전투 유닛 움직임 계산 (Day79 신규 — 그림 한 장을 코드로 움직인다. 화면 표시만 바꾸고 실제 위치는 건드리지 않는다)
    {
        public const float IdleAmplitude = 4f; // 대기 중 오르내리는 폭 (유닛 캔버스 픽셀, 캔버스 높이 285 기준)
        public const float IdleSpeed = 2.2f; // 대기 오르내림 빠르기 (초당 라디안 — 한 번 오르내리는 데 약 2.9초)
        public const float LungeDistance = 26f; // 공격할 때 앞으로 튀어나가는 거리
        public const float LungeOutSeconds = 0.07f; // 튀어나가는 시간
        public const float LungeBackSeconds = 0.16f; // 돌아오는 시간
        public const float RecoilDistance = 14f; // 맞았을 때 뒤로 밀리는 거리
        public const float RecoilOutSeconds = 0.04f; // 밀리는 시간
        public const float RecoilBackSeconds = 0.14f; // 돌아오는 시간
        public const float DefeatSeconds = 0.30f; // 쓰러지는 시간 (전장에서 사라지기까지 0.35초보다 짧게)
        public const float DefeatTilt = 22f; // 쓰러질 때 기우는 각도
        public const float DefeatDrop = 10f; // 쓰러질 때 내려앉는 거리
        public const float ReducedScale = 0.35f; // 설정에서 '흔들림 줄이기'를 켰을 때 움직임 배율
        public const int MinSortingOrder = 1; // 유닛 겹침 순서 하한 (전투 배경 -20 위)
        public const int MaxSortingOrder = 18; // 유닛 겹침 순서 상한 (전투 HUD 20 아래)
        private const int MiddleSortingOrder = 10; // 화면 세로 가운데의 겹침 순서
        private const float SortingPerUnit = 3f; // 세로 1칸마다 달라지는 겹침 순서

        public static float Pulse(float elapsed, float outSeconds, float backSeconds) // 0 → 1 → 0 으로 한 번 튀는 값 (나갈 때는 곧게, 돌아올 때는 부드럽게)
        {
            if (elapsed < 0f) return 0f; // 시작 전
            if (elapsed < outSeconds) return outSeconds <= 0f ? 1f : elapsed / outSeconds; // 나가는 중
            float back = elapsed - outSeconds; // 돌아오기 시작한 뒤 흐른 시간
            if (backSeconds <= 0f || back >= backSeconds) return 0f; // 복귀 끝
            float t = back / backSeconds; // 복귀 진행률
            return 1f - (t * t * (3f - (2f * t))); // 부드러운 복귀
        }

        public static bool IsPulseDone(float elapsed, float outSeconds, float backSeconds) // 한 번 튀는 움직임이 끝났는지
        {
            float total = outSeconds + backSeconds; // 전체 시간 (float 변수에 담아 정밀도를 맞춘다 — 식을 그대로 비교하면 중간 계산이 더 정밀해 경계에서 어긋난다)
            return elapsed >= total; // 전체 시간 경과
        }

        public static float Idle(float time, float phase) // 대기 중 세로 오르내림 (픽셀)
        {
            return Mathf.Sin((time * IdleSpeed) + phase) * IdleAmplitude; // 사인 곡선
        }

        public static float GetPhase(string runtimeId) // 유닛마다 다른 박자 (같은 유닛은 늘 같은 값 — 모두 똑같이 오르내리면 기계처럼 보인다)
        {
            if (string.IsNullOrEmpty(runtimeId)) return 0f; // 이름 없음
            int hash = 17; // 해시 시작값

            for (int index = 0; index < runtimeId.Length; index++) // 글자 순회
            {
                hash = unchecked((hash * 31) + runtimeId[index]); // 글자를 섞음
            }

            return (Mathf.Abs(hash % 628) / 100f); // 0 ~ 6.27 (한 바퀴)
        }

        public static float GetDefeatProgress(float elapsed) // 쓰러짐 진행률 (0 ~ 1)
        {
            return elapsed < 0f ? 0f : Mathf.Clamp01(elapsed / DefeatSeconds); // 진행률
        }

        public static float GetMotionScale(bool reduceShake) // 움직임 배율 (설정의 '흔들림 줄이기' 연동)
        {
            return reduceShake ? ReducedScale : 1f; // 줄이기 · 보통
        }

        public static int GetSortingOrder(float worldY) // 겹침 순서 : 화면 아래쪽(가까운 쪽) 유닛이 앞에 그려진다
        {
            return Mathf.Clamp(MiddleSortingOrder - Mathf.RoundToInt(worldY * SortingPerUnit), MinSortingOrder, MaxSortingOrder); // 배경과 HUD 사이로 제한
        }
    }
}
