using System; // 문자열 비교 기능
using System.Collections.Generic; // 사전·목록 자료형
using UnityEngine; // Resources·JSON 기능

namespace ProjectH.Dialogue // 프로젝트 대화 영역
{
    public static class DialogueLibrary // 대화 파일 불러오기·검사 (Day58 신규 — Resources/Dialogues/{id}.json)
    {
        public const string ResourceFolder = "Dialogues/"; // Resources 하위 대화 폴더
        private static readonly Dictionary<string, DialogueScript> Cache = new Dictionary<string, DialogueScript>(StringComparer.Ordinal); // 불러온 대화 캐시

        public static DialogueScript Load(string scriptId) // 대화 ID로 불러오기 (없으면 null)
        {
            if (string.IsNullOrWhiteSpace(scriptId)) // ID 확인
            {
                return null; // 빈 ID 반환
            }

            if (Cache.TryGetValue(scriptId, out DialogueScript cached)) // 캐시 확인
            {
                return cached; // 캐시 반환
            }

            TextAsset asset = Resources.Load<TextAsset>(ResourceFolder + scriptId); // JSON 파일 불러오기
            DialogueScript script = asset == null ? null : Parse(asset.text); // JSON 해석
            if (script != null) Cache[scriptId] = script; // 성공 시 캐시 저장
            return script; // 대화 반환
        }

        public static DialogueScript Parse(string json) // JSON 문자열 해석 (실패 시 null)
        {
            if (string.IsNullOrWhiteSpace(json)) // 내용 확인
            {
                return null; // 빈 내용 반환
            }

            try // JSON 형식 오류 대비
            {
                return JsonUtility.FromJson<DialogueScript>(json); // 대화 해석 반환
            }
            catch (Exception) // JSON 형식 오류 처리 (Unity 버전별 예외 종류 차이 대비)
            {
                return null; // 해석 실패 반환
            }
        }

        public static void ClearCache() // 캐시 비우기 (에디터에서 대사 수정 후 다시 불러올 때)
        {
            Cache.Clear(); // 캐시 초기화
        }

        public static bool Validate(DialogueScript script, List<string> errors) // 대화 구조 검사 (끊긴 분기·빈 대사·잘못된 화자)
        {
            int before = errors.Count; // 검사 전 오류 수

            if (script == null) // 대화 확인
            {
                errors.Add("대화를 해석할 수 없습니다."); // 해석 실패 오류
                return false; // 검사 실패 반환
            }

            if (script.Nodes.Count == 0) // 노드 확인
            {
                errors.Add($"[{script.Id}] 노드가 없습니다."); // 빈 대화 오류
                return false; // 검사 실패 반환
            }

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal); // 중복 ID 검사 집합

            for (int index = 0; index < script.Nodes.Count; index++) // 노드 순회
            {
                DialogueNode node = script.Nodes[index]; // 노드 조회

                if (node == null) // null 노드 확인
                {
                    errors.Add($"[{script.Id}] {index}번 노드가 비어 있습니다."); // null 노드 오류
                    continue; // 다음 노드 검사
                }

                if (!string.IsNullOrEmpty(node.Id) && !ids.Add(node.Id)) errors.Add($"[{script.Id}] 중복 노드 ID: {node.Id}"); // 중복 ID 오류
                if (string.IsNullOrWhiteSpace(node.Text) && !node.HasChoices) errors.Add($"[{script.Id}] {index}번 노드에 대사와 선택지가 모두 없습니다."); // 빈 노드 오류
                if (!IsValidSpeaker(node.Speaker)) errors.Add($"[{script.Id}] {index}번 노드 화자 오류: {node.Speaker}"); // 화자 오류
                CheckTarget(script, node.Next, $"{index}번 노드 next", errors); // 다음 노드 대상 검사

                for (int choiceIndex = 0; choiceIndex < node.Choices.Count; choiceIndex++) // 선택지 순회
                {
                    DialogueChoice choice = node.Choices[choiceIndex]; // 선택지 조회
                    if (choice == null || string.IsNullOrWhiteSpace(choice.Text)) errors.Add($"[{script.Id}] {index}번 노드 {choiceIndex}번 선택지 문구가 비어 있습니다."); // 빈 선택지 오류
                    else CheckTarget(script, choice.Next, $"{index}번 노드 선택지 「{choice.Text}」", errors); // 선택지 대상 검사
                }
            }

            return errors.Count == before; // 새 오류가 없으면 성공
        }

        public static bool IsValidSpeaker(string speaker) // 허용 화자 확인
        {
            return speaker == DialogueSpeakers.Hero || speaker == DialogueSpeakers.Narration || (speaker != null && (speaker.StartsWith("CH_", StringComparison.Ordinal) || speaker.StartsWith("NPC_", StringComparison.Ordinal))); // 주인공·나레이션·캐릭터·NPC ID 허용 (Day62 NPC 추가)
        }

        private static void CheckTarget(DialogueScript script, string target, string owner, List<string> errors) // 이동 대상 존재 검사
        {
            if (string.IsNullOrEmpty(target) || target == DialogueSpeakers.EndNodeId) // 기본 이동·종료 확인
            {
                return; // 검사 통과
            }

            if (script.IndexOf(target) < 0) // 대상 노드 존재 확인
            {
                errors.Add($"[{script.Id}] {owner} → 없는 노드 {target}"); // 끊긴 분기 오류
            }
        }
    }
}
