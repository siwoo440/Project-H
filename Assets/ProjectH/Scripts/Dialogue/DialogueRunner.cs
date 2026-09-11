using System; // 예외 기능
using System.Collections.Generic; // 목록 자료형

namespace ProjectH.Dialogue // 프로젝트 대화 영역
{
    public sealed class DialogueRunner // 대화 진행기 (Day58 신규 — 화면과 분리된 순수 진행 로직)
    {
        private readonly List<DialogueLogEntry> log = new List<DialogueLogEntry>(); // 지나간 대사 기록
        private int currentIndex = -1; // 현재 노드 위치

        public DialogueScript Script { get; } // 진행 중인 대화
        public DialogueNode Current => IsFinished || currentIndex < 0 ? null : Script.Nodes[currentIndex]; // 현재 노드 반환
        public bool IsFinished { get; private set; } // 대화 종료 여부
        public bool IsWaitingForChoice => !IsFinished && Current != null && Current.HasChoices; // 선택지 대기 여부
        public int AccumulatedAffinity { get; private set; } // 선택지로 쌓인 호감도 (종료 후 한 번에 반영)
        public int PreferredChoiceCount { get; private set; } // 고른 선호 선택지 수
        public IReadOnlyList<DialogueLogEntry> Log => log; // 대화 로그 반환

        public DialogueRunner(DialogueScript script) // 진행기 생성
        {
            Script = script ?? throw new ArgumentNullException(nameof(script)); // 대화 저장
            MoveToIndex(0); // 첫 노드부터 시작
        }

        public bool Advance() // 다음 대사로 넘기기
        {
            if (IsFinished || IsWaitingForChoice) // 종료·선택지 대기 확인
            {
                return false; // 넘기지 않음 (선택지는 반드시 골라야 함)
            }

            MoveTo(Current.Next, currentIndex + 1); // 지정 노드 또는 바로 아래 노드로 이동
            return true; // 넘김 성공 반환
        }

        public bool Choose(int choiceIndex) // 선택지 고르기
        {
            if (!IsWaitingForChoice || choiceIndex < 0 || choiceIndex >= Current.Choices.Count) // 선택 가능 여부 확인
            {
                return false; // 선택 실패 반환
            }

            DialogueChoice choice = Current.Choices[choiceIndex]; // 선택지 조회
            AccumulatedAffinity += choice.Affinity; // 호감도 누적
            if (choice.Preferred) PreferredChoiceCount++; // 선호 선택지 수 증가
            log.Add(new DialogueLogEntry(DialogueSpeakers.Hero, choice.Text, true)); // 선택 기록
            MoveTo(choice.Next, currentIndex + 1); // 선택지 목적지로 이동
            return true; // 선택 성공 반환
        }

        public int SkipToChoiceOrEnd() // 다음 선택지 또는 끝까지 건너뛰기 (선택지는 건너뛰지 않음)
        {
            int steps = 0; // 넘긴 대사 수
            int guard = Script.Nodes.Count * 4 + 4; // 순환 대화 무한 반복 방지

            while (!IsFinished && !IsWaitingForChoice && steps < guard) // 멈출 지점까지 반복
            {
                Advance(); // 한 줄 넘기기
                steps++; // 넘긴 수 증가
            }

            return steps; // 넘긴 수 반환
        }

        private void MoveTo(string nextId, int fallbackIndex) // 노드 ID 또는 기본 위치로 이동
        {
            if (string.Equals(nextId, DialogueSpeakers.EndNodeId, StringComparison.Ordinal)) // 종료 지정 확인
            {
                Finish(); // 대화 종료
                return; // 이동 종료
            }

            MoveToIndex(string.IsNullOrEmpty(nextId) ? fallbackIndex : Script.IndexOf(nextId)); // ID가 없으면 바로 아래, 있으면 해당 노드
        }

        private void MoveToIndex(int index) // 지정 위치로 이동
        {
            if (index < 0 || index >= Script.Nodes.Count || Script.Nodes[index] == null) // 범위 확인
            {
                Finish(); // 범위를 벗어나면 종료
                return; // 이동 종료
            }

            currentIndex = index; // 위치 갱신
            DialogueNode node = Script.Nodes[index]; // 새 노드 조회
            if (!string.IsNullOrEmpty(node.Text)) log.Add(new DialogueLogEntry(node.Speaker, node.Text, false)); // 대사 기록
        }

        private void Finish() // 대화 종료 처리
        {
            IsFinished = true; // 종료 기록
            currentIndex = -1; // 현재 노드 해제
        }
    }
}
