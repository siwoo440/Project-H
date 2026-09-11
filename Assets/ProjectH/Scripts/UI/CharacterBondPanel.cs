using System; // 콜백 델리게이트 기능
using System.Collections.Generic; // 목록 자료형
using ProjectH.Core; // 전역 게임 관리자 기능
using ProjectH.Dialogue; // 결속 대사 기능
using ProjectH.SaveSystem; // 결속·저장 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 중복 패널 방지
    public sealed class CharacterBondPanel : MonoBehaviour // 캐릭터 창 결속 패널 (Day59 신규 — CharacterAffinityRewardPanel과 같은 구조)
    {
        private static readonly Color TitleColor = new Color(0.30f, 0.18f, 0.42f, 1f); // 제목 보라색
        private static readonly Color AvailableColor = new Color(1f, 0.86f, 0.46f, 1f); // 올리기 금색
        private static readonly Color CompletedColor = new Color(0.84f, 0.80f, 0.98f, 1f); // 완료 연보라색
        private static readonly Color LockedColor = new Color(0.84f, 0.84f, 0.86f, 1f); // 잠김 회색

        private readonly List<Button> stageButtons = new List<Button>(); // 단계 버튼 1~5
        private readonly List<string> reasonBuffer = new List<string>(); // 미충족 사유 버퍼
        private Text titleText; // 패널 제목
        private Text resourceText; // 결속 자원 문구
        private Text skillText; // 결속 스킬 문구
        private Text resultText; // 결과 문구
        private string boundCharacterId = string.Empty; // 표시 캐릭터 ID
        private Action onChanged; // 변경 후 화면 갱신 콜백

        public bool IsOpen => gameObject.activeSelf; // 열림 여부 반환

        public static CharacterBondPanel Create(Transform parent, Action changedCallback) // 패널 생성
        {
            Image panel = RuntimeUiKit.CreateImage(parent, "BondPanel", new Color(0.97f, 0.96f, 0.99f, 0.98f)); // 패널 배경
            RuntimeUiKit.SetRect(panel.rectTransform, new Vector2(0.18f, 0.10f), new Vector2(0.82f, 0.90f)); // 화면 중앙 배치
            panel.gameObject.AddComponent<Outline>().effectColor = new Color(0.55f, 0.40f, 0.80f, 0.8f); // 보라 외곽선
            CharacterBondPanel view = panel.gameObject.AddComponent<CharacterBondPanel>(); // 컴포넌트 추가
            view.onChanged = changedCallback; // 콜백 저장
            view.Build(); // 구성
            panel.gameObject.SetActive(false); // 초기 숨김
            return view; // 패널 반환
        }

        private void Build() // 패널 구성
        {
            titleText = RuntimeUiKit.CreateText(transform, "Title", "결속", 22, TitleColor, FontStyle.Bold, TextAnchor.MiddleLeft); // 제목
            RuntimeUiKit.SetRect(titleText.rectTransform, new Vector2(0.04f, 0.88f), new Vector2(0.80f, 0.97f)); // 제목 배치
            Button closeButton = CreateButton("CloseButton", "닫기", new Color(0.88f, 0.88f, 0.90f, 1f)); // 닫기
            RuntimeUiKit.SetRect((RectTransform)closeButton.transform, new Vector2(0.84f, 0.89f), new Vector2(0.97f, 0.97f)); // 닫기 배치
            closeButton.onClick.AddListener(Toggle); // 닫기 연결
            resourceText = RuntimeUiKit.CreateText(transform, "Resource", string.Empty, 16, new Color(0.35f, 0.20f, 0.50f, 1f), FontStyle.Bold, TextAnchor.MiddleLeft); // 결속 자원 문구
            RuntimeUiKit.SetRect(resourceText.rectTransform, new Vector2(0.04f, 0.80f), new Vector2(0.72f, 0.87f)); // 자원 배치
            Button devButton = CreateButton("DevBondResource", "자원 +1 (테스트)", new Color(0.88f, 0.83f, 0.66f, 1f)); // 테스트 자원 버튼
            RuntimeUiKit.SetRect((RectTransform)devButton.transform, new Vector2(0.74f, 0.805f), new Vector2(0.96f, 0.865f)); // 테스트 버튼 배치
            devButton.onClick.AddListener(AddDemoResource); // 테스트 자원 연결
            DevelopmentFeatures.HideInRelease(devButton); // 출시 빌드 숨김

            for (int index = 0; index < BondCatalog.MaxLevel; index++) // 1~5단계 버튼
            {
                int level = index + 1; // 단계 번호
                float top = 0.79f - (index * 0.108f); // 세로 위치
                Button stage = CreateButton($"BondStage_{level}", string.Empty, LockedColor); // 단계 버튼
                RuntimeUiKit.SetRect((RectTransform)stage.transform, new Vector2(0.04f, top - 0.098f), new Vector2(0.96f, top)); // 버튼 배치
                stage.onClick.AddListener(() => Raise(level)); // 올리기 연결
                stageButtons.Add(stage); // 목록 등록
            }

            skillText = RuntimeUiKit.CreateText(transform, "Skill", string.Empty, 14, new Color(0.25f, 0.20f, 0.35f, 1f), FontStyle.Normal, TextAnchor.UpperLeft).Wrap(); // 결속 스킬·시너지 안내
            skillText.supportRichText = true; // 강조 색 사용
            RuntimeUiKit.SetRect(skillText.rectTransform, new Vector2(0.04f, 0.09f), new Vector2(0.96f, 0.255f)); // 안내 배치
            resultText = RuntimeUiKit.CreateText(transform, "Result", string.Empty, 15, new Color(0.45f, 0.25f, 0.60f, 1f)).Wrap(); // 결과 문구
            RuntimeUiKit.SetRect(resultText.rectTransform, new Vector2(0.04f, 0.005f), new Vector2(0.96f, 0.085f)); // 결과 배치
        }

        private Button CreateButton(string name, string label, Color color) // 캐릭터 화면 스타일 버튼
        {
            Button button = RuntimeUiKit.CreateButton(transform, name, color, false); // 버튼 생성
            Text text = RuntimeUiKit.CreateText(button.transform, "Text", label, 15, new Color(0.10f, 0.10f, 0.10f, 1f)).Wrap(); // 줄바꿈 라벨
            text.supportRichText = true; // 강조 색 사용
            RuntimeUiKit.Stretch(text.rectTransform, 6f); // 라벨 확장
            return button; // 버튼 반환
        }

        public void Toggle() // 열기·닫기
        {
            bool open = !gameObject.activeSelf; // 전환 후 상태
            gameObject.SetActive(open); // 표시 전환
            transform.SetAsLastSibling(); // 최상단
            resultText.text = string.Empty; // 결과 초기화
            if (open) onChanged?.Invoke(); // 열 때 최신 상태 반영
        }

        public void Refresh(SaveData saveData, CharacterSaveData characterSave, string displayName) // 선택 캐릭터 기준 갱신
        {
            if (!IsOpen || saveData == null || characterSave == null) // 표시·데이터 확인
            {
                return; // 닫혀 있으면 생략
            }

            boundCharacterId = characterSave.CharacterId; // 캐릭터 저장
            int level = characterSave.BondLevel; // 결속 단계
            int resource = BondService.GetResource(saveData); // 결속 자원 (회복 반영)
            titleText.text = $"{displayName} · 결속 {level}/{BondCatalog.MaxLevel}"; // 제목
            resourceText.text = $"결속 자원 {new string('◆', resource)}{new string('◇', Mathf.Max(0, BondCatalog.MaxResource - resource))} {resource}/{BondCatalog.MaxResource} · 하루 +{BondCatalog.ResourcePerDay}"; // 자원 표시

            for (int index = 0; index < stageButtons.Count; index++) // 단계 버튼 순회
            {
                int stage = index + 1; // 단계 번호
                BondStageState state = BondService.GetStageState(saveData, boundCharacterId, stage, reasonBuffer); // 상태·사유 조회
                string effect = BondCatalog.GetStageEffectText(boundCharacterId, stage); // 효과 설명
                string head = state == BondStageState.Completed ? "[완료]" : state == BondStageState.Available ? (resource > 0 ? "<color=#8A4B00>[올리기 · 자원 1]</color>" : "[자원 부족]") : "[잠김]"; // 상태 문구
                string condition = state == BondStageState.Locked ? $"\n<color=#777777>조건 : {string.Join(" · ", reasonBuffer)}</color>" : string.Empty; // 부족 조건
                SetButtonLabel(stageButtons[index], $"{head}  {stage}단계 · {effect}{condition}"); // 버튼 문구
                stageButtons[index].interactable = state == BondStageState.Available; // 올릴 수 있는 단계만 입력
                stageButtons[index].GetComponent<Image>().color = state == BondStageState.Completed ? CompletedColor : state == BondStageState.Available ? AvailableColor : LockedColor; // 상태 색상
            }

            BondSkillDefinition skill = BondCatalog.GetSkill(boundCharacterId); // 결속 스킬 조회
            string skillLine = skill == null ? "결속 스킬 · 추가 캐릭터 일차에 연결" : $"결속 스킬 「{skill.SkillName}」 {(BondCatalog.HasBondSkill(level) ? "<color=#2E8B57>[사용 중]</color>" : "(5단계 해금)")} · {skill.SkillText}"; // 스킬 안내
            skillText.text = $"{skillLine}\n조합 시너지 · 균형 파티(초기 4인 편성) 공격·방어·회복 +{BondCatalog.BalancedPartyBonus * 100f:0}% / 결속의 원탁(결속 {BondCatalog.RoundTableMinLevel}단계 이상 4명) 궁극기 게이지 +{BondCatalog.RoundTableGaugeBonus * 100f:0}%"; // 스킬·시너지 안내
        }

        private void Raise(int level) // 결속 단계 올리기
        {
            SaveManager saveManager = GameManager.Instance == null ? null : GameManager.Instance.Save; // 저장 관리자
            SaveData saveData = saveManager == null ? null : saveManager.CurrentSave; // 저장 데이터

            if (saveData == null || string.IsNullOrEmpty(boundCharacterId)) // 데이터 확인
            {
                resultText.text = "저장 데이터를 찾을 수 없습니다."; // 오류 안내
                return; // 중단
            }

            bool raised = BondService.TryRaise(saveData, boundCharacterId, out string message); // 단계 올리기
            bool saved = raised && saveManager.SaveCurrent(); // 성공 시 저장
            resultText.text = raised && !saved ? $"{message} (저장 실패)" : message; // 결과 표시
            onChanged?.Invoke(); // 화면 갱신

            if (raised) // 성공 확인
            {
                DialogueOverlayView.Open(DialogueLibrary.Load(BondCatalog.GetBondScriptId(boundCharacterId, BondService.GetLevel(saveData, boundCharacterId))), runner => onChanged?.Invoke()); // 결속 대사 (파일이 없으면 생략)
            }
        }

        private void AddDemoResource() // 테스트 결속 자원 +1 (개발 빌드 전용)
        {
            SaveManager saveManager = GameManager.Instance == null ? null : GameManager.Instance.Save; // 저장 관리자
            SaveData saveData = saveManager == null ? null : saveManager.CurrentSave; // 저장 데이터
            if (saveData == null) return; // 데이터 확인
            BondService.AddResource(saveData, 1); // 자원 +1
            saveManager.SaveCurrent(); // 저장
            resultText.text = "테스트 결속 자원 +1"; // 안내
            onChanged?.Invoke(); // 갱신
        }

        private static void SetButtonLabel(Button button, string label) // 버튼 문구 변경
        {
            Text text = button.GetComponentInChildren<Text>(true); // 문구 조회
            if (text != null) text.text = label; // 문구 적용
        }
    }
}
