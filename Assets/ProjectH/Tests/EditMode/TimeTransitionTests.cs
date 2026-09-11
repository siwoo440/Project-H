using System.Collections.Generic; // 목록 자료형
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.SaveSystem; // 시간 진행 기능
using ProjectH.UI; // 시간대 그림 기능
using ProjectH.Village; // 여관 잠자기 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class TimeTransitionTests // Day62 시간 전환 연출 : 시간 진행 알림 · 합치기 · 시간대 그림·표기 테스트
    {
        private readonly List<GameTimeChange> received = new List<GameTimeChange>(); // 받은 알림

        private void Collect(GameTimeChange change) => received.Add(change); // 알림 기록

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 알림 구독
        {
            received.Clear(); // 기록 비움
            GameTimeService.TimeAdvanced += Collect; // 구독
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 구독 해제
        {
            GameTimeService.TimeAdvanced -= Collect; // 해제
        }

        [Test] // 시간 한 칸 진행 시 이전·이후 일차·시간대 알림
        public void AdvanceTime_RaisesChange_WithDayRollover() // 알림 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 새 게임 (1일차 아침)
            GameTimeService.AdvanceTime(saveData); // 아침 → 점심
            saveData.SetCurrentTime(SaveTimeOfDay.Night); // 밤으로
            GameTimeService.AdvanceTime(saveData); // 밤 → 다음 날 아침

            Assert.That(received.Count, Is.EqualTo(2)); // 알림 2번
            Assert.That(received[0].FromPhase, Is.EqualTo(SaveTimeOfDay.Morning)); // 이전 아침
            Assert.That(received[0].ToPhase, Is.EqualTo(SaveTimeOfDay.Day)); // 이후 점심
            Assert.That(received[0].DayChanged, Is.False); // 같은 날
            Assert.That(received[1].FromDay, Is.EqualTo(1)); // 이전 1일차
            Assert.That(received[1].ToDay, Is.EqualTo(2)); // 이후 2일차
            Assert.That(received[1].DayChanged, Is.True); // 날 바뀜
        }

        [Test] // 잠자기처럼 연속 진행은 한 번의 전환으로 합쳐짐 (저녁 → 아침)
        public void ConsecutiveAdvances_MergeIntoOneTransition() // 합치기 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 새 게임
            saveData.SetCurrentDay(4); // 4일차
            saveData.SetCurrentTime(SaveTimeOfDay.Evening); // 저녁
            Assert.That(VillageActionService.TrySleep(saveData, out string message), Is.True, message); // 잠자기 (저녁 → 밤 → 아침)

            Assert.That(received.Count, Is.EqualTo(2)); // 내부 진행 2번
            GameTimeChange merged = received[0].Merge(received[1]); // 연출에서 합친 결과
            Assert.That(merged.FromPhase, Is.EqualTo(SaveTimeOfDay.Evening)); // 저녁에서
            Assert.That(merged.ToPhase, Is.EqualTo(SaveTimeOfDay.Morning)); // 아침으로
            Assert.That(merged.FromDay, Is.EqualTo(4)); // 4일차에서
            Assert.That(merged.ToDay, Is.EqualTo(5)); // 5일차로
        }

        [Test] // 시간대 표기 · 그림 · 문구
        public void PhaseLabels_ArtAndSubtitles_ExistForEveryPhase() // 표기 테스트
        {
            Assert.That(GameTimeService.GetPhaseLabel(SaveTimeOfDay.Morning), Is.EqualTo("아침")); // 아침
            Assert.That(GameTimeService.GetPhaseLabel(SaveTimeOfDay.Day), Is.EqualTo("점심")); // 점심 (사용자 요청 표기)
            Assert.That(GameTimeService.GetPhaseLabel(SaveTimeOfDay.Evening), Is.EqualTo("저녁")); // 저녁
            Assert.That(GameTimeService.GetPhaseLabel(SaveTimeOfDay.Night), Is.EqualTo("밤")); // 밤

            foreach (SaveTimeOfDay phase in new[] { SaveTimeOfDay.Morning, SaveTimeOfDay.Day, SaveTimeOfDay.Evening, SaveTimeOfDay.Night }) // 4시간대
            {
                Assert.That(TimeOfDayArt.Get(phase), Is.Not.Null, phase.ToString()); // 그림 (정식 또는 임시)
                Assert.That(TimeOfDayArt.GetSubtitle(phase), Is.Not.Empty, phase.ToString()); // 문구
            }
        }
    }
}
