using System; // 콜백 자료형
using System.Collections.Generic; // 목록 자료형
using ProjectH.Core; // 게임 관리자·씬 이름 기능
using ProjectH.SaveSystem; // 저장 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 중복 생성 방지
    public sealed class EscMenuView : MonoBehaviour // ESC 세로형 메뉴바 (Day72 신규 — 로비의 일기장·저장 버튼을 여기로 모았다)
    {
        private static readonly Color BarColor = new Color(0.07f, 0.08f, 0.12f, 0.97f); // 메뉴바 배경
        private static readonly Color ItemColor = new Color(0.19f, 0.22f, 0.30f, 1f); // 메뉴 버튼
        private static readonly Color ExitColor = new Color(0.42f, 0.24f, 0.24f, 1f); // 나가기 버튼
        private static readonly Color TitleColor = new Color(0.92f, 0.78f, 0.42f, 1f); // 제목 금색
        private static readonly Color HintColor = new Color(0.74f, 0.77f, 0.84f, 1f); // 안내 글자
        private const float ItemHeight = 0.082f; // 버튼 한 칸 높이
        private const float ItemGap = 0.016f; // 버튼 사이 간격

        private static EscMenuView current; // 현재 열린 메뉴 (중복 방지)

        private Transform bar; // 세로 메뉴바
        private Text statusText; // 안내 줄
        private bool busy; // 하위 화면이 열려 있음

        public static bool IsOpen => current != null; // 열려 있는지

        public static void Toggle() // ESC : 열기 · 닫기
        {
            if (current != null) { current.Close(); return; } // 열려 있으면 닫기
            Open(); // 없으면 열기
        }

        public static EscMenuView Open() // 메뉴 열기
        {
            if (current != null) return current; // 이미 열림
            GameObject root = new GameObject("EscMenuView", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 런타임 Canvas
            Canvas canvas = root.GetComponent<Canvas>(); // Canvas 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // Overlay 렌더링
            canvas.sortingOrder = 750; // 대화·놀이판보다 위
            CanvasScaler scaler = root.GetComponent<CanvasScaler>(); // 스케일러 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반
            scaler.referenceResolution = new Vector2(1600f, 900f); // 기준 해상도
            current = root.AddComponent<EscMenuView>(); // 컴포넌트 추가
            current.Build(); // 화면 구성
            return current; // 반환
        }

        private void Build() // 세로 메뉴바 구성
        {
            Button dim = RuntimeUiKit.CreateButton(transform, "Dim", new Color(0f, 0f, 0f, 0.62f)); // 어두운 막 (누르면 닫힘)
            RuntimeUiKit.Stretch((RectTransform)dim.transform); // 전체
            dim.onClick.AddListener(Close); // 바깥 클릭으로 닫기
            Image barImage = RuntimeUiKit.CreateImage(transform, "Bar", BarColor); // 세로 메뉴바
            RuntimeUiKit.SetRect(barImage.rectTransform, new Vector2(0f, 0f), new Vector2(0.24f, 1f)); // 왼쪽 세로 전체
            barImage.gameObject.AddComponent<Outline>().effectColor = new Color(0.86f, 0.72f, 0.36f, 0.8f); // 금색 테두리
            bar = barImage.transform; // 보관
            Text title = RuntimeUiKit.CreateText(bar, "Title", "메 뉴", 32, TitleColor, FontStyle.Bold); // 제목
            RuntimeUiKit.SetRect(title.rectTransform, new Vector2(0.08f, 0.89f), new Vector2(0.92f, 0.97f)); // 위
            float top = 0.84f; // 첫 버튼 위치
            top = AddItem("계속하기", ItemColor, top, Close); // 닫기
            top = AddItem("저장하기", ItemColor, top, () => OpenSlots(SaveSlotMode.Save)); // 저장
            top = AddItem("불러오기", ItemColor, top, () => OpenSlots(SaveSlotMode.Load)); // 불러오기
            top = AddItem("일기장", ItemColor, top, OpenDiary); // 일기장 (로비 버튼을 옮겨 옴)
            top = AddItem("설정", ItemColor, top, OpenSettings); // 설정
            AddItem("타이틀로 나가기", ExitColor, top - 0.02f, ExitToTitle); // 타이틀
            statusText = RuntimeUiKit.CreateText(bar, "Status", "ESC를 다시 누르면 닫힙니다.", 15, HintColor, FontStyle.Normal).Wrap(); // 안내
            RuntimeUiKit.SetRect(statusText.rectTransform, new Vector2(0.06f, 0.02f), new Vector2(0.94f, 0.12f)); // 아래
        }

        private float AddItem(string label, Color color, float top, UnityEngine.Events.UnityAction action) // 메뉴 버튼 한 칸 추가 후 다음 위치 반환
        {
            Button button = RuntimeUiKit.CreateButton(bar, label, color); // 버튼
            RuntimeUiKit.SetRect((RectTransform)button.transform, new Vector2(0.08f, top - ItemHeight), new Vector2(0.92f, top)); // 배치
            RuntimeUiKit.Stretch(RuntimeUiKit.CreateText(button.transform, "Label", label, 21, Color.white, FontStyle.Bold).rectTransform); // 글자
            button.onClick.AddListener(action); // 기능 연결
            return top - ItemHeight - ItemGap; // 다음 위치 반환
        }

        private void SetStatus(string text) // 안내 줄 갱신
        {
            if (statusText != null) statusText.text = string.IsNullOrEmpty(text) ? "ESC를 다시 누르면 닫힙니다." : text; // 반영
        }

        private void OpenSlots(SaveSlotMode mode) // 저장 · 불러오기 화면 열기
        {
            if (busy) return; // 진행 중
            SaveManager manager = GameManager.Instance == null ? null : GameManager.Instance.Save; // 저장 관리자

            if (manager == null || manager.CurrentSave == null) // 진행 중인 저장 확인
            {
                SetStatus("진행 중인 게임이 없습니다."); // 안내
                return; // 중단
            }

            busy = true; // 하위 화면 열림
            bar.gameObject.SetActive(false); // 메뉴바 감추기 (겹치지 않게)
            SaveSlotBrowserView.Open(mode, message => // 슬롯 화면
            {
                busy = false; // 하위 화면 닫힘
                if (this == null) return; // 이미 닫힘
                bar.gameObject.SetActive(true); // 메뉴바 다시 표시
                SetStatus(message); // 결과 안내
                if (mode == SaveSlotMode.Load && !string.IsNullOrEmpty(message) && message.Contains("불러왔")) ReloadCurrentScene(); // 불러왔으면 화면 다시 그리기
            });
        }

        private void OpenSettings() // 설정 화면 열기
        {
            if (busy) return; // 진행 중
            busy = true; // 하위 화면 열림
            bar.gameObject.SetActive(false); // 메뉴바 감추기
            SettingsView.Open(message => // 설정 화면
            {
                busy = false; // 하위 화면 닫힘
                if (this == null) return; // 이미 닫힘
                bar.gameObject.SetActive(true); // 메뉴바 다시 표시
                SetStatus(message); // 결과 안내
            });
        }

        private void OpenDiary() // 일기장 열기 (로비 [일기장] 버튼을 여기로 옮겼다)
        {
            string from = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name; // 돌아올 씬
            Close(); // 메뉴 닫기
            DiarySceneRuntimePatch.OpenDiary(from); // 일기장 이동
        }

        private void ExitToTitle() // 타이틀로 나가기
        {
            Close(); // 메뉴 닫기
            if (GameManager.Instance != null && GameManager.Instance.Scenes != null) GameManager.Instance.Scenes.LoadScene(GameScenes.Title); // 타이틀 이동
        }

        private static void ReloadCurrentScene() // 불러오기 후 현재 씬을 다시 열어 화면을 새 저장에 맞춘다
        {
            if (GameManager.Instance == null || GameManager.Instance.Scenes == null) return; // 씬 로더 확인
            GameManager.Instance.Scenes.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name); // 같은 씬 다시 로드
        }

        private void Close() // 메뉴 닫기
        {
            if (busy) return; // 하위 화면이 열려 있으면 그쪽부터
            current = null; // 현재 메뉴 해제
            Destroy(gameObject); // 화면 닫기
        }

        private void OnDestroy() // 파괴 시 정리
        {
            if (current == this) current = null; // 현재 메뉴 해제
        }
    }
}
