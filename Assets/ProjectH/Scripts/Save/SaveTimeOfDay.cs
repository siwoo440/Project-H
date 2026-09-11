namespace ProjectH.SaveSystem // 프로젝트 저장 영역 (최적화 — SaveData.cs에서 분리)
{
    public enum SaveTimeOfDay // 저장 시간대 (Day41 Morning/Day/Evening/Night 4단계)
    {
        Morning = 0, // 아침
        Day = 1, // 낮
        Evening = 2, // 저녁
        Night = 3 // 밤
    }
}
