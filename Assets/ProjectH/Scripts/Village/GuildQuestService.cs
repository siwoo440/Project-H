using System; // 난수·문자열 기능
using System.Collections.Generic; // 목록 자료형
using ProjectH.Battle; // 던전 해금 기능
using ProjectH.Data; // 속성·데이터 관리자 기능
using ProjectH.Dungeon; // 균열 해금 기능
using ProjectH.SaveSystem; // 저장·보상 기능

namespace ProjectH.Village // 프로젝트 마을 영역
{
    public sealed class GuildQuestProgress // 화면 표시용 의뢰 진행 정보 (Day67 신규)
    {
        public GuildQuestDefinition Definition { get; } // 의뢰 정의
        public int Progress { get; } // 진행도
        public bool Claimed { get; } // 수령 여부

        public GuildQuestProgress(GuildQuestDefinition definition, int progress, bool claimed) // 진행 정보 생성
        {
            Definition = definition; // 정의 저장
            Progress = progress; // 진행도 저장
            Claimed = claimed; // 수령 저장
        }

        public bool IsComplete => Definition != null && Progress >= Definition.Required; // 목표 달성 여부
        public bool CanClaim => IsComplete && !Claimed; // 보상 수령 가능 여부
    }

    public static class GuildQuestService // 길드 의뢰 진행·보상 (Day67 신규 — 하루 3개, 전투 결과로 진행)
    {
        public static GuildQuestBoardSaveData EnsureToday(SaveData saveData) // 오늘 의뢰 보장 (날짜가 바뀌면 새 의뢰)
        {
            if (saveData == null) return null; // 입력 확인
            GuildQuestBoardSaveData board = saveData.QuestBoard; // 게시판

            if (board.Day != saveData.CurrentDay) // 날짜 변경
            {
                board.ResetForDay(saveData.CurrentDay, PickToday(saveData, saveData.CurrentDay)); // 새 의뢰
            }

            return board; // 게시판 반환
        }

        public static List<string> PickToday(SaveData saveData, int day) // 오늘의 의뢰 선택 (같은 날이면 항상 같은 의뢰)
        {
            List<GuildQuestDefinition> candidates = new List<GuildQuestDefinition>(); // 후보

            foreach (GuildQuestDefinition definition in GuildQuestCatalog.All) // 후보 순회
            {
                if (IsAvailable(saveData, definition)) candidates.Add(definition); // 지금 할 수 있는 의뢰만
            }

            Random random = new Random(StableHash($"GUILD_QUEST|{day}")); // 결정적 난수
            List<string> result = new List<string>(); // 결과

            while (candidates.Count > 0 && result.Count < GuildQuestCatalog.DailyQuestCount) // 필요한 수만큼
            {
                int index = random.Next(candidates.Count); // 선택
                result.Add(candidates[index].Id); // 등록
                candidates.RemoveAt(index); // 중복 방지
            }

            return result; // 결과 반환
        }

        public static bool IsAvailable(SaveData saveData, GuildQuestDefinition definition) // 지금 받을 수 있는 의뢰인지
        {
            if (saveData == null || definition == null) return false; // 입력 확인

            switch (definition.Kind) // 종류 분기
            {
                case GuildQuestKind.ClearDungeon: return DungeonProgressionPolicy.IsUnlocked(saveData, definition.Target); // 열린 던전만
                case GuildQuestKind.ClearRift: return RiftService.IsUnlocked(saveData); // 균열 해금 후
                case GuildQuestKind.WinWithElement: return HasElement(saveData, definition.Element); // 해당 속성 동료 보유
                default: return true; // 전투 승리
            }
        }

        public static List<GuildQuestProgress> GetToday(SaveData saveData) // 오늘의 의뢰 진행 목록
        {
            List<GuildQuestProgress> result = new List<GuildQuestProgress>(); // 결과
            GuildQuestBoardSaveData board = EnsureToday(saveData); // 오늘 의뢰
            if (board == null) return result; // 저장 없음

            foreach (GuildQuestEntrySaveData entry in board.Quests) // 의뢰 순회
            {
                GuildQuestDefinition definition = GuildQuestCatalog.Find(entry.QuestId); // 정의
                if (definition != null) result.Add(new GuildQuestProgress(definition, entry.Progress, entry.Claimed)); // 추가
            }

            return result; // 결과 반환
        }

        public static bool AllComplete(SaveData saveData) // 오늘 의뢰를 모두 끝냈는지
        {
            List<GuildQuestProgress> quests = GetToday(saveData); // 오늘 의뢰
            if (quests.Count == 0) return false; // 의뢰 없음

            foreach (GuildQuestProgress quest in quests) // 의뢰 순회
            {
                if (!quest.IsComplete) return false; // 미완료 있음
            }

            return true; // 전부 완료
        }

        public static bool CanClaimBonus(SaveData saveData) => AllComplete(saveData) && !saveData.QuestBoard.BonusClaimed; // 전체 완료 보너스 수령 가능 여부

