namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    public static class HeroNameService // 주인공 이름 (Day68 신규 — 새 게임에서 정하고 모든 대사에 반영)
    {
        public const string DefaultName = "용사"; // 기본 이름 (입력하지 않으면 이 이름)
        public const int MaxLength = 8; // 최대 글자 수
        public const string Placeholder = "{HERO}"; // 대사 속 이름 자리

        public static string Get(SaveData saveData) => saveData == null || string.IsNullOrWhiteSpace(saveData.HeroName) ? DefaultName : saveData.HeroName; // 현재 이름 (없으면 기본 이름)

        public static bool NeedsInput(SaveData saveData) => saveData != null && string.IsNullOrWhiteSpace(saveData.HeroName); // 이름을 아직 정하지 않았는지

        public static string Sanitize(string name) // 입력 이름 정리 (앞뒤 공백 제거 · 길이 제한 · 비었으면 기본 이름)
        {
            string trimmed = (name ?? string.Empty).Trim(); // 공백 제거
            if (trimmed.Length == 0) return DefaultName; // 비었으면 기본 이름
            return trimmed.Length > MaxLength ? trimmed.Substring(0, MaxLength) : trimmed; // 길이 제한
        }

        public static string TrySetName(SaveData saveData, string name) // 이름 저장 (안내 문구 반환)
        {
            if (saveData == null) return "저장 데이터를 찾을 수 없습니다."; // 입력 확인
            string resolved = Sanitize(name); // 정리
            saveData.SetHeroName(resolved); // 저장
            return $"{resolved} · 이름이 정해졌습니다."; // 안내
        }

        public static string Apply(string text, SaveData saveData) // 대사 속 {HERO}를 실제 이름으로 교체
        {
            return string.IsNullOrEmpty(text) || !text.Contains(Placeholder) ? text : text.Replace(Placeholder, Get(saveData)); // 교체 결과 반환
        }
    }
}
