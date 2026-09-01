using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.UI; // 프로젝트 UI 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class TitleScreenViewStateTests // 타이틀 화면 상태 테스트
    {
        [Test] // 테스트 표시
        public void Build_WithoutSave_DisablesContinue() // 저장 없음 상태 검증
        {
            TitleScreenViewState state = TitleScreenViewState.Build(false); // 저장 없음 화면 상태 생성

            Assert.That(state.CanContinue, Is.False); // 이어하기 비활성 검증
            Assert.That(state.StatusText, Does.Contain("새로운 여정")); // 신규 여정 안내 검증
        }

        [Test] // 테스트 표시
        public void Build_WithSave_EnablesContinue() // 저장 있음 상태 검증
        {
            TitleScreenViewState state = TitleScreenViewState.Build(true); // 저장 있음 화면 상태 생성

            Assert.That(state.CanContinue, Is.True); // 이어하기 활성 검증
            Assert.That(state.StatusText, Does.Contain("저장된 여정")); // 저장 여정 안내 검증
        }
    }
}
