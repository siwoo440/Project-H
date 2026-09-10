namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    public static class GameTimeService // 날짜 및 Morning/Day/Evening/Night 시간대 진행 공통 처리 기능
    {
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
                    break; // 밤 분기 종료
            }

            return saveData.CurrentTime; // 진행 후 시간대 반환
        }
    }
}
