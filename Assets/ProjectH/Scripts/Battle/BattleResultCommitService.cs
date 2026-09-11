using System; // 문자열 및 숫자 범위 기능
using System.Collections.Generic; // 중복 검사 집합 기능
using ProjectH.Core; // 게임 관리자 기능
using ProjectH.Data; // 아이템 데이터 관리자 기능
using ProjectH.SaveSystem; // 저장 데이터 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleResultCommitService // 전투 결과 영구 반영 기능
    {
        public static bool CommitOnce(SaveData saveData, BattleResultData result) // 전투 결과 1회 영구 반영
        {
            if (saveData == null || result == null || string.IsNullOrWhiteSpace(result.ResultId)) // 저장 데이터 및 결과 ID 확인
            {
                return false; // 잘못된 결과 반영 중단
            }

            if (BattleProgressSaveAdapter.HasBattleResultCommit(saveData, result.ResultId)) // 기존 결과 반영 여부 확인
            {
                return false; // 중복 결과 반영 차단
            }

            DataManager dataManager = GameManager.Instance == null ? null : GameManager.Instance.Data; // 현재 데이터 관리자 조회

            if (result.Outcome == BattleOutcome.Victory && !DungeonDropGrantService.CanGrantAll(saveData, dataManager, result.Drops, out _)) // 승리 드롭 지급 가능 여부 확인
            {
                return false; // 드롭 데이터 이상 시 전체 결과 반영 중단
            }

            if (result.Outcome == BattleOutcome.Victory) // 승리 결과 여부 확인
            {
                BattleProgressSaveAdapter.AddGold(saveData, result.Gold); // 승리 골드 영구 반영
                ApplyExperience(saveData, result); // 참가 캐릭터 경험치 및 레벨 영구 반영
                if (result.CountsAsDungeonClear) // 던전 클리어 기록 대상 확인 (Day55 — 탐험 중에는 보스 노드만)
                {
                    DungeonProgressSaveAdapter.MarkCleared(saveData, result.DungeonId); // 승리 던전 클리어 영구 반영
                    DungeonProgressSaveAdapter.TrySetBestStars(saveData, result.DungeonId, result.StarCount); // 승리 던전 최고 별점 영구 반영 (Day47)
                }
                DungeonDropGrantService.GrantAll(saveData, dataManager, result.Drops); // 승리 던전 드롭 영구 반영
            }

            BattleProgressSaveAdapter.MarkBattleResultCommitted(saveData, result.ResultId); // 전투 결과 반영 완료 기록
            return true; // 결과 반영 성공 반환
        }

        private static void ApplyExperience(SaveData saveData, BattleResultData result) // 참가 캐릭터 경험치 및 레벨 반영
        {
            if (result.Experience <= 0 || result.Members == null) // 경험치 및 파티 결과 확인
            {
                return; // 경험치 반영 불필요 처리
            }

            HashSet<string> rewardedCharacterIds = new HashSet<string>(StringComparer.Ordinal); // 중복 경험치 지급 방지 집합 생성

            for (int index = 0; index < result.Members.Count; index++) // 결과 파티원 순회
            {
                BattleResultPartyMember member = result.Members[index]; // 현재 결과 파티원 조회

                if (member == null || string.IsNullOrWhiteSpace(member.CharacterId) || !rewardedCharacterIds.Add(member.CharacterId)) // 파티원 ID 및 중복 여부 확인
                {
                    continue; // 잘못된 또는 중복 파티원 제외
                }

                CharacterSaveData character = saveData.FindCharacter(member.CharacterId); // 저장 캐릭터 진행 조회

                if (character == null) // 저장 캐릭터 존재 확인
                {
                    continue; // 미보유 캐릭터 경험치 지급 제외
                }

                int startLevel = character.Level; // 성장 적용 전 레벨 저장
                int startExperience = character.Experience; // 성장 적용 전 경험치 저장
                CharacterLevelProgressionResult progression = CharacterLevelProgression.Apply(startLevel, startExperience, result.Experience); // 현재 성장 상태와 획득 경험치 계산
                character.SetLevel(progression.Level); // 계산된 캐릭터 레벨 저장
                character.SetExperience(progression.Experience); // 계산된 잔여 경험치 저장
                member.ApplyGrowthResult(startLevel, startExperience, result.Experience, progression); // 결과 화면용 실제 성장 결과 연결
            }
        }
    }
}
