using System.Collections.Generic; // 목록 자료형
using System.Linq; // 목록 검색 기능
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Core; // 회차 기록 기능
using ProjectH.Diary; // 일기장 목록 기능
using ProjectH.Dialogue; // 대사 파일 기능
using ProjectH.SaveSystem; // 저장 기능
using ProjectH.Story; // 엔딩·최종장 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class EndingTests // Day72 엔딩 4종 · 엔딩 기록 · 회차 테스트
    {
        [SetUp] // 테스트 준비 표시
        public void SetUp() // 기록을 비우고 시작
        {
            PlayerProfile.ResetRecords(); // 회차·엔딩 기록 초기화
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 다른 테스트에 영향 주지 않게 정리
        {
            PlayerProfile.ResetRecords(); // 기록 초기화
        }

        private static SaveData CreateFinishedSave(bool sealStopped, bool nemesisSpared) // 최종장을 마친 저장 만들기
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 새 게임
            saveData.SetStoryFlag(FinaleCatalog.FinaleClearedFlag); // 최종장 완료
            saveData.SetStoryFlag(sealStopped ? FinaleCatalog.SealStoppedFlag : FinaleCatalog.SealKeptFlag); // 선택 1
            saveData.SetStoryFlag(nemesisSpared ? FinaleCatalog.NemesisSparedFlag : FinaleCatalog.NemesisSlainFlag); // 선택 2
            return saveData; // 저장 반환
        }

        [Test] // 엔딩은 4종이고, 두 선택의 네 조합을 모두 덮는다
        public void Endings_CoverEveryChoiceCombination() // 구성 테스트
        {
            Assert.That(EndingCatalog.All.Count, Is.EqualTo(4)); // 4종
            Assert.That(EndingCatalog.All.Select(item => item.Id).Distinct().Count(), Is.EqualTo(4)); // ID 중복 없음
            Assert.That(EndingCatalog.All.Select(item => item.Title).Distinct().Count(), Is.EqualTo(4)); // 제목 중복 없음
            Assert.That(EndingCatalog.All.Select(item => item.ScriptId).Distinct().Count(), Is.EqualTo(4)); // 대사 중복 없음
            Assert.That(EndingCatalog.All.Select(item => (item.SealStopped, item.NemesisSpared)).Distinct().Count(), Is.EqualTo(4)); // 조합 중복 없음

            foreach (EndingDefinition ending in EndingCatalog.All) // 엔딩 순회
            {
                Assert.That(ending.Summary, Is.Not.Empty, ending.Id); // 요약
                Assert.That(EndingCatalog.Find(ending.Id), Is.SameAs(ending)); // 조회 일치
                DialogueScript script = DialogueLibrary.Load(ending.ScriptId); // 대사 파일
                Assert.That(script, Is.Not.Null, ending.ScriptId); // 존재
                Assert.That(DialogueLibrary.Validate(script, new List<string>()), Is.True, ending.ScriptId); // 구조 검사
                Assert.That(script.Nodes.Count, Is.GreaterThanOrEqualTo(8), ending.ScriptId); // 엔딩다운 길이
            }
        }

        [Test] // 저장의 두 선택이 그대로 엔딩을 결정한다
        public void Resolve_MapsFlagsToEnding() // 결정 테스트
        {
            SaveData unfinished = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 선택 전
            Assert.That(EndingCatalog.Resolve(unfinished), Is.Null); // 정해지지 않음
            unfinished.SetStoryFlag(FinaleCatalog.SealStoppedFlag); // 선택 1만
            Assert.That(EndingCatalog.Resolve(unfinished), Is.Null); // 아직 부족

            List<string> resolved = new List<string>(); // 결정된 엔딩 모음

            foreach (bool sealStopped in new[] { true, false }) // 네 조합
            {
                foreach (bool spared in new[] { true, false }) // 네 조합
                {
                    SaveData saveData = CreateFinishedSave(sealStopped, spared); // 저장
                    EndingDefinition ending = EndingCatalog.Resolve(saveData); // 결정
                    Assert.That(ending, Is.Not.Null, $"{sealStopped}/{spared}"); // 정해짐
                    Assert.That(ending.SealStopped, Is.EqualTo(sealStopped)); // 선택 1 일치
                    Assert.That(ending.NemesisSpared, Is.EqualTo(spared)); // 선택 2 일치
                    resolved.Add(ending.Id); // 모으기
                }
            }

            Assert.That(resolved.Distinct().Count(), Is.EqualTo(4)); // 네 조합이 서로 다른 엔딩
        }

        [Test] // 엔딩은 최종장을 마쳤을 때 한 번만 재생된다
        public void IsReadyToPlay_OnlyAfterFinaleAndOnlyOnce() // 재생 시점 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 새 게임
            Assert.That(EndingCatalog.IsReadyToPlay(saveData), Is.False); // 최종장 전
            saveData.SetStoryFlag(FinaleCatalog.SealStoppedFlag); // 선택 1
            saveData.SetStoryFlag(FinaleCatalog.NemesisSparedFlag); // 선택 2
            Assert.That(EndingCatalog.IsReadyToPlay(saveData), Is.False); // 최종장 완료 플래그가 없음
            saveData.SetStoryFlag(FinaleCatalog.FinaleClearedFlag); // 최종장 완료
            Assert.That(EndingCatalog.IsReadyToPlay(saveData), Is.True); // 재생 대상
            EndingCatalog.MarkPlayed(saveData); // 재생 기록
            Assert.That(EndingCatalog.IsReadyToPlay(saveData), Is.False); // 다시 재생하지 않음
            Assert.That(saveData.HasStoryFlag(EndingCatalog.EndingPlayedFlag), Is.True); // 기록됨
        }

        [Test] // 엔딩 기록과 회차 수는 새로 시작해도 남는다
        public void Profile_KeepsRecordsAcrossNewGames() // 회차 기록 테스트
        {
            Assert.That(PlayerProfile.SeenEndingCount, Is.EqualTo(0)); // 처음에는 없음
            Assert.That(PlayerProfile.ClearCount, Is.EqualTo(0)); // 클리어 없음
            Assert.That(PlayerProfile.PlaythroughNumber, Is.EqualTo(1)); // 1회차

            EndingDefinition first = EndingCatalog.All[0]; // 첫 엔딩
            Assert.That(PlayerProfile.RecordEnding(first.Id), Is.True); // 처음 본 엔딩
            Assert.That(PlayerProfile.HasSeenEnding(first.Id), Is.True); // 기록됨
            Assert.That(PlayerProfile.ClearCount, Is.EqualTo(1)); // 1회 클리어
            Assert.That(PlayerProfile.PlaythroughNumber, Is.EqualTo(2)); // 다음은 2회차

            Assert.That(PlayerProfile.RecordEnding(first.Id), Is.False); // 같은 엔딩은 새 엔딩이 아님
            Assert.That(PlayerProfile.SeenEndingCount, Is.EqualTo(1)); // 목록은 그대로
            Assert.That(PlayerProfile.ClearCount, Is.EqualTo(2)); // 클리어 수는 증가

            SaveData fresh = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 새 회차 저장 (기록과 무관)
            Assert.That(fresh.HasStoryFlag(EndingCatalog.EndingPlayedFlag), Is.False); // 새 저장은 엔딩 기록 없음
            Assert.That(PlayerProfile.HasSeenEnding(first.Id), Is.True); // 회차 기록은 유지
            Assert.That(PlayerProfile.SeenEndingCount, Is.EqualTo(1)); // 유지
        }

        [Test] // 엔딩 4편이 일기장 다시 보기 목록에 들어간다
        public void Diary_ListsEndings() // 일기장 목록 테스트
        {
            List<DiaryScenarioEntry> endings = DiaryCatalog.Scenarios.Where(entry => entry.Category == DiaryScenarioCategory.Ending).ToList(); // 엔딩 항목
            Assert.That(endings.Count, Is.EqualTo(EndingCatalog.All.Count)); // 4편
            Assert.That(DiaryCatalog.GetCategoryLabel(DiaryScenarioCategory.Ending), Is.EqualTo("엔딩")); // 분류 이름

            foreach (EndingDefinition ending in EndingCatalog.All) // 엔딩 순회
            {
                Assert.That(endings.Any(entry => entry.ScriptId == ending.ScriptId), Is.True, ending.Id); // 목록에 있음
            }
        }
    }
}
