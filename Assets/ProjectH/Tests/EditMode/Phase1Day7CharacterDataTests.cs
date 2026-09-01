using System; // 시스템 자료형
using System.Collections.Generic; // 집합 자료형
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Data; // 프로젝트 데이터 기능
using UnityEditor; // Unity 에디터 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class Phase1Day7CharacterDataTests // 7일차 캐릭터 데이터 테스트
    {
        private const string CatalogPath = "Assets/ProjectH/Data/Database/ProjectHDataCatalog.asset"; // 데이터 카탈로그 경로

        [Test] // 테스트 표시
        public void BattlePosition_HasOnlyTankDealerHealer() // 포지션 3종 검증
        {
            BattlePosition[] values = (BattlePosition[])Enum.GetValues(typeof(BattlePosition)); // 포지션 값 조회

            Assert.That(values.Length, Is.EqualTo(3)); // 포지션 개수 검증
            Assert.That(values, Does.Contain(BattlePosition.Tank)); // 탱커 포지션 검증
            Assert.That(values, Does.Contain(BattlePosition.Dealer)); // 딜러 포지션 검증
            Assert.That(values, Does.Contain(BattlePosition.Healer)); // 힐러 포지션 검증
        }

        [Test] // 테스트 표시
        public void Catalog_ContainsTwelveUniqueCharacters() // 12인 카탈로그 검증
        {
            ProjectHDataCatalog catalog = AssetDatabase.LoadAssetAtPath<ProjectHDataCatalog>(CatalogPath); // 카탈로그 로드

            Assert.That(catalog, Is.Not.Null); // 카탈로그 존재 검증
            Assert.That(catalog.Characters.Count, Is.EqualTo(12)); // 캐릭터 수 검증

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal); // ID 중복 검사 집합

            foreach (CharacterData character in catalog.Characters) // 캐릭터 목록 순회
            {
                Assert.That(character, Is.Not.Null); // 캐릭터 에셋 존재 검증
                Assert.That(ids.Add(character.Id), Is.True, $"Duplicate character ID: {character.Id}"); // 캐릭터 ID 고유성 검증
                Assert.That(character.BaseHp, Is.GreaterThan(0)); // 체력 유효성 검증
                Assert.That(character.BaseAttack, Is.GreaterThanOrEqualTo(0)); // 공격력 유효성 검증
                Assert.That(character.BaseDefense, Is.GreaterThanOrEqualTo(0)); // 방어력 유효성 검증
                Assert.That(character.AttackSpeed, Is.GreaterThan(0f)); // 공격속도 유효성 검증
                Assert.That(character.Accuracy, Is.InRange(0f, 1f)); // 명중률 유효성 검증
                Assert.That(character.Job, Is.Not.EqualTo(CharacterJob.None)); // 직군 지정 검증
            }
        }

        [Test] // 테스트 표시
        public void CharacterPositions_MatchSimplifiedRoles() // 12인 포지션 배치 검증
        {
            AssertPosition("CH_SERENA", BattlePosition.Healer); // 세레나 힐러 검증
            AssertPosition("CH_ELLEN", BattlePosition.Tank); // 엘렌 탱커 검증
            AssertPosition("CH_LILIA", BattlePosition.Dealer); // 릴리아 딜러 검증
            AssertPosition("CH_NATASHA", BattlePosition.Dealer); // 나타샤 딜러 검증
            AssertPosition("CH_EVE", BattlePosition.Dealer); // 이브 딜러 검증
            AssertPosition("CH_CLAIRE", BattlePosition.Healer); // 클레어 힐러 검증
            AssertPosition("CH_LUCIA", BattlePosition.Dealer); // 루시아 딜러 검증
            AssertPosition("CH_PYRA", BattlePosition.Dealer); // 파이라 딜러 검증
            AssertPosition("CH_TYRIA", BattlePosition.Tank); // 티리아 탱커 검증
            AssertPosition("CH_MERCIA", BattlePosition.Dealer); // 메르시아 딜러 검증
            AssertPosition("CH_NOEL", BattlePosition.Dealer); // 노엘 딜러 검증
            AssertPosition("CH_SEPHIRA", BattlePosition.Healer); // 세피라 힐러 검증
        }

        private static void AssertPosition(string characterId, BattlePosition expected) // 캐릭터 포지션 검증
        {
            string path = $"Assets/ProjectH/Data/Characters/{characterId}.asset"; // 캐릭터 에셋 경로 생성
            CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>(path); // 캐릭터 에셋 로드

            Assert.That(character, Is.Not.Null, $"Missing character asset: {characterId}"); // 캐릭터 존재 검증
            Assert.That(character.Position, Is.EqualTo(expected), characterId); // 캐릭터 포지션 검증
        }
    }
}
