using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 의상 파괴 규칙 · 연출 기능
using ProjectH.Core; // 환경 설정 기능
using ProjectH.UI; // 그림 찾기 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // GraphicRaycaster 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class OutfitBreakTests // Day87 의상 파괴 연출 테스트 (문턱 · 한 번만 · 차례 · 시간 · 그림 · 화면)
    {
        private OutfitBreakView view; // 테스트 연출

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 설정을 기본값으로 (이전 테스트가 연출을 꺼 둔 채 끝나도 영향이 없게)
        {
            GameSettings.ResetToDefault(); // 설정 기본값
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 연출 제거 · 설정 복원
        {
            if (view != null) Object.DestroyImmediate(view.gameObject); // 연출 제거
            view = null; // 참조 해제
            GameSettings.ResetToDefault(); // 설정 기본값 복원
        }

        [Test] // 문턱 : 60% 이하는 1단계, 30% 이하는 2단계, 쓰러지면 단계 없음
        public void Stage_FollowsThresholds() // 단계 판정 테스트
        {
            Assert.That(OutfitBreakRules.GetStage(1f), Is.EqualTo(OutfitBreakRules.None)); // 가득
            Assert.That(OutfitBreakRules.GetStage(0.61f), Is.EqualTo(OutfitBreakRules.None)); // 문턱 바로 위
            Assert.That(OutfitBreakRules.GetStage(0.60f), Is.EqualTo(OutfitBreakRules.Light)); // 60% 정확히
            Assert.That(OutfitBreakRules.GetStage(0.31f), Is.EqualTo(OutfitBreakRules.Light)); // 2단계 바로 위
            Assert.That(OutfitBreakRules.GetStage(0.30f), Is.EqualTo(OutfitBreakRules.Heavy)); // 30% 정확히
            Assert.That(OutfitBreakRules.GetStage(0.01f), Is.EqualTo(OutfitBreakRules.Heavy)); // 거의 바닥
            Assert.That(OutfitBreakRules.GetStage(0f), Is.EqualTo(OutfitBreakRules.None)); // 쓰러짐
            Assert.That(OutfitBreakRules.GetLabel(OutfitBreakRules.Light), Is.EqualTo("의상 손상")); // 1단계 이름
            Assert.That(OutfitBreakRules.GetLabel(OutfitBreakRules.Heavy), Is.EqualTo("의상 파괴")); // 2단계 이름
            Assert.That(OutfitBreakRules.GetLabel(OutfitBreakRules.None), Is.Empty); // 이름 없음
        }

        [Test] // 실제 체력 숫자 : 100 중 60이 남으면 1단계, 30이 남으면 2단계 (나눗셈 오차로 문턱을 놓치지 않는다)
        public void Stage_MatchesRealHealthNumbers() // 체력 숫자 테스트
        {
            BattleStats stats = CreateStats("ALLY_0", "CH_SERENA", "세레나"); // 체력 100
            stats.TakeDamage(39); // 61 남음
            Assert.That(OutfitBreakRules.GetStage(stats.HealthRatio), Is.EqualTo(OutfitBreakRules.None)); // 아직 멀쩡
            stats.TakeDamage(1); // 60 남음
            Assert.That(OutfitBreakRules.GetStage(stats.HealthRatio), Is.EqualTo(OutfitBreakRules.Light)); // 1단계
            stats.TakeDamage(30); // 30 남음
            Assert.That(OutfitBreakRules.GetStage(stats.HealthRatio), Is.EqualTo(OutfitBreakRules.Heavy)); // 2단계
        }

        [Test] // 한 번만 : 단계마다 한 번씩 나오고, 회복했다가 다시 내려와도 또 나오지 않는다
        public void Tracker_ShowsEachStageOnce() // 한 번만 테스트
        {
            OutfitBreakTracker tracker = new OutfitBreakTracker(); // 기록
            Assert.That(tracker.Report("ALLY_0", 0.80f), Is.EqualTo(OutfitBreakRules.None)); // 아직 멀쩡
            Assert.That(tracker.Report("ALLY_0", 0.55f), Is.EqualTo(OutfitBreakRules.Light)); // 1단계
            Assert.That(tracker.Report("ALLY_0", 0.50f), Is.EqualTo(OutfitBreakRules.None)); // 같은 단계 안
            Assert.That(tracker.Report("ALLY_0", 0.90f), Is.EqualTo(OutfitBreakRules.None)); // 회복
            Assert.That(tracker.Report("ALLY_0", 0.58f), Is.EqualTo(OutfitBreakRules.None)); // 다시 내려와도 안 나옴
            Assert.That(tracker.Report("ALLY_0", 0.25f), Is.EqualTo(OutfitBreakRules.Heavy)); // 2단계
            Assert.That(tracker.Report("ALLY_0", 0.10f), Is.EqualTo(OutfitBreakRules.None)); // 같은 단계 안
            Assert.That(tracker.GetShownStage("ALLY_0"), Is.EqualTo(OutfitBreakRules.Heavy)); // 기록
            Assert.That(tracker.Report("ALLY_1", 0.55f), Is.EqualTo(OutfitBreakRules.Light)); // 다른 아군은 따로
            tracker.Clear(); // 다음 전투
            Assert.That(tracker.Report("ALLY_0", 0.55f), Is.EqualTo(OutfitBreakRules.Light)); // 다시 나옴
        }

        [Test] // 건너뛰기 : 한 방에 30% 아래로 떨어지면 2단계만 나오고 1단계는 나중에도 나오지 않는다
        public void Tracker_SkipsLightWhenDroppingStraightToHeavy() // 단계 건너뛰기 테스트
        {
            OutfitBreakTracker tracker = new OutfitBreakTracker(); // 기록
            Assert.That(tracker.Report("ALLY_0", 0.20f), Is.EqualTo(OutfitBreakRules.Heavy)); // 바로 2단계
            Assert.That(tracker.Report("ALLY_0", 0.90f), Is.EqualTo(OutfitBreakRules.None)); // 회복
            Assert.That(tracker.Report("ALLY_0", 0.50f), Is.EqualTo(OutfitBreakRules.None)); // 1단계는 다시 안 나옴
        }

        [Test] // 쓰러짐 · 부활 : 한 방에 쓰러지면 안 나오고, 부활로 체력이 오를 때도 안 나오며, 그 뒤에 맞으면 나온다
        public void Tracker_IgnoresDownAndRevive() // 쓰러짐 · 부활 테스트
        {
            OutfitBreakTracker tracker = new OutfitBreakTracker(); // 기록
            Assert.That(tracker.Report("ALLY_0", 0f), Is.EqualTo(OutfitBreakRules.None)); // 한 방에 쓰러짐
            Assert.That(tracker.Report("ALLY_0", 0.30f), Is.EqualTo(OutfitBreakRules.None)); // 부활 (체력이 오름)
            Assert.That(tracker.Report("ALLY_0", 0.22f), Is.EqualTo(OutfitBreakRules.Heavy)); // 부활 뒤에 맞음
            Assert.That(tracker.Report(string.Empty, 0.10f), Is.EqualTo(OutfitBreakRules.None)); // 대상 없음
        }

        [Test] // 차례 : 온 순서대로 나오고, 같은 캐릭터가 기다리는 중이면 높은 단계 하나로 합친다
        public void Queue_KeepsOrderAndMergesSameCharacter() // 대기열 테스트
        {
            OutfitBreakQueue queue = new OutfitBreakQueue(); // 대기열
            queue.Enqueue(new OutfitBreakRequest("CH_SERENA", "세레나", OutfitBreakRules.Light)); // 세레나 1단계
            queue.Enqueue(new OutfitBreakRequest("CH_ELLEN", "엘렌", OutfitBreakRules.Light)); // 엘렌 1단계
            queue.Enqueue(new OutfitBreakRequest("CH_SERENA", "세레나", OutfitBreakRules.Heavy)); // 세레나 2단계 (합쳐짐)
            queue.Enqueue(new OutfitBreakRequest("CH_ELLEN", "엘렌", OutfitBreakRules.Light)); // 같은 단계 (무시)
            queue.Enqueue(new OutfitBreakRequest(string.Empty, "?", OutfitBreakRules.Heavy)); // 잘못된 요청 (무시)
            queue.Enqueue(new OutfitBreakRequest("CH_LILIA", "릴리아", OutfitBreakRules.None)); // 단계 없음 (무시)
            Assert.That(queue.Count, Is.EqualTo(2)); // 두 명
            Assert.That(queue.TryDequeue(out OutfitBreakRequest first), Is.True); // 첫 번째
            Assert.That(first.CharacterId, Is.EqualTo("CH_SERENA")); // 먼저 온 세레나
            Assert.That(first.Stage, Is.EqualTo(OutfitBreakRules.Heavy)); // 2단계 하나로
            Assert.That(queue.TryDequeue(out OutfitBreakRequest second), Is.True); // 두 번째
            Assert.That(second.CharacterId, Is.EqualTo("CH_ELLEN")); // 엘렌
            Assert.That(queue.TryDequeue(out _), Is.False); // 끝
        }

        [Test] // 시간 : 들어옴 → 머묾 → 나감. 기다리는 그림이 있으면 짧게 머문다
        public void Timeline_EntersHoldsAndLeaves() // 시간 진행 테스트
        {
            OutfitBreakTimeline timeline = new OutfitBreakTimeline(); // 타임라인
            timeline.Start(false); // 여유 있음
            Assert.That(timeline.Slide, Is.EqualTo(1f).Within(0.001f)); // 처음에는 화면 밖
            timeline.Advance(OutfitBreakTimeline.EnterSeconds); // 다 들어옴
            Assert.That(timeline.Slide, Is.EqualTo(0f).Within(0.001f)); // 제자리
            Assert.That(timeline.Flash, Is.EqualTo(1f).Within(0.001f)); // 도착 순간 가장 밝음
            timeline.Advance(OutfitBreakTimeline.HoldSeconds * 0.5f); // 머무는 중
            Assert.That(timeline.Slide, Is.EqualTo(0f).Within(0.001f)); // 제자리
            Assert.That(timeline.Flash, Is.EqualTo(0f).Within(0.001f)); // 번쩍임 끝
            Assert.That(timeline.IsDone, Is.False); // 아직
            timeline.Advance((OutfitBreakTimeline.HoldSeconds * 0.5f) + OutfitBreakTimeline.ExitSeconds + 0.01f); // 끝까지 (소수 오차만큼 넉넉히)
            Assert.That(timeline.Slide, Is.EqualTo(1f).Within(0.001f)); // 화면 밖
            Assert.That(timeline.IsDone, Is.True); // 끝
            Assert.That(timeline.TotalSeconds, Is.EqualTo(1.6f).Within(0.001f)); // 전체 1.6초

            timeline.Start(true); // 기다리는 그림 있음
            Assert.That(timeline.IsDone, Is.False); // 다시 시작
            Assert.That(timeline.TotalSeconds, Is.LessThan(1.2f)); // 짧게
            timeline.Start(false); // 여유 있게 시작
            timeline.Hurry(); // 도중에 다음 그림이 생김
            Assert.That(timeline.TotalSeconds, Is.LessThan(1.2f)); // 짧아짐
            timeline.Advance(-1f); // 음수 시간
            Assert.That(timeline.Elapsed, Is.EqualTo(0f)); // 무시
        }

        [Test] // 그림 : 12명 × 2단계 모두 보여 줄 그림이 있다 (전용 그림이 없으면 스탠딩 표정)
        public void Art_EveryCharacterHasSomethingToShow() // 그림 찾기 테스트
        {
            foreach (string characterId in StandingFaceCatalog.CharacterIds) // 12명 순회
            {
                for (int stage = OutfitBreakRules.Light; stage <= OutfitBreakRules.Heavy; stage++) // 2단계 순회
                {
                    Sprite sprite = OutfitBreakArt.Get(characterId, stage, out bool dedicated); // 그림
                    Assert.That(sprite, Is.Not.Null, $"그림 없음 : {characterId} {stage}단계"); // 있음
                    Assert.That(dedicated, Is.EqualTo(OutfitBreakArt.HasArt(characterId, stage) || OutfitBreakArt.HasArt(characterId, OutfitBreakRules.Light)), characterId); // 전용 그림을 썼는지 (그 단계 또는 아래 단계)
                }
            }

            Assert.That(OutfitBreakArt.Get("CH_NOBODY", OutfitBreakRules.Heavy, out _), Is.Null); // 스탠딩도 없는 캐릭터는 보여 주지 않음
            Assert.That(OutfitBreakArt.Get("CH_SERENA", OutfitBreakRules.None, out _), Is.Null); // 단계 없음
            Assert.That(OutfitBreakArt.Get(string.Empty, OutfitBreakRules.Light, out _), Is.Null); // 대상 없음
            Assert.That(OutfitBreakArt.GetResourcePath("CH_SERENA", 2), Is.EqualTo("BattleBreak/CH_SERENA_2")); // 파일 이름 규칙
            Assert.That(OutfitBreakArt.Folder.StartsWith(UiIcon.Folder, System.StringComparison.OrdinalIgnoreCase), Is.False); // 메뉴 아이콘 폴더와 겹치지 않음 (Day86의 이름 겹침 사고 방지)
            Assert.That(OutfitBreakArt.GetFallbackExpression(OutfitBreakRules.Light), Is.Not.EqualTo(OutfitBreakArt.GetFallbackExpression(OutfitBreakRules.Heavy))); // 단계마다 다른 표정
        }

        [Test] // 전용 그림 : 12명 × 2단계 = 24장이 모두 들어 있다 (이름이 틀리거나 빠지면 스탠딩 표정이 조용히 대신 나와 눈에 띄지 않는다)
        public void Art_AllDedicatedArtIsInstalled() // 전용 그림 설치 테스트
        {
            int count = 0; // 확인한 그림 수

            foreach (string characterId in StandingFaceCatalog.CharacterIds) // 12명 순회
            {
                Sprite light = OutfitBreakArt.Get(characterId, OutfitBreakRules.Light, out bool lightDedicated); // 1단계 그림
                Sprite heavy = OutfitBreakArt.Get(characterId, OutfitBreakRules.Heavy, out bool heavyDedicated); // 2단계 그림
                Assert.That(OutfitBreakArt.HasArt(characterId, OutfitBreakRules.Light), Is.True, $"전용 그림 없음 : {OutfitBreakArt.GetResourcePath(characterId, OutfitBreakRules.Light)}"); // 1단계 파일
                Assert.That(OutfitBreakArt.HasArt(characterId, OutfitBreakRules.Heavy), Is.True, $"전용 그림 없음 : {OutfitBreakArt.GetResourcePath(characterId, OutfitBreakRules.Heavy)}"); // 2단계 파일
                Assert.That(lightDedicated && heavyDedicated, Is.True, characterId); // 스탠딩 표정이 아닌 전용 그림
                Assert.That(heavy, Is.Not.SameAs(light), characterId); // 단계마다 다른 그림
                Assert.That(light.rect.width / light.rect.height, Is.EqualTo(1024f / 1536f).Within(0.001f), characterId); // 스탠딩과 같은 비율 (틀 안 구도가 같아야 한다)
                Assert.That(heavy.rect.width / heavy.rect.height, Is.EqualTo(1024f / 1536f).Within(0.001f), characterId); // 스탠딩과 같은 비율
                count += 2; // 두 장 확인
            }

            Assert.That(count, Is.EqualTo(24)); // 12명 × 2단계
        }

        [Test] // 배치 : 틀은 HUD 카드 위 · 웨이브 표시 아래 · 아군 왼쪽의 빈자리에 있고, 입력을 막지 않는다
        public void View_SitsInTheFreeLeftAreaAndPassesInput() // 화면 배치 테스트
        {
            view = OutfitBreakView.Create(); // 연출
            Assert.That(OutfitBreakView.FrameMin.y, Is.GreaterThan(0.225f)); // HUD 카드 위쪽 끝보다 위
            Assert.That(OutfitBreakView.FrameMax.y, Is.LessThan(0.915f)); // 웨이브 표시 아래쪽 끝보다 아래
            Assert.That(OutfitBreakView.FrameMin.x, Is.EqualTo(0f)); // 화면 왼쪽 끝
            Assert.That(OutfitBreakView.FrameMax.x, Is.LessThanOrEqualTo(0.20f)); // 아군이 서는 자리보다 왼쪽
            Assert.That(view.GetComponent<GraphicRaycaster>() == null, Is.True); // 클릭 판정 없음
            Assert.That(view.GetComponent<CanvasGroup>().blocksRaycasts, Is.False); // 입력 통과
            Assert.That(view.GetComponent<CanvasGroup>().alpha, Is.EqualTo(0f)); // 처음에는 숨김
            Assert.That(view.GetComponent<Canvas>().sortingOrder, Is.LessThan(450)); // 보스 연출 · 결과창 아래
            Assert.That(view.IsShowing, Is.False); // 표시 안 함
        }

        [Test] // 표시 : 체력이 문턱 아래로 내려가면 그림이 뜨고, 시간이 지나면 사라진다
        public void View_ShowsThenHides() // 표시 · 숨김 테스트
        {
            view = OutfitBreakView.Create(); // 연출
            Assert.That(view.Report("ALLY_0", "CH_SERENA", "세레나", 0.80f), Is.False); // 아직 멀쩡
            Assert.That(view.Report("ALLY_0", "CH_SERENA", "세레나", 0.55f), Is.True); // 1단계
            Assert.That(view.PendingCount, Is.EqualTo(1)); // 대기 1
            view.Tick(0.05f, true); // 진행
            Assert.That(view.IsShowing, Is.True); // 표시 중
            Assert.That(view.PendingCount, Is.EqualTo(0)); // 대기 없음
            Assert.That(view.Current.CharacterId, Is.EqualTo("CH_SERENA")); // 세레나
            Assert.That(view.Art.sprite, Is.Not.Null); // 그림 있음
            Assert.That(view.Label.text, Is.EqualTo("세레나 · 의상 손상")); // 이름표
            Assert.That(view.GetComponent<CanvasGroup>().alpha, Is.GreaterThan(0f)); // 보임
            Assert.That(view.Frame.anchorMin.x, Is.LessThan(OutfitBreakView.FrameMin.x)); // 아직 왼쪽에서 들어오는 중
            view.Tick(OutfitBreakTimeline.EnterSeconds, true); // 다 들어옴
            Assert.That(view.Frame.anchorMin.x, Is.EqualTo(OutfitBreakView.FrameMin.x).Within(0.0001f)); // 제자리
            Assert.That(view.Frame.anchorMax.x, Is.EqualTo(OutfitBreakView.FrameMax.x).Within(0.0001f)); // 제자리
            view.Tick(5f, true); // 끝까지
            Assert.That(view.IsShowing, Is.False); // 사라짐
            Assert.That(view.GetComponent<CanvasGroup>().alpha, Is.EqualTo(0f)); // 숨김
        }

        [Test] // 기다림 : 전투가 멈춘 동안(일시정지 · 궁극기 컷인 · 리듬 판정)에는 시작하지 않고, 풀리면 차례대로 보여 준다
        public void View_WaitsWhilePausedAndShowsInOrder() // 일시정지 · 차례 테스트
        {
            view = OutfitBreakView.Create(); // 연출
            view.Report("ALLY_0", "CH_SERENA", "세레나", 0.25f); // 세레나 2단계
            view.Report("ALLY_1", "CH_ELLEN", "엘렌", 0.50f); // 엘렌 1단계
            view.Tick(3f, false); // 전투 멈춤
            Assert.That(view.IsShowing, Is.False); // 시작 안 함
            Assert.That(view.PendingCount, Is.EqualTo(2)); // 그대로 대기
            view.Tick(0.05f, true); // 전투 재개
            Assert.That(view.Current.CharacterId, Is.EqualTo("CH_SERENA")); // 먼저 온 세레나
            Assert.That(view.Label.text, Is.EqualTo("세레나 · 의상 파괴")); // 2단계 이름표
            view.Tick(0.2f, false); // 보여 주는 도중에 멈춤
            Assert.That(view.IsShowing, Is.True); // 그대로 떠 있음
            view.Tick(5f, true); // 세레나 끝
            Assert.That(view.IsShowing, Is.False); // 사라짐
            view.Tick(0.05f, true); // 다음 차례
            Assert.That(view.Current.CharacterId, Is.EqualTo("CH_ELLEN")); // 엘렌
            Assert.That(view.Label.text, Is.EqualTo("엘렌 · 의상 손상")); // 1단계 이름표
        }

        [Test] // 설정 : 끄면 그림이 나오지 않고, 꺼 둔 동안 지나간 단계는 켠 뒤에도 다시 나오지 않는다
        public void View_RespectsTheSetting() // 설정 테스트
        {
            GameSettings.ResetToDefault(); // 기본값
            Assert.That(GameSettings.OutfitBreak, Is.True); // 기본은 켬
            view = OutfitBreakView.Create(); // 연출
            GameSettings.SetOutfitBreak(false); // 끔
            Assert.That(view.Report("ALLY_0", "CH_SERENA", "세레나", 0.55f), Is.False); // 안 나옴
            Assert.That(view.PendingCount, Is.EqualTo(0)); // 대기 없음
            GameSettings.SetOutfitBreak(true); // 다시 켬
            Assert.That(view.Report("ALLY_0", "CH_SERENA", "세레나", 0.50f), Is.False); // 지나간 1단계는 다시 안 나옴
            Assert.That(view.Report("ALLY_0", "CH_SERENA", "세레나", 0.25f), Is.True); // 2단계는 나옴
        }

        [Test] // 연결 : 실제 전투 스탯의 체력이 깎이면 보고가 들어오고, 정리한 뒤에는 들어오지 않는다
        public void View_ListensToRealHealthChanges() // 체력 이벤트 연결 테스트
        {
            view = OutfitBreakView.Create(); // 연출
            BattleStats stats = CreateStats("ALLY_0", "CH_SERENA", "세레나"); // 체력 100
            view.Add(stats); // 지켜보기
            view.Add(stats); // 같은 아군을 다시 (무시)
            Assert.That(view.WatchedCount, Is.EqualTo(1)); // 한 번만
            stats.TakeDamage(30); // 70 남음
            Assert.That(view.PendingCount, Is.EqualTo(0)); // 아직
            stats.TakeDamage(15); // 55 남음
            Assert.That(view.PendingCount, Is.EqualTo(1)); // 1단계 대기
            view.Tick(0.05f, true); // 표시
            Assert.That(view.Current.DisplayName, Is.EqualTo("세레나")); // 이름 전달
            stats.Heal(40); // 95 남음
            stats.TakeDamage(40); // 55 남음
            Assert.That(view.PendingCount, Is.EqualTo(0)); // 다시 안 나옴
            view.Release(); // 전투 종료
            Assert.That(view.IsShowing, Is.False); // 그림 치움
            Assert.That(view.WatchedCount, Is.EqualTo(0)); // 지켜보기 끝
            stats.TakeDamage(40); // 15 남음
            Assert.That(view.PendingCount, Is.EqualTo(0)); // 보고가 들어오지 않음
        }

        private static BattleStats CreateStats(string runtimeId, string characterId, string displayName) // 테스트용 전투 스탯 (체력 100)
        {
            return new BattleStats(runtimeId, characterId, displayName, ProjectH.Data.BattlePosition.Dealer, 1, 100, 30, 5, 1f, 1f, 0f); // 스탯 생성
        }
    }
}
