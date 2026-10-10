using ProjectH.Core; // 소리 재생 · 이름표 기능
using UnityEngine.UI; // Button 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class UiSound // 화면 소리 공용 기능 (Day88 신규 — 버튼 소리 붙이기 · 결과 소리 · NPC 대사 상황별 소리)
    {
        public const string Silent = ""; // 소리 없음

        public static void Play(string key) // 효과음 한 번
        {
            AudioService.PlaySfx(key); // 재생 (재생기가 없는 편집 모드 테스트에서는 아무 일도 없다)
        }

        public static void Result(bool success) // 행동 결과 소리 (됨 = 확인 · 안 됨 = 실패)
        {
            Result(success, AudioCatalog.SfxConfirm); // 기본 성공 소리
        }

        public static void Result(bool success, string successKey) // 행동 결과 소리 (성공 소리를 고를 때)
        {
            Play(success ? successKey : AudioCatalog.SfxError); // 성공 · 실패
        }

        public static UiButtonSound Attach(Button button) // 버튼에 누르는 소리 붙이기 (이름을 보고 소리를 고른다. 이미 붙어 있으면 그대로 둔다)
        {
            return Attach(button, null); // 이름 기준
        }

        public static UiButtonSound Attach(Button button, string key) // 버튼에 소리 붙이기 (key를 주면 그 소리로 바꾼다. 한 버튼에 한 번만 붙어 소리가 두 번 나지 않는다)
        {
            if (button == null) return null; // 대상 없음
            UiButtonSound sound = button.GetComponent<UiButtonSound>(); // 이미 붙은 표시

            if (sound == null) // 처음 붙임
            {
                sound = button.gameObject.AddComponent<UiButtonSound>(); // 표시 추가
                sound.Key = GetDefaultKey(button.gameObject.name); // 이름으로 고른 소리
                UiButtonSound captured = sound; // 누를 때 읽을 표시
                button.onClick.AddListener(() => Play(captured.Key)); // 누를 때 그 순간의 Key 소리 (나중에 Key를 바꿔도 반영된다)
            }

            if (key != null) sound.Key = key; // 소리 지정
            return sound; // 표시 반환
        }

        public static string GetDefaultKey(string objectName) // 버튼 이름으로 소리 고르기 (닫기 · 뒤로 = 취소 / 탭 · 넘기기 = 페이지 / 확인 = 확인 / 화면 전체를 덮는 투명 버튼 = 무음)
        {
            string name = objectName ?? string.Empty; // 이름
            if (name.Contains("Catcher") || name == "Dim") return Silent; // 화면 전체를 덮는 투명 버튼 (대사 넘김 · 건너뛰기 — 그 동작이 따로 소리를 낸다)
            if (HasWord(name, "Close") || HasWord(name, "Back") || HasWord(name, "Cancel") || HasWord(name, "No") || name.Contains("닫기")) return AudioCatalog.SfxCancel; // 닫기 · 뒤로 · 취소 · 아니오
            if (HasWord(name, "Confirm") || HasWord(name, "Yes")) return AudioCatalog.SfxConfirm; // 확인 · 예
            if (HasWord(name, "Tab") || HasWord(name, "Filter") || HasWord(name, "Next") || HasWord(name, "Prev") || HasWord(name, "Previous") || HasWord(name, "Page") || HasWord(name, "Nav")) return AudioCatalog.SfxPage; // 탭 · 필터 · 앞뒤 넘기기 · 내비
            return AudioCatalog.SfxClick; // 그 밖의 버튼
        }

        public static string GetNpcLineKey(NpcLineKind kind) // 상점 · 대장간에서 NPC가 말하는 상황별 소리 (대사가 바뀌는 그 순간에 결과가 정해진다)
        {
            switch (kind) // 상황 분기
            {
                case NpcLineKind.Buy: // 구매 성공
                    return AudioCatalog.SfxItem; // 아이템 획득
                case NpcLineKind.Sell: // 판매
                    return AudioCatalog.SfxGold; // 골드 획득
                case NpcLineKind.Reroll: // 오늘의 상품 새로고침
                    return AudioCatalog.SfxPage; // 넘김
                case NpcLineKind.EnhanceSuccess: // 강화 성공
                case NpcLineKind.TranscendSuccess: // 초월 성공
                    return AudioCatalog.SfxEnhanceSuccess; // 성공
                case NpcLineKind.EnhanceFail: // 강화 실패
                    return AudioCatalog.SfxEnhanceFail; // 실패
                case NpcLineKind.SoldOut: // 품절
                case NpcLineKind.NoGold: // 골드 부족
                case NpcLineKind.NeedMaterial: // 재료 부족
                    return AudioCatalog.SfxError; // 할 수 없음
                default: // 인사 · 설명 · 안내
                    return Silent; // 소리 없음
            }
        }

        private static bool HasWord(string name, string word) // 이름에 그 낱말이 들어 있는지 (PascalCase 낱말 단위 — "Background"의 "Back", "Notice"의 "No"는 아니다)
        {
            int index = name.IndexOf(word, System.StringComparison.Ordinal); // 낱말 위치 (대소문자 구분)

            while (index >= 0) // 찾은 위치마다 확인
            {
                int end = index + word.Length; // 낱말 끝 (찾는 낱말은 대문자로 시작하므로 앞쪽은 그 자체가 경계다)
                if (end >= name.Length || !char.IsLower(name[end])) return true; // 뒤에 소문자가 이어지지 않으면 낱말 단위 (이어지면 더 긴 낱말의 일부)
                index = name.IndexOf(word, end, System.StringComparison.Ordinal); // 다음 위치
            }

            return false; // 없음
        }
    }
}
