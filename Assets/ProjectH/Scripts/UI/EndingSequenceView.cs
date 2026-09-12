using System; // 콜백 자료형
using System.Collections; // 코루틴 기능
using ProjectH.Core; // 회차 기록 기능
using ProjectH.Dialogue; // 대사 파일 기능
using ProjectH.SaveSystem; // 저장 기능
using ProjectH.Story; // 엔딩 정의 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 중복 생성 방지
    public sealed class EndingSequenceView : MonoBehaviour // 엔딩 연출 (Day72 신규 — 암전 → 엔딩 대사 → 제목 카드 → 크레딧 → 타이틀)
    {
        private static readonly Color TitleColor = new Color(0.95f, 0.86f, 0.56f, 1f); // 엔딩 제목 금색
        private static readonly Color BodyColor = new Color(0.88f, 0.90f, 0.96f, 1f); // 본문 글자
        private static readonly Color HintColor = new Color(0.72f, 0.75f, 0.82f, 1f); // 안내 글자
        private static readonly Color ButtonColor = new Color(0.30f, 0.36f, 0.48f, 1f); // 버튼

        private EndingDefinition ending; // 재생할 엔딩
        private Action onFinished; // 끝났을 때 알림
        private Image fade; // 암전 막
        private RectTransform card; // 제목 카드 영역
        private bool isNewEnding; // 처음 본 엔딩인지

        public static EndingSequenceView Play(EndingDefinition definition, Action finished) // 엔딩 재생
        {
            GameObject root = new GameObject("EndingSequenceView", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 런타임 Canvas
            Canvas canvas = root.GetComponent<Canvas>(); // Canvas 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // Overlay 렌더링
            canvas.sortingOrder = 800; // 모든 화면보다 위
            CanvasScaler scaler = root.GetComponent<CanvasScaler>(); // 스케일러 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반
            scaler.referenceResolution = new Vector2(1600f, 900f); // 기준 해상도
            EndingSequenceView view = root.AddComponent<EndingSequenceView>(); // 컴포넌트 추가
            view.ending = definition; // 엔딩 저장
            view.onFinished = finished; // 콜백 저장
            view.Build(); // 화면 구성
            view.StartCoroutine(view.Run()); // 연출 시작
            return view; // 반환
        }

        private void Build() // 화면 뼈대 (별하늘 + 암전 막 + 카드 영역)
        {
            Image sky = RuntimeUiKit.CreateImage(transform, "Sky", Color.white); // 밤하늘 (71일차 우주 배경 재사용)
            sky.sprite = StarfieldArt.GetBackground(); // 별·성운 그림
            sky.preserveAspect = false; // 화면 채움
            RuntimeUiKit.Stretch(sky.rectTransform); // 전체
            Image cardImage = RuntimeUiKit.CreateImage(transform, "Card", new Color(0f, 0f, 0f, 0f)); // 카드 영역 (투명)
            RuntimeUiKit.Stretch(cardImage.rectTransform); // 전체
            card = cardImage.rectTransform; // 보관
            fade = RuntimeUiKit.CreateImage(transform, "Fade", Color.black); // 암전 막 (맨 위)
            RuntimeUiKit.Stretch(fade.rectTransform); // 전체
        }

        private IEnumerator Run() // 엔딩 순서 진행
        {
            yield return FadeTo(1f, 0.8f); // 암전
            yield return new WaitForSeconds(0.6f); // 정적
            yield return FadeTo(0f, 1.2f); // 밝아짐 (별하늘)
            bool finished = false; // 대사 종료 여부
            DialogueScript script = DialogueLibrary.Load(ending.ScriptId); // 엔딩 대사

            if (script != null) // 대사 파일 확인
            {
                DialogueOverlayView.Open(script, _ => finished = true); // 엔딩 대사 재생
                while (!finished) yield return null; // 끝까지 대기
            }

            yield return FadeTo(1f, 1.0f); // 다시 암전
            isNewEnding = PlayerProfile.RecordEnding(ending.Id); // 엔딩 기록 (회차 수 증가)
            BuildTitleCard(); // 제목 카드 만들기
            yield return FadeTo(0f, 1.4f); // 제목 카드 나타남
        }

        private void BuildTitleCard() // 엔딩 제목 카드 + 크레딧 + 타이틀 버튼
        {
            Text badge = RuntimeUiKit.CreateText(card, "Badge", isNewEnding ? "NEW ENDING" : "ENDING", 22, HintColor, FontStyle.Bold); // 새 엔딩 표시
            RuntimeUiKit.SetRect(badge.rectTransform, new Vector2(0.1f, 0.70f), new Vector2(0.9f, 0.76f)); // 위
            Text title = RuntimeUiKit.CreateText(card, "Title", ending.Title, 58, TitleColor, FontStyle.Bold).Outlined(new Color(0f, 0f, 0f, 0.85f), new Vector2(2f, -2f)); // 엔딩 제목
            RuntimeUiKit.SetRect(title.rectTransform, new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.70f)); // 가운데 위
            Text summary = RuntimeUiKit.CreateText(card, "Summary", ending.Summary, 20, BodyColor, FontStyle.Normal).Wrap(); // 한 줄 요약
            RuntimeUiKit.SetRect(summary.rectTransform, new Vector2(0.16f, 0.44f), new Vector2(0.84f, 0.57f)); // 제목 아래
            string progress = $"{PlayerProfile.ClearCount}회차 완료   ·   본 엔딩 {PlayerProfile.SeenEndingCount} / {EndingCatalog.All.Count}"; // 진행 기록
            Text record = RuntimeUiKit.CreateText(card, "Record", progress, 20, TitleColor, FontStyle.Bold); // 기록 표시
            RuntimeUiKit.SetRect(record.rectTransform, new Vector2(0.1f, 0.36f), new Vector2(0.9f, 0.42f)); // 요약 아래
            Text credits = RuntimeUiKit.CreateText(card, "Credits", "프로젝트 H\n기획 · 개발 · 시나리오   siwoo440\n\n함께해 주셔서 고맙습니다.", 17, HintColor, FontStyle.Normal).Wrap(); // 간단 크레딧
            RuntimeUiKit.SetRect(credits.rectTransform, new Vector2(0.2f, 0.18f), new Vector2(0.8f, 0.33f)); // 아래
            Button toTitle = RuntimeUiKit.CreateButton(card, "ToTitle", ButtonColor); // 타이틀 버튼
            RuntimeUiKit.SetRect(toTitle.GetComponent<RectTransform>(), new Vector2(0.36f, 0.07f), new Vector2(0.64f, 0.14f)); // 아래
            RuntimeUiKit.Stretch(RuntimeUiKit.CreateText(toTitle.transform, "Label", "타이틀로", 24, Color.white, FontStyle.Bold).rectTransform); // 글자
            toTitle.onClick.AddListener(Finish); // 연결
            Text hint = RuntimeUiKit.CreateText(card, "Hint", "다른 선택을 하면 다른 결말을 볼 수 있습니다. 엔딩 기록과 설정은 새로 시작해도 남습니다.", 15, HintColor, FontStyle.Normal).Wrap(); // 안내
            RuntimeUiKit.SetRect(hint.rectTransform, new Vector2(0.12f, 0.01f), new Vector2(0.88f, 0.06f)); // 맨 아래
        }

        private IEnumerator FadeTo(float target, float duration) // 암전 막 투명도 바꾸기
        {
            Color color = fade.color; // 현재 색
            float from = color.a; // 시작 투명도
            float elapsed = 0f; // 경과 시간
            fade.raycastTarget = target > 0.5f; // 암전 중에는 뒤 클릭 차단

            while (elapsed < duration) // 시간 동안
            {
                elapsed += Time.deltaTime; // 시간 누적
                color.a = Mathf.Lerp(from, target, Mathf.Clamp01(elapsed / duration)); // 투명도 계산
                fade.color = color; // 반영
                yield return null; // 다음 프레임
            }

            color.a = target; // 값 확정
            fade.color = color; // 반영
            fade.raycastTarget = target > 0.5f; // 입력 차단 갱신
        }

        private void Finish() // 엔딩 종료 : 타이틀로
        {
            Action callback = onFinished; // 콜백 복사
            onFinished = null; // 중복 방지
            Destroy(gameObject); // 화면 닫기
            callback?.Invoke(); // 알림
        }
    }
}
