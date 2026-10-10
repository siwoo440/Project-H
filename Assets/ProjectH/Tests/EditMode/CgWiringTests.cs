using System.Collections.Generic; // 목록 자료형
using System.Linq; // 목록 비교 기능
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Dialogue; // 대사 파일 · 진행기 기능
using ProjectH.Diary; // 일기장 CG 목록 기능
using UnityEngine; // Resources 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class CgWiringTests // Day89 이벤트 CG 연결 테스트 (일기장의 칸과 대사의 CG 표시가 서로 맞는지)
    {
        [Test] // 보관함 : 개인 1화 12 + 결속 5단계 12 + 엔딩 4 = 28칸이고, 그림을 그리지 않는 특별한 밤 칸은 없다
        public void Catalog_HasTwentyEightSlots() // CG 칸 구성 테스트
        {
            List<string> ids = DiaryCatalog.Cgs.Select(cg => cg.Id).ToList(); // CG 이름
            Assert.That(ids.Count, Is.EqualTo(28)); // 28칸
            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Count)); // 이름 중복 없음
            Assert.That(ids.Count(id => id.EndsWith("_EVENT1")), Is.EqualTo(12)); // 개인 1화 12
            Assert.That(ids.Count(id => id.EndsWith("_BOND5")), Is.EqualTo(12)); // 결속 5단계 12
            Assert.That(ids.Count(id => id.StartsWith("CG_ENDING_")), Is.EqualTo(4)); // 엔딩 4
            Assert.That(ids.Any(id => id.EndsWith("_INN")), Is.False); // 특별한 밤 칸 없음
            Assert.That(ids, Does.Contain("CG_SERENA_EVENT1")); // 이름 규칙 (개인 1화)
            Assert.That(ids, Does.Contain("CG_SERENA_BOND5")); // 이름 규칙 (결속)
            Assert.That(ids, Does.Contain("CG_ENDING_01")); // 이름 규칙 (엔딩)
        }

        [Test] // 그림 : 28칸 모두 그림 파일이 있고 가로 16:9다 (이름이 틀리거나 빠지면 그 장면에서 CG가 조용히 뜨지 않는다)
        public void EverySlot_HasWideArt() // CG 그림 설치 테스트
        {
            foreach (DiaryCgEntry cg in DiaryCatalog.Cgs) // CG 순회
            {
                Sprite art = ProjectH.UI.RuntimeSpriteLoader.Load("Diary/CG/" + cg.Id); // CG 그림
                Assert.That(art, Is.Not.Null, $"CG 그림 없음 : Resources/Diary/CG/{cg.Id}.png"); // 있음
                Assert.That(art.rect.width / art.rect.height, Is.EqualTo(16f / 9f).Within(0.01f), cg.Id); // 화면과 같은 가로 16:9 (잘리는 부분이 거의 없다)
            }
        }

        [Test] // 엔딩 칸 : 캐릭터가 없고, 나머지 칸은 캐릭터가 있다
        public void EndingSlots_HaveNoCharacter() // 엔딩 칸 테스트
        {
            foreach (DiaryCgEntry cg in DiaryCatalog.Cgs) // CG 순회
            {
                Assert.That(cg.IsEnding, Is.EqualTo(cg.Id.StartsWith("CG_ENDING_")), cg.Id); // 엔딩 여부가 이름과 일치
                Assert.That(string.IsNullOrEmpty(cg.CharacterId), Is.EqualTo(cg.IsEnding), cg.Id); // 엔딩만 캐릭터 없음
                Assert.That(cg.ScriptId, Is.Not.Empty, cg.Id); // 연결 대사 있음
            }
        }

        [Test] // 칸 → 대사 : 칸마다 그 CG를 켜는 대사가 딱 한 줄 있고, 어느 선택지를 골라도 그 줄을 지나며, 한번 켜지면 끝까지 꺼지지 않는다
        public void EverySlot_IsTurnedOnByItsScriptOnEveryPath() // CG 표시 테스트
        {
            foreach (DiaryCgEntry cg in DiaryCatalog.Cgs) // CG 순회
            {
                DialogueScript script = DialogueLibrary.Load(cg.ScriptId); // 연결 대사
                Assert.That(script, Is.Not.Null, $"대사 없음 : {cg.ScriptId}"); // 존재
                List<string> marks = script.Nodes.Where(node => !string.IsNullOrEmpty(node.Cg)).Select(node => node.Cg).ToList(); // 대사에 적힌 CG 표시
                Assert.That(marks, Is.EqualTo(new[] { cg.Id }), $"CG 표시가 한 줄이 아니거나 이름이 다름 : {cg.ScriptId}"); // 딱 한 줄 · 같은 이름 (끄는 표시 없음)

                foreach (List<DialogueNode> path in WalkAllPaths(script)) // 선택지 조합마다
                {
                    Assert.That(path.Any(node => node.Cg == cg.Id), Is.True, $"CG를 지나지 않는 길이 있음 : {cg.ScriptId}"); // 어느 길로 가도 본다
                }
            }
        }

        [Test] // 대사 → 칸 : 대사에 적힌 CG 이름은 모두 보관함에 등록되어 있다 (이름을 잘못 적으면 그림이 조용히 안 뜬다)
        public void EveryCgInDialogues_IsRegistered() // CG 이름 등록 테스트
        {
            HashSet<string> registered = new HashSet<string>(DiaryCatalog.Cgs.Select(cg => cg.Id)); // 보관함의 이름
            int found = 0; // 찾은 CG 표시 수

            foreach (TextAsset asset in Resources.LoadAll<TextAsset>("Dialogues")) // 모든 대사 파일
            {
                DialogueScript script = DialogueLibrary.Parse(asset.text); // 해석
                if (script == null) continue; // 대사 파일이 아님

                foreach (DialogueNode node in script.Nodes) // 줄 순회
                {
                    if (string.IsNullOrEmpty(node.Cg) || node.Cg == "-" || node.Cg == "CG_TEST") continue; // 표시 없음 · 끄기 · 개발용 테스트 그림
                    Assert.That(registered.Contains(node.Cg), Is.True, $"보관함에 없는 CG : {node.Cg} ({asset.name})"); // 등록됨
                    found++; // 집계
                }
            }

            Assert.That(found, Is.EqualTo(28)); // 대사 28편에 한 줄씩
        }

        private static List<List<DialogueNode>> WalkAllPaths(DialogueScript script) // 선택지를 모든 조합으로 골라 본 길 목록 (길 = 지나간 대사들)
        {
            List<List<DialogueNode>> paths = new List<List<DialogueNode>>(); // 결과
            Queue<List<int>> pending = new Queue<List<int>>(); // 아직 가 보지 않은 선택 순서
            pending.Enqueue(new List<int>()); // 처음에는 아무것도 고르지 않은 상태

            while (pending.Count > 0 && paths.Count < 64) // 조합 순회 (끝없이 늘지 않게 상한)
            {
                List<int> picks = pending.Dequeue(); // 이번에 따라갈 선택 순서
                DialogueRunner runner = new DialogueRunner(script); // 처음부터 진행
                List<DialogueNode> visited = new List<DialogueNode>(); // 지나간 대사
                int used = 0; // 쓴 선택 수
                bool branched = false; // 새 갈림길을 만났는지

                for (int guard = 0; guard < 500 && !runner.IsFinished; guard++) // 끝까지 진행 (돌고 도는 대사 방지)
                {
                    if (runner.Current != null) visited.Add(runner.Current); // 지나간 대사 기록

                    if (!runner.IsWaitingForChoice) // 보통 대사
                    {
                        runner.Advance(); // 다음 줄
                        continue; // 계속
                    }

                    if (used < picks.Count) // 정해 둔 선택이 남음
                    {
                        runner.Choose(picks[used++]); // 그 선택으로
                        continue; // 계속
                    }

                    for (int choice = 0; choice < runner.Current.Choices.Count; choice++) // 새 갈림길 : 선택지마다 따로 가 본다
                    {
                        pending.Enqueue(new List<int>(picks) { choice }); // 선택 순서 추가
                    }

                    branched = true; // 이 길은 여기서 멈춤
                    break; // 종료
                }

                if (!branched) paths.Add(visited); // 끝까지 간 길만 기록
            }

            return paths; // 길 목록
        }
    }
}
