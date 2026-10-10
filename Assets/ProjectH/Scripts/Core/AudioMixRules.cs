using System.Collections.Generic; // 사전 자료형
using UnityEngine; // 수학 기능

namespace ProjectH.Core // 프로젝트 핵심 영역
{
    public sealed class SfxThrottle // 같은 효과음이 한순간에 여러 번 겹치지 않게 거르는 기능 (Day88 신규 — 재생기와 분리해 테스트)
    {
        private readonly Dictionary<string, float> lastTimes = new Dictionary<string, float>(); // 소리별 마지막 재생 시각

        public bool TryPass(string key, float now, float minInterval) // 지금 이 소리를 내도 되는지 (되면 시각을 기록하고 true)
        {
            if (string.IsNullOrEmpty(key)) return false; // 소리 이름 없음
            if (lastTimes.TryGetValue(key, out float last) && now >= last && now - last < minInterval) return false; // 방금 난 소리 (시각이 되돌아간 경우는 통과시킨다)
            lastTimes[key] = now; // 재생 시각 기록
            return true; // 통과
        }

        public void Clear() // 기록 비우기
        {
            lastTimes.Clear(); // 전부 제거
        }
    }

    public sealed class BgmFader // 배경음 전환 (Day88 신규 — 지금 곡을 줄였다가 새 곡으로 바꾸고 다시 키운다. 재생기와 분리해 테스트)
    {
        public const float FadeOutSeconds = 0.35f; // 지금 곡이 사라지는 시간
        public const float FadeInSeconds = 0.5f; // 새 곡이 커지는 시간

        public string Current { get; private set; } = string.Empty; // 지금 채널에 올라가 있는 곡 (빈 값 = 무음)
        public string Target { get; private set; } = string.Empty; // 틀려는 곡
        public float Gain { get; private set; } = 1f; // 음량 배율 0~1
        public bool IsBusy => Target != Current || Gain < 1f; // 전환 중인지 (아니면 매 프레임 할 일이 없다)

        public void Request(string key) // 곡 바꾸기 요청 (같은 곡이면 아무 일도 없다)
        {
            Target = key ?? string.Empty; // 목표 곡 기록
        }

        public bool Tick(float deltaTime) // 시간 진행 → 이번에 채널의 곡을 Current로 바꿔 끼워야 하면 true
        {
            float step = Mathf.Max(0f, deltaTime); // 음수 시간 방지

            if (Target != Current) // 곡을 바꾸는 중
            {
                Gain = string.IsNullOrEmpty(Current) ? 0f : Mathf.MoveTowards(Gain, 0f, step / FadeOutSeconds); // 나오는 곡이 없으면 바로, 있으면 줄여 간다
                if (Gain > 0f) return false; // 아직 줄이는 중
                Current = Target; // 곡 교체
                return true; // 호출 측이 실제로 곡을 바꿔 끼운다
            }

            if (Gain < 1f) Gain = Mathf.MoveTowards(Gain, 1f, step / FadeInSeconds); // 새 곡을 키운다 (바꾸는 도중에 원래 곡으로 돌아온 경우도 여기서 다시 커진다)
            return false; // 교체 없음
        }
    }
}
