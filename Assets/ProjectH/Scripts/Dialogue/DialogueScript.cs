using System; // 직렬화·문자열 비교 기능
using System.Collections.Generic; // 목록 자료형
using UnityEngine; // JSON 직렬화 속성 기능

namespace ProjectH.Dialogue // 프로젝트 대화 영역 (Day58 신규)
{
    public static class DialogueSpeakers // 특수 화자 ID (Day58 신규)
    {
        public const string Hero = "HERO"; // 주인공
        public const string Narration = "NARRATION"; // 나레이션 (이름표 없음)
        public const string HeroLabel = "나"; // 주인공 이름표 (이름 입력은 프롤로그 일차에서 연결)
        public const string EndNodeId = "END"; // 대화 종료 지정 ID
    }

    [Serializable] // JSON 직렬화 허용
    public sealed class DialogueChoice // 선택지 1개 (Day58 신규)
    {
        [SerializeField] private string text; // 선택지 문구
        [SerializeField] private int affinity; // 선택 시 호감도 변화
        [SerializeField] private bool preferred; // 캐릭터 선호 성향 선택지 여부 (기획서 9.11)
        [SerializeField] private string next; // 선택 후 이동할 노드 ID (비우면 다음 노드)
        public string Text => text ?? string.Empty; // 선택지 문구 반환
        public int Affinity => affinity; // 호감도 변화 반환
        public bool Preferred => preferred; // 선호 선택지 여부 반환
        public string Next => next ?? string.Empty; // 이동 노드 ID 반환
    }

    [Serializable] // JSON 직렬화 허용
    public sealed class DialogueNode // 대사 1줄 (Day58 신규)
    {
        [SerializeField] private string id; // 노드 ID
        [SerializeField] private string speaker; // 화자 ID (캐릭터 ID·HERO·NARRATION)
        [SerializeField] private string expression; // 표정 이름 (기획서 12.3)
        [SerializeField] private string text; // 대사
        [SerializeField] private string next; // 다음 노드 ID (비우면 바로 아래 노드)
        [SerializeField] private List<DialogueChoice> choices = new List<DialogueChoice>(); // 선택지 목록
        public string Id => id ?? string.Empty; // 노드 ID 반환
        public string Speaker => string.IsNullOrEmpty(speaker) ? DialogueSpeakers.Narration : speaker; // 화자 반환 (비우면 나레이션)
        public string Expression => expression ?? string.Empty; // 표정 반환
        public string Text => text ?? string.Empty; // 대사 반환
        public string Next => next ?? string.Empty; // 다음 노드 ID 반환
        public IReadOnlyList<DialogueChoice> Choices => choices ?? (IReadOnlyList<DialogueChoice>)Array.Empty<DialogueChoice>(); // 선택지 반환
        public bool HasChoices => choices != null && choices.Count > 0; // 선택지 존재 여부 반환
    }

    [Serializable] // JSON 직렬화 허용
    public sealed class DialogueScript // 대화 한 편 (Day58 신규 — Resources/Dialogues/{id}.json)
    {
        [SerializeField] private string id; // 대화 ID
        [SerializeField] private string title; // 대화 제목
        [SerializeField] private string characterId; // 스탠딩으로 표시할 캐릭터 ID
        [SerializeField] private string background; // 배경 키
        [SerializeField] private string location; // 장소 표시 문구
        [SerializeField] private List<DialogueNode> nodes = new List<DialogueNode>(); // 노드 목록
        public string Id => id ?? string.Empty; // 대화 ID 반환
        public string Title => title ?? string.Empty; // 제목 반환
        public string CharacterId => characterId ?? string.Empty; // 스탠딩 캐릭터 반환
        public string Background => background ?? string.Empty; // 배경 키 반환
        public string Location => location ?? string.Empty; // 장소 문구 반환
        public IReadOnlyList<DialogueNode> Nodes => nodes ?? (IReadOnlyList<DialogueNode>)Array.Empty<DialogueNode>(); // 노드 목록 반환

        public int IndexOf(string nodeId) // 노드 ID 위치 조회
        {
            if (nodes == null || string.IsNullOrEmpty(nodeId)) // 목록·ID 확인
            {
                return -1; // 조회 실패 반환
            }

            for (int index = 0; index < nodes.Count; index++) // 노드 순회
            {
                if (nodes[index] != null && string.Equals(nodes[index].Id, nodeId, StringComparison.Ordinal)) // ID 일치 확인
                {
                    return index; // 위치 반환
                }
            }

            return -1; // 조회 실패 반환
        }
    }

    public readonly struct DialogueLogEntry // 대화 로그 한 줄 (Day58 신규)
    {
        public string Speaker { get; } // 화자 ID (선택지는 HERO)
        public string Text { get; } // 대사 또는 선택지 문구
        public bool IsChoice { get; } // 선택지 기록 여부

        public DialogueLogEntry(string speaker, string text, bool isChoice) // 로그 생성
        {
            Speaker = speaker ?? string.Empty; // 화자 저장
            Text = text ?? string.Empty; // 문구 저장
            IsChoice = isChoice; // 선택지 여부 저장
        }
    }
}
