namespace ProjectH.UI // 프로젝트 UI 영역
{
    public readonly struct TitleScreenViewState // 타이틀 화면 표시 상태
    {
        public string StatusText { get; } // 상태 문구 반환
        public bool CanContinue { get; } // 이어하기 가능 여부 반환

        private TitleScreenViewState(string statusText, bool canContinue) // 타이틀 화면 상태 생성
        {
            StatusText = statusText; // 상태 문구 저장
            CanContinue = canContinue; // 이어하기 가능 여부 저장
        }

        public static TitleScreenViewState Build(bool hasSaveData) // 저장 상태 기반 화면 상태 생성
        {
            if (hasSaveData) // 저장 존재 확인
            {
                return new TitleScreenViewState("저장된 여정이 있습니다.", true); // 저장 있음 화면 상태 반환
            }

            return new TitleScreenViewState("새로운 여정을 시작해 주세요.", false); // 저장 없음 화면 상태 반환
        }
    }
}
