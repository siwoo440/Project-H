using System; // 문자열 비교 기능
using System.Collections.Generic; // 사전 자료형
using ProjectH.SaveSystem; // 저장 기능
using UnityEngine; // Unity 기본 기능

namespace ProjectH.Events // 프로젝트 이벤트 영역
{
    [DisallowMultipleComponent] // 중복 컴포넌트 방지
    [RequireComponent(typeof(SaveManager))] // 저장 관리자 자동 보장
    public sealed class EventManager : MonoBehaviour // 이벤트 상태 관리자
    {
        [SerializeField] private ProjectHEventCatalog catalog; // 이벤트 카탈로그

        private readonly Dictionary<string, EventDefinition> definitions = new Dictionary<string, EventDefinition>(StringComparer.Ordinal); // 이벤트 정의 저장소
        private readonly List<string> validationErrors = new List<string>(); // 이벤트 검증 오류 목록
        private SaveManager saveManager; // 저장 관리자 참조

        public bool IsInitialized { get; private set; } // 초기화 상태
        public IReadOnlyList<string> ValidationErrors => validationErrors; // 검증 오류 반환
        public IReadOnlyList<EventDefinition> Definitions => catalog == null ? Array.Empty<EventDefinition>() : catalog.Events; // 이벤트 정의 목록 반환
        public int DefinitionCount => definitions.Count; // 이벤트 정의 개수 반환

        public void Initialize(SaveManager save) // 이벤트 관리자 초기화
        {
            if (IsInitialized) // 기존 초기화 확인
            {
                return; // 중복 초기화 중단
            }

            validationErrors.Clear(); // 이전 검증 오류 제거
            definitions.Clear(); // 이전 이벤트 정의 제거

            if (save == null) // 저장 관리자 확인
            {
                validationErrors.Add("SaveManager가 지정되지 않았습니다."); // 저장 관리자 누락 기록
                LogValidationErrors(); // 검증 오류 출력
                return; // 초기화 중단
            }

            saveManager = save; // 저장 관리자 연결
            BuildDefinitions(); // 이벤트 정의 저장소 생성

            if (validationErrors.Count > 0) // 검증 오류 확인
            {
                LogValidationErrors(); // 검증 오류 출력
                return; // 초기화 중단
            }

            IsInitialized = true; // 초기화 완료 기록
            Debug.Log($"[Project H][EVENT] EventManager initialized. Definitions={DefinitionCount}"); // 초기화 완료 로그
        }

        public bool HasStoryFlag(string flagId) // 스토리 플래그 확인
        {
            SaveData saveData = saveManager == null ? null : saveManager.CurrentSave; // 현재 저장 데이터 조회
            return saveData != null && saveData.HasStoryFlag(flagId); // 플래그 상태 반환
        }

        public bool SetStoryFlag(string flagId) // 스토리 플래그 활성화
        {
            SaveData saveData = saveManager == null ? null : saveManager.CurrentSave; // 현재 저장 데이터 조회

            if (saveData == null) // 저장 데이터 확인
            {
                Debug.LogError("[Project H][FLAG] SaveData is missing."); // 저장 데이터 누락 로그
                return false; // 플래그 변경 실패
            }

            bool changed = saveData.SetStoryFlag(flagId); // 플래그 활성화 실행

            if (!changed) // 변경 여부 확인
            {
                return false; // 중복 변경 중단
            }

            Debug.Log($"[Project H][FLAG] {flagId} = True"); // 플래그 변경 로그
            ProjectHEventBus.Publish(new StoryFlagChangedEvent(flagId, true)); // 플래그 변경 이벤트 발행
            return true; // 플래그 변경 성공
        }

        public bool RemoveStoryFlag(string flagId) // 스토리 플래그 비활성화
        {
            SaveData saveData = saveManager == null ? null : saveManager.CurrentSave; // 현재 저장 데이터 조회

            if (saveData == null) // 저장 데이터 확인
            {
                Debug.LogError("[Project H][FLAG] SaveData is missing."); // 저장 데이터 누락 로그
                return false; // 플래그 변경 실패
            }

            bool changed = saveData.RemoveStoryFlag(flagId); // 플래그 제거 실행

            if (!changed) // 변경 여부 확인
            {
                return false; // 변경 없음 반환
            }

            Debug.Log($"[Project H][FLAG] {flagId} = False"); // 플래그 변경 로그
            ProjectHEventBus.Publish(new StoryFlagChangedEvent(flagId, false)); // 플래그 변경 이벤트 발행
            return true; // 플래그 변경 성공
        }

