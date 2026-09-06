using System.Collections.Generic; // 집합 자료형
using NUnit.Framework; // Unity 테스트 기능
using ProjectH.UI; // 던전 선택 기능

namespace ProjectH.Tests // 프로젝트 테스트 영역
{
    public sealed class DungeonSelectionRuntimeStateTests // 26일차 던전 선택 상태 테스트
    {
        [SetUp] // 테스트 초기화 지정
        public void SetUp() // 선택 상태 초기화
        {
            DungeonSelectionRuntimeState.ResetAll(); // 선택 상태 전체 초기화
        }

        [Test] // 던전 ID 목록 검증
        public void SupportedDungeonIds_AreDay26FourDungeons() // 4개 던전 ID 검증
        {
            CollectionAssert.AreEqual(new[] { "DG001", "DG002", "DG003", "DG004" }, DungeonSelectionRuntimeState.SupportedDungeonIds); // 지원 던전 ID 순서 검증
        }

        [Test] // 초기 선택 상태 검증
        public void InitialState_HasNoSelectionAndCannotEnter() // 초기 진입 차단 검증
        {
            Assert.That(DungeonSelectionRuntimeState.SelectedDungeonId, Is.Empty); // 초기 선택 ID 검증
            Assert.That(DungeonSelectionRuntimeState.CanEnter(IsExistingDungeon), Is.False); // 초기 전투 진입 차단 검증
        }

        [Test] // 유효 던전 선택 검증
        public void TrySelect_ValidSupportedDungeon_StoresSelection() // 유효 선택 저장 검증
        {
            bool selected = DungeonSelectionRuntimeState.TrySelect("DG001", IsExistingDungeon); // 첫 던전 선택 실행

            Assert.That(selected, Is.True); // 선택 성공 검증
            Assert.That(DungeonSelectionRuntimeState.SelectedDungeonId, Is.EqualTo("DG001")); // 선택 ID 저장 검증
            Assert.That(DungeonSelectionRuntimeState.CanEnter(IsExistingDungeon), Is.True); // 전투 진입 허용 검증
        }

        [Test] // 잘못된 던전 선택 검증
        public void TrySelect_UnsupportedDungeon_RejectsSelection() // 미지원 선택 차단 검증
        {
            bool selected = DungeonSelectionRuntimeState.TrySelect("DG999", IsExistingDungeon); // 미지원 던전 선택 실행

            Assert.That(selected, Is.False); // 선택 실패 검증
            Assert.That(DungeonSelectionRuntimeState.SelectedDungeonId, Is.Empty); // 선택 상태 유지 검증
        }

        [Test] // 누락 데이터 선택 검증
        public void TrySelect_MissingDungeonData_RejectsSelection() // 데이터 누락 선택 차단 검증
        {
            bool selected = DungeonSelectionRuntimeState.TrySelect("DG004", id => false); // 누락 던전 선택 실행

            Assert.That(selected, Is.False); // 선택 실패 검증
            Assert.That(DungeonSelectionRuntimeState.CanEnter(IsExistingDungeon), Is.False); // 진입 차단 검증
        }

        [Test] // 선택 전환 검증
        public void TrySelect_SecondDungeon_ReplacesCurrentSelection() // 선택 전환 저장 검증
        {
            DungeonSelectionRuntimeState.TrySelect("DG001", IsExistingDungeon); // 첫 던전 선택 실행
            DungeonSelectionRuntimeState.TrySelect("DG003", IsExistingDungeon); // 세 번째 던전 선택 실행

            Assert.That(DungeonSelectionRuntimeState.SelectedDungeonId, Is.EqualTo("DG003")); // 최종 선택 ID 검증
        }

        [Test] // 선택 데이터 소실 검증
        public void CanEnter_SelectedDungeonBecomesMissing_ReturnsFalse() // 선택 후 데이터 누락 차단 검증
        {
            DungeonSelectionRuntimeState.TrySelect("DG002", IsExistingDungeon); // 두 번째 던전 선택 실행

            Assert.That(DungeonSelectionRuntimeState.CanEnter(id => false), Is.False); // 데이터 소실 진입 차단 검증
        }

        private static bool IsExistingDungeon(string dungeonId) // 테스트 던전 존재 판정
        {
            HashSet<string> existingIds = new HashSet<string> { "DG001", "DG002", "DG003", "DG004" }; // 테스트 던전 ID 집합 생성
            return existingIds.Contains(dungeonId); // 던전 존재 결과 반환
        }
    }
}
