using System.Collections.Generic; // 목록 자료형
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Dialogue; // 대화 진행 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class DialogueRunnerTests // Day58 대화 진행기 회귀 테스트
    {
        private const string BranchJson = // 분기 테스트 대화 (작은따옴표는 Json()에서 큰따옴표로 변환)
            "{'id':'T_BRANCH','characterId':'CH_SERENA','nodes':[" +
            "{'id':'n1','speaker':'CH_SERENA','text':'하나'}," +
            "{'id':'n2','speaker':'HERO','text':'둘'}," +
            "{'id':'c1','speaker':'CH_SERENA','text':'고르세요','choices':[{'text':'A','affinity':3,'preferred':true,'next':'a1'},{'text':'B','affinity':1,'next':'b1'}]}," +
            "{'id':'a1','speaker':'CH_SERENA','text':'A 대사','next':'m1'}," +
            "{'id':'b1','speaker':'CH_SERENA','text':'B 대사','next':'m1'}," +
            "{'id':'m1','speaker':'NARRATION','text':'끝','next':'END'}," +
            "{'id':'x1','speaker':'NARRATION','text':'도달하면 안 되는 줄'}]}";

        private static string Json(string text) => text.Replace('\'', '"'); // 작은따옴표 → 큰따옴표

        private static DialogueRunner CreateRunner() // 분기 대화 진행기 생성
        {
            DialogueScript script = DialogueLibrary.Parse(Json(BranchJson)); // 대화 해석
            Assert.That(script, Is.Not.Null); // 해석 성공 검증
            return new DialogueRunner(script); // 진행기 반환
        }

        [Test] // 첫 노드 시작 검증
        public void Start_ShowsFirstNodeAndLogsIt() // 시작 테스트
        {
            DialogueRunner runner = CreateRunner(); // 진행기 생성

            Assert.That(runner.Current.Id, Is.EqualTo("n1")); // 첫 노드 검증
            Assert.That(runner.Log.Count, Is.EqualTo(1)); // 첫 대사 기록 검증
            Assert.That(runner.IsFinished, Is.False); // 진행 중 검증
        }

        [Test] // 선택지 전에는 넘어가지 않음 검증
        public void Advance_StopsAtChoiceUntilChosen() // 선택지 대기 테스트
        {
            DialogueRunner runner = CreateRunner(); // 진행기 생성
            runner.Advance(); // n2
            runner.Advance(); // c1

            Assert.That(runner.Current.Id, Is.EqualTo("c1")); // 선택지 노드 도착 검증
            Assert.That(runner.IsWaitingForChoice, Is.True); // 선택지 대기 검증
            Assert.That(runner.Advance(), Is.False); // 넘기기 거부 검증
            Assert.That(runner.Current.Id, Is.EqualTo("c1")); // 위치 유지 검증
        }

        [Test] // 선호 선택지 분기·호감도 누적 검증
        public void Choose_PreferredChoice_JumpsToBranchAndAccumulates() // 선택 분기 테스트
        {
            DialogueRunner runner = CreateRunner(); // 진행기 생성
            runner.SkipToChoiceOrEnd(); // 선택지까지 이동

            Assert.That(runner.Choose(0), Is.True); // A 선택 성공 검증
            Assert.That(runner.Current.Id, Is.EqualTo("a1")); // A 분기 이동 검증
            Assert.That(runner.AccumulatedAffinity, Is.EqualTo(3)); // 호감도 +3 누적 검증
            Assert.That(runner.PreferredChoiceCount, Is.EqualTo(1)); // 선호 선택 1회 검증
            runner.Advance(); // m1 (next 지정)
            Assert.That(runner.Current.Id, Is.EqualTo("m1")); // 합류 노드 검증
            runner.Advance(); // END
            Assert.That(runner.IsFinished, Is.True); // END에서 종료 (x1 도달 안 함) 검증
        }

        [Test] // 다른 선택지 분기 검증
        public void Choose_OtherChoice_TakesOtherBranch() // 다른 분기 테스트
        {
            DialogueRunner runner = CreateRunner(); // 진행기 생성
            runner.SkipToChoiceOrEnd(); // 선택지까지 이동
            runner.Choose(1); // B 선택

            Assert.That(runner.Current.Id, Is.EqualTo("b1")); // B 분기 이동 검증
            Assert.That(runner.AccumulatedAffinity, Is.EqualTo(1)); // 호감도 +1 검증
            Assert.That(runner.PreferredChoiceCount, Is.EqualTo(0)); // 선호 선택 없음 검증
        }

        [Test] // 건너뛰기는 선택지에서 멈춤 검증
        public void Skip_StopsAtChoiceThenRunsToEnd() // 건너뛰기 테스트
        {
            DialogueRunner runner = CreateRunner(); // 진행기 생성

            Assert.That(runner.SkipToChoiceOrEnd(), Is.EqualTo(2)); // 두 줄 넘기고 멈춤 검증
            Assert.That(runner.IsWaitingForChoice, Is.True); // 선택지 대기 검증
            runner.Choose(0); // A 선택
            runner.SkipToChoiceOrEnd(); // 끝까지 건너뛰기
            Assert.That(runner.IsFinished, Is.True); // 종료 검증
        }

        [Test] // 로그 기록 검증
        public void Log_RecordsLinesAndChoices() // 로그 테스트
        {
            DialogueRunner runner = CreateRunner(); // 진행기 생성
            runner.SkipToChoiceOrEnd(); // 선택지까지 이동
            runner.Choose(0); // A 선택
            runner.SkipToChoiceOrEnd(); // 끝까지

            List<string> texts = new List<string>(); // 로그 문구 목록
            foreach (DialogueLogEntry entry in runner.Log) texts.Add(entry.IsChoice ? $"▶{entry.Text}" : entry.Text); // 로그 수집
            Assert.That(texts, Is.EqualTo(new[] { "하나", "둘", "고르세요", "▶A", "A 대사", "끝" })); // 순서·선택 기록 검증
        }

        [Test] // 잘못된 선택 거부 검증
        public void Choose_WhenNotWaitingOrOutOfRange_ReturnsFalse() // 잘못된 선택 테스트
        {
            DialogueRunner runner = CreateRunner(); // 진행기 생성

            Assert.That(runner.Choose(0), Is.False); // 선택지 전 선택 거부 검증
            runner.SkipToChoiceOrEnd(); // 선택지까지 이동
            Assert.That(runner.Choose(5), Is.False); // 범위 밖 선택 거부 검증
            Assert.That(runner.AccumulatedAffinity, Is.EqualTo(0)); // 호감도 변화 없음 검증
        }

        [Test] // 구조 검사기 검증
        public void Validate_DetectsBrokenTargetAndUnknownSpeaker() // 검사기 테스트
        {
            DialogueScript broken = DialogueLibrary.Parse(Json("{'id':'T_BROKEN','nodes':[{'id':'n1','speaker':'BOB','text':'누구?','next':'zz'}]}")); // 잘못된 대화
            List<string> errors = new List<string>(); // 오류 목록

            Assert.That(DialogueLibrary.Validate(broken, errors), Is.False); // 검사 실패 검증
            Assert.That(errors.Count, Is.EqualTo(2)); // 화자·끊긴 분기 오류 2개 검증
            Assert.That(DialogueLibrary.Validate(DialogueLibrary.Parse(Json(BranchJson)), new List<string>()), Is.True); // 정상 대화 통과 검증
            Assert.That(DialogueLibrary.Parse("{ 깨진 json"), Is.Null); // 형식 오류 null 검증
        }
    }
}
