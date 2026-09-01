using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 HUD 버튼 라벨 기능
using UnityEngine; // Unity 게임 오브젝트 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleHudButtonLabelUtilityTests // HUD 버튼 단일 라벨 유틸리티 테스트
    {
        private GameObject buttonObject; // 테스트 버튼 객체
        private Button button; // 테스트 Button 컴포넌트

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 테스트 버튼 준비
        {
            buttonObject = new GameObject("SpeedButton", typeof(RectTransform), typeof(Image), typeof(Button)); // 테스트 버튼 생성
            button = buttonObject.GetComponent<Button>(); // 테스트 Button 조회
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 테스트 버튼 정리
        {
            Object.DestroyImmediate(buttonObject); // 테스트 버튼 제거
        }

        [Test] // 테스트 표시
        public void EnsureSingleLabel_RemovesExtraTextsAndKeepsOneLabel() // 중복 라벨 정리 검증
        {
            CreateText("LabelA"); // 첫 번째 라벨 생성
            CreateText("LabelB"); // 두 번째 라벨 생성
            CreateText("LabelC"); // 세 번째 라벨 생성

            Text label = BattleHudButtonLabelUtility.EnsureSingleLabel(button, "STATE"); // 단일 라벨 정리 실행
            Text[] labels = button.GetComponentsInChildren<Text>(true); // 버튼 하위 Text 목록 조회

            Assert.That(labels.Length, Is.EqualTo(1)); // 최종 Text 1개 유지 검증
            Assert.That(labels[0], Is.SameAs(label)); // 반환 라벨과 실제 단일 라벨 일치 검증
            Assert.That(label.text, Is.EqualTo("STATE")); // 단일 라벨 문구 적용 검증
        }

        private void CreateText(string name) // 테스트용 중복 라벨 생성
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // 테스트 라벨 객체 생성
            textObject.transform.SetParent(buttonObject.transform, false); // 테스트 버튼 하위 연결
            Text text = textObject.GetComponent<Text>(); // 테스트 라벨 Text 조회
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 적용
            text.text = name; // 테스트 라벨 문구 적용
        }
    }
}
