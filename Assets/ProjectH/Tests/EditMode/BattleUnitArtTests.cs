using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 유닛 그림·전투 배경 기능
using ProjectH.Diary; // 12인 목록 기능
using ProjectH.Dungeon; // 모험 지역 표 기능
using ProjectH.UI; // 배경 조회·던전 목록 기능
using UnityEngine; // 스프라이트·벡터 기능
using UnityEngine.UI; // Image·Text 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleUnitArtTests // Day78 전투 유닛 그림 자리 · 전투 배경 테스트
    {
        [Test] // 아군은 SD 그림이 아직 없어도 스탠딩으로 보인다 (색 상자로 떨어지지 않는다)
        public void Ally_AlwaysHasArt() // 아군 그림 테스트
        {
            foreach (string characterId in DiaryCatalog.AllCharacters) // 12인 순회
            {
                Assert.That(BattleUnitArt.GetAlly(characterId), Is.Not.Null, $"전투 그림 없음 : {characterId}"); // SD 또는 스탠딩
            }

            Assert.That(BattleUnitArt.GetAlly(string.Empty), Is.Null); // 빈 ID
            Assert.That(BattleUnitArt.GetAlly("CH_NOT_EXIST"), Is.Null); // 없는 캐릭터는 색 상자
        }

        [Test] // 몬스터는 SD 그림이 없으면 null을 돌려준다 (색 상자 유지)
        public void Enemy_WithoutArt_ReturnsNull() // 몬스터 그림 테스트
        {
            Assert.That(BattleUnitArt.GetEnemy("MON_NOT_EXIST"), Is.Null); // 없는 몬스터
            Assert.That(BattleUnitArt.GetEnemy(null), Is.Null); // null 안전
            Assert.That(BattleUnitArt.GetSd(string.Empty), Is.Null); // 빈 ID
        }

        [Test] // 보스는 ID 접두어로 가려낸다
        public void IsBoss_UsesIdPrefix() // 보스 판정 테스트
        {
            Assert.That(BattleUnitArt.IsBoss("MON_BOSS_ARCHAI"), Is.True); // 보스
            Assert.That(BattleUnitArt.IsBoss("MON_NOIR_SLIME"), Is.False); // 일반 몬스터
            Assert.That(BattleUnitArt.IsBoss(null), Is.False); // null 안전
        }

        [Test] // 그림 영역은 발 높이와 가로 중심을 지킨 채 배율만큼 커진다
        public void SpriteAnchors_ScaleFromFeet() // 그림 영역 테스트
        {
            BattleUnitArt.GetSpriteAnchors(1f, out Vector2 min, out Vector2 max); // 일반 크기
            Assert.That(min.x, Is.EqualTo(BattleUnitArt.SpriteAnchorMin.x).Within(0.0001f)); // 기본 왼쪽
            Assert.That(min.y, Is.EqualTo(BattleUnitArt.SpriteAnchorMin.y).Within(0.0001f)); // 기본 아래
            Assert.That(max.x, Is.EqualTo(BattleUnitArt.SpriteAnchorMax.x).Within(0.0001f)); // 기본 오른쪽
            Assert.That(max.y, Is.EqualTo(BattleUnitArt.SpriteAnchorMax.y).Within(0.0001f)); // 기본 위

            BattleUnitArt.GetSpriteAnchors(BattleUnitArt.BossScale, out Vector2 bossMin, out Vector2 bossMax); // 보스 크기
            Assert.That(bossMin.y, Is.EqualTo(min.y).Within(0.0001f)); // 발 높이 그대로
            Assert.That((bossMin.x + bossMax.x) * 0.5f, Is.EqualTo((min.x + max.x) * 0.5f).Within(0.0001f)); // 가로 중심 그대로
            Assert.That(bossMax.y - bossMin.y, Is.EqualTo((max.y - min.y) * BattleUnitArt.BossScale).Within(0.0001f)); // 높이 × 배율
            Assert.That(bossMax.x - bossMin.x, Is.EqualTo((max.x - min.x) * BattleUnitArt.BossScale).Within(0.0001f)); // 너비 × 배율
        }

        [Test] // 그림이 있으면 바디가 그림 배치로 바뀌고, 없으면 색 상자가 그대로 남는다
        public void ApplyTo_SwitchesBodyToSpriteLayout() // 바디 적용 테스트
        {
            Texture2D texture = new Texture2D(64, 64); // 임시 그림
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f)); // 임시 스프라이트
            GameObject bodyObject = new GameObject("Body", typeof(RectTransform), typeof(Image)); // 바디 객체
            Image body = bodyObject.GetComponent<Image>(); // 바디 이미지

            try // 정리 보장
            {
                body.color = new Color(0.5f, 0.6f, 0.7f, 0.95f); // 역할 색 상자
                Vector2 boxAnchorMin = body.rectTransform.anchorMin; // 상자 배치
                Assert.That(BattleUnitArt.ApplyTo(body, null), Is.False); // 그림 없음
                Assert.That(body.sprite, Is.Null); // 상자 그대로
                Assert.That(body.rectTransform.anchorMin, Is.EqualTo(boxAnchorMin)); // 배치 그대로
                Assert.That(body.color.a, Is.EqualTo(0.95f).Within(0.0001f)); // 색 그대로

                Assert.That(BattleUnitArt.ApplyTo(body, sprite), Is.True); // 그림 적용
                Assert.That(body.sprite, Is.SameAs(sprite)); // 그림
                Assert.That(body.color, Is.EqualTo(Color.white)); // 원래 색
                Assert.That(body.preserveAspect, Is.True); // 비율 유지
                Assert.That(body.rectTransform.anchorMin.x, Is.EqualTo(BattleUnitArt.SpriteAnchorMin.x).Within(0.0001f)); // 그림 영역 왼쪽
                Assert.That(body.rectTransform.anchorMax.y, Is.EqualTo(BattleUnitArt.SpriteAnchorMax.y).Within(0.0001f)); // 그림 영역 위
                Assert.That(body.rectTransform.pivot, Is.EqualTo(BattleUnitArt.SpritePivot)); // 발 쪽 기준
                Assert.That(() => BattleUnitArt.ApplyTo(null, sprite), Throws.Nothing); // null 안전
            }
            finally // 임시 객체 정리
            {
                Object.DestroyImmediate(bodyObject); // 바디 제거
                Object.DestroyImmediate(sprite); // 스프라이트 제거
                Object.DestroyImmediate(texture); // 텍스처 제거
            }
        }

        [Test] // 그림 유닛의 이름은 머리 위 한 줄로 옮겨지고, 외곽선은 한 번만 붙는다
        public void PlaceNameAbove_MovesLabelOverTheHead() // 이름 배치 테스트
        {
            GameObject bodyObject = new GameObject("Body", typeof(RectTransform), typeof(Image)); // 바디 객체
            GameObject labelObject = new GameObject("CharacterText", typeof(RectTransform), typeof(Text)); // 이름 객체
            labelObject.transform.SetParent(bodyObject.transform, false); // 바디 하위

            try // 정리 보장
            {
                Text label = labelObject.GetComponent<Text>(); // 이름 글자
                Image body = bodyObject.GetComponent<Image>(); // 바디 이미지
                BattleUnitArt.PlaceNameAbove(label, body); // 머리 위로
                BattleUnitArt.PlaceNameAbove(label, body); // 두 번 불러도
                Assert.That(label.rectTransform.anchorMin, Is.EqualTo(new Vector2(0f, 1f))); // 그림 위쪽에 붙음
                Assert.That(label.rectTransform.anchorMax, Is.EqualTo(new Vector2(1f, 1f))); // 가로 전체
                Assert.That(label.rectTransform.pivot.y, Is.EqualTo(0f)); // 아래 기준 → 그림 위로 올라감
                Assert.That(labelObject.GetComponents<Outline>().Length, Is.EqualTo(1)); // 외곽선은 하나
                Assert.That(() => BattleUnitArt.PlaceNameAbove(null, body), Throws.Nothing); // null 안전
            }
            finally // 임시 객체 정리
            {
                Object.DestroyImmediate(bodyObject); // 바디와 이름 제거
            }
        }

        [Test] // 17던전 모두 지역이 있고, 그 지역의 전투 배경에 그림이 나온다
        public void EveryDungeon_ResolvesToBattleBackgroundWithArt() // 전투 배경 전수 테스트
        {
            Assert.That(DungeonSelectionRuntimeState.SupportedDungeonIds.Count, Is.EqualTo(17)); // 17던전

            foreach (string dungeonId in DungeonSelectionRuntimeState.SupportedDungeonIds) // 던전 순회
            {
                Assert.That(AdventureRegionCatalog.FindByDungeon(dungeonId), Is.Not.Null, $"지역이 없는 던전 : {dungeonId}"); // 지역 소속
                string key = BattleBackgroundCatalog.GetKeyForDungeon(dungeonId); // 전투 배경 키
                Assert.That(key, Does.StartWith("BATTLE_"), $"전투 배경 키가 아님 : {dungeonId}"); // 전투 배경 키
                Assert.That(DialogueArtFactory.HasBackgroundArt(key), Is.True, $"전투 배경 그림 없음 : {dungeonId} → {key}"); // 그림 (전용 또는 대체)
            }

            Assert.That(BattleBackgroundCatalog.GetKeyForDungeon("DG_NOT_EXIST"), Is.EqualTo(BattleBackgroundCatalog.DefaultKey)); // 모르는 던전은 기본
            Assert.That(BattleBackgroundCatalog.GetKeyForRegion(null), Is.EqualTo(BattleBackgroundCatalog.DefaultKey)); // null 안전
            Assert.That(BattleBackgroundCatalog.GetKeyForRegion("REGION_KARNIAN"), Is.EqualTo("BATTLE_KARNIAN")); // 지역별 키
        }

        [Test] // 배경을 적용하면 그림과 비율 맞춤이 들어가고, 어두운 막은 배경 바로 위에 한 장만 생긴다
        public void BackgroundApply_SetsSpriteAndSingleDim() // 전투 배경 적용 테스트
        {
            GameObject canvasObject = new GameObject("BackgroundCanvas", typeof(RectTransform)); // 배경 캔버스 역할
            GameObject backgroundObject = new GameObject("BattleBackground", typeof(RectTransform), typeof(Image)); // 배경 객체
            backgroundObject.transform.SetParent(canvasObject.transform, false); // 캔버스 하위

            try // 정리 보장
            {
                Image background = backgroundObject.GetComponent<Image>(); // 배경 이미지
                Assert.That(BattleBackgroundRuntimePatch.Apply(background, "BATTLE_NOT_EXIST"), Is.False); // 그림 없는 키
                Assert.That(canvasObject.transform.childCount, Is.EqualTo(1)); // 막이 생기지 않음

                Assert.That(BattleBackgroundRuntimePatch.Apply(background, "BATTLE_KARNIAN"), Is.True); // 적용
                Assert.That(BattleBackgroundRuntimePatch.Apply(background, "BATTLE_DESERT"), Is.True); // 다시 적용
                Assert.That(background.sprite, Is.Not.Null); // 그림
                Assert.That(backgroundObject.GetComponent<AspectRatioFitter>(), Is.Not.Null); // 비율 맞춤
                Assert.That(canvasObject.transform.childCount, Is.EqualTo(2)); // 배경 + 막 한 장
                Assert.That(canvasObject.transform.GetChild(1).GetComponent<Image>().color.a, Is.EqualTo(BattleBackgroundCatalog.DimAlpha).Within(0.0001f)); // 배경 바로 위의 막
                Assert.That(() => BattleBackgroundRuntimePatch.Apply(null, "BATTLE_KARNIAN"), Throws.Nothing); // null 안전
            }
            finally // 임시 객체 정리
            {
                Object.DestroyImmediate(canvasObject); // 캔버스와 하위 제거
            }
        }
    }
}
