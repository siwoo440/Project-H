using System.Collections; // 코루틴 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 전투 액터 방지
    public sealed class BattleActor : MonoBehaviour // 공통 전투 행동 객체
    {
        [SerializeField] private Image bodyImage; // 임시 전투 바디 이미지
        [SerializeField] private BattleActionDebugText actionDebugText; // 머리 위 행동 디버그 텍스트
        [SerializeField] private BattleFloatingValueText floatingValueText; // 피해 회복 숫자 디버그 텍스트
        private Color baseBodyColor = Color.white; // 기본 바디 색상
        private Coroutine flashRoutine; // 피격 미리보기 코루틴
        public BattleTeam Team { get; private set; } // 전투 팀
        public IBattleCombatantStats Stats { get; private set; } // 공통 전투 스탯
        public Vector3 HomePosition { get; private set; } // 최초 진형 위치
        public bool IsCombatReady => Stats != null; // 전투 초기화 완료 여부
        public float ForwardDirection => Team == BattleTeam.Ally ? 1f : -1f; // 팀별 전진 방향 반환

        public void ConfigureVisuals(Image body, BattleActionDebugText debugText) // 이전 전투 시각 참조 설정
        {
            ConfigureVisuals(body, debugText, floatingValueText); // 기존 체력 변화 텍스트 유지 설정
        }

        public void ConfigureVisuals(Image body, BattleActionDebugText debugText, BattleFloatingValueText valueText) // 체력 변화 포함 전투 시각 참조 설정
        {
            bodyImage = body; // 전투 바디 이미지 연결
            actionDebugText = debugText; // 행동 디버그 텍스트 연결
            floatingValueText = valueText; // 체력 변화 텍스트 연결

            if (bodyImage != null) // 전투 바디 이미지 확인
            {
                baseBodyColor = bodyImage.color; // 현재 바디 색상 저장
            }
        }

        public void Initialize(BattleTeam team, IBattleCombatantStats stats, Vector3 homePosition) // 전투 액터 초기화
        {
            Team = team; // 전투 팀 저장
            Stats = stats; // 공통 전투 스탯 저장
            HomePosition = homePosition; // 최초 진형 위치 저장
        }

        public void SetBodyColor(Color color) // 전투 바디 기본색 설정
        {
            baseBodyColor = color; // 기본 바디 색상 저장

            if (bodyImage != null) // 전투 바디 이미지 확인
            {
                bodyImage.color = color; // 전투 바디 색상 적용
            }
        }

        public float HorizontalDistanceTo(BattleActor target) // 다른 전투 객체와 가로 거리 계산
        {
            if (target == null) // 대상 전투 객체 확인
            {
                return float.PositiveInfinity; // 대상 없음 거리 반환
            }

            return Mathf.Abs(target.transform.position.x - transform.position.x); // 가로 절대 거리 반환
        }

        public float ForwardDistanceTo(BattleActor target) // 전진 방향 기준 상대 거리 계산
        {
            if (target == null) // 대상 전투 객체 확인
            {
                return float.PositiveInfinity; // 대상 없음 거리 반환
            }

            return (target.transform.position.x - transform.position.x) * ForwardDirection; // 전진 방향 상대 거리 반환
        }

        public bool IsOpponentAhead(BattleActor target) // 상대가 전방에 있는지 확인
        {
            return target != null && ForwardDistanceTo(target) >= 0f; // 전진 방향 앞쪽 여부 반환
        }

        public bool IsWithinAttackRange(BattleActor target) // 현재 기본 공격 사거리 확인
        {
            if (!IsCombatReady || target == null) // 전투 상태 및 타겟 확인
            {
                return false; // 공격 사거리 판정 실패
            }

            float safeRange = Mathf.Max(0.2f, Stats.AttackRange); // 최소 공격 사거리 보정
            return HorizontalDistanceTo(target) <= safeRange; // 횡스크롤 가로 사거리 판정 반환
        }

        public void MoveForwardToward(BattleActor target, float deltaTime) // 전방 타겟 방향 횡스크롤 이동
        {
            if (!IsCombatReady || target == null) // 전투 상태 및 타겟 확인
            {
                return; // 전방 이동 중단
            }

            if (!IsOpponentAhead(target)) // 타겟 전방 여부 확인
            {
                return; // 이미 지나친 타겟 방향 이동 차단
            }

            float safeRange = Mathf.Max(0.2f, Stats.AttackRange); // 최소 공격 사거리 보정
            float stopX = target.transform.position.x - (ForwardDirection * safeRange); // 타겟 앞 공격 정지선 계산
            float currentX = transform.position.x; // 현재 X 위치 저장
            float maxStep = Mathf.Max(0.01f, Stats.MoveSpeed) * Mathf.Max(0f, deltaTime); // 프레임 최대 전진 거리 계산
            float nextX = Mathf.MoveTowards(currentX, stopX, maxStep); // 공격 정지선까지 전진 위치 계산

            if (Team == BattleTeam.Ally) // 아군 전진 처리
            {
                nextX = Mathf.Min(nextX, stopX); // 적군 정지선 초과 이동 방지
            }
            else // 적군 전진 처리
            {
                nextX = Mathf.Max(nextX, stopX); // 아군 정지선 초과 이동 방지
            }

            Vector3 nextPosition = transform.position; // 현재 위치 복사
            nextPosition.x = nextX; // 가로 전진 위치 적용
            transform.position = nextPosition; // 전투 객체 전진 위치 저장
        }

        public int ApplyDamage(BattleDamageResult result) // 계산 완료 피해 적용
        {
            if (!IsCombatReady || !Stats.IsAlive) // 전투 상태 및 생존 여부 확인
            {
                return 0; // 피해 적용 중단
            }

            if (result.TargetRuntimeId != Stats.RuntimeId) // 피해 대상 ID 일치 확인
            {
                return 0; // 잘못된 대상 피해 차단
            }

            IBattleMutableCombatantStats mutableStats = Stats as IBattleMutableCombatantStats; // 변경 가능 전투 스탯 변환

            if (mutableStats == null) // 변경 가능 전투 스탯 확인
            {
                return 0; // 체력 변경 불가 대상 차단
            }

            int applied = mutableStats.TakeDamage(result.Damage); // 실제 피해 적용

            if (applied <= 0) // 실제 피해 발생 확인
            {
                return 0; // 피해 시각 처리 중단
            }

            FlashHitPreview(); // 피해 피격 표시
            floatingValueText?.ShowDamage(applied); // 피해 숫자 표시
            return applied; // 실제 피해량 반환
        }

        public int ApplyHealing(BattleHealingResult result) // 계산 완료 회복 적용
        {
            if (!IsCombatReady || !Stats.IsAlive) // 전투 상태 및 생존 여부 확인
            {
                return 0; // 일반 회복 부활 차단
            }

            if (result.TargetRuntimeId != Stats.RuntimeId) // 회복 대상 ID 일치 확인
            {
                return 0; // 잘못된 대상 회복 차단
            }

            IBattleMutableCombatantStats mutableStats = Stats as IBattleMutableCombatantStats; // 변경 가능 전투 스탯 변환

            if (mutableStats == null) // 변경 가능 전투 스탯 확인
            {
                return 0; // 체력 변경 불가 대상 차단
            }

            int applied = mutableStats.Heal(result.Healing); // 실제 회복 적용

            if (applied > 0) // 실제 회복 발생 확인
            {
                floatingValueText?.ShowHealing(applied); // 회복 숫자 표시
            }

            return applied; // 실제 회복량 반환
        }

        public void ShowAction(BattleActionKind actionKind) // 머리 위 행동 텍스트 표시
        {
            actionDebugText?.Show(actionKind); // 행동 디버그 텍스트 호출
        }

        public void FlashHitPreview() // 기본 공격 적중 미리보기 표시
        {
            if (bodyImage == null) // 전투 바디 이미지 확인
            {
                return; // 적중 미리보기 중단
            }

            if (flashRoutine != null) // 기존 피격 코루틴 확인
            {
                StopCoroutine(flashRoutine); // 기존 피격 코루틴 중단
            }

            flashRoutine = StartCoroutine(FlashRoutine()); // 피격 미리보기 코루틴 시작
        }

        private IEnumerator FlashRoutine() // 피격 미리보기 색상 변화
        {
            bodyImage.color = Color.white; // 피격 순간 흰색 표시
            yield return new WaitForSeconds(0.08f); // 짧은 피격 표시 대기
            bodyImage.color = baseBodyColor; // 기본 바디 색상 복원
            flashRoutine = null; // 피격 코루틴 참조 초기화
        }
    }
}
