using System; // 이벤트 대리자 기능
using System.Collections.Generic; // 사전 자료형
using UnityEngine; // Unity 런타임 기능

namespace ProjectH.Events // 프로젝트 이벤트 영역
{
    public readonly struct StoryFlagChangedEvent // 스토리 플래그 변경 신호
    {
        public string FlagId { get; } // 플래그 ID
        public bool IsActive { get; } // 활성 상태

        public StoryFlagChangedEvent(string flagId, bool isActive) // 플래그 신호 생성
        {
            FlagId = flagId; // 플래그 ID 설정
            IsActive = isActive; // 활성 상태 설정
        }
    }

    public enum SaveLifecycleType // 저장 생명주기 종류
    {
        NewGameCreated = 0, // 새 게임 생성
        Saved = 1, // 저장 완료
        Loaded = 2, // 불러오기 완료
        Deleted = 3 // 저장 삭제
    }

    public readonly struct SaveLifecycleEvent // 저장 생명주기 신호
    {
        public SaveLifecycleType Type { get; } // 생명주기 종류

        public SaveLifecycleEvent(SaveLifecycleType type) // 저장 신호 생성
        {
            Type = type; // 생명주기 종류 설정
        }
    }

    public static class ProjectHEventBus // 프로젝트 공통 이벤트 버스
    {
        private static readonly Dictionary<Type, Delegate> Listeners = new Dictionary<Type, Delegate>(); // 이벤트 구독 목록

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 플레이 시작 초기화
        private static void Reset() // 이벤트 버스 초기화
        {
            Listeners.Clear(); // 모든 구독 제거
        }

        public static void Subscribe<T>(Action<T> listener) // 이벤트 구독
        {
            if (listener == null) // 구독자 확인
            {
                return; // 구독 중단
            }

            Type eventType = typeof(T); // 이벤트 자료형 조회

            if (Listeners.TryGetValue(eventType, out Delegate existing)) // 기존 구독 확인
            {
                Listeners[eventType] = Delegate.Combine(existing, listener); // 기존 구독에 추가
                return; // 구독 완료
            }

            Listeners[eventType] = listener; // 첫 구독 등록
        }

        public static void Unsubscribe<T>(Action<T> listener) // 이벤트 구독 해제
        {
            if (listener == null) // 구독자 확인
            {
                return; // 해제 중단
            }

            Type eventType = typeof(T); // 이벤트 자료형 조회

            if (!Listeners.TryGetValue(eventType, out Delegate existing)) // 기존 구독 확인
            {
                return; // 해제 대상 없음
            }

            Delegate remaining = Delegate.Remove(existing, listener); // 대상 구독 제거

            if (remaining == null) // 남은 구독 확인
            {
                Listeners.Remove(eventType); // 이벤트 항목 제거
                return; // 해제 완료
            }

            Listeners[eventType] = remaining; // 남은 구독 저장
        }

        public static void Publish<T>(T message) // 이벤트 발행
        {
            Type eventType = typeof(T); // 이벤트 자료형 조회

            if (!Listeners.TryGetValue(eventType, out Delegate existing)) // 구독 존재 확인
            {
                return; // 발행 종료
            }

            Action<T> callback = existing as Action<T>; // 구독 콜백 변환
            callback?.Invoke(message); // 이벤트 전달
        }
    }
}