        public static int RecordVictory(SaveData saveData, string dungeonId, bool countsAsClear, IEnumerable<string> partyCharacterIds, Func<string, ElementType> elementLookup, bool riftCleared) // 전투 승리 반영 (진행이 늘어난 의뢰 수 반환)
        {
            GuildQuestBoardSaveData board = EnsureToday(saveData); // 오늘 의뢰
            if (board == null) return 0; // 저장 없음
            HashSet<ElementType> elements = new HashSet<ElementType>(); // 파티 속성

            if (partyCharacterIds != null && elementLookup != null) // 파티 확인
            {
                foreach (string characterId in partyCharacterIds) elements.Add(elementLookup(characterId)); // 속성 수집
            }

            int changed = 0; // 진행 변화 수

            foreach (GuildQuestEntrySaveData entry in board.Quests) // 의뢰 순회
            {
                GuildQuestDefinition definition = GuildQuestCatalog.Find(entry.QuestId); // 정의
                if (definition == null || entry.Progress >= definition.Required) continue; // 없음·완료
                bool hit = false; // 진행 여부

                switch (definition.Kind) // 종류 분기
                {
                    case GuildQuestKind.ClearDungeon: hit = countsAsClear && definition.Target == dungeonId; // 그 던전 클리어
                        break;
                    case GuildQuestKind.WinBattles: hit = true; // 모든 승리
                        break;
                    case GuildQuestKind.ClearRift: hit = riftCleared; // 균열 클리어
                        break;
                    default: hit = elements.Contains(definition.Element); // 속성 동료 참가
                        break;
                }

                if (!hit) continue; // 진행 없음
                entry.AddProgress(1, definition.Required); // 진행 증가
                changed++; // 변화 수 증가
            }

            return changed; // 변화 수 반환
        }

        public static string TryClaim(SaveData saveData, DataManager dataManager, string questId) // 보상 수령 (안내 문구 반환)
        {
            GuildQuestBoardSaveData board = EnsureToday(saveData); // 오늘 의뢰
            GuildQuestEntrySaveData entry = board == null ? null : board.Find(questId); // 의뢰 기록
            GuildQuestDefinition definition = GuildQuestCatalog.Find(questId); // 정의
            if (entry == null || definition == null) return "의뢰를 찾을 수 없습니다."; // 없음
            if (entry.Claimed) return "이미 보상을 받은 의뢰입니다."; // 중복
            if (entry.Progress < definition.Required) return "아직 완료하지 않은 의뢰입니다."; // 미완료
            entry.MarkClaimed(); // 수령 기록
            Grant(saveData, dataManager, definition.RewardGold, definition.RewardItemId, definition.RewardItemCount, definition.RewardBondResource); // 보상 지급
            return $"의뢰 완료! {definition.RewardText}"; // 안내
        }

        public static string TryClaimBonus(SaveData saveData) // 전체 완료 보너스 수령
        {
            if (!AllComplete(saveData)) return "오늘 의뢰를 모두 끝내야 받을 수 있어요."; // 미완료
            if (saveData.QuestBoard.BonusClaimed) return "이미 보너스를 받았습니다."; // 중복
            saveData.QuestBoard.MarkBonusClaimed(); // 수령 기록
            Grant(saveData, null, GuildQuestCatalog.BonusGold, string.Empty, 0, GuildQuestCatalog.BonusBondResource); // 보상 지급
            return $"오늘 의뢰 전부 완료! {GuildQuestCatalog.BonusGold}G · 결속 자원 {GuildQuestCatalog.BonusBondResource}"; // 안내
        }

        private static void Grant(SaveData saveData, DataManager dataManager, int gold, string itemId, int itemCount, int bondResource) // 보상 지급
        {
            if (gold > 0) GoldCurrencyService.AddGold(saveData, gold); // 골드
            if (!string.IsNullOrEmpty(itemId) && itemCount > 0 && dataManager != null) ItemInventoryService.TryAdd(saveData, dataManager, itemId, itemCount, out _); // 아이템 (데이터 관리자 필요)
            if (bondResource > 0) BondService.AddResource(saveData, bondResource); // 결속 자원
        }

        private static bool HasElement(SaveData saveData, ElementType element) // 해당 속성 동료 보유 여부 (데이터 관리자 없이 판정하기 위해 표를 사용)
        {
            foreach (CharacterSaveData character in saveData.Characters) // 보유 캐릭터 순회
            {
                if (CharacterElementTable.Get(character.CharacterId) == element) return true; // 일치
            }

            return false; // 없음
        }

        private static int StableHash(string value) // 실행 환경과 무관한 문자열 해시 (FNV-1a)
        {
            unchecked // 오버플로 허용
            {
                int hash = (int)2166136261; // 시작값

                for (int index = 0; index < value.Length; index++) // 문자 순회
                {
                    hash = (hash ^ value[index]) * 16777619; // 해시 갱신
                }

                return hash & 0x7fffffff; // 양수 반환
            }
        }
    }
}
