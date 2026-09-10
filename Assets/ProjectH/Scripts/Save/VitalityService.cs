namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    public static class VitalityService // 활력 자원 공통 처리 기능 (Day42 기본값, 추후 수치 조정 예정)
    {
        public static int GetVitality(SaveData saveData) // 현재 활력 조회
        {
            if (saveData == null) // 저장 데이터 확인
            {
                return 0; // 저장 데이터 없음 기본 활력 반환
            }

            saveData.EnsureDefaults(); // 저장 기본값 보정
            return saveData.CurrentVitality; // 현재 활력 반환
        }

        public static int AddVitality(SaveData saveData, int amount) // 활력 증가
        {
            if (saveData == null || amount <= 0) // 저장 데이터 및 증가량 확인
            {
                return GetVitality(saveData); // 기존 활력 유지 반환
            }

            saveData.EnsureDefaults(); // 저장 기본값 보정
            int nextVitality = saveData.CurrentVitality + amount; // 증가 후 활력 계산
            saveData.SetCurrentVitality(nextVitality); // 최대값 보정 후 활력 저장
            return saveData.CurrentVitality; // 변경 활력 반환
        }

        public static bool TrySpendVitality(SaveData saveData, int amount, out string error) // 활력 안전 소비
        {
            error = string.Empty; // 오류 문구 초기화

            if (saveData == null) // 저장 데이터 확인
            {
                error = "SaveData가 없습니다."; // 저장 데이터 누락 오류
                return false; // 소비 실패 반환
            }

            if (amount <= 0) // 소비량 확인
            {
                error = "활력 소비량은 1 이상이어야 합니다."; // 잘못된 소비량 오류
                return false; // 소비 실패 반환
            }

            saveData.EnsureDefaults(); // 저장 기본값 보정

            if (saveData.CurrentVitality < amount) // 활력 부족 확인
            {
                error = $"활력이 부족합니다. Current={saveData.CurrentVitality}, Cost={amount}"; // 활력 부족 오류
                return false; // 소비 실패 반환
            }

            saveData.SetCurrentVitality(saveData.CurrentVitality - amount); // 소비 후 활력 저장
            return true; // 소비 성공 반환
        }

        public static int RestoreFull(SaveData saveData) // 활력 최대치 전체 회복
        {
            if (saveData == null) // 저장 데이터 확인
            {
                return 0; // 저장 데이터 없음 기본 활력 반환
            }

            saveData.EnsureDefaults(); // 저장 기본값 보정
            saveData.SetCurrentVitality(SaveData.MaxVitality); // 활력 최대치로 복원
            return saveData.CurrentVitality; // 회복 후 활력 반환
        }
    }
}
