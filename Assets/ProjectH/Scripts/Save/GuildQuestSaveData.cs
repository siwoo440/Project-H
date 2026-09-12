using System; // 숫자 범위 기능
using System.Collections.Generic; // 목록 자료형
using UnityEngine; // 직렬화 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    [Serializable] // 저장 직렬화 허용
    public sealed class GuildQuestEntrySaveData // 오늘의 길드 의뢰 하나 (Day67 신규)
    {
        [SerializeField] private string questId = string.Empty; // 의뢰 ID
        [SerializeField] private int progress; // 진행도
        [SerializeField] private bool claimed; // 보상 수령 여부

        public string QuestId => questId ?? string.Empty; // 의뢰 ID 반환
        public int Progress => Math.Max(0, progress); // 진행도 반환
        public bool Claimed => claimed; // 수령 여부 반환

        public GuildQuestEntrySaveData() // 직렬화용 생성
        {
        }

        public GuildQuestEntrySaveData(string id) // 의뢰 생성
        {
            questId = id ?? string.Empty; // ID 저장
        }

        public void AddProgress(int amount, int required) // 진행도 증가 (목표를 넘지 않음)
        {
            progress = Math.Min(Math.Max(1, required), Math.Max(0, progress) + Math.Max(0, amount)); // 진행도 저장
        }

        public void MarkClaimed() => claimed = true; // 보상 수령 기록

        public void EnsureDefaults() // 이전 저장 기본값 복원
        {
            if (questId == null) questId = string.Empty; // ID 복원
            progress = Math.Max(0, progress); // 진행도 보정
        }
    }

    [Serializable] // 저장 직렬화 허용
    public sealed class GuildQuestBoardSaveData // 오늘의 길드 의뢰 게시판 (Day67 신규 — 날짜가 바뀌면 새 의뢰)
    {
        [SerializeField] private int day; // 의뢰가 올라온 일차 (0 = 아직 없음)
        [SerializeField] private bool bonusClaimed; // 전체 완료 보너스 수령 여부
        [SerializeField] private List<GuildQuestEntrySaveData> quests = new List<GuildQuestEntrySaveData>(); // 오늘의 의뢰 목록

        public int Day => Math.Max(0, day); // 일차 반환
        public bool BonusClaimed => bonusClaimed; // 보너스 수령 여부 반환
        public IReadOnlyList<GuildQuestEntrySaveData> Quests => quests; // 의뢰 목록 반환

        public void ResetForDay(int newDay, IEnumerable<string> questIds) // 새 일차 의뢰 교체
        {
            day = Math.Max(1, newDay); // 일차 저장
            bonusClaimed = false; // 보너스 초기화
            quests.Clear(); // 이전 의뢰 제거
            foreach (string id in questIds) quests.Add(new GuildQuestEntrySaveData(id)); // 새 의뢰 등록
        }

        public GuildQuestEntrySaveData Find(string questId) // 의뢰 조회
        {
            foreach (GuildQuestEntrySaveData quest in quests) // 의뢰 순회
            {
                if (quest != null && quest.QuestId == questId) return quest; // 일치
            }

            return null; // 없음
        }

        public void MarkBonusClaimed() => bonusClaimed = true; // 보너스 수령 기록

        public void EnsureDefaults() // 이전 저장 기본값 복원
        {
            if (quests == null) quests = new List<GuildQuestEntrySaveData>(); // 목록 복원
            quests.RemoveAll(quest => quest == null || string.IsNullOrWhiteSpace(quest.QuestId)); // 잘못된 의뢰 제거
            foreach (GuildQuestEntrySaveData quest in quests) quest.EnsureDefaults(); // 의뢰 보정
            day = Math.Max(0, day); // 일차 보정
        }
    }
}
