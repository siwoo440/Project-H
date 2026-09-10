using System.Collections.Generic; // 목록 자료형
using ProjectH.Data; // 던전 인카운터 데이터 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class DungeonEncounterResolver // 던전 인카운터 웨이브 해석 기능 (Day45)
    {
        public static List<string[]> ResolveWaves(DungeonData dungeon, string[] legacyFallbackEnemyIds) // 던전 데이터 기반 웨이브 목록 해석
        {
            List<string[]> waves = new List<string[]>(); // 해석 웨이브 목록 생성

            if (dungeon != null && dungeon.EncounterWaves != null) // 던전 인카운터 데이터 확인
            {
                for (int index = 0; index < dungeon.EncounterWaves.Count; index++) // 인카운터 웨이브 순회
                {
                    DungeonEncounterWave wave = dungeon.EncounterWaves[index]; // 현재 웨이브 조회

                    if (wave == null || wave.MonsterIds == null || wave.MonsterIds.Count == 0) // 웨이브 유효성 확인
                    {
                        continue; // 빈 웨이브 제외
                    }

                    string[] monsterIds = new string[wave.MonsterIds.Count]; // 웨이브 몬스터 ID 배열 생성

                    for (int monsterIndex = 0; monsterIndex < wave.MonsterIds.Count; monsterIndex++) // 웨이브 몬스터 ID 순회
                    {
                        monsterIds[monsterIndex] = wave.MonsterIds[monsterIndex]; // 웨이브 몬스터 ID 복사
                    }

                    waves.Add(monsterIds); // 해석 웨이브 목록 추가
                }
            }

            if (waves.Count == 0 && legacyFallbackEnemyIds != null && legacyFallbackEnemyIds.Length > 0) // 인카운터 데이터 부재 확인
            {
                waves.Add(legacyFallbackEnemyIds); // 기존 단일 편성을 단일 웨이브로 대체
            }

            return waves; // 최종 웨이브 목록 반환
        }
    }
}
