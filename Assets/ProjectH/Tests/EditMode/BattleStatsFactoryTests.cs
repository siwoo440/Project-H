using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 런타임 기능
using ProjectH.Data; // 캐릭터 데이터 기능
using ProjectH.SaveSystem; // 캐릭터 저장 기능
using UnityEditor; // Unity 에디터 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleStatsFactoryTests // 전투 스탯 생성 테스트
    {
        private const string SerenaPath = "Assets/ProjectH/Data/Characters/CH_SERENA.asset"; // 세레나 에셋 경로

        [Test] // 테스트 표시
        public void CreateCharacter_LevelOneMatchesCharacterBaseStats() // 1레벨 원본 스탯 검증
        {
            CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>(SerenaPath); // 세레나 데이터 로드
            CharacterSaveData saveData = new CharacterSaveData("CH_SERENA"); // 1레벨 저장 데이터 생성

            BattleStats stats = BattleStatsFactory.CreateCharacter(character, saveData, "ALLY_0"); // 런타임 스탯 생성

            Assert.That(stats.CharacterId, Is.EqualTo(character.Id)); // 캐릭터 ID 검증
            Assert.That(stats.Level, Is.EqualTo(1)); // 캐릭터 레벨 검증
            Assert.That(stats.MaxHp, Is.EqualTo(character.BaseHp)); // 기본 체력 검증
            Assert.That(stats.Attack, Is.EqualTo(character.BaseAttack)); // 기본 공격력 검증
            Assert.That(stats.Defense, Is.EqualTo(character.BaseDefense)); // 기본 방어력 검증
            Assert.That(stats.Accuracy, Is.EqualTo(character.Accuracy).Within(0.0001f)); // 명중률 검증
        }

        [Test] // 테스트 표시
        public void CreateCharacter_LevelFiveUsesPrototypeGrowth() // 5레벨 성장 공식 검증
        {
            CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>(SerenaPath); // 세레나 데이터 로드
            CharacterSaveData saveData = new CharacterSaveData("CH_SERENA"); // 저장 데이터 생성
            saveData.SetLevel(5); // 5레벨 설정

            BattleStats stats = BattleStatsFactory.CreateCharacter(character, saveData, "ALLY_0"); // 런타임 스탯 생성

            Assert.That(BattleGrowthFormula.GetLevelMultiplier(5), Is.EqualTo(1.20f).Within(0.0001f)); // 성장 배율 검증
            Assert.That(stats.MaxHp, Is.EqualTo(2640)); // 성장 체력 검증
            Assert.That(stats.Attack, Is.EqualTo(216)); // 성장 공격력 검증
            Assert.That(stats.Defense, Is.EqualTo(144)); // 성장 방어력 검증
        }

        [Test] // 테스트 표시
        public void CreateCharacter_DoesNotModifyCharacterAsset() // 원본 에셋 불변 검증
        {
            CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>(SerenaPath); // 세레나 데이터 로드
            int originalHp = character.BaseHp; // 원본 체력 저장
            CharacterSaveData saveData = new CharacterSaveData("CH_SERENA"); // 저장 데이터 생성
            saveData.SetLevel(10); // 테스트 레벨 설정

            BattleStats stats = BattleStatsFactory.CreateCharacter(character, saveData, "ALLY_0"); // 런타임 스탯 생성
            stats.TakeDamage(stats.MaxHp); // 런타임 체력 변경

            Assert.That(character.BaseHp, Is.EqualTo(originalHp)); // 원본 체력 유지 검증
        }
    }
}
