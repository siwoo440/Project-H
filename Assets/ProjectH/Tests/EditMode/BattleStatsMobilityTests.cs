using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 스탯 생성 기능
using ProjectH.Data; // 캐릭터 데이터 기능
using UnityEditor; // Unity 에디터 에셋 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleStatsMobilityTests // 캐릭터 전투 이동 수치 테스트
    {
        private const string CharacterPath = "Assets/ProjectH/Data/Characters/CH_SERENA.asset"; // 테스트 캐릭터 경로

        [Test] // 테스트 표시
        public void CreateCharacter_CopiesAttackRangeAndMoveSpeed() // 캐릭터 사거리 이동속도 반영 검증
        {
            CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>(CharacterPath); // 테스트 캐릭터 데이터 로드
            BattleStats stats = BattleStatsFactory.CreateCharacter(character, 1, "ALLY_0"); // 캐릭터 전투 스탯 생성

            Assert.That(character, Is.Not.Null); // 캐릭터 에셋 존재 검증
            Assert.That(stats.AttackRange, Is.EqualTo(character.AttackRange)); // 공격 사거리 반영 검증
            Assert.That(stats.MoveSpeed, Is.EqualTo(character.MoveSpeed)); // 이동속도 반영 검증
        }
    }
}
