using System.Linq; // 목록 비교 기능
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Core; // 소리 이름표 · 전환 규칙 기능
using ProjectH.UI; // 버튼 소리 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Button 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class AudioWiringTests // Day88 소리 연결 테스트 (버튼 소리 · 겹침 방지 · 배경음 전환 · 상황별 소리)
    {
        private GameObject root; // 테스트 화면 루트

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 빈 루트 준비
        {
            root = new GameObject("Canvas", typeof(RectTransform)); // 루트
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 루트 제거
        {
            Object.DestroyImmediate(root); // 루트와 하위 제거
        }

        [Test] // 이름표 : 소리 이름이 겹치지 않고, 전체 목록에 모든 소리가 들어 있다
        public void Catalog_ListsEverySoundOnce() // 소리 목록 테스트
        {
            Assert.That(AudioCatalog.AllSfx.Distinct().Count(), Is.EqualTo(AudioCatalog.AllSfx.Length)); // 효과음 이름 중복 없음
            Assert.That(AudioCatalog.AllBgm.Distinct().Count(), Is.EqualTo(AudioCatalog.AllBgm.Length)); // 배경음 이름 중복 없음
            Assert.That(AudioCatalog.AllSfx.Length, Is.EqualTo(23)); // 효과음 23개 (기존 9 + Day88 14)
            Assert.That(AudioCatalog.AllBgm.Length, Is.EqualTo(6)); // 배경음 6곡 (기존 3 + Day88 3)
            Assert.That(AudioCatalog.AllSfx, Does.Contain(AudioCatalog.SfxBreak)); // 의상 파괴
            Assert.That(AudioCatalog.AllSfx, Does.Contain(AudioCatalog.SfxError)); // 실패
            Assert.That(AudioCatalog.AllBgm, Does.Contain(AudioCatalog.BgmBoss)); // 보스전

            foreach (string key in AudioCatalog.AllSfx) // 효과음 순회
            {
                Assert.That(AudioCatalog.GetSfxVolume(key), Is.InRange(0.3f, 1f), key); // 음량 배율 범위
                Assert.That(AudioCatalog.GetSfxInterval(key), Is.InRange(0.02f, 0.2f), key); // 겹침 간격 범위
            }

            Assert.That(AudioCatalog.GetSfxVolume(AudioCatalog.SfxHit), Is.LessThan(AudioCatalog.GetSfxVolume(AudioCatalog.SfxUltimate))); // 자주 나는 타격은 궁극기보다 작게
        }

        [Test] // 대신할 곡 : 새 곡의 파일이 없으면 원래 쓰던 곡으로 돌아가고, 끝없이 돌지 않는다
        public void Catalog_BgmFallbackEndsAtAnOriginalTrack() // 대체 곡 테스트
        {
            Assert.That(AudioCatalog.GetBgmFallback(AudioCatalog.BgmBoss), Is.EqualTo(AudioCatalog.BgmBattle)); // 보스전 → 전투
            Assert.That(AudioCatalog.GetBgmFallback(AudioCatalog.BgmVictory), Is.EqualTo(AudioCatalog.BgmVillage)); // 승리 → 평상시
            Assert.That(AudioCatalog.GetBgmFallback(AudioCatalog.BgmEnding), Is.EqualTo(AudioCatalog.BgmTitle)); // 엔딩 → 타이틀

            foreach (string key in AudioCatalog.AllBgm) // 배경음 순회
            {
                string current = key; // 따라갈 곡
                for (int step = 0; step < 4 && current.Length > 0; step++) current = AudioCatalog.GetBgmFallback(current); // 대신할 곡을 따라감
                Assert.That(current, Is.Empty, key); // 네 번 안에 끝남
            }
        }

        [Test] // 겹침 방지 : 같은 소리는 짧은 간격 안에 한 번만, 다른 소리는 따로 센다
        public void Throttle_PassesTheSameSoundOncePerInterval() // 겹침 방지 테스트
        {
            SfxThrottle throttle = new SfxThrottle(); // 거름 장치
            Assert.That(throttle.TryPass(AudioCatalog.SfxHit, 10.00f, 0.08f), Is.True); // 첫 타격
            Assert.That(throttle.TryPass(AudioCatalog.SfxHit, 10.00f, 0.08f), Is.False); // 같은 순간의 두 번째 타격 (광역 공격)
            Assert.That(throttle.TryPass(AudioCatalog.SfxHit, 10.05f, 0.08f), Is.False); // 간격 안
            Assert.That(throttle.TryPass(AudioCatalog.SfxHeal, 10.05f, 0.05f), Is.True); // 다른 소리는 따로
            Assert.That(throttle.TryPass(AudioCatalog.SfxHit, 10.09f, 0.08f), Is.True); // 간격이 지남
            Assert.That(throttle.TryPass(AudioCatalog.SfxHit, 1.00f, 0.08f), Is.True); // 시각이 되돌아가도 막히지 않음
            Assert.That(throttle.TryPass(string.Empty, 20f, 0.05f), Is.False); // 이름 없는 소리
            throttle.Clear(); // 기록 비움
            Assert.That(throttle.TryPass(AudioCatalog.SfxHit, 1.00f, 0.08f), Is.True); // 다시 통과
        }

        [Test] // 배경음 전환 : 지금 곡을 줄인 뒤에 바꾸고 다시 키운다. 처음 트는 곡은 기다리지 않는다
        public void Fader_FadesOutSwapsThenFadesIn() // 배경음 전환 테스트
        {
            BgmFader fader = new BgmFader(); // 전환 장치
            Assert.That(fader.IsBusy, Is.False); // 할 일 없음
            fader.Request(AudioCatalog.BgmTitle); // 첫 곡
            Assert.That(fader.Tick(0.01f), Is.True); // 나오는 곡이 없으므로 바로 교체
            Assert.That(fader.Current, Is.EqualTo(AudioCatalog.BgmTitle)); // 타이틀 곡
            Assert.That(fader.Gain, Is.EqualTo(0f)); // 0에서 시작
            Assert.That(fader.Tick(BgmFader.FadeInSeconds), Is.False); // 키움
            Assert.That(fader.Gain, Is.EqualTo(1f).Within(0.001f)); // 다 커짐
            Assert.That(fader.IsBusy, Is.False); // 전환 끝

            fader.Request(AudioCatalog.BgmTitle); // 같은 곡 요청
            Assert.That(fader.IsBusy, Is.False); // 아무 일도 없음 (씬이 바뀌어도 곡이 이어진다)

            fader.Request(AudioCatalog.BgmBattle); // 다른 곡 요청
            Assert.That(fader.Tick(BgmFader.FadeOutSeconds * 0.5f), Is.False); // 줄이는 중
            Assert.That(fader.Current, Is.EqualTo(AudioCatalog.BgmTitle)); // 아직 이전 곡
            Assert.That(fader.Gain, Is.EqualTo(0.5f).Within(0.01f)); // 절반
            Assert.That(fader.Tick(BgmFader.FadeOutSeconds), Is.True); // 다 줄이고 교체
            Assert.That(fader.Current, Is.EqualTo(AudioCatalog.BgmBattle)); // 전투 곡
            fader.Tick(BgmFader.FadeInSeconds); // 키움
            Assert.That(fader.Gain, Is.EqualTo(1f).Within(0.001f)); // 다 커짐
        }

        [Test] // 배경음 전환 : 줄이는 도중에 원래 곡으로 돌아오면 바꾸지 않고 다시 키우고, 빈 값은 무음으로 바뀐다
        public void Fader_ReturnsToTheSameTrackAndStops() // 되돌아오기 · 끄기 테스트
        {
            BgmFader fader = new BgmFader(); // 전환 장치
            fader.Request(AudioCatalog.BgmBattle); // 전투 곡
            fader.Tick(0.01f); // 교체
            fader.Tick(1f); // 다 커짐
            fader.Request(AudioCatalog.BgmBoss); // 보스 곡으로
            fader.Tick(BgmFader.FadeOutSeconds * 0.5f); // 절반쯤 줄임
            fader.Request(AudioCatalog.BgmBattle); // 다시 전투 곡으로
            Assert.That(fader.Tick(0.01f), Is.False); // 교체 없음
            Assert.That(fader.Current, Is.EqualTo(AudioCatalog.BgmBattle)); // 그대로
            fader.Tick(1f); // 다시 키움
            Assert.That(fader.Gain, Is.EqualTo(1f).Within(0.001f)); // 다 커짐

            fader.Request(null); // 끄기 (패배)
            Assert.That(fader.Tick(1f), Is.True); // 다 줄이고 교체
            Assert.That(fader.Current, Is.Empty); // 무음
            Assert.That(fader.Tick(-5f), Is.False); // 음수 시간은 무시
        }

        [Test] // 버튼 이름 : 닫기 · 뒤로는 취소, 탭 · 넘기기는 페이지, 확인은 확인, 화면을 덮는 투명 버튼은 무음
        public void DefaultKey_FollowsTheButtonName() // 버튼 이름 → 소리 테스트
        {
            Assert.That(UiSound.GetDefaultKey("Close"), Is.EqualTo(AudioCatalog.SfxCancel)); // 닫기
            Assert.That(UiSound.GetDefaultKey("CloseMenuButton"), Is.EqualTo(AudioCatalog.SfxCancel)); // 전투 메뉴 닫기 (씬 버튼)
            Assert.That(UiSound.GetDefaultKey("BackButton"), Is.EqualTo(AudioCatalog.SfxCancel)); // 뒤로
            Assert.That(UiSound.GetDefaultKey("CancelPopupButton"), Is.EqualTo(AudioCatalog.SfxCancel)); // 취소 (씬 버튼)
            Assert.That(UiSound.GetDefaultKey("No"), Is.EqualTo(AudioCatalog.SfxCancel)); // 아니오
            Assert.That(UiSound.GetDefaultKey("닫기"), Is.EqualTo(AudioCatalog.SfxCancel)); // 한글 이름
            Assert.That(UiSound.GetDefaultKey("Confirm"), Is.EqualTo(AudioCatalog.SfxConfirm)); // 확인
            Assert.That(UiSound.GetDefaultKey("Yes"), Is.EqualTo(AudioCatalog.SfxConfirm)); // 예
            Assert.That(UiSound.GetDefaultKey("Tab_2"), Is.EqualTo(AudioCatalog.SfxPage)); // 탭
            Assert.That(UiSound.GetDefaultKey("EnhanceTab"), Is.EqualTo(AudioCatalog.SfxPage)); // 대장간 탭
            Assert.That(UiSound.GetDefaultKey("FilterButton_0"), Is.EqualTo(AudioCatalog.SfxPage)); // 필터 (씬 버튼)
            Assert.That(UiSound.GetDefaultKey("NextPage"), Is.EqualTo(AudioCatalog.SfxPage)); // 다음 쪽
            Assert.That(UiSound.GetDefaultKey("PreviousCharacter"), Is.EqualTo(AudioCatalog.SfxPage)); // 이전 캐릭터
            Assert.That(UiSound.GetDefaultKey("Nav_모험"), Is.EqualTo(AudioCatalog.SfxPage)); // 로비 내비 (씬 버튼)
            Assert.That(UiSound.GetDefaultKey("NewGameButton"), Is.EqualTo(AudioCatalog.SfxClick)); // 그 밖의 버튼
            Assert.That(UiSound.GetDefaultKey("Background"), Is.EqualTo(AudioCatalog.SfxClick)); // "Back"으로 시작하지만 다른 낱말
            Assert.That(UiSound.GetDefaultKey("Node_3"), Is.EqualTo(AudioCatalog.SfxClick)); // "No"로 시작하지만 다른 낱말
            Assert.That(UiSound.GetDefaultKey("Table"), Is.EqualTo(AudioCatalog.SfxClick)); // "Tab"으로 시작하지만 다른 낱말
            Assert.That(UiSound.GetDefaultKey("ClickCatcher"), Is.EqualTo(UiSound.Silent)); // 대사 넘김용 투명 버튼
            Assert.That(UiSound.GetDefaultKey("Dim"), Is.EqualTo(UiSound.Silent)); // 어두운 막
            Assert.That(UiSound.GetDefaultKey(null), Is.EqualTo(AudioCatalog.SfxClick)); // 이름 없음
        }

        [Test] // 코드로 만든 버튼 : 만들 때 소리가 붙고, 다시 붙여도 하나뿐이며, 소리를 바꿀 수 있다
        public void RuntimeButton_GetsOneSound() // 런타임 버튼 테스트
        {
            Button close = RuntimeUiKit.CreateButton(root.transform, "Close", Color.white); // 닫기 버튼
            Button buy = RuntimeUiKit.CreateButton(root.transform, "Buy", Color.white); // 구매 버튼
            Assert.That(close.GetComponent<UiButtonSound>().Key, Is.EqualTo(AudioCatalog.SfxCancel)); // 이름으로 고른 취소 소리
            Assert.That(buy.GetComponent<UiButtonSound>().Key, Is.EqualTo(AudioCatalog.SfxClick)); // 기본 소리
            UiButtonSound again = UiSound.Attach(buy); // 다시 붙이기
            Assert.That(buy.GetComponents<UiButtonSound>().Length, Is.EqualTo(1)); // 하나뿐 (소리가 두 번 나지 않는다)
            Assert.That(again.Key, Is.EqualTo(AudioCatalog.SfxClick)); // 그대로
            UiSound.Attach(buy, AudioCatalog.SfxConfirm); // 소리 바꾸기
            Assert.That(buy.GetComponent<UiButtonSound>().Key, Is.EqualTo(AudioCatalog.SfxConfirm)); // 바뀜
            Assert.That(buy.GetComponents<UiButtonSound>().Length, Is.EqualTo(1)); // 여전히 하나
            Assert.That(UiSound.Attach(null), Is.Null); // null 안전
            Assert.DoesNotThrow(() => close.onClick.Invoke()); // 재생기가 없는 편집 모드에서도 눌러서 오류가 없다
        }

        [Test] // 씬에 놓인 버튼 : 소리가 없는 버튼에만 붙이고, 꺼져 있는 버튼도 빠뜨리지 않는다
        public void ScenePatch_AttachesToPlainButtonsOnly() // 씬 버튼 테스트
        {
            Button runtime = RuntimeUiKit.CreateButton(root.transform, "Runtime", Color.white); // 이미 소리가 붙은 버튼
            GameObject plainObject = new GameObject("NewGameButton", typeof(RectTransform), typeof(Image), typeof(Button)); // 씬에 놓인 것과 같은 맨 버튼
            plainObject.transform.SetParent(root.transform, false); // 루트 하위
            GameObject hiddenObject = new GameObject("CloseMenuButton", typeof(RectTransform), typeof(Image), typeof(Button)); // 꺼져 있는 맨 버튼
            hiddenObject.transform.SetParent(root.transform, false); // 루트 하위
            hiddenObject.SetActive(false); // 끔
            Assert.That(ButtonSoundScenePatch.Apply(root.transform), Is.EqualTo(2)); // 맨 버튼 두 개에만 붙임
            Assert.That(plainObject.GetComponent<UiButtonSound>().Key, Is.EqualTo(AudioCatalog.SfxClick)); // 기본 소리
            Assert.That(hiddenObject.GetComponent<UiButtonSound>().Key, Is.EqualTo(AudioCatalog.SfxCancel)); // 이름으로 고른 취소 소리
            Assert.That(runtime.GetComponents<UiButtonSound>().Length, Is.EqualTo(1)); // 이미 붙은 버튼은 그대로
            Assert.That(ButtonSoundScenePatch.Apply(root.transform), Is.EqualTo(0)); // 다시 불러도 더 붙지 않음
            Assert.That(ButtonSoundScenePatch.Apply(null), Is.EqualTo(0)); // null 안전
        }

        [Test] // 상점 · 대장간 : NPC가 말하는 상황마다 알맞은 소리가 정해져 있다
        public void NpcLine_MapsToResultSound() // 상황별 소리 테스트
        {
            Assert.That(UiSound.GetNpcLineKey(NpcLineKind.Buy), Is.EqualTo(AudioCatalog.SfxItem)); // 구매
            Assert.That(UiSound.GetNpcLineKey(NpcLineKind.Sell), Is.EqualTo(AudioCatalog.SfxGold)); // 판매
            Assert.That(UiSound.GetNpcLineKey(NpcLineKind.NoGold), Is.EqualTo(AudioCatalog.SfxError)); // 골드 부족
            Assert.That(UiSound.GetNpcLineKey(NpcLineKind.SoldOut), Is.EqualTo(AudioCatalog.SfxError)); // 품절
            Assert.That(UiSound.GetNpcLineKey(NpcLineKind.NeedMaterial), Is.EqualTo(AudioCatalog.SfxError)); // 재료 부족
            Assert.That(UiSound.GetNpcLineKey(NpcLineKind.EnhanceSuccess), Is.EqualTo(AudioCatalog.SfxEnhanceSuccess)); // 강화 성공
            Assert.That(UiSound.GetNpcLineKey(NpcLineKind.TranscendSuccess), Is.EqualTo(AudioCatalog.SfxEnhanceSuccess)); // 초월 성공
            Assert.That(UiSound.GetNpcLineKey(NpcLineKind.EnhanceFail), Is.EqualTo(AudioCatalog.SfxEnhanceFail)); // 강화 실패
            Assert.That(UiSound.GetNpcLineKey(NpcLineKind.Greeting), Is.EqualTo(UiSound.Silent)); // 인사는 소리 없음
            Assert.That(UiSound.GetNpcLineKey(NpcLineKind.Describe), Is.EqualTo(UiSound.Silent)); // 설명도 소리 없음
            Assert.DoesNotThrow(() => UiSound.Result(true)); // 재생기가 없어도 오류 없음
            Assert.DoesNotThrow(() => UiSound.Result(false, AudioCatalog.SfxItem)); // 재생기가 없어도 오류 없음
        }
    }
}
