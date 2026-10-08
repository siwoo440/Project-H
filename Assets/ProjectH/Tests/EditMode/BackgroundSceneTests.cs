using System.Collections.Generic; // 목록 자료형
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Dialogue; // 대사 파일 기능
using ProjectH.UI; // 배경 조회 기능
using UnityEditor; // 에셋 조회 기능
using UnityEngine; // 텍스트 에셋·텍스처 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BackgroundSceneTests // Day78 장면과 배경 연결 테스트 (그림이 일러스트가 되면서, 다른 장소의 배경 키를 빌려 쓰면 엉뚱한 그림이 나온다)
    {
        private const string DialogueRoot = "Assets/ProjectH/Resources/Dialogues"; // 대사 폴더
        private const string BackgroundRoot = "Assets/ProjectH/Resources/Dialogues/Backgrounds/"; // 배경 폴더

        private static DialogueScript Load(string scriptId) // 대사 파일 읽기
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>($"{DialogueRoot}/{scriptId}.json"); // 원본
            return asset == null ? null : DialogueLibrary.Parse(asset.text); // 해석
        }

        private static List<DialogueScript> LoadAllUsing(string backgroundKey) // 이 배경 키를 쓰는 대사 전부
        {
            List<DialogueScript> result = new List<DialogueScript>(); // 결과

            foreach (string guid in AssetDatabase.FindAssets("t:TextAsset", new[] { DialogueRoot })) // 대사 순회
            {
                TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(guid)); // 원본
                DialogueScript script = asset == null ? null : DialogueLibrary.Parse(asset.text); // 해석
                if (script != null && script.Background == backgroundKey) result.Add(script); // 수집
            }

            return result; // 반환
        }

        [TestCase("CH5_05")] // 지하 고대 도시 · 봉인실
        [TestCase("CH5_06")] // 지하 고대 도시 · 심연의 입구
        [TestCase("CH6_03")] // 마왕성 최심부 · 무명의 옥좌
        [TestCase("CH6_04")] // 마왕성 최심부 · 관이 모이는 방
        [TestCase("CH6_05")] // 마왕성 최심부 · 무명의 옥좌
        [TestCase("CH6_06")] // 마왕성 · 지상으로 오르는 계단
        [TestCase("ENDING_02")] // 엔딩 · 마지막 침묵
        [TestCase("ENDING_03")] // 엔딩 · 잠든 이웃
        [TestCase("ENDING_04")] // 엔딩 · 다시 지워진 이름
        public void AbyssScenes_UseAbyssBackground(string scriptId) // 심연 · 옥좌 장면 배경 테스트
        {
            DialogueScript script = Load(scriptId); // 대사
            Assert.That(script, Is.Not.Null, $"대사 없음 : {scriptId}"); // 존재
            Assert.That(script.Background, Is.EqualTo("ABYSS"), $"심연 장면이 다른 배경을 씀 : {scriptId}"); // 심연 배경
        }

        [Test] // 심연 배경에는 전용 그림이 있다
        public void AbyssBackground_HasOwnArt() // 심연 그림 테스트
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundRoot + "ABYSS.png"), Is.Not.Null); // 그림 파일
            Assert.That(DialogueArtFactory.GetBackgroundAlias("BATTLE_DEMON_CASTLE"), Is.EqualTo("ABYSS")); // 마왕성 전투도 같은 그림
        }

        [Test] // 바다 균열 배경은 해안 장면에만 쓴다 (예전에는 옥좌 장면이 이 키를 빌려 써서 바다 그림이 나왔다)
        public void SeaRift_IsUsedOnlyAtTheCoast() // 바다 배경 테스트
        {
            List<DialogueScript> scripts = LoadAllUsing("SEA_RIFT"); // 바다 배경을 쓰는 대사
            Assert.That(scripts.Count, Is.GreaterThanOrEqualTo(1)); // 해안 장면은 남아 있음

            foreach (DialogueScript script in scripts) // 대사 순회
            {
                Assert.That(script.Location, Does.Contain("해안"), $"해안이 아닌 장면이 바다 배경을 씀 : {script.Id}"); // 해안 장면
            }
        }

        [Test] // 노아르 도시 배경은 도시 장면에만 쓰고, 서고와 연구실은 제 배경을 쓴다
        public void Noir_IsSplitIntoCityArchiveAndLab() // 노아르 배경 테스트
        {
            foreach (DialogueScript script in LoadAllUsing("NOIR")) // 도시 배경을 쓰는 대사
            {
                Assert.That(script.Location, Does.Contain("노아르"), $"노아르가 아닌 장면이 도시 배경을 씀 : {script.Id}"); // 노아르 장면
                Assert.That(script.Location, Does.Not.Contain("서고").And.Not.Contain("연구실"), $"실내 장면이 도시 배경을 씀 : {script.Id}"); // 실내 제외
            }

            Assert.That(Load("CH3_02").Background, Is.EqualTo("NOIR_ARCHIVE")); // 금지된 지하 서고
            Assert.That(Load("CH3_05").Background, Is.EqualTo("ALCHEMY_LAB")); // 릴리아의 옛 연구실
        }

        [Test] // 그림이 아직 없는 노아르 배경도 대체 배경을 따라가 그림이 나온다 (대체의 대체까지)
        public void NoirBackgrounds_ResolveThroughAliasChain() // 대체 배경 사슬 테스트
        {
            Assert.That(DialogueArtFactory.HasBackgroundArt("NOIR"), Is.True); // 도시
            Assert.That(DialogueArtFactory.HasBackgroundArt("NOIR_ARCHIVE"), Is.True); // 지하 서고
            Assert.That(DialogueArtFactory.GetBackgroundAlias("BATTLE_NOIR"), Is.EqualTo("NOIR_ARCHIVE")); // 노아르 전투 → 지하 서고
            Assert.That(DialogueArtFactory.HasBackgroundArt("BATTLE_NOIR"), Is.True); // 서고 그림이 없어도 그 대체 배경으로 이어짐
            Assert.That(DialogueArtFactory.HasBackgroundArt("NO_SUCH_BACKGROUND"), Is.False); // 없는 키는 끝까지 가도 없음
        }
    }
}
