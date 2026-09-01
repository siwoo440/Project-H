using System.Collections; // 코루틴 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 체력 디버그 컨트롤러 방지
    public sealed class BattleHealthDebugController : MonoBehaviour // 13일차 체력 디버그 기능
    {
        [SerializeField] private BattleCombatRegistry registry; // 전투 객체 레지스트리
        [SerializeField] private Button healButton; // 아군 회복 디버그 버튼
        [SerializeField] private Text statusText; // 전투 상태 텍스트
        [SerializeField, Min(1)] private int healAmount = 25; // 디버그 회복량
        private bool bound; // 버튼 이벤트 연결 상태

        public void Configure(BattleCombatRegistry combatRegistry, Button debugHealButton, Text battleStatusText) // 에디터 참조 설정
        {
            registry = combatRegistry; // 전투 레지스트리 연결
            healButton = debugHealButton; // 회복 디버그 버튼 연결
            statusText = battleStatusText; // 전투 상태 텍스트 연결
        }

        private void Start() // 체력 디버그 시작
        {
            BindButton(); // 회복 버튼 이벤트 연결
            StartCoroutine(UpdateStatusNextFrame()); // 기존 전투 초기화 이후 상태 문구 갱신
        }

        private void OnDestroy() // 체력 디버그 종료
        {
            if (bound && healButton != null) // 회복 버튼 이벤트 연결 확인
            {
                healButton.onClick.RemoveListener(DebugHealFirstDamagedAlly); // 회복 버튼 이벤트 해제
            }

            bound = false; // 버튼 연결 상태 초기화
        }

        public void DebugHealFirstDamagedAlly() // 첫 피해 아군 회복 디버그 실행
        {
            if (registry == null) // 전투 레지스트리 확인
            {
                SetStatus("BattleCombatRegistry를 찾을 수 없습니다."); // 레지스트리 오류 표시
                return; // 회복 디버그 중단
            }

            for (int index = 0; index < registry.Actors.Count; index++) // 전체 전투 객체 순회
            {
                BattleActor actor = registry.Actors[index]; // 현재 전투 객체 조회

                if (actor == null || actor.Team != BattleTeam.Ally || !actor.IsCombatReady) // 아군 전투 객체 확인
                {
                    continue; // 잘못된 회복 대상 제외
                }

                if (!actor.Stats.IsAlive || actor.Stats.CurrentHp >= actor.Stats.MaxHp) // 생존 및 손실 체력 확인
                {
                    continue; // 회복 불필요 대상 제외
                }

                BattleHealingResult result = BattleHealingResolver.Resolve(actor.Stats, healAmount); // 디버그 회복량 계산
                int applied = actor.ApplyHealing(result); // 실제 아군 회복 적용
                SetStatus($"{actor.Stats.DisplayName} +{applied} 회복"); // 회복 결과 상태 표시
                return; // 첫 피해 아군 회복 완료
            }

            SetStatus("회복 가능한 피해 아군이 없습니다."); // 회복 대상 없음 표시
        }

        private void BindButton() // 회복 디버그 버튼 이벤트 연결
        {
            if (bound || healButton == null) // 기존 연결 및 버튼 존재 확인
            {
                return; // 중복 버튼 연결 중단
            }

            healButton.onClick.AddListener(DebugHealFirstDamagedAlly); // 회복 디버그 버튼 이벤트 연결
            bound = true; // 버튼 연결 완료 기록
        }

        private IEnumerator UpdateStatusNextFrame() // 전투 초기화 이후 상태 문구 갱신
        {
            yield return null; // BattleScreenController 초기화 완료 대기
            SetStatus("실제 피해/회복 활성화 · HP 0 대상은 전투 행동 중지"); // 13일차 활성 상태 표시
        }

        private void SetStatus(string value) // 전투 상태 텍스트 안전 설정
        {
            if (statusText != null) // 전투 상태 텍스트 확인
            {
                statusText.text = value; // 전투 상태 문구 적용
            }
        }
    }
}
