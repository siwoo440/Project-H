using System.Collections.Generic; // 목록 자료형
using ProjectH.Core; // 게임 관리자 기능
using ProjectH.SaveSystem; // 저장 · 시간대 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Image · Text 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class LobbyBackdropCatalog // 로비 배경 표 (Day80 신규 — 시간대에 따라 배경이 바뀐다)
    {
        public const float DimAlpha = 0.16f; // 로비 배경을 어둡게 덮는 정도 (패널마다 바탕이 있어 전투보다 옅게)

        public static string GetKey(SaveTimeOfDay phase) // 시간대의 배경 키
        {
            switch (phase) // 시간대 분기
            {
                case SaveTimeOfDay.Morning: return "MORNING"; // 아침
                case SaveTimeOfDay.Evening: return "EVENING"; // 저녁
                case SaveTimeOfDay.Night: return "NIGHT"; // 밤
                default: return "DAY"; // 점심 · 기타
            }
        }

        public static string GetLeadCharacterId(SaveData saveData) // 로비에 세울 동료 : 파티 첫 자리 → 없으면 첫 동료
        {
            if (saveData == null) return string.Empty; // 저장 없음
            IReadOnlyList<string> party = saveData.PartyCharacterIds; // 활성 파티

            for (int index = 0; party != null && index < party.Count; index++) // 파티 순회
            {
                if (!string.IsNullOrEmpty(party[index])) return party[index]; // 첫 파티원
            }

            return saveData.Characters != null && saveData.Characters.Count > 0 ? saveData.Characters[0].CharacterId : string.Empty; // 첫 동료
        }
    }

    [DisallowMultipleComponent] // 중복 방지
    public sealed class LobbyBackdropView : MonoBehaviour // 로비 배경과 대표 동료 그림 (Day80 신규 — 시간대와 파티가 바뀌면 따라 바뀐다)
    {
        private const string DimObjectName = "LobbyBackgroundDim"; // 배경을 덮는 막 이름
        private const string StandingObjectName = "HeroStanding"; // 대표 동료 그림 이름

        private Image background; // 로비 배경
        private Image frame; // 씬에 놓인 주인공 그림 틀 ("MAIN CHARACTER" 임시 칸)
        private Image standing; // 대표 동료 스탠딩
        private bool hasAppliedPhase; // 배경을 한 번이라도 적용했는지
        private SaveTimeOfDay appliedPhase; // 마지막으로 적용한 시간대
        private string appliedCharacterId; // 마지막으로 적용한 동료

        public void Configure(Image backgroundImage, Image heroFrame) // 대상 연결
        {
            background = backgroundImage; // 배경
            frame = heroFrame; // 그림 틀
            hasAppliedPhase = false; // 다시 적용
            appliedCharacterId = null; // 다시 적용
            Refresh(); // 즉시 반영
        }

        private void Start() // 첫 프레임 (Day84 추가 — 다른 연결이 내비 버튼을 다 만든 뒤에 아이콘을 올린다)
        {
            LobbyNavIcons.Apply(transform.parent); // 하단 내비 아이콘
        }

        private void Update() // 시간대 · 파티 변화 감시 (값 두 개만 비교한다)
        {
            Refresh(); // 달라졌을 때만 다시 그림
        }

        private void Refresh() // 현재 저장에 맞춰 배경과 동료 그림 갱신
        {
            SaveData saveData = GameManager.Instance == null || GameManager.Instance.Save == null ? null : GameManager.Instance.Save.CurrentSave; // 현재 저장
            if (saveData == null) return; // 저장 없음 (씬을 직접 실행한 경우) — 원래 배경 유지
            SaveTimeOfDay phase = GameTimeService.GetCurrentPhase(saveData); // 현재 시간대

            if (!hasAppliedPhase || phase != appliedPhase) // 시간대가 바뀜
            {
                hasAppliedPhase = true; // 적용 기록
                appliedPhase = phase; // 시간대 기록
                SceneBackdrop.Apply(background, LobbyBackdropCatalog.GetKey(phase), LobbyBackdropCatalog.DimAlpha, DimObjectName); // 시간대 배경
            }

            string characterId = LobbyBackdropCatalog.GetLeadCharacterId(saveData); // 대표 동료
            if (characterId == appliedCharacterId) return; // 변화 없음
            appliedCharacterId = characterId; // 동료 기록
            ApplyStanding(characterId); // 동료 그림
        }

        private void ApplyStanding(string characterId) // 그림 틀 자리에 대표 동료 스탠딩 표시 (정식 그림이 없으면 틀을 그대로 둔다)
        {
            if (frame == null) return; // 그림 틀 없음
            bool placeholder = true; // 임시 그림 여부
            Sprite sprite = string.IsNullOrEmpty(characterId) ? null : DialogueArtFactory.GetStanding(characterId, null, out placeholder); // 기본 표정 스탠딩
            bool hasArt = sprite != null && !placeholder; // 정식 그림 여부

            if (hasArt && standing == null) // 그림 자리 준비
            {
                standing = RuntimeUiKit.CreateImage(frame.transform, StandingObjectName, Color.white); // 스탠딩 이미지
                standing.preserveAspect = true; // 비율 유지
                standing.raycastTarget = false; // 입력 통과
                RuntimeUiKit.Stretch(standing.rectTransform); // 틀 전체
            }

            if (standing != null) // 스탠딩 반영
            {
                standing.sprite = hasArt ? sprite : null; // 그림
                standing.gameObject.SetActive(hasArt); // 표시
            }

            Color frameColor = frame.color; // 틀 색
            frameColor.a = hasArt ? 0f : 1f; // 그림이 있으면 틀은 숨김
            frame.color = frameColor; // 적용

            foreach (Text hint in frame.GetComponentsInChildren<Text>(true)) // 틀 안의 임시 글자 ("MAIN CHARACTER")
            {
                hint.gameObject.SetActive(!hasArt); // 그림이 있으면 숨김
            }
        }
    }

    public static class LobbyBackdropRuntimePatch // 로비 배경 연결 (Day80 신규)
    {
        private const string BackgroundObjectName = "Background"; // 씬에 놓인 배경 오브젝트 이름
        private const string FrameObjectName = "HeroIllustrationFrame"; // 씬에 놓인 주인공 그림 틀 이름

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // 첫 씬 로드 전 이벤트 구독 지정
        private static void Register() // 씬 로드 이벤트 등록
        {
            SceneRuntimePatch.Register(GameScenes.Lobby, OnSceneLoaded); // Lobby 씬 로드 시 배경 연결 등록
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene) // Lobby 씬 로드 완료 처리
        {
            LobbyScreenController lobby = Object.FindFirstObjectByType<LobbyScreenController>(); // 로비 컨트롤러
            if (lobby == null) return; // 로비 화면 없음
            Transform backgroundTransform = lobby.transform.Find(BackgroundObjectName); // 배경
            Image background = backgroundTransform == null ? null : backgroundTransform.GetComponent<Image>(); // 배경 이미지
            if (background == null) return; // 배경 없음
            Transform frameTransform = lobby.transform.Find(FrameObjectName); // 그림 틀
            LobbyBackdropView view = background.GetComponent<LobbyBackdropView>(); // 기존 연결
            if (view == null) view = background.gameObject.AddComponent<LobbyBackdropView>(); // 없으면 추가
            view.Configure(background, frameTransform == null ? null : frameTransform.GetComponent<Image>()); // 대상 연결
        }
    }
}
