using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Diary; // 12인 목록 기능
using ProjectH.Dialogue; // 대화 스탠딩 구도 기능
using ProjectH.UI; // 얼굴 위치표·초상화 기능
using UnityEngine; // 벡터·사각형·스프라이트 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class StandingFrameTests // Day77 얼굴 위치표 · 초상화 자르기 · 대화 스탠딩 구도 테스트
    {
        private static readonly Rect SourceRect = new Rect(0f, 0f, 1024f, 1536f); // 받은 스탠딩 그림 크기

        [Test] // 12인 모두 얼굴 위치가 있고, 값이 그림 위쪽 가운데 근처다
        public void FaceCatalog_CoversEveryCharacterWithSaneValues() // 얼굴 위치표 테스트
        {
            Assert.That(DiaryCatalog.AllCharacters.Count, Is.EqualTo(12)); // 12인

            foreach (string characterId in DiaryCatalog.AllCharacters) // 12인 순회
            {
                Assert.That(StandingFaceCatalog.TryGet(characterId, out StandingFace face), Is.True, $"얼굴 위치 없음 : {characterId}"); // 등록됨
                Assert.That(face.CenterX, Is.InRange(0.40f, 0.60f), $"얼굴 가로 위치가 이상함 : {characterId}"); // 가운데 근처
                Assert.That(face.CenterY, Is.InRange(0.08f, 0.20f), $"얼굴 세로 위치가 이상함 : {characterId}"); // 위쪽 (Day86 — 4.5등신 새 그림은 얼굴 중심이 조금 더 아래)
                Assert.That(face.Height, Is.InRange(0.06f, 0.14f), $"얼굴 크기가 이상함 : {characterId}"); // 8등신(약 0.08) ~ 4.5등신(약 0.115)
            }
        }

        [Test] // 위치표에 없는 그림은 기준값을 돌려주고 false를 알린다
        public void FaceCatalog_UnknownCharacter_ReturnsDefault() // 기준값 테스트
        {
            Assert.That(StandingFaceCatalog.TryGet("NPC_NOT_MEASURED", out StandingFace face), Is.False); // 잰 값 없음
            Assert.That(face.Height, Is.EqualTo(StandingFaceCatalog.Default.Height)); // 기준값
            Assert.That(StandingFaceCatalog.TryGet(null, out _), Is.False); // null 안전
        }

        [Test] // 초상화로 자르는 영역은 정사각이고 그림 안에 있으며 얼굴 중심을 품는다
        public void PortraitRect_IsSquareInsideSpriteAndContainsFace() // 초상화 영역 테스트
        {
            foreach (string characterId in DiaryCatalog.AllCharacters) // 12인 순회
            {
                StandingFaceCatalog.TryGet(characterId, out StandingFace face); // 얼굴 위치
                Rect rect = StandingFaceCatalog.GetPortraitRect(face, SourceRect); // 자를 영역
                Assert.That(rect.width, Is.EqualTo(rect.height), $"정사각이 아님 : {characterId}"); // 정사각
                Assert.That(rect.width, Is.InRange(250f, 520f), $"초상화 크기가 이상함 : {characterId}"); // 머리가 들어갈 크기 (Day86 — 4.5등신 새 그림은 머리가 커서 약 458)
                Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(SourceRect.xMin), characterId); // 왼쪽 안
                Assert.That(rect.yMin, Is.GreaterThanOrEqualTo(SourceRect.yMin), characterId); // 아래 안
                Assert.That(rect.xMax, Is.LessThanOrEqualTo(SourceRect.xMax), characterId); // 오른쪽 안
                Assert.That(rect.yMax, Is.LessThanOrEqualTo(SourceRect.yMax), characterId); // 위 안
                Vector2 faceCenter = new Vector2(face.CenterX * SourceRect.width, (1f - face.CenterY) * SourceRect.height); // 얼굴 중심 (아래 기준)
                Assert.That(rect.Contains(faceCenter), Is.True, $"얼굴이 영역 밖 : {characterId}"); // 얼굴 포함
            }
        }

        [Test] // 얼굴이 그림 모서리에 있어도 자르는 영역이 그림 밖으로 나가지 않는다
        public void PortraitRect_ClampsAtEdges() // 가장자리 제한 테스트
        {
            Rect topLeft = StandingFaceCatalog.GetPortraitRect(new StandingFace(0.01f, 0.01f, 0.08f), SourceRect); // 왼쪽 위 모서리
            Rect bottomRight = StandingFaceCatalog.GetPortraitRect(new StandingFace(0.99f, 0.99f, 0.08f), SourceRect); // 오른쪽 아래 모서리
            Rect huge = StandingFaceCatalog.GetPortraitRect(new StandingFace(0.5f, 0.5f, 0.9f), SourceRect); // 그림보다 큰 얼굴

            foreach (Rect rect in new[] { topLeft, bottomRight, huge }) // 세 경우 순회
            {
                Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(0f)); // 왼쪽 안
                Assert.That(rect.yMin, Is.GreaterThanOrEqualTo(0f)); // 아래 안
                Assert.That(rect.xMax, Is.LessThanOrEqualTo(SourceRect.width)); // 오른쪽 안
                Assert.That(rect.yMax, Is.LessThanOrEqualTo(SourceRect.height)); // 위 안
                Assert.That(rect.width, Is.EqualTo(rect.height)); // 정사각 유지
            }

            Assert.That(huge.width, Is.EqualTo(SourceRect.width)); // 그림 가로보다 클 수 없음
        }

        [Test] // 얼굴 기준 배치 : 얼굴 중심이 피벗이 되고, 얼굴이 정한 높이로 보이는 크기가 된다
        public void FrameOnFace_SetsPivotAndSize() // 얼굴 기준 배치 테스트
        {
            GameObject target = new GameObject("Portrait", typeof(RectTransform)); // 대상 객체

            try // 정리 보장
            {
                RectTransform rect = (RectTransform)target.transform; // 영역
                StandingFace face = new StandingFace(0.5f, 0.125f, 0.08f); // 가운데 · 위에서 1/8 · 높이 8%
                StandingFaceCatalog.FrameOnFace(rect, face, 2f / 3f, 110f); // 얼굴 높이 110으로 배치
                Assert.That(rect.pivot.x, Is.EqualTo(0.5f).Within(0.0001f)); // 가로 피벗
                Assert.That(rect.pivot.y, Is.EqualTo(0.875f).Within(0.0001f)); // 세로 피벗 (아래 기준으로 뒤집힘)
                Assert.That(rect.sizeDelta.y, Is.EqualTo(1375f).Within(0.01f)); // 110 ÷ 0.08
                Assert.That(rect.sizeDelta.x, Is.EqualTo(1375f * 2f / 3f).Within(0.01f)); // 그림 비율 유지
                Assert.That(() => StandingFaceCatalog.FrameOnFace(null, face, 1f, 100f), Throws.Nothing); // null 안전
            }
            finally // 임시 객체 정리
            {
                Object.DestroyImmediate(target); // 대상 제거
            }
        }

        [Test] // 12인 모두 초상화가 스탠딩에서 잘려 나오고, 같은 그림을 다시 만들지 않는다
        public void Portrait_IsCroppedFromStandingAndCached() // 초상화 자르기 테스트
        {
            foreach (string characterId in DiaryCatalog.AllCharacters) // 12인 순회
            {
                Sprite portrait = CharacterPortraitArt.Get(characterId, out bool placeholder); // 초상화
                Assert.That(placeholder, Is.False, $"임시 원으로 떨어짐 : {characterId}"); // 정식 그림
                Assert.That(portrait.rect.width, Is.EqualTo(portrait.rect.height), $"정사각이 아님 : {characterId}"); // 정사각
                Assert.That(CharacterPortraitArt.Get(characterId, out _), Is.SameAs(portrait), $"캐시되지 않음 : {characterId}"); // 캐시 사용
            }

            Assert.That(CharacterPortraitArt.Get("NPC_NOT_MEASURED", out bool unknownPlaceholder), Is.Not.Null); // 모르는 ID도 그림은 나옴
            Assert.That(unknownPlaceholder, Is.True); // 임시 원
        }

        [Test] // 대화 스탠딩 : 위쪽은 고정하고 배율만큼 아래로 커지며, 그림이 높이에 맞춰 그려질 만큼 영역이 넓다
        public void DialogueFrame_GrowsDownwardFromFixedTop() // 대화 구도 테스트
        {
            Assert.That(DialogueStandingFrame.Scale, Is.GreaterThanOrEqualTo(1f)); // 전신보다 작아지지 않음

            foreach (DialogueStageSlot slot in new[] { DialogueStageSlot.Left, DialogueStageSlot.Right, DialogueStageSlot.Center }) // 자리 순회
            {
                DialogueStandingFrame.GetAnchors(slot, out Vector2 min, out Vector2 max); // 영역
                float top = DialogueStandingFrame.GetTop(slot); // 그림 위쪽
                Assert.That(max.y, Is.EqualTo(top).Within(0.0001f)); // 위쪽 고정
                Assert.That(max.y - min.y, Is.EqualTo(top * DialogueStandingFrame.Scale).Within(0.0001f)); // 높이 = 전신 높이 × 배율
                Assert.That((min.x + max.x) * 0.5f, Is.EqualTo(DialogueStandingFrame.GetCenterX(slot)).Within(0.0001f)); // 가로 중심
                float rectAspect = ((max.x - min.x) * 16f) / ((max.y - min.y) * 9f); // 16:9 화면에서 영역의 가로÷세로
                Assert.That(rectAspect, Is.GreaterThanOrEqualTo(2f / 3f), $"영역이 좁아 그림이 줄어듦 : {slot}"); // 2:3 그림이 높이에 맞춰 그려짐
            }

            Assert.That(DialogueStandingFrame.GetCenterX(DialogueStageSlot.Left), Is.LessThan(0.5f)); // 왼쪽 자리
            Assert.That(DialogueStandingFrame.GetCenterX(DialogueStageSlot.Right), Is.GreaterThan(0.5f)); // 오른쪽 자리
            Assert.That(DialogueStandingFrame.Pivot, Is.EqualTo(new Vector2(0.5f, 1f))); // 머리 쪽 기준
        }

        [Test] // 상점 · 대장간 NPC 영역 : 위쪽과 가로는 그대로 두고 높이만 배율만큼 아래로 커진다
        public void GrowDown_KeepsTopAndWidth() // 아래로 키우기 테스트
        {
            Vector2 baseMin = new Vector2(-0.10f, 0.20f); // 원래 왼쪽 아래 (상점 NPC 영역)
            Vector2 baseMax = new Vector2(0.50f, 0.91f); // 원래 오른쪽 위
            DialogueStandingFrame.GrowDown(baseMin, baseMax, out Vector2 min, out Vector2 max); // 키운 영역
            Assert.That(max, Is.EqualTo(baseMax)); // 위쪽 고정
            Assert.That(min.x, Is.EqualTo(baseMin.x)); // 가로 그대로
            Assert.That(max.y - min.y, Is.EqualTo(0.71f * DialogueStandingFrame.Scale).Within(0.0001f)); // 높이 = 원래 높이 × 배율
            float rectAspect = ((max.x - min.x) * 16f) / ((max.y - min.y) * 9f); // 16:9 화면에서 영역의 가로÷세로
            Assert.That(rectAspect, Is.GreaterThanOrEqualTo(2f / 3f)); // 2:3 그림이 높이에 맞춰 그려짐
        }

        [Test] // NPC 4명에게 정식 스탠딩이 있고, 동료들과 같은 크기의 그림이다 (같은 배율로 나란히 서도록)
        public void NpcStandings_HaveArtOfTheSameSizeAsCharacters() // NPC 스탠딩 테스트
        {
            Sprite reference = DialogueArtFactory.GetStanding("CH_SERENA", null, out bool referencePlaceholder); // 기준 그림
            Assert.That(referencePlaceholder, Is.False); // 기준 그림 있음

            foreach (NpcProfile npc in new[] { NpcLineCatalog.Shopkeeper, NpcLineCatalog.Blacksmith, NpcLineCatalog.Shadow, NpcLineCatalog.Archai }) // NPC 순회
            {
                Sprite sprite = DialogueArtFactory.GetStanding(npc.Id, null, out bool placeholder); // NPC 스탠딩
                Assert.That(placeholder, Is.False, $"실루엣으로 떨어짐 : {npc.Id}"); // 정식 그림
                Assert.That(sprite.rect.size, Is.EqualTo(reference.rect.size), $"그림 크기가 다름 : {npc.Id}"); // 같은 크기
            }
        }
    }
}
