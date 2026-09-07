using UnityEngine; // Unity 수학 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public readonly struct CharacterLevelProgressionResult // 캐릭터 성장 계산 결과
    {
        public int Level { get; } // 최종 레벨 반환
        public int Experience { get; } // 최종 잔여 경험치 반환
        public int LevelsGained { get; } // 상승 레벨 수 반환
        public bool ReachedMaxLevel { get; } // 최대 레벨 도달 여부 반환

        public CharacterLevelProgressionResult(int level, int experience, int levelsGained, bool reachedMaxLevel) // 성장 계산 결과 생성
        {
            Level = level; // 최종 레벨 저장
            Experience = experience; // 최종 잔여 경험치 저장
            LevelsGained = levelsGained; // 상승 레벨 수 저장
            ReachedMaxLevel = reachedMaxLevel; // 최대 레벨 도달 여부 저장
        }
    }

    public static class CharacterLevelProgression // 임시 캐릭터 레벨링 규칙
    {
        public const int MaxLevel = 20; // 임시 최대 레벨
        public const int BaseRequiredExperience = 100; // 1레벨 기본 필요 경험치
        public const int RequiredExperienceIncreasePerLevel = 50; // 레벨당 필요 경험치 증가량

        public static int GetRequiredExperience(int level) // 현재 레벨 필요 경험치 계산
        {
            int safeLevel = Mathf.Clamp(level, 1, MaxLevel); // 유효 레벨 범위 보정

            if (safeLevel >= MaxLevel) // 최대 레벨 여부 확인
            {
                return 0; // 최대 레벨 필요 경험치 없음 반환
            }

            return BaseRequiredExperience + ((safeLevel - 1) * RequiredExperienceIncreasePerLevel); // 임시 선형 필요 경험치 반환
        }

        public static CharacterLevelProgressionResult Apply(int currentLevel, int currentExperience, int gainedExperience) // 경험치 획득 성장 계산
        {
            int level = Mathf.Clamp(currentLevel, 1, MaxLevel); // 현재 레벨 안전 보정
            int experience = Mathf.Max(0, currentExperience); // 현재 경험치 안전 보정
            int gain = Mathf.Max(0, gainedExperience); // 획득 경험치 안전 보정
            int startingLevel = level; // 시작 레벨 저장

            if (level >= MaxLevel) // 이미 최대 레벨인지 확인
            {
                return new CharacterLevelProgressionResult(MaxLevel, 0, 0, true); // 최대 레벨 경험치 폐기 결과 반환
            }

            long totalExperience = (long)experience + gain; // 오버플로 방지 경험치 합산

            while (level < MaxLevel) // 최대 레벨 전까지 반복 계산
            {
                int requiredExperience = GetRequiredExperience(level); // 현재 레벨 필요 경험치 조회

                if (requiredExperience <= 0 || totalExperience < requiredExperience) // 레벨업 조건 충족 여부 확인
                {
                    break; // 레벨업 반복 종료
                }

                totalExperience -= requiredExperience; // 현재 레벨 필요 경험치 소비
                level++; // 다음 레벨 상승
            }

            bool reachedMaxLevel = level >= MaxLevel; // 최대 레벨 도달 여부 계산

            if (reachedMaxLevel) // 최대 레벨 도달 여부 확인
            {
                totalExperience = 0; // 최대 레벨 잔여 경험치 폐기
            }

            int finalExperience = totalExperience > int.MaxValue ? int.MaxValue : (int)totalExperience; // 최종 경험치 정수 범위 보정
            return new CharacterLevelProgressionResult(level, finalExperience, level - startingLevel, reachedMaxLevel); // 최종 성장 결과 반환
        }
    }
}
