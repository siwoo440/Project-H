using System.Collections.Generic; // 목록 자료형
using ProjectH.SaveSystem; // 저장·스토리 플래그 기능

namespace ProjectH.Story // 프로젝트 메인 스토리 영역
{
    public sealed class EndingDefinition // 엔딩 하나 (Day72 신규 — 최종장 두 선택의 조합)
    {
        public string Id { get; } // 엔딩 ID
        public string Title { get; } // 엔딩 제목
        public string ScriptId { get; } // 엔딩 대사 파일 ID
        public string Summary { get; } // 한 줄 요약 (엔딩 목록에 표시)
        public bool SealStopped { get; } // 봉인 장치를 멈췄는지
        public bool NemesisSpared { get; } // 아르카이에게 이름을 돌려줬는지

        public EndingDefinition(string id, string title, string scriptId, string summary, bool sealStopped, bool nemesisSpared) // 엔딩 생성
        {
            Id = id ?? string.Empty; // ID 저장
            Title = title ?? string.Empty; // 제목 저장
            ScriptId = scriptId ?? string.Empty; // 대사 저장
            Summary = summary ?? string.Empty; // 요약 저장
            SealStopped = sealStopped; // 선택 1 저장
            NemesisSpared = nemesisSpared; // 선택 2 저장
        }
    }

    public static class EndingCatalog // 엔딩 4종 (Day72 신규 — 71일차 최종장 선택 2개의 조합)
    {
        public const string EndingPlayedFlag = "STORY_ENDING_PLAYED"; // 엔딩을 이미 본 저장인지 (중복 재생 방지)

        private static readonly EndingDefinition[] Endings = // 엔딩 목록 (관 × 이름)
        {
            new EndingDefinition("ENDING_RETURNED_NAME", "되돌아온 이름", "ENDING_01", "굶주림을 끊고 이름을 돌려주었다. 아르카이는 이름을 가진 채 스스로 잠들고, 사람들은 남은 침식과 함께 살아가기로 한다.", true, true), // 멈춤 + 받아들임
            new EndingDefinition("ENDING_LAST_SILENCE", "마지막 침묵", "ENDING_02", "굶주림을 끊고 이름 없이 베었다. 위협은 사라졌지만, 무엇을 끝냈는지 아무도 부를 수 없다.", true, false), // 멈춤 + 베어 냄
            new EndingDefinition("ENDING_SLEEPING_NEIGHBOR", "잠든 이웃", "ENDING_03", "공급을 이어 가며 이름을 돌려주었다. 세상은 안전해졌지만, 그 대가를 매일 조금씩 치른다.", false, true), // 유지 + 받아들임
            new EndingDefinition("ENDING_ERASED_AGAIN", "다시 지워진 이름", "ENDING_04", "공급을 이어 가며 이름 없이 베었다. 천 년 전과 같은 선택이, 같은 자리에 같은 것을 남긴다.", false, false) // 유지 + 베어 냄
        };

        public static IReadOnlyList<EndingDefinition> All => Endings; // 전체 엔딩 반환

        public static EndingDefinition Find(string endingId) // 엔딩 ID로 조회
        {
            for (int index = 0; index < Endings.Length; index++) // 전체 순회
            {
                if (Endings[index].Id == endingId) return Endings[index]; // 일치 반환
            }

            return null; // 없음
        }

        public static EndingDefinition Resolve(SaveData saveData) // 저장의 두 선택으로 엔딩 결정 (선택이 없으면 null)
        {
            if (saveData == null || !FinaleCatalog.HasSealChoice(saveData) || !FinaleCatalog.HasNemesisChoice(saveData)) return null; // 선택 미완료
            bool stopped = saveData.HasStoryFlag(FinaleCatalog.SealStoppedFlag); // 관을 끊었는지
            bool spared = saveData.HasStoryFlag(FinaleCatalog.NemesisSparedFlag); // 이름을 돌려줬는지

            for (int index = 0; index < Endings.Length; index++) // 전체 순회
            {
                if (Endings[index].SealStopped == stopped && Endings[index].NemesisSpared == spared) return Endings[index]; // 조합 일치
            }

            return null; // 없음
        }

        public static bool IsReadyToPlay(SaveData saveData) // 지금 엔딩을 재생해야 하는지 (최종장을 마쳤고 아직 보지 않음)
        {
            if (saveData == null || !FinaleCatalog.IsFinaleCleared(saveData)) return false; // 최종장 미완료
            if (saveData.HasStoryFlag(EndingPlayedFlag)) return false; // 이미 봄
            return Resolve(saveData) != null; // 엔딩이 정해지는지
        }

        public static void MarkPlayed(SaveData saveData) // 엔딩을 본 것으로 기록 (같은 저장에서 다시 재생하지 않는다)
        {
            if (saveData != null) saveData.SetStoryFlag(EndingPlayedFlag); // 플래그 기록
        }
    }
}
