using System.Collections.Generic; // 목록 자료형

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    public sealed class BondSkillDefinition // 캐릭터별 결속 4·5단계 효과 설명 (Day59 신규)
    {
        public string CharacterId { get; } // 캐릭터 ID
        public string PassiveBoostText { get; } // 4단계 패시브 강화 설명
        public string SkillName { get; } // 5단계 결속 스킬 이름
        public string SkillText { get; } // 5단계 결속 스킬 설명

        public BondSkillDefinition(string characterId, string passiveBoostText, string skillName, string skillText) // 정의 생성
        {
            CharacterId = characterId; // 캐릭터 ID 저장
            PassiveBoostText = passiveBoostText; // 4단계 설명 저장
            SkillName = skillName; // 스킬 이름 저장
            SkillText = skillText; // 스킬 설명 저장
        }
    }

    public readonly struct BondSynergyResult // 파티 조합 시너지 판정 결과 (Day59 신규)
    {
        public bool BalancedParty { get; } // 균형 파티 (초기 4인)
        public bool RoundTable { get; } // 결속의 원탁 (결속 3단계 이상 4명)

        public BondSynergyResult(bool balancedParty, bool roundTable) // 결과 생성
        {
            BalancedParty = balancedParty; // 균형 파티 저장
            RoundTable = roundTable; // 결속의 원탁 저장
        }
    }

    public static class BondCatalog // 결속 단계 조건·효과 수치 목록 (Day59 신규 — 수치는 이 파일에서 조정, 기획서 5.7·6.11·8.17·15.19)
    {
        public const int MaxLevel = 5; // 최대 결속 단계
        public const int MaxResource = 5; // 결속 자원 최대치 (기획서 5.7)
        public const int StartingResource = 1; // 첫 지급 결속 자원
        public const int ResourcePerDay = 1; // 하루 회복량 (기획서 5.7)
        public const float HpBonus = 0.02f; // 1단계 최대 체력 증가율
        public const float GaugeGainBonus = 0.10f; // 2단계 궁극기 게이지 충전 증가율
        public const float PerfectBonusPerHit = 0.04f; // 3단계 Perfect 1개당 궁극기 위력 추가
        public const float PerfectBonusCap = 0.20f; // 3단계 Perfect 추가 위력 상한
        public const float BalancedPartyBonus = 0.03f; // 균형 파티 공격력·방어력·회복량 증가율 (기획서 4.8)
        public const float RoundTableGaugeBonus = 0.10f; // 결속의 원탁 게이지 획득 증가율 (기획서 4.8)
        public const int RoundTableMinLevel = 3; // 결속의 원탁 인정 결속 단계
        private static readonly string[] BalancedPartyMembers = { "CH_SERENA", "CH_ELLEN", "CH_LILIA", "CH_EVE" }; // 균형 파티 구성원

        private static readonly BondSkillDefinition[] Skills = // 초기 4인 결속 4·5단계 효과 (기획서 15.19 캐릭터별 방향)
        {
            new BondSkillDefinition("CH_SERENA", "패시브 보호막 +30%", "수호의 기도", "아군이 체력 30% 이하가 되면 전투당 1회 최대 체력 20% 보호막"), // 세레나 (회복·보호막)
            new BondSkillDefinition("CH_ELLEN", "패시브 방어력 증가 20% → 25%", "철벽의 맹세", "궁극기 사용 시 파티 전체 피해 감소 +10% (6초)"), // 엘렌 (피해 감소·도발)
            new BondSkillDefinition("CH_LILIA", "광역 스킬 방어 감소 5% → 10%", "마력 공명", "궁극기에 맞은 적마다 마력 폭발 추가 피해 (공격력 50%)"), // 릴리아 (마법 피해·저항 감소)
            new BondSkillDefinition("CH_EVE", "패시브 치명타율 +4% → +9%", "정령의 화살", "치명타 시 관통 화살 1발 추가 (공격력 40%, 3초마다)") // 이브 (치명타·관통)
        };

        public static BondSkillDefinition GetSkill(string characterId) // 캐릭터 결속 효과 조회 (없으면 null — 추가 캐릭터 일차에 연결)
        {
            for (int index = 0; index < Skills.Length; index++) // 목록 순회
            {
                if (Skills[index].CharacterId == characterId) // 캐릭터 일치 확인
                {
                    return Skills[index]; // 정의 반환
                }
            }

            return null; // 없음 반환
        }

        public static List<GameEventCondition> GetConditions(string characterId, int level) // 단계별 해금 조건 (단계 올리기에는 결속 자원 1 별도 소모)
        {
            List<GameEventCondition> conditions = new List<GameEventCondition>(); // 조건 목록

            switch (level) // 단계 분기
            {
                case 1: // 1단계 : 안면
                    conditions.Add(GameEventCondition.AffinityAtLeast(characterId, AffinityTier.Acquaintance)); // 안면 이상
                    break; // 분기 종료
                case 2: // 2단계 : 호감 + 개인 이벤트 1화 (기획서 9.8 '이전 개인 스토리 완료')
                    conditions.Add(GameEventCondition.AffinityAtLeast(characterId, AffinityTier.Friendly)); // 호감 이상
                    List<CharacterEventDefinition> events = CharacterEventCatalog.GetForCharacter(characterId); // 개인 이벤트 조회
                    if (events.Count > 0) conditions.Add(GameEventCondition.Flag($"SYS_EVENT_DONE:{events[0].Id}", $"{events[0].Episode}화 「{events[0].Title}」 완료")); // 1화 완료 (Day58 완료 플래그)
                    break; // 분기 종료
                case 3: // 3단계 : 신뢰
                    conditions.Add(GameEventCondition.AffinityAtLeast(characterId, AffinityTier.Trusted)); // 신뢰 이상
                    break; // 분기 종료
                case 4: // 4단계 : 유대 (기획서 9.8 '보통 Lv.7 이상')
                    conditions.Add(GameEventCondition.AffinityAtLeast(characterId, AffinityTier.Bonded)); // 유대 이상
                    break; // 분기 종료
                case 5: // 5단계 : 호감도 100 (기획서 15.19 '강력한 최종 보상')
                    conditions.Add(GameEventCondition.AffinityValueAtLeast(characterId, CharacterSaveData.MaxAffinity)); // 호감도 100
                    break; // 분기 종료
            }

            return conditions; // 조건 반환
        }

        public static string GetStageEffectText(string characterId, int level) // 단계별 효과 설명
        {
            BondSkillDefinition skill = GetSkill(characterId); // 캐릭터 결속 효과 조회

            switch (level) // 단계 분기
            {
                case 1: return $"결속 대사 · 최대 체력 +{HpBonus * 100f:0}%"; // 1단계 설명
                case 2: return $"궁극기 게이지 충전 +{GaugeGainBonus * 100f:0}%"; // 2단계 설명
                case 3: return $"리듬 Perfect마다 궁극기 위력 +{PerfectBonusPerHit:0.00} (최대 +{PerfectBonusCap:0.00})"; // 3단계 설명
                case 4: return skill == null ? "패시브 강화 (추가 캐릭터 일차에 연결)" : skill.PassiveBoostText; // 4단계 설명
                case 5: return skill == null ? "결속 스킬 (추가 캐릭터 일차에 연결)" : $"결속 스킬 「{skill.SkillName}」 · {skill.SkillText}"; // 5단계 설명
                default: return string.Empty; // 기타 단계
            }
        }

        public static string GetBondScriptId(string characterId, int level) // 단계 상승 결속 대사 파일 ID (예: BOND_SERENA_2)
        {
            string shortId = characterId != null && characterId.StartsWith("CH_") ? characterId.Substring(3) : characterId ?? string.Empty; // CH_ 접두사 제거
            return $"BOND_{shortId}_{level}"; // 파일 ID 반환
        }

        public static float GetHpBonus(int level) => level >= 1 ? HpBonus : 0f; // 1단계 이상 체력 보너스
        public static float GetGaugeGainBonus(int level) => level >= 2 ? GaugeGainBonus : 0f; // 2단계 이상 게이지 보너스
        public static float GetPerfectBonusPerHit(int level) => level >= 3 ? PerfectBonusPerHit : 0f; // 3단계 이상 Perfect 보너스
        public static bool HasPassiveBoost(int level) => level >= 4; // 4단계 이상 패시브 강화
        public static bool HasBondSkill(int level) => level >= MaxLevel; // 5단계 결속 스킬

        public static BondSynergyResult EvaluateSynergy(IReadOnlyList<string> partyCharacterIds, IReadOnlyList<int> partyBondLevels) // 파티 조합 시너지 판정 (기획서 4.8, 현재 성립 가능한 2종)
        {
            if (partyCharacterIds == null || partyBondLevels == null) // 입력 확인
            {
                return new BondSynergyResult(false, false); // 시너지 없음 반환
            }

            bool balanced = true; // 균형 파티 판정

            for (int index = 0; index < BalancedPartyMembers.Length; index++) // 구성원 순회
            {
                bool found = false; // 구성원 포함 여부

                for (int member = 0; member < partyCharacterIds.Count; member++) // 파티 순회
                {
                    if (partyCharacterIds[member] == BalancedPartyMembers[index]) found = true; // 포함 확인
                }

                balanced &= found; // 모두 포함해야 성립
            }

            int bonded = 0; // 결속 3단계 이상 인원

            for (int index = 0; index < partyBondLevels.Count; index++) // 결속 단계 순회
            {
                if (partyBondLevels[index] >= RoundTableMinLevel) bonded++; // 인원 증가
            }

            return new BondSynergyResult(balanced, bonded >= 4); // 판정 결과 반환
        }
    }
}
