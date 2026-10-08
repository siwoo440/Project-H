using System.Collections.Generic; // 목록 자료형
using System.IO; // 파일 입출력
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.UI; // 게임 폰트 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Text 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class GameFontTests // Day81 게임 폰트 테스트 (폰트 파일 하나로 게임 전체 글자가 바뀌어야 한다)
    {
        [Test] // 폰트는 늘 있다. 정식 폰트 파일이 있을 때만 "정식 폰트 사용 중"이다
        public void DefaultFont_AlwaysExists() // 기본 폰트 테스트
        {
            Assert.That(RuntimeUiKit.DefaultFont, Is.Not.Null); // 폰트 있음
            bool hasFile = Resources.Load<Font>(RuntimeUiKit.GameFontResourcePath) != null; // 정식 폰트 파일 유무
            Assert.That(RuntimeUiKit.HasGameFont, Is.EqualTo(hasFile)); // 파일이 있을 때만 정식 폰트
        }

        [Test] // 씬에 놓인 글자 교체 : 꺼져 있는 글자까지 한 번씩 바꾸고, 다시 불러도 또 바꾸지 않는다
        public void ScenePatch_ReplacesEveryTextOnce() // 글자 교체 테스트
        {
            GameObject root = new GameObject("Root", typeof(RectTransform)); // 씬 루트 역할
            Font replacement = Font.CreateDynamicFontFromOSFont("Arial", 16); // 바꿔 넣을 폰트

            try // 정리 보장
            {
                Text visible = CreateText(root.transform, "Visible"); // 보이는 글자
                Text hidden = CreateText(root.transform, "Hidden"); // 꺼져 있는 글자
                hidden.gameObject.SetActive(false); // 끄기
                Assert.That(GameFontScenePatch.Apply(root.transform, replacement), Is.EqualTo(2)); // 둘 다 교체
                Assert.That(visible.font, Is.SameAs(replacement)); // 보이는 글자
                Assert.That(hidden.font, Is.SameAs(replacement)); // 꺼져 있는 글자
                Assert.That(GameFontScenePatch.Apply(root.transform, replacement), Is.EqualTo(0)); // 이미 같은 폰트
                Assert.That(GameFontScenePatch.Apply(null, replacement), Is.EqualTo(0)); // null 안전
                Assert.That(GameFontScenePatch.Apply(root.transform, null), Is.EqualTo(0)); // 폰트 없음
            }
            finally // 임시 객체 정리
            {
                Object.DestroyImmediate(root); // 루트와 하위 제거
                Object.DestroyImmediate(replacement); // 폰트 제거
            }
        }

        [Test] // 기본 폰트를 직접 지정하는 스크립트가 없다 (있으면 정식 폰트를 넣어도 그 글자만 바뀌지 않는다)
        public void NoScript_PinsTheBuiltinFont() // 폰트 직접 지정 금지 테스트
        {
            List<string> offenders = new List<string>(); // 직접 지정한 파일
            string scriptsRoot = Path.Combine(Application.dataPath, "ProjectH", "Scripts"); // 스크립트 폴더

            foreach (string file in Directory.GetFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories)) // 스크립트 순회
            {
                string normalized = file.Replace('\\', '/'); // 경로 구분자 통일
                if (normalized.Contains("/Editor/") || normalized.EndsWith("/RuntimeUiKit.cs")) continue; // 에디터 도구 · 폰트를 정하는 곳은 제외
                if (File.ReadAllText(file).Contains("GetBuiltinResource<Font>")) offenders.Add(Path.GetFileName(file)); // 직접 지정 발견
            }

            Assert.That(offenders, Is.Empty, "RuntimeUiKit.DefaultFont를 써 주세요 : " + string.Join(", ", offenders)); // 없음
        }

        private static Text CreateText(Transform parent, string name) // 기본 폰트를 쓰는 글자 생성
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // 글자 객체
            textObject.transform.SetParent(parent, false); // 부모 연결
            Text text = textObject.GetComponent<Text>(); // 글자
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 (씬에 놓인 글자와 같은 상태)
            return text; // 반환
        }
    }
}
