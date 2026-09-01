using System.Text; // 문자열 조립 기능
using ProjectH.Core; // 프로젝트 핵심 기능
using ProjectH.Data; // 프로젝트 데이터 기능
using ProjectH.SaveSystem; // 프로젝트 저장 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public enum PrototypeScreenKind // 프로토타입 화면 종류
    {
        Title = 0, // 타이틀 화면
        Lobby = 1, // 로비 화면
        Party = 2, // 파티 화면
        DungeonSelect = 3, // 던전 선택 화면
        Battle = 4, // 전투 화면
        Result = 5 // 결과 화면
    }

    [DisallowMultipleComponent] // 중복 컨트롤러 방지
    public sealed class PrototypeScreenController : MonoBehaviour // 화면 흐름 컨트롤러
    {
        private const string PrototypeDungeonId = "DG_LETICIA_FOREST"; // 프로토타입 던전 ID

        [SerializeField] private PrototypeScreenKind screenKind; // 현재 화면 종류
        [SerializeField] private Text statusText; // 상태 표시 텍스트
        [SerializeField] private Text bodyText; // 본문 텍스트
        [SerializeField] private Text auxiliaryText; // 보조 텍스트
        [SerializeField] private Button continueButton; // 이어하기 버튼

        public void Configure(PrototypeScreenKind kind, Text status, Text body, Text auxiliary, Button continueTarget) // 에디터 화면 참조 설정
        {
            screenKind = kind; // 화면 종류 설정
            statusText = status; // 상태 텍스트 설정
            bodyText = body; // 본문 텍스트 설정
            auxiliaryText = auxiliary; // 보조 텍스트 설정
            continueButton = continueTarget; // 이어하기 버튼 설정
        }

        private void Start() // 화면 초기 표시
        {
            Refresh(); // 화면 데이터 갱신
        }

        public void Refresh() // 화면 데이터 갱신
        {
            if (GameManager.Instance == null) // 게임 관리자 확인
            {
                SetText(statusText, "Bootstrap 씬부터 실행해 주세요."); // 실행 안내 표시
                return; // 갱신 중단
            }

            if (continueButton != null) // 이어하기 버튼 확인
            {
                continueButton.interactable = GameManager.Instance.Save != null && GameManager.Instance.Save.HasSaveData; // 저장 존재 여부 적용
            }

            switch (screenKind) // 화면별 데이터 분기
            {
                case PrototypeScreenKind.Title: // 타이틀 화면 처리
                    RefreshTitle(); // 타이틀 정보 갱신
                    break; // 타이틀 분기 종료
                case PrototypeScreenKind.Lobby: // 로비 화면 처리
                    RefreshLobby(); // 로비 정보 갱신
                    break; // 로비 분기 종료
                case PrototypeScreenKind.Party: // 파티 화면 처리
                    RefreshParty(); // 파티 정보 갱신
                    break; // 파티 분기 종료
                case PrototypeScreenKind.DungeonSelect: // 던전 화면 처리
                    RefreshDungeon(); // 던전 정보 갱신
                    break; // 던전 분기 종료
                case PrototypeScreenKind.Battle: // 전투 화면 처리
                    RefreshBattle(); // 전투 정보 갱신
                    break; // 전투 분기 종료
                case PrototypeScreenKind.Result: // 결과 화면 처리
                    RefreshResult(); // 결과 정보 갱신
                    break; // 결과 분기 종료
            }
        }

        public void NewGame() // 새 게임 시작
        {
            if (!TryGetManagers(out SceneLoader scenes, out SaveManager save)) // 공통 관리자 확인
            {
                return; // 새 게임 중단
            }

            if (!save.CreateNewGame()) // 새 저장 생성 확인
            {
                return; // 로비 이동 중단
            }

            scenes.LoadScene(GameScenes.Lobby); // 로비 화면 이동
        }

        public void ContinueGame() // 이어하기 시작
        {
            if (!TryGetManagers(out SceneLoader scenes, out SaveManager save)) // 공통 관리자 확인
            {
                return; // 이어하기 중단
            }

            if (!save.LoadCurrent()) // 저장 불러오기 확인
            {
                Refresh(); // 이어하기 상태 갱신
                return; // 로비 이동 중단
            }

            scenes.LoadScene(GameScenes.Lobby); // 로비 화면 이동
        }

        public void SaveGame() // 현재 진행 저장
        {
            if (GameManager.Instance == null || GameManager.Instance.Save == null) // 저장 관리자 확인
            {
                return; // 저장 중단
            }

            GameManager.Instance.Save.SaveCurrent(); // 현재 진행 저장
            Refresh(); // 화면 상태 갱신
        }

        public void GoTitle() // 타이틀 이동
        {
            LoadScene(GameScenes.Title); // 타이틀 씬 전환
        }

        public void GoLobby() // 로비 이동
        {
            LoadScene(GameScenes.Lobby); // 로비 씬 전환
        }

        public void GoParty() // 파티 이동
        {
            LoadScene(GameScenes.Party); // 파티 씬 전환
        }

        public void GoDungeonSelect() // 던전 선택 이동
        {
            LoadScene(GameScenes.DungeonSelect); // 던전 선택 씬 전환
        }

        public void GoBattle() // 전투 이동
        {
            LoadScene(GameScenes.Battle); // 전투 씬 전환
        }

        public void GoResult() // 결과 이동
        {
            LoadScene(GameScenes.Result); // 결과 씬 전환
        }

        public void QuitGame() // 게임 종료
        {
            Debug.Log("[Project H] Quit requested."); // 종료 요청 로그
            Application.Quit(); // 애플리케이션 종료
        }

        private void RefreshTitle() // 타이틀 상태 갱신
        {
            bool hasSave = GameManager.Instance.Save != null && GameManager.Instance.Save.HasSaveData; // 저장 존재 확인
            SetText(statusText, hasSave ? "저장된 여정이 있습니다." : "새로운 여정을 시작해 주세요."); // 타이틀 상태 표시
            SetText(bodyText, "PROJECT H\n침식된 세계에서 이어지는 소녀들의 이야기"); // 타이틀 문구 표시
        }

        private void RefreshLobby() // 로비 상태 갱신
        {
            SaveData save = GetCurrentSave(); // 현재 저장 데이터 조회

            if (save == null) // 저장 데이터 확인
            {
                SetText(statusText, "진행 데이터 없음"); // 저장 없음 표시
                SetText(bodyText, "타이틀에서 새 게임 또는 이어하기를 선택해 주세요."); // 진행 안내 표시
                return; // 로비 갱신 중단
            }

            SetText(statusText, $"DAY {save.CurrentDay}  ·  {save.CurrentTime}"); // 현재 일차 표시
            SetText(bodyText, $"{save.CurrentChapter}\n{save.CurrentMainQuest}"); // 현재 목표 표시
            SetText(auxiliaryText, GameManager.Instance.Save.HasSaveData ? "SAVE DATA · ONLINE" : "SAVE DATA · UNSAVED"); // 저장 상태 표시
        }

        private void RefreshParty() // 파티 상태 갱신
        {
            SaveData save = GetCurrentSave(); // 현재 저장 데이터 조회

            if (save == null) // 저장 데이터 확인
            {
                SetText(bodyText, "파티 데이터가 없습니다."); // 저장 없음 표시
                return; // 파티 갱신 중단
            }

            StringBuilder builder = new StringBuilder(); // 파티 문자열 생성

            for (int index = 0; index < save.PartyCharacterIds.Count; index++) // 파티 슬롯 순회
            {
                string characterId = save.PartyCharacterIds[index]; // 캐릭터 ID 조회
                CharacterData character = GameManager.Instance.Data.GetCharacter(characterId); // 캐릭터 원본 조회
                CharacterSaveData progress = save.FindCharacter(characterId); // 캐릭터 진행 조회
                string displayName = character == null ? characterId : character.DisplayName; // 표시 이름 결정
                int level = progress == null ? 1 : progress.Level; // 표시 레벨 결정
                builder.AppendLine($"{index + 1}. {displayName}  Lv.{level}"); // 파티 슬롯 문자열 추가
            }

            SetText(statusText, $"PARTY · {save.PartyCharacterIds.Count}/4"); // 파티 인원 표시
            SetText(bodyText, builder.ToString()); // 파티 목록 표시
        }

        private void RefreshDungeon() // 던전 상태 갱신
        {
            DungeonData dungeon = GameManager.Instance.Data.GetDungeon(PrototypeDungeonId); // 던전 데이터 조회

            if (dungeon == null) // 던전 존재 확인
            {
                SetText(bodyText, $"던전 데이터 없음\n{PrototypeDungeonId}"); // 던전 누락 표시
                return; // 던전 갱신 중단
            }

            SetText(statusText, dungeon.DisplayName); // 던전 이름 표시
            SetText(bodyText, $"권장 Lv.{dungeon.RecommendedLevel}\n보상 EXP {dungeon.RewardExp}\n보상 GOLD {dungeon.RewardGold}"); // 던전 정보 표시
            SetText(auxiliaryText, $"REGION · {dungeon.RegionId}"); // 지역 정보 표시
        }

        private void RefreshBattle() // 전투 상태 갱신
        {
            DungeonData dungeon = GameManager.Instance.Data.GetDungeon(PrototypeDungeonId); // 던전 데이터 조회
            SaveData save = GetCurrentSave(); // 현재 저장 데이터 조회
            string dungeonName = dungeon == null ? PrototypeDungeonId : dungeon.DisplayName; // 던전 이름 결정
            int partyCount = save == null ? 0 : save.PartyCharacterIds.Count; // 파티 인원 계산
            SetText(statusText, "WAVE 1 / 3"); // 웨이브 표시
            SetText(bodyText, $"{dungeonName}\nPARTY {partyCount}/4\n자동 전투 시스템 연결 예정"); // 전투 정보 표시
        }

        private void RefreshResult() // 결과 상태 갱신
        {
            DungeonData dungeon = GameManager.Instance.Data.GetDungeon(PrototypeDungeonId); // 던전 데이터 조회

            if (dungeon == null) // 던전 존재 확인
            {
                SetText(bodyText, "보상 데이터를 불러오지 못했습니다."); // 보상 누락 표시
                return; // 결과 갱신 중단
            }

            SetText(statusText, "VICTORY"); // 승리 상태 표시
            SetText(bodyText, $"{dungeon.DisplayName}\nEXP +{dungeon.RewardExp}\nGOLD +{dungeon.RewardGold}"); // 보상 정보 표시
            SetText(auxiliaryText, "전투 보상 지급은 Phase 1에서 연결됩니다."); // 프로토타입 안내 표시
        }

        private SaveData GetCurrentSave() // 현재 저장 데이터 조회
        {
            if (GameManager.Instance == null || GameManager.Instance.Save == null) // 저장 관리자 확인
            {
                return null; // 저장 데이터 없음
            }

            return GameManager.Instance.Save.CurrentSave; // 현재 저장 반환
        }

        private bool TryGetManagers(out SceneLoader scenes, out SaveManager save) // 공통 관리자 조회
        {
            scenes = null; // 씬 로더 초기화
            save = null; // 저장 관리자 초기화

            if (GameManager.Instance == null) // 게임 관리자 확인
            {
                Debug.LogError("[Project H] GameManager instance is missing."); // 관리자 누락 로그
                return false; // 조회 실패
            }

            scenes = GameManager.Instance.Scenes; // 씬 로더 연결
            save = GameManager.Instance.Save; // 저장 관리자 연결
            return scenes != null && save != null; // 관리자 준비 결과 반환
        }

        private void LoadScene(string sceneName) // 공통 씬 이동
        {
            if (GameManager.Instance == null || GameManager.Instance.Scenes == null) // 씬 로더 확인
            {
                Debug.LogError("[Project H] SceneLoader is missing."); // 씬 로더 누락 로그
                return; // 씬 이동 중단
            }

            GameManager.Instance.Scenes.LoadScene(sceneName); // 대상 씬 이동
        }

        private static void SetText(Text target, string value) // 텍스트 안전 설정
        {
            if (target == null) // 텍스트 참조 확인
            {
                return; // 설정 중단
            }

            target.text = value; // 텍스트 값 적용
        }
    }
}
