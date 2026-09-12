using System; // 빈 배열 기능
using System.Collections.Generic; // 목록 자료형
using ProjectH.SaveSystem; // 저장·호감도·이벤트 조건 기능

namespace ProjectH.Dialogue // 프로젝트 대화 영역
{
    public enum CharacterEventState // 개인 이벤트 상태 (Day58 신규)
    {
        Locked = 0, // 조건 미충족
        Available = 1, // 볼 수 있음 (처음)
        Completed = 2 // 완료 (다시보기)
    }

    public sealed class DialogueRewardResult // 대화 종료 반영 결과 (Day58 신규)
    {
        public bool Applied { get; } // 보상 반영 여부
        public string Message { get; } // 안내 문구
        public int AffinityGain { get; } // 실제 호감도 증가량
        public IReadOnlyList<AffinityTier> NewTiers { get; } // 새로 도달한 단계

        public DialogueRewardResult(bool applied, string message, int affinityGain, IReadOnlyList<AffinityTier> newTiers) // 결과 생성
        {
            Applied = applied; // 반영 여부 저장
            Message = message ?? string.Empty; // 문구 저장
            AffinityGain = affinityGain; // 증가량 저장
            NewTiers = newTiers ?? Array.Empty<AffinityTier>(); // 새 단계 저장
        }

        public static DialogueRewardResult NotApplied(string message) => new DialogueRewardResult(false, message, 0, null); // 미반영 결과 생성
    }

    public static class DialogueService // 대화 결과 저장 연결 (Day58 신규 — 개인 이벤트 완료·일상 대화 1회 제한)
    {
        public const int PersonalEventAffinity = 10; // 개인 이벤트 첫 완료 호감도 (기획서 9.3 '큰 폭 상승')
        public const int DailyTalkAffinity = 2; // 일상 대화 호감도 (기획서 9.3 '안정적인 기본 상승')
        public const string EventDoneFlagPrefix = "SYS_EVENT_DONE:"; // 개인 이벤트 완료 플래그 접두사

        public static string BuildEventDoneFlag(string eventId) => EventDoneFlagPrefix + eventId; // 완료 플래그 ID 생성

        public static bool IsEventCompleted(SaveData saveData, string eventId) // 개인 이벤트 완료 여부
        {
            return saveData != null && saveData.HasStoryFlag(BuildEventDoneFlag(eventId)); // 플래그 보유 여부 반환
        }

        public static CharacterEventState GetEventState(SaveData saveData, CharacterEventDefinition definition, List<string> unmetReasons = null) // 개인 이벤트 상태 조회
        {
            if (saveData == null || definition == null) // 데이터 확인
            {
                return CharacterEventState.Locked; // 잠김 반환
            }

            if (IsEventCompleted(saveData, definition.Id)) // 완료 확인
            {
                return CharacterEventState.Completed; // 완료 반환 (조건이 다시 안 맞아도 다시보기 유지)
            }

            return GameEventConditionEvaluator.Evaluate(saveData, definition.Conditions, unmetReasons) ? CharacterEventState.Available : CharacterEventState.Locked; // 조건 판정 반환
        }

