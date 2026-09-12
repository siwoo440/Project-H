using System.Linq; // 목록 검색 기능
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Data; // 장비 슬롯·데이터 기능
using ProjectH.Diary; // 12인 목록 기능
using ProjectH.SaveSystem; // 저장·전용 장비·룬 기능
using UnityEditor; // 에셋 로드 기능
using UnityEngine; // Unity 오브젝트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class UniqueEquipmentTests // Day70 전용 장비 12종 · 결속 초월 · 룬 5번 칸 테스트
    {
        private const string WeaponScrollC = "IT_SCROLL_WEAPON_C"; // 무기 주문서 C
        private GameObject dataObject; // 데이터 관리자 오브젝트
        private DataManager dataManager; // 실제 카탈로그 데이터 관리자

        private sealed class AlwaysSuccessRandom : System.Random // 강화를 반드시 성공시키는 난수
        {
            public override double NextDouble() => 0.0; // 항상 성공
        }

        private static readonly System.Random AlwaysSuccess = new AlwaysSuccessRandom(); // 공유 난수

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 실제 카탈로그 준비 (전용 장비 24개 에셋 등록 검증 포함)
        {
            dataObject = new GameObject("UniqueEquipmentTests"); // 오브젝트 생성
            dataManager = dataObject.AddComponent<DataManager>(); // 컴포넌트 추가
            dataManager.Configure(AssetDatabase.LoadAssetAtPath<ProjectHDataCatalog>("Assets/ProjectH/Data/Database/ProjectHDataCatalog.asset")); // 실제 카탈로그
            dataManager.Initialize(); // 초기화
            Assert.That(dataManager.IsInitialized, Is.True, string.Join("\n", dataManager.ValidationErrors)); // 초기화 검증
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 정리
        {
            Object.DestroyImmediate(dataObject); // 오브젝트 제거
        }

        private SaveData CreateSave(string characterId, int gold = 200000) // 그 동료 · 골드 · 주문서를 가진 새 게임
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { characterId }); // 새 게임
            GoldCurrencyService.TrySpendGold(saveData, GoldCurrencyService.GetGold(saveData), out _); // 시작 골드 비우기
            GoldCurrencyService.AddGold(saveData, gold); // 골드 지급
            ItemInventoryService.TryAdd(saveData, dataManager, WeaponScrollC, 20, out _); // 주문서 지급
            return saveData; // 저장 반환
        }

        private EquipmentInstanceSaveData GrantAndEquip(SaveData saveData, string characterId) // 전용 장비를 받아 착용
        {
            Assert.That(UniqueEquipmentService.TryGrant(saveData, characterId, out EquipmentInstanceSaveData instance, out _), Is.True); // 지급
            Assert.That(CharacterEquipmentService.TryEquip(saveData, dataManager, characterId, instance.InstanceId, out string error), Is.True, error); // 착용
            return instance; // 인스턴스 반환
        }

        private void EnhanceToMax(SaveData saveData, EquipmentInstanceSaveData instance) // +5까지 강제 성공
        {
            while (instance.EnhanceLevel < EquipmentUpgradeCatalog.MaxEnhanceLevel) // 목표까지
            {
                Assert.That(EquipmentUpgradeService.TryEnhance(saveData, dataManager, instance.InstanceId, WeaponScrollC, out string message, AlwaysSuccess), Is.EqualTo(EnhanceOutcome.Success), message); // 성공
            }
        }

        [Test] // 전용 장비는 12인 전원에게 하나씩 있고, 데이터 에셋도 전부 등록되어 있다
        public void Catalog_CoversTwelveCharactersAndAssetsExist() // 목록 테스트
        {
            Assert.That(UniqueEquipmentCatalog.All.Count, Is.EqualTo(12)); // 12종
            Assert.That(UniqueEquipmentCatalog.All.Select(item => item.EquipmentId).Distinct().Count(), Is.EqualTo(12)); // ID 중복 없음
            Assert.That(UniqueEquipmentCatalog.All.Select(item => item.DisplayName).Distinct().Count(), Is.EqualTo(12)); // 이름 중복 없음

            foreach (string characterId in DiaryCatalog.AllCharacters) // 12인 순회
            {
                UniqueEquipmentDefinition definition = UniqueEquipmentCatalog.FindByCharacter(characterId); // 정의 조회
                Assert.That(definition, Is.Not.Null, characterId); // 존재
                Assert.That(definition.Attack, Is.GreaterThan(0), characterId); // 공격력
                Assert.That(definition.SubStatText, Is.Not.Empty, characterId); // 보조 옵션
                Assert.That(UniqueEquipmentCatalog.GetOwnerId(definition.EquipmentId), Is.EqualTo(characterId)); // 주인 확인
                EquipmentData equipment = dataManager.GetEquipment(definition.EquipmentId); // 장비 에셋
                Assert.That(equipment, Is.Not.Null, definition.EquipmentId); // 등록 확인
                Assert.That(equipment.Slot, Is.EqualTo(EquipmentSlot.Weapon), definition.EquipmentId); // 전용 장비는 무기 칸
                Assert.That(equipment.Grade, Is.EqualTo(ItemGrade.Legendary), definition.EquipmentId); // 전설 등급
                Assert.That(equipment.GetStatValue(EquipmentStatType.Attack), Is.EqualTo((float)definition.Attack).Within(0.01f), definition.EquipmentId); // 공격력 일치
            }

            Assert.That(UniqueEquipmentCatalog.IsUnique("EQ_WEAPON_IRON"), Is.False); // 일반 장비는 아님
            Assert.That(UniqueEquipmentCatalog.GetOwnerId("EQ_WEAPON_IRON"), Is.Empty); // 주인 없음
        }

        [Test] // 지급은 한 번만, 착용은 주인만
        public void Grant_IsOnceAndOwnerOnly() // 지급·착용 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_PYRA", "CH_ELLEN" }); // 파이라 · 엘렌
            Assert.That(UniqueEquipmentService.HasUnique(saveData, "CH_PYRA"), Is.False); // 처음에는 없음
            Assert.That(UniqueEquipmentService.TryGrant(saveData, "CH_PYRA", out EquipmentInstanceSaveData first, out string message), Is.True); // 지급
            Assert.That(message, Does.Contain("붉은 서약창")); // 안내
            Assert.That(UniqueEquipmentService.TryGrant(saveData, "CH_PYRA", out EquipmentInstanceSaveData second, out _), Is.False); // 중복 지급 없음
            Assert.That(second.InstanceId, Is.EqualTo(first.InstanceId)); // 같은 장비
            Assert.That(saveData.EquipmentInventory.Count(item => item.EquipmentId == "EQ_UNIQUE_PYRA"), Is.EqualTo(1)); // 하나만
            Assert.That(CharacterEquipmentService.TryEquip(saveData, dataManager, "CH_ELLEN", first.InstanceId, out string error), Is.False); // 남이 착용 불가
            Assert.That(error, Does.Contain("전용 장비")); // 안내
            Assert.That(CharacterEquipmentService.TryEquip(saveData, dataManager, "CH_PYRA", first.InstanceId, out _), Is.True); // 주인은 가능
        }

        [Test] // 초월은 같은 장비 대신 결속 단계로 한다 (+5 · 결속 3단계 · 골드)
        public void Transcend_UsesBondInsteadOfDuplicate() // 초월 테스트
        {
            SaveData saveData = CreateSave("CH_EVE"); // 이브
            EquipmentInstanceSaveData instance = GrantAndEquip(saveData, "CH_EVE"); // 지급·착용
            Assert.That(EquipmentUpgradeService.TryTranscend(saveData, dataManager, instance.InstanceId, out string reason), Is.False); // +5 전
            Assert.That(reason, Does.Contain("강화")); // 안내
            EnhanceToMax(saveData, instance); // +5
            Assert.That(EquipmentUpgradeService.TryTranscend(saveData, dataManager, instance.InstanceId, out reason), Is.False); // 결속 부족
            Assert.That(reason, Does.Contain("결속")); // 안내 (같은 장비를 요구하지 않는다)
            saveData.FindCharacter("CH_EVE").SetBondLevel(UniqueEquipmentCatalog.TranscendBondLevel); // 결속 3단계
            int goldBefore = GoldCurrencyService.GetGold(saveData); // 초월 전 골드
            Assert.That(EquipmentUpgradeService.TryTranscend(saveData, dataManager, instance.InstanceId, out string message), Is.True, message); // 초월
            Assert.That(instance.TranscendStage, Is.EqualTo(2)); // ★2
            Assert.That(instance.EnhanceLevel, Is.EqualTo(0)); // 강화 초기화
            Assert.That(goldBefore - GoldCurrencyService.GetGold(saveData), Is.EqualTo(EquipmentUpgradeCatalog.GetTranscendGold(1))); // 골드 소모
            Assert.That(saveData.EquipmentInventory.Count(item => item.EquipmentId == "EQ_UNIQUE_EVE"), Is.EqualTo(1)); // 재료로 사라진 장비 없음
            EnhanceToMax(saveData, instance); // 다시 +5
            Assert.That(EquipmentUpgradeService.TryTranscend(saveData, dataManager, instance.InstanceId, out reason), Is.False); // ★3은 결속 5단계
            Assert.That(reason, Does.Contain($"{UniqueEquipmentCatalog.MaxTranscendBondLevel}단계")); // 안내
        }

        [Test] // 룬 5번 칸은 전용 장비 ★2 초월로 열린다
        public void RuneSlotFive_OpensWithTranscendedUnique() // 룬 칸 테스트
        {
            SaveData saveData = CreateSave("CH_LILIA"); // 릴리아
            const int lastSlot = RuneCatalog.SlotCount - 1; // 5번 칸
            Assert.That(RuneService.IsSlotUnlocked(saveData, dataManager, "CH_LILIA", lastSlot), Is.False); // 잠김
            EquipmentInstanceSaveData instance = GrantAndEquip(saveData, "CH_LILIA"); // 전용 장비 착용
            Assert.That(RuneService.IsSlotUnlocked(saveData, dataManager, "CH_LILIA", lastSlot), Is.False); // ★1은 아직
            EnhanceToMax(saveData, instance); // +5
            saveData.FindCharacter("CH_LILIA").SetBondLevel(UniqueEquipmentCatalog.TranscendBondLevel); // 결속 3단계
            Assert.That(EquipmentUpgradeService.TryTranscend(saveData, dataManager, instance.InstanceId, out string message), Is.True, message); // ★2 초월
            Assert.That(RuneService.IsSlotUnlocked(saveData, dataManager, "CH_LILIA", lastSlot), Is.True); // 열림
            CharacterEquipmentService.TryUnequip(saveData, "CH_LILIA", EquipmentSlot.Weapon, out _, out _); // 장비 해제
            Assert.That(RuneService.IsSlotUnlocked(saveData, dataManager, "CH_LILIA", lastSlot), Is.False); // 다시 잠김
            Assert.That(RuneCatalog.GetSlotRequirement(lastSlot), Does.Contain("전용 장비")); // 조건 문구
        }

        [Test] // 개인 이야기 1화는 12인 전원에게 있고, 1화를 마치면 전용 장비를 받는다
        public void PersonalEpisodeOne_ExistsForEveryoneAndGrantsUnique() // 개인 1화 테스트
        {
            foreach (string characterId in DiaryCatalog.AllCharacters) // 12인 순회
            {
                CharacterEventDefinition first = CharacterEventCatalog.GetForCharacter(characterId).FirstOrDefault(item => item.Episode == 1); // 1화
                Assert.That(first, Is.Not.Null, characterId); // 존재
                Assert.That(first.Conditions.Count, Is.GreaterThan(0), characterId); // 해금 조건
                Assert.That(ProjectH.Dialogue.DialogueLibrary.Load(first.ScriptId), Is.Not.Null, first.ScriptId); // 대사 파일
            }

            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_NOEL" }); // 노엘
            Assert.That(UniqueEquipmentService.HasUnique(saveData, "CH_NOEL"), Is.False); // 아직 없음
            UniqueEquipmentService.TryGrant(saveData, "CH_NOEL", out _, out _); // 1화 보상과 같은 경로
            Assert.That(UniqueEquipmentService.HasUnique(saveData, "CH_NOEL"), Is.True); // 획득
        }
    }
}
