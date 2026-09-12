using System; // 콜백·난수 자료형
using System.Collections; // 코루틴 기능
using System.Collections.Generic; // 목록 자료형
using ProjectH.Data; // 아이템 데이터 기능
using ProjectH.Minigame; // 놀이판 정의·진행 기능
using ProjectH.SaveSystem; // 저장·골드 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 중복 생성 방지
    public sealed class MinigameView : MonoBehaviour // 마을 시장 야시장 놀이판 화면 (Day70 신규 — 카드·다트·야바위·내기·마차 5종)
    {
        private static readonly Color BoxColor = new Color(0.09f, 0.08f, 0.11f, 0.98f); // 놀이판 배경
        private static readonly Color CardBack = new Color(0.24f, 0.20f, 0.32f, 1f); // 카드 뒷면
        private static readonly Color CardFace = new Color(0.86f, 0.82f, 0.70f, 1f); // 카드 앞면
        private static readonly Color AccentColor = new Color(0.82f, 0.58f, 0.24f, 1f); // 강조 주황
        private static readonly Color ActionColor = new Color(0.30f, 0.46f, 0.66f, 1f); // 행동 파랑
        private static readonly Color SubColor = new Color(0.24f, 0.26f, 0.32f, 1f); // 보조 회색
        private static readonly Color HintColor = new Color(0.80f, 0.82f, 0.88f, 1f); // 안내 글자
        private static readonly Color GoodColor = new Color(0.38f, 0.68f, 0.44f, 1f); // 성공 초록
        private static readonly Color BadColor = new Color(0.70f, 0.32f, 0.32f, 1f); // 실패 빨강
        private static readonly string[] CardFaces = { "★", "●", "▲", "◆", "■", "✦" }; // 카드 그림 6종

        private SaveData saveData; // 현재 저장
        private DataManager dataManager; // 아이템 데이터
        private Action<string> onClosed; // 닫을 때 알림 (저장·안내용)
        private RectTransform body; // 내용 영역
        private Text headerText; // 제목 줄
        private Text statusText; // 안내 줄
        private readonly List<GameObject> bodyItems = new List<GameObject>(); // 내용 요소 목록
        private readonly System.Random random = new System.Random(); // 놀이 진행 난수
        private Coroutine routine; // 진행 중 연출
        private int bet = MinigameCatalog.MinBet; // 건 골드
        private string lastMessage = string.Empty; // 마지막 결과 안내

        public static MinigameView Open(SaveData save, DataManager data, Action<string> closed) // 놀이판 열기
        {
            GameObject root = new GameObject("MinigameView", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 런타임 Canvas
            Canvas canvas = root.GetComponent<Canvas>(); // Canvas 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // Overlay 렌더링
            canvas.sortingOrder = 650; // 대화 화면(600)보다 위
            CanvasScaler scaler = root.GetComponent<CanvasScaler>(); // 스케일러 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반
            scaler.referenceResolution = new Vector2(1600f, 900f); // 기준 해상도
            MinigameView view = root.AddComponent<MinigameView>(); // 컴포넌트 추가
            view.saveData = save; // 저장 연결
            view.dataManager = data; // 데이터 연결
            view.onClosed = closed; // 콜백 저장
            view.Build(); // 화면 구성
            view.ShowSelect(); // 놀이 고르기
            return view; // 반환
        }

        private void Build() // 화면 뼈대 (어두운 막 + 가운데 상자 + 제목 · 내용 · 안내)
        {
            Image dim = RuntimeUiKit.CreateImage(transform, "Dim", new Color(0f, 0f, 0f, 0.82f)); // 어두운 막
            RuntimeUiKit.Stretch(dim.rectTransform); // 전체
            Image box = RuntimeUiKit.CreateImage(transform, "Box", BoxColor); // 놀이판 상자
            RuntimeUiKit.SetRect(box.rectTransform, new Vector2(0.14f, 0.10f), new Vector2(0.86f, 0.92f)); // 가운데
            box.gameObject.AddComponent<Outline>().effectColor = new Color(0.86f, 0.66f, 0.30f, 0.85f); // 금색 테두리
            headerText = RuntimeUiKit.CreateText(box.transform, "Header", "야시장 놀이판", 30, Color.white, FontStyle.Bold); // 제목
            RuntimeUiKit.SetRect(headerText.rectTransform, new Vector2(0.04f, 0.90f), new Vector2(0.80f, 0.98f)); // 위
            headerText.alignment = TextAnchor.MiddleLeft; // 왼쪽 정렬
            Button close = RuntimeUiKit.CreateButton(box.transform, "Close", SubColor); // 닫기 버튼
            RuntimeUiKit.SetRect(close.GetComponent<RectTransform>(), new Vector2(0.82f, 0.90f), new Vector2(0.97f, 0.98f)); // 오른쪽 위
            RuntimeUiKit.Stretch(RuntimeUiKit.CreateText(close.transform, "Label", "닫기", 20, Color.white, FontStyle.Bold).rectTransform); // 버튼 글자
            close.onClick.AddListener(Close); // 닫기 연결
            Image bodyImage = RuntimeUiKit.CreateImage(box.transform, "Body", new Color(0f, 0f, 0f, 0f)); // 내용 영역 (투명)
            RuntimeUiKit.SetRect(bodyImage.rectTransform, new Vector2(0.04f, 0.11f), new Vector2(0.97f, 0.88f)); // 가운데
            body = bodyImage.rectTransform; // 영역 저장
            statusText = RuntimeUiKit.CreateText(box.transform, "Status", string.Empty, 18, HintColor, FontStyle.Normal).Wrap(); // 안내 줄
            RuntimeUiKit.SetRect(statusText.rectTransform, new Vector2(0.04f, 0.02f), new Vector2(0.97f, 0.10f)); // 아래
            statusText.alignment = TextAnchor.MiddleLeft; // 왼쪽 정렬
        }

        private void ClearBody() // 내용 비우기 (연출도 중단)
        {
            if (routine != null) StopCoroutine(routine); // 연출 중단
            routine = null; // 초기화
            foreach (GameObject item in bodyItems) Destroy(item); // 이전 요소 제거
            bodyItems.Clear(); // 목록 비움
        }

        private void SetStatus(string text) // 안내 줄 갱신
        {
            if (statusText != null) statusText.text = text ?? string.Empty; // 글자 반영
        }

        private void Close() // 닫기 : 결과 안내 전달
        {
            Action<string> callback = onClosed; // 콜백 복사
            onClosed = null; // 중복 방지
            Destroy(gameObject); // 화면 닫기
            callback?.Invoke(lastMessage); // 안내 전달
        }

        // ───────────────────────── 놀이 고르기 ─────────────────────────

        private void ShowSelect() // 놀이 5종 목록 + 배팅 조절
        {
            ClearBody(); // 내용 비우기
            headerText.text = $"야시장 놀이판   ·   {MinigameService.GetRemainingText(saveData)}   ·   보유 {GoldCurrencyService.GetGold(saveData)}G"; // 제목
            Text betLabel = AddLabel($"거는 돈 : {bet}G", 1f, 0.07f, 22, AccentColor, FontStyle.Bold); // 배팅 표시
            RuntimeUiKit.SetRect(betLabel.rectTransform, new Vector2(0f, 0.93f), new Vector2(0.58f, 1f)); // 왼쪽만 사용
            betLabel.alignment = TextAnchor.MiddleLeft; // 왼쪽 정렬
            AddButton("-100", 1f, 0.07f, 0.62f, 0.74f, SubColor, bet > MinigameCatalog.MinBet, () => ChangeBet(-MinigameCatalog.BetStep)); // 줄이기
            AddButton("+100", 1f, 0.07f, 0.76f, 0.88f, SubColor, bet < MinigameCatalog.MaxBet, () => ChangeBet(MinigameCatalog.BetStep)); // 늘리기
            float top = 0.90f; // 첫 줄 위치

            foreach (MinigameDefinition definition in MinigameCatalog.All) // 놀이 5종 순회
            {
                MinigameKind kind = definition.Kind; // 클릭용 복사
                bool unlocked = MinigameService.IsUnlocked(saveData, kind); // 해금 여부
                bool canPlay = MinigameService.CanPlay(saveData, kind, bet, out string reason); // 시작 가능 여부
                string cost = definition.UsesBet ? $"{bet}G 걸기" : "골드 없이 · 재료 보상"; // 비용 표시
                string label = unlocked ? $"{definition.Name}  [{definition.SkillLabel}]   ·   {cost}" : $"{definition.Name}  ·  잠김"; // 버튼 글자
                string hint = !unlocked ? definition.UnlockText : canPlay ? definition.Rule : $"{definition.Rule}  ({reason})"; // 설명 (불가 사유 포함)
                AddButton(label, top, 0.085f, 0f, 1f, unlocked ? ActionColor : SubColor, canPlay, () => StartGame(kind)); // 놀이 버튼
                AddLabel(hint, top - 0.088f, 0.07f, 15, canPlay || !unlocked ? HintColor : BadColor, FontStyle.Normal); // 규칙 설명
                top -= 0.175f; // 다음 줄
            }

            SetStatus(string.IsNullOrEmpty(lastMessage) ? "카드는 기억, 다트는 반사, 야바위는 관찰, 내기는 배짱, 마차는 계산입니다." : lastMessage); // 안내
        }

        private void ChangeBet(int delta) // 배팅 금액 조절
        {
            bet = MinigameCatalog.ClampBet(bet + delta); // 범위 보정
            ShowSelect(); // 다시 그리기
        }

        private void StartGame(MinigameKind kind) // 놀이 시작
        {
            if (!MinigameService.CanPlay(saveData, kind, bet, out string reason)) // 조건 재확인
            {
                SetStatus(reason); // 안내
                return; // 중단
            }

            ClearBody(); // 내용 비우기
            MinigameDefinition definition = MinigameCatalog.Get(kind); // 정의
            headerText.text = $"{definition.Name}   ·   {(definition.UsesBet ? $"{bet}G 걸었습니다" : "삯 : 재료")}"; // 제목

            switch (kind) // 종류 분기
            {
                case MinigameKind.CardMatch: BuildCardMatch(); break; // 카드 뒤집기
                case MinigameKind.Dart: BuildDart(); break; // 다트
                case MinigameKind.ShellGame: BuildShellGame(); break; // 야바위
                case MinigameKind.HighLow: BuildHighLow(); break; // 상인의 내기
                default: BuildCargoBalance(); break; // 마차 균형
            }
        }

        private void Settle(MinigameKind kind, int score, bool busted) // 결과 반영 후 고르기 화면으로
        {
            MinigameResult result = MinigameService.Settle(saveData, dataManager, kind, bet, score, busted); // 보상 반영
            lastMessage = result.Message; // 안내 저장
            ShowSelect(); // 고르기 화면
        }

        // ───────────────────────── 1. 카드 뒤집기 (기억) ─────────────────────────

        private void BuildCardMatch() // 12장을 보여 준 뒤 뒤집어 같은 그림 찾기
        {
            int[] faces = new int[MinigameCatalog.CardPairCount * 2]; // 카드 그림 배열
            for (int index = 0; index < faces.Length; index++) faces[index] = index / 2; // 짝 채우기
            Shuffle(faces); // 섞기
            Button[] cards = new Button[faces.Length]; // 카드 버튼
            Text[] labels = new Text[faces.Length]; // 카드 글자
            bool[] opened = new bool[faces.Length]; // 열린 카드
            bool[] matched = new bool[faces.Length]; // 맞춘 카드

            for (int index = 0; index < faces.Length; index++) // 4×3 배치
            {
                int column = index % 4; // 열
                int row = index / 4; // 행
                Button card = AddButton(string.Empty, 0.92f - (row * 0.31f), 0.28f, 0.04f + (column * 0.24f), 0.26f + (column * 0.24f), CardBack, false, null); // 카드
                cards[index] = card; // 보관
                labels[index] = RuntimeUiKit.CreateText(card.transform, "Face", CardFaces[faces[index]], 46, new Color(0.20f, 0.18f, 0.24f, 1f), FontStyle.Bold); // 그림
                RuntimeUiKit.Stretch(labels[index].rectTransform); // 카드 채움
            }

            routine = StartCoroutine(CardMatchRoutine(faces, cards, labels, opened, matched)); // 진행 시작
        }

        private IEnumerator CardMatchRoutine(int[] faces, Button[] cards, Text[] labels, bool[] opened, bool[] matched) // 카드 뒤집기 진행
        {
            for (int index = 0; index < cards.Length; index++) SetCardFace(cards[index], labels[index], true); // 전부 보여 주기
            SetStatus("잘 기억하세요… 3초 뒤에 뒤집습니다."); // 안내
            yield return new WaitForSeconds(3f); // 암기 시간
            for (int index = 0; index < cards.Length; index++) SetCardFace(cards[index], labels[index], false); // 전부 뒤집기
            int pairs = 0; // 맞춘 짝
            int misses = 0; // 실패 횟수
            int first = -1; // 먼저 고른 카드
            int picked = -1; // 이번에 누른 카드
            for (int index = 0; index < cards.Length; index++) { int captured = index; cards[index].interactable = true; cards[index].onClick.AddListener(() => picked = captured); } // 클릭 연결

            while (pairs < MinigameCatalog.CardPairCount && misses < MinigameCatalog.CardMissLimit) // 끝날 때까지
            {
                SetStatus($"맞춘 짝 {pairs}/{MinigameCatalog.CardPairCount} · 실패 {misses}/{MinigameCatalog.CardMissLimit}"); // 진행 안내
                if (picked < 0 || opened[picked] || matched[picked]) { picked = -1; yield return null; continue; } // 유효한 클릭 대기
                int current = picked; // 선택 카드
                picked = -1; // 입력 비우기
                opened[current] = true; // 열림 표시
                SetCardFace(cards[current], labels[current], true); // 앞면

                if (first < 0) { first = current; continue; } // 첫 장 기억

                if (faces[first] == faces[current]) // 짝 성공
                {
                    matched[first] = true; // 고정
                    matched[current] = true; // 고정
                    cards[first].interactable = false; // 잠금
                    cards[current].interactable = false; // 잠금
                    pairs++; // 짝 증가
                }
                else // 짝 실패
                {
                    misses++; // 실패 증가
                    SetStatus($"땡! 실패 {misses}/{MinigameCatalog.CardMissLimit}"); // 안내
                    yield return new WaitForSeconds(0.8f); // 보여 주는 시간
                    SetCardFace(cards[first], labels[first], false); // 다시 뒤집기
                    SetCardFace(cards[current], labels[current], false); // 다시 뒤집기
                    opened[first] = false; // 닫힘
                    opened[current] = false; // 닫힘
                }

                first = -1; // 다음 차례
            }

            SetStatus($"끝! 맞춘 짝 {pairs}쌍"); // 결과 안내
            yield return new WaitForSeconds(0.8f); // 여운
            Settle(MinigameKind.CardMatch, pairs, false); // 보상 반영
        }

        private static void SetCardFace(Button card, Text label, bool open) // 카드 앞면·뒷면 전환
        {
            card.image.color = open ? CardFace : CardBack; // 배경색
            label.enabled = open; // 그림 표시
        }

        // ───────────────────────── 2. 다트 (반사) ─────────────────────────

        private void BuildDart() // 좌우로 움직이는 조준선을 과녁 가운데에서 멈추기
        {
            AddLabel("조준선이 과녁 한가운데(금색)를 지날 때 '던지기'를 누르세요.", 0.92f, 0.07f, 18, HintColor, FontStyle.Normal); // 안내
            Image outer = AddImage(0.78f, 0.30f, 0.10f, 0.90f, new Color(0.26f, 0.28f, 0.34f, 1f)); // 과녁 바깥 (10점)
            AddImage(0.70f, 0.14f, 0.34f, 0.66f, new Color(0.44f, 0.40f, 0.52f, 1f)); // 과녁 중간 (25점)
            AddImage(0.66f, 0.06f, 0.46f, 0.54f, AccentColor); // 과녁 한가운데 (50점)
            Image marker = AddImage(0.80f, 0.34f, 0.10f, 0.115f, Color.white); // 조준선
            Button throwButton = AddButton("던지기", 0.34f, 0.10f, 0.28f, 0.72f, ActionColor, true, null); // 던지기 버튼
            routine = StartCoroutine(DartRoutine(outer.rectTransform, marker.rectTransform, throwButton)); // 진행 시작
        }

        private IEnumerator DartRoutine(RectTransform track, RectTransform marker, Button throwButton) // 다트 진행 (3발 · 점점 빨라짐)
        {
            float[] speeds = { 1.15f, 1.55f, 2.05f }; // 발마다 속도 (화면 비율/초)
            int total = 0; // 점수 합
            bool pressed = false; // 던지기 입력
            throwButton.onClick.AddListener(() => pressed = true); // 입력 연결

            for (int shot = 0; shot < MinigameCatalog.DartThrowCount; shot++) // 3발
            {
                float position = 0f; // 조준선 위치 (0~1)
                float direction = 1f; // 진행 방향
                pressed = false; // 입력 비우기
                SetStatus($"{shot + 1}번째 발 · 지금까지 {total}점"); // 안내

                while (!pressed) // 누를 때까지 왕복
                {
                    position += direction * speeds[shot] * Time.deltaTime; // 이동
                    if (position >= 1f) { position = 1f; direction = -1f; } // 오른쪽 끝에서 반환
                    if (position <= 0f) { position = 0f; direction = 1f; } // 왼쪽 끝에서 반환
                    marker.anchorMin = new Vector2(Mathf.Lerp(0.10f, 0.885f, position), marker.anchorMin.y); // 위치 반영
                    marker.anchorMax = new Vector2(marker.anchorMin.x + 0.015f, marker.anchorMax.y); // 폭 유지
                    yield return null; // 다음 프레임
                }

                float offset = Mathf.Abs(position - 0.5f); // 가운데에서 벗어난 정도
                int score = offset <= 0.08f ? 50 : offset <= 0.22f ? 25 : 10; // 점수 판정
                total += score; // 합산
                SetStatus($"{score}점! · 합계 {total}점"); // 안내
                yield return new WaitForSeconds(0.7f); // 여운
            }

            throwButton.interactable = false; // 입력 잠금
            SetStatus($"세 발 합계 {total}점"); // 결과 안내
            yield return new WaitForSeconds(0.6f); // 여운
            Settle(MinigameKind.Dart, total, false); // 보상 반영
        }

        // ───────────────────────── 3. 야바위 (관찰) ─────────────────────────

        private void BuildShellGame() // 컵 3개를 섞은 뒤 구슬이 든 컵 고르기
        {
            AddLabel("구슬이 든 컵을 눈으로 쫓으세요. 판이 올라갈수록 빨라집니다.", 0.92f, 0.07f, 18, HintColor, FontStyle.Normal); // 안내
            Button[] cups = new Button[3]; // 컵 3개
            Text[] marks = new Text[3]; // 구슬 표시

            for (int index = 0; index < cups.Length; index++) // 컵 배치
            {
                cups[index] = AddButton(string.Empty, 0.70f, 0.34f, 0.08f + (index * 0.30f), 0.32f + (index * 0.30f), CardBack, false, null); // 컵
                RuntimeUiKit.Stretch(RuntimeUiKit.CreateText(cups[index].transform, "Cup", "▽", 60, new Color(0.86f, 0.80f, 0.66f, 1f), FontStyle.Bold).rectTransform); // 컵 모양
                marks[index] = RuntimeUiKit.CreateText(cups[index].transform, "Ball", "●", 34, AccentColor, FontStyle.Bold); // 구슬
                RuntimeUiKit.SetRect(marks[index].rectTransform, new Vector2(0.3f, 0.02f), new Vector2(0.7f, 0.26f)); // 컵 아래
                marks[index].enabled = false; // 평소에는 숨김
            }

            routine = StartCoroutine(ShellGameRoutine(cups, marks)); // 진행 시작
        }

        private IEnumerator ShellGameRoutine(Button[] cups, Text[] marks) // 야바위 진행 (3판 · 실패해도 직전 배당 절반)
        {
            int[] swapCounts = { 5, 8, 12 }; // 판별 섞는 횟수
            float[] swapTimes = { 0.34f, 0.24f, 0.16f }; // 판별 한 번 바꾸는 시간
            int cleared = 0; // 성공한 판
            int picked = -1; // 고른 컵
            for (int index = 0; index < cups.Length; index++) { int captured = index; cups[index].onClick.AddListener(() => picked = captured); } // 클릭 연결

            for (int round = 0; round < MinigameCatalog.ShellRoundCount; round++) // 3판
            {
                int[] order = { 0, 1, 2 }; // 자리별 컵 번호
                int ball = random.Next(3); // 구슬이 든 컵
                for (int index = 0; index < cups.Length; index++) { cups[index].interactable = false; marks[index].enabled = index == ball; } // 시작 위치 공개
                SetStatus($"{round + 1}판 · 구슬은 여기 있습니다."); // 안내
                yield return new WaitForSeconds(1.2f); // 보여 주는 시간
                for (int index = 0; index < cups.Length; index++) marks[index].enabled = false; // 구슬 숨기기

                for (int swap = 0; swap < swapCounts[round]; swap++) // 섞기
                {
                    int left = random.Next(3); // 자리 A
                    int right = (left + 1 + random.Next(2)) % 3; // 자리 B (다른 자리)
                    SetStatus($"{round + 1}판 · 섞는 중… ({swap + 1}/{swapCounts[round]})"); // 안내
                    yield return SwapCups(cups, order, left, right, swapTimes[round]); // 자리 바꾸기
                }

                SetStatus($"{round + 1}판 · 구슬이 든 컵을 고르세요."); // 안내
                picked = -1; // 입력 비우기
                for (int index = 0; index < cups.Length; index++) cups[index].interactable = true; // 입력 허용
                while (picked < 0) yield return null; // 선택 대기
                for (int index = 0; index < cups.Length; index++) { cups[index].interactable = false; marks[index].enabled = index == ball; } // 정답 공개
                bool correct = picked == ball; // 정답 여부

                if (!correct) // 틀림
                {
                    SetStatus($"아쉽네요! 구슬은 다른 컵에 있었습니다. 성공 {cleared}판 · 직전 배당의 절반만 돌려받습니다."); // 안내
                    yield return new WaitForSeconds(1.4f); // 여운
                    Settle(MinigameKind.ShellGame, cleared, true); // 절반 보상
                    yield break; // 종료
                }

                cleared++; // 성공 증가
                SetStatus($"정답! {cleared}판 성공"); // 안내
                yield return new WaitForSeconds(1.2f); // 여운
            }

            SetStatus("3판 전부 성공! 배당 5배입니다."); // 결과 안내
            yield return new WaitForSeconds(0.8f); // 여운
            Settle(MinigameKind.ShellGame, cleared, false); // 전액 보상
        }

        private IEnumerator SwapCups(Button[] cups, int[] order, int slotA, int slotB, float duration) // 두 자리의 컵을 서로 옮기는 연출
        {
            if (slotA == slotB) yield break; // 같은 자리 무시
            RectTransform first = (RectTransform)cups[order[slotA]].transform; // 컵 A
            RectTransform second = (RectTransform)cups[order[slotB]].transform; // 컵 B
            float fromA = SlotAnchor(slotA); // 시작 위치 A
            float fromB = SlotAnchor(slotB); // 시작 위치 B
            float elapsed = 0f; // 경과 시간

            while (elapsed < duration) // 이동
            {
                elapsed += Time.deltaTime; // 시간 누적
                float t = Mathf.Clamp01(elapsed / duration); // 진행률
                MoveCup(first, Mathf.Lerp(fromA, fromB, t)); // 컵 A 이동
                MoveCup(second, Mathf.Lerp(fromB, fromA, t)); // 컵 B 이동
                yield return null; // 다음 프레임
            }

            MoveCup(first, fromB); // 위치 확정
            MoveCup(second, fromA); // 위치 확정
            int swap = order[slotA]; // 자리 정보 교환
            order[slotA] = order[slotB]; // 자리 갱신
            order[slotB] = swap; // 자리 갱신
        }

        private static float SlotAnchor(int slot) => 0.08f + (slot * 0.30f); // 자리별 왼쪽 앵커

        private static void MoveCup(RectTransform cup, float left) // 컵 가로 위치 반영
        {
            cup.anchorMin = new Vector2(left, cup.anchorMin.y); // 왼쪽
            cup.anchorMax = new Vector2(left + 0.24f, cup.anchorMax.y); // 오른쪽
        }

        // ───────────────────────── 4. 상인의 내기 (판단) ─────────────────────────

        private void BuildHighLow() // 다음 카드가 위인지 아래인지 맞히며 배당 올리기
        {
            AddLabel("다음 카드가 지금보다 높을지 낮을지 고르세요. 같은 숫자는 성공으로 칩니다.", 0.92f, 0.07f, 18, HintColor, FontStyle.Normal); // 안내
            Image card = AddImage(0.80f, 0.32f, 0.36f, 0.64f, CardFace); // 카드
            Text number = RuntimeUiKit.CreateText(card.transform, "Number", "?", 80, new Color(0.18f, 0.16f, 0.22f, 1f), FontStyle.Bold); // 숫자
            RuntimeUiKit.Stretch(number.rectTransform); // 카드 채움
            Text payout = AddLabel(string.Empty, 0.44f, 0.08f, 24, AccentColor, FontStyle.Bold); // 배당 표시
            Button high = AddButton("위 (높다)", 0.32f, 0.10f, 0.04f, 0.32f, ActionColor, true, null); // 위
            Button low = AddButton("아래 (낮다)", 0.32f, 0.10f, 0.36f, 0.64f, ActionColor, true, null); // 아래
            Button stop = AddButton("여기서 멈추기", 0.32f, 0.10f, 0.68f, 0.96f, GoodColor, false, null); // 멈추기
            routine = StartCoroutine(HighLowRoutine(number, payout, high, low, stop)); // 진행 시작
        }

        private IEnumerator HighLowRoutine(Text number, Text payout, Button high, Button low, Button stop) // 상인의 내기 진행
        {
            int current = random.Next(2, 13); // 첫 카드 (2~12 — 끝 숫자는 피함)
            int streak = 0; // 연속 성공
            int choice = 0; // 1 = 위, -1 = 아래, 9 = 멈추기
            number.text = current.ToString(); // 첫 카드 표시
            high.onClick.AddListener(() => choice = 1); // 위 연결
            low.onClick.AddListener(() => choice = -1); // 아래 연결
            stop.onClick.AddListener(() => choice = 9); // 멈추기 연결

            while (streak < MinigameCatalog.HighLowMaxStreak) // 최대 5연속
            {
                payout.text = streak > 0 ? $"지금 멈추면 배당 {MinigameCatalog.GetPayout(MinigameKind.HighLow, streak, false):0.0}배 · 더 가면 {MinigameCatalog.GetPayout(MinigameKind.HighLow, streak + 1, false):0.0}배" : $"한 번 맞히면 배당 {MinigameCatalog.GetPayout(MinigameKind.HighLow, 1, false):0.0}배"; // 배당 안내
                SetStatus($"현재 카드 {current} · {streak}연속 성공"); // 진행 안내
                stop.interactable = streak > 0; // 한 번은 해야 멈출 수 있음
                choice = 0; // 입력 비우기
                while (choice == 0) yield return null; // 선택 대기

                if (choice == 9) // 멈추기
                {
                    SetStatus($"{streak}연속에서 멈췄습니다."); // 안내
                    yield return new WaitForSeconds(0.6f); // 여운
                    Settle(MinigameKind.HighLow, streak, false); // 확정 보상
                    yield break; // 종료
                }

                int next = random.Next(1, 14); // 다음 카드 (1~13)
                number.text = next.ToString(); // 표시
                bool correct = choice > 0 ? next >= current : next <= current; // 판정 (같으면 성공)
                current = next; // 카드 갱신

                if (!correct) // 실패
                {
                    number.color = BadColor; // 붉게
                    SetStatus("빗나갔습니다! 건 돈을 전부 잃었어요."); // 안내
                    yield return new WaitForSeconds(1.3f); // 여운
                    Settle(MinigameKind.HighLow, 0, true); // 전액 손실
                    yield break; // 종료
                }

                streak++; // 연속 증가
                number.color = GoodColor; // 초록
                yield return new WaitForSeconds(0.5f); // 여운
                number.color = new Color(0.18f, 0.16f, 0.22f, 1f); // 색 복귀
            }

            SetStatus($"{MinigameCatalog.HighLowMaxStreak}연속 성공! 최고 배당입니다."); // 결과 안내
            yield return new WaitForSeconds(0.8f); // 여운
            Settle(MinigameKind.HighLow, MinigameCatalog.HighLowMaxStreak, false); // 최고 보상
        }

        // ───────────────────────── 5. 마차 균형 (계산) ─────────────────────────

        private void BuildCargoBalance() // 화물을 좌우에 나눠 실어 무게 맞추기
        {
            int[] weights = BuildCargoWeights(); // 화물 무게 (완벽한 답이 반드시 있음)
            int[] sides = new int[weights.Length]; // 0 = 제외, 1 = 왼쪽, 2 = 오른쪽
            Text[] labels = new Text[weights.Length]; // 화물 글자
            Text summary = AddLabel(string.Empty, 0.92f, 0.08f, 22, AccentColor, FontStyle.Bold); // 좌우 요약
            AddLabel($"화물을 눌러 왼쪽 → 오른쪽 → 제외로 바꿉니다. 한쪽에 최대 {MinigameCatalog.CargoSideCapacity}개.", 0.84f, 0.06f, 16, HintColor, FontStyle.Normal); // 안내
            Button finish = AddButton("출발!", 0.10f, 0.09f, 0.30f, 0.70f, GoodColor, true, null); // 완료 버튼

            for (int index = 0; index < weights.Length; index++) // 4×2 배치
            {
                int captured = index; // 클릭용 복사
                int column = index % 4; // 열
                int row = index / 4; // 행
                Button cargo = AddButton(string.Empty, 0.74f - (row * 0.28f), 0.24f, 0.04f + (column * 0.24f), 0.26f + (column * 0.24f), SubColor, true, null); // 화물
                labels[index] = RuntimeUiKit.CreateText(cargo.transform, "Cargo", string.Empty, 22, Color.white, FontStyle.Bold).Wrap(); // 글자
                RuntimeUiKit.Stretch(labels[index].rectTransform, 6f); // 여백
                cargo.onClick.AddListener(() => { sides[captured] = (sides[captured] + 1) % 3; RefreshCargo(weights, sides, labels, cargo, captured, summary); }); // 좌 → 우 → 제외
                RefreshCargo(weights, sides, labels, cargo, index, summary); // 첫 표시
            }

            finish.onClick.AddListener(() => Settle(MinigameKind.CargoBalance, ScoreCargo(weights, sides), false)); // 완료
            SetStatus("좌우 무게를 정확히 맞추고 8개를 모두 실으면 100점입니다."); // 안내
        }

        private int[] BuildCargoWeights() // 화물 무게 만들기 (같은 무게 4쌍 → 완벽 분배가 반드시 가능)
        {
            int[] weights = new int[MinigameCatalog.CargoCount]; // 무게 배열

            for (int pair = 0; pair < MinigameCatalog.CargoCount / 2; pair++) // 4쌍
            {
                int weight = 10 + (random.Next(11) * 5); // 10~60 (5 단위)
                weights[pair * 2] = weight; // 첫 개
                weights[(pair * 2) + 1] = weight; // 짝
            }

            Shuffle(weights); // 섞기
            return weights; // 반환
        }

        private void RefreshCargo(int[] weights, int[] sides, Text[] labels, Button cargo, int index, Text summary) // 화물 한 칸 표시 갱신
        {
            string place = sides[index] == 1 ? "◀ 왼쪽" : sides[index] == 2 ? "오른쪽 ▶" : "제외"; // 자리
            labels[index].text = $"{weights[index]}kg\n{place}"; // 글자
            cargo.image.color = sides[index] == 1 ? ActionColor : sides[index] == 2 ? new Color(0.52f, 0.38f, 0.62f, 1f) : SubColor; // 색
            int left = SumSide(weights, sides, 1); // 왼쪽 무게
            int right = SumSide(weights, sides, 2); // 오른쪽 무게
            int loaded = CountSide(sides, 1) + CountSide(sides, 2); // 실은 개수
            summary.text = $"왼쪽 {left}kg ({CountSide(sides, 1)}개)   ·   오른쪽 {right}kg ({CountSide(sides, 2)}개)   ·   차이 {Mathf.Abs(left - right)}kg   ·   실은 화물 {loaded}/{weights.Length}"; // 요약
            summary.color = left == right && loaded == weights.Length ? GoodColor : AccentColor; // 완벽하면 초록
        }

        private static int ScoreCargo(int[] weights, int[] sides) // 마차 균형 점수 (적재 60 + 균형 40)
        {
            if (CountSide(sides, 1) > MinigameCatalog.CargoSideCapacity || CountSide(sides, 2) > MinigameCatalog.CargoSideCapacity) return 0; // 한쪽 초과 = 짐이 무너짐
            int total = 0; // 전체 무게
            int loadedWeight = 0; // 실은 무게
            for (int index = 0; index < weights.Length; index++) { total += weights[index]; if (sides[index] != 0) loadedWeight += weights[index]; } // 합산
            if (total <= 0) return 0; // 나눗셈 보호
            int diff = Mathf.Abs(SumSide(weights, sides, 1) - SumSide(weights, sides, 2)); // 무게 차이
            float loadRatio = (float)loadedWeight / total; // 적재율
            float balance = Mathf.Max(0f, 1f - (diff / 20f)); // 균형 점수 (20kg 이상 차이면 0)
            return Mathf.Clamp(Mathf.RoundToInt((loadRatio * 60f) + (balance * 40f)), 0, 100); // 점수 반환
        }

        private static int SumSide(int[] weights, int[] sides, int side) // 한쪽 무게 합
        {
            int sum = 0; // 합계
            for (int index = 0; index < weights.Length; index++) if (sides[index] == side) sum += weights[index]; // 누적
            return sum; // 반환
        }

        private static int CountSide(int[] sides, int side) // 한쪽 화물 개수
        {
            int count = 0; // 개수
            for (int index = 0; index < sides.Length; index++) if (sides[index] == side) count++; // 누적
            return count; // 반환
        }

        // ───────────────────────── 공용 도구 ─────────────────────────

        private void Shuffle(int[] values) // 배열 섞기 (피셔-예이츠)
        {
            for (int index = values.Length - 1; index > 0; index--) // 뒤에서부터
            {
                int swap = random.Next(index + 1); // 바꿀 자리
                int keep = values[index]; // 임시 보관
                values[index] = values[swap]; // 교환
                values[swap] = keep; // 교환
            }
        }

        private Text AddLabel(string text, float top, float height, int size, Color color, FontStyle style) // 내용 영역 글자 추가
        {
            Text label = RuntimeUiKit.CreateText(body, "Label", text, size, color, style, TextAnchor.MiddleCenter).Wrap(); // 글자
            RuntimeUiKit.SetRect(label.rectTransform, new Vector2(0f, top - height), new Vector2(1f, top)); // 배치
            bodyItems.Add(label.gameObject); // 목록 등록
            return label; // 반환
        }

        private Image AddImage(float top, float height, float left, float right, Color color) // 내용 영역 이미지 추가
        {
            Image image = RuntimeUiKit.CreateImage(body, "Shape", color); // 이미지
            RuntimeUiKit.SetRect(image.rectTransform, new Vector2(left, top - height), new Vector2(right, top)); // 배치
            bodyItems.Add(image.gameObject); // 목록 등록
            return image; // 반환
        }

        private Button AddButton(string text, float top, float height, float left, float right, Color color, bool interactable, UnityEngine.Events.UnityAction action) // 내용 영역 버튼 추가
        {
            Button button = RuntimeUiKit.CreateButton(body, "Action", color); // 버튼
            RuntimeUiKit.SetRect((RectTransform)button.transform, new Vector2(left, top - height), new Vector2(right, top)); // 배치
            if (!string.IsNullOrEmpty(text)) RuntimeUiKit.Stretch(RuntimeUiKit.CreateText(button.transform, "Label", text, 20, Color.white, FontStyle.Bold).Wrap().rectTransform, 6f); // 버튼 글자
            button.interactable = interactable; // 가능 여부
            if (action != null) button.onClick.AddListener(action); // 기능 연결
            bodyItems.Add(button.gameObject); // 목록 등록
            return button; // 반환
        }
    }
}
