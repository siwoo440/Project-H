using System; // 문자열 비교 기능
using ProjectH.Dialogue; // 대화 진행 결과 기능
using ProjectH.SaveSystem; // 저장·시간·호감도·결속 기능

namespace ProjectH.Village // 프로젝트 마을 영역
{
    public static class VillageActionService // 마을 구역 행동과 시간·일차 소비 (Day62 신규)
    {
        public const int ZoneEventAffinity = 3; // 구역 이벤트 완료 호감도
        public const int InnEventAffinity = 5; // 여관 특별한 밤 호감도
        public const int OnsenVitality = 30; // 온천 활력 회복량
        public const int InnEventBondLevel = 3; // 여관 특별한 밤 결속 단계 조건
        public const int InnEventBondCost = 1; // 여관 특별한 밤 결속 자원 소모

        public static string GetZoneEventScriptId(string characterId, VillageZone zone) => $"VILLAGE_{zone.ToString().ToUpperInvariant()}_{ShortId(characterId)}"; // 구역 이벤트 대사 파일 ID (예: VILLAGE_PLAZA_SERENA)

        public static string GetInnEventScriptId(string characterId) => $"INN_{ShortId(characterId)}"; // 여관 특별한 밤 대사 파일 ID (예: INN_SERENA)

        public static bool HasZoneEvent(VillageZone zone) => zone == VillageZone.Plaza || zone == VillageZone.Market || zone == VillageZone.Onsen || zone == VillageZone.Guild; // 구역 이벤트가 있는 구역 (여관은 특별한 밤)

        public static bool CanDoZoneEvent(SaveData saveData, string characterId, VillageZone zone, out string reason) // 구역 이벤트 가능 여부
        {
            CharacterSaveData character = saveData == null ? null : saveData.FindCharacter(characterId); // 캐릭터 진행

            if (character == null || VillagePresenceService.GetZone(saveData, characterId) != zone) // 이 구역에 있는지
            {
                reason = "지금 이 구역에 없는 캐릭터입니다."; // 안내
                return false; // 불가
            }

            if (!HasZoneEvent(zone)) // 이벤트 구역 확인
            {
                reason = "이 구역에는 함께할 일이 없습니다."; // 안내
                return false; // 불가
            }

            if (character.LastVillageEventDay == GameTimeService.GetCurrentDay(saveData)) // 하루 1회
            {
                reason = "오늘은 이미 함께 시간을 보냈어요. 내일 다시 만나요."; // 안내
                return false; // 불가
            }

            reason = string.Empty; // 사유 없음
            return true; // 가능
        }

        public static string CompleteZoneEvent(SaveData saveData, string characterId, VillageZone zone, DialogueRunner runner) // 구역 이벤트 끝까지 본 뒤 반영 (호감도 + 시간 1칸)
        {
            if (runner == null || !runner.IsFinished) return "끝까지 보지 않아 기록하지 않았습니다."; // 중간 종료
            if (!CanDoZoneEvent(saveData, characterId, zone, out string reason)) return reason; // 조건 재확인
            int day = GameTimeService.GetCurrentDay(saveData); // 현재 일차
            int gained = AddAffinity(saveData, characterId, ZoneEventAffinity + runner.AccumulatedAffinity); // 호감도 반영
            saveData.FindCharacter(characterId).MarkVillageEvent(day); // 오늘 기록
            SaveTimeOfDay next = GameTimeService.AdvanceTime(saveData); // 시간 1칸 소비
            return $"함께 시간을 보냈어요. 호감도 +{gained} · {FormatTime(saveData, next)}"; // 결과 안내
        }

        public static bool TryBathe(SaveData saveData, out string message) // 온천 목욕 : 활력 +30, 시간 1칸
        {
            if (saveData == null) // 저장 확인
            {
                message = "저장 데이터를 찾을 수 없습니다."; // 안내
                return false; // 실패
            }

            int before = VitalityService.GetVitality(saveData); // 목욕 전 활력
            VitalityService.AddVitality(saveData, OnsenVitality); // 활력 회복
            int after = VitalityService.GetVitality(saveData); // 목욕 후 활력
            SaveTimeOfDay next = GameTimeService.AdvanceTime(saveData); // 시간 1칸 (밤이면 다음 날 아침 · 전체 회복)
            message = $"따뜻한 물에 몸을 담갔다. 활력 +{after - before} · {FormatTime(saveData, next)}"; // 안내
            return true; // 성공
        }

        public static bool CanSleep(SaveData saveData, out string reason) // 여관 잠자기 가능 여부 (저녁·밤)
        {
            SaveTimeOfDay phase = GameTimeService.GetCurrentPhase(saveData); // 현재 시간대

            if (phase == SaveTimeOfDay.Morning || phase == SaveTimeOfDay.Day) // 아침·낮
            {
                reason = "아직 잠들 시간이 아니에요. 저녁부터 방을 잡을 수 있어요."; // 안내
                return false; // 불가
            }

            reason = string.Empty; // 사유 없음
            return saveData != null; // 가능
        }

