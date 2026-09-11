using ProjectH.SaveSystem; // 결속 스킬 이름 기능
using ProjectH.UI; // 캐릭터 대표 색 기능
using UnityEngine; // Unity 색상 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public enum UltimateCutInPhase // 컷인 진행 구간 (Day59 신규)
    {
        Enter = 0, // 등장
        Hold = 1, // 유지
        Exit = 2, // 퇴장
        Done = 3 // 종료
    }

    public readonly struct UltimateCutInInfo // 컷인 표시 정보 (Day59 신규)
    {
        public string CharacterId { get; } // 캐릭터 ID
        public string CharacterName { get; } // 캐릭터 이름
        public string UltimateName { get; } // 궁극기 이름
        public string Line { get; } // 기합 대사
        public Color Tint { get; } // 캐릭터 대표 색
        public bool IsBondBurst { get; } // 결속 5단계 금색 연출 여부
        public string BondSkillName { get; } // 결속 스킬 이름 (5단계만)

        public UltimateCutInInfo(string characterId, string characterName, string ultimateName, string line, Color tint, bool isBondBurst, string bondSkillName) // 정보 생성
        {
            CharacterId = characterId ?? string.Empty; // ID 저장
            CharacterName = characterName ?? string.Empty; // 이름 저장
            UltimateName = ultimateName ?? string.Empty; // 궁극기 이름 저장
            Line = line ?? string.Empty; // 대사 저장
            Tint = tint; // 색 저장
            IsBondBurst = isBondBurst; // 결속 연출 저장
            BondSkillName = bondSkillName ?? string.Empty; // 결속 스킬 이름 저장
        }

        public string SubTitle => IsBondBurst && !string.IsNullOrEmpty(BondSkillName) ? $"결속 · {BondSkillName}" : $"{CharacterName} · ULTIMATE"; // 이름 아래 문구
    }

    public static class UltimateCutInCatalog // 궁극기 컷인 데이터 (Day59 신규 — 기합 대사는 기획서 말투 기반 임시안)
    {
        public static string GetLine(string characterId) // 캐릭터 기합 대사
        {
            switch (characterId) // 캐릭터별 분기
            {
                case "CH_SERENA": return "리리아스여, 모두에게 빛을!"; // 세레나 (부드러운 존댓말)
                case "CH_ELLEN": return "제 뒤로. 한 걸음도 물러서지 않겠습니다."; // 엘렌 (짧고 정확한 존댓말)
                case "CH_LILIA": return "좌표 고정. 전부 떨어져."; // 릴리아 (건조한 반말)
                case "CH_EVE": return "정령들아… 비가 되어 줘."; // 이브 (짧은 반말)
                case "CH_NATASHA": return "쉿— 끝나는 건 한순간이야."; // 나타샤 (장난스러운 반말) (Day64 추가)
                case "CH_CLAIRE": return "배합 완료! 다들 버텨, 지금 살려 줄게!"; // 클레어 (활발한 반말)
                case "CH_LUCIA": return "계약 이행. 머리를 노린다."; // 루시아 (냉정한 말투)
                case "CH_PYRA": return "비켜! 전부 불태워 주지!"; // 파이라 (열혈 반말)
                case "CH_TYRIA": return "……내 뒤는, 절대 무너지지 않아."; // 티리아 (과묵)
                case "CH_MERCIA": return "금강의 법으로… 봉인합니다!"; // 메르시아 (순수한 존댓말)
                case "CH_NOEL": return "모래바람아, 길을 막아!"; // 노엘 (자유로운 반말)
                case "CH_SEPHIRA": return "신이시여, 이번만은 제 뜻으로 빌겠습니다."; // 세피라 (경건한 존댓말)
                default: return string.Empty; // 기타 캐릭터
            }
        }

        public static UltimateCutInInfo Build(string characterId, string characterName, string ultimateName, int bondLevel) // 컷인 정보 생성
        {
            BondSkillDefinition skill = BondCatalog.GetSkill(characterId); // 결속 스킬 조회
            bool bondBurst = BondCatalog.HasBondSkill(bondLevel) && skill != null; // 결속 5단계 연출 여부 (기획서 9.8 궁극기 연출 변화)
            return new UltimateCutInInfo(characterId, characterName, ultimateName, GetLine(characterId), DialogueArtFactory.GetCharacterTint(characterId), bondBurst, bondBurst ? skill.SkillName : string.Empty); // 정보 반환
        }
    }

    public sealed class UltimateCutInTimeline // 컷인 시간 진행 계산 (Day59 신규 — 화면과 분리해 테스트)
    {
        public const float EnterEnd = 0.25f; // 등장 끝 시각
        public const float HoldEnd = 1.15f; // 유지 끝 시각
        public const float TotalDuration = 1.5f; // 전체 길이

        public float Elapsed { get; private set; } // 경과 시간
        public bool Skipped { get; private set; } // 클릭 스킵 여부

        public UltimateCutInPhase Phase => Skipped || Elapsed >= TotalDuration ? UltimateCutInPhase.Done : Elapsed < EnterEnd ? UltimateCutInPhase.Enter : Elapsed < HoldEnd ? UltimateCutInPhase.Hold : UltimateCutInPhase.Exit; // 현재 구간
        public bool IsDone => Phase == UltimateCutInPhase.Done; // 종료 여부
        public float EnterProgress => Mathf.Clamp01(Elapsed / EnterEnd); // 등장 진행률 0~1
        public float HoldProgress => Mathf.Clamp01((Elapsed - EnterEnd) / (HoldEnd - EnterEnd)); // 유지 진행률 0~1
        public float ExitProgress => Mathf.Clamp01((Elapsed - HoldEnd) / (TotalDuration - HoldEnd)); // 퇴장 진행률 0~1
        public float Visibility => Phase == UltimateCutInPhase.Enter ? EnterProgress : Phase == UltimateCutInPhase.Exit ? 1f - ExitProgress : Phase == UltimateCutInPhase.Done ? 0f : 1f; // 전체 투명도

        public void Advance(float deltaTime) // 시간 진행
        {
            Elapsed += Mathf.Max(0f, deltaTime); // 경과 누적
        }

        public void Skip() // 클릭 스킵 (기획서 12.18)
        {
            Skipped = true; // 즉시 종료
        }
    }
}
