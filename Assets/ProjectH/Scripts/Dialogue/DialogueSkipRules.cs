using ProjectH.Core; // 회차 기록 기능
using ProjectH.Diary; // 본 이야기 기록 기능
using ProjectH.SaveSystem; // 저장 데이터 기능

namespace ProjectH.Dialogue // 프로젝트 대화 영역
{
    public static class DialogueSkipRules // 대사 빨리 넘기기 규칙 (Day81 신규 — 이미 본 이야기는 Ctrl로 빨리 넘기고, 건너뛰기 확인 창도 생략한다)
    {
        public const float StepSeconds = 0.05f; // 빨리 넘길 때 한 줄에 머무는 시간 (1초에 20줄)

        public static bool IsSeen(SaveData saveData, string scriptId) // 이미 본 이야기인지 : 이번 회차의 기록 → 지난 회차의 기록
        {
            if (string.IsNullOrEmpty(scriptId)) return false; // 대사 ID 없음
            return DiaryService.IsDialogueSeen(saveData, scriptId) || PlayerProfile.HasSeenDialogue(scriptId); // 둘 중 하나라도 있으면 본 이야기
        }

        public static bool CanFastForward(bool seen, bool allowUnread) => seen || allowUnread; // 빨리 넘길 수 있는지 (처음 보는 이야기는 설정에서 허용했을 때만)

        public static bool NeedsSkipConfirm(bool seen) => !seen; // 건너뛰기 전에 확인 창을 띄울지 (처음 보는 이야기만 — 실수로 날리지 않게)

        public static bool Tick(ref float timer, float delta) // 누르고 있는 동안 시간을 쌓아 한 줄 넘길 때가 되면 true (프레임이 길어도 한 번에 한 줄)
        {
            timer += delta < 0f ? 0f : delta; // 시간 누적
            if (timer < StepSeconds) return false; // 아직
            timer = 0f; // 다음 줄을 위해 처음부터
            return true; // 한 줄 넘김
        }

        public static string GetHint(bool seen, bool allowUnread) // 화면 구석에 보여 줄 안내 (빨리 넘길 수 없으면 빈 문자열)
        {
            if (seen) return "이미 본 이야기 · Ctrl 빨리 넘기기"; // 본 이야기
            return allowUnread ? "Ctrl 빨리 넘기기" : string.Empty; // 처음 보는 이야기
        }
    }
}