        public bool TryGetDefinition(string eventId, out EventDefinition definition) // 이벤트 정의 조회
        {
            if (string.IsNullOrWhiteSpace(eventId)) // 이벤트 ID 확인
            {
                definition = null; // 빈 조회 결과 설정
                return false; // 조회 실패
            }

            return definitions.TryGetValue(eventId, out definition); // 이벤트 조회 결과 반환
        }

        public bool IsEventAvailable(string eventId, out string reason) // 이벤트 사용 가능 여부 확인
        {
            if (!TryGetDefinition(eventId, out EventDefinition definition)) // 이벤트 정의 조회
            {
                reason = $"EventDefinition not found. ID={eventId}."; // 이벤트 누락 사유 설정
                return false; // 평가 실패
            }

            return IsEventAvailable(definition, out reason); // 이벤트 정의 평가 반환
        }

        public bool IsEventAvailable(EventDefinition definition, out string reason) // 이벤트 정의 사용 가능 여부 확인
        {
            if (saveManager == null) // 저장 관리자 확인
            {
                reason = "SaveManager is missing."; // 저장 관리자 누락 사유 설정
                return false; // 평가 실패
            }

            EventEvaluationContext context = new EventEvaluationContext(saveManager.CurrentSave, saveManager.HasSaveData); // 평가 문맥 생성
            return EventConditionEvaluator.Evaluate(definition, context, out reason); // 이벤트 조건 평가 반환
        }

        public int GetAvailableEvents(List<EventDefinition> buffer) // 사용 가능 이벤트 목록 조회
        {
            if (buffer == null) // 결과 목록 확인
            {
                return 0; // 조회 중단
            }

            buffer.Clear(); // 기존 결과 제거

            foreach (EventDefinition definition in definitions.Values) // 이벤트 정의 순회
            {
                if (!IsEventAvailable(definition, out string reason)) // 이벤트 조건 평가
                {
                    continue; // 사용 불가 이벤트 제외
                }

                buffer.Add(definition); // 사용 가능 이벤트 추가
            }

            buffer.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id)); // 이벤트 ID 순 정렬
            return buffer.Count; // 사용 가능 이벤트 수 반환
        }

        private void BuildDefinitions() // 이벤트 정의 저장소 생성
        {
            if (catalog == null) // 이벤트 카탈로그 확인
            {
                validationErrors.Add("Event Catalog가 지정되지 않았습니다."); // 카탈로그 누락 기록
                return; // 저장소 생성 중단
            }

            foreach (EventDefinition definition in catalog.Events) // 이벤트 정의 순회
            {
                if (definition == null) // 이벤트 정의 null 확인
                {
                    validationErrors.Add("EventDefinition null 항목이 있습니다."); // null 정의 오류 기록
                    continue; // 다음 정의 이동
                }

                if (string.IsNullOrWhiteSpace(definition.Id)) // 이벤트 ID 확인
                {
                    validationErrors.Add($"EventDefinition ID가 비어 있습니다. Asset={definition.name}"); // 빈 ID 오류 기록
                    continue; // 다음 정의 이동
                }

                if (definitions.ContainsKey(definition.Id)) // 중복 이벤트 ID 확인
                {
                    validationErrors.Add($"EventDefinition ID 중복: {definition.Id}"); // 중복 ID 오류 기록
                    continue; // 다음 정의 이동
                }

                definitions.Add(definition.Id, definition); // 이벤트 정의 등록
            }
        }

        private void LogValidationErrors() // 검증 오류 출력
        {
            foreach (string error in validationErrors) // 오류 목록 순회
            {
                Debug.LogError($"[Project H][EVENT] {error}"); // 개별 오류 출력
            }
        }
    }
}
