using System.Collections.Generic; // 목록 자료형
using System.Linq; // 목록 검색 기능
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Diary; // 12인 목록 기능
using ProjectH.Dialogue; // 대사 파일 기능
using ProjectH.SaveSystem; // 결속 대사 ID 기능
using ProjectH.Village; // 마을·여관 대사 ID 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class CompanionStoryTests // Day70 개인·결속 이야기 확장 (12인 전원 결속 5단계 · 마을 4구역 · 특별한 밤) 테스트
    {
        private static void AssertScript(string scriptId, string characterId) // 대사 파일이 있고 화자가 맞는지
        {
            DialogueScript script = DialogueLibrary.Load(scriptId); // 대사 파일
            Assert.That(script, Is.Not.Null, scriptId); // 존재
            Assert.That(script.CharacterId, Is.EqualTo(characterId), scriptId); // 화자
            Assert.That(script.Nodes.Any(item => item.Speaker == characterId), Is.True, scriptId); // 그 동료가 최소 한 줄은 말한다 (줄 수는 대사마다 다름)
            Assert.That(DialogueLibrary.Validate(script, new List<string>()), Is.True, scriptId); // 구조 검사 (끊긴 분기 없음)
        }

        [Test] // 결속 1~5단계 대사가 12인 전원에게 있다
        public void BondScripts_ExistForEveryStageAndCharacter() // 결속 대사 테스트
        {
            Assert.That(DiaryCatalog.AllCharacters.Count, Is.EqualTo(12)); // 12인

            foreach (string characterId in DiaryCatalog.AllCharacters) // 12인 순회
            {
                for (int level = 1; level <= BondCatalog.MaxLevel; level++) // 1~5단계
                {
                    AssertScript(BondCatalog.GetBondScriptId(characterId, level), characterId); // 확인
                }
            }
        }

        [Test] // 마을 구역 이벤트 4곳이 12인 전원에게 있다
        public void VillageZoneScripts_ExistForEveryZoneAndCharacter() // 마을 대사 테스트
        {
            int zones = 0; // 이벤트 구역 수

            foreach (VillageZoneInfo info in VillageZoneCatalog.All) // 구역 순회
            {
                if (!VillageActionService.HasZoneEvent(info.Zone)) continue; // 이벤트 구역만
                zones++; // 구역 수 증가

                foreach (string characterId in DiaryCatalog.AllCharacters) // 12인 순회
                {
                    AssertScript(VillageActionService.GetZoneEventScriptId(characterId, info.Zone), characterId); // 확인
                }
            }

            Assert.That(zones, Is.EqualTo(4)); // 광장 · 시장 · 온천 · 길드
        }

        [Test] // 특별한 밤 대사가 12인 전원에게 있다 (진입 구조 : 조건 → 대사 → 다음 날 아침)
        public void InnScripts_ExistForEveryCharacter() // 특별한 밤 테스트
        {
            foreach (string characterId in DiaryCatalog.AllCharacters) // 12인 순회
            {
                AssertScript(VillageActionService.GetInnEventScriptId(characterId), characterId); // 확인
            }
        }

        [Test] // 일기장 다시 보기 목록이 새 대사를 전부 담는다
        public void Diary_ListsEveryNewScript() // 일기장 목록 테스트
        {
            Dictionary<DiaryScenarioCategory, int> counts = new Dictionary<DiaryScenarioCategory, int>(); // 분류별 수

            foreach (DiaryScenarioEntry entry in DiaryCatalog.Scenarios) // 목록 순회
            {
                counts[entry.Category] = counts.TryGetValue(entry.Category, out int value) ? value + 1 : 1; // 누적
                Assert.That(DialogueLibrary.Load(entry.ScriptId), Is.Not.Null, entry.ScriptId); // 실제 파일 확인
            }

            Assert.That(counts[DiaryScenarioCategory.Bond], Is.EqualTo(60)); // 결속 12인 × 5단계
            Assert.That(counts[DiaryScenarioCategory.Village], Is.EqualTo(48)); // 마을 12인 × 4구역
            Assert.That(counts[DiaryScenarioCategory.InnNight], Is.EqualTo(12)); // 특별한 밤 12인
            Assert.That(counts[DiaryScenarioCategory.Personal], Is.GreaterThanOrEqualTo(12)); // 개인 1화 12편 이상
        }
    }
}
