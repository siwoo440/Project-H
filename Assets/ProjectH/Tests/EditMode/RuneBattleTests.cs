using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 룬 기능
using ProjectH.Data; // 캐릭터 데이터 기능
using ProjectH.SaveSystem; // 룬 저장 기능
using UnityEditor; // 에셋 로드 기능
using UnityEngine; // Unity 수학 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class RuneBattleTests // Day60 룬 전투 반영 테스트
    {
        private const string Serena = "CH_SERENA"; // 테스트 캐릭터

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 전투 정적 상태 정리
        {
            BattleRuntimeStates.ResetAll(); // 통합 초기화
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 다음 테스트 영향 방지
        {
            BattleRuntimeStates.ResetAll(); // 통합 초기화
        }

        private static RuneLoadout EquipRunes(params (RuneKind kind, int grade)[] runes) // 세레나에게 룬 장착 후 합계 반환 (결속 5·레벨 10으로 슬롯 확보)
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { Serena }); // 새 게임
            saveData.FindCharacter(Serena).SetLevel(10); // 2번 슬롯
            saveData.FindCharacter(Serena).SetBondLevel(5); // 3·5번 슬롯

            for (int index = 0; index < runes.Length; index++) // 룬 순회
            {
                RuneInstanceSaveData rune = RuneService.Grant(saveData, runes[index].kind, runes[index].grade); // 지급
                int slot = index == 0 ? 0 : index == 1 ? 1 : index == 2 ? 2 : 4; // 해금 슬롯 (4번은 장비 조건이라 제외)
                Assert.That(RuneService.TryEquip(saveData, null, Serena, rune.InstanceId, slot, out string message), Is.True, message); // 장착
            }

            return RuneService.BuildLoadout(saveData, Serena); // 합계 반환
        }

        private static CharacterData LoadSerena() => AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/ProjectH/Data/Characters/CH_SERENA.asset"); // 세레나 데이터

        [Test] // 룬 없으면 스탯 그대로 검증
        public void Stats_WithoutRunes_AreUnchanged() // 기존 동작 유지 테스트
        {
            CharacterData serena = LoadSerena(); // 세레나
            BattleStats plain = BattleStatsFactory.CreateCharacter(serena, 1, "ALLY_0", BattleEquipmentStatBonus.Empty); // 기존
            BattleStats empty = BattleStatsFactory.CreateCharacter(serena, 1, "ALLY_0", BattleEquipmentStatBonus.Empty.WithRunes(RuneLoadout.Empty)); // 빈 룬

            Assert.That(empty.MaxHp, Is.EqualTo(plain.MaxHp)); // 체력 동일
            Assert.That(empty.Attack, Is.EqualTo(plain.Attack)); // 공격력 동일
            Assert.That(empty.Defense, Is.EqualTo(plain.Defense)); // 방어력 동일
        }

        [Test] // 스탯형 룬 반영 검증
        public void Stats_PowerVitalityGuardSwiftCritical_AreApplied() // 스탯 룬 테스트
        {
            CharacterData serena = LoadSerena(); // 세레나
            RuneLoadout loadout = EquipRunes((RuneKind.Power, 3), (RuneKind.Vitality, 2), (RuneKind.Critical, 1), (RuneKind.Swift, 1)); // 힘★3·체력★2·치명★1·신속★1
            BattleStats stats = BattleStatsFactory.CreateCharacter(serena, 1, "ALLY_0", BattleEquipmentStatBonus.Empty.WithRunes(loadout)); // 룬 반영 스탯

            Assert.That(stats.Attack, Is.EqualTo(Mathf.RoundToInt(serena.BaseAttack * 1.20f))); // 공격력 +20%
            Assert.That(stats.MaxHp, Is.EqualTo(Mathf.RoundToInt(serena.BaseHp * 1.10f))); // 체력 +10%
            Assert.That(stats.CriticalRate, Is.EqualTo(serena.CriticalRate + 0.10f).Within(0.0001f)); // 치명타율 +10%p
            Assert.That(stats.AttackSpeed, Is.EqualTo(serena.AttackSpeed + 0.10f).Within(0.0001f)); // 공격 속도 +0.1
        }

        [Test] // 발동형 룬 등록·제거 검증
        public void TriggerRunes_RegisterByRuntimeId_AndEmptyClears() // 등록 테스트
        {
            CharacterData serena = LoadSerena(); // 세레나
            RuneLoadout loadout = EquipRunes((RuneKind.Vampire, 2), (RuneKind.Thorns, 3)); // 흡혈★2·가시★3
            BattleStatsFactory.CreateCharacter(serena, 1, "ALLY_0", BattleEquipmentStatBonus.Empty.WithRunes(loadout)); // 등록

            Assert.That(BattleRuneRuntimeState.Get("ALLY_0", RuneKind.Vampire), Is.EqualTo(0.06f).Within(0.0001f)); // 흡혈 6%
            Assert.That(BattleRuneRuntimeState.Get("ALLY_0", RuneKind.Thorns), Is.EqualTo(0.09f).Within(0.0001f)); // 가시 9%
            BattleStatsFactory.CreateCharacter(serena, 1, "ALLY_0", BattleEquipmentStatBonus.Empty); // 룬 없이 재생성
            Assert.That(BattleRuneRuntimeState.Get("ALLY_0", RuneKind.Vampire), Is.EqualTo(0f)); // 잔여 등록 제거 (Day55 누수 방지 규칙)
        }

        [Test] // 궁극의 룬 게이지 배율 검증
        public void UltimateRune_RaisesGaugeGain() // 게이지 테스트
        {
            CharacterData serena = LoadSerena(); // 세레나
            BattleStatsFactory.CreateCharacter(serena, 1, "ALLY_0", BattleEquipmentStatBonus.Empty.WithRunes(EquipRunes((RuneKind.Ultimate, 3)))); // 궁극★3 (+25%)

            Assert.That(BattleUltimateGaugeRuntimeState.AddGauge(Serena, 20), Is.EqualTo(25)); // 20 × 1.25
        }

        [Test] // 전투의 룬 흐트러짐 배율 검증
        public void BattleRune_ScalesDisarray() // 흐트러짐 테스트
        {
            CharacterData serena = LoadSerena(); // 세레나
            BattleStatsFactory.CreateCharacter(serena, 1, "ALLY_0", BattleEquipmentStatBonus.Empty.WithRunes(EquipRunes((RuneKind.Battle, 3)))); // 전투★3 (+30%)

            Assert.That(BattleRuneEffects.ScaleDisarray("ALLY_0", 10), Is.EqualTo(13)); // 10 × 1.3
            Assert.That(BattleRuneEffects.ScaleDisarray("ENEMY_0", 10), Is.EqualTo(10)); // 룬 없는 공격자 그대로
        }

        [Test] // 전투 시작 시 등록 유지·부활 1회 검증
        public void BeginBattle_KeepsRunes_ReviveIsOncePerBattle() // 등록형 규칙 테스트
        {
            CharacterData serena = LoadSerena(); // 세레나
            BattleStatsFactory.CreateCharacter(serena, 1, "ALLY_0", BattleEquipmentStatBonus.Empty.WithRunes(EquipRunes((RuneKind.Revive, 1)))); // 부활★1

            BattleRuntimeStates.BeginBattle(null); // 전투 시작 초기화
            Assert.That(BattleRuneRuntimeState.Get("ALLY_0", RuneKind.Revive), Is.EqualTo(0.10f).Within(0.0001f)); // 등록 유지 (Day52 규칙)
            Assert.That(BattleRuneRuntimeState.TryConsumeRevive("ALLY_0"), Is.True); // 첫 부활
            Assert.That(BattleRuneRuntimeState.TryConsumeRevive("ALLY_0"), Is.False); // 두 번째 차단
            BattleRuntimeStates.BeginBattle(null); // 새 전투
            Assert.That(BattleRuneRuntimeState.TryConsumeRevive("ALLY_0"), Is.True); // 새 전투 재사용
        }
    }
}
