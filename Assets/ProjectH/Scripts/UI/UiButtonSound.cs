using ProjectH.Core; // 소리 이름표 기능
using UnityEngine; // 컴포넌트 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 한 버튼에 하나만
    public sealed class UiButtonSound : MonoBehaviour // 버튼 소리 표시 (Day88 신규 — 이 컴포넌트가 붙은 버튼은 누를 때 Key 소리가 난다. 붙어 있으면 다시 붙이지 않아 소리가 두 번 나지 않는다)
    {
        public string Key = AudioCatalog.SfxClick; // 누를 때 날 소리 (빈 값이면 소리 없음)
    }
}
