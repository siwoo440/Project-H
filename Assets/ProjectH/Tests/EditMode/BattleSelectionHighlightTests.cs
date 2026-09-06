using System.Collections.Generic; // 테스트 객체 목록 기능
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 선택 기능
using ProjectH.Data; // 캐릭터 포지션 기능
using UnityEngine; // Unity 게임 오브젝트 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleSelectionHighlightTests // 캐릭터 선택 윤곽 표시 테스트
    {
        private readonly List<GameObject> spawnedObjects = new List<GameObject>(); // 테스트 생성 객체 목록
        private GameObject registryObject; // 테스트 Registry 객체
        private BattleCombatRegistry registry; // 테스트 전투 Registry
        private BattleManualMoveController manualMoveController; // 테스트 수동 이동 컨트롤러

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 선택 윤곽 테스트 준비
        {
            BattleSelectionRuntimeState.ResetAll(); // 이전 선택 Runtime 상태 초기화
            registryObject = new GameObject("BattleSelectionHighlightTests.Registry"); // 테스트 Registry 객체 생성
            spawnedObjects.Add(registryObject); // 테스트 정리 목록 등록
            registry = registryObject.AddComponent<BattleCombatRegistry>(); // 테스트 Registry 생성
            manualMoveController = registryObject.AddComponent<BattleManualMoveController>(); // 테스트 수동 이동 컨트롤러 생성
            manualMoveController.Configure(registry, null); // 테스트 수동 이동 참조 연결
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 선택 윤곽 테스트 정리
        {
            BattleSelectionRuntimeState.ResetAll(); // 테스트 선택 Runtime 상태 초기화

            for (int index = spawnedObjects.Count - 1; index >= 0; index--) // 생성 객체 역순 순회
            {
                if (spawnedObjects[index] != null) // 현재 테스트 객체 존재 확인
                {
                    Object.DestroyImmediate(spawnedObjects[index]); // 현재 테스트 객체 즉시 제거
                }
            }

            spawnedObjects.Clear(); // 테스트 객체 목록 초기화
        }

        [Test] // 테스트 표시
        public void SelectSlot_EnablesActorAndPortraitOutline() // 선택 캐릭터와 초상화 윤곽 활성화 검증
        {
            BattleActor actor = CreateAlly("ALLY_0", "CH_SERENA", out BattleStats stats); // 첫 번째 아군 생성
            BattleHudCardView hudCard = CreateHudCard(stats); // 첫 번째 HUD 카드 생성
            manualMoveController.RegisterAlly(actor, null); // 첫 번째 아군 수동 이동 슬롯 등록

            Assert.That(manualMoveController.TrySelectSlot(1), Is.True); // 첫 번째 슬롯 선택 성공 검증
            Assert.That(BattleSelectionRuntimeState.SelectedRuntimeId, Is.EqualTo("ALLY_0")); // 선택 Runtime ID 저장 검증
            Assert.That(actor.IsSelectionHighlighted, Is.True); // 전장 캐릭터 윤곽 활성화 검증
            Assert.That(hudCard.IsSelectionHighlighted, Is.True); // HUD 초상화 윤곽 활성화 검증
        }

        [Test] // 테스트 표시
        public void SelectAnotherSlot_MovesOutlineToNewActorAndPortrait() // 다른 캐릭터 선택 시 윤곽 이전 검증
        {
            BattleActor firstActor = CreateAlly("ALLY_0", "CH_SERENA", out BattleStats firstStats); // 첫 번째 아군 생성
            BattleActor secondActor = CreateAlly("ALLY_1", "CH_ELLEN", out BattleStats secondStats); // 두 번째 아군 생성
            BattleHudCardView firstHud = CreateHudCard(firstStats); // 첫 번째 HUD 카드 생성
            BattleHudCardView secondHud = CreateHudCard(secondStats); // 두 번째 HUD 카드 생성
            manualMoveController.RegisterAlly(firstActor, null); // 첫 번째 아군 수동 이동 슬롯 등록
            manualMoveController.RegisterAlly(secondActor, null); // 두 번째 아군 수동 이동 슬롯 등록

            Assert.That(manualMoveController.TrySelectSlot(1), Is.True); // 첫 번째 슬롯 선택 검증
            Assert.That(manualMoveController.TrySelectSlot(2), Is.True); // 두 번째 슬롯 재선택 검증
            Assert.That(firstActor.IsSelectionHighlighted, Is.False); // 이전 전장 캐릭터 윤곽 해제 검증
            Assert.That(firstHud.IsSelectionHighlighted, Is.False); // 이전 HUD 초상화 윤곽 해제 검증
            Assert.That(secondActor.IsSelectionHighlighted, Is.True); // 신규 전장 캐릭터 윤곽 활성화 검증
            Assert.That(secondHud.IsSelectionHighlighted, Is.True); // 신규 HUD 초상화 윤곽 활성화 검증
        }

        [Test] // 테스트 표시
        public void DeadSelectedActor_IssueMoveClearsSelectionOutline() // 선택 캐릭터 사망 시 윤곽 해제 검증
        {
            BattleActor actor = CreateAlly("ALLY_0", "CH_EVE", out BattleStats stats); // 사망 처리 아군 생성
            BattleHudCardView hudCard = CreateHudCard(stats); // 사망 처리 HUD 카드 생성
            manualMoveController.RegisterAlly(actor, null); // 사망 처리 아군 수동 이동 슬롯 등록

            Assert.That(manualMoveController.TrySelectSlot(1), Is.True); // 사망 전 슬롯 선택 검증
            stats.SetCurrentHp(0); // 선택 캐릭터 전투 불능 처리
            Assert.That(manualMoveController.TryIssueMove(new Vector3(2f, 0f, 0f)), Is.False); // 사망 상태 이동 차단 및 선택 해제 검증
            Assert.That(BattleSelectionRuntimeState.SelectedRuntimeId, Is.Empty); // 사망 후 선택 Runtime ID 해제 검증
            Assert.That(actor.IsSelectionHighlighted, Is.False); // 사망 후 전장 캐릭터 윤곽 해제 검증
            Assert.That(hudCard.IsSelectionHighlighted, Is.False); // 사망 후 HUD 초상화 윤곽 해제 검증
        }

        private BattleActor CreateAlly(string runtimeId, string characterId, out BattleStats stats) // 선택 테스트 아군 생성
        {
            GameObject actorObject = new GameObject(runtimeId); // 테스트 아군 GameObject 생성
            spawnedObjects.Add(actorObject); // 테스트 정리 목록 등록
            BattleActor actor = actorObject.AddComponent<BattleActor>(); // 테스트 전투 액터 추가
            GameObject bodyObject = new GameObject($"{runtimeId}.Body", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 테스트 캐릭터 바디 UI 생성
            bodyObject.transform.SetParent(actorObject.transform, false); // 캐릭터 바디를 아군 하위 연결
            Image bodyImage = bodyObject.GetComponent<Image>(); // 테스트 캐릭터 바디 이미지 조회
            actor.ConfigureVisuals(bodyImage, null); // 선택 윤곽 대상 바디 연결
            stats = new BattleStats(runtimeId, characterId, characterId, BattlePosition.Dealer, 1, 1000, 100, 50, 1f, 1f, 0f); // 테스트 전투 스탯 생성
            actor.Initialize(BattleTeam.Ally, stats, Vector3.zero); // 테스트 아군 전투 초기화
            registry.Register(actor); // 테스트 아군 Registry 등록
            return actor; // 생성 테스트 아군 반환
        }

        private BattleHudCardView CreateHudCard(BattleStats stats) // 선택 테스트 HUD 카드 생성
        {
            GameObject cardObject = new GameObject($"{stats.RuntimeId}.Hud", typeof(RectTransform)); // 테스트 HUD 카드 GameObject 생성
            spawnedObjects.Add(cardObject); // 테스트 정리 목록 등록
            BattleHudCardView hudCard = cardObject.AddComponent<BattleHudCardView>(); // 테스트 HUD 카드 View 추가
            GameObject portraitObject = new GameObject($"{stats.RuntimeId}.Portrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); // 테스트 초상화 텍스트 생성
            portraitObject.transform.SetParent(cardObject.transform, false); // 초상화 텍스트 HUD 하위 연결
            Text portraitText = portraitObject.GetComponent<Text>(); // 테스트 초상화 텍스트 조회
            hudCard.Configure(null, null, portraitText, null, null, null); // 테스트 초상화 HUD 참조 연결
            hudCard.Bind(stats); // 테스트 HUD 전투 스탯 연결
            return hudCard; // 생성 테스트 HUD 카드 반환
        }
    }
}
