using System.Collections.Generic; // 목록 자료형
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 궁극기·컷인 기능
using ProjectH.Data; // 캐릭터·스킬 데이터 기능
using ProjectH.Dialogue; // 대화 파일 기능
using ProjectH.SaveSystem; // 선물 취향 기능
using ProjectH.UI; // 임시 실루엣 색 기능
using ProjectH.Village; // 합류 정의 기능
using UnityEditor; // 에셋 로드 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class NewCharacterKitTests // Day64 합류 8인 구성품 (속성·스킬 3종·궁극기·컷인·선물·색·대사) 테스트
    {
        private static readonly string[] Recruits = { "CH_NATASHA", "CH_CLAIRE", "CH_LUCIA", "CH_PYRA", "CH_TYRIA", "CH_MERCIA", "CH_NOEL", "CH_SEPHIRA" }; // 합류 8인
        private static readonly string[] GiftIds = { GiftPreferenceCatalog.Flower, GiftPreferenceCatalog.Candle, GiftPreferenceCatalog.Brooch, GiftPreferenceCatalog.StarMap, GiftPreferenceCatalog.Shell, GiftPreferenceCatalog.Sweets }; // 선물 6종

        private static CharacterData LoadCharacter(string id) => AssetDatabase.LoadAssetAtPath<CharacterData>($"Assets/ProjectH/Data/Characters/{id}.asset"); // 캐릭터 에셋

        [Test] // 8인 모두 속성과 스킬 3종(강화 1~3 효과 포함)을 가짐
        public void Recruits_HaveElementAndThreeRealSkills() // 스킬 데이터 테스트
        {
            HashSet<ElementType> elements = new HashSet<ElementType>(); // 속성 종류

            foreach (string id in Recruits) // 8인 순회
            {
                CharacterData character = LoadCharacter(id); // 캐릭터
                Assert.That(character, Is.Not.Null, id); // 에셋 존재
                elements.Add(character.Element); // 속성 기록
                Assert.That(character.Skills.Count, Is.EqualTo(3), id); // 스킬 3종

                foreach (SkillData skill in character.Skills) // 스킬 순회
                {
                    Assert.That(skill, Is.Not.Null, id); // 스킬 연결
                    Assert.That(skill.DisplayName, Does.Not.Contain(" 스킬 "), skill.Id); // 임시 이름("노엘 스킬 1") 교체
                    Assert.That(skill.Description, Is.Not.Empty, skill.Id); // 설명 있음

                    for (int level = 1; level <= 3; level++) // 강화 1~3
                    {
                        SkillEnhancementData enhancement = skill.GetEnhancement(level); // 강화 데이터
                        Assert.That(enhancement.Effects, Is.Not.Null.And.Not.Empty, $"{skill.Id} Lv{level}"); // 실제 효과 있음
                    }
                }
            }

            Assert.That(elements.Count, Is.GreaterThanOrEqualTo(5)); // 속성이 한쪽으로 몰리지 않음
        }

        [Test] // 8인 궁극기 : 지원 · 이름 · 효과 · 기합 대사
        public void Recruits_HaveUltimateAndCutInLine() // 궁극기 테스트
        {
            foreach (string id in Recruits) // 8인 순회
            {
                Assert.That(BattleUltimateEffectExecutor.IsSupported(id), Is.True, id); // 실행 지원
                Assert.That(BattleUltimateCatalog.Get(id).Effects.Count, Is.GreaterThan(0), id); // 효과 목록
                Assert.That(BattleUltimateEffectExecutor.GetUltimateName(id), Is.EqualTo(BattleUltimateCatalog.Get(id).Name), id); // 이름 연결
                Assert.That(UltimateCutInCatalog.GetLine(id), Is.Not.Empty, id); // 컷인 기합 대사
            }
        }

        [Test] // 8인 선물 취향 : 아주 좋아함·좋아함·별로 각 1개
        public void Recruits_HaveGiftPreferences() // 선물 취향 테스트
        {
            foreach (string id in Recruits) // 8인 순회
            {
                int love = 0, like = 0, dislike = 0; // 취향별 개수

                foreach (string giftId in GiftIds) // 선물 순회
                {
                    GiftPreference preference = GiftPreferenceCatalog.GetPreference(id, giftId); // 취향
                    if (preference == GiftPreference.Love) love++; // 아주 좋아함
                    else if (preference == GiftPreference.Like) like++; // 좋아함
                    else if (preference == GiftPreference.Dislike) dislike++; // 별로
                }

                Assert.That(new[] { love, like, dislike }, Is.EqualTo(new[] { 1, 1, 1 }), id); // 각 1개
            }
        }

        [Test] // 8인 임시 실루엣 색은 기본 회색이 아니고 서로 다름
        public void Recruits_HaveDistinctTints() // 색 테스트
        {
            HashSet<string> tints = new HashSet<string>(); // 색 문자열

            foreach (string id in Recruits) // 8인 순회
            {
                string tint = DialogueArtFactory.GetCharacterTint(id).ToString(); // 색
                Assert.That(tint, Is.Not.EqualTo(DialogueArtFactory.GetCharacterTint("CH_UNKNOWN").ToString()), id); // 기본 회색 아님
                Assert.That(tints.Add(tint), Is.True, id); // 중복 없음
            }
        }

        [Test] // 8인 합류 이야기 + 시간대 일상 대화 4편 : 파일 존재 · 구조 검사 · 모든 경로 종료
        public void Recruits_HaveRecruitAndTalkScripts() // 대사 파일 테스트
        {
            SaveTimeOfDay[] phases = { SaveTimeOfDay.Morning, SaveTimeOfDay.Day, SaveTimeOfDay.Evening, SaveTimeOfDay.Night }; // 시간대 4개

            foreach (string id in Recruits) // 8인 순회
            {
                RecruitDefinition recruit = RecruitService.Find(id); // 길드 합류 정의 (루시아는 챕터 1 고용이라 없음)
                List<string> scriptIds = new List<string> { recruit == null ? "CH1_02" : recruit.ScriptId }; // 합류 이야기
                foreach (SaveTimeOfDay phase in phases) scriptIds.Add(DialogueService.GetTalkScriptId(id, phase)); // 일상 대화 4편

                foreach (string scriptId in scriptIds) // 파일 순회
                {
                    DialogueScript script = DialogueLibrary.Load(scriptId); // 불러오기
                    Assert.That(script, Is.Not.Null, scriptId); // 파일 존재
                    List<string> errors = new List<string>(); // 오류 목록
                    Assert.That(DialogueLibrary.Validate(script, errors), Is.True, string.Join("\n", errors)); // 구조 검사
                    Assert.That(script.CharacterId, Is.EqualTo(id), scriptId); // 캐릭터 연결

                    for (int choice = 0; choice < 2; choice++) // 첫째·둘째 선택지 경로
                    {
                        DialogueRunner runner = new DialogueRunner(script); // 진행기
                        int guard = 0; // 무한 반복 방지

                        while (!runner.IsFinished && guard++ < 200) // 끝까지 진행
                        {
                            if (runner.IsWaitingForChoice) runner.Choose(System.Math.Min(choice, runner.Current.Choices.Count - 1)); // 선택
                            else runner.Advance(); // 넘기기
                        }

                        Assert.That(runner.IsFinished, Is.True, $"{scriptId} {choice}번 경로"); // 종료
                    }
                }
            }
        }
    }
}
