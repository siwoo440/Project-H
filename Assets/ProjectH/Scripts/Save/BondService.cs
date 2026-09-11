using System; // 수학 기능
using System.Collections.Generic; // 목록 자료형

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    public enum BondStageState // 결속 단계 버튼 상태 (Day59 신규)
    {
        Locked = 0, // 이전 단계 미완료 또는 조건 미충족
        Available = 1, // 조건 충족 (자원이 있으면 올릴 수 있음)
        Completed = 2 // 이미 도달
    }

    public static class BondService // 결속 단계·자원 기능 (Day59 신규)
    {
        public static int GetLevel(SaveData saveData, string characterId) // 결속 단계 조회
        {
            CharacterSaveData character = saveData == null ? null : saveData.FindCharacter(characterId); // 캐릭터 진행 조회
            return character == null ? 0 : character.BondLevel; // 단계 반환
        }

        public static int GetResource(SaveData saveData) // 결속 자원 조회 (날짜 경과만큼 회복 반영)
        {
            if (saveData == null) // 저장 확인
            {
                return 0; // 자원 없음 반환
            }

            RefreshResource(saveData); // 회복 반영
            return saveData.BondResource; // 자원 반환
        }

        public static void RefreshResource(SaveData saveData) // 날짜 경과 회복 (Day57 선물처럼 조회 시 날짜 비교 — 시간 진행 코드 수정 없음)
        {
            if (saveData == null) // 저장 확인
            {
                return; // 처리 중단
            }

            int today = GameTimeService.GetCurrentDay(saveData); // 현재 일차

            if (saveData.LastBondRefillDay <= 0) // 첫 지급 전 (새 게임·기존 세이브)
            {
                saveData.SetBondResourceState(Math.Max(saveData.BondResource, BondCatalog.StartingResource), today); // 첫 자원 지급
                return; // 처리 종료
            }

            if (today > saveData.LastBondRefillDay) // 날짜 경과 확인
            {
                int refilled = Math.Min(BondCatalog.MaxResource, saveData.BondResource + ((today - saveData.LastBondRefillDay) * BondCatalog.ResourcePerDay)); // 경과 일수만큼 회복 (최대 5)
                saveData.SetBondResourceState(refilled, today); // 회복 반영
            }
        }

        public static void AddResource(SaveData saveData, int amount) // 결속 자원 추가 (아이템·이벤트·테스트용, 최대 5)
        {
            RefreshResource(saveData); // 회복 먼저 반영
            if (saveData != null) saveData.SetBondResourceState(Math.Min(BondCatalog.MaxResource, Math.Max(0, saveData.BondResource + amount)), saveData.LastBondRefillDay); // 범위 보정 후 저장
        }

        public static BondStageState GetStageState(SaveData saveData, string characterId, int level, List<string> unmetReasons = null) // 단계 상태 조회
        {
            unmetReasons?.Clear(); // 사유 초기화
            int current = GetLevel(saveData, characterId); // 현재 단계

            if (level <= current) // 도달 확인
            {
                return BondStageState.Completed; // 완료 반환
            }

            if (level != current + 1) // 순서 확인 (한 단계씩)
            {
                unmetReasons?.Add($"{level - 1}단계 먼저"); // 순서 사유
                return BondStageState.Locked; // 잠김 반환
            }

            return GameEventConditionEvaluator.Evaluate(saveData, BondCatalog.GetConditions(characterId, level), unmetReasons) ? BondStageState.Available : BondStageState.Locked; // 조건 판정 반환
        }

        public static bool TryRaise(SaveData saveData, string characterId, out string message) // 결속 단계 올리기 (결속 자원 1 소모)
        {
            CharacterSaveData character = saveData == null ? null : saveData.FindCharacter(characterId); // 캐릭터 진행 조회

            if (character == null) // 캐릭터 확인
            {
                message = "결속할 캐릭터를 찾을 수 없습니다."; // 누락 안내
                return false; // 실패 반환
            }

            int next = character.BondLevel + 1; // 다음 단계

            if (next > BondCatalog.MaxLevel) // 최대 확인
            {
                message = "이미 최고 결속 단계입니다."; // 최대 안내
                return false; // 실패 반환
            }

            List<string> reasons = new List<string>(); // 미충족 사유

            if (GetStageState(saveData, characterId, next, reasons) != BondStageState.Available) // 조건 확인
            {
                message = $"{next}단계 조건이 부족합니다 · {string.Join(" · ", reasons)}"; // 조건 부족 안내
                return false; // 실패 반환
            }

            if (GetResource(saveData) < 1) // 자원 확인
            {
                message = "결속 자원이 부족합니다. 하루가 지나면 1 회복됩니다."; // 자원 부족 안내
                return false; // 실패 반환 (상태 변경 없음)
            }

            saveData.SetBondResourceState(saveData.BondResource - 1, saveData.LastBondRefillDay); // 자원 1 소모
            character.SetBondLevel(next); // 단계 상승
            message = $"결속 {next}단계! {BondCatalog.GetStageEffectText(characterId, next)}"; // 성공 안내
            return true; // 성공 반환
        }
    }
}
