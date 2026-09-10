using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.SaveSystem; // 저장 및 시간 진행 기능
using UnityEngine; // Unity JSON 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class GameTimeServiceTests // Day41 날짜 및 시간대 진행 회귀 테스트
    {
        [Test] // 새 게임 기본 일차 검증
        public void NewGame_DefaultDay_IsOne() // 새 게임 기본 일차 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(System.Array.Empty<string>()); // 새 게임 데이터 생성

            Assert.That(GameTimeService.GetCurrentDay(saveData), Is.EqualTo(1)); // 기본 일차 검증
        }

        [Test] // 새 게임 기본 시간대 검증
        public void NewGame_DefaultPhase_IsMorning() // 새 게임 기본 시간대 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(System.Array.Empty<string>()); // 새 게임 데이터 생성

            Assert.That(GameTimeService.GetCurrentPhase(saveData), Is.EqualTo(SaveTimeOfDay.Morning)); // 기본 시간대 검증
        }

        [Test] // 아침에서 낮 진행 검증
        public void AdvanceTime_FromMorning_MovesToDay() // Morning to Day 진행 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(System.Array.Empty<string>()); // 새 게임 데이터 생성

            SaveTimeOfDay result = GameTimeService.AdvanceTime(saveData); // 시간대 진행 실행

            Assert.That(result, Is.EqualTo(SaveTimeOfDay.Day)); // 진행 결과 검증
            Assert.That(GameTimeService.GetCurrentDay(saveData), Is.EqualTo(1)); // 일차 유지 검증
        }

        [Test] // 낮에서 저녁 진행 검증
        public void AdvanceTime_FromDay_MovesToEvening() // Day to Evening 진행 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(System.Array.Empty<string>()); // 새 게임 데이터 생성
            GameTimeService.AdvanceTime(saveData); // Morning to Day 진행

            SaveTimeOfDay result = GameTimeService.AdvanceTime(saveData); // Day to Evening 진행 실행

            Assert.That(result, Is.EqualTo(SaveTimeOfDay.Evening)); // 진행 결과 검증
            Assert.That(GameTimeService.GetCurrentDay(saveData), Is.EqualTo(1)); // 일차 유지 검증
        }

        [Test] // 저녁에서 밤 진행 검증
        public void AdvanceTime_FromEvening_MovesToNight() // Evening to Night 진행 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(System.Array.Empty<string>()); // 새 게임 데이터 생성
            GameTimeService.AdvanceTime(saveData); // Morning to Day 진행
            GameTimeService.AdvanceTime(saveData); // Day to Evening 진행

            SaveTimeOfDay result = GameTimeService.AdvanceTime(saveData); // Evening to Night 진행 실행

            Assert.That(result, Is.EqualTo(SaveTimeOfDay.Night)); // 진행 결과 검증
            Assert.That(GameTimeService.GetCurrentDay(saveData), Is.EqualTo(1)); // 일차 유지 검증
        }

        [Test] // 밤에서 다음 날 아침 진행 검증
        public void AdvanceTime_FromNight_MovesToNextDayMorning() // Night to next day Morning 진행 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(System.Array.Empty<string>()); // 새 게임 데이터 생성
            GameTimeService.AdvanceTime(saveData); // Morning to Day 진행
            GameTimeService.AdvanceTime(saveData); // Day to Evening 진행
            GameTimeService.AdvanceTime(saveData); // Evening to Night 진행

            SaveTimeOfDay result = GameTimeService.AdvanceTime(saveData); // Night to Morning 진행 실행

            Assert.That(result, Is.EqualTo(SaveTimeOfDay.Morning)); // 다음 날 아침 검증
            Assert.That(GameTimeService.GetCurrentDay(saveData), Is.EqualTo(2)); // 다음 일차 증가 검증
        }

        [Test] // 여러 번 진행 시 순환 정확성 검증
        public void AdvanceTime_RepeatedNineTimes_CyclesCorrectly() // 9회 반복 진행 순환 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(System.Array.Empty<string>()); // 새 게임 데이터 생성

            for (int index = 0; index < 9; index++) // 시간대 9회 진행 반복
            {
                GameTimeService.AdvanceTime(saveData); // 시간대 진행 실행
            }

            // Morning -> Day -> Evening -> Night -> Day2 Morning -> Day -> Evening -> Night -> Day3 Morning -> Day 순서로 9회 진행됨
            Assert.That(GameTimeService.GetCurrentDay(saveData), Is.EqualTo(3)); // 3일차 도달 검증
            Assert.That(GameTimeService.GetCurrentPhase(saveData), Is.EqualTo(SaveTimeOfDay.Day)); // 낮 시간대 검증
        }

        [Test] // 저장 데이터 기본값 보정 검증
        public void EnsureDefaults_WithInvalidDayAndPhase_RestoresSafeValues() // 잘못된 저장 데이터 보정 테스트
        {
            string json = "{\"saveVersion\":1,\"currentDay\":-5,\"currentTime\":99}"; // 손상된 최소 저장 데이터 직접 구성
            SaveData loaded = JsonUtility.FromJson<SaveData>(json); // 손상 데이터 역직렬화

            loaded.EnsureDefaults(); // 저장 기본값 보정 실행

            Assert.That(loaded.CurrentDay, Is.EqualTo(1)); // 최소 일차 보정 검증
            Assert.That(loaded.CurrentTime, Is.EqualTo(SaveTimeOfDay.Morning)); // 잘못된 시간대 보정 검증
        }
    }
}
