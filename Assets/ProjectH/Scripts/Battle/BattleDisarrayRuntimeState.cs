using System.Collections.Generic; // 사전 자료형
using UnityEngine; // Unity 시간 및 수학 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleDisarrayRuntimeState // 흐트러짐 게이지 Runtime 저장소 (Day53 신규)
    {
        public const int BaseDisarrayPerHit = 10; // 1회 타격 기본 누적량
        public const float WeakHitMultiplier = 2.0f; // 약점 적중 누적 배율 (Day52 속성 연계)
        public const float ResistHitMultiplier = 0.5f; // 저항 적중 누적 배율
        public const float DisarrayedSeconds = 4.0f; // 흐트러짐 유지 시간
        public const float DisarrayedDamageMultiplier = 1.6f; // 흐트러진 대상이 받는 피해 배율
        public const float MaxGrowthPerDisarray = 1.25f; // 흐트러질 때마다 증가하는 최대치 배율
        public const int MinMaxGauge = 60; // 흐트러짐 최대치 하한
        public const int MaxHpDivisor = 8; // 최대 체력 대비 흐트러짐 최대치 산출 제수
        public const string DisarraySourceKey = "DISARRAY:STAGGER"; // 흐트러짐 경직 Modifier 출처 키

        private sealed class DisarrayEntry // 단일 대상 흐트러짐 상태
        {
            public int Current; // 현재 누적 수치
            public int Max; // 현재 최대치
            public float RecoverAt; // 흐트러짐 해제 전투 시간
            public bool Disarrayed; // 현재 흐트러짐 여부
            public int DisarrayCount; // 누적 흐트러짐 발생 횟수
        }

        private static readonly Dictionary<string, DisarrayEntry> entries = new Dictionary<string, DisarrayEntry>(); // Runtime ID별 흐트러짐 상태 목록

        public static void ResetAll() // 흐트러짐 상태 전체 초기화
        {
            entries.Clear(); // 등록 흐트러짐 상태 전체 제거
        }

        public static void Register(string runtimeId, int maxHp) // 대상 흐트러짐 게이지 등록 (스탯 생성 시 최대치 산출)
        {
            if (string.IsNullOrWhiteSpace(runtimeId)) // Runtime ID 확인
            {
                return; // 잘못된 등록 중단
            }

            entries[runtimeId] = new DisarrayEntry // 신규 흐트러짐 상태 생성 또는 갱신
            {
                Current = 0, // 누적 수치 초기화
                Max = CalculateMaxGauge(maxHp), // 최대 체력 비례 최대치 산출
                RecoverAt = 0f, // 해제 시간 초기화
                Disarrayed = false, // 흐트러짐 상태 초기화
                DisarrayCount = 0 // 흐트러짐 횟수 초기화
            };
        }

        public static void Unregister(string runtimeId) // 대상 흐트러짐 게이지 등록 해제 (Day55 추가, 새 스탯 생성 시 이전 잔여 상태 제거)
        {
            if (!string.IsNullOrWhiteSpace(runtimeId)) // Runtime ID 확인
            {
                entries.Remove(runtimeId); // 등록 상태 제거
            }
        }

        public static int CalculateMaxGauge(int maxHp) // 최대 체력 기반 흐트러짐 최대치 계산
        {
            return Mathf.Max(MinMaxGauge, Mathf.RoundToInt(Mathf.Max(0, maxHp) / (float)MaxHpDivisor)); // 하한 적용 최대치 반환
        }

        public static bool IsRegistered(string runtimeId) // 흐트러짐 게이지 등록 여부 확인
        {
            return !string.IsNullOrWhiteSpace(runtimeId) && entries.ContainsKey(runtimeId); // 등록 여부 반환
        }

        public static int GetMaxGauge(string runtimeId) // 현재 흐트러짐 최대치 조회
        {
            return TryGet(runtimeId, out DisarrayEntry entry) ? entry.Max : 0; // 미등록 대상 0 반환
        }

        public static int GetCurrentGauge(string runtimeId) // 현재 흐트러짐 누적 수치 조회
        {
            return TryGet(runtimeId, out DisarrayEntry entry) ? entry.Current : 0; // 미등록 대상 0 반환
        }

        public static int GetDisarrayCount(string runtimeId) // 누적 흐트러짐 발생 횟수 조회
        {
            return TryGet(runtimeId, out DisarrayEntry entry) ? entry.DisarrayCount : 0; // 미등록 대상 0 반환
        }

        public static float GetGaugeRatio(string runtimeId, float nowSeconds = -1f) // 흐트러짐 게이지 표시 비율 조회
        {
            if (!TryGet(runtimeId, out DisarrayEntry entry)) // 등록 상태 확인
            {
                return 0f; // 미등록 대상 빈 게이지 반환
            }

            RefreshRecovery(entry, ResolveNow(nowSeconds)); // 흐트러짐 해제 시각 확인

            if (entry.Disarrayed) // 흐트러짐 유지 중 확인
            {
                return 1f; // 흐트러짐 중에는 가득 찬 게이지 반환
            }

            return entry.Max <= 0 ? 0f : Mathf.Clamp01(entry.Current / (float)entry.Max); // 누적 비율 반환
        }

        public static bool IsDisarrayed(string runtimeId, float nowSeconds = -1f) // 현재 흐트러짐 상태 확인
        {
            if (!TryGet(runtimeId, out DisarrayEntry entry)) // 등록 상태 확인
            {
                return false; // 미등록 대상 흐트러짐 아님 반환
            }

            RefreshRecovery(entry, ResolveNow(nowSeconds)); // 흐트러짐 해제 시각 확인
            return entry.Disarrayed; // 현재 흐트러짐 여부 반환
        }

        public static float GetRemainingSeconds(string runtimeId, float nowSeconds = -1f) // 흐트러짐 잔여 시간 조회
        {
            if (!IsDisarrayed(runtimeId, nowSeconds)) // 흐트러짐 상태 확인
            {
                return 0f; // 흐트러짐 아님 잔여 시간 0 반환
            }

            TryGet(runtimeId, out DisarrayEntry entry); // 흐트러짐 상태 조회
            return Mathf.Max(0f, entry.RecoverAt - ResolveNow(nowSeconds)); // 잔여 시간 반환
        }

        public static int GetHitAmount(BattleElementAffinity affinity) // 속성 상성 기반 1회 타격 누적량 계산 (Day52 연계)
        {
            switch (affinity) // 상성 판정 분기
            {
                case BattleElementAffinity.Weak: // 약점 적중 처리
                    return Mathf.RoundToInt(BaseDisarrayPerHit * WeakHitMultiplier); // 약점 2배 누적량 반환
                case BattleElementAffinity.Resist: // 저항 적중 처리
                    return Mathf.RoundToInt(BaseDisarrayPerHit * ResistHitMultiplier); // 저항 절반 누적량 반환
                default: // 상성 없음 처리
                    return BaseDisarrayPerHit; // 기본 누적량 반환
            }
        }

        public static bool AddDisarray(string runtimeId, int amount, float nowSeconds = -1f) // 흐트러짐 누적 및 발생 판정 (새로 흐트러지면 true)
        {
            if (!TryGet(runtimeId, out DisarrayEntry entry) || amount <= 0) // 등록 상태 및 누적량 확인
            {
                return false; // 누적 불가 반환
            }

            float now = ResolveNow(nowSeconds); // 현재 전투 시간 계산
            RefreshRecovery(entry, now); // 흐트러짐 해제 시각 확인

            if (entry.Disarrayed) // 이미 흐트러진 상태 확인
            {
                return false; // 흐트러짐 중 추가 누적 무시 (무한 연장 방지)
            }

            entry.Current += amount; // 흐트러짐 수치 누적

            if (entry.Current < entry.Max) // 최대치 도달 여부 확인
            {
                return false; // 흐트러짐 미발생 반환
            }

            entry.Current = entry.Max; // 누적 수치 최대치 고정
            entry.Disarrayed = true; // 흐트러짐 상태 적용
            entry.RecoverAt = now + DisarrayedSeconds; // 흐트러짐 해제 시각 설정
            entry.DisarrayCount++; // 흐트러짐 발생 횟수 증가
            return true; // 신규 흐트러짐 발생 반환
        }

        public static int RecoverDisarray(string runtimeId, float ratio, float nowSeconds = -1f) // 흐트러짐 누적 비율 회복 (Day54 추가, 보스 패턴용 — 실제 감소량 반환)
        {
            if (!TryGet(runtimeId, out DisarrayEntry entry) || ratio <= 0f) // 등록 상태 및 회복 비율 확인
            {
                return 0; // 회복 불가 반환
            }

            RefreshRecovery(entry, ResolveNow(nowSeconds)); // 흐트러짐 해제 시각 확인

            if (entry.Disarrayed) // 흐트러짐 유지 중 확인
            {
                return 0; // 흐트러진 동안에는 회복 불가 반환
            }

            int reduced = Mathf.RoundToInt(entry.Current * Mathf.Clamp01(ratio)); // 현재 누적 대비 회복량 계산
            entry.Current = Mathf.Max(0, entry.Current - reduced); // 누적 수치 감소 적용
            return reduced; // 실제 감소량 반환
        }

        public static float GetDamageMultiplier(string runtimeId, float nowSeconds = -1f) // 흐트러짐 상태 기반 받는 피해 배율 조회
        {
            return IsDisarrayed(runtimeId, nowSeconds) ? DisarrayedDamageMultiplier : 1f; // 흐트러짐 중 추가 피해 배율 반환
        }

        private static void RefreshRecovery(DisarrayEntry entry, float now) // 흐트러짐 해제 시각 도달 처리
        {
            if (!entry.Disarrayed || now < entry.RecoverAt) // 흐트러짐 유지 여부 확인
            {
                return; // 해제 처리 중단
            }

            entry.Disarrayed = false; // 흐트러짐 상태 해제
            entry.Current = 0; // 누적 수치 초기화
            entry.Max = Mathf.RoundToInt(entry.Max * MaxGrowthPerDisarray); // 다음 흐트러짐 최대치 증가 (무한 흐트러짐 방지)
        }

        private static bool TryGet(string runtimeId, out DisarrayEntry entry) // Runtime ID 기반 흐트러짐 상태 조회
        {
            entry = null; // 조회 결과 초기화

            return !string.IsNullOrWhiteSpace(runtimeId) && entries.TryGetValue(runtimeId, out entry); // 등록 상태 조회 결과 반환
        }

        private static float ResolveNow(float nowSeconds) // 테스트 또는 Runtime 현재 시간 계산
        {
            return nowSeconds >= 0f ? nowSeconds : Time.time; // 명시 시간 또는 Unity 전투 시간 반환
        }
    }
}
