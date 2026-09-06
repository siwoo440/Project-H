using System.Collections.Generic; // 테스트 블록 목록 기능
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle.SkillBlock; // 스킬 블록 UI 기능
using UnityEngine; // Unity 게임 오브젝트 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleSkillBlockPanelLifecycleTests // 스킬 블록 패널 생명주기 테스트
    {
        private GameObject panelObject; // 테스트 패널 객체
        private GameObject layerObject; // 테스트 블록 레이어 객체
        private BattleSkillBlockPanel panel; // 테스트 스킬 블록 패널
        private RectTransform blockLayer; // 테스트 블록 레이어

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 테스트 패널 준비
        {
            panelObject = new GameObject("SkillBlockPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(BattleSkillBlockPanel)); // 테스트 패널 객체 생성
            layerObject = new GameObject("BlockLayer", typeof(RectTransform)); // 테스트 블록 레이어 생성
            layerObject.transform.SetParent(panelObject.transform, false); // 테스트 블록 레이어 부모 연결
            panel = panelObject.GetComponent<BattleSkillBlockPanel>(); // 테스트 패널 컴포넌트 조회
            blockLayer = layerObject.GetComponent<RectTransform>(); // 테스트 블록 레이어 조회
            panel.Configure(blockLayer, null, null, 7); // 테스트 패널 참조 설정
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 테스트 패널 정리
        {
            Object.DestroyImmediate(panelObject); // 테스트 패널 객체 제거
        }

        [Test] // 테스트 표시
        public void Refresh_RecreatesDestroyedRuntimeViewWithoutMissingReference() // 파괴된 Runtime View 참조 복구 검증
        {
            List<BattleSkillBlock> blocks = new List<BattleSkillBlock> // 테스트 블록 목록 생성
            {
                BattleSkillBlock.CreateTest("SK_TEST_01", "CH_TEST", 1) // 테스트 스킬 블록 생성
            }; // 테스트 블록 목록 종료
            panel.Refresh(blocks); // 첫 Runtime View 생성
            BattleSkillBlockView createdView = blockLayer.GetComponentInChildren<BattleSkillBlockView>(true); // 생성된 Runtime View 조회
            Assert.That(createdView, Is.Not.Null); // 첫 Runtime View 존재 검증
            Object.DestroyImmediate(createdView.gameObject); // 외부 생명주기 종료 상황처럼 Runtime View 강제 제거

            Assert.DoesNotThrow(() => panel.Refresh(blocks)); // 파괴된 참조를 보유한 상태에서도 Refresh 예외 없음 검증
            BattleSkillBlockView recreatedView = blockLayer.GetComponentInChildren<BattleSkillBlockView>(true); // 재생성된 Runtime View 조회
            Assert.That(recreatedView, Is.Not.Null); // Runtime View 재생성 검증
        }

        [Test] // 테스트 표시
        public void Refresh_DoesNothingAfterPanelIsDisabled() // 패널 종료 중 Refresh 무시 검증
        {
            List<BattleSkillBlock> blocks = new List<BattleSkillBlock> // 테스트 블록 목록 생성
            {
                BattleSkillBlock.CreateTest("SK_TEST_01", "CH_TEST", 1) // 테스트 스킬 블록 생성
            }; // 테스트 블록 목록 종료
            panel.Refresh(blocks); // 첫 Runtime View 생성
            panelObject.SetActive(false); // Scene 종료와 같은 패널 비활성 상태 적용
            BattleSkillBlockView createdView = blockLayer.GetComponentInChildren<BattleSkillBlockView>(true); // 기존 Runtime View 조회

            if (createdView != null) // 기존 Runtime View 존재 확인
            {
                Object.DestroyImmediate(createdView.gameObject); // 종료 중 Runtime View 제거
            }

            Assert.DoesNotThrow(() => panel.Refresh(blocks)); // 종료 상태의 Registry 이벤트 Refresh 예외 없음 검증
            Assert.That(blockLayer.GetComponentInChildren<BattleSkillBlockView>(true), Is.Null); // 종료 중 Runtime View 재생성 방지 검증
        }
    }
}
