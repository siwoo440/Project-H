using System; // 숫자 범위 보정 기능
using System.Collections.Generic; // 중복 검사 집합 기능
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

            if (result.Outcome == BattleOutcome.Victory) // 승리 결과 여부 확인
            {
                BattleProgressSaveAdapter.AddGold(saveData, result.Gold); // 승리 골드 영구 반영
                ApplyExperience(saveData, result); // 참가 캐릭터 경험치 영구 반영
            }

            BattleProgressSaveAdapter.MarkBattleResultCommitted(saveData, result.ResultId); // 전투 결과 반영 완료 기록
            return true; // 결과 반영 성공 반환
        }

        private static void ApplyExperience(SaveData saveData, BattleResultData result) // 참가 캐릭터 경험치 반영
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

                long summedExperience = (long)character.Experience + result.Experience; // 경험치 오버플로 방지 합산
                int nextExperience = summedExperience > int.MaxValue ? int.MaxValue : (int)summedExperience; // 최대 정수 범위 보정
                character.SetExperience(nextExperience); // 참가 캐릭터 경험치 저장
            }
        }
    }
}
