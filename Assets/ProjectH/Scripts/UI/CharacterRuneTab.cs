using System; // 콜백 델리게이트 기능
using System.Collections.Generic; // 목록 자료형
using ProjectH.Core; // 전역 게임 관리자 기능
using ProjectH.Data; // 캐릭터·아이템 데이터 기능
using ProjectH.SaveSystem; // 룬 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 중복 탭 방지
    public sealed class CharacterRuneTab : MonoBehaviour // 캐릭터 창 [룬] 탭 (Day60 신규 — 일러스트 둘레 5슬롯 + 보유 룬 목록 + 상세·강화·합성·분해)
    {
        private static readonly string[] FilterLabels = { "전체", "공격", "방어", "회복", "특수" }; // 필터 문구
        private static readonly Color SelectedColor = new Color(1f, 0.80f, 0.25f, 1f); // 선택 강조 금색

        private readonly Image[] slotIcons = new Image[RuneCatalog.SlotCount]; // 슬롯 아이콘
        private readonly Text[] slotStars = new Text[RuneCatalog.SlotCount]; // 슬롯 별
        private readonly Text[] slotLocks = new Text[RuneCatalog.SlotCount]; // 슬롯 잠금 문구
        private readonly Outline[] slotOutlines = new Outline[RuneCatalog.SlotCount]; // 슬롯 선택 테두리
        private readonly List<Button> filterButtons = new List<Button>(); // 필터 버튼
        private GameObject slotRoot; // 슬롯 묶음 (탭 전환 시 함께 숨김)
        private RectTransform listContent; // 룬 목록
        private Image detailIcon; // 상세 아이콘
        private Text nameText; // 룬 이름
        private Text gradeText; // 등급·레벨
        private Text effectText; // 효과
        private Text nextText; // 다음 레벨·비용
        private Text stateText; // 장착·잠금·합성 상태
        private Text footerText; // 추천·재료
        private Button equipButton; // 장착
        private Button unequipButton; // 해제
        private Button enhanceButton; // 강화
        private Button synthButton; // 합성
        private Button dismantleButton; // 분해
        private Button lockButton; // 잠금
        private Action onChanged; // 변경 후 화면 갱신 콜백
        private Action<string> onStatus; // 상태 문구 콜백
        private string characterId = string.Empty; // 표시 캐릭터
        private string selectedRuneId = string.Empty; // 선택 룬
        private int selectedSlot = -1; // 선택 슬롯
        private int filter; // 필터 번호 (0 전체)

        public static CharacterRuneTab Create(RectTransform contentRoot, RectTransform leftArea, Action changedCallback, Action<string> statusCallback) // 탭 생성
        {
            CharacterRuneTab tab = contentRoot.gameObject.AddComponent<CharacterRuneTab>(); // 컴포넌트 추가
            tab.onChanged = changedCallback; // 갱신 콜백
            tab.onStatus = statusCallback; // 상태 콜백
            tab.BuildSlots(leftArea); // 일러스트 둘레 슬롯
            tab.BuildContent(contentRoot); // 목록·상세
            return tab; // 탭 반환
        }

        public void SetVisible(bool visible) // 탭 표시 (슬롯도 함께)
        {
            gameObject.SetActive(visible); // 목록·상세 표시
            slotRoot.SetActive(visible); // 슬롯 표시
        }

        private void BuildSlots(RectTransform leftArea) // 슬롯 5칸 생성
        {
            slotRoot = new GameObject("RuneSlots", typeof(RectTransform)); // 슬롯 묶음
            slotRoot.transform.SetParent(leftArea, false); // 좌측 영역 자식
            RuntimeUiKit.Stretch((RectTransform)slotRoot.transform); // 좌측 영역 채움

            for (int index = 0; index < RuneCatalog.SlotCount; index++) // 슬롯 순회
            {
                int slot = index; // 클릭용 번호
                Button button = RuntimeUiKit.CreateButton(slotRoot.transform, $"RuneSlot_{index + 1}", new Color(0f, 0f, 0f, 0f)); // 투명 클릭 영역
                RuntimeUiKit.SetRect((RectTransform)button.transform, CharacterSlotLayout.GetMin(index), CharacterSlotLayout.GetMax(index)); // 슬롯 배치 (장비 탭과 같은 자리)
                button.onClick.AddListener(() => SelectSlot(slot)); // 슬롯 선택
                slotIcons[index] = RuneIconView.Create(button.transform, "Icon"); // 아이콘
                RuntimeUiKit.Stretch(slotIcons[index].rectTransform); // 칸 채움
                slotOutlines[index] = slotIcons[index].gameObject.AddComponent<Outline>(); // 선택 테두리
                slotOutlines[index].effectColor = SelectedColor; // 금색
                slotOutlines[index].effectDistance = new Vector2(3f, -3f); // 두께
                slotLocks[index] = RuntimeUiKit.CreateText(button.transform, "Lock", string.Empty, 13, new Color(0.85f, 0.85f, 0.88f, 1f)).Wrap(); // 잠금 문구
                RuntimeUiKit.Stretch(slotLocks[index].rectTransform, 4f); // 칸 채움
                slotStars[index] = RuntimeUiKit.CreateText(slotRoot.transform, $"Stars_{index + 1}", string.Empty, 18, new Color(1f, 0.78f, 0.20f, 1f)).Overflow().Outlined(new Color(0.3f, 0.2f, 0f, 0.6f), new Vector2(1f, -1f)); // 별 표시
                RuntimeUiKit.SetRect(slotStars[index].rectTransform, CharacterSlotLayout.GetCaptionMin(index), CharacterSlotLayout.GetCaptionMax(index)); // 칸 아래 배치
            }
        }

        private void BuildContent(RectTransform root) // 목록·필터·상세·버튼 생성
        {
            BuildList(root); // 보유 룬 목록

            for (int index = 0; index < FilterLabels.Length; index++) // 필터 버튼
            {
                int value = index; // 클릭용 번호
                Button button = CreateButton(root, $"Filter_{index}", FilterLabels[index], new Color(0.90f, 0.90f, 0.92f, 1f)); // 필터 버튼
                float left = 0.28f + (index * 0.14f); // 가로 위치
                RuntimeUiKit.SetRect((RectTransform)button.transform, new Vector2(left, 0.91f), new Vector2(left + 0.13f, 0.985f)); // 배치
                button.onClick.AddListener(() => SetFilter(value)); // 필터 전환
                filterButtons.Add(button); // 등록
            }

            Image box = RuntimeUiKit.CreateImage(root, "DetailBox", new Color(0.86f, 0.86f, 0.88f, 1f)); // 상세 상자 (목업 회색 칸)
            RuntimeUiKit.SetRect(box.rectTransform, new Vector2(0.28f, 0.30f), new Vector2(0.98f, 0.895f)); // 배치
            box.gameObject.AddComponent<Outline>().effectColor = new Color(0.3f, 0.3f, 0.35f, 0.6f); // 테두리
            detailIcon = RuneIconView.Create(box.transform, "DetailIcon"); // 큰 아이콘
            RuntimeUiKit.SetRect(detailIcon.rectTransform, new Vector2(0.03f, 0.56f), new Vector2(0.22f, 0.96f)); // 배치
            nameText = CreateLabel(box.transform, "Name", 26, FontStyle.Bold, new Vector2(0.25f, 0.83f), new Vector2(0.98f, 0.96f)); // 이름
            gradeText = CreateLabel(box.transform, "Grade", 20, FontStyle.Bold, new Vector2(0.25f, 0.71f), new Vector2(0.98f, 0.83f)); // 등급·레벨
            gradeText.color = new Color(0.75f, 0.52f, 0.05f, 1f); // 금색
            effectText = CreateLabel(box.transform, "Effect", 19, FontStyle.Normal, new Vector2(0.25f, 0.56f), new Vector2(0.98f, 0.71f)); // 효과
            nextText = CreateLabel(box.transform, "Next", 16, FontStyle.Normal, new Vector2(0.03f, 0.30f), new Vector2(0.98f, 0.54f)); // 다음 레벨·비용
            stateText = CreateLabel(box.transform, "State", 15, FontStyle.Normal, new Vector2(0.03f, 0.03f), new Vector2(0.98f, 0.30f)); // 상태
            stateText.color = new Color(0.30f, 0.30f, 0.38f, 1f); // 회색

            equipButton = CreateAction(root, "Equip", "장착", 0, Equip); // 장착
            unequipButton = CreateAction(root, "Unequip", "해제", 1, Unequip); // 해제
            enhanceButton = CreateAction(root, "Enhance", "강화", 2, Enhance); // 강화
            synthButton = CreateAction(root, "Synthesize", "합성", 3, Synthesize); // 합성
            dismantleButton = CreateAction(root, "Dismantle", "분해", 4, Dismantle); // 분해
            lockButton = CreateAction(root, "Lock", "잠금", 5, ToggleLock); // 잠금
            footerText = CreateLabel(root, "Footer", 15, FontStyle.Normal, new Vector2(0.28f, 0.10f), new Vector2(0.98f, 0.185f)); // 추천·재료
            Button dev = CreateButton(root, "DevGrantRunes", "테스트 룬 지급", new Color(0.88f, 0.83f, 0.66f, 1f)); // 테스트 지급
            RuntimeUiKit.SetRect((RectTransform)dev.transform, new Vector2(0.015f, 0.02f), new Vector2(0.26f, 0.09f)); // 배치
            dev.onClick.AddListener(GrantDemoRunes); // 연결
            DevelopmentFeatures.HideInRelease(dev); // 출시 빌드 숨김
        }

        private void BuildList(RectTransform root) // 보유 룬 스크롤 목록 (2열)
        {
            GameObject scroll = new GameObject("RuneList", typeof(RectTransform), typeof(Image), typeof(ScrollRect)); // 스크롤
            scroll.transform.SetParent(root, false); // 부모 연결
            scroll.GetComponent<Image>().color = new Color(0.86f, 0.86f, 0.88f, 1f); // 회색 바탕
            RuntimeUiKit.SetRect((RectTransform)scroll.transform, new Vector2(0.015f, 0.10f), new Vector2(0.26f, 0.985f)); // 좌측 배치
            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask)); // 뷰포트
            viewport.transform.SetParent(scroll.transform, false); // 부모 연결
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f); // 마스크 바탕
            viewport.GetComponent<Mask>().showMaskGraphic = false; // 마스크 숨김
            RuntimeUiKit.Stretch((RectTransform)viewport.transform, 6f); // 여백
            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter)); // 목록 내용
            content.transform.SetParent(viewport.transform, false); // 부모 연결
            listContent = (RectTransform)content.transform; // 저장
            listContent.anchorMin = new Vector2(0f, 1f); // 위쪽 기준
            listContent.anchorMax = new Vector2(1f, 1f); // 가로 채움
            listContent.pivot = new Vector2(0.5f, 1f); // 위쪽 피벗
            listContent.sizeDelta = Vector2.zero; // 크기 초기화
            GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>(); // 격자
            grid.cellSize = new Vector2(96f, 116f); // 칸 크기
            grid.spacing = new Vector2(6f, 6f); // 간격
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; // 열 고정
            grid.constraintCount = 2; // 2열
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize; // 세로 자동
            ScrollRect rect = scroll.GetComponent<ScrollRect>(); // 스크롤 조회
            rect.viewport = (RectTransform)viewport.transform; // 뷰포트
            rect.content = listContent; // 내용
            rect.horizontal = false; // 가로 끔
            rect.movementType = ScrollRect.MovementType.Clamped; // 범위 제한
            rect.scrollSensitivity = 30f; // 감도
        }

        public void Refresh(SaveData saveData, DataManager dataManager, CharacterSaveData characterSave, CharacterData characterData) // 탭 갱신
        {
            if (!gameObject.activeInHierarchy || saveData == null || characterSave == null) // 표시·데이터 확인
            {
                return; // 숨겨져 있으면 생략
            }

            if (characterId != characterSave.CharacterId) // 캐릭터 변경 확인
            {
                characterId = characterSave.CharacterId; // 캐릭터 저장
                selectedSlot = -1; // 슬롯 선택 초기화
            }

            RuneInstanceSaveData[] equipped = RuneService.GetEquipped(saveData, characterId); // 장착 룬

            for (int index = 0; index < RuneCatalog.SlotCount; index++) // 슬롯 갱신
            {
                bool unlocked = RuneService.IsSlotUnlocked(saveData, dataManager, characterId, index); // 해금 여부
                RuneIconView.Apply(slotIcons[index], unlocked ? equipped[index] : null); // 아이콘
                slotIcons[index].color = unlocked ? new Color(0.10f, 0.10f, 0.14f, 1f) : new Color(0.25f, 0.25f, 0.28f, 1f); // 잠김 회색
                slotLocks[index].text = unlocked ? (equipped[index] == null ? $"{index + 1}" : string.Empty) : $"잠김\n{RuneCatalog.GetSlotRequirement(index)}"; // 빈 칸 번호 또는 잠금 조건
                slotStars[index].text = unlocked && equipped[index] != null ? RuneCatalog.GetStars(equipped[index].Grade) : string.Empty; // 별
                slotOutlines[index].enabled = index == selectedSlot; // 선택 테두리
            }

            RefreshList(saveData); // 목록 갱신
            RefreshDetail(saveData, dataManager, characterData); // 상세 갱신

            for (int index = 0; index < filterButtons.Count; index++) // 필터 강조
            {
                filterButtons[index].GetComponent<Image>().color = index == filter ? SelectedColor : new Color(0.90f, 0.90f, 0.92f, 1f); // 선택 금색
            }
        }

        private void RefreshList(SaveData saveData) // 보유 룬 목록 다시 만들기
        {
            for (int index = listContent.childCount - 1; index >= 0; index--) Destroy(listContent.GetChild(index).gameObject); // 기존 칸 제거

            List<RuneInstanceSaveData> runes = new List<RuneInstanceSaveData>(saveData.RuneInventory); // 룬 복사
            runes.Sort((a, b) => b.Grade != a.Grade ? b.Grade.CompareTo(a.Grade) : a.Kind != b.Kind ? a.Kind.CompareTo(b.Kind) : b.Level.CompareTo(a.Level)); // 등급 → 종류 → 레벨 순

            foreach (RuneInstanceSaveData rune in runes) // 룬 순회
            {
                if (filter > 0 && (int)RuneCatalog.Get(rune.Kind).Category != filter - 1) continue; // 필터 제외
                string runeId = rune.InstanceId; // 클릭용 ID
                Button tile = RuntimeUiKit.CreateButton(listContent, $"Rune_{runeId}", new Color(0.97f, 0.97f, 0.98f, 1f)); // 칸
                tile.onClick.AddListener(() => SelectRune(runeId)); // 선택
                if (runeId == selectedRuneId) tile.gameObject.AddComponent<Outline>().effectColor = SelectedColor; // 선택 강조
                Image icon = RuneIconView.Create(tile.transform, "Icon"); // 아이콘
                RuntimeUiKit.SetRect(icon.rectTransform, new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.98f)); // 위쪽 배치
                RuneIconView.Apply(icon, rune); // 룬 적용
                string tag = rune.IsEquipped ? (rune.EquippedCharacterId == characterId ? " 장착" : " 사용중") : rune.Locked ? " 잠금" : string.Empty; // 상태 표시
                Text label = RuntimeUiKit.CreateText(tile.transform, "Label", $"<color=#C98A00>{RuneCatalog.GetStars(rune.Grade)}</color>\nLv.{rune.Level}{tag}", 13, new Color(0.15f, 0.15f, 0.2f, 1f)).Overflow(); // 별·레벨
                label.supportRichText = true; // 색 태그
                RuntimeUiKit.SetRect(label.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.34f)); // 아래쪽 배치
            }
        }

        private void RefreshDetail(SaveData saveData, DataManager dataManager, CharacterData characterData) // 선택 룬 상세
        {
            RuneInstanceSaveData rune = string.IsNullOrEmpty(selectedRuneId) ? null : saveData.FindRune(selectedRuneId); // 선택 룬
            RuneIconView.Apply(detailIcon, rune); // 큰 아이콘
            int shards = ItemInventoryService.GetCount(saveData, RuneCatalog.ShardItemId); // 룬 조각
            int gold = GoldCurrencyService.GetGold(saveData); // 골드
            string recommend = characterData == null ? string.Empty : string.Join(" · ", Array.ConvertAll(RuneCatalog.GetRecommended(characterData.Position), kind => RuneCatalog.Get(kind).Name.Replace("의 룬", string.Empty))); // 추천 룬
            footerText.text = $"추천 : {recommend}      보유 : 룬 조각 {shards} · {gold:N0}G"; // 추천·재료

            if (rune == null) // 선택 없음
            {
                nameText.text = "룬을 선택하세요"; // 안내
                gradeText.text = string.Empty; // 비움
                effectText.text = "왼쪽 목록에서 룬을 고르고, 일러스트 옆 슬롯을 눌러 장착할 칸을 정하세요."; // 사용법
                nextText.text = "룬 상자는 상점에서 사고 가방에서 열 수 있어요. 던전에서도 룬 상자와 룬 조각이 나와요."; // 획득 안내
                stateText.text = selectedSlot >= 0 ? $"선택한 슬롯 : {selectedSlot + 1}번" : string.Empty; // 슬롯 안내
                SetActions(false, false, false, false, false, false); // 버튼 비활성
                return; // 처리 종료
            }

            RuneDefinition definition = RuneCatalog.Get(rune.Kind); // 룬 정의
            bool maxed = rune.Level >= RuneCatalog.MaxLevel; // 최대 강화
            RuneInstanceSaveData partner = RuneService.FindSynthesisPartner(saveData, rune); // 합성 짝
            nameText.text = definition.Name + (definition.Unique ? "  <size=16>(캐릭터당 1개)</size>" : string.Empty); // 이름
            nameText.supportRichText = true; // 크기 태그
            gradeText.text = $"{RuneCatalog.GetStars(rune.Grade)}  Lv.{rune.Level}{(maxed ? " (MAX)" : string.Empty)}"; // 등급·레벨
            effectText.text = RuneCatalog.Describe(rune.Kind, rune.Grade, rune.Level); // 효과
            nextText.text = maxed ? "최대 강화에 도달했습니다." : $"다음 Lv.{rune.Level + 1} : {RuneCatalog.Describe(rune.Kind, rune.Grade, rune.Level + 1)}\n강화 비용 : 룬 조각 {RuneCatalog.GetEnhanceShards(rune.Grade, rune.Level)} · {RuneCatalog.GetEnhanceGold(rune.Grade, rune.Level)}G"; // 다음 레벨
            string where = rune.IsEquipped ? $"장착 : {ResolveName(rune.EquippedCharacterId)} {rune.SlotIndex + 1}번 슬롯" : "미장착"; // 장착 위치
            string synth = rune.Grade >= RuneCatalog.MaxGrade ? "합성 : ★3은 최고 등급" : partner == null ? "합성 : 같은 종류·등급 룬이 1개 더 필요" : $"합성 : 짝 있음 (Lv.{partner.Level}) · {RuneCatalog.GetSynthesisGold(rune.Grade)}G → {RuneCatalog.GetStars(rune.Grade + 1)}"; // 합성 안내
            stateText.text = $"{where}{(rune.Locked ? " · 잠금" : string.Empty)}\n{synth}\n분해 : 룬 조각 {RuneCatalog.GetDismantleShards(rune.Grade, rune.Level)}{(selectedSlot >= 0 ? $"\n선택한 슬롯 : {selectedSlot + 1}번" : string.Empty)}"; // 상태
            bool free = !rune.Locked && !rune.IsEquipped; // 재료로 쓸 수 있는지
            SetActions(true, rune.IsEquipped, !maxed, partner != null && free && rune.Grade < RuneCatalog.MaxGrade, free, true); // 버튼 활성
            SetLabel(lockButton, rune.Locked ? "잠금 해제" : "잠금"); // 잠금 문구
            SetLabel(equipButton, selectedSlot >= 0 ? $"{selectedSlot + 1}번에 장착" : "장착"); // 장착 문구
        }

        private void SetFilter(int value) // 필터 변경
        {
            filter = value; // 필터 저장
            onChanged?.Invoke(); // 갱신
        }

        private void SelectSlot(int slot) // 슬롯 선택 (장착된 룬이 있으면 상세 표시)
        {
            selectedSlot = slot; // 슬롯 저장
            SaveData saveData = CurrentSave(); // 저장 데이터
            RuneInstanceSaveData[] equipped = RuneService.GetEquipped(saveData, characterId); // 장착 룬
            if (equipped[slot] != null) selectedRuneId = equipped[slot].InstanceId; // 장착 룬 선택
            onChanged?.Invoke(); // 갱신
        }

        private void SelectRune(string runeId) // 룬 선택
        {
            selectedRuneId = runeId; // 선택 저장
            onChanged?.Invoke(); // 갱신
        }

        private void Equip() // 장착 (슬롯 미선택 시 첫 번째 빈 해금 칸)
        {
            SaveData saveData = CurrentSave(); // 저장 데이터
            DataManager dataManager = GameManager.Instance == null ? null : GameManager.Instance.Data; // 데이터 관리자
            int slot = selectedSlot >= 0 ? selectedSlot : FindEmptySlot(saveData, dataManager); // 대상 슬롯

            if (slot < 0) // 빈 칸 확인
            {
                onStatus?.Invoke("빈 슬롯이 없습니다. 슬롯을 눌러 교체할 칸을 고르세요."); // 안내
                return; // 중단
            }

            Commit(RuneService.TryEquip(saveData, dataManager, characterId, selectedRuneId, slot, out string message), message); // 장착
        }

        private int FindEmptySlot(SaveData saveData, DataManager dataManager) // 첫 빈 해금 슬롯
        {
            RuneInstanceSaveData[] equipped = RuneService.GetEquipped(saveData, characterId); // 장착 룬

            for (int index = 0; index < RuneCatalog.SlotCount; index++) // 슬롯 순회
            {
                if (equipped[index] == null && RuneService.IsSlotUnlocked(saveData, dataManager, characterId, index)) return index; // 빈 해금 칸
            }

            return -1; // 없음
        }

        private void Unequip() => Commit(RuneService.TryUnequip(CurrentSave(), selectedRuneId, out string message), message); // 해제
        private void Enhance() => Commit(RuneService.TryEnhance(CurrentSave(), GameManager.Instance.Data, selectedRuneId, out string message), message); // 강화

        private void Synthesize() // 합성 (짝 자동 선택)
        {
            SaveData saveData = CurrentSave(); // 저장 데이터
            RuneInstanceSaveData partner = RuneService.FindSynthesisPartner(saveData, saveData?.FindRune(selectedRuneId)); // 합성 짝
            bool done = RuneService.TrySynthesize(saveData, GameManager.Instance.Data, selectedRuneId, partner?.InstanceId, out RuneInstanceSaveData result, out string message); // 합성
            if (done) selectedRuneId = result.InstanceId; // 결과 룬 선택
            Commit(done, message); // 반영
        }

        private void Dismantle() // 분해
        {
            bool done = RuneService.TryDismantle(CurrentSave(), GameManager.Instance.Data, selectedRuneId, out string message); // 분해
            if (done) selectedRuneId = string.Empty; // 선택 해제
            Commit(done, message); // 반영
        }

        private void ToggleLock() // 잠금 전환
        {
            bool locked = RuneService.ToggleLock(CurrentSave(), selectedRuneId); // 전환
            Commit(true, locked ? "잠금 : 합성·분해 재료로 쓰이지 않습니다." : "잠금 해제"); // 반영
        }

        private void GrantDemoRunes() // 테스트 룬 3개 + 룬 조각 20 (개발 빌드 전용)
        {
            SaveData saveData = CurrentSave(); // 저장 데이터
            if (saveData == null) return; // 확인
            System.Random random = new System.Random(); // 난수

            for (int count = 0; count < 3; count++) // 3개 지급
            {
                RuneService.Grant(saveData, (RuneKind)random.Next(RuneCatalog.KindCount), 1 + random.Next(2)); // ★1~★2 무작위
            }

            ItemInventoryService.TryAdd(saveData, GameManager.Instance.Data, RuneCatalog.ShardItemId, 20, out _); // 조각 20
            GoldCurrencyService.AddGold(saveData, 1000); // 골드 1000
            Commit(true, "테스트 룬 3개 · 룬 조각 20 · 1000G 지급"); // 반영
        }

        private void Commit(bool success, string message) // 결과 저장·갱신
        {
            bool saved = success && GameManager.Instance != null && GameManager.Instance.Save.SaveCurrent(); // 성공 시 저장
            onStatus?.Invoke(success && !saved ? $"{message} (저장 실패)" : message); // 결과 안내
            onChanged?.Invoke(); // 능력치 포함 화면 갱신
        }

        private static SaveData CurrentSave() => GameManager.Instance == null || GameManager.Instance.Save == null ? null : GameManager.Instance.Save.CurrentSave; // 현재 저장 데이터

        private static string ResolveName(string id) // 캐릭터 이름
        {
            CharacterData data = GameManager.Instance == null || GameManager.Instance.Data == null ? null : GameManager.Instance.Data.GetCharacter(id); // 원본 조회
            return data == null ? id : data.DisplayName; // 이름 반환
        }

        private void SetActions(bool equip, bool unequip, bool enhance, bool synth, bool dismantle, bool lockable) // 버튼 활성 설정
        {
            equipButton.interactable = equip; // 장착
            unequipButton.interactable = unequip; // 해제
            enhanceButton.interactable = enhance; // 강화
            synthButton.interactable = synth; // 합성
            dismantleButton.interactable = dismantle; // 분해
            lockButton.interactable = lockable; // 잠금
        }

        private Button CreateAction(RectTransform root, string name, string label, int index, UnityEngine.Events.UnityAction action) // 하단 동작 버튼
        {
            Button button = CreateButton(root, name, label, new Color(0.80f, 0.86f, 0.96f, 1f)); // 버튼 생성
            float left = 0.28f + (index * 0.1175f); // 가로 위치
            RuntimeUiKit.SetRect((RectTransform)button.transform, new Vector2(left, 0.195f), new Vector2(left + 0.11f, 0.285f)); // 배치
            button.onClick.AddListener(action); // 연결
            return button; // 반환
        }

        private static Button CreateButton(Transform parent, string name, string label, Color color) // 기본 버튼
        {
            Button button = RuntimeUiKit.CreateButton(parent, name, color); // 버튼 생성
            Text text = RuntimeUiKit.CreateText(button.transform, "Label", label, 16, new Color(0.10f, 0.10f, 0.12f, 1f)).BestFit(11); // 라벨
            RuntimeUiKit.Stretch(text.rectTransform, 4f); // 확장
            return button; // 반환
        }

        private static Text CreateLabel(Transform parent, string name, int size, FontStyle style, Vector2 min, Vector2 max) // 왼쪽 정렬 문구
        {
            Text text = RuntimeUiKit.CreateText(parent, name, string.Empty, size, new Color(0.12f, 0.12f, 0.16f, 1f), style, TextAnchor.UpperLeft).Wrap(); // 문구 생성
            RuntimeUiKit.SetRect(text.rectTransform, min, max); // 배치
            return text; // 반환
        }

        private static void SetLabel(Button button, string label) // 버튼 문구 변경
        {
            Text text = button.GetComponentInChildren<Text>(true); // 문구 조회
            if (text != null) text.text = label; // 적용
        }
    }
}
