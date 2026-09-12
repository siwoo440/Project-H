using ProjectH.Battle; // 던전 클리어 기록 기능
using ProjectH.SaveSystem; // 저장 기능

namespace ProjectH.Story // 프로젝트 메인 스토리 영역
{
    public static class ChapterService // 메인 스토리 진행 (Day68 신규 — 이야기 보기 → 던전 클리어 → 다음 이야기)
    {
        public static void EnsureStarted(SaveData saveData) // 스토리 시작 보장 (기존 세이브는 이미 지난 것으로 처리)
        {
            if (saveData == null || saveData.ChapterProgress.Started) return; // 이미 시작
            if (IsLegacySave(saveData)) saveData.ChapterProgress.Finish(); // 기존 진행 세이브는 스토리를 건너뜀 (동료·진행 그대로 유지)
            else saveData.ChapterProgress.Begin(ChapterCatalog.PrologueId); // 새 게임은 프롤로그부터
            UpdateSummary(saveData); // 로비 안내 갱신
        }

        public static bool IsLegacySave(SaveData saveData) // 68일차 이전에 시작한 저장인지 (동료가 여럿이거나 이미 진행한 흔적)
        {
            if (saveData == null) return false; // 입력 확인
            return saveData.Characters.Count > 1 || saveData.CurrentDay > 1 || DungeonProgressSaveAdapter.IsCleared(saveData, "DG001"); // 판정
        }

        public static ChapterDefinition GetCurrentChapter(SaveData saveData) // 현재 챕터 (없으면 null)
        {
            return saveData == null ? null : ChapterCatalog.Find(saveData.ChapterProgress.ChapterId); // 조회
        }

        public static ChapterStep GetCurrentStep(SaveData saveData) // 현재 단계 (없으면 null)
        {
            ChapterDefinition chapter = GetCurrentChapter(saveData); // 챕터
            int index = saveData == null ? 0 : saveData.ChapterProgress.StepIndex; // 단계 번호
            return chapter == null || index >= chapter.Steps.Count ? null : chapter.Steps[index]; // 단계 반환
        }

        public static string GetPendingDialogueId(SaveData saveData) // 지금 자동으로 재생할 이야기 (없으면 빈 값)
        {
            ChapterStep step = GetCurrentStep(saveData); // 현재 단계
            return step != null && step.Kind == ChapterStepKind.Dialogue ? step.Target : string.Empty; // 대사 ID 반환
        }

        public static string GetProgressText(SaveData saveData) // 로비 표시 문구 (챕터 · 다음 할 일)
        {
            ChapterDefinition chapter = GetCurrentChapter(saveData); // 챕터
            ChapterStep step = GetCurrentStep(saveData); // 단계
            if (chapter == null || step == null) return "메인 스토리 · 준비된 이야기를 모두 보았습니다 (69일차 챕터 3~5 예정)"; // 완료
            return $"{chapter.Title} · 다음: {step.Summary}"; // 진행 중
        }

        public static string CompleteDialogue(SaveData saveData, string scriptId) // 이야기를 끝까지 본 뒤 진행 (합류 안내 반환)
        {
            ChapterStep step = GetCurrentStep(saveData); // 현재 단계
            if (step == null || step.Kind != ChapterStepKind.Dialogue || step.Target != scriptId) return string.Empty; // 현재 단계가 아님
            return Advance(saveData, step); // 다음 단계로
        }

        public static string NotifyDungeonCleared(SaveData saveData, string dungeonId) // 던전 클리어 반영 (합류 안내 반환)
        {
            EnsureStarted(saveData); // 시작 보장
            ChapterStep step = GetCurrentStep(saveData); // 현재 단계
            if (step == null || step.Kind != ChapterStepKind.ClearDungeon || step.Target != dungeonId) return string.Empty; // 현재 단계가 아님
            return Advance(saveData, step); // 다음 단계로
        }

        public static void Refresh(SaveData saveData) // 화면을 열 때 진행 상황 정리 (이미 깬 던전 단계는 자동 통과)
        {
            EnsureStarted(saveData); // 시작 보장
            int guard = 0; // 무한 반복 방지

            while (guard++ < 32) // 연속 통과 처리
            {
                ChapterStep step = GetCurrentStep(saveData); // 현재 단계
                if (step == null || step.Kind != ChapterStepKind.ClearDungeon || !DungeonProgressSaveAdapter.IsCleared(saveData, step.Target)) break; // 통과 대상 없음
                Advance(saveData, step); // 이미 깬 던전은 통과
            }

            UpdateSummary(saveData); // 안내 갱신
        }

        private static string Advance(SaveData saveData, ChapterStep step) // 단계 완료 처리 (동료 합류 · 다음 단계 · 안내 갱신)
        {
            string message = string.Empty; // 합류 안내

            if (!string.IsNullOrEmpty(step.GrantCharacterId) && saveData.AddCharacterInternal(step.GrantCharacterId)) // 동료 합류
            {
                message = $"{step.GrantCharacterId} 합류"; // 안내 (표시용 이름은 화면에서 변환)
            }

            ChapterDefinition chapter = GetCurrentChapter(saveData); // 현재 챕터
            int nextIndex = saveData.ChapterProgress.StepIndex + 1; // 다음 단계

            if (chapter != null && nextIndex < chapter.Steps.Count) // 같은 챕터 안
            {
                saveData.ChapterProgress.MoveTo(chapter.Id, nextIndex); // 다음 단계
            }
            else // 챕터 종료
            {
                ChapterDefinition next = chapter == null ? null : ChapterCatalog.GetNext(chapter.Id); // 다음 챕터
                if (next == null) saveData.ChapterProgress.Finish(); // 준비된 이야기 끝
                else saveData.ChapterProgress.MoveTo(next.Id, 0); // 다음 챕터 첫 단계
            }

            UpdateSummary(saveData); // 안내 갱신
            return message; // 합류 안내 반환
        }

        private static void UpdateSummary(SaveData saveData) // 저장의 챕터·목표 문구 갱신 (로비 상단 표시)
        {
            ChapterDefinition chapter = GetCurrentChapter(saveData); // 챕터
            ChapterStep step = GetCurrentStep(saveData); // 단계
            saveData.SetCurrentChapter(chapter == null ? "메인 스토리 완료" : chapter.Title); // 챕터 문구
            saveData.SetCurrentMainQuest(step == null ? "69일차 챕터 3~5에서 이어집니다" : step.Summary); // 목표 문구
        }
    }
}
