using ProjectH.SaveSystem; // 저장·스토리 플래그 기능

namespace ProjectH.Story // 프로젝트 메인 스토리 영역
{
    public static class FinaleCatalog // 최종장 선택과 엔딩 재료 (Day71 신규 — 엔딩 분기 자체는 72일차)
    {
        public const string ArchaiName = "아르카이"; // 네메시스의 지워진 진명
        public const string ArchaiFlag = "LORE_ARCHAI"; // 진명 해금 플래그 (CH6_03에서 설정)
        public const string SealStoppedFlag = "ENDING_SEAL_STOPPED"; // 선택 1 : 봉인 장치를 멈춘다
        public const string SealKeptFlag = "ENDING_SEAL_KEPT"; // 선택 1 : 봉인 장치를 유지한다
        public const string NemesisSparedFlag = "ENDING_NEMESIS_SPARED"; // 선택 2 : 이름을 돌려준다
        public const string NemesisSlainFlag = "ENDING_NEMESIS_SLAIN"; // 선택 2 : 이름 없이 끝낸다
        public const string FinaleClearedFlag = "STORY_FINALE_CLEARED"; // 최종장 완료 플래그

        public static bool HasSealChoice(SaveData saveData) => Has(saveData, SealStoppedFlag) || Has(saveData, SealKeptFlag); // 선택 1을 했는지

        public static bool HasNemesisChoice(SaveData saveData) => Has(saveData, NemesisSparedFlag) || Has(saveData, NemesisSlainFlag); // 선택 2를 했는지

        public static bool IsFinaleCleared(SaveData saveData) => Has(saveData, FinaleClearedFlag); // 최종장을 마쳤는지

        public static string GetSealChoiceLabel(SaveData saveData) // 선택 1 표시 문구
        {
            if (Has(saveData, SealStoppedFlag)) return "장치를 멈췄다"; // 공급 중단
            if (Has(saveData, SealKeptFlag)) return "장치를 유지했다"; // 공급 유지
            return "아직 고르지 않았다"; // 미선택
        }

        public static string GetNemesisChoiceLabel(SaveData saveData) // 선택 2 표시 문구
        {
            if (Has(saveData, NemesisSparedFlag)) return $"{ArchaiName}에게 이름을 돌려주었다"; // 받아들임
            if (Has(saveData, NemesisSlainFlag)) return "이름 없는 채로 끝냈다"; // 베어 냄
            return "아직 고르지 않았다"; // 미선택
        }

        public static string GetEndingSeedText(SaveData saveData) // 두 선택이 만들 결말 미리보기 (72일차 엔딩 분기의 재료)
        {
            if (!HasSealChoice(saveData) || !HasNemesisChoice(saveData)) return "최종장의 두 선택이 아직 남아 있습니다."; // 미완료
            bool stopped = Has(saveData, SealStoppedFlag); // 장치를 멈췄는지
            bool spared = Has(saveData, NemesisSparedFlag); // 이름을 돌려줬는지

            if (stopped && spared) return "굶주림을 끊고 이름을 돌려주었다 — 되돌아온 이름"; // 멈춤 + 받아들임
            if (stopped) return "굶주림을 끊고 이름 없이 베었다 — 마지막 침묵"; // 멈춤 + 베어 냄
            if (spared) return "공급을 이어 가며 이름을 돌려주었다 — 잠든 이웃"; // 유지 + 받아들임
            return "공급을 이어 가며 이름 없이 베었다 — 다시 지워진 이름"; // 유지 + 베어 냄
        }

        private static bool Has(SaveData saveData, string flag) => saveData != null && saveData.HasStoryFlag(flag); // 플래그 확인
    }
}
