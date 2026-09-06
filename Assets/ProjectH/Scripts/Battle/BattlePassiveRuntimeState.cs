using System.Collections.Generic; // Dictionary 자료형
using UnityEngine; // Unity 시간 및 수학 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattlePassiveRuntimeState // 전투 패시브 Runtime 상태 저장소
    {
        private sealed class TimedValue // 시간 제한 패시브 수치
        {
            public float Value; // 패시브 수치
            public float ExpiresAt; // 만료 전투 시간
        }

        private static readonly Dictionary<string, float> cooldownUntil = new Dictionary<string, float>(); // 패시브 쿨다운 만료 시각
        private static readonly Dictionary<string, int> counters = new Dictionary<string, int>(); // 패시브 누적 카운터
        private static readonly Dictionary<string, int> shields = new Dictionary<string, int>(); // Runtime ID별 보호막
        private static readonly Dictionary<string, TimedValue> criticalChanceBonuses = new Dictionary<string, TimedValue>(); // Runtime ID별 치명타율 증가
        private static readonly Dictionary<string, TimedValue> defenseReductions = new Dictionary<string, TimedValue>(); // Runtime ID별 방어 감소

        public static void ResetAll() // 패시브 Runtime 상태 전체 초기화
        {
            cooldownUntil.Clear(); // 패시브 쿨다운 제거
            counters.Clear(); // 패시브 카운터 제거
            shields.Clear(); // 패시브 보호막 제거
            criticalChanceBonuses.Clear(); // 치명타율 증가 제거
            defenseReductions.Clear(); // 방어 감소 제거
        }

        public static bool IsCooldownReady(string key, float nowSeconds = -1f) // 패시브 쿨다운 사용 가능 여부
        {
            if (string.IsNullOrWhiteSpace(key)) // 패시브 키 확인
            {
                return false; // 빈 키 사용 차단
            }

            float now = ResolveNow(nowSeconds); // 현재 전투 시간 계산

            if (!cooldownUntil.TryGetValue(key, out float expiresAt)) // 기존 쿨다운 존재 확인
            {
                return true; // 미사용 패시브 사용 가능 반환
            }

            return now >= expiresAt; // 쿨다운 만료 여부 반환
        }

        public static void StartCooldown(string key, float duration, float nowSeconds = -1f) // 패시브 쿨다운 시작
        {
            if (string.IsNullOrWhiteSpace(key) || duration <= 0f) // 패시브 키 및 지속시간 확인
            {
                return; // 잘못된 쿨다운 시작 중단
            }

            float now = ResolveNow(nowSeconds); // 현재 전투 시간 계산
            cooldownUntil[key] = now + duration; // 패시브 쿨다운 만료 시각 저장
        }

        public static int IncrementCounter(string runtimeId, string counterKey) // 패시브 카운터 1 증가
        {
            string key = BuildCounterKey(runtimeId, counterKey); // Runtime별 카운터 키 생성

            if (string.IsNullOrEmpty(key)) // 카운터 키 유효성 확인
            {
                return 0; // 잘못된 카운터 증가 차단
            }

            counters.TryGetValue(key, out int current); // 현재 카운터 조회
            current++; // 현재 카운터 증가
            counters[key] = current; // 증가 카운터 저장
            return current; // 증가 후 카운터 반환
        }

        public static int GetCounter(string runtimeId, string counterKey) // 패시브 카운터 조회
        {
            string key = BuildCounterKey(runtimeId, counterKey); // Runtime별 카운터 키 생성

            if (string.IsNullOrEmpty(key)) // 카운터 키 유효성 확인
            {
                return 0; // 잘못된 카운터 0 반환
            }

            return counters.TryGetValue(key, out int current) ? current : 0; // 현재 카운터 반환
        }

        public static void ResetCounter(string runtimeId, string counterKey) // 패시브 카운터 초기화
        {
            string key = BuildCounterKey(runtimeId, counterKey); // Runtime별 카운터 키 생성

            if (!string.IsNullOrEmpty(key)) // 카운터 키 유효성 확인
            {
                counters.Remove(key); // 현재 카운터 제거
            }
        }

        public static void GrantShield(string runtimeId, int amount) // 패시브 보호막 부여
        {
            if (string.IsNullOrWhiteSpace(runtimeId) || amount <= 0) // 보호막 입력 확인
            {
                return; // 잘못된 보호막 부여 중단
            }

            shields.TryGetValue(runtimeId, out int current); // 기존 보호막 조회
            shields[runtimeId] = Mathf.Max(current, amount); // 중첩 대신 더 큰 보호막 유지
        }

        public static int GetShield(string runtimeId) // 현재 보호막 조회
        {
            if (string.IsNullOrWhiteSpace(runtimeId)) // Runtime ID 확인
            {
                return 0; // 빈 Runtime ID 보호막 0 반환
            }

            return shields.TryGetValue(runtimeId, out int current) ? Mathf.Max(0, current) : 0; // 현재 보호막 반환
        }

        public static int AbsorbShield(string runtimeId, int incomingDamage, out int absorbed) // 보호막 우선 피해 흡수
        {
            absorbed = 0; // 보호막 흡수량 초기화
            int damage = Mathf.Max(0, incomingDamage); // 입력 피해 음수 방지

            if (damage <= 0 || string.IsNullOrWhiteSpace(runtimeId)) // 피해 및 Runtime ID 확인
            {
                return damage; // 보호막 미적용 피해 반환
            }

            int currentShield = GetShield(runtimeId); // 현재 보호막 조회

            if (currentShield <= 0) // 보호막 존재 확인
            {
                return damage; // 보호막 없는 피해 반환
            }

            absorbed = Mathf.Min(currentShield, damage); // 실제 보호막 흡수량 계산
            int remainingShield = currentShield - absorbed; // 잔여 보호막 계산

            if (remainingShield > 0) // 잔여 보호막 존재 확인
            {
                shields[runtimeId] = remainingShield; // 잔여 보호막 저장
            }
            else // 보호막 소진 처리
            {
                shields.Remove(runtimeId); // 소진 보호막 제거
            }

            return damage - absorbed; // 보호막 적용 후 잔여 피해 반환
        }

        public static void SetCriticalChanceBonus(string runtimeId, float value, float duration, float nowSeconds = -1f) // 치명타율 증가 적용
        {
            SetTimedValue(criticalChanceBonuses, runtimeId, value, duration, nowSeconds); // 치명타율 증가 저장
        }

        public static float GetCriticalChanceBonus(string runtimeId, float nowSeconds = -1f) // 치명타율 증가 조회
        {
            return GetTimedValue(criticalChanceBonuses, runtimeId, nowSeconds); // 현재 치명타율 증가 반환
        }

        public static void SetDefenseReduction(string runtimeId, float value, float duration, float nowSeconds = -1f) // 방어 감소 적용
        {
            SetTimedValue(defenseReductions, runtimeId, Mathf.Clamp(value, 0f, 0.90f), duration, nowSeconds); // 방어 감소 저장
        }

        public static float GetDefenseReduction(string runtimeId, float nowSeconds = -1f) // 방어 감소 조회
        {
            return Mathf.Clamp(GetTimedValue(defenseReductions, runtimeId, nowSeconds), 0f, 0.90f); // 현재 방어 감소 반환
        }

        private static void SetTimedValue(Dictionary<string, TimedValue> storage, string runtimeId, float value, float duration, float nowSeconds) // 시간 제한 수치 저장
        {
            if (storage == null || string.IsNullOrWhiteSpace(runtimeId) || value <= 0f || duration <= 0f) // 시간 제한 입력 확인
            {
                return; // 잘못된 시간 제한 수치 저장 중단
            }

            float now = ResolveNow(nowSeconds); // 현재 전투 시간 계산
            storage[runtimeId] = new TimedValue // Runtime ID별 최신 효과 저장
            {
                Value = value, // 패시브 수치 저장
                ExpiresAt = now + duration // 만료 전투 시간 저장
            }; // 시간 제한 수치 저장 완료
        }

        private static float GetTimedValue(Dictionary<string, TimedValue> storage, string runtimeId, float nowSeconds) // 시간 제한 수치 조회
        {
            if (storage == null || string.IsNullOrWhiteSpace(runtimeId)) // 시간 제한 조회 입력 확인
            {
                return 0f; // 잘못된 조회 0 반환
            }

            if (!storage.TryGetValue(runtimeId, out TimedValue value)) // 저장된 시간 제한 효과 확인
            {
                return 0f; // 활성 효과 없음 반환
            }

            float now = ResolveNow(nowSeconds); // 현재 전투 시간 계산

            if (now >= value.ExpiresAt) // 효과 만료 여부 확인
            {
                storage.Remove(runtimeId); // 만료 효과 제거
                return 0f; // 만료 효과 0 반환
            }

            return Mathf.Max(0f, value.Value); // 활성 효과 수치 반환
        }

        private static string BuildCounterKey(string runtimeId, string counterKey) // Runtime별 카운터 키 생성
        {
            if (string.IsNullOrWhiteSpace(runtimeId) || string.IsNullOrWhiteSpace(counterKey)) // 카운터 키 입력 확인
            {
                return string.Empty; // 잘못된 카운터 키 반환
            }

            return $"{runtimeId}:{counterKey}"; // Runtime별 고유 카운터 키 반환
        }

        private static float ResolveNow(float nowSeconds) // 현재 전투 시간 결정
        {
            return nowSeconds >= 0f ? nowSeconds : Time.time; // 테스트 주입 시간 또는 Unity 시간 반환
        }
    }
}
