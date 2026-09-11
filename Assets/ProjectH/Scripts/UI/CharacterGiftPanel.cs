using System; // 콜백 델리게이트 기능
using System.Collections.Generic; // 목록 자료형
using ProjectH.Core; // 전역 게임 관리자 기능
using ProjectH.Data; // 아이템 데이터 기능
using ProjectH.SaveSystem; // 선물·호감도 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 중복 패널 방지
    public sealed class CharacterGiftPanel : MonoBehaviour // 캐릭터 창 선물하기 패널 (Day57 신규, CharacterAffinityRewardPanel과 같은 구조)
    {
        private static readonly Color TitleColor = new Color(0.36f, 0.20f, 0.30f, 1f); // 제목 색상
        private static readonly Color LoveColor = new Color(1f, 0.80f, 0.86f, 1f); // 아주 좋아함 분홍색
        private static readonly Color LikeColor = new Color(0.84f, 0.94f, 0.82f, 1f); // 좋아함 연두색
        private static readonly Color NormalColor = new Color(0.94f, 0.93f, 0.90f, 1f); // 보통 베이지색
        private static readonly Color DislikeColor = new Color(0.82f, 0.86f, 0.92f, 1f); // 별로 회청색
        private static readonly Color UnknownColor = new Color(0.90f, 0.90f, 0.92f, 1f); // 모르는 취향 회색

        private readonly List<Button> giftButtons = new List<Button>(); // 선물 버튼 목록
        private readonly List<string> giftOrder = new List<string>(); // 버튼 순서별 선물 ID
        private Text titleText; // 패널 제목
        private Text dailyText; // 오늘 선물 횟수 문구
        private Text legendText; // 취향 증가량 안내 문구
        private Text resultText; // 선물 결과 문구
        private Button rewardShortcutButton; // 호감도 보상 바로가기 버튼
        private string boundCharacterId = string.Empty; // 현재 표시 캐릭터 ID
        private Action onChanged; // 선물 후 화면 갱신 콜백
        private Action onOpenReward; // 호감도 보상 패널 열기 콜백

        public bool IsOpen => gameObject.activeSelf; // 패널 열림 여부 반환

        public static CharacterGiftPanel Create(Transform parent, Action changedCallback, Action openRewardCallback) // 패널 생성
        {
            Image panel = RuntimeUiKit.CreateImage(parent, "GiftPanel", new Color(0.98f, 0.97f, 0.95f, 0.98f)); // 패널 배경 생성
            RuntimeUiKit.SetRect(panel.rectTransform, new Vector2(0.18f, 0.10f), new Vector2(0.82f, 0.90f)); // 화면 중앙 배치
            panel.gameObject.AddComponent<Outline>().effectColor = new Color(0.80f, 0.45f, 0.55f, 0.8f); // 분홍 외곽선 적용
            CharacterGiftPanel view = panel.gameObject.AddComponent<CharacterGiftPanel>(); // 패널 컴포넌트 추가
            view.onChanged = changedCallback; // 갱신 콜백 저장
            view.onOpenReward = openRewardCallback; // 보상 바로가기 콜백 저장
            view.Build(); // 구성 요소 생성
            panel.gameObject.SetActive(false); // 초기 숨김
            return view; // 패널 반환
        }

        private void Build() // 패널 구성 요소 생성
        {
            titleText = RuntimeUiKit.CreateText(transform, "Title", "선물하기", 22, TitleColor, FontStyle.Bold, TextAnchor.MiddleLeft); // 제목 생성
            RuntimeUiKit.SetRect(titleText.rectTransform, new Vector2(0.04f, 0.88f), new Vector2(0.80f, 0.97f)); // 제목 배치
            Button closeButton = CreateButton("CloseButton", "닫기", new Color(0.88f, 0.88f, 0.90f, 1f)); // 닫기 버튼 생성
            RuntimeUiKit.SetRect((RectTransform)closeButton.transform, new Vector2(0.84f, 0.89f), new Vector2(0.97f, 0.97f)); // 닫기 버튼 배치
            closeButton.onClick.AddListener(Toggle); // 닫기 이벤트 연결
            dailyText = RuntimeUiKit.CreateText(transform, "DailyText", string.Empty, 16, new Color(0.20f, 0.35f, 0.55f, 1f), FontStyle.Bold, TextAnchor.MiddleLeft); // 오늘 선물 횟수 문구 생성
            RuntimeUiKit.SetRect(dailyText.rectTransform, new Vector2(0.04f, 0.80f), new Vector2(0.96f, 0.87f)); // 오늘 횟수 배치

            for (int index = 0; index < GiftPreferenceCatalog.AllGiftIds.Count; index++) // 선물 목록 순회
            {
                string itemId = GiftPreferenceCatalog.AllGiftIds[index]; // 선물 ID 조회
                Button giftButton = CreateButton($"Gift_{itemId}", itemId, UnknownColor); // 선물 버튼 생성
                int column = index % 2; // 2열 배치 열 번호
                int row = index / 2; // 2열 배치 행 번호
                float top = 0.78f - (row * 0.165f); // 버튼 세로 위치 계산
                float left = column == 0 ? 0.04f : 0.51f; // 버튼 왼쪽 위치 계산
                RuntimeUiKit.SetRect((RectTransform)giftButton.transform, new Vector2(left, top - 0.15f), new Vector2(left + 0.45f, top)); // 버튼 배치
                giftButton.onClick.AddListener(() => Give(itemId)); // 선물 이벤트 연결
                giftButtons.Add(giftButton); // 버튼 목록 등록
                giftOrder.Add(itemId); // 선물 순서 등록
            }

            legendText = RuntimeUiKit.CreateText(transform, "LegendText", BuildLegend(), 14, new Color(0.35f, 0.30f, 0.30f, 1f), FontStyle.Normal, TextAnchor.MiddleLeft).Wrap(); // 취향 안내 문구 생성
            RuntimeUiKit.SetRect(legendText.rectTransform, new Vector2(0.04f, 0.20f), new Vector2(0.96f, 0.29f)); // 취향 안내 배치
            Button devGrantButton = CreateButton("DevGrantGifts", "테스트 선물 지급", new Color(0.88f, 0.83f, 0.66f, 1f)); // 테스트 선물 지급 버튼 생성
            RuntimeUiKit.SetRect((RectTransform)devGrantButton.transform, new Vector2(0.04f, 0.105f), new Vector2(0.30f, 0.185f)); // 테스트 버튼 배치
            devGrantButton.onClick.AddListener(GrantDemoGifts); // 테스트 지급 이벤트 연결
            DevelopmentFeatures.HideInRelease(devGrantButton); // 출시 빌드에서는 테스트 버튼 숨김
            rewardShortcutButton = CreateButton("RewardShortcut", "받을 수 있는 호감도 보상이 있어요  ▶", new Color(1f, 0.86f, 0.46f, 1f)); // 호감도 보상 바로가기 버튼 생성
            RuntimeUiKit.SetRect((RectTransform)rewardShortcutButton.transform, new Vector2(0.50f, 0.105f), new Vector2(0.96f, 0.185f)); // 바로가기 버튼 배치
            rewardShortcutButton.onClick.AddListener(() => onOpenReward?.Invoke()); // 보상 패널 열기 연결
            rewardShortcutButton.gameObject.SetActive(false); // 초기 숨김
            resultText = RuntimeUiKit.CreateText(transform, "ResultText", string.Empty, 15, new Color(0.55f, 0.35f, 0.10f, 1f)).Wrap(); // 선물 결과 문구 생성
            RuntimeUiKit.SetRect(resultText.rectTransform, new Vector2(0.04f, 0.005f), new Vector2(0.96f, 0.10f)); // 결과 문구 배치
        }

        private Button CreateButton(string name, string label, Color color) // 캐릭터 화면 스타일 버튼 생성 (색 전환 대상 미연결·줄바꿈 라벨)
        {
            Button button = RuntimeUiKit.CreateButton(transform, name, color, false); // 버튼 생성
            Text text = RuntimeUiKit.CreateText(button.transform, "Text", label, 16, new Color(0.10f, 0.10f, 0.10f, 1f)).Wrap(); // 줄바꿈 라벨 생성
            RuntimeUiKit.Stretch(text.rectTransform, 7f); // 라벨 확장
            return button; // 버튼 반환
        }

        public void Toggle() // 패널 열기·닫기
        {
            bool open = !gameObject.activeSelf; // 전환 후 상태 결정
            gameObject.SetActive(open); // 패널 표시 전환
            transform.SetAsLastSibling(); // 최상단 렌더링
            resultText.text = string.Empty; // 결과 문구 초기화

            if (open) // 열림 확인
            {
                onChanged?.Invoke(); // 최신 상태 반영을 위해 화면 갱신 요청
            }
        }

        public void Refresh(SaveData saveData, DataManager dataManager, CharacterSaveData characterSave, string displayName) // 선택 캐릭터 기준 패널 갱신
        {
            if (!IsOpen || saveData == null || characterSave == null) // 표시 및 데이터 확인
            {
                return; // 닫혀 있으면 갱신 생략
            }

            boundCharacterId = characterSave.CharacterId; // 표시 캐릭터 저장
            int affinity = AffinityService.GetAffinity(saveData, boundCharacterId); // 현재 호감도 조회
            AffinityTier tier = AffinityService.GetAffinityTier(saveData, boundCharacterId); // 현재 단계 조회
            int giftsToday = GiftService.GetGiftsGivenToday(saveData, boundCharacterId); // 오늘 선물 횟수 조회
            titleText.text = $"{displayName} · 선물하기   ({AffinityService.GetTierLabel(tier)} {affinity}/{CharacterSaveData.MaxAffinity})"; // 제목 적용
            dailyText.text = BuildDailyLine(affinity, giftsToday, GameTimeService.GetCurrentDay(saveData)); // 오늘 횟수 문구 적용

            for (int index = 0; index < giftButtons.Count; index++) // 선물 버튼 순회
            {
                string itemId = giftOrder[index]; // 선물 ID 조회
                ItemData item = dataManager == null ? null : dataManager.GetItem(itemId); // 선물 원본 조회
                string itemName = item == null ? itemId : item.DisplayName; // 선물 표시 이름 결정
                int count = saveData.GetItemCount(itemId); // 보유 수량 조회
                bool known = GiftService.IsPreferenceKnown(saveData, boundCharacterId, itemId); // 취향 발견 여부
                GiftPreference preference = GiftPreferenceCatalog.GetPreference(boundCharacterId, itemId); // 취향 조회
                string preferenceText = known ? $"{GiftPreferenceCatalog.GetLabel(preference)} (+{GiftPreferenceCatalog.GetAffinityGain(preference)})" : "?"; // 모르면 ? 표시
                SetButtonLabel(giftButtons[index], $"{itemName}   보유 {count}\n취향 : {preferenceText}"); // 버튼 문구 적용
                giftButtons[index].interactable = count > 0; // 보유 중일 때만 입력 허용 (제한·최대는 누르면 사유 안내)
                giftButtons[index].GetComponent<Image>().color = known ? GetPreferenceColor(preference) : UnknownColor; // 취향별 색상 적용
            }

            rewardShortcutButton.gameObject.SetActive(HasClaimableReward(saveData, boundCharacterId)); // 받을 보상이 있을 때만 바로가기 표시
        }

        private static string BuildDailyLine(int affinity, int giftsToday, int currentDay) // 오늘 선물 횟수 문구 생성
        {
            if (affinity >= CharacterSaveData.MaxAffinity) // 호감도 최대 확인
            {
                return "호감도가 최대입니다 · 더 이상 선물을 받지 않아요"; // 최대 안내 반환
            }

            string limitText = giftsToday >= GiftService.DailyGiftLimit ? "  (오늘은 끝! 날짜가 바뀌면 초기화)" : string.Empty; // 제한 도달 안내
            return $"DAY {currentDay} · 오늘 선물 {giftsToday}/{GiftService.DailyGiftLimit}{limitText}"; // 오늘 횟수 문구 반환
        }

        private static string BuildLegend() // 취향 증가량 안내 문구 생성
        {
            return $"취향 : 아주 좋아함 +{GiftPreferenceCatalog.GetAffinityGain(GiftPreference.Love)} · 좋아함 +{GiftPreferenceCatalog.GetAffinityGain(GiftPreference.Like)} · 보통 +{GiftPreferenceCatalog.GetAffinityGain(GiftPreference.Normal)} · 별로 +{GiftPreferenceCatalog.GetAffinityGain(GiftPreference.Dislike)}\n한 번 선물하면 그 캐릭터의 취향을 알게 됩니다. 선물은 상점에서 살 수 있어요."; // 안내 문구 반환
        }

        private static Color GetPreferenceColor(GiftPreference preference) // 취향별 버튼 색상 반환
        {
            switch (preference) // 취향 분기
            {
                case GiftPreference.Love: // 아주 좋아함 처리
                    return LoveColor; // 분홍색 반환
                case GiftPreference.Like: // 좋아함 처리
                    return LikeColor; // 연두색 반환
                case GiftPreference.Dislike: // 별로 처리
                    return DislikeColor; // 회청색 반환
                default: // 보통 처리
                    return NormalColor; // 베이지색 반환
            }
        }

        private static bool HasClaimableReward(SaveData saveData, string characterId) // 받을 수 있는 호감도 보상 존재 여부
        {
            for (int index = 0; index < AffinityRewardCatalog.All.Count; index++) // 단계 보상 순회
            {
                if (AffinityRewardService.GetState(saveData, characterId, AffinityRewardCatalog.All[index].Tier) == AffinityRewardState.Claimable) // 받기 가능 확인
                {
                    return true; // 받을 보상 있음 반환
                }
            }

            return false; // 받을 보상 없음 반환
        }

        private void Give(string itemId) // 선택 선물 주기
        {
            SaveManager saveManager = GameManager.Instance == null ? null : GameManager.Instance.Save; // 저장 관리자 조회
            SaveData saveData = saveManager == null ? null : saveManager.CurrentSave; // 저장 데이터 조회

            if (saveData == null || string.IsNullOrEmpty(boundCharacterId)) // 데이터 및 캐릭터 확인
            {
                resultText.text = "저장 데이터를 찾을 수 없습니다."; // 오류 안내
                return; // 선물 중단
            }

            bool given = GiftService.TryGive(saveData, GameManager.Instance.Data, boundCharacterId, itemId, out GiftResult result); // 선물 시도
            bool saved = given && saveManager.SaveCurrent(); // 성공 시 즉시 저장
            resultText.text = given && !saved ? $"{result.Message} (저장 실패)" : result.Message; // 결과 문구 표시
            onChanged?.Invoke(); // 호감도·버튼 상태 갱신 요청
        }

        private void GrantDemoGifts() // 테스트 선물 각 1개 지급 (개발 빌드 전용)
        {
            SaveManager saveManager = GameManager.Instance == null ? null : GameManager.Instance.Save; // 저장 관리자 조회
            SaveData saveData = saveManager == null ? null : saveManager.CurrentSave; // 저장 데이터 조회
            DataManager dataManager = GameManager.Instance == null ? null : GameManager.Instance.Data; // 데이터 관리자 조회

            if (saveData == null || dataManager == null) // 데이터 확인
            {
                resultText.text = "저장 데이터를 찾을 수 없습니다."; // 오류 안내
                return; // 지급 중단
            }

            int granted = 0; // 지급 성공 개수

            for (int index = 0; index < giftOrder.Count; index++) // 선물 목록 순회
            {
                if (ItemInventoryService.TryAdd(saveData, dataManager, giftOrder[index], 1, out _)) // 선물 1개 지급
                {
                    granted++; // 지급 개수 증가
                }
            }

            bool saved = granted > 0 && saveManager.SaveCurrent(); // 지급 시 저장
            resultText.text = granted == 0 ? "지급할 수 있는 선물이 없습니다. (ItemData 등록 확인)" : saved ? $"테스트 선물 {granted}종을 1개씩 지급했습니다." : "테스트 선물은 지급되었지만 저장에 실패했습니다."; // 지급 결과 표시
            onChanged?.Invoke(); // 화면 갱신 요청
        }

        private static void SetButtonLabel(Button button, string label) // 버튼 문구 변경
        {
            Text text = button.GetComponentInChildren<Text>(true); // 버튼 문구 조회
            if (text != null) text.text = label; // 문구 적용
        }
    }
}
