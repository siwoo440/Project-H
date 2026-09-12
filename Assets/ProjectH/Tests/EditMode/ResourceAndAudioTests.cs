using System.Collections.Generic; // 목록 자료형
using System.Linq; // 목록 검색 기능
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Core; // 씬 이름·소리 경로 기능
using ProjectH.Diary; // 12인 목록 기능
using ProjectH.Dialogue; // 대사 파일 기능
using ProjectH.UI; // 초상화 경로 기능
using UnityEditor; // 에셋 조회 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class ResourceAndAudioTests // Day73 리소스 규약 · 소리 카탈로그 테스트
    {
        private const string ResourcesRoot = "Assets/ProjectH/Resources/"; // Resources 폴더

        private static bool Exists(string resourcePath, string extension) // Resources 안에 파일이 있는지
        {
            return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ResourcesRoot + resourcePath + extension) != null; // 존재 확인
        }

        private static List<string> CollectBackgroundKeys() // 대사가 실제로 쓰는 배경 키
        {
            HashSet<string> keys = new HashSet<string>(); // 중복 제거

            foreach (string guid in AssetDatabase.FindAssets("t:TextAsset", new[] { ResourcesRoot + "Dialogues" })) // 대사 순회
            {
                UnityEngine.TextAsset asset = AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>(AssetDatabase.GUIDToAssetPath(guid)); // 원본
                DialogueScript script = asset == null ? null : DialogueLibrary.Parse(asset.text); // 해석
                if (script != null && !string.IsNullOrEmpty(script.Background)) keys.Add(script.Background); // 수집
            }

            return keys.ToList(); // 목록 반환
        }

        [Test] // 대사가 쓰는 배경 키마다 그림 파일이 있다
        public void EveryDialogueBackground_HasImage() // 배경 그림 테스트
        {
            List<string> keys = CollectBackgroundKeys(); // 배경 키
            Assert.That(keys.Count, Is.GreaterThanOrEqualTo(20), "대사에서 배경 키를 찾지 못했습니다"); // 수집 확인

            foreach (string key in keys) // 키 순회
            {
                Assert.That(Exists($"Dialogues/Backgrounds/{key}", ".png"), Is.True, $"배경 그림 없음 : {key}"); // 그림 확인
            }
        }

        [Test] // 12인 전원에게 정사각 초상화가 있다
        public void EveryCharacter_HasSquarePortrait() // 초상화 테스트
        {
            Assert.That(DiaryCatalog.AllCharacters.Count, Is.EqualTo(12)); // 12인

            foreach (string characterId in DiaryCatalog.AllCharacters) // 12인 순회
            {
                string path = ResourcesRoot + CharacterPortraitArt.PortraitFolder + characterId + ".png"; // 초상화 경로
                UnityEngine.Texture2D texture = AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(path); // 그림
                Assert.That(texture, Is.Not.Null, $"초상화 없음 : {characterId}"); // 존재
                Assert.That(texture.width, Is.EqualTo(texture.height), $"초상화가 정사각이 아님 : {characterId}"); // 정사각 확인
            }
        }

        [Test] // 씬마다 알맞은 배경음이 정해져 있다
        public void AudioCatalog_MapsSceneToBgm() // 배경음 매핑 테스트
        {
            Assert.That(AudioCatalog.GetSceneBgm(GameScenes.Title), Is.EqualTo(AudioCatalog.BgmTitle)); // 타이틀
            Assert.That(AudioCatalog.GetSceneBgm(GameScenes.Battle), Is.EqualTo(AudioCatalog.BgmBattle)); // 전투
            Assert.That(AudioCatalog.GetSceneBgm(GameScenes.Lobby), Is.EqualTo(AudioCatalog.BgmVillage)); // 로비
            Assert.That(AudioCatalog.GetSceneBgm(GameScenes.Village), Is.EqualTo(AudioCatalog.BgmVillage)); // 마을
            Assert.That(AudioCatalog.GetSceneBgm(GameScenes.Bootstrap), Is.Empty); // 부트스트랩은 소리 없음
        }

        [Test] // 소리 파일이 경로 규약대로 들어와 있다
        public void AudioFiles_ExistForEveryKey() // 소리 파일 테스트
        {
            string[] bgm = { AudioCatalog.BgmTitle, AudioCatalog.BgmVillage, AudioCatalog.BgmBattle }; // 배경음
            string[] sfx =
            {
                AudioCatalog.SfxClick, AudioCatalog.SfxConfirm, AudioCatalog.SfxCancel, AudioCatalog.SfxPage,
                AudioCatalog.SfxGold, AudioCatalog.SfxItem, AudioCatalog.SfxHit, AudioCatalog.SfxPerfect, AudioCatalog.SfxLevelUp
            }; // 효과음

            Assert.That(bgm.Distinct().Count(), Is.EqualTo(bgm.Length)); // 곡 이름 중복 없음
            Assert.That(sfx.Distinct().Count(), Is.EqualTo(sfx.Length)); // 소리 이름 중복 없음

            foreach (string key in bgm) // 배경음 순회
            {
                Assert.That(Exists(AudioCatalog.BgmFolder + key, ".wav"), Is.True, $"배경음 없음 : {key}"); // 확인
            }

            foreach (string key in sfx) // 효과음 순회
            {
                Assert.That(Exists(AudioCatalog.SfxFolder + key, ".wav"), Is.True, $"효과음 없음 : {key}"); // 확인
            }
        }
    }
}
