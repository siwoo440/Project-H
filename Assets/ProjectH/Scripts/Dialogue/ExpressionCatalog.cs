using System; // 문자열 비교 기능
using System.Collections.Generic; // 사전·목록 자료형

namespace ProjectH.Dialogue // 프로젝트 대화 영역
{
    public static class ExpressionCatalog // 표정 연결표 (Day76 신규 — 대사에 적힌 표정 이름 12종 → 그림 파일의 표정 키 7종)
    {
        public const string Normal = "normal"; // 기본 얼굴
        public const string Smile = "smile"; // 웃는 얼굴
        public const string Serious = "serious"; // 진지한 얼굴
        public const string Shy = "shy"; // 수줍은 얼굴
        public const string Surprised = "surprised"; // 놀란 얼굴
        public const string Sad = "sad"; // 슬픈 얼굴
        public const string Angry = "angry"; // 화난 얼굴

        private static readonly string[] KeyList = { Normal, Smile, Serious, Shy, Surprised, Sad, Angry }; // 그림 표정 키 7종 (대사에서 많이 쓰는 순서)

        private static readonly Dictionary<string, string> NameToKey = new Dictionary<string, string>(StringComparer.Ordinal) // 대사 표정 이름 → 그림 표정 키
        {
            { "기본", Normal }, // 평소 얼굴
            { "미소", Smile }, // 웃음
            { "흥미", Smile }, // 관심이 생긴 얼굴 → 웃음
            { "진지", Serious }, // 진지
            { "각오", Serious }, // 마음을 굳힌 얼굴 → 진지
            { "생각", Serious }, // 생각에 잠긴 얼굴 → 진지
            { "경계", Serious }, // 경계하는 얼굴 → 진지
            { "부끄러움", Shy }, // 수줍음
            { "놀람", Surprised }, // 놀람
            { "당황", Surprised }, // 당황 → 놀람
            { "슬픔", Sad }, // 슬픔
            { "상처", Sad }, // 상처받은 얼굴 → 슬픔
            { "화남", Angry }, // 화남 (아직 대사에서 쓰지 않음 — 앞으로 쓸 자리)
            { "분노", Angry } // 분노 → 화남
        };

        private static readonly Dictionary<string, string> KeyToLabel = new Dictionary<string, string>(StringComparer.Ordinal) // 그림 표정 키 → 한글 이름 (점검 표 표시용)
        {
            { Normal, "기본" }, // 기본
            { Smile, "웃음" }, // 웃음
            { Serious, "진지" }, // 진지
            { Shy, "수줍음" }, // 수줍음
            { Surprised, "놀람" }, // 놀람
            { Sad, "슬픔" }, // 슬픔
            { Angry, "화남" } // 화남
        };

        public static IReadOnlyList<string> Keys => KeyList; // 그림 표정 키 목록
        public static IEnumerable<string> Names => NameToKey.Keys; // 대사에서 쓸 수 있는 표정 이름 목록

        public static bool IsKey(string value) // 그림 표정 키인지 (대소문자 무시)
        {
            if (string.IsNullOrEmpty(value)) return false; // 빈 값
            string lowered = value.Trim().ToLowerInvariant(); // 소문자로 통일

            for (int index = 0; index < KeyList.Length; index++) // 키 순회
            {
                if (KeyList[index] == lowered) return true; // 일치
            }

            return false; // 키 아님
        }

        public static bool IsKnown(string expression) // 연결표가 아는 표정인지 (빈 값·한글 이름·그림 키)
        {
            if (string.IsNullOrWhiteSpace(expression)) return true; // 빈 값은 '표정 유지'
            return NameToKey.ContainsKey(expression.Trim()) || IsKey(expression); // 이름 또는 키
        }

        public static string Resolve(string expression) // 대사 표정 → 그림 표정 키 (모르는 이름은 기본)
        {
            if (string.IsNullOrWhiteSpace(expression)) return Normal; // 빈 값은 기본
            string trimmed = expression.Trim(); // 앞뒤 공백 제거
            if (NameToKey.TryGetValue(trimmed, out string key)) return key; // 한글 이름
            return IsKey(trimmed) ? trimmed.ToLowerInvariant() : Normal; // 그림 키는 그대로, 나머지는 기본
        }

        public static string GetLabel(string key) // 그림 표정 키의 한글 이름
        {
            return key != null && KeyToLabel.TryGetValue(key, out string label) ? label : string.Empty; // 없으면 빈 문자열
        }
    }
}
