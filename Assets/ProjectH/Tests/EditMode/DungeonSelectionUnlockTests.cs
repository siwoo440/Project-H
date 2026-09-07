using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.UI; // 던전 선택 기능

namespace ProjectH.Tests.EditMode // 프로젝트 EditMode 테스트 영역
{
    public sealed class DungeonSelectionUnlockTests // 던전 선택 해금 검증 테스트
    {
        [SetUp] // 테스트 초기화 지정
        public void SetUp() // 던전 선택 상태 초기화
        {
            DungeonSelectionRuntimeState.ResetAll(); // 선택 및 해금 검사 상태 초기화
        }

        [Test] // 잠금 선택 차단 테스트 지정
        public void TrySelect_UnlockEvaluatorRejectsDungeon_DoesNotSelect() // 잠긴 던전 선택 차단 확인
        {
            DungeonSelectionRuntimeState.SetUnlockEvaluator(id => id == "DG001"); // 첫 던전만 해금 검사 설정

            bool lockedSelected = DungeonSelectionRuntimeState.TrySelect("DG002", id => true); // 잠긴 두 번째 던전 선택 시도
            bool firstSelected = DungeonSelectionRuntimeState.TrySelect("DG001", id => true); // 해금된 첫 던전 선택 시도

            Assert.IsFalse(lockedSelected); // 잠긴 던전 선택 실패 확인
            Assert.IsTrue(firstSelected); // 해금 던전 선택 성공 확인
            Assert.AreEqual("DG001", DungeonSelectionRuntimeState.SelectedDungeonId); // 최종 선택 던전 확인
        }

        [Test] // 진입 해금 재검사 테스트 지정
        public void CanEnter_UnlockStateChanges_BlocksSelectedDungeon() // 선택 후 잠금 변경 시 진입 차단 확인
        {
            bool unlocked = true; // 가변 해금 상태 생성
            DungeonSelectionRuntimeState.SetUnlockEvaluator(id => unlocked); // 가변 해금 검사 설정
            DungeonSelectionRuntimeState.TrySelect("DG001", id => true); // 첫 던전 선택
            unlocked = false; // 선택 이후 잠금 상태 전환

            Assert.IsFalse(DungeonSelectionRuntimeState.CanEnter(id => true)); // 잠금 전환 후 진입 차단 확인
        }
    }
}
