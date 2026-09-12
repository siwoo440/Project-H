using System.Collections.Generic; // 목록 자료형

namespace ProjectH.Story // 프로젝트 메인 스토리 영역 (Day68 신규)
{
    public enum ChapterStepKind // 챕터 단계 종류
    {
        Dialogue = 0, // 이야기 보기
        ClearDungeon = 1 // 던전 클리어
    }

    public sealed class ChapterStep // 챕터 한 단계
    {
        public ChapterStepKind Kind { get; } // 종류
        public string Target { get; } // 대사 ID 또는 던전 ID
        public string Summary { get; } // 로비에 표시할 "다음 할 일"
        public string GrantCharacterId { get; } // 이 단계를 마치면 합류하는 동료 (없으면 빈 값)
        public string GrantStoryFlag { get; } // 이 단계를 마치면 켜지는 스토리 플래그 (Day69 추가 — 봉인의 진실 등)

        public ChapterStep(ChapterStepKind kind, string target, string summary, string grantCharacterId = "", string grantStoryFlag = "") // 단계 생성
        {
            Kind = kind; // 종류 저장
            Target = target ?? string.Empty; // 대상 저장
            Summary = summary ?? string.Empty; // 안내 저장
            GrantCharacterId = grantCharacterId ?? string.Empty; // 합류 동료 저장
            GrantStoryFlag = grantStoryFlag ?? string.Empty; // 스토리 플래그 저장
        }
    }

    public sealed class ChapterDefinition // 챕터 하나
    {
        public string Id { get; } // 챕터 ID
        public string Title { get; } // 챕터 제목 (로비 표시)
        public IReadOnlyList<ChapterStep> Steps { get; } // 단계 목록

        public ChapterDefinition(string id, string title, params ChapterStep[] steps) // 챕터 생성
        {
            Id = id; // ID 저장
            Title = title; // 제목 저장
            Steps = steps; // 단계 저장
        }
    }

    public static class ChapterCatalog // 메인 스토리 목록 (Day68 신규 — 프롤로그 · 챕터 1 · 2, Day69 챕터 3 · 4 · 5 추가)
    {
        public const string PrologueId = "CHAPTER_00"; // 프롤로그 ID
        public const string NemesisFlag = "LORE_NEMESIS"; // 네메시스 세계관 해금 플래그 (Day69 추가)

