using System; // 문자열 비교 기능
using System.Collections.Generic; // 사전·집합 자료형
using ProjectH.SaveSystem; // 룬 합계 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleRuneRuntimeState // 전투 중 장착 룬 상태 (Day60 신규 — 등록형: 스탯 생성 시 등록, 초기화는 BattleRuntimeStates)
    {
        private static readonly Dictionary<string, RuneLoadout> byRuntime = new Dictionary<string, RuneLoadout>(StringComparer.Ordinal); // Runtime ID별 룬 합계 (발동 효과용)
        private static readonly Dictionary<string, RuneLoadout> byCharacter = new Dictionary<string, RuneLoadout>(StringComparer.Ordinal); // 캐릭터 ID별 룬 합계 (게이지 배율용)
        private static readonly HashSet<string> revived = new HashSet<string>(StringComparer.Ordinal); // 부활 룬 사용 기록 (전투당 1회)
        internal static float RegenTimer; // 재생 1초 누적
        internal static float BarrierTimer; // 보호막 5초 누적
        internal static float FrenzyTimer; // 광기 확인 누적

        public static void Register(string runtimeId, string characterId, RuneLoadout loadout) // 룬 합계 등록 (빈 합계면 기존 등록 제거 — 잔여 상태 정리)
        {
            bool empty = loadout == null || loadout.IsEmpty; // 빈 합계 여부
            Store(byRuntime, runtimeId, empty ? null : loadout); // Runtime 기준 등록
            Store(byCharacter, characterId, empty ? null : loadout); // 캐릭터 기준 등록
        }

        private static void Store(Dictionary<string, RuneLoadout> target, string key, RuneLoadout loadout) // 등록 또는 제거
        {
            if (string.IsNullOrWhiteSpace(key)) // 키 확인
            {
                return; // 처리 없음
            }

            if (loadout == null) // 빈 합계 확인
            {
                target.Remove(key); // 기존 등록 제거
            }
            else // 합계 존재
            {
                target[key] = loadout; // 등록
            }
        }

        public static float Get(string runtimeId, RuneKind kind) // Runtime 기준 룬 수치
        {
            return runtimeId != null && byRuntime.TryGetValue(runtimeId, out RuneLoadout loadout) ? loadout.Get(kind) : 0f; // 수치 반환
        }

        public static float GetByCharacter(string characterId, RuneKind kind) // 캐릭터 기준 룬 수치
        {
            return characterId != null && byCharacter.TryGetValue(characterId, out RuneLoadout loadout) ? loadout.Get(kind) : 0f; // 수치 반환
        }

        public static bool TryConsumeRevive(string runtimeId) // 부활 룬 1회 사용
        {
            return revived.Add(runtimeId ?? string.Empty); // 처음이면 true
        }

        public static void ResetBattleEffects() // 전투 시작 초기화 (등록 룬은 유지 — Day52 등록형 규칙)
        {
            revived.Clear(); // 부활 기록 초기화
            RegenTimer = 0f; // 재생 누적 초기화
            BarrierTimer = 0f; // 보호막 누적 초기화
            FrenzyTimer = 0f; // 광기 누적 초기화
        }

        public static void ResetAll() // 전체 초기화 (전투 종료·테스트)
        {
            ResetBattleEffects(); // 효과 초기화
            byRuntime.Clear(); // Runtime 등록 제거
            byCharacter.Clear(); // 캐릭터 등록 제거
        }
    }
}
