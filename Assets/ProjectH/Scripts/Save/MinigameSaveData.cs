using System; // 숫자 범위 기능
using System.Collections.Generic; // 목록 자료형
using ProjectH.Minigame; // 놀이판 종류 기능
using UnityEngine; // 직렬화 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    [Serializable] // 저장 직렬화 허용
    public sealed class MinigameRecordSaveData // 놀이판 한 종류의 기록 (Day70 신규)
    {
        [SerializeField] private int kind; // 놀이판 종류 (MinigameKind 숫자)
        [SerializeField] private int plays; // 해 본 횟수
        [SerializeField] private int bestScore; // 최고 점수
        [SerializeField] private int bestGain; // 한 판 최고 이득 (골드 · 마차 균형은 0)

        public int Kind => Math.Max(0, kind); // 종류 반환
        public int Plays => Math.Max(0, plays); // 횟수 반환
        public int BestScore => Math.Max(0, bestScore); // 최고 점수 반환
        public int BestGain => Math.Max(0, bestGain); // 최고 이득 반환

        public MinigameRecordSaveData() // 직렬화용 생성
        {
        }

        public MinigameRecordSaveData(int kindValue) // 기록 생성
        {
            kind = Math.Max(0, kindValue); // 종류 저장
        }

        public void Report(int score, int gain) // 한 판 결과 반영
        {
            plays = Math.Max(0, plays) + 1; // 횟수 증가
            bestScore = Math.Max(bestScore, Math.Max(0, score)); // 최고 점수 갱신
            bestGain = Math.Max(bestGain, Math.Max(0, gain)); // 최고 이득 갱신
        }

        public void EnsureDefaults() // 이전 저장 기본값 복원
        {
            kind = Math.Max(0, kind); // 종류 보정
            plays = Math.Max(0, plays); // 횟수 보정
            bestScore = Math.Max(0, bestScore); // 점수 보정
            bestGain = Math.Max(0, bestGain); // 이득 보정
        }
    }

    [Serializable] // 저장 직렬화 허용
    public sealed class MinigameBoardSaveData // 시장 놀이판 진행 상태 (Day70 신규 — 하루 3회 제한과 기록)
    {
        [SerializeField] private int day; // 오늘 횟수를 센 일차 (0 = 아직 놀지 않음)
        [SerializeField] private int playsToday; // 오늘 놀이 횟수
        [SerializeField] private int lifetimeGain; // 지금까지 벌어들인 골드 합 (손해는 제외)
        [SerializeField] private List<MinigameRecordSaveData> records = new List<MinigameRecordSaveData>(); // 종류별 기록

        public int Day => Math.Max(0, day); // 일차 반환
        public int PlaysToday => Math.Max(0, playsToday); // 오늘 횟수 반환
        public int LifetimeGain => Math.Max(0, lifetimeGain); // 누적 이득 반환
        public IReadOnlyList<MinigameRecordSaveData> Records => records; // 기록 목록 반환

        public int GetRemaining(int currentDay) // 오늘 남은 횟수
        {
            int used = Day == currentDay ? PlaysToday : 0; // 날짜가 바뀌면 0회부터
            return Math.Max(0, MinigameCatalog.DailyPlayLimit - used); // 남은 횟수 반환
        }

        public void ConsumePlay(int currentDay) // 놀이 1회 사용 기록 (날짜가 바뀌면 초기화)
        {
            if (Day != currentDay) // 새 일차 확인
            {
                day = Math.Max(1, currentDay); // 일차 저장
                playsToday = 0; // 횟수 초기화
            }

            playsToday = Math.Max(0, playsToday) + 1; // 횟수 증가
        }

        public void Report(MinigameKind kind, int score, int gain) // 한 판 결과 기록
        {
            MinigameRecordSaveData record = Find((int)kind); // 기록 조회

            if (record == null) // 없으면 생성
            {
                record = new MinigameRecordSaveData((int)kind); // 새 기록
                records.Add(record); // 목록 추가
            }

            record.Report(score, gain); // 기록 반영
            if (gain > 0) lifetimeGain = Math.Max(0, lifetimeGain) + gain; // 누적 이득 반영
        }

        public MinigameRecordSaveData Find(int kindValue) // 종류별 기록 조회
        {
            foreach (MinigameRecordSaveData record in records) // 기록 순회
            {
                if (record != null && record.Kind == kindValue) return record; // 일치 반환
            }

            return null; // 없음
        }

        public void EnsureDefaults() // 이전 저장 기본값 복원
        {
            if (records == null) records = new List<MinigameRecordSaveData>(); // 목록 복원
            records.RemoveAll(record => record == null); // 잘못된 기록 제거
            foreach (MinigameRecordSaveData record in records) record.EnsureDefaults(); // 기록 보정
            day = Math.Max(0, day); // 일차 보정
            playsToday = Math.Max(0, playsToday); // 횟수 보정
            lifetimeGain = Math.Max(0, lifetimeGain); // 누적 보정
        }
    }
}