        private static readonly ChapterDefinition[] Chapters = // 진행 순서
        {
            new ChapterDefinition(PrologueId, "프롤로그 · 부름받은 자",
                new ChapterStep(ChapterStepKind.Dialogue, "PROLOGUE_01", "낯선 천장에서 눈을 뜨기"), // 소환
                new ChapterStep(ChapterStepKind.Dialogue, "PROLOGUE_02", "세레나의 이야기를 듣기"), // 성녀와 첫 대면
                new ChapterStep(ChapterStepKind.Dialogue, "PROLOGUE_03", "첫 싸움 준비하기")), // 전투 안내
            new ChapterDefinition("CHAPTER_01", "챕터 1 · 숲의 침식",
                new ChapterStep(ChapterStepKind.Dialogue, "CH1_01", "길드의 의뢰 듣기"), // 숲의 이변
                new ChapterStep(ChapterStepKind.Dialogue, "CH1_02", "길드에서 용병 고용하기", "CH_LUCIA"), // 루시아 합류 (초반부터 2인 파티)
                new ChapterStep(ChapterStepKind.ClearDungeon, "DG001", "무너진 성역의 숲 클리어"), // 첫 던전
                new ChapterStep(ChapterStepKind.Dialogue, "CH1_03", "숲에서 만난 기사와 이야기하기", "CH_ELLEN"), // 엘렌 합류
                new ChapterStep(ChapterStepKind.ClearDungeon, "DG002", "성역 외곽 폐허 클리어"), // 두 번째 던전
                new ChapterStep(ChapterStepKind.Dialogue, "CH1_04", "폐허에서 찾은 흔적 살펴보기")), // 단서
            new ChapterDefinition("CHAPTER_02", "챕터 2 · 무너진 회랑",
                new ChapterStep(ChapterStepKind.Dialogue, "CH2_01", "노아르에서 온 마법사 만나기", "CH_LILIA"), // 릴리아 합류
                new ChapterStep(ChapterStepKind.Dialogue, "CH2_02", "숲의 궁수와 이야기하기", "CH_EVE"), // 이브 합류
                new ChapterStep(ChapterStepKind.ClearDungeon, "DG003", "침식된 회랑 클리어"), // 늪지대
                new ChapterStep(ChapterStepKind.Dialogue, "CH2_03", "회랑에서 알게 된 사실 정리하기")), // 챕터 2 마무리
            new ChapterDefinition("CHAPTER_03", "챕터 3 · 마법도시의 그림자",
                new ChapterStep(ChapterStepKind.Dialogue, "CH3_01", "노아르 마법사 의회 찾아가기"), // 노아르 도착
                new ChapterStep(ChapterStepKind.ClearDungeon, "DG005", "금지된 지하 서고 클리어"), // 서고
                new ChapterStep(ChapterStepKind.Dialogue, "CH3_02", "지하에서 만난 연금술사 돕기", "CH_CLAIRE"), // 클레어 합류
                new ChapterStep(ChapterStepKind.Dialogue, "CH3_03", "동방에서 온 순례자와 이야기하기", "CH_MERCIA"), // 메르시아 합류
                new ChapterStep(ChapterStepKind.ClearDungeon, "DG010", "의회 금단 실험실 클리어"), // 실험실
                new ChapterStep(ChapterStepKind.Dialogue, "CH3_04", "실험 기록 읽기"), // 배신자의 흔적
                new ChapterStep(ChapterStepKind.Dialogue, "CH3_05", "사라진 서명 추적하기")), // 챕터 3 마무리
            new ChapterDefinition("CHAPTER_04", "챕터 4 · 숲과 제국",
                new ChapterStep(ChapterStepKind.Dialogue, "CH4_01", "실바란 숲의 부탁 듣기"), // 실바란
                new ChapterStep(ChapterStepKind.ClearDungeon, "DG006", "울부짖는 정령의 숲 클리어"), // 숲
                new ChapterStep(ChapterStepKind.Dialogue, "CH4_02", "붉은 창의 기사와 마주하기", "CH_PYRA"), // 파이라 합류
                new ChapterStep(ChapterStepKind.Dialogue, "CH4_03", "얼어붙은 요새로 향하기"), // 카르니안
                new ChapterStep(ChapterStepKind.ClearDungeon, "DG007", "얼어붙은 요새 클리어"), // 요새
                new ChapterStep(ChapterStepKind.Dialogue, "CH4_04", "마지막 수호대와 이야기하기", "CH_TYRIA"), // 티리아 합류
                new ChapterStep(ChapterStepKind.Dialogue, "CH4_05", "제국의 기록 확인하기")), // 챕터 4 마무리
            new ChapterDefinition("CHAPTER_05", "챕터 5 · 모래 아래의 진실",
                new ChapterStep(ChapterStepKind.Dialogue, "CH5_01", "아스타르 사막으로 가기"), // 사막
                new ChapterStep(ChapterStepKind.Dialogue, "CH5_02", "지도를 그리는 탐험가 만나기", "CH_NOEL"), // 노엘 합류
                new ChapterStep(ChapterStepKind.ClearDungeon, "DG008", "잠든 봉인 신전 클리어"), // 신전
                new ChapterStep(ChapterStepKind.Dialogue, "CH5_03", "그림자의 거래 받아들이기", "CH_NATASHA"), // 나타샤 합류
                new ChapterStep(ChapterStepKind.Dialogue, "CH5_04", "사도의 선택 지켜보기", "CH_SEPHIRA"), // 세피라 합류
                new ChapterStep(ChapterStepKind.ClearDungeon, "DG014", "지하 고대 도시 클리어"), // 고대 도시
                new ChapterStep(ChapterStepKind.Dialogue, "CH5_05", "봉인의 진실 마주하기", "", ProjectH.Diary.WorldGlossaryCatalog.SealTruthFlag), // 봉인의 진실 해금
                new ChapterStep(ChapterStepKind.Dialogue, "CH5_06", "이름 없는 그림자의 목소리 듣기", "", NemesisFlag)) // 네메시스 예고
        };

        public static IReadOnlyList<ChapterDefinition> All => Chapters; // 전체 챕터

        public static ChapterDefinition Find(string chapterId) // 챕터 조회
        {
            foreach (ChapterDefinition chapter in Chapters) // 챕터 순회
            {
                if (chapter.Id == chapterId) return chapter; // 일치
            }

            return null; // 없음
        }

        public static ChapterDefinition GetNext(string chapterId) // 다음 챕터 (없으면 null = 준비된 이야기 끝)
        {
            for (int index = 0; index < Chapters.Length; index++) // 챕터 순회
            {
                if (Chapters[index].Id == chapterId) return index + 1 < Chapters.Length ? Chapters[index + 1] : null; // 다음 챕터
            }

            return null; // 없음
        }

        public static List<string> GetDialogueScriptIds() // 메인 스토리 대사 목록 (일기장 다시 보기)
        {
            List<string> result = new List<string>(); // 결과

            foreach (ChapterDefinition chapter in Chapters) // 챕터 순회
            {
                foreach (ChapterStep step in chapter.Steps) // 단계 순회
                {
                    if (step.Kind == ChapterStepKind.Dialogue) result.Add(step.Target); // 대사 추가
                }
            }

            return result; // 결과 반환
        }
    }
}
