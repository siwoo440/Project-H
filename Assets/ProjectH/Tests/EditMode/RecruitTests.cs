using System.Collections.Generic; // 목록 자료형
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 던전 클리어 기록 기능
using ProjectH.Data; // 던전 데이터 기능
using ProjectH.Dialogue; // 대화 진행기 기능
using ProjectH.Dungeon; // 던전 입장 기능
using ProjectH.SaveSystem; // 저장 기능
using ProjectH.Village; // 합류 기능
using UnityEditor; // 에셋 로드 기능
using UnityEngine; // JSON 직렬화 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class RecruitTests // Day64 새 동료 합류 (던전 진행 조건 → 길드 합류 이야기) 테스트
    {
        private static readonly string[] Starters = { "CH_SERENA", "CH_ELLEN", "CH_LILIA", "CH_EVE" }; // 초기 4인

        private static SaveData CreateSave() => SaveData.CreateNewGame(Starters); // 새 게임

        private static List<string> PendingIds(SaveData saveData) // 대기 중 동료 ID
        {
            List<string> ids = new List<string>(); // 결과
            foreach (RecruitDefinition definition in RecruitService.GetPending(saveData)) ids.Add(definition.CharacterId); // 변환
            return ids; // 반환
        }

        private static DialogueRunner FinishedRunner(string scriptId) // 끝까지 본 대화 진행기
        {
            DialogueRunner runner = new DialogueRunner(DialogueLibrary.Load(scriptId)); // 진행기
            int guard = 0; // 무한 반복 방지

            while (!runner.IsFinished && guard++ < 200) // 끝까지
            {
                if (runner.IsWaitingForChoice) runner.Choose(0); // 첫 선택지 (선호)
                else runner.Advance(); // 넘기기
            }

            return runner; // 반환
        }

        [Test] // 8인 정의 · 새 게임에는 소식 없음
        public void Definitions_CoverEightRecruits_NoneAtStart() // 정의 테스트
        {
            Assert.That(RecruitService.All.Count, Is.EqualTo(8)); // 8인
            Assert.That(RecruitService.GetPending(CreateSave()), Is.Empty); // 새 게임은 소식 없음
        }

        [Test] // 던전 진행에 따라 소식이 열림 : 숲 → 클레어·메르시아, 늪 → 파이라·티리아, 마왕성 도전 → 나머지 4인
        public void Conditions_FollowDungeonProgress() // 조건 테스트
        {
            SaveData saveData = CreateSave(); // 새 게임
            DungeonProgressSaveAdapter.MarkCleared(saveData, "DG001"); // 숲 클리어
            Assert.That(PendingIds(saveData), Is.EquivalentTo(new[] { "CH_CLAIRE", "CH_MERCIA" })); // 2명
            DungeonProgressSaveAdapter.MarkCleared(saveData, "DG003"); // 늪 클리어
            Assert.That(PendingIds(saveData), Has.Member("CH_PYRA").And.Member("CH_TYRIA")); // 2명 추가
            Assert.That(PendingIds(saveData), Has.No.Member("CH_NOEL")); // 마왕성 전
            saveData.SetStoryFlag(DungeonEntryService.BuildEnteredFlag("DG004")); // 마왕성 도전
            Assert.That(PendingIds(saveData).Count, Is.EqualTo(8)); // 전원 소식
        }

        [Test] // 던전 입장에 성공하면 도전 기록이 남음
        public void DungeonEntry_SetsEnteredFlag() // 도전 기록 테스트
        {
            SaveData saveData = CreateSave(); // 새 게임 (활력 100)
            DungeonData castle = AssetDatabase.LoadAssetAtPath<DungeonData>("Assets/ProjectH/Data/Dungeons/DG004.asset"); // 마왕성
            Assert.That(DungeonEntryService.TryPayEntry(saveData, castle, out string error), Is.True, error); // 입장
            Assert.That(saveData.HasStoryFlag(DungeonEntryService.BuildEnteredFlag("DG004")), Is.True); // 도전 기록
            Assert.That(RecruitService.IsConditionMet(saveData, RecruitService.Find("CH_SEPHIRA")), Is.True); // 세피라 조건 충족
        }

        [Test] // 합류 이야기를 끝까지 봐야 동료가 되고, 두 번 합류하지 않음
        public void CompleteRecruit_RequiresFinish_AndNoDuplicate() // 합류 테스트
        {
            SaveData saveData = CreateSave(); // 새 게임
            DungeonProgressSaveAdapter.MarkCleared(saveData, "DG001"); // 숲 클리어
            DialogueRunner unfinished = new DialogueRunner(DialogueLibrary.Load("RECRUIT_CLAIRE")); // 시작만 한 대화
            RecruitService.CompleteRecruit(saveData, "CH_CLAIRE", unfinished, "클레어"); // 중간 종료
            Assert.That(saveData.HasCharacter("CH_CLAIRE"), Is.False); // 합류 안 함
            RecruitService.CompleteRecruit(saveData, "CH_CLAIRE", FinishedRunner("RECRUIT_CLAIRE"), "클레어"); // 끝까지 봄
            Assert.That(saveData.HasCharacter("CH_CLAIRE"), Is.True); // 합류
            Assert.That(AffinityService.GetAffinity(saveData, "CH_CLAIRE"), Is.GreaterThan(0)); // 선택지 호감도
            Assert.That(PendingIds(saveData), Has.No.Member("CH_CLAIRE")); // 소식 사라짐
            string again = RecruitService.CompleteRecruit(saveData, "CH_CLAIRE", FinishedRunner("RECRUIT_CLAIRE"), "클레어"); // 다시
            Assert.That(again, Does.Contain("이미")); // 중복 안내
            Assert.That(RecruitService.CompleteRecruit(saveData, "CH_NOEL", FinishedRunner("RECRUIT_NOEL"), "노엘"), Does.Contain("아직")); // 조건 미충족은 합류 불가
            Assert.That(saveData.HasCharacter("CH_NOEL"), Is.False); // 노엘 미합류
        }

        [Test] // 개발용 전원 합류 · 저장 후 불러와도 유지
        public void RecruitAll_PersistsThroughSave() // 저장 테스트
        {
            SaveData saveData = CreateSave(); // 새 게임
            Assert.That(RecruitService.RecruitAllForDebug(saveData), Is.EqualTo(8)); // 8명 합류
            Assert.That(RecruitService.RecruitAllForDebug(saveData), Is.EqualTo(0)); // 중복 없음
            SaveData loaded = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(saveData)); // 저장 → 불러오기
            Assert.That(loaded.HasCharacter("CH_SEPHIRA"), Is.True); // 유지
            Assert.That(loaded.Characters.Count, Is.EqualTo(12)); // 12인
            Assert.That(RecruitService.GetPending(loaded), Is.Empty); // 소식 없음
        }
    }
}
