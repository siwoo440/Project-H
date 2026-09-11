using System; // 문자열 비교 기능
using System.Collections.Generic; // 목록 자료형

namespace ProjectH.Dialogue // 프로젝트 대화 영역
{
    public enum DialogueStageSlot // 스탠딩 자리 (Day62 신규)
    {
        None = 0, // 무대에 없음
        Center = 1, // 가운데 (1인 대화)
        Left = 2, // 왼쪽 (2인 대화)
        Right = 3 // 오른쪽 (2인 대화)
    }

    public sealed class DialogueStageLayout // 대화 스탠딩 배치 (Day62 신규 — 목업 1번 2인 좌우 배치, 기존 대사 파일은 수정 없이 자동 배치)
    {
        public string Left { get; } // 왼쪽 캐릭터 (없으면 빈 문자열)
        public string Right { get; } // 오른쪽 캐릭터
        public string Center { get; } // 가운데 캐릭터 (1인일 때)

        private DialogueStageLayout(string left, string right, string center) // 배치 생성
        {
            Left = left ?? string.Empty; // 왼쪽 저장
            Right = right ?? string.Empty; // 오른쪽 저장
            Center = center ?? string.Empty; // 가운데 저장
        }

        public static DialogueStageLayout Build(DialogueScript script) // 대화 파일로 배치 결정
        {
            if (script == null) return new DialogueStageLayout(null, null, null); // 대화 없음

            if (!string.IsNullOrEmpty(script.LeftCharacterId) || !string.IsNullOrEmpty(script.RightCharacterId)) // 직접 지정 우선
            {
                return new DialogueStageLayout(script.LeftCharacterId, script.RightCharacterId, null); // 지정 배치
            }

            List<string> cast = new List<string>(); // 등장 순서 캐릭터
            AddCast(cast, script.CharacterId); // 대화 주인공 먼저

            foreach (DialogueNode node in script.Nodes) // 노드 순회
            {
                if (node != null) AddCast(cast, node.Speaker); // 말하는 캐릭터 추가
            }

            if (cast.Count == 0) return new DialogueStageLayout(null, null, null); // 캐릭터 없음 (주인공·나레이션만)
            if (cast.Count == 1) return new DialogueStageLayout(null, null, cast[0]); // 1인 → 가운데
            return new DialogueStageLayout(cast[0], cast[1], null); // 2인 이상 → 앞의 두 명 좌우 (세 번째부터는 스탠딩 없이 이름표만)
        }

        public DialogueStageSlot GetSlot(string characterId) // 캐릭터 자리 조회
        {
            if (string.IsNullOrEmpty(characterId)) return DialogueStageSlot.None; // 빈 ID
            if (string.Equals(characterId, Center, StringComparison.Ordinal)) return DialogueStageSlot.Center; // 가운데
            if (string.Equals(characterId, Left, StringComparison.Ordinal)) return DialogueStageSlot.Left; // 왼쪽
            if (string.Equals(characterId, Right, StringComparison.Ordinal)) return DialogueStageSlot.Right; // 오른쪽
            return DialogueStageSlot.None; // 무대에 없음
        }

        public static bool IsCharacterSpeaker(string speaker) => !string.IsNullOrEmpty(speaker) && speaker != DialogueSpeakers.Hero && speaker != DialogueSpeakers.Narration; // 스탠딩이 있는 화자인지 (주인공·나레이션 제외)

        private static void AddCast(List<string> cast, string speaker) // 중복 없이 추가
        {
            if (IsCharacterSpeaker(speaker) && !cast.Contains(speaker)) cast.Add(speaker); // 캐릭터 화자만
        }
    }
}