        public static bool TrySleep(SaveData saveData, out string message) // 여관 잠자기 : 다음 날 아침 + 활력 전체 회복
        {
            if (!CanSleep(saveData, out message)) return false; // 조건 확인
            SkipToNextMorning(saveData); // 다음 날 아침으로
            message = $"푹 잤다. {GameTimeService.GetCurrentDay(saveData)}일차 아침 · 활력 {VitalityService.GetVitality(saveData)}/{SaveData.MaxVitality}"; // 안내
            return true; // 성공
        }

        public static bool CanStartInnEvent(SaveData saveData, string characterId, out string reason) // 여관 특별한 밤 진입 조건 (밤 · 여관에 있음 · 결속 3단계 · 결속 자원 1 · 하루 1회)
        {
            CharacterSaveData character = saveData == null ? null : saveData.FindCharacter(characterId); // 캐릭터 진행

            if (character == null || VillagePresenceService.GetZone(saveData, characterId) != VillageZone.Inn) // 여관에 있는지
            {
                reason = "지금 여관에 없는 캐릭터입니다."; // 안내
                return false; // 불가
            }

            if (GameTimeService.GetCurrentPhase(saveData) != SaveTimeOfDay.Night) // 밤 확인
            {
                reason = "밤에만 찾아갈 수 있어요."; // 안내
                return false; // 불가
            }

            if (character.BondLevel < InnEventBondLevel) // 결속 확인
            {
                reason = $"결속 {InnEventBondLevel}단계 이상 필요 (현재 {character.BondLevel}단계)"; // 안내
                return false; // 불가
            }

            if (BondService.GetResource(saveData) < InnEventBondCost) // 결속 자원 확인
            {
                reason = "결속 자원이 부족합니다. 하루가 지나면 1 회복됩니다."; // 안내
                return false; // 불가
            }

            if (character.LastInnEventDay == GameTimeService.GetCurrentDay(saveData)) // 하루 1회
            {
                reason = "오늘 밤은 이미 함께했어요."; // 안내
                return false; // 불가
            }

            reason = string.Empty; // 사유 없음
            return true; // 가능
        }

        public static string CompleteInnEvent(SaveData saveData, string characterId, DialogueRunner runner) // 특별한 밤 끝까지 본 뒤 반영 (결속 자원 -1 · 호감도 · 다음 날 아침)
        {
            if (runner == null || !runner.IsFinished) return "끝까지 보지 않아 기록하지 않았습니다."; // 중간 종료
            if (!CanStartInnEvent(saveData, characterId, out string reason)) return reason; // 조건 재확인
            BondService.AddResource(saveData, -InnEventBondCost); // 결속 자원 소모 (끝까지 본 경우에만)
            int gained = AddAffinity(saveData, characterId, InnEventAffinity + runner.AccumulatedAffinity); // 호감도 반영
            saveData.FindCharacter(characterId).MarkInnEvent(GameTimeService.GetCurrentDay(saveData)); // 오늘 기록
            SkipToNextMorning(saveData); // 함께 밤을 보내고 다음 날 아침
            return $"조용한 밤이 지나갔다. 호감도 +{gained} · 결속 자원 -{InnEventBondCost} · {GameTimeService.GetCurrentDay(saveData)}일차 아침"; // 결과 안내
        }

        public static string FormatTime(SaveData saveData, SaveTimeOfDay phase) => $"{GameTimeService.GetCurrentDay(saveData)}일차 {GetPhaseLabel(phase)}"; // 일차·시간대 문구

        public static string GetPhaseLabel(SaveTimeOfDay phase) => GameTimeService.GetPhaseLabel(phase); // 시간대 한글 (Day62 — 공용 표기로 통일)

        private static void SkipToNextMorning(SaveData saveData) // 다음 날 아침까지 시간 진행 (일차 변경 시 활력 전체 회복)
        {
            int today = GameTimeService.GetCurrentDay(saveData); // 오늘

            while (GameTimeService.GetCurrentDay(saveData) == today) // 날짜가 바뀔 때까지
            {
                GameTimeService.AdvanceTime(saveData); // 시간 1칸
            }
        }

        private static int AddAffinity(SaveData saveData, string characterId, int amount) // 호감도 반영 후 실제 증가량
        {
            int before = AffinityService.GetAffinity(saveData, characterId); // 이전
            int after = AffinityService.AddAffinity(saveData, characterId, amount); // 이후
            return after - before; // 증가량
        }

        private static string ShortId(string characterId) => characterId != null && characterId.StartsWith("CH_", StringComparison.Ordinal) ? characterId.Substring(3) : characterId ?? string.Empty; // CH_ 접두사 제거
    }
}
