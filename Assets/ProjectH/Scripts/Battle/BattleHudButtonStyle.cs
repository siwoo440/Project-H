using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleHudButtonStyle // 전투 HUD 버튼 시각 스타일 복사 기능
    {
        public static void Copy(Button source, Button target) // 원본 버튼 스타일을 대상 버튼에 복사
        {
            if (source == null || target == null) // 원본 및 대상 버튼 확인
            {
                return; // 버튼 스타일 복사 중단
            }

            Image sourceImage = source.GetComponent<Image>(); // 원본 버튼 Image 조회
            Image targetImage = target.GetComponent<Image>(); // 대상 버튼 Image 조회

            if (targetImage == null) // 대상 버튼 Image 존재 확인
            {
                targetImage = target.gameObject.AddComponent<Image>(); // 대상 버튼 Image 추가
            }

            if (sourceImage != null) // 원본 버튼 Image 존재 확인
            {
                targetImage.sprite = sourceImage.sprite; // 원본 버튼 Sprite 복사
                targetImage.overrideSprite = sourceImage.overrideSprite; // 원본 버튼 Override Sprite 복사
                targetImage.type = sourceImage.type; // 원본 버튼 Image 타입 복사
                targetImage.preserveAspect = sourceImage.preserveAspect; // 원본 버튼 비율 유지 설정 복사
                targetImage.fillCenter = sourceImage.fillCenter; // 원본 버튼 중앙 채움 설정 복사
                targetImage.color = sourceImage.color; // 원본 버튼 Image 색상 복사
                targetImage.material = sourceImage.material; // 원본 버튼 Material 복사
            }

            target.targetGraphic = targetImage; // 대상 버튼 TargetGraphic 연결
            target.transition = source.transition; // 원본 버튼 Transition 복사
            target.colors = source.colors; // 원본 버튼 ColorBlock 복사
            target.spriteState = source.spriteState; // 원본 버튼 SpriteState 복사
            target.animationTriggers = source.animationTriggers; // 원본 버튼 AnimationTrigger 복사
        }

        public static void CopyLabel(Text source, Text target) // 원본 버튼 텍스트 스타일 복사
        {
            if (source == null || target == null) // 원본 및 대상 텍스트 확인
            {
                return; // 텍스트 스타일 복사 중단
            }

            target.font = source.font; // 원본 버튼 Font 복사
            target.fontStyle = source.fontStyle; // 원본 버튼 FontStyle 복사
            target.fontSize = source.fontSize; // 원본 버튼 FontSize 복사
            target.color = source.color; // 원본 버튼 Text 색상 복사
            target.alignment = source.alignment; // 원본 버튼 Text 정렬 복사
        }
    }
}
