using System; // 숫자 범위 기능
using UnityEngine; // 직렬화 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    [Serializable] // 저장 직렬화 허용
    public sealed class ChapterProgressSaveData // 메인 스토리 진행 (Day68 신규 — 어느 챕터의 몇 번째 단계인지)
    {
        [SerializeField] private bool started; // 스토리 시작 여부 (기존 세이브 구분용)
        [SerializeField] private string chapterId = string.Empty; // 현재 챕터 ID (빈 값 = 모든 챕터 완료)
        [SerializeField] private int stepIndex; // 현재 단계 번호

        public bool Started => started; // 시작 여부 반환
        public string ChapterId => chapterId ?? string.Empty; // 챕터 반환
        public int StepIndex => Math.Max(0, stepIndex); // 단계 반환
        public bool IsFinished => started && string.IsNullOrEmpty(ChapterId); // 모든 챕터 완료 여부

        public void Begin(string firstChapterId) // 스토리 시작
        {
            started = true; // 시작 기록
            chapterId = firstChapterId ?? string.Empty; // 첫 챕터 저장
            stepIndex = 0; // 첫 단계
        }

        public void MoveTo(string nextChapterId, int nextStepIndex) // 챕터·단계 이동
        {
            started = true; // 시작 기록
            chapterId = nextChapterId ?? string.Empty; // 챕터 저장
            stepIndex = Math.Max(0, nextStepIndex); // 단계 저장
        }

        public void Finish() // 준비된 챕터를 모두 본 상태
        {
            started = true; // 시작 기록
            chapterId = string.Empty; // 챕터 비움
            stepIndex = 0; // 단계 초기화
        }

        public void EnsureDefaults() // 이전 저장 기본값 복원
        {
            if (chapterId == null) chapterId = string.Empty; // 챕터 복원
            stepIndex = Math.Max(0, stepIndex); // 단계 보정
        }
    }
}
