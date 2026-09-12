using System.Collections.Generic; // 목록 자료형
using System.Text; // 문자열 조립 기능
using ProjectH.Battle; // 밸런스 계산 기능
using ProjectH.Data; // 던전·몬스터·캐릭터 데이터 기능
using ProjectH.UI; // 지원 던전 목록 기능
using UnityEditor; // 에디터 메뉴·에셋 기능
using UnityEngine; // 로그 기능

namespace ProjectH.EditorTools // 프로젝트 에디터 도구 영역
{
    public static class BalanceReportMenu // 밸런스 표 출력 (Day67 신규 — 권장 레벨 파티 기준 예상 처치 시간·버티는 시간)
    {
        private static readonly string[] Party = { "CH_SERENA", "CH_ELLEN", "CH_LILIA", "CH_EVE" }; // 기준 파티 (초기 4인)

        [MenuItem("Tools/Project H/Phase 2/67일차 밸런스 표 출력")] // 밸런스 표 메뉴 등록
        public static void PrintBalanceReport() // 던전별 예상 전투 결과 출력
        {
            List<CharacterData> party = new List<CharacterData>(); // 기준 파티

            foreach (string characterId in Party) // 파티 순회
            {
                CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>($"Assets/ProjectH/Data/Characters/{characterId}.asset"); // 캐릭터 에셋
                if (character != null) party.Add(character); // 등록
            }

            StringBuilder builder = new StringBuilder("[Project H][BALANCE] 던전별 예상 전투 (권장 레벨 · 장비 평균 가정)\n"); // 표

            foreach (string dungeonId in DungeonSelectionRuntimeState.SupportedDungeonIds) // 던전 순회
            {
                DungeonData dungeon = AssetDatabase.LoadAssetAtPath<DungeonData>($"Assets/ProjectH/Data/Dungeons/{dungeonId}.asset"); // 던전 에셋
                if (dungeon == null) continue; // 없음
                DungeonBattleTestProfile profile = DungeonBattleTestProfile.Get(dungeonId); // 배율

                for (int index = 0; index < dungeon.EncounterWaves.Count; index++) // 웨이브 순회
                {
                    List<MonsterData> monsters = new List<MonsterData>(); // 웨이브 몬스터

                    foreach (string monsterId in dungeon.EncounterWaves[index].MonsterIds) // 몬스터 순회
                    {
                        MonsterData monster = AssetDatabase.LoadAssetAtPath<MonsterData>($"Assets/ProjectH/Data/Monsters/{monsterId}.asset"); // 몬스터 에셋
                        if (monster != null) monsters.Add(monster); // 등록
                    }

                    BalanceEstimate estimate = BalanceSimulator.Estimate(party, dungeon.RecommendedLevel, monsters, profile); // 예상 결과
                    builder.AppendLine($"  {dungeon.DisplayName} · {index + 1}번 웨이브 · {BalanceSimulator.Describe(dungeonId, dungeon.RecommendedLevel, estimate, BalanceSimulator.IsBossWave(monsters))}"); // 한 줄
                }
            }

            Debug.Log(builder.ToString()); // 콘솔 출력
        }
    }
}
