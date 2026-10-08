using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.SaveSystem; // 저장 · 시간대 기능
using ProjectH.UI; // 로비 배경 · 아이템 아이콘 기능
using UnityEngine; // 좌표 기능
using UnityEngine.UI; // Image 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class LobbyBackdropTests // Day80 로비 시간대 배경 · 공용 배경 적용 · 아이템 아이콘 자리 테스트
    {
        [TestCase(SaveTimeOfDay.Morning, "MORNING")] // 아침
        [TestCase(SaveTimeOfDay.Day, "DAY")] // 점심
        [TestCase(SaveTimeOfDay.Evening, "EVENING")] // 저녁
        [TestCase(SaveTimeOfDay.Night, "NIGHT")] // 밤
        public void Lobby_UsesTimeOfDayBackgroundWithArt(SaveTimeOfDay phase, string expectedKey) // 시간대 배경 테스트
        {
            Assert.That(LobbyBackdropCatalog.GetKey(phase), Is.EqualTo(expectedKey)); // 시간대별 배경 키
            Assert.That(DialogueArtFactory.HasBackgroundArt(expectedKey), Is.True, $"로비 배경 그림 없음 : {expectedKey}"); // 그림 있음
        }

        [Test] // 로비에 세울 동료 : 파티 첫 자리가 우선이고, 저장이 없으면 빈 값이다
        public void Lobby_LeadCharacterIsFirstPartyMember() // 대표 동료 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 새 게임 (세레나 1인)
            Assert.That(LobbyBackdropCatalog.GetLeadCharacterId(saveData), Is.EqualTo("CH_SERENA")); // 파티 첫 자리
            Assert.That(LobbyBackdropCatalog.GetLeadCharacterId(null), Is.Empty); // 저장 없음
        }

        [Test] // 공용 배경 적용 : 그림이 있으면 배경 · 비율 맞춤 · 막 한 장, 막 투명도 0이면 막을 만들지 않는다
        public void SceneBackdrop_AppliesArtAndSingleDim() // 공용 배경 테스트
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform)); // 캔버스 역할
            GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image)); // 배경
            backgroundObject.transform.SetParent(canvasObject.transform, false); // 캔버스 하위

            try // 정리 보장
            {
                Image background = backgroundObject.GetComponent<Image>(); // 배경 이미지
                Assert.That(SceneBackdrop.Apply(background, "NO_SUCH_BACKGROUND", 0.2f, "Dim"), Is.False); // 그림 없는 키
                Assert.That(background.sprite, Is.Null); // 원래 배경 그대로

                Assert.That(SceneBackdrop.Apply(background, "DAY", 0f, "Dim"), Is.True); // 막 없이 적용
                Assert.That(canvasObject.transform.childCount, Is.EqualTo(1)); // 막이 생기지 않음

                Assert.That(SceneBackdrop.Apply(background, "NIGHT", 0.2f, "Dim"), Is.True); // 막과 함께 적용
                Assert.That(SceneBackdrop.Apply(background, "MORNING", 0.2f, "Dim"), Is.True); // 시간대가 바뀌어 다시 적용
                Assert.That(background.sprite, Is.Not.Null); // 그림
                Assert.That(backgroundObject.GetComponent<AspectRatioFitter>(), Is.Not.Null); // 비율 맞춤
                Assert.That(canvasObject.transform.childCount, Is.EqualTo(2)); // 배경 + 막 한 장
                Assert.That(canvasObject.transform.GetChild(1).name, Is.EqualTo("Dim")); // 배경 바로 위
                Assert.That(() => SceneBackdrop.Apply(null, "DAY", 0.2f, "Dim"), Throws.Nothing); // null 안전
            }
            finally // 임시 객체 정리
            {
                Object.DestroyImmediate(canvasObject); // 캔버스와 하위 제거
            }
        }

        [Test] // 그림 불러오기 : 같은 경로는 같은 그림을 돌려주고, 캐시를 비워도 다시 불러온다
        public void SpriteLoader_CachesAndReloadsAfterClear() // 그림 캐시 테스트
        {
            const string path = "Dialogues/Backgrounds/DAY"; // 있는 그림
            RuntimeSpriteLoader.ClearCache(); // 깨끗한 상태에서 시작
            Sprite first = RuntimeSpriteLoader.Load(path); // 처음 불러오기
            Assert.That(first, Is.Not.Null); // 그림 있음
            Assert.That(RuntimeSpriteLoader.Load(path), Is.SameAs(first)); // 두 번째는 캐시
            Assert.That(RuntimeSpriteLoader.CachedCount, Is.GreaterThanOrEqualTo(1)); // 캐시에 들어 있음
            Assert.That(RuntimeSpriteLoader.Load("Dialogues/Backgrounds/NO_SUCH_FILE"), Is.Null); // 없는 그림
            Assert.That(RuntimeSpriteLoader.Load(null), Is.Null); // null 안전

            RuntimeSpriteLoader.ClearCache(); // 씬 전환 때처럼 비움
            Assert.That(RuntimeSpriteLoader.CachedCount, Is.EqualTo(0)); // 비워짐
            Assert.That(RuntimeSpriteLoader.Load(path), Is.Not.Null); // 다시 불러와짐
        }

        [Test] // 아이템 아이콘 : 파일이 없는 아이템은 null을 돌려준다 (색 원과 글자로 그린다)
        public void ItemIcon_WithoutArt_ReturnsNull() // 아이콘 자리 테스트
        {
            Assert.That(ItemIconArt.Get("IT_NOT_EXIST"), Is.Null); // 없는 아이템
            Assert.That(ItemIconArt.Get((string)null), Is.Null); // null 안전
            Assert.That(ItemIconArt.Get((ProjectH.Data.ItemData)null), Is.Null); // 아이템 없음
            Assert.That(ItemIconArt.Folder, Is.EqualTo("Icons/Items/")); // 아이콘 폴더 (요청문과 같은 경로)
        }

        [Test] // 아이템 데이터가 있는 아이템은 모두 정사각 아이콘이 있다 (아이템을 추가하고 그림을 빠뜨리면 여기서 실패)
        public void EveryItem_HasSquareIcon() // 아이템 아이콘 테스트
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/ProjectH/Data/Items" }); // 아이템 데이터
            Assert.That(guids.Length, Is.GreaterThanOrEqualTo(40)); // 40종 이상

            foreach (string guid in guids) // 아이템 순회
            {
                ProjectH.Data.ItemData item = UnityEditor.AssetDatabase.LoadAssetAtPath<ProjectH.Data.ItemData>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid)); // 아이템 원본
                Assert.That(item, Is.Not.Null); // 데이터 존재
                Sprite icon = ItemIconArt.Get(item.Id); // 아이콘
                Assert.That(icon, Is.Not.Null, $"아이콘 없음 : {item.Id} ({item.DisplayName})"); // 존재
                Assert.That(icon.rect.width, Is.EqualTo(icon.rect.height), $"아이콘이 정사각이 아님 : {item.Id}"); // 정사각 (칸마다 크기가 같아 보이도록)
                Assert.That(ItemIconArt.Get(item), Is.SameAs(icon), $"아이콘 대신 다른 그림을 씀 : {item.Id}"); // 아이콘 파일 우선
            }
        }
    }
}
