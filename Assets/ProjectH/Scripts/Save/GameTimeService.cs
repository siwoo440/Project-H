using System; // 이벤트 델리게이트 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    public readonly struct GameTimeChange // 시간 진행 한 번의 변화 (Day62 추가 — 시간 전환 연출용)
    {
        public int FromDay { get; } // 이전 일차
        public SaveTimeOfDay FromPhase { get; } // 이전 시간대
        public int ToDay { get; } // 이후 일차
        public SaveTimeOfDay ToPhase { get; } // 이후 시간대
        public bool DayChanged => ToDay != FromDay; // 일차가 바뀌었는지

        public GameTimeChange(int fromDay, SaveTimeOfDay fromPhase, int toDay, SaveTimeOfDay toPhase) // 변화 생성
        {
            FromDay = fromDay; // 이전 일차 저장
            FromPhase = fromPhase; // 이전 시간대 저장
            ToDay = toDay; // 이후 일차 저장
            ToPhase = toPhase; // 이후 시간대 저장
        }

        public GameTimeChange Merge(GameTimeChange next) => new GameTimeChange(FromDay, FromPhase, next.ToDay, next.ToPhase); // 연속 진행 합치기 (예: 저녁→밤→아침을 저녁→아침 한 번으로)
    }

    public static class GameTimeService // 날짜 및 Morning/Day/Evening/Night 시간대 진행 공통 처리 기능
    {
        public static event Action<GameTimeChange> TimeAdvanced; // 시간이 한 칸 진행될 때마다 알림 (Day62 추가 — 전환 연출이 구독)

        public static string GetPhaseLabel(SaveTimeOfDay phase) // 시간대 한글 이름 (Day62 추가 — 화면 공용)
        {
            switch (phase) // 시간대 분기
            {
                case SaveTimeOfDay.Morning: return "아침"; // 아침
                case SaveTimeOfDay.Day: return "점심"; // 낮 (사용자 요청 표기 '점심')
                case SaveTimeOfDay.Evening: return "저녁"; // 저녁
                default: return "밤"; // 밤
            }
        }

        public static int GetCurrentDay(SaveData saveData) // 현재 일차 조회
        {
            if (saveData == null) // 저장 데이터 확인
            {
                return 1; // 저장 데이터 없음 기본 일차 반환
            }

            saveData.EnsureDefaults(); // 저장 기본값 보정
            return saveData.CurrentDay; // 현재 일차 반환
        }

        public static SaveTimeOfDay GetCurrentPhase(SaveData saveData) // 현재 시간대 조회
        {
            if (saveData == null) // 저장 데이터 확인
            {
                return SaveTimeOfDay.Morning; // 저장 데이터 없음 기본 시간대 반환
            }

            saveData.EnsureDefaults(); // 저장 기본값 보정
            return saveData.CurrentTime; // 현재 시간대 반환
        }

        public static SaveTimeOfDay AdvanceTime(SaveData saveData) // 시간대 한 단계 진행 처리
        {
            if (saveData == null) // 저장 데이터 확인
            {
                return SaveTimeOfDay.Morning; // 저장 데이터 없음 기본 시간대 반환
            }

            saveData.EnsureDefaults(); // 저장 기본값 보정
            int fromDay = saveData.CurrentDay; // 진행 전 일차 (Day62 연출용)
            SaveTimeOfDay fromPhase = saveData.CurrentTime; // 진행 전 시간대 (Day62 연출용)

            switch (saveData.CurrentTime) // 현재 시간대 분기
            {
                case SaveTimeOfDay.Morning: // 아침 처리
                    saveData.SetCurrentTime(SaveTimeOfDay.Day); // 낮으로 진행
                    break; // 아침 분기 종료
                case SaveTimeOfDay.Day: // 낮 처리
                    saveData.SetCurrentTime(SaveTimeOfDay.Evening); // 저녁으로 진행
                    break; // 낮 분기 종료
                case SaveTimeOfDay.Evening: // 저녁 처리
                    saveData.SetCurrentTime(SaveTimeOfDay.Night); // 밤으로 진행
                    break; // 저녁 분기 종료
                default: // 밤 및 정의되지 않은 값 처리
                    saveData.SetCurrentDay(saveData.CurrentDay + 1); // 다음 일차로 진행
                    saveData.SetCurrentTime(SaveTimeOfDay.Morning); // 다음 일차 아침으로 복귀
                    VitalityService.RestoreFull(saveData); // 다음 일차 진입 시 활력 전체 회복 (Day42)
                    break; // 밤 분기 종료
            }

            TimeAdvanced?.Invoke(new GameTimeChange(fromDay, fromPhase, saveData.CurrentDay, saveData.CurrentTime)); // 시간 진행 알림 (Day62 — 구독자가 없으면 아무 일 없음)
            return saveData.CurrentTime; // 진행 후 시간대 반환
        }
    }
}
