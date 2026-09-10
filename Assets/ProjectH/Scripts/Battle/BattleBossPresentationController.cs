using System.Collections; // 코루틴 기능
using ProjectH.UI; // Runtime UI 생성 공용 헬퍼 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 보스 연출 컨트롤러 방지
    public sealed class BattleBossPresentationController : MonoBehaviour // 보스 등장 연출 및 상단 체력바 컨트롤러 (Day46)
    {
        private static readonly Color FlashColor = new Color(0.75f, 0.05f, 0.05f, 1f); // 보스 등장 붉은 번쩍임 색상
        private static readonly Color HpFillColor = new Color(0.82f, 0.14f, 0.14f, 1f); // 보스 체력바 채움 색상
        private Image flashImage; // 붉은 번쩍임 전체 화면 이미지
        private Text introText; // 보스 등장 안내 텍스트
        private GameObject healthBarRoot; // 상단 보스 체력바 루트
        private Text bossNameText; // 보스 체력바 이름 텍스트
        private Image bossHpFillImage; // 보스 체력바 채움 이미지
        private Text bossHpText; // 보스 체력바 수치 텍스트
        private BattleEnemyStats boundBoss; // 현재 연결된 보스 전투 스탯
        private Coroutine introRoutine; // 등장 연출 코루틴 참조

        public static BattleBossPresentationController EnsureRuntime() // Battle 씬 보스 연출 컨트롤러 보장
        {
            BattleBossPresentationController existing = FindFirstObjectByType<BattleBossPresentationController>(); // 기존 보스 연출 컨트롤러 조회

            if (existing != null) // 기존 컨트롤러 존재 확인
            {
                return existing; // 기존 컨트롤러 재사용
            }

            GameObject canvasObject = new GameObject("BattleBossPresentationRuntime", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(BattleBossPresentationController)); // 보스 연출 Canvas 생성
            Canvas canvas = canvasObject.GetComponent<Canvas>(); // 보스 연출 Canvas 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 Overlay 렌더링 설정
            canvas.sortingOrder = 450; // 전투 HUD 위 · 결과 Overlay(500) 아래 정렬
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // 보스 연출 CanvasScaler 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 UI 스케일 설정
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 해상도 설정
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응 방식 설정
            scaler.matchWidthOrHeight = 0.5f; // 가로 세로 중간 스케일 적용
            BattleBossPresentationController controller = canvasObject.GetComponent<BattleBossPresentationController>(); // 보스 연출 컨트롤러 조회
            controller.BuildVisual(); // 보스 연출 화면 요소 구성
            return controller; // 생성 컨트롤러 반환
        }

        private void BuildVisual() // 보스 연출 화면 요소 구성
        {
            flashImage = CreateImage(transform, "BossFlash", new Color(FlashColor.r, FlashColor.g, FlashColor.b, 0f)); // 붉은 번쩍임 이미지 생성
            Stretch(flashImage.rectTransform); // 번쩍임 이미지 전체 화면 확장
            flashImage.raycastTarget = false; // 번쩍임 이미지 입력 차단 해제
            introText = CreateText(transform, "BossIntroText", "보스 등장!", 64, FontStyle.Bold, new Color(1f, 1f, 1f, 0f)); // 보스 등장 문구 생성
            SetRect(introText.rectTransform, new Vector2(0.15f, 0.40f), new Vector2(0.85f, 0.60f)); // 보스 등장 문구 화면 중앙 배치
            introText.raycastTarget = false; // 보스 등장 문구 입력 차단 해제
            Outline introOutline = introText.gameObject.AddComponent<Outline>(); // 보스 등장 문구 외곽선 추가
            introOutline.effectColor = new Color(0.25f, 0f, 0f, 0.9f); // 보스 등장 문구 외곽선 색상 설정
            introOutline.effectDistance = new Vector2(2.5f, -2.5f); // 보스 등장 문구 외곽선 두께 설정
            BuildHealthBar(); // 상단 보스 체력바 구성
        }

        private void BuildHealthBar() // 상단 보스 체력바 구성
        {
            healthBarRoot = new GameObject("BossHealthBar", typeof(RectTransform)); // 보스 체력바 루트 생성
            healthBarRoot.transform.SetParent(transform, false); // 보스 체력바 루트 부모 연결
            SetRect((RectTransform)healthBarRoot.transform, new Vector2(0.26f, 0.895f), new Vector2(0.74f, 0.965f)); // 화면 상단 중앙 보스 체력바 배치
            Image back = CreateImage(healthBarRoot.transform, "Back", new Color(0.06f, 0.06f, 0.08f, 0.88f)); // 보스 체력바 배경 생성
            Stretch(back.rectTransform); // 보스 체력바 배경 확장
            bossNameText = CreateText(healthBarRoot.transform, "Name", string.Empty, 19, FontStyle.Bold, Color.white); // 보스 체력바 이름 텍스트 생성
            SetRect(bossNameText.rectTransform, new Vector2(0.02f, 0.52f), new Vector2(0.98f, 0.98f)); // 보스 체력바 이름 텍스트 배치
            Image hpBack = CreateImage(healthBarRoot.transform, "HpBack", new Color(0.22f, 0.05f, 0.05f, 1f)); // 보스 체력 게이지 배경 생성
            SetRect(hpBack.rectTransform, new Vector2(0.03f, 0.16f), new Vector2(0.97f, 0.50f)); // 보스 체력 게이지 배경 배치
            bossHpFillImage = CreateImage(hpBack.transform, "HpFill", HpFillColor); // 보스 체력 게이지 채움 생성
            Stretch(bossHpFillImage.rectTransform); // 보스 체력 게이지 채움 확장 (앵커 기반 채움, Image.Type.Filled는 스프라이트 없이 갱신되지 않아 미사용)
            bossHpText = CreateText(healthBarRoot.transform, "HpText", string.Empty, 15, FontStyle.Bold, Color.white); // 보스 체력바 수치 텍스트 생성
            SetRect(bossHpText.rectTransform, new Vector2(0.02f, 0f), new Vector2(0.98f, 0.15f)); // 보스 체력바 수치 텍스트 배치
            healthBarRoot.SetActive(false); // 초기 보스 체력바 숨김
        }

        public void AnnounceBoss(BattleEnemyStats bossStats) // 보스 등장 연출 및 체력바 연결 시작 (Day46)
        {
            if (bossStats == null) // 보스 전투 스탯 확인
            {
                return; // 보스 연출 시작 중단
            }

            BindHealthBar(bossStats); // 보스 체력바 연결

            if (introRoutine != null) // 기존 등장 연출 진행 확인
            {
                StopCoroutine(introRoutine); // 기존 등장 연출 중단
            }

            introRoutine = StartCoroutine(PlayIntroRoutine(bossStats.DisplayName)); // 보스 등장 연출 시작
        }

        private void BindHealthBar(BattleEnemyStats bossStats) // 보스 체력바 전투 스탯 연결
        {
            UnbindHealthBar(); // 기존 보스 체력바 연결 해제
            boundBoss = bossStats; // 신규 보스 전투 스탯 저장
            boundBoss.HealthChanged += RefreshHealthBar; // 보스 체력 변경 이벤트 연결
            SetText(bossNameText, boundBoss.DisplayName); // 보스 체력바 이름 표시
            healthBarRoot.SetActive(true); // 보스 체력바 표시
            RefreshHealthBar(); // 보스 체력바 초기 상태 표시
        }

        private void UnbindHealthBar() // 기존 보스 체력바 연결 해제
        {
            if (boundBoss != null) // 기존 연결 보스 존재 확인
            {
                boundBoss.HealthChanged -= RefreshHealthBar; // 기존 체력 변경 이벤트 해제
            }

            boundBoss = null; // 연결 보스 참조 초기화
        }

        private void RefreshHealthBar() // 보스 체력바 상태 갱신
        {
            if (boundBoss == null || healthBarRoot == null) // 연결 보스 및 체력바 존재 확인
            {
                return; // 보스 체력바 갱신 중단
            }

            SetFill(bossHpFillImage, boundBoss.HealthRatio); // 보스 체력 게이지 비율 적용
            SetText(bossHpText, boundBoss.IsAlive ? $"{boundBoss.CurrentHp} / {boundBoss.MaxHp}" : "DOWN"); // 보스 체력 수치 또는 처치 표시

            if (!boundBoss.IsAlive) // 보스 전투 불능 여부 확인
            {
                healthBarRoot.SetActive(false); // 처치된 보스 체력바 숨김
                UnbindHealthBar(); // 처치된 보스 연결 해제
            }
        }

        private IEnumerator PlayIntroRoutine(string bossDisplayName) // 보스 등장 연출 코루틴 (붉은 번쩍임 + 중앙 문구)
        {
            introText.text = string.IsNullOrWhiteSpace(bossDisplayName) ? "보스 등장!" : $"{bossDisplayName}\n보스 등장!"; // 보스 등장 문구 구성
            yield return FadeGraphic(flashImage, 0f, 0.55f, 0.08f); // 붉은 화면 빠르게 번쩍임 시작
            yield return FadeGraphic(flashImage, 0.55f, 0.08f, 0.10f); // 붉은 화면 1차 감쇠
            yield return FadeGraphic(flashImage, 0.08f, 0.45f, 0.08f); // 붉은 화면 2차 번쩍임
            StartCoroutine(FadeGraphic(flashImage, 0.45f, 0f, 0.35f)); // 붉은 화면 최종 감쇠 (텍스트와 병행)
            yield return FadeGraphic(introText, 0f, 1f, 0.18f); // 등장 문구 빠르게 표시
            yield return new WaitForSeconds(1.1f); // 등장 문구 유지 대기
            yield return FadeGraphic(introText, 1f, 0f, 0.45f); // 등장 문구 서서히 소멸
            introRoutine = null; // 등장 연출 코루틴 참조 초기화
        }

        private static IEnumerator FadeGraphic(Graphic graphic, float from, float to, float duration) // 그래픽 알파 페이드 공통 처리
        {
            if (graphic == null) // 대상 그래픽 확인
            {
                yield break; // 페이드 처리 중단
            }

            float elapsed = 0f; // 경과 시간 초기화
            Color baseColor = graphic.color; // 기존 색상 조회

            while (elapsed < duration) // 페이드 진행 반복
            {
                elapsed += Time.deltaTime; // 경과 시간 누적
                float ratio = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration); // 진행 비율 계산
                float alpha = Mathf.Lerp(from, to, ratio); // 현재 알파 계산
                graphic.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha); // 알파 적용
                yield return null; // 다음 프레임 대기
            }

            graphic.color = new Color(baseColor.r, baseColor.g, baseColor.b, to); // 최종 알파 확정 적용
        }

        private static Image CreateImage(Transform parent, string name, Color color) => RuntimeUiKit.CreateImage(parent, name, color); // 공통 보스 연출 이미지 생성 (RuntimeUiKit 위임, 최적화 정리)

        private static Text CreateText(Transform parent, string name, string value, int size, FontStyle style, Color color) // 공통 보스 연출 텍스트 생성
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // UI 텍스트 객체 생성
            textObject.transform.SetParent(parent, false); // UI 텍스트 부모 연결
            Text text = textObject.GetComponent<Text>(); // UI Text 컴포넌트 조회
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 적용
            text.text = value; // UI 텍스트 값 적용
            text.fontSize = size; // UI 텍스트 크기 적용
            text.fontStyle = style; // UI 텍스트 스타일 적용
            text.color = color; // UI 텍스트 색상 적용
            text.alignment = TextAnchor.MiddleCenter; // UI 텍스트 중앙 정렬
            text.resizeTextForBestFit = true; // 글자 자동 크기 활성화
            text.resizeTextMinSize = 12; // 최소 글자 크기 설정
            text.resizeTextMaxSize = size; // 최대 글자 크기 설정
            text.raycastTarget = false; // UI 텍스트 입력 비활성화
            return text; // 생성 UI 텍스트 반환
        }

        private static void SetFill(Image target, float ratio) // 가로 게이지 비율 설정 (앵커 기반, 기존 BattleHudCardView/BattleEnemyView와 동일 방식)
        {
            if (target == null) // 게이지 이미지 확인
            {
                return; // 게이지 설정 중단
            }

            RectTransform rect = target.rectTransform; // 게이지 RectTransform 조회
            rect.anchorMin = Vector2.zero; // 게이지 최소 앵커 고정
            rect.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f); // 게이지 비율 기반 최대 앵커 적용
            rect.offsetMin = Vector2.zero; // 게이지 최소 오프셋 초기화
            rect.offsetMax = Vector2.zero; // 게이지 최대 오프셋 초기화
        }

        private static void SetText(Text target, string value) // 텍스트 안전 설정
        {
            if (target != null) // 텍스트 참조 확인
            {
                target.text = value; // 텍스트 값 적용
            }
        }

        private static void Stretch(RectTransform rect) => RuntimeUiKit.Stretch(rect); // RectTransform 전체 확장 (RuntimeUiKit 위임, 최적화 정리)

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max) => RuntimeUiKit.SetRect(rect, min, max); // RectTransform 앵커 배치 (RuntimeUiKit 위임, 최적화 정리)

        private void OnDestroy() // 보스 연출 컨트롤러 제거 처리
        {
            UnbindHealthBar(); // 보스 체력바 연결 해제
        }
    }
}
