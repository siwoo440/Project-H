using System; // 문자열 분리 기능
using System.Collections.Generic; // 목록 자료형
using UnityEngine; // PlayerPrefs 기능

namespace ProjectH.Core // 프로젝트 핵심 영역
{
    public static class PlayerProfile // 회차를 넘어 유지되는 기록 (Day72 신규 — 저장 파일을 새로 만들어도 남는다)
    {
        private const string SeenEndingsKey = "ProjectH.Profile.SeenEndings"; // 본 엔딩 목록 저장 키
        private const string ClearCountKey = "ProjectH.Profile.ClearCount"; // 클리어 횟수 저장 키
        private const string SeenDialoguesKey = "ProjectH.Profile.SeenDialogues"; // 본 이야기 목록 저장 키 (Day81)
        private const char Separator = '|'; // 목록 구분자

        private static bool loaded; // 불러왔는지
        private static readonly List<string> seenEndings = new List<string>(); // 본 엔딩 ID 목록
        private static int clearCount; // 끝까지 마친 횟수
        private static readonly HashSet<string> seenDialogues = new HashSet<string>(); // 끝까지 본 이야기 ID (Day81 추가 — 다음 회차에서 빨리 넘길 수 있다)

        public static event Action Changed; // 기록이 바뀔 때 알림

        public static IReadOnlyList<string> SeenEndings { get { EnsureLoaded(); return seenEndings; } } // 본 엔딩 목록 반환

        public static int SeenEndingCount { get { EnsureLoaded(); return seenEndings.Count; } } // 본 엔딩 수 반환

        public static int ClearCount { get { EnsureLoaded(); return clearCount; } } // 클리어 횟수 반환

        public static int PlaythroughNumber { get { EnsureLoaded(); return clearCount + 1; } } // 지금이 몇 회차인지 (첫 회차 = 1)

        public static int SeenDialogueCount { get { EnsureLoaded(); return seenDialogues.Count; } } // 본 이야기 수 반환 (Day81)

        public static bool HasSeenDialogue(string scriptId) // 이 이야기를 끝까지 본 적 있는지 (Day81 추가 — 회차와 무관)
        {
            EnsureLoaded(); // 불러오기 보장
            return !string.IsNullOrEmpty(scriptId) && seenDialogues.Contains(scriptId); // 확인
        }

        public static bool RecordSeenDialogue(string scriptId) // 본 이야기 기록 (Day81 추가 — 처음 기록하면 true)
        {
            EnsureLoaded(); // 불러오기 보장
            if (string.IsNullOrEmpty(scriptId) || scriptId.IndexOf(Separator) >= 0 || !seenDialogues.Add(scriptId)) return false; // 입력 확인 · 이미 기록됨
            PlayerPrefs.SetString(SeenDialoguesKey, string.Join(Separator.ToString(), seenDialogues)); // 기록
            PlayerPrefs.Save(); // 저장
            Changed?.Invoke(); // 알림
            return true; // 새 기록
        }

        public static bool HasSeenEnding(string endingId) // 이 엔딩을 본 적 있는지
        {
            EnsureLoaded(); // 불러오기 보장
            return !string.IsNullOrEmpty(endingId) && seenEndings.Contains(endingId); // 확인
        }

        public static bool RecordEnding(string endingId) // 엔딩 기록 (처음 본 엔딩이면 true)
        {
            EnsureLoaded(); // 불러오기 보장
            if (string.IsNullOrEmpty(endingId)) return false; // 입력 확인
            clearCount++; // 클리어 횟수 증가
            PlayerPrefs.SetInt(ClearCountKey, clearCount); // 기록
            bool isNew = !seenEndings.Contains(endingId); // 처음 보는 엔딩인지

            if (isNew) // 새 엔딩
            {
                seenEndings.Add(endingId); // 목록 추가
                PlayerPrefs.SetString(SeenEndingsKey, string.Join(Separator.ToString(), seenEndings)); // 기록
            }

            PlayerPrefs.Save(); // 저장
            Changed?.Invoke(); // 알림
            return isNew; // 새 엔딩 여부 반환
        }

        public static void ResetRecords() // 기록 지우기 (테스트·초기화용)
        {
            EnsureLoaded(); // 불러오기 보장
            seenEndings.Clear(); // 목록 비움
            seenDialogues.Clear(); // 본 이야기 비움 (Day81)
            clearCount = 0; // 횟수 초기화
            PlayerPrefs.DeleteKey(SeenDialoguesKey); // 기록 삭제 (Day81)
            PlayerPrefs.DeleteKey(SeenEndingsKey); // 기록 삭제
            PlayerPrefs.DeleteKey(ClearCountKey); // 기록 삭제
            PlayerPrefs.Save(); // 저장
            Changed?.Invoke(); // 알림
        }

        private static void EnsureLoaded() // 저장된 기록 불러오기 (한 번만)
        {
            if (loaded) return; // 이미 불러옴
            loaded = true; // 표시 (아래 읽기에서 다시 들어오지 않도록 먼저)
            clearCount = Mathf.Max(0, PlayerPrefs.GetInt(ClearCountKey, 0)); // 클리어 횟수
            seenDialogues.Clear(); // 본 이야기 비움 (Day81)

            foreach (string id in PlayerPrefs.GetString(SeenDialoguesKey, string.Empty).Split(Separator)) // 본 이야기 항목 순회 (Day81)
            {
                if (!string.IsNullOrWhiteSpace(id)) seenDialogues.Add(id); // 추가
            }

            seenEndings.Clear(); // 목록 비움
            string raw = PlayerPrefs.GetString(SeenEndingsKey, string.Empty); // 저장된 목록
            if (string.IsNullOrEmpty(raw)) return; // 비어 있음

            foreach (string id in raw.Split(Separator)) // 항목 순회
            {
                if (!string.IsNullOrWhiteSpace(id) && !seenEndings.Contains(id)) seenEndings.Add(id); // 추가
            }
        }
    }
}
