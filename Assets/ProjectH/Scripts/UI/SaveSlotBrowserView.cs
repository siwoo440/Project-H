using System; // 콜백 자료형
using System.Collections.Generic; // 목록 자료형
using ProjectH.Core; // 게임 관리자 기능
using ProjectH.SaveSystem; // 저장 슬롯 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public enum SaveSlotMode // 슬롯 화면 용도 (Day72 신규)
    {
        Save = 0, // 저장하기
        Load = 1 // 불러오기
    }

    [DisallowMultipleComponent] // 중복 생성 방지
    public sealed class SaveSlotBrowserView : MonoBehaviour // 저장·불러오기 슬롯 화면 (Day72 신규 — 한 페이지에 가로 3 × 세로 2 = 6칸)
    {
        private static readonly Color BoxColor = new Color(0.08f, 0.09f, 0.13f, 0.98f); // 화면 배경
        private static readonly Color FilledColor = new Color(0.17f, 0.21f, 0.30f, 1f); // 저장이 있는 칸
        private static readonly Color EmptyColor = new Color(0.13f, 0.14f, 0.17f, 1f); // 빈 칸
        private static readonly Color NumberColor = new Color(0.92f, 0.78f, 0.42f, 1f); // 칸 번호 금색
        private static readonly Color DateColor = new Color(0.86f, 0.90f, 0.98f, 1f); // 날짜·시간
        private static readonly Color HintColor = new Color(0.76f, 0.78f, 0.84f, 1f); // 안내 글자
        private static readonly Color SubColor = new Color(0.24f, 0.26f, 0.32f, 1f); // 보조 버튼
        private static readonly Color YesColor = new Color(0.30f, 0.52f, 0.36f, 1f); // 예 버튼
        private static readonly Color NoColor = new Color(0.48f, 0.28f, 0.28f, 1f); // 아니오 버튼
        private const float CellGapX = 0.02f; // 칸 가로 간격
        private const float CellGapY = 0.05f; // 칸 세로 간격

        private SaveSlotMode mode; // 저장 · 불러오기
        private Action<string> onClosed; // 닫을 때 알림
        private RectTransform body; // 칸이 놓이는 영역
        private Text headerText; // 제목
        private Text statusText; // 안내 줄
        private RectTransform confirmBar; // 확인 줄
        private Text confirmText; // 확인 문구
        private readonly List<GameObject> cells = new List<GameObject>(); // 칸 목록
        private int page; // 현재 페이지
        private string lastMessage = string.Empty; // 마지막 결과 안내

        public static SaveSlotBrowserView Open(SaveSlotMode slotMode, Action<string> closed) // 슬롯 화면 열기
        {
            GameObject root = new GameObject("SaveSlotBrowserView", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 런타임 Canvas
            Canvas canvas = root.GetComponent<Canvas>(); // Canvas 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // Overlay 렌더링
            canvas.sortingOrder = 760; // ESC 메뉴(750)보다 위
            CanvasScaler scaler = root.GetComponent<CanvasScaler>(); // 스케일러 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반
            scaler.referenceResolution = new Vector2(1600f, 900f); // 기준 해상도
            SaveSlotBrowserView view = root.AddComponent<SaveSlotBrowserView>(); // 컴포넌트 추가
            view.mode = slotMode; // 용도 저장
            view.onClosed = closed; // 콜백 저장
            view.Build(); // 화면 구성
            view.Refresh(); // 칸 채우기
            return view; // 반환
        }

        private void Build() // 화면 뼈대 (어두운 막 + 상자 + 제목 · 칸 영역 · 페이지 · 안내)
        {
            Image dim = RuntimeUiKit.CreateImage(transform, "Dim", new Color(0f, 0f, 0f, 0.84f)); // 어두운 막
            RuntimeUiKit.Stretch(dim.rectTransform); // 전체
            Image box = RuntimeUiKit.CreateImage(transform, "Box", BoxColor); // 상자
            RuntimeUiKit.SetRect(box.rectTransform, new Vector2(0.10f, 0.08f), new Vector2(0.90f, 0.94f)); // 가운데
            box.gameObject.AddComponent<Outline>().effectColor = new Color(0.86f, 0.72f, 0.36f, 0.85f); // 금색 테두리
            headerText = RuntimeUiKit.CreateText(box.transform, "Header", string.Empty, 30, Color.white, FontStyle.Bold); // 제목
            RuntimeUiKit.SetRect(headerText.rectTransform, new Vector2(0.03f, 0.91f), new Vector2(0.80f, 0.99f)); // 위
            headerText.alignment = TextAnchor.MiddleLeft; // 왼쪽 정렬
            Button close = RuntimeUiKit.CreateButton(box.transform, "Close", SubColor); // 닫기
            RuntimeUiKit.SetRect(close.GetComponent<RectTransform>(), new Vector2(0.83f, 0.91f), new Vector2(0.97f, 0.99f)); // 오른쪽 위
            RuntimeUiKit.Stretch(RuntimeUiKit.CreateText(close.transform, "Label", "닫기", 20, Color.white, FontStyle.Bold).rectTransform); // 글자
            close.onClick.AddListener(Close); // 닫기 연결
            Image bodyImage = RuntimeUiKit.CreateImage(box.transform, "Body", new Color(0f, 0f, 0f, 0f)); // 칸 영역 (투명)
            RuntimeUiKit.SetRect(bodyImage.rectTransform, new Vector2(0.03f, 0.20f), new Vector2(0.97f, 0.89f)); // 가운데
            body = bodyImage.rectTransform; // 영역 저장
            BuildPageBar(box.transform); // 페이지 이동 줄
            BuildConfirmBar(box.transform); // 확인 줄
            statusText = RuntimeUiKit.CreateText(box.transform, "Status", string.Empty, 18, HintColor, FontStyle.Normal).Wrap(); // 안내 줄
            RuntimeUiKit.SetRect(statusText.rectTransform, new Vector2(0.03f, 0.01f), new Vector2(0.97f, 0.07f)); // 아래
            statusText.alignment = TextAnchor.MiddleLeft; // 왼쪽 정렬
        }

        private void BuildPageBar(Transform parent) // 페이지 이동 줄 (◀ 1 / 3 ▶)
        {
            Button previous = RuntimeUiKit.CreateButton(parent, "PrevPage", SubColor); // 이전 페이지
            RuntimeUiKit.SetRect(previous.GetComponent<RectTransform>(), new Vector2(0.34f, 0.09f), new Vector2(0.42f, 0.17f)); // 왼쪽
            RuntimeUiKit.Stretch(RuntimeUiKit.CreateText(previous.transform, "Label", "◀", 24, Color.white, FontStyle.Bold).rectTransform); // 글자
            previous.onClick.AddListener(() => ChangePage(-1)); // 연결
            Text pageText = RuntimeUiKit.CreateText(parent, "PageText", string.Empty, 22, DateColor, FontStyle.Bold); // 페이지 표시
            RuntimeUiKit.SetRect(pageText.rectTransform, new Vector2(0.43f, 0.09f), new Vector2(0.57f, 0.17f)); // 가운데
            pageText.name = "PageText"; // 이름 (갱신 때 찾기)
            Button next = RuntimeUiKit.CreateButton(parent, "NextPage", SubColor); // 다음 페이지
            RuntimeUiKit.SetRect(next.GetComponent<RectTransform>(), new Vector2(0.58f, 0.09f), new Vector2(0.66f, 0.17f)); // 오른쪽
            RuntimeUiKit.Stretch(RuntimeUiKit.CreateText(next.transform, "Label", "▶", 24, Color.white, FontStyle.Bold).rectTransform); // 글자
            next.onClick.AddListener(() => ChangePage(1)); // 연결
        }

        private void BuildConfirmBar(Transform parent) // 덮어쓰기·불러오기 확인 줄 (평소에는 숨김)
        {
            Image bar = RuntimeUiKit.CreateImage(parent, "ConfirmBar", new Color(0.16f, 0.13f, 0.10f, 0.96f)); // 확인 줄 배경
            RuntimeUiKit.SetRect(bar.rectTransform, new Vector2(0.03f, 0.08f), new Vector2(0.97f, 0.18f)); // 페이지 줄 자리
            confirmBar = bar.rectTransform; // 보관
            confirmText = RuntimeUiKit.CreateText(bar.transform, "Text", string.Empty, 20, Color.white, FontStyle.Bold).Wrap(); // 확인 문구
            RuntimeUiKit.SetRect(confirmText.rectTransform, new Vector2(0.02f, 0f), new Vector2(0.62f, 1f)); // 왼쪽
            confirmText.alignment = TextAnchor.MiddleLeft; // 왼쪽 정렬
            bar.gameObject.SetActive(false); // 평소에는 숨김
        }

        private void Refresh() // 제목 · 칸 · 페이지 표시 갱신
        {
            headerText.text = mode == SaveSlotMode.Save ? "저장하기" : "불러오기"; // 제목
            Text pageText = transform.Find("Box/PageText") == null ? null : transform.Find("Box/PageText").GetComponent<Text>(); // 페이지 표시
            if (pageText != null) pageText.text = $"{page + 1} / {SaveSlotCatalog.PageCount}"; // 페이지 문구
            BuildCells(); // 칸 다시 그리기
            if (string.IsNullOrEmpty(lastMessage)) SetStatus(mode == SaveSlotMode.Save ? "저장할 칸을 고르세요. 저장이 들어 있는 칸을 고르면 덮어씁니다." : "불러올 칸을 고르세요. 지금 진행 중인 내용은 저장하지 않으면 사라집니다."); // 안내
            else SetStatus(lastMessage); // 결과 안내
        }

        private void BuildCells() // 한 페이지 6칸 만들기 (가로 3 × 세로 2)
        {
            foreach (GameObject cell in cells) Destroy(cell); // 이전 칸 제거
            cells.Clear(); // 목록 비움
            List<SaveSlotInfo> infos = GetSaveManager() == null ? new List<SaveSlotInfo>() : GetSaveManager().ReadPage(page); // 요약 읽기
            float cellWidth = (1f - (CellGapX * (SaveSlotCatalog.Columns - 1))) / SaveSlotCatalog.Columns; // 칸 가로 크기
            float cellHeight = (1f - (CellGapY * (SaveSlotCatalog.Rows - 1))) / SaveSlotCatalog.Rows; // 칸 세로 크기

            for (int index = 0; index < infos.Count; index++) // 칸 순회
            {
                SaveSlotInfo info = infos[index]; // 요약
                int column = SaveSlotCatalog.GetColumn(info.Slot); // 가로 위치
                int row = SaveSlotCatalog.GetRow(info.Slot); // 세로 위치
                float left = column * (cellWidth + CellGapX); // 왼쪽
                float top = 1f - (row * (cellHeight + CellGapY)); // 위쪽 (위에서부터 채움)
                BuildCell(info, left, left + cellWidth, top - cellHeight, top); // 칸 하나
            }
        }

        private void BuildCell(SaveSlotInfo info, float left, float right, float bottom, float top) // 칸 하나 만들기
        {
            Button cell = RuntimeUiKit.CreateButton(body, $"Slot{info.Slot}", info.Exists ? FilledColor : EmptyColor); // 칸 버튼
            RuntimeUiKit.SetRect((RectTransform)cell.transform, new Vector2(left, bottom), new Vector2(right, top)); // 배치
            cell.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f); // 테두리
            int slot = info.Slot; // 클릭용 복사
            cell.onClick.AddListener(() => AskConfirm(slot, info.Exists)); // 선택
            cell.interactable = mode == SaveSlotMode.Save || info.Exists; // 불러오기는 빈 칸을 고를 수 없음
            Text number = RuntimeUiKit.CreateText(cell.transform, "Number", $"{SaveSlotCatalog.GetSlotNumber(info.Slot):00}", 34, NumberColor, FontStyle.Bold, TextAnchor.UpperLeft); // 칸 번호 (왼쪽 위)
            RuntimeUiKit.SetRect(number.rectTransform, new Vector2(0.05f, 0.66f), new Vector2(0.45f, 0.96f)); // 왼쪽 위 배치
            Text date = RuntimeUiKit.CreateText(cell.transform, "Date", info.GetDateText(), 19, info.Exists ? DateColor : HintColor, FontStyle.Bold, TextAnchor.UpperRight); // 날짜·시간
            RuntimeUiKit.SetRect(date.rectTransform, new Vector2(0.35f, 0.66f), new Vector2(0.95f, 0.96f)); // 오른쪽 위 배치
            Text summary = RuntimeUiKit.CreateText(cell.transform, "Summary", info.GetSummaryText(), 17, info.Exists ? Color.white : HintColor, FontStyle.Normal, TextAnchor.UpperLeft).Wrap(); // 진행 요약
            RuntimeUiKit.SetRect(summary.rectTransform, new Vector2(0.05f, 0.26f), new Vector2(0.95f, 0.62f)); // 가운데 배치

            if (info.Exists && !string.IsNullOrEmpty(info.Chapter)) // 챕터 문구 표시
            {
                Text chapter = RuntimeUiKit.CreateText(cell.transform, "Chapter", info.Chapter, 16, HintColor, FontStyle.Normal, TextAnchor.LowerLeft).Wrap(); // 챕터
                RuntimeUiKit.SetRect(chapter.rectTransform, new Vector2(0.05f, 0.05f), new Vector2(0.72f, 0.25f)); // 아래 배치
            }

            if (info.Exists) // 저장이 있는 칸만 비우기 버튼
            {
                Button clear = RuntimeUiKit.CreateButton(cell.transform, "Clear", NoColor); // 비우기
                RuntimeUiKit.SetRect(clear.GetComponent<RectTransform>(), new Vector2(0.76f, 0.05f), new Vector2(0.95f, 0.22f)); // 오른쪽 아래
                RuntimeUiKit.Stretch(RuntimeUiKit.CreateText(clear.transform, "Label", "비우기", 15, Color.white, FontStyle.Bold).rectTransform); // 글자
                clear.onClick.AddListener(() => AskDelete(slot)); // 연결
            }

            cells.Add(cell.gameObject); // 목록 등록
        }

        private void ChangePage(int delta) // 페이지 이동
        {
            int next = Mathf.Clamp(page + delta, 0, SaveSlotCatalog.PageCount - 1); // 범위 보정
            if (next == page) return; // 변화 없음
            page = next; // 페이지 변경
            lastMessage = string.Empty; // 안내 비움
            HideConfirm(); // 확인 줄 닫기
            Refresh(); // 다시 그리기
        }

        private void AskConfirm(int slot, bool exists) // 저장·불러오기 확인 (빈 칸에 저장할 때는 바로 실행)
        {
            if (mode == SaveSlotMode.Save && !exists) { Apply(slot); return; } // 빈 칸 저장은 확인 없이
            string question = mode == SaveSlotMode.Save
                ? $"{SaveSlotCatalog.GetSlotNumber(slot)}번 칸에 덮어씁니다. 계속할까요?"
                : $"{SaveSlotCatalog.GetSlotNumber(slot)}번 칸을 불러옵니다. 저장하지 않은 진행은 사라집니다."; // 질문
            ShowConfirm(question, () => Apply(slot)); // 확인 줄 표시
        }

        private void AskDelete(int slot) // 칸 비우기 확인
        {
            ShowConfirm($"{SaveSlotCatalog.GetSlotNumber(slot)}번 칸을 비웁니다. 되돌릴 수 없습니다.", () => // 확인 줄 표시
            {
                SaveManager manager = GetSaveManager(); // 저장 관리자
                if (manager == null) return; // 없음
                manager.DeleteSlot(slot, out string message); // 삭제
                lastMessage = message; // 안내 저장
                Refresh(); // 다시 그리기
            });
        }

        private void Apply(int slot) // 고른 칸에 저장하거나 불러오기
        {
            SaveManager manager = GetSaveManager(); // 저장 관리자

            if (manager == null) // 없음
            {
                SetStatus("저장 관리자를 찾을 수 없습니다."); // 안내
                return; // 중단
            }

            bool done = mode == SaveSlotMode.Save ? manager.SaveToSlot(slot, out string message) : manager.LoadFromSlot(slot, out message); // 실행
            lastMessage = message; // 안내 저장

            if (done && mode == SaveSlotMode.Load) // 불러오기 성공
            {
                Close(); // 화면 닫기 (호출 측이 씬을 다시 그린다)
                return; // 종료
            }

            Refresh(); // 다시 그리기
        }

        private void ShowConfirm(string question, Action accepted) // 확인 줄 열기
        {
            confirmText.text = question; // 질문
            foreach (Transform child in confirmBar) if (child.name == "Yes" || child.name == "No") Destroy(child.gameObject); // 이전 버튼 제거
            Button yes = RuntimeUiKit.CreateButton(confirmBar, "Yes", YesColor); // 예
            RuntimeUiKit.SetRect(yes.GetComponent<RectTransform>(), new Vector2(0.64f, 0.14f), new Vector2(0.79f, 0.86f)); // 배치
            RuntimeUiKit.Stretch(RuntimeUiKit.CreateText(yes.transform, "Label", "예", 20, Color.white, FontStyle.Bold).rectTransform); // 글자
            yes.onClick.AddListener(() => { HideConfirm(); accepted?.Invoke(); }); // 연결
            Button no = RuntimeUiKit.CreateButton(confirmBar, "No", SubColor); // 아니오
            RuntimeUiKit.SetRect(no.GetComponent<RectTransform>(), new Vector2(0.81f, 0.14f), new Vector2(0.96f, 0.86f)); // 배치
            RuntimeUiKit.Stretch(RuntimeUiKit.CreateText(no.transform, "Label", "아니오", 20, Color.white, FontStyle.Bold).rectTransform); // 글자
            no.onClick.AddListener(HideConfirm); // 연결
            confirmBar.gameObject.SetActive(true); // 표시
        }

        private void HideConfirm() // 확인 줄 닫기
        {
            if (confirmBar != null) confirmBar.gameObject.SetActive(false); // 숨김
        }

        private void SetStatus(string text) // 안내 줄 갱신
        {
            if (statusText != null) statusText.text = text ?? string.Empty; // 반영
        }

        private void Close() // 닫기 : 결과 안내 전달
        {
            Action<string> callback = onClosed; // 콜백 복사
            onClosed = null; // 중복 방지
            Destroy(gameObject); // 화면 닫기
            callback?.Invoke(lastMessage); // 안내 전달
        }

        private static SaveManager GetSaveManager() => GameManager.Instance == null ? null : GameManager.Instance.Save; // 저장 관리자 조회
    }
}
