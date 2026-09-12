using System.Collections.Generic; // 목록 자료형
using System.Linq; // 목록 검색 기능
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 던전 해금·클리어 기록 기능
using ProjectH.Core; // 회차 기록 기능
using ProjectH.SaveSystem; // 저장 기능
using ProjectH.Story; // 챕터·엔딩 기능
using ProjectH.UI; // 지원 던전 목록 기능
using UnityEngine; // JSON 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class ReleaseCandidateTests // Day74 출시 점검 — 처음부터 끝까지 막히는 곳이 없는지, 깨진 저장에서 살아남는지
    {
        [SetUp] // 테스트 준비 표시
        public void SetUp() // 회차 기록 초기화
        {
            PlayerProfile.ResetRecords(); // 기록 비우기
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 회차 기록 정리
        {
            PlayerProfile.ResetRecords(); // 기록 비우기
        }

        private static SaveData PlayFromStartToEnding(out List<string> log) // 새 게임부터 최종장까지 끝까지 진행
        {
            log = new List<string>(); // 진행 기록
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 새 게임
            ChapterService.EnsureStarted(saveData); // 스토리 시작
            int guard = 0; // 무한 반복 방지

            while (guard++ < 128) // 단계 순회
            {
                ChapterStep step = ChapterService.GetCurrentStep(saveData); // 현재 단계
                if (step == null) break; // 준비된 이야기 끝

                if (step.Kind == ChapterStepKind.Dialogue) // 이야기 단계
                {
                    log.Add($"대사 {step.Target}"); // 기록
                    ChapterService.CompleteDialogue(saveData, step.Target); // 진행
                }
                else // 던전 단계
                {
                    Assert.That(DungeonSelectionRuntimeState.IsSupportedDungeonId(step.Target), Is.True, $"챕터가 없는 던전을 가리킴 : {step.Target}"); // 던전 존재
                    log.Add($"던전 {step.Target}"); // 기록
                    DungeonProgressSaveAdapter.MarkCleared(saveData, step.Target); // 클리어
                    ChapterService.NotifyDungeonCleared(saveData, step.Target); // 진행
                }
            }

            Assert.That(guard, Is.LessThan(128), "진행이 끝나지 않고 맴돌았습니다"); // 무한 반복 확인
            return saveData; // 저장 반환
        }

        [Test] // 새 게임부터 최종장까지 한 번도 막히지 않는다
        public void FullPlaythrough_ReachesFinaleWithoutStalling() // 전체 진행 테스트
        {
            SaveData saveData = PlayFromStartToEnding(out List<string> log); // 끝까지 진행
            Assert.That(saveData.ChapterProgress.IsFinished, Is.True); // 이야기 끝
            Assert.That(saveData.ChapterProgress.FinishedChapterId, Is.EqualTo("CHAPTER_06")); // 최종장까지
            Assert.That(saveData.Characters.Count, Is.EqualTo(12), "12인이 모두 합류하지 않았습니다"); // 12인 완성
            Assert.That(FinaleCatalog.IsFinaleCleared(saveData), Is.True); // 최종장 완료
            Assert.That(log.Count(entry => entry.StartsWith("대사")), Is.EqualTo(32), "메인 스토리 대사 수가 다릅니다"); // 대사 32편
            Assert.That(log.Count(entry => entry.StartsWith("던전")), Is.GreaterThanOrEqualTo(8), "챕터 던전 단계가 부족합니다"); // 던전 단계
        }

        [Test] // 최종장 뒤 네 조합이 각각 엔딩으로 이어지고 회차가 쌓인다
        public void FullPlaythrough_EveryChoiceLeadsToAnEnding() // 엔딩 연결 테스트
        {
            HashSet<string> endings = new HashSet<string>(); // 나온 엔딩

            foreach (bool sealStopped in new[] { true, false }) // 선택 1
            {
                foreach (bool spared in new[] { true, false }) // 선택 2
                {
                    SaveData saveData = PlayFromStartToEnding(out _); // 끝까지 진행
                    saveData.SetStoryFlag(sealStopped ? FinaleCatalog.SealStoppedFlag : FinaleCatalog.SealKeptFlag); // 선택 1
                    saveData.SetStoryFlag(spared ? FinaleCatalog.NemesisSparedFlag : FinaleCatalog.NemesisSlainFlag); // 선택 2
                    Assert.That(EndingCatalog.IsReadyToPlay(saveData), Is.True, $"{sealStopped}/{spared} 엔딩이 재생되지 않습니다"); // 재생 대상
                    EndingDefinition ending = EndingCatalog.Resolve(saveData); // 엔딩
                    Assert.That(ending, Is.Not.Null); // 결정됨
                    endings.Add(ending.Id); // 수집
                    PlayerProfile.RecordEnding(ending.Id); // 기록
                    EndingCatalog.MarkPlayed(saveData); // 재생 기록
                    Assert.That(EndingCatalog.IsReadyToPlay(saveData), Is.False); // 다시 재생하지 않음
                }
            }

            Assert.That(endings.Count, Is.EqualTo(4)); // 네 조합 = 네 엔딩
            Assert.That(PlayerProfile.SeenEndingCount, Is.EqualTo(4)); // 전부 수집
            Assert.That(PlayerProfile.ClearCount, Is.EqualTo(4)); // 4회차
        }

        [Test] // 던전 17개가 하나도 끊기지 않고 순서대로 열린다
        public void DungeonChain_UnlocksEveryDungeon() // 던전 해금 체인 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 새 게임
            List<string> remaining = DungeonSelectionRuntimeState.SupportedDungeonIds.ToList(); // 남은 던전
            int guard = 0; // 무한 반복 방지

            while (remaining.Count > 0 && guard++ < 64) // 열릴 때까지 반복
            {
                List<string> opened = remaining.Where(id => DungeonProgressionPolicy.IsUnlocked(saveData, id)).ToList(); // 지금 열린 던전
                Assert.That(opened.Count, Is.GreaterThan(0), $"더 열 수 있는 던전이 없습니다. 남은 던전 : {string.Join(", ", remaining)}"); // 막힘 확인

                foreach (string dungeonId in opened) // 열린 던전 순회
                {
                    DungeonProgressSaveAdapter.MarkCleared(saveData, dungeonId); // 클리어
                    remaining.Remove(dungeonId); // 목록에서 제거
                }
            }

            Assert.That(remaining, Is.Empty, "끝내 열리지 않는 던전이 있습니다"); // 전부 해금
            Assert.That(DungeonSelectionRuntimeState.SupportedDungeonIds.Count, Is.EqualTo(17)); // 17던전
        }

        [Test] // 깨진 저장이 들어와도 게임이 죽지 않고 기본값으로 복구된다
        public void BrokenSave_RecoversWithDefaults() // 저장 복구 테스트
        {
            const string broken = "{\"saveVersion\":1,\"currentDay\":-5,\"currentVitality\":-999,\"bondResource\":9999," +
                                  "\"partyCharacterIds\":null,\"characters\":null,\"storyFlags\":null,\"itemInventory\":null," +
                                  "\"equipmentInventory\":null,\"runeInventory\":null,\"shopStates\":null,\"regionErosion\":null," +
                                  "\"seenDialogueIds\":null,\"seenMonsterIds\":null,\"chapterProgress\":null,\"riftState\":null," +
                                  "\"questBoard\":null,\"minigameBoard\":null,\"heroName\":null,\"savedAtTicks\":-1}"; // 값이 깨진 저장
            SaveData saveData = JsonUtility.FromJson<SaveData>(broken); // 역직렬화
            Assert.That(saveData, Is.Not.Null); // 해석 성공
            Assert.That(() => saveData.EnsureDefaults(), Throws.Nothing); // 복구 중 예외 없음

            Assert.That(saveData.CurrentDay, Is.GreaterThanOrEqualTo(1)); // 일차 복구
            Assert.That(saveData.CurrentVitality, Is.GreaterThanOrEqualTo(0)); // 활력 복구
            Assert.That(saveData.Characters, Is.Not.Null); // 목록 복구
            Assert.That(saveData.StoryFlags, Is.Not.Null); // 목록 복구
            Assert.That(saveData.ChapterProgress, Is.Not.Null); // 진행 복구
            Assert.That(saveData.MinigameBoard, Is.Not.Null); // 놀이판 복구
            Assert.That(saveData.RiftState, Is.Not.Null); // 균열 복구
            Assert.That(saveData.QuestBoard, Is.Not.Null); // 의뢰 복구
            Assert.That(saveData.HeroName, Is.Not.Null); // 이름 복구
            Assert.That(saveData.SavedAt, Is.EqualTo(System.DateTime.MinValue)); // 잘못된 시각은 '기록 없음'

            Assert.That(() => ChapterService.EnsureStarted(saveData), Throws.Nothing); // 스토리 진입 가능
            Assert.That(() => ChapterService.Refresh(saveData), Throws.Nothing); // 로비 진입 가능
            Assert.That(() => ProjectH.Minigame.MinigameService.GetRemainingPlays(saveData), Throws.Nothing); // 놀이판 조회 가능
        }

        [Test] // 빈 저장으로 만든 새 게임이 곧바로 정상 상태다
        public void NewGame_StartsInPlayableState() // 새 게임 상태 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 새 게임
            Assert.That(saveData.CurrentDay, Is.EqualTo(1)); // 1일차
            Assert.That(saveData.Characters.Count, Is.EqualTo(1)); // 세레나 1인
            Assert.That(DungeonProgressionPolicy.IsUnlocked(saveData, "DG001"), Is.True); // 첫 던전 열림
            Assert.That(HeroNameService.NeedsInput(saveData), Is.True); // 이름 입력 대기
            ChapterService.EnsureStarted(saveData); // 스토리 시작
            Assert.That(ChapterService.GetPendingDialogueId(saveData), Is.Not.Empty); // 첫 이야기 준비
            Assert.That(saveData.MinigameBoard.GetRemaining(1), Is.EqualTo(ProjectH.Minigame.MinigameCatalog.DailyPlayLimit)); // 놀이판 3회
        }
    }
}
