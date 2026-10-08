using System.Collections.Generic; // 목록 자료형
using System.Linq; // 목록 검색 기능
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Diary; // 12인 목록 기능
using ProjectH.Dialogue; // 표정 연결표·대사 파일 기능
using ProjectH.UI; // 스탠딩·배경 조회 기능
using UnityEditor; // 에셋 조회 기능
using UnityEngine; // 텍스처·스프라이트 기능
using UnityEngine.UI; // Image·비율 맞춤 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class ExpressionTests // Day76 표정 연결표 · 스탠딩 찾는 순서 · 배경 대체 · 배경 비율 맞춤 테스트
    {
        private const string DialogueRoot = "Assets/ProjectH/Resources/Dialogues"; // 대사 폴더
        private const string ResourcesRoot = "Assets/ProjectH/Resources/"; // Resources 폴더

        [TestCase("기본", "normal")] // 평소 얼굴
        [TestCase("미소", "smile")] // 웃음
        [TestCase("흥미", "smile")] // 흥미 → 웃음
        [TestCase("진지", "serious")] // 진지
        [TestCase("각오", "serious")] // 각오 → 진지
        [TestCase("생각", "serious")] // 생각 → 진지
        [TestCase("경계", "serious")] // 경계 → 진지
        [TestCase("부끄러움", "shy")] // 수줍음
        [TestCase("놀람", "surprised")] // 놀람
        [TestCase("당황", "surprised")] // 당황 → 놀람
        [TestCase("슬픔", "sad")] // 슬픔
        [TestCase("상처", "sad")] // 상처 → 슬픔
        [TestCase("화남", "angry")] // 화남
        public void Resolve_MapsDialogueNamesToArtKeys(string name, string expected) // 대사 표정 이름 → 그림 표정 키 테스트
        {
            Assert.That(ExpressionCatalog.Resolve(name), Is.EqualTo(expected)); // 연결 확인
        }

        [Test] // 빈 값·모르는 이름은 기본, 그림 키는 그대로
        public void Resolve_FallsBackToNormal_AndKeepsArtKeys() // 대체·통과 테스트
        {
            Assert.That(ExpressionCatalog.Resolve(null), Is.EqualTo(ExpressionCatalog.Normal)); // null
            Assert.That(ExpressionCatalog.Resolve(string.Empty), Is.EqualTo(ExpressionCatalog.Normal)); // 빈 값
            Assert.That(ExpressionCatalog.Resolve("없는표정"), Is.EqualTo(ExpressionCatalog.Normal)); // 모르는 이름
            Assert.That(ExpressionCatalog.Resolve("smile"), Is.EqualTo(ExpressionCatalog.Smile)); // 그림 키 그대로
            Assert.That(ExpressionCatalog.Resolve(" Smile "), Is.EqualTo(ExpressionCatalog.Smile)); // 공백·대문자 정리
            Assert.That(ExpressionCatalog.IsKnown(string.Empty), Is.True); // 빈 값은 '표정 유지'
            Assert.That(ExpressionCatalog.IsKnown("미소"), Is.True); // 한글 이름
            Assert.That(ExpressionCatalog.IsKnown("serious"), Is.True); // 그림 키
            Assert.That(ExpressionCatalog.IsKnown("없는표정"), Is.False); // 모르는 이름
        }

        [Test] // 그림 표정 키는 7종이고, 연결표의 모든 이름이 그 안으로 이어진다
        public void Catalog_HasSevenKeys_AndEveryNameResolvesIntoThem() // 연결표 구성 테스트
        {
            Assert.That(ExpressionCatalog.Keys.Count, Is.EqualTo(7)); // 7종
            Assert.That(ExpressionCatalog.Keys.Distinct().Count(), Is.EqualTo(7)); // 중복 없음

            foreach (string name in ExpressionCatalog.Names) // 이름 순회
            {
                Assert.That(ExpressionCatalog.Keys, Does.Contain(ExpressionCatalog.Resolve(name)), $"연결 대상이 키가 아님 : {name}"); // 키로 연결
            }

            foreach (string key in ExpressionCatalog.Keys) // 키 순회
            {
                Assert.That(ExpressionCatalog.GetLabel(key), Is.Not.Empty, $"한글 이름 없음 : {key}"); // 표시 이름
            }
        }

        [Test] // 대사 파일에 적힌 표정 이름을 연결표가 전부 안다 (오타가 나면 여기서 실패)
        public void EveryDialogueExpression_IsKnown() // 대사 표정 전수 테스트
        {
            List<string> unknown = new List<string>(); // 모르는 표정
            int characterLines = 0; // 표정이 적힌 줄 수

            foreach (string guid in AssetDatabase.FindAssets("t:TextAsset", new[] { DialogueRoot })) // 대사 순회
            {
                TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(guid)); // 원본
                DialogueScript script = asset == null ? null : DialogueLibrary.Parse(asset.text); // 해석
                if (script == null) continue; // 대사 아님

                foreach (DialogueNode node in script.Nodes) // 줄 순회
                {
                    if (string.IsNullOrEmpty(node.Expression)) continue; // 표정 없음
                    characterLines++; // 집계
                    if (!ExpressionCatalog.IsKnown(node.Expression)) unknown.Add($"{script.Id} : {node.Expression}"); // 모르는 표정 수집
                }
            }

            Assert.That(characterLines, Is.GreaterThanOrEqualTo(800), "표정이 적힌 대사를 찾지 못했습니다"); // 수집 확인
            Assert.That(unknown, Is.Empty, "연결표에 없는 표정 : " + string.Join(", ", unknown.Distinct())); // 전부 등록됨
        }

        [Test] // 스탠딩은 표정 키 → 대사에 적힌 이름 → 기본 표정 → 표정 없는 그림 순서로 찾는다
        public void StandingPaths_SearchKeyThenRawNameThenNormalThenBase() // 스탠딩 찾는 순서 테스트
        {
            string folder = DialogueArtFactory.StandingFolder; // 스탠딩 폴더
            Assert.That(DialogueArtFactory.GetStandingResourcePaths("CH_SERENA", "미소"), Is.EqualTo(new[] { folder + "CH_SERENA_smile", folder + "CH_SERENA_미소", folder + "CH_SERENA_normal", folder + "CH_SERENA" })); // 한글 이름
            Assert.That(DialogueArtFactory.GetStandingResourcePaths("CH_SERENA", "smile"), Is.EqualTo(new[] { folder + "CH_SERENA_smile", folder + "CH_SERENA_normal", folder + "CH_SERENA" })); // 그림 키 (중복 없음)
            Assert.That(DialogueArtFactory.GetStandingResourcePaths("CH_SERENA", string.Empty), Is.EqualTo(new[] { folder + "CH_SERENA_normal", folder + "CH_SERENA" })); // 표정 없음
        }

        [Test] // 표정 그림이 아직 없어도 12인 모두 기본 스탠딩으로 보인다 (실루엣으로 떨어지지 않는다)
        public void Standing_FallsBackToBaseArt_ForEveryCharacter() // 표정 대체 테스트
        {
            foreach (string characterId in DiaryCatalog.AllCharacters) // 12인 순회
            {
                Sprite sprite = DialogueArtFactory.GetStanding(characterId, "미소", out bool placeholder); // 표정 지정 조회
                Assert.That(sprite, Is.Not.Null, $"스탠딩 없음 : {characterId}"); // 그림 있음
                Assert.That(placeholder, Is.False, $"임시 실루엣으로 떨어짐 : {characterId}"); // 정식 그림 사용
            }
        }

        [Test] // 12인 모두 표정 7종 그림이 있고, 한 캐릭터의 그림은 크기가 모두 같다 (겹쳐서 바꾸므로 크기가 다르면 어긋난다)
        public void EveryCharacter_HasSevenExpressionStandings_OfSameSize() // 표정 그림 전수 테스트
        {
            foreach (string characterId in DiaryCatalog.AllCharacters) // 12인 순회
            {
                int width = 0; // 기준 가로
                int height = 0; // 기준 세로

                foreach (string key in ExpressionCatalog.Keys) // 표정 7종 순회
                {
                    string path = $"{ResourcesRoot}{DialogueArtFactory.StandingFolder}{characterId}_{key}.png"; // 표정 그림 경로
                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path); // 그림
                    Assert.That(texture, Is.Not.Null, $"표정 그림 없음 : {characterId}_{key}"); // 존재

                    if (width == 0) // 첫 그림을 기준으로
                    {
                        width = texture.width; // 기준 가로 저장
                        height = texture.height; // 기준 세로 저장
                    }

                    Assert.That(texture.width, Is.EqualTo(width), $"가로 크기가 다름 : {characterId}_{key}"); // 가로 일치
                    Assert.That(texture.height, Is.EqualTo(height), $"세로 크기가 다름 : {characterId}_{key}"); // 세로 일치
                }

                Assert.That(height, Is.GreaterThan(width), $"세로형 전신 그림이 아님 : {characterId}"); // 세로형
            }
        }

        [Test] // 상점 · 대장간 · 마을 지도 · 여관 객실에 전용 배경 그림이 있다
        public void ScreenBackgrounds_HaveOwnArt() // 화면 배경 테스트
        {
            foreach (string key in new[] { "SHOP", "BLACKSMITH", "VILLAGE", "INN_NIGHT" }) // 화면 배경 키
            {
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ResourcesRoot}Dialogues/Backgrounds/{key}.png"); // 그림
                Assert.That(texture, Is.Not.Null, $"배경 그림 없음 : {key}"); // 존재
                Assert.That(DialogueArtFactory.HasBackgroundArt(key), Is.True, $"배경을 불러오지 못함 : {key}"); // 불러오기
            }
        }

        [Test] // 전용 그림이 없는 화면 배경은 정해 둔 다른 배경으로 대신한다
        public void BackgroundAlias_CoversScreensWithoutOwnArt() // 배경 대체 테스트
        {
            Assert.That(DialogueArtFactory.GetBackgroundAlias("SHOP"), Is.EqualTo("VILLAGE_MARKET")); // 상점 → 시장
            Assert.That(DialogueArtFactory.GetBackgroundAlias("VILLAGE"), Is.EqualTo("VILLAGE_PLAZA")); // 마을 지도 → 광장
            Assert.That(DialogueArtFactory.GetBackgroundAlias("DAY"), Is.Empty); // 대체 없음
            Assert.That(DialogueArtFactory.GetBackgroundAlias(null), Is.Empty); // null 안전
            Assert.That(DialogueArtFactory.HasBackgroundArt("SHOP"), Is.True); // 상점에 그림이 나옴
            Assert.That(DialogueArtFactory.HasBackgroundArt("VILLAGE"), Is.True); // 마을 지도에 그림이 나옴
            Assert.That(DialogueArtFactory.HasBackgroundArt("NO_SUCH_BACKGROUND"), Is.False); // 없는 키
            Assert.That(DialogueArtFactory.HasBackgroundArt(string.Empty), Is.False); // 빈 키
            Assert.That(DialogueArtFactory.GetBackground("NO_SUCH_BACKGROUND"), Is.Not.Null); // 없는 키도 임시 그림으로 동작
        }

        [Test] // 배경은 그림 비율을 지킨 채 부모를 덮고, 그림이 바뀌면 비율만 다시 맞춘다
        public void BackgroundFit_EnvelopesParentWithSpriteAspect() // 배경 비율 맞춤 테스트
        {
            Texture2D wideTexture = new Texture2D(160, 90); // 가로 그림
            Texture2D tallTexture = new Texture2D(90, 160); // 세로 그림
            Sprite wide = Sprite.Create(wideTexture, new Rect(0f, 0f, 160f, 90f), new Vector2(0.5f, 0.5f)); // 가로 스프라이트
            Sprite tall = Sprite.Create(tallTexture, new Rect(0f, 0f, 90f, 160f), new Vector2(0.5f, 0.5f)); // 세로 스프라이트
            GameObject parentObject = new GameObject("Screen", typeof(RectTransform)); // 화면 역할 부모
            ((RectTransform)parentObject.transform).sizeDelta = new Vector2(1600f, 900f); // 기준 해상도 크기
            GameObject imageObject = new GameObject("Background", typeof(RectTransform), typeof(Image)); // 배경 객체
            imageObject.transform.SetParent(parentObject.transform, false); // 부모 연결
            Image image = imageObject.GetComponent<Image>(); // 이미지

            try // 정리 보장
            {
                image.sprite = wide; // 가로 그림 적용
                BackgroundFit.Apply(image); // 맞춤
                AspectRatioFitter fitter = imageObject.GetComponent<AspectRatioFitter>(); // 맞춤 컴포넌트
                Assert.That(fitter, Is.Not.Null); // 추가됨
                Assert.That(fitter.aspectMode, Is.EqualTo(AspectRatioFitter.AspectMode.EnvelopeParent)); // 덮기 방식
                Assert.That(fitter.aspectRatio, Is.EqualTo(160f / 90f).Within(0.0001f)); // 가로 비율

                image.sprite = tall; // 그림 교체
                BackgroundFit.Apply(image); // 다시 맞춤
                Assert.That(imageObject.GetComponents<AspectRatioFitter>().Length, Is.EqualTo(1)); // 컴포넌트는 하나
                Assert.That(fitter.aspectRatio, Is.EqualTo(90f / 160f).Within(0.0001f)); // 세로 비율

                Assert.That(BackgroundFit.GetAspect(null), Is.EqualTo(BackgroundFit.DefaultAspect).Within(0.0001f)); // 그림 없으면 16:9
                Assert.That(() => BackgroundFit.Apply(null), Throws.Nothing); // null 안전
            }
            finally // 임시 객체 정리
            {
                Object.DestroyImmediate(parentObject); // 부모와 배경 객체 제거
                Object.DestroyImmediate(wide); // 스프라이트 제거
                Object.DestroyImmediate(tall); // 스프라이트 제거
                Object.DestroyImmediate(wideTexture); // 텍스처 제거
                Object.DestroyImmediate(tallTexture); // 텍스처 제거
            }
        }
    }
}
