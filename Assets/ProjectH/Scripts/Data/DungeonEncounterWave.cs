using System; // 직렬화 기능
using System.Collections.Generic; // 목록 자료형
using UnityEngine; // Unity 기본 기능

namespace ProjectH.Data // 프로젝트 데이터 영역
{
    [Serializable] // 던전 인카운터 웨이브 직렬화 허용
    public sealed class DungeonEncounterWave // 단일 던전 인카운터 웨이브 (Day45)
    {
        [SerializeField] private List<string> monsterIds = new List<string>(); // 웨이브 등장 몬스터 ID 목록
        public IReadOnlyList<string> MonsterIds => monsterIds; // 웨이브 몬스터 ID 목록 반환

        public DungeonEncounterWave(IEnumerable<string> ids) // 인카운터 웨이브 생성
        {
            monsterIds = new List<string>(); // 몬스터 ID 목록 초기화

            if (ids == null) // 입력 목록 확인
            {
                return; // 빈 웨이브 유지
            }

            foreach (string monsterId in ids) // 입력 몬스터 ID 순회
            {
                if (!string.IsNullOrWhiteSpace(monsterId)) // 몬스터 ID 유효성 확인
                {
                    monsterIds.Add(monsterId); // 웨이브 몬스터 ID 추가
                }
            }
        }
    }
}
