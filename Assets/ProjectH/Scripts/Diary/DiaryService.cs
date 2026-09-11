using System.Collections.Generic; // 목록 자료형
using ProjectH.Battle; // 속성 상성·던전 클리어 기능
using ProjectH.Data; // 몬스터·던전 데이터 기능
using ProjectH.Dialogue; // 개인 이벤트 완료 기능
using ProjectH.SaveSystem; // 저장·개인 이벤트·결속 기능
using ProjectH.UI; // 지원 던전 목록 기능

namespace ProjectH.Diary // 프로젝트 일기장 영역
{
    public static class DiaryService // 일기장 해금 판정 (Day63 신규 — 본 이야기 · 만난 몬스터 · 약점 공개)
    {
        public static bool MarkDialogueSeen(SaveData saveData, string scriptId) => saveData != null && saveData.AddSeenDialogue(scriptId); // 끝까지 본 대화 기록 (새로 기록하면 true)

        public static bool IsDialogueSeen(SaveData saveData, string scriptId) // 본 이야기인지 (기록 + 이전 세이브 자동 인정)
        {
            if (saveData == null || string.IsNullOrEmpty(scriptId)) return false; // 입력 확인
            if (saveData.HasSeenDialogue(scriptId)) return true; // 기록 있음
            if (CharacterEventCatalog.Find(scriptId) != null && DialogueService.IsEventCompleted(saveData, scriptId)) return true; // 완료한 개인 이벤트 (Day58 완료 플래그)

            foreach (CharacterSaveData character in saveData.Characters) // 올린 결속 단계의 대사
            {
                for (int level = 1; character != null && level <= character.BondLevel; level++) // 달성 단계까지
                {
                    if (BondCatalog.GetBondScriptId(character.CharacterId, level) == scriptId) return true; // 결속 대사 본 것으로 인정
                }
            }

            return false; // 아직 안 봄
        }

        public static int MarkMonstersSeen(SaveData saveData, IEnumerable<string> monsterIds) // 전투에 나온 몬스터 기록 (새로 기록한 수)
        {
            int added = 0; // 새 기록 수
            if (saveData == null || monsterIds == null) return 0; // 입력 확인

            foreach (string monsterId in monsterIds) // 몬스터 순회
            {
                if (saveData.AddSeenMonster(monsterId)) added++; // 새 몬스터
            }

            return added; // 새 기록 수 반환
        }

        public static List<string> GetAppearanceDungeonIds(DataManager dataManager, string monsterId) // 몬스터가 나오는 던전 (지원 던전 순서)
        {
            List<string> result = new List<string>(); // 결과
            if (dataManager == null) return result; // 데이터 없음

            foreach (string dungeonId in DungeonSelectionRuntimeState.SupportedDungeonIds) // 던전 순회
            {
                DungeonData dungeon = dataManager.GetDungeon(dungeonId); // 던전
                if (dungeon != null && ContainsMonster(dungeon, monsterId)) result.Add(dungeonId); // 등장 던전
            }

            return result; // 결과 반환
        }

        public static bool IsWeaknessKnown(SaveData saveData, DataManager dataManager, string monsterId) // 약점 공개 여부 (등장 던전을 한 번이라도 클리어)
        {
            foreach (string dungeonId in GetAppearanceDungeonIds(dataManager, monsterId)) // 등장 던전
            {
                if (DungeonProgressSaveAdapter.IsCleared(saveData, dungeonId)) return true; // 클리어한 곳이 있음
            }

            return false; // 아직
        }

        public static List<ElementType> GetWeaknesses(ElementType defender) // 이 속성에 약점을 찌르는 공격 속성 (전투 상성표 그대로)
        {
            List<ElementType> result = new List<ElementType>(); // 결과

            for (int attack = (int)ElementType.Fire; attack <= (int)ElementType.Dark; attack++) // 무속성 제외 공격 속성
            {
                if (BattleElementAffinityTable.Evaluate((BattleElement)attack, (BattleElement)(int)defender) == BattleElementAffinity.Weak) result.Add((ElementType)attack); // 약점 속성
            }

            return result; // 결과 반환
        }

        public static string GetElementLabel(ElementType element) // 속성 한글 이름
        {
            switch (element) // 속성 분기
            {
                case ElementType.Fire: return "불"; // 불
                case ElementType.Water: return "물"; // 물
                case ElementType.Grass: return "풀"; // 풀
                case ElementType.Light: return "빛"; // 빛
                case ElementType.Dark: return "어둠"; // 어둠
                default: return "무속성"; // 무속성
            }
        }

        public static string GetStatGrade(int value, int low, int high) // 수치 등급 (낮음 · 보통 · 높음 · 매우 높음)
        {
            if (value >= high * 3) return "매우 높음"; // 보스급
            if (value >= high) return "높음"; // 높음
            return value >= low ? "보통" : "낮음"; // 보통·낮음
        }

        private static bool ContainsMonster(DungeonData dungeon, string monsterId) // 던전 웨이브에 몬스터가 있는지
        {
            if (dungeon.EncounterWaves == null) return false; // 웨이브 없음

            foreach (DungeonEncounterWave wave in dungeon.EncounterWaves) // 웨이브 순회
            {
                if (wave == null || wave.MonsterIds == null) continue; // 빈 웨이브

                foreach (string id in wave.MonsterIds) // 몬스터 순회
                {
                    if (id == monsterId) return true; // 발견
                }
            }

            return false; // 없음
        }
    }
}
