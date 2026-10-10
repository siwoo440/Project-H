using System.Collections.Generic; // 사전·목록 자료형
using UnityEngine; // 수학·색 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class OutfitBreakRules // 의상 파괴 규칙 (Day87 신규 — 아군의 체력이 문턱 아래로 내려가면 단계가 오른다. 연출만 있고 능력치는 바뀌지 않는다)
    {
        public const int None = 0; // 멀쩡함
        public const int Light = 1; // 1단계 : 의상 손상
        public const int Heavy = 2; // 2단계 : 의상 파괴
        public const float LightThreshold = 0.60f; // 1단계 문턱 (체력 60% 이하)
        public const float HeavyThreshold = 0.30f; // 2단계 문턱 (체력 30% 이하)
        private static readonly Color LightColor = new Color(1f, 0.92f, 0.62f, 1f); // 1단계 글자색 (연노랑)
        private static readonly Color HeavyColor = new Color(1f, 0.66f, 0.52f, 1f); // 2단계 글자색 (주황)

        public static int GetStage(float healthRatio) // 체력 비율 → 단계 (쓰러지면 단계 없음 — 쓰러짐 연출이 따로 있다)
        {
            if (healthRatio <= 0f) return None; // 쓰러짐
            if (healthRatio <= HeavyThreshold) return Heavy; // 30% 이하
            if (healthRatio <= LightThreshold) return Light; // 60% 이하
            return None; // 멀쩡함
        }

        public static string GetLabel(int stage) // 단계 이름 (화면에 보이는 글자)
        {
            return stage >= Heavy ? "의상 파괴" : stage == Light ? "의상 손상" : string.Empty; // 2단계 · 1단계 · 없음
        }

        public static Color GetLabelColor(int stage) // 단계 글자색
        {
            return stage >= Heavy ? HeavyColor : LightColor; // 2단계는 주황, 1단계는 연노랑
        }
    }

    public readonly struct OutfitBreakRequest // 보여 줄 그림 한 건 (Day87 신규)
    {
        public string CharacterId { get; } // 캐릭터 ID (그림을 찾는 이름)
        public string DisplayName { get; } // 캐릭터 이름 (이름표 글자)
        public int Stage { get; } // 단계
        public bool IsValid => !string.IsNullOrEmpty(CharacterId) && Stage > OutfitBreakRules.None; // 보여 줄 수 있는 요청인지

        public OutfitBreakRequest(string characterId, string displayName, int stage) // 요청 생성
        {
            CharacterId = characterId ?? string.Empty; // 캐릭터 ID 저장
            DisplayName = displayName ?? string.Empty; // 이름 저장
            Stage = Mathf.Clamp(stage, OutfitBreakRules.None, OutfitBreakRules.Heavy); // 단계 범위 보정
        }
    }

    public sealed class OutfitBreakTracker // 전투 한 판 동안 누가 어느 단계까지 나왔는지 기억 (Day87 신규 — 단계마다 한 번만 나온다)
    {
        private readonly Dictionary<string, int> shownStages = new Dictionary<string, int>(); // 이미 보여 준 가장 높은 단계
        private readonly Dictionary<string, float> lastRatios = new Dictionary<string, float>(); // 마지막으로 본 체력 비율

        public int GetShownStage(string runtimeId) // 이미 보여 준 단계 (없으면 0)
        {
            return !string.IsNullOrEmpty(runtimeId) && shownStages.TryGetValue(runtimeId, out int stage) ? stage : OutfitBreakRules.None; // 기록 조회
        }

        public int Report(string runtimeId, float healthRatio) // 체력 변화 보고 → 새로 보여 줄 단계 (없으면 0)
        {
            if (string.IsNullOrEmpty(runtimeId)) return OutfitBreakRules.None; // 대상 없음
            float previous = lastRatios.TryGetValue(runtimeId, out float last) ? last : 1f; // 이전 체력 (전투는 가득 찬 체력으로 시작한다)
            lastRatios[runtimeId] = healthRatio; // 체력 기록
            if (healthRatio >= previous) return OutfitBreakRules.None; // 회복 · 부활로는 나오지 않는다 (맞아서 내려갈 때만)
            int stage = OutfitBreakRules.GetStage(healthRatio); // 지금 단계 (한 방에 쓰러지면 0)
            if (stage <= GetShownStage(runtimeId)) return OutfitBreakRules.None; // 이미 보여 준 단계 (회복했다가 다시 내려와도 한 번만)
            shownStages[runtimeId] = stage; // 보여 준 단계 기록 (60%를 건너뛰고 30% 아래로 떨어지면 2단계만 나온다)
            return stage; // 새 단계
        }

        public void Clear() // 전투가 끝나면 비운다
        {
            shownStages.Clear(); // 단계 기록 비움
            lastRatios.Clear(); // 체력 기록 비움
        }
    }

    public sealed class OutfitBreakQueue // 차례를 기다리는 그림 (Day87 신규 — 여러 명이 한꺼번에 맞으면 한 명씩 보여 준다)
    {
        private readonly List<OutfitBreakRequest> pending = new List<OutfitBreakRequest>(); // 대기 목록

        public int Count => pending.Count; // 기다리는 수

        public void Enqueue(OutfitBreakRequest request) // 대기 추가 (같은 캐릭터가 이미 기다리면 높은 단계 하나로 합친다)
        {
            if (!request.IsValid) return; // 잘못된 요청

            for (int index = 0; index < pending.Count; index++) // 대기 목록 순회
            {
                if (pending[index].CharacterId != request.CharacterId) continue; // 다른 캐릭터
                if (request.Stage > pending[index].Stage) pending[index] = request; // 1단계가 나오기도 전에 2단계가 되면 2단계만 (차례는 그대로)
                return; // 합침
            }

            pending.Add(request); // 맨 뒤에 추가
        }

        public bool TryDequeue(out OutfitBreakRequest request) // 다음 차례 꺼내기
        {
            if (pending.Count == 0) // 대기 없음
            {
                request = default; // 빈 요청
                return false; // 없음
            }

            request = pending[0]; // 맨 앞
            pending.RemoveAt(0); // 목록에서 제거
            return true; // 꺼냄
        }

        public void Clear() // 대기 전부 버리기
        {
            pending.Clear(); // 목록 비움
        }
    }

    public sealed class OutfitBreakTimeline // 그림 한 장의 시간 진행 (Day87 신규 — 화면과 분리해 테스트)
    {
        public const float EnterSeconds = 0.22f; // 왼쪽에서 들어오는 시간
        public const float HoldSeconds = 1.10f; // 머무는 시간
        public const float BusyHoldSeconds = 0.60f; // 뒤에 기다리는 그림이 있을 때 머무는 시간
        public const float ExitSeconds = 0.28f; // 왼쪽으로 나가는 시간
        public const float FlashSeconds = 0.30f; // 도착한 뒤 번쩍임이 사라지는 시간

        private float hold = HoldSeconds; // 이번 그림이 머무는 시간

        public float Elapsed { get; private set; } // 경과 시간
        public float TotalSeconds => EnterSeconds + hold + ExitSeconds; // 전체 길이
        public bool IsDone => Elapsed >= TotalSeconds; // 끝났는지
        public float EnterProgress => Mathf.Clamp01(Elapsed / EnterSeconds); // 들어오는 진행률 0~1
        public float ExitProgress => Mathf.Clamp01((Elapsed - EnterSeconds - hold) / ExitSeconds); // 나가는 진행률 0~1
        public float Slide => Mathf.Clamp01(Mathf.Pow(1f - EnterProgress, 3f) + (ExitProgress * ExitProgress)); // 화면 밖으로 밀린 정도 (1 = 화면 밖 · 0 = 제자리. 빠르게 들어와 멈추고 천천히 출발해 나간다)
        public float Flash => Elapsed < EnterSeconds ? EnterProgress : Mathf.Clamp01(1f - ((Elapsed - EnterSeconds) / FlashSeconds)); // 번쩍임 (도착하는 순간 가장 밝다)

        public void Start(bool busy) // 새 그림 시작
        {
            Elapsed = 0f; // 처음부터
            hold = busy ? BusyHoldSeconds : HoldSeconds; // 기다리는 그림이 있으면 짧게
        }

        public void Hurry() // 보여 주는 도중에 다음 그림이 생김 → 머무는 시간을 줄인다
        {
            hold = Mathf.Min(hold, BusyHoldSeconds); // 짧은 쪽으로
        }

        public void Advance(float deltaTime) // 시간 진행
        {
            Elapsed += Mathf.Max(0f, deltaTime); // 음수 방지
        }
    }
}
