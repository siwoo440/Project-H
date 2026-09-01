using System.Text; // 문자열 조립 기능
using ProjectH.Data; // 프로젝트 데이터 기능
using ProjectH.SaveSystem; // 프로젝트 저장 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public readonly struct LobbyScreenViewData // 로비 화면 표시 데이터
    {
        public string StatusText { get; } // 진행 상태 문구 반환
        public string BodyText { get; } // 본문 문구 반환
        public string SaveStateText { get; } // 저장 상태 문구 반환
        public string PartyText { get; } // 파티 요약 문구 반환
        public bool CanNavigate { get; } // 주요 이동 가능 여부 반환

        private LobbyScreenViewData(string statusText, string bodyText, string saveStateText, string partyText, bool canNavigate) // 로비 표시 데이터 생성
        {
            StatusText = statusText; // 진행 상태 문구 저장
            BodyText = bodyText; // 본문 문구 저장
            SaveStateText = saveStateText; // 저장 상태 문구 저장
            PartyText = partyText; // 파티 문구 저장
            CanNavigate = canNavigate; // 이동 가능 여부 저장
        }

        public static LobbyScreenViewData Build(DataManager dataManager, SaveData saveData, bool hasSaveData) // 저장 데이터 기반 로비 표시 생성
        {
            if (saveData == null) // 현재 저장 데이터 확인
            {
                return new LobbyScreenViewData("진행 데이터 없음", "타이틀에서 새 게임 또는 이어하기를 선택해 주세요.", "SAVE DATA · NONE", "PARTY\n-", false); // 저장 없음 화면 데이터 반환
            }

            saveData.EnsureDefaults(); // 저장 기본값 보정
            string status = $"DAY {saveData.CurrentDay}  ·  {saveData.CurrentTime}"; // 일차 및 시간 문구 생성
            string body = $"{saveData.CurrentChapter}\n{saveData.CurrentMainQuest}"; // 챕터 및 목표 문구 생성
            string saveState = hasSaveData ? "SAVE DATA · ONLINE" : "SAVE DATA · UNSAVED"; // 저장 상태 문구 생성
            string party = BuildPartySummary(dataManager, saveData); // 파티 요약 문구 생성
            return new LobbyScreenViewData(status, body, saveState, party, true); // 정상 로비 화면 데이터 반환
        }

        private static string BuildPartySummary(DataManager dataManager, SaveData saveData) // 파티 요약 문구 생성
        {
            StringBuilder builder = new StringBuilder(); // 파티 문자열 생성
            builder.AppendLine($"PARTY · {saveData.PartyCharacterIds.Count}/4"); // 파티 인원 문구 추가

            for (int index = 0; index < saveData.PartyCharacterIds.Count; index++) // 파티 슬롯 순회
            {
                string characterId = saveData.PartyCharacterIds[index]; // 캐릭터 ID 조회
                CharacterData character = dataManager == null ? null : dataManager.GetCharacter(characterId); // 캐릭터 원본 조회
                CharacterSaveData progress = saveData.FindCharacter(characterId); // 캐릭터 진행 조회
                string displayName = character == null ? characterId : character.DisplayName; // 표시 이름 결정
                int level = progress == null ? 1 : progress.Level; // 표시 레벨 결정
                builder.AppendLine($"{index + 1}. {displayName}  Lv.{level}"); // 캐릭터 파티 문구 추가
            }

            return builder.ToString().TrimEnd(); // 파티 요약 반환
        }
    }
}