        public static DialogueRewardResult CompleteEvent(SaveData saveData, CharacterEventDefinition definition, DialogueRunner runner) // 개인 이벤트 끝까지 본 뒤 반영
        {
            if (definition == null || runner == null || !runner.IsFinished) // 끝까지 봤는지 확인
            {
                return DialogueRewardResult.NotApplied("이야기를 끝까지 보지 않아 기록하지 않았습니다."); // 중간 종료 결과
            }

            CharacterEventState state = GetEventState(saveData, definition); // 현재 상태 조회

            if (state == CharacterEventState.Completed) // 다시보기 확인
            {
                return DialogueRewardResult.NotApplied($"「{definition.Title}」 다시보기를 마쳤습니다. (보상은 처음 한 번만)"); // 다시보기 결과
            }

            if (state == CharacterEventState.Locked) // 잠김 확인
            {
                return DialogueRewardResult.NotApplied($"「{definition.Title}」은(는) 아직 잠겨 있습니다."); // 잠김 결과
            }

            int before = AffinityService.GetAffinity(saveData, definition.CharacterId); // 반영 전 호감도
            int after = AffinityService.AddAffinity(saveData, definition.CharacterId, PersonalEventAffinity + runner.AccumulatedAffinity); // 완료 보상 + 선택지 호감도 반영
            saveData.SetStoryFlag(BuildEventDoneFlag(definition.Id)); // 완료 기록
            List<AffinityTier> newTiers = AffinityService.CollectNewTiers(before, after); // 새 단계 계산
            string preferredText = runner.PreferredChoiceCount > 0 ? $" · 마음에 드는 대답 {runner.PreferredChoiceCount}회" : string.Empty; // 선호 선택 안내
            string uniqueText = definition.Episode == 1 && UniqueEquipmentService.TryGrant(saveData, definition.CharacterId, out _, out string grantMessage) ? $"\n{grantMessage}" : string.Empty; // 1화 보상 : 전용 장비 (Day70 추가)
            string message = $"{definition.Episode}화 「{definition.Title}」 완료! 호감도 +{after - before} ({after}/{CharacterSaveData.MaxAffinity}){preferredText}" + AffinityService.BuildTierReachedNotice(newTiers) + uniqueText; // 결과 문구
            return new DialogueRewardResult(true, message, after - before, newTiers); // 반영 결과 반환
        }

        public static int GetTalkSlot(SaveData saveData) // 현재 시간 칸 (일차×4+시간대)
        {
            return (GameTimeService.GetCurrentDay(saveData) * 4) + (int)GameTimeService.GetCurrentPhase(saveData); // 시간 칸 반환 (1일차 아침 = 4)
        }

        public static string GetTalkScriptId(string characterId, SaveTimeOfDay phase) // 일상 대화 파일 ID (예: TALK_SERENA_MORNING)
        {
            string shortId = characterId != null && characterId.StartsWith("CH_", StringComparison.Ordinal) ? characterId.Substring(3) : characterId ?? string.Empty; // CH_ 접두사 제거
            return $"TALK_{shortId}_{phase.ToString().ToUpperInvariant()}"; // 파일 ID 반환
        }

        public static bool CanTalk(SaveData saveData, string characterId, out string reason) // 일상 대화 가능 여부
        {
            CharacterSaveData character = saveData == null ? null : saveData.FindCharacter(characterId); // 캐릭터 진행 조회

            if (character == null) // 캐릭터 확인
            {
                reason = "대화할 캐릭터를 찾을 수 없습니다."; // 캐릭터 누락 사유
                return false; // 대화 불가 반환
            }

            if (character.HasTalkedInSlot(GetTalkSlot(saveData))) // 이번 시간대 대화 확인
            {
                reason = "이 시간대에는 이미 대화했어요. 시간이 지나면 다시 이야기할 수 있어요."; // 1회 제한 사유
                return false; // 대화 불가 반환
            }

            reason = string.Empty; // 사유 없음
            return true; // 대화 가능 반환
        }

        public static DialogueRewardResult CompleteTalk(SaveData saveData, string characterId, DialogueRunner runner) // 일상 대화 끝까지 본 뒤 반영
        {
            if (runner == null || !runner.IsFinished) // 끝까지 봤는지 확인
            {
                return DialogueRewardResult.NotApplied("대화를 끝까지 보지 않아 기록하지 않았습니다."); // 중간 종료 결과
            }

            if (!CanTalk(saveData, characterId, out string reason)) // 대화 가능 여부 재확인
            {
                return DialogueRewardResult.NotApplied(reason); // 불가 결과
            }

            int before = AffinityService.GetAffinity(saveData, characterId); // 반영 전 호감도
            int after = AffinityService.AddAffinity(saveData, characterId, DailyTalkAffinity + runner.AccumulatedAffinity); // 대화 호감도 반영
            saveData.FindCharacter(characterId).MarkTalked(GetTalkSlot(saveData)); // 이번 시간대 대화 기록
            List<AffinityTier> newTiers = AffinityService.CollectNewTiers(before, after); // 새 단계 계산
            string message = $"대화를 나눴어요. 호감도 +{after - before} ({after}/{CharacterSaveData.MaxAffinity})" + AffinityService.BuildTierReachedNotice(newTiers); // 결과 문구
            return new DialogueRewardResult(true, message, after - before, newTiers); // 반영 결과 반환
        }
    }
}
