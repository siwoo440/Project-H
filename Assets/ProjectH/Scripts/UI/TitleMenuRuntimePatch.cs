using System.Collections.Generic; // 목록 자료형
using ProjectH.Core; // 씬 이름 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // 씬 이벤트 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class TitleMenuRuntimePatch // 타이틀 메뉴 보강 (Day72 신규 — [설정] 추가 + 메뉴 세로 배치 정리)
    {
        public const string SettingsButtonName = "TitleSettingsButton"; // 설정 버튼 이름
        private const float MenuTop = 0.74f; // 메뉴가 차지하는 위쪽 (앵커)
        private const float MenuBottom = 0.06f; // 메뉴가 차지하는 아래쪽 (앵커)
        private const float MenuGap = 0.022f; // 버튼 사이 간격 (앵커)
        private const float MenuLeft = 0.15f; // 버튼 왼쪽 (앵커)
        private const float MenuRight = 0.85f; // 버튼 오른쪽 (앵커)

        private static readonly string[] MenuOrder = // 위에서 아래로 보여 줄 순서 (없는 버튼은 건너뛴다)
        {
            "NewGameButton", // 새 게임
            "ContinueButton", // 이어하기
            "DiaryButton", // 일기장 (Day63)
            SettingsButtonName, // 설정 (Day72)
            "QuitButton" // 종료
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // 첫 씬 로드 전 등록
        private static void Register() // 씬 로드 이벤트 등록
        {
            SceneRuntimePatch.Register(GameScenes.Title, OnTitleLoaded); // 타이틀 [설정]
        }

        private static void OnTitleLoaded(Scene scene) // 타이틀 : [설정] 추가 후 메뉴 정렬
        {
            GameObject sourceObject = GameObject.Find("ContinueButton"); // 모양을 빌려올 버튼
            if (sourceObject == null) return; // 구조 확인

            if (sourceObject.transform.parent.Find(SettingsButtonName) == null) // 중복 방지
            {
                GameObject settings = Object.Instantiate(sourceObject, sourceObject.transform.parent); // 같은 모양으로 복제
                settings.name = SettingsButtonName; // 이름
                Button button = settings.GetComponent<Button>(); // 버튼
                button.onClick = new Button.ButtonClickedEvent(); // 복제된 [이어하기] 동작 제거
                button.onClick.AddListener(OpenSettings); // 설정 열기
                button.interactable = true; // 저장이 없어도 언제나 열 수 있음
                Text label = settings.GetComponentInChildren<Text>(true); // 글자
                if (label != null) label.text = "설정"; // 글자 교체
            }

            RelayoutMenu(); // 메뉴 세로 배치 정리
        }

        public static void RelayoutMenu() // 타이틀 메뉴 버튼을 위에서부터 같은 간격으로 배치 (Day72 — 이 씬은 앵커로 배치되어 있어 위치값 계산이 통하지 않는다)
        {
            List<RectTransform> items = new List<RectTransform>(); // 배치할 버튼 목록

            foreach (string name in MenuOrder) // 정해진 순서대로
            {
                GameObject found = GameObject.Find(name); // 버튼 조회
                if (found != null) items.Add((RectTransform)found.transform); // 있으면 목록에 추가
            }

            if (items.Count == 0) return; // 배치할 버튼 없음
            float slot = (MenuTop - MenuBottom + MenuGap) / items.Count; // 한 칸 높이 (간격 포함)
            float height = Mathf.Max(0.04f, slot - MenuGap); // 버튼 높이

            for (int index = 0; index < items.Count; index++) // 버튼 순회
            {
                float max = MenuTop - (index * slot); // 위쪽 앵커
                RectTransform rect = items[index]; // 버튼 영역
                rect.anchorMin = new Vector2(MenuLeft, max - height); // 왼쪽 아래
                rect.anchorMax = new Vector2(MenuRight, max); // 오른쪽 위
                rect.offsetMin = Vector2.zero; // 앵커에 딱 맞춤
                rect.offsetMax = Vector2.zero; // 앵커에 딱 맞춤
                rect.anchoredPosition = Vector2.zero; // 위치 보정값 제거
                rect.SetSiblingIndex(index); // 겹침 순서도 같게
            }
        }

        private static void OpenSettings() // 타이틀에서 설정 열기 (ESC 메뉴·대화 화면과 같은 창)
        {
            SettingsView.Open(null); // 통합 설정 화면
        }
    }
}
