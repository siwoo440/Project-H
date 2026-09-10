using System; // 열거형 순회 기능
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 속성 기능
using ProjectH.Data; // 데이터 계층 속성 열거형

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleElementTests // Day52 속성 상성 회귀 테스트
    {
        [SetUp] // 각 테스트 시작 전 처리
        public void SetUp() // 속성 등록 초기화
        {
            BattleElementRuntimeState.ResetAll(); // 이전 테스트 잔여 등록 제거
        }

        [TearDown] // 각 테스트 종료 후 처리
        public void TearDown() // 속성 등록 정리
        {
            BattleElementRuntimeState.ResetAll(); // 다음 테스트 영향 방지
        }

        [Test] // 불·물·풀 삼각 순환 우위 검증
        public void Evaluate_ElementTriangle_ReturnsWeak() // 삼각 상성 약점 테스트
        {
            Assert.That(BattleElementAffinityTable.Evaluate(BattleElement.Fire, BattleElement.Grass), Is.EqualTo(BattleElementAffinity.Weak)); // 불이 풀에 우위인지 검증
            Assert.That(BattleElementAffinityTable.Evaluate(BattleElement.Grass, BattleElement.Water), Is.EqualTo(BattleElementAffinity.Weak)); // 풀이 물에 우위인지 검증
            Assert.That(BattleElementAffinityTable.Evaluate(BattleElement.Water, BattleElement.Fire), Is.EqualTo(BattleElementAffinity.Weak)); // 물이 불에 우위인지 검증
        }

        [Test] // 삼각 순환 역방향 저항 검증
        public void Evaluate_ReverseTriangle_ReturnsResist() // 삼각 상성 저항 테스트
        {
            Assert.That(BattleElementAffinityTable.Evaluate(BattleElement.Grass, BattleElement.Fire), Is.EqualTo(BattleElementAffinity.Resist)); // 풀이 불에 저항당하는지 검증
            Assert.That(BattleElementAffinityTable.Evaluate(BattleElement.Water, BattleElement.Grass), Is.EqualTo(BattleElementAffinity.Resist)); // 물이 풀에 저항당하는지 검증
            Assert.That(BattleElementAffinityTable.Evaluate(BattleElement.Fire, BattleElement.Water), Is.EqualTo(BattleElementAffinity.Resist)); // 불이 물에 저항당하는지 검증
        }

        [Test] // 빛에서 어둠으로 향하는 일방향 상성 검증
        public void Evaluate_LightBeatsDark_IsOneDirectional() // 빛·어둠 일방향 상성 테스트
        {
            Assert.That(BattleElementAffinityTable.Evaluate(BattleElement.Light, BattleElement.Dark), Is.EqualTo(BattleElementAffinity.Weak)); // 빛이 어둠에 우위인지 검증
            Assert.That(BattleElementAffinityTable.Evaluate(BattleElement.Dark, BattleElement.Light), Is.EqualTo(BattleElementAffinity.Resist)); // 어둠이 빛에 저항당하는지 검증
        }

        [Test] // 무속성이 빛·어둠에 약점인지 검증
        public void Evaluate_NoneDefender_IsWeakToLightAndDark() // 무속성 약점 테스트
        {
            Assert.That(BattleElementAffinityTable.Evaluate(BattleElement.Light, BattleElement.None), Is.EqualTo(BattleElementAffinity.Weak)); // 빛 공격에 무속성 약점 검증
            Assert.That(BattleElementAffinityTable.Evaluate(BattleElement.Dark, BattleElement.None), Is.EqualTo(BattleElementAffinity.Weak)); // 어둠 공격에 무속성 약점 검증
            Assert.That(BattleElementAffinityTable.Evaluate(BattleElement.Fire, BattleElement.None), Is.EqualTo(BattleElementAffinity.Neutral)); // 불 공격에는 상성 없음 검증
            Assert.That(BattleElementAffinityTable.Evaluate(BattleElement.Water, BattleElement.None), Is.EqualTo(BattleElementAffinity.Neutral)); // 물 공격에는 상성 없음 검증
            Assert.That(BattleElementAffinityTable.Evaluate(BattleElement.Grass, BattleElement.None), Is.EqualTo(BattleElementAffinity.Neutral)); // 풀 공격에는 상성 없음 검증
        }

        [Test] // 무속성 공격이 항상 상성 없음인지 검증 (기존 전투 결과 불변 보장)
        public void Evaluate_NoneAttack_IsAlwaysNeutral() // 무속성 공격 불변 테스트
        {
            foreach (BattleElement defender in Enum.GetValues(typeof(BattleElement))) // 전체 방어 속성 순회
            {
                Assert.That(BattleElementAffinityTable.Evaluate(BattleElement.None, defender), Is.EqualTo(BattleElementAffinity.Neutral)); // 무속성 공격 상성 없음 검증
            }
        }

        [Test] // 같은 속성끼리 상성 없음 검증
        public void Evaluate_SameElement_ReturnsNeutral() // 동일 속성 테스트
        {
            foreach (BattleElement element in Enum.GetValues(typeof(BattleElement))) // 전체 속성 순회
            {
                Assert.That(BattleElementAffinityTable.Evaluate(element, element), Is.EqualTo(BattleElementAffinity.Neutral)); // 동일 속성 상성 없음 검증
            }
        }

        [Test] // 상성 판정별 피해 배율 검증
        public void GetMultiplier_ReturnsExpectedValues() // 배율 값 테스트
        {
            Assert.That(BattleElementAffinityTable.GetMultiplier(BattleElementAffinity.Weak), Is.EqualTo(1.5f).Within(0.0001f)); // 약점 배율 검증
            Assert.That(BattleElementAffinityTable.GetMultiplier(BattleElementAffinity.Resist), Is.EqualTo(0.75f).Within(0.0001f)); // 저항 배율 검증
            Assert.That(BattleElementAffinityTable.GetMultiplier(BattleElementAffinity.Neutral), Is.EqualTo(1f).Within(0.0001f)); // 상성 없음 배율 검증
            Assert.That(BattleElementAffinityTable.GetMultiplier(BattleElement.Fire, BattleElement.Grass), Is.EqualTo(1.5f).Within(0.0001f)); // 속성 직접 지정 배율 검증
        }

        [Test] // 전체 속성 표시 정보 보유 검증
        public void Catalog_EveryElement_HasLabelAndShortLabel() // 속성 표시 정보 테스트
        {
            foreach (BattleElement element in Enum.GetValues(typeof(BattleElement))) // 전체 속성 순회
            {
                Assert.That(BattleElementAffinityTable.GetLabel(element), Is.Not.Empty); // 표시 문구 정의 검증
                Assert.That(BattleElementAffinityTable.GetShortLabel(element), Is.Not.Empty); // 축약 문구 정의 검증
            }
        }

        [Test] // Runtime 속성 등록과 조회 검증
        public void RuntimeState_RegistersAndResolvesElement() // 속성 등록 테스트
        {
            Assert.That(BattleElementRuntimeState.GetElement("ENEMY_0"), Is.EqualTo(BattleElement.None)); // 미등록 대상 무속성 반환 검증
            BattleElementRuntimeState.Register("ENEMY_0", BattleElement.Grass); // 적군 풀 속성 등록

            Assert.That(BattleElementRuntimeState.GetElement("ENEMY_0"), Is.EqualTo(BattleElement.Grass)); // 등록 속성 조회 검증
            Assert.That(BattleElementRuntimeState.EvaluateAgainst(BattleElement.Fire, "ENEMY_0"), Is.EqualTo(BattleElementAffinity.Weak)); // 등록 속성 기준 상성 판정 검증
        }

        [Test] // 공격 속성 상속 검증
        public void RuntimeState_ResolveAttackElement_InheritsFromCaster() // 공격 속성 상속 테스트
        {
            BattleElementRuntimeState.Register("ALLY_0", BattleElement.Fire); // 시전자 불 속성 등록

            Assert.That(BattleElementRuntimeState.ResolveAttackElement(BattleElement.None, "ALLY_0"), Is.EqualTo(BattleElement.Fire)); // 미지정 시 시전자 속성 상속 검증
            Assert.That(BattleElementRuntimeState.ResolveAttackElement(BattleElement.Water, "ALLY_0"), Is.EqualTo(BattleElement.Water)); // 명시 속성 우선 검증
            Assert.That(BattleElementRuntimeState.ResolveAttackElement(BattleElement.None, "UNKNOWN"), Is.EqualTo(BattleElement.None)); // 미등록 시전자 무속성 검증
        }

        [Test] // 데이터 계층 열거형과 전투 열거형 순서 일치 검증
        public void ElementType_MatchesBattleElementOrder() // 열거형 동기화 테스트
        {
            Array dataValues = Enum.GetValues(typeof(ElementType)); // 데이터 계층 속성 목록 조회
            Array battleValues = Enum.GetValues(typeof(BattleElement)); // 전투 계층 속성 목록 조회

            Assert.That(dataValues.Length, Is.EqualTo(battleValues.Length)); // 항목 수 일치 검증

            foreach (ElementType dataElement in dataValues) // 데이터 계층 속성 순회
            {
                Assert.That((int)(BattleElement)dataElement, Is.EqualTo((int)dataElement)); // 단순 캐스팅 변환이 안전한지 검증
                Assert.That(Enum.GetName(typeof(ElementType), dataElement), Is.EqualTo(Enum.GetName(typeof(BattleElement), (BattleElement)dataElement))); // 항목 이름 일치 검증
            }
        }
    }
}
