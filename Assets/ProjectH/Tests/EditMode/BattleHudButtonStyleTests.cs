using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 HUD 버튼 스타일 기능
using UnityEngine; // Unity 게임 오브젝트 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleHudButtonStyleTests // HUD 버튼 스타일 복사 테스트
    {
        private GameObject sourceObject; // 원본 버튼 객체
        private GameObject targetObject; // 대상 버튼 객체

        [SetUp] // 테스트 준비 표시
        public void SetUp() // HUD 버튼 객체 준비
        {
            sourceObject = new GameObject("SourceButton", typeof(RectTransform), typeof(Image), typeof(Button)); // 원본 버튼 생성
            targetObject = new GameObject("TargetButton", typeof(RectTransform), typeof(Image), typeof(Button)); // 대상 버튼 생성
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // HUD 버튼 객체 정리
        {
            Object.DestroyImmediate(sourceObject); // 원본 버튼 제거
            Object.DestroyImmediate(targetObject); // 대상 버튼 제거
        }

        [Test] // 테스트 표시
        public void Copy_CopiesVisualAndTransitionSettings() // 버튼 시각 및 Transition 복사 검증
        {
            Button sourceButton = sourceObject.GetComponent<Button>(); // 원본 Button 조회
            Button targetButton = targetObject.GetComponent<Button>(); // 대상 Button 조회
            Image sourceImage = sourceObject.GetComponent<Image>(); // 원본 Image 조회
            Image targetImage = targetObject.GetComponent<Image>(); // 대상 Image 조회
            sourceImage.color = new Color(0.21f, 0.42f, 0.63f, 0.91f); // 원본 Image 색상 설정
            sourceImage.type = Image.Type.Sliced; // 원본 Image 타입 설정
            sourceButton.transition = Selectable.Transition.ColorTint; // 원본 Transition 설정
            ColorBlock colors = sourceButton.colors; // 원본 ColorBlock 조회
            colors.colorMultiplier = 1.3f; // 원본 ColorBlock 배율 변경
            sourceButton.colors = colors; // 원본 ColorBlock 적용

            BattleHudButtonStyle.Copy(sourceButton, targetButton); // HUD 버튼 스타일 복사

            Assert.That(targetImage.color, Is.EqualTo(sourceImage.color)); // Image 색상 복사 검증
            Assert.That(targetImage.type, Is.EqualTo(sourceImage.type)); // Image 타입 복사 검증
            Assert.That(targetButton.transition, Is.EqualTo(sourceButton.transition)); // Transition 복사 검증
            Assert.That(targetButton.colors.colorMultiplier, Is.EqualTo(sourceButton.colors.colorMultiplier)); // ColorBlock 복사 검증
            Assert.That(targetButton.targetGraphic, Is.SameAs(targetImage)); // TargetGraphic 대상 Image 검증
        }
    }
}
