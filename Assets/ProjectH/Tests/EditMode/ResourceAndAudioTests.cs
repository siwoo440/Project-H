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

        [Test] // 대사가 쓰는 배경 키마다 그림이 나온다 (전용 그림 또는 정해 둔 대체 배경 — 코드로 그린 임시 배경으로 떨어지지 않는다)
        public void EveryDialogueBackground_HasImage() // 배경 그림 테스트
        {
            List<string> keys = CollectBackgroundKeys(); // 배경 키
            Assert.That(keys.Count, Is.GreaterThanOrEqualTo(20), "대사에서 배경 키를 찾지 못했습니다"); // 수집 확인

            foreach (string key in keys) // 키 순회
            {
                Assert.That(DialogueArtFactory.HasBackgroundArt(key), Is.True, $"배경 그림 없음 : {key}"); // 그림 확인 (Day78 — 대체 배경도 인정)
            }
        }

        [Test] // Resources의 모든 그림은 가로 · 세로가 4의 배수다 (아니면 압축되지 않아 메모리와 빌드 용량을 6배쯤 쓴다)
        public void EveryResourceTexture_HasBlockCompressibleSize() // 그림 크기 테스트 (Day80 — 배경 1672×941이 압축되지 않고 올라가던 문제의 재발 방지)
        {
            List<string> bad = new List<string>(); // 크기가 맞지 않는 그림

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/ProjectH/Resources" })) // 그림 순회
            {
                string path = AssetDatabase.GUIDToAssetPath(guid); // 경로
                UnityEngine.Texture2D texture = AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(path); // 그림
                if (texture == null) continue; // 그림 아님
                if (texture.width % 4 != 0 || texture.height % 4 != 0) bad.Add($"{path} ({texture.width}×{texture.height})"); // 4의 배수가 아님
            }

            Assert.That(bad, Is.Empty, "가로 · 세로를 4의 배수로 맞춰 주세요 (예: 941 → 940) : " + string.Join(", ", bad)); // 전부 4의 배수
        }

        [Test] // 12인 전원에게 정사각 초상화가 있다
        public void EveryCharacter_HasSquarePortrait() // 초상화 테스트
        {
            Assert.That(DiaryCatalog.AllCharacters.Count, Is.EqualTo(12)); // 12인

            foreach (string characterId in DiaryCatalog.AllCharacters) // 12인 순회
            {
                UnityEngine.Sprite portrait = CharacterPortraitArt.Get(characterId, out bool placeholder); // 초상화 (Day77 — 전용 그림 또는 스탠딩에서 자른 얼굴)
                Assert.That(portrait, Is.Not.Null, $"초상화 없음 : {characterId}"); // 존재
                Assert.That(placeholder, Is.False, $"임시 원으로 떨어짐 : {characterId}"); // 정식 그림 사용
                Assert.That(portrait.rect.width, Is.EqualTo(portrait.rect.height), $"초상화가 정사각이 아님 : {characterId}"); // 정사각 확인
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
            string[] bgm = AudioCatalog.AllBgm; // 배경음 전체 (Day88 — 카탈로그의 목록을 그대로 쓴다. 소리를 추가하고 파일을 빠뜨리면 여기서 걸린다)
            string[] sfx = AudioCatalog.AllSfx; // 효과음 전체

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
