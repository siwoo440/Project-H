using UnityEngine; // Unity 기본 기능
using UnityEngine.Events; // Unity 버튼 이벤트 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 결과 Overlay 방지
    public sealed class BattleResultOverlay : MonoBehaviour // 24일차 전투 결과 화면
    {
        private static readonly Color BackgroundColor = new Color(0.035f, 0.08f, 0.16f, 0.96f); // 결과 배경 색상
        private static readonly Color PanelColor = new Color(0.93f, 0.91f, 0.84f, 0.99f); // 결과 중앙 패널 색상
        private static readonly Color NavyColor = new Color(0.08f, 0.16f, 0.28f, 1f); // 결과 진한 글자 색상
        private static readonly Color GoldColor = new Color(0.93f, 0.72f, 0.22f, 1f); // 결과 강조 금색
        private static readonly Color HpColor = new Color(0.23f, 0.68f, 0.42f, 1f); // 결과 체력 게이지 색상
        private static readonly Color DownColor = new Color(0.52f, 0.22f, 0.25f, 1f); // 전투 불능 강조 색상
        private Text resultTitle; // 결과 제목 텍스트
        private int renderedMemberCount; // 생성 파티 카드 수
        public static BattleResultOverlay ActiveOverlay { get; private set; } // 현재 활성 결과 Overlay 반환
        public BattleResultData Result { get; private set; } // 현재 표시 결과 데이터 반환
        public int RenderedMemberCount => renderedMemberCount; // 생성 파티 카드 수 반환
        public string TitleText => resultTitle == null ? string.Empty : resultTitle.text; // 현재 결과 제목 반환

        public static BattleResultOverlay ShowRuntime(BattleResultData result, UnityAction returnAction) // 결과 데이터 기반 Runtime 전투 결과 화면 생성
        {
            RemoveActiveOverlay(); // 기존 결과 Overlay 제거
            BattleResultData safeResult = result ?? BattleResultData.Create(BattleOutcome.Defeat, null); // null 결과 안전 보정
            GameObject canvasObject = new GameObject("BattleResultOverlayRuntime", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(BattleResultOverlay)); // 결과 Overlay Canvas 생성
            Canvas canvas = canvasObject.GetComponent<Canvas>(); // 결과 Canvas 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 Overlay 렌더링 설정
            canvas.sortingOrder = 500; // 전투 UI 위 결과 표시 설정
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // 결과 CanvasScaler 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 UI 스케일 설정
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 해상도 설정
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응 방식 설정
            scaler.matchWidthOrHeight = 0.5f; // 가로 세로 중간 스케일 적용
            BattleResultOverlay overlay = canvasObject.GetComponent<BattleResultOverlay>(); // 결과 Overlay 컴포넌트 조회
            overlay.Result = safeResult; // 현재 결과 데이터 연결
            ActiveOverlay = overlay; // 활성 결과 Overlay 저장
            overlay.BuildVisual(returnAction); // 결과 화면 구성
            return overlay; // 생성 결과 Overlay 반환
        }

        public static BattleResultOverlay ShowRuntime(BattleOutcome outcome, UnityAction returnAction) // 기존 승패 기반 결과 Overlay 호환 생성
        {
            BattleResultData result = BattleResultData.Create(outcome, null); // 기존 호출용 최소 결과 데이터 생성
            return ShowRuntime(result, returnAction); // 결과 데이터 기반 Overlay 생성 반환
        }

        private void BuildVisual(UnityAction returnAction) // 현재 결과 데이터 기반 화면 구성
        {
            Image dim = CreateImage(transform, "Dim", BackgroundColor); // 전투 종료 전체 배경 생성
            Stretch(dim.rectTransform); // 전체 배경 화면 확장
            Image panelShadow = CreateImage(transform, "ResultPanelShadow", new Color(0f, 0f, 0f, 0.34f)); // 결과 패널 그림자 생성
            SetRect(panelShadow.rectTransform, new Vector2(0.075f, 0.065f), new Vector2(0.925f, 0.915f)); // 결과 패널 그림자 배치
            Image panel = CreateImage(transform, "ResultPanel", PanelColor); // 결과 메인 패널 생성
            SetRect(panel.rectTransform, new Vector2(0.065f, 0.075f), new Vector2(0.915f, 0.925f)); // 결과 메인 패널 배치
            Image topBand = CreateImage(panel.transform, "TopBand", new Color(0.12f, 0.24f, 0.39f, 1f)); // 상단 결과 띠 생성
            SetRect(topBand.rectTransform, new Vector2(0f, 0.82f), new Vector2(1f, 1f)); // 상단 결과 띠 배치
            Text battleResultLabel = CreateText(topBand.transform, "BattleResultLabel", "BATTLE RESULT", 22, FontStyle.Bold, new Color(0.80f, 0.86f, 0.92f, 1f)); // 결과 보조 제목 생성
            SetRect(battleResultLabel.rectTransform, new Vector2(0.03f, 0.70f), new Vector2(0.30f, 0.96f)); // 결과 보조 제목 배치
            resultTitle = CreateText(topBand.transform, "ResultTitle", Result.Outcome == BattleOutcome.Victory ? "WIN!" : "LOSE", 72, FontStyle.Bold, Color.white); // 승패 메인 제목 생성
            SetRect(resultTitle.rectTransform, new Vector2(0.28f, 0.22f), new Vector2(0.72f, 0.88f)); // 승패 메인 제목 배치
            Text starText = CreateText(topBand.transform, "StarText", Result.StarCount > 0 ? "★  ★  ★" : "☆  ☆  ☆", 36, FontStyle.Bold, GoldColor); // 결과 별 표시 생성
            SetRect(starText.rectTransform, new Vector2(0.70f, 0.22f), new Vector2(0.97f, 0.78f)); // 결과 별 표시 배치
            Image rewardBar = CreateImage(panel.transform, "RewardBar", new Color(0.84f, 0.81f, 0.72f, 1f)); // 보상 요약 바 생성
            SetRect(rewardBar.rectTransform, new Vector2(0.08f, 0.70f), new Vector2(0.92f, 0.80f)); // 보상 요약 바 배치
            Text rewardText = CreateText(rewardBar.transform, "RewardText", $"EXP  +{Result.Experience}        GOLD  +{Result.Gold}", 28, FontStyle.Bold, NavyColor); // 보상 요약 텍스트 생성
            Stretch(rewardText.rectTransform, 8f); // 보상 요약 텍스트 확장
            Text partyLabel = CreateText(panel.transform, "PartyLabel", "PARTY", 21, FontStyle.Bold, NavyColor); // 파티 결과 라벨 생성
            SetRect(partyLabel.rectTransform, new Vector2(0.07f, 0.635f), new Vector2(0.22f, 0.69f)); // 파티 결과 라벨 배치
            BuildPartyCards(panel.transform); // 파티원 결과 카드 구성
            Button returnButton = CreateButton(panel.transform, "NextButton", "다음"); // 다음 버튼 생성
            SetRect(returnButton.GetComponent<RectTransform>(), new Vector2(0.76f, 0.035f), new Vector2(0.93f, 0.12f)); // 다음 버튼 배치

            if (returnAction != null) // 다음 버튼 이벤트 존재 확인
            {
                returnButton.onClick.AddListener(returnAction); // 던전 선택 복귀 이벤트 연결
            }
        }

        private void BuildPartyCards(Transform parent) // 결과 파티 카드 전체 구성
        {
            renderedMemberCount = 0; // 파티 카드 수 초기화

            if (Result == null || Result.Members == null) // 결과 파티 데이터 존재 확인
            {
                return; // 파티 카드 구성 중단
            }

            int visibleCount = Mathf.Min(BattlePartyRuntime.MaxPartySize, Result.Members.Count); // 최대 4인 표시 수 계산

            for (int index = 0; index < visibleCount; index++) // 표시 파티원 순회
            {
                BattleResultPartyMember member = Result.Members[index]; // 현재 결과 파티원 조회

                if (member == null) // 결과 파티원 존재 확인
                {
                    continue; // 빈 파티원 카드 제외
                }

                CreateMemberCard(parent, member, index); // 현재 파티원 결과 카드 생성
                renderedMemberCount++; // 생성 파티 카드 수 증가
            }
        }

        private static void CreateMemberCard(Transform parent, BattleResultPartyMember member, int index) // 단일 파티원 결과 카드 생성
        {
            float cardWidth = 0.205f; // 파티 카드 너비 설정
            float gap = 0.02f; // 파티 카드 간격 설정
            float startX = 0.07f; // 첫 파티 카드 시작 위치 설정
            float minX = startX + ((cardWidth + gap) * index); // 현재 파티 카드 최소 X 계산
            float maxX = minX + cardWidth; // 현재 파티 카드 최대 X 계산
            Image card = CreateImage(parent, $"MemberCard_{index + 1}", new Color(0.98f, 0.97f, 0.92f, 1f)); // 파티원 카드 배경 생성
            SetRect(card.rectTransform, new Vector2(minX, 0.16f), new Vector2(maxX, 0.62f)); // 파티원 카드 배치
            Outline cardOutline = card.gameObject.AddComponent<Outline>(); // 파티원 카드 외곽선 추가
            cardOutline.effectColor = member.IsAlive ? new Color(0.28f, 0.38f, 0.50f, 0.65f) : new Color(0.55f, 0.22f, 0.24f, 0.78f); // 생존 상태별 카드 외곽선 적용
            cardOutline.effectDistance = new Vector2(2f, -2f); // 카드 외곽선 두께 설정
            Image portrait = CreateImage(card.transform, "PortraitPlaceholder", GetPortraitColor(index, member.IsAlive)); // 임시 캐릭터 스탠딩 영역 생성
            SetRect(portrait.rectTransform, new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.91f)); // 임시 캐릭터 영역 배치
            Text portraitLabel = CreateText(portrait.transform, "PortraitLabel", GetPortraitLabel(member.DisplayName), 58, FontStyle.Bold, new Color(1f, 1f, 1f, 0.94f)); // 임시 캐릭터 이름 문자 생성
            Stretch(portraitLabel.rectTransform, 4f); // 임시 캐릭터 문자 확장
            Text nameText = CreateText(card.transform, "Name", member.DisplayName, 24, FontStyle.Bold, NavyColor); // 캐릭터 이름 생성
            SetRect(nameText.rectTransform, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.99f)); // 캐릭터 이름 배치
            Text levelText = CreateText(card.transform, "Level", $"Lv. {member.Level}", 22, FontStyle.Bold, NavyColor); // 캐릭터 레벨 생성
            SetRect(levelText.rectTransform, new Vector2(0.06f, 0.25f), new Vector2(0.94f, 0.35f)); // 캐릭터 레벨 배치
            Image hpBack = CreateImage(card.transform, "HpBack", new Color(0.25f, 0.27f, 0.29f, 0.28f)); // 결과 HP 배경 생성
            SetRect(hpBack.rectTransform, new Vector2(0.08f, 0.16f), new Vector2(0.92f, 0.22f)); // 결과 HP 배경 배치
            Image hpFill = CreateImage(hpBack.transform, "HpFill", member.IsAlive ? HpColor : DownColor); // 결과 HP 채움 생성
            Stretch(hpFill.rectTransform); // 결과 HP 채움 확장
            hpFill.type = Image.Type.Filled; // 결과 HP Filled 타입 설정
            hpFill.fillMethod = Image.FillMethod.Horizontal; // 결과 HP 가로 채움 설정
            hpFill.fillOrigin = 0; // 결과 HP 왼쪽 시작 설정
            hpFill.fillAmount = Mathf.Clamp01(member.HealthRatio); // 결과 HP 비율 적용
            Text hpText = CreateText(card.transform, "HpText", $"HP {member.CurrentHp} / {member.MaxHp}", 18, FontStyle.Bold, NavyColor); // 종료 체력 텍스트 생성
            SetRect(hpText.rectTransform, new Vector2(0.05f, 0.075f), new Vector2(0.95f, 0.15f)); // 종료 체력 텍스트 배치
            Text stateText = CreateText(card.transform, "State", member.IsAlive ? "ALIVE" : "DOWN", 18, FontStyle.Bold, member.IsAlive ? HpColor : DownColor); // 종료 생존 상태 텍스트 생성
            SetRect(stateText.rectTransform, new Vector2(0.05f, 0.005f), new Vector2(0.95f, 0.075f)); // 종료 생존 상태 텍스트 배치
        }

        private static Color GetPortraitColor(int index, bool isAlive) // 임시 캐릭터 영역 색상 반환
        {
            if (!isAlive) // 전투 불능 여부 확인
            {
                return new Color(0.34f, 0.34f, 0.36f, 1f); // 전투 불능 회색 영역 반환
            }

            switch (index % BattlePartyRuntime.MaxPartySize) // 파티 슬롯별 임시 색상 선택
            {
                case 0: // 첫 번째 슬롯 처리
                    return new Color(0.24f, 0.48f, 0.68f, 1f); // 첫 번째 슬롯 청색 반환
                case 1: // 두 번째 슬롯 처리
                    return new Color(0.52f, 0.39f, 0.64f, 1f); // 두 번째 슬롯 보라색 반환
                case 2: // 세 번째 슬롯 처리
                    return new Color(0.33f, 0.58f, 0.45f, 1f); // 세 번째 슬롯 녹색 반환
                default: // 네 번째 슬롯 처리
                    return new Color(0.68f, 0.43f, 0.31f, 1f); // 네 번째 슬롯 주황색 반환
            }
        }

        private static string GetPortraitLabel(string displayName) // 임시 캐릭터 문자 반환
        {
            if (string.IsNullOrEmpty(displayName)) // 표시 이름 존재 확인
            {
                return "?"; // 빈 표시 이름 대체 문자 반환
            }

            return displayName.Substring(0, 1); // 표시 이름 첫 문자 반환
        }

        private static Image CreateImage(Transform parent, string name, Color color) // 공통 결과 이미지 생성
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image)); // UI 이미지 객체 생성
            imageObject.transform.SetParent(parent, false); // UI 이미지 부모 연결
            Image image = imageObject.GetComponent<Image>(); // UI Image 컴포넌트 조회
            image.color = color; // UI 이미지 색상 설정
            image.raycastTarget = true; // 결과 UI 입력 차단 활성화
            return image; // 생성 Image 반환
        }

        private static Text CreateText(Transform parent, string name, string value, int size, FontStyle style, Color color) // 공통 결과 텍스트 생성
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // 결과 Text 객체 생성
            textObject.transform.SetParent(parent, false); // 결과 Text 부모 연결
            Text text = textObject.GetComponent<Text>(); // 결과 Text 컴포넌트 조회
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 적용
            text.text = value; // 결과 Text 내용 설정
            text.fontSize = size; // 결과 Text 크기 설정
            text.fontStyle = style; // 결과 Text 스타일 설정
            text.color = color; // 결과 Text 색상 설정
            text.alignment = TextAnchor.MiddleCenter; // 결과 Text 중앙 정렬
            text.alignByGeometry = true; // 글리프 기준 정렬 적용
            text.resizeTextForBestFit = true; // 자동 Text 크기 적용
            text.resizeTextMinSize = 12; // 최소 Text 크기 설정
            text.resizeTextMaxSize = size; // 최대 Text 크기 설정
            text.raycastTarget = false; // 결과 Text 입력 비활성화
            return text; // 생성 Text 반환
        }

        private static Button CreateButton(Transform parent, string name, string label) // 결과 버튼 생성
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); // 결과 Button 객체 생성
            buttonObject.transform.SetParent(parent, false); // 결과 Button 부모 연결
            Image image = buttonObject.GetComponent<Image>(); // 결과 Button 이미지 조회
            image.color = new Color(0.14f, 0.29f, 0.48f, 1f); // 결과 Button 배경색 설정
            Button button = buttonObject.GetComponent<Button>(); // 결과 Button 컴포넌트 조회
            button.targetGraphic = image; // 결과 Button 대상 그래픽 연결
            Text text = CreateText(buttonObject.transform, "Label", label, 25, FontStyle.Bold, Color.white); // 결과 Button 라벨 생성
            Stretch(text.rectTransform, 6f); // 결과 Button 라벨 전체 확장
            return button; // 생성 Button 반환
        }

        private static void RemoveActiveOverlay() // 기존 활성 결과 Overlay 제거
        {
            if (ActiveOverlay == null) // 기존 활성 Overlay 존재 확인
            {
                return; // 기존 Overlay 제거 불필요 처리
            }

            GameObject activeObject = ActiveOverlay.gameObject; // 기존 Overlay GameObject 저장
            activeObject.SetActive(false); // 기존 Overlay 즉시 화면 비활성화
            ActiveOverlay = null; // 활성 Overlay 참조 우선 초기화

            if (Application.isPlaying) // Play Mode 실행 여부 확인
            {
                UnityEngine.Object.Destroy(activeObject); // Play Mode 기존 Overlay 지연 제거
            }
            else // EditMode 실행 처리
            {
                UnityEngine.Object.DestroyImmediate(activeObject); // EditMode 기존 Overlay 즉시 제거
            }
        }

        private static void Stretch(RectTransform rect) // RectTransform 전체 확장
        {
            rect.anchorMin = Vector2.zero; // 최소 앵커 전체 설정
            rect.anchorMax = Vector2.one; // 최대 앵커 전체 설정
            rect.offsetMin = Vector2.zero; // 최소 오프셋 초기화
            rect.offsetMax = Vector2.zero; // 최대 오프셋 초기화
        }

        private static void Stretch(RectTransform rect, float padding) // RectTransform 내부 여백 확장
        {
            rect.anchorMin = Vector2.zero; // 최소 앵커 전체 설정
            rect.anchorMax = Vector2.one; // 최대 앵커 전체 설정
            rect.offsetMin = new Vector2(padding, padding); // 최소 내부 여백 설정
            rect.offsetMax = new Vector2(-padding, -padding); // 최대 내부 여백 설정
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max) // RectTransform 앵커 배치
        {
            rect.anchorMin = min; // 최소 앵커 설정
            rect.anchorMax = max; // 최대 앵커 설정
            rect.offsetMin = Vector2.zero; // 최소 오프셋 초기화
            rect.offsetMax = Vector2.zero; // 최대 오프셋 초기화
        }

        private void OnDestroy() // 결과 Overlay 제거 처리
        {
            if (ActiveOverlay == this) // 현재 활성 Overlay 제거 여부 확인
            {
                ActiveOverlay = null; // 활성 Overlay 참조 초기화
            }
        }
    }
}
