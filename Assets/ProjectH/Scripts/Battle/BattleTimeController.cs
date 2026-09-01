using UnityEngine; // Unity 시간 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleSpeedRules // 지원 전투 속도 규칙
    {
        public static float GetNext(float currentSpeed) // 다음 전투 속도 계산
        {
            float normalized = Normalize(currentSpeed); // 현재 속도 지원 값 보정

            if (normalized < 1.25f) // 현재 1배속 여부 확인
            {
                return 1.5f; // 다음 1.5배속 반환
            }

            if (normalized < 1.75f) // 현재 1.5배속 여부 확인
            {
                return 2f; // 다음 2배속 반환
            }

            return 1f; // 다음 1배속 반환
        }

        public static float Normalize(float speed) // 지원 전투 속도 보정
        {
            if (speed < 1.25f) // 1배속 근접 값 확인
            {
                return 1f; // 1배속 반환
            }

            if (speed < 1.75f) // 1.5배속 근접 값 확인
            {
                return 1.5f; // 1.5배속 반환
            }

            return 2f; // 2배속 반환
        }

        public static string GetLabel(float speed) // 전투 속도 표시 문구 반환
        {
            float normalized = Normalize(speed); // 전투 속도 지원 값 보정

            if (Mathf.Approximately(normalized, 1.5f)) // 1.5배속 여부 확인
            {
                return "×1.5"; // 1.5배속 문구 반환
            }

            if (Mathf.Approximately(normalized, 2f)) // 2배속 여부 확인
            {
                return "×2"; // 2배속 문구 반환
            }

            return "×1"; // 1배속 문구 반환
        }
    }

    [DisallowMultipleComponent] // 중복 전투 시간 컨트롤러 방지
    public sealed class BattleTimeController : MonoBehaviour // 전투 배속 및 일시정지 컨트롤러
    {
        [SerializeField] private Button speedButton; // 전투 속도 변경 버튼
        [SerializeField] private Text speedText; // 전투 속도 표시 텍스트
        private bool buttonBound; // 속도 버튼 이벤트 연결 상태
        public float CurrentSpeed { get; private set; } = 1f; // 현재 요청 전투 속도
        public bool IsPaused { get; private set; } // 현재 일시정지 상태

        public void Configure(Button speedTarget, Text labelTarget) // 전투 시간 UI 참조 설정
        {
            speedButton = speedTarget; // 전투 속도 버튼 연결
            speedText = labelTarget; // 전투 속도 텍스트 연결
            RefreshLabel(); // 현재 전투 속도 문구 갱신
        }

        private void Start() // 전투 시간 컨트롤러 시작
        {
            CurrentSpeed = 1f; // 시작 전투 속도 1배 설정
            IsPaused = false; // 시작 일시정지 해제
            BindButton(); // 전투 속도 버튼 이벤트 연결
            ApplyTimeScale(); // 시작 전투 시간 배율 적용
            RefreshLabel(); // 시작 전투 속도 문구 갱신
        }

        public void CycleSpeed() // 전투 속도 순환 변경
        {
            CurrentSpeed = BattleSpeedRules.GetNext(CurrentSpeed); // 다음 지원 전투 속도 적용
            ApplyTimeScale(); // 변경된 전투 시간 배율 적용
            RefreshLabel(); // 변경된 전투 속도 문구 갱신
        }

        public void Pause() // 전투 일시정지
        {
            SetPaused(true); // 전투 일시정지 상태 적용
        }

        public void Resume() // 전투 일시정지 해제
        {
            SetPaused(false); // 전투 진행 상태 적용
        }

        public void SetPaused(bool paused) // 전투 일시정지 상태 설정
        {
            IsPaused = paused; // 전투 일시정지 상태 저장
            ApplyTimeScale(); // 전투 시간 배율 재적용
            RefreshLabel(); // 전투 속도 문구 갱신
        }

        public void SetInteractable(bool interactable) // 전투 속도 버튼 입력 상태 설정
        {
            if (speedButton != null) // 전투 속도 버튼 확인
            {
                speedButton.interactable = interactable && !IsPaused; // 일시정지 고려 속도 버튼 입력 설정
            }
        }

        public void ResetTimeScale() // 전투 시간 배율 기본값 복원
        {
            CurrentSpeed = 1f; // 요청 전투 속도 1배 복원
            IsPaused = false; // 일시정지 상태 해제
            Time.timeScale = 1f; // Unity 시간 배율 기본값 복원
            RefreshLabel(); // 전투 속도 문구 갱신
        }

        private void BindButton() // 전투 속도 버튼 이벤트 연결
        {
            if (buttonBound || speedButton == null) // 기존 연결 및 버튼 존재 확인
            {
                return; // 중복 속도 버튼 연결 중단
            }

            speedButton.onClick.AddListener(CycleSpeed); // 전투 속도 순환 이벤트 연결
            buttonBound = true; // 전투 속도 버튼 연결 완료 기록
        }

        private void ApplyTimeScale() // 현재 전투 시간 배율 적용
        {
            float safeSpeed = BattleSpeedRules.Normalize(CurrentSpeed); // 지원 전투 속도 보정
            Time.timeScale = IsPaused ? 0f : safeSpeed; // 일시정지 또는 전투 속도 적용
        }

        private void RefreshLabel() // 전투 속도 문구 갱신
        {
            if (speedText == null) // 전투 속도 텍스트 확인
            {
                return; // 전투 속도 문구 갱신 중단
            }

            speedText.text = IsPaused ? "PAUSE" : BattleSpeedRules.GetLabel(CurrentSpeed); // 일시정지 또는 배속 문구 적용
        }

        private void OnDestroy() // 전투 시간 컨트롤러 제거 처리
        {
            if (buttonBound && speedButton != null) // 전투 속도 버튼 연결 여부 확인
            {
                speedButton.onClick.RemoveListener(CycleSpeed); // 전투 속도 버튼 이벤트 해제
            }

            Time.timeScale = 1f; // Scene 종료 시 Unity 시간 배율 복원
            buttonBound = false; // 전투 속도 버튼 연결 상태 초기화
        }
    }
}
