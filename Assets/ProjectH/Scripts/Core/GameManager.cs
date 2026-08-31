using ProjectH.Data; // 데이터 기능
using UnityEngine; // Unity 기본 기능

namespace ProjectH.Core // 프로젝트 핵심 영역
{
    [DisallowMultipleComponent] // 중복 컴포넌트 방지
    [RequireComponent(typeof(SceneLoader))] // 씬 로더 자동 보장
    [RequireComponent(typeof(DataManager))] // 데이터 관리자 자동 보장
    public sealed class GameManager : MonoBehaviour // 게임 전역 관리자
    {
        public static GameManager Instance { get; private set; } // 전역 인스턴스
        public SceneLoader Scenes { get; private set; } // 씬 로더 참조
        public DataManager Data { get; private set; } // 데이터 관리자 참조
        public bool IsInitialized { get; private set; } // 초기화 상태

        private void Awake() // 관리자 초기 설정
        {
            if (Instance != null && Instance != this) // 기존 인스턴스 확인
            {
                Destroy(gameObject); // 중복 관리자 제거
                return; // 중복 초기화 중단
            }

            Instance = this; // 현재 인스턴스 등록
            Scenes = GetComponent<SceneLoader>(); // 씬 로더 연결
            Data = GetComponent<DataManager>(); // 데이터 관리자 연결
            DontDestroyOnLoad(gameObject); // 씬 전환 유지
            Initialize(); // 공통 초기화 실행
        }

        private void Initialize() // 공통 초기화 처리
        {
            if (IsInitialized) // 초기화 여부 확인
            {
                return; // 중복 초기화 중단
            }

            if (Data == null) // 데이터 관리자 확인
            {
                Debug.LogError("[Project H] DataManager is missing."); // 데이터 관리자 오류
                return; // 초기화 중단
            }

            Data.Initialize(); // 데이터 초기화 실행

            if (!Data.IsInitialized) // 데이터 초기화 결과 확인
            {
                Debug.LogError("[Project H] Data initialization failed."); // 데이터 초기화 실패 로그
                return; // 초기화 중단
            }

            IsInitialized = true; // 초기화 완료 기록
            Debug.Log("[Project H] GameManager initialized."); // 초기화 완료 로그
        }

        private void OnDestroy() // 관리자 해제 처리
        {
            if (Instance == this) // 현재 인스턴스 확인
            {
                Instance = null; // 전역 참조 해제
            }
        }
    }
}
