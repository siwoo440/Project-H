using ProjectH.Core; // 설정(흔들림 줄이기) 기능
using UnityEngine; // Unity 기본 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 움직임 방지
    public sealed class BattleUnitMotion : MonoBehaviour // 전투 유닛 움직임 (Day79 신규 — 대기 오르내림 · 공격 튀어나감 · 피격 밀림 · 쓰러짐. 바디 그림만 움직이고 유닛의 실제 위치는 그대로다)
    {
        private RectTransform body; // 움직일 바디 그림
        private Canvas canvas; // 겹침 순서를 정할 유닛 캔버스
        private Vector2 basePosition; // 바디의 원래 자리
        private float forward = 1f; // 전진 방향 (아군 +1 · 적군 -1)
        private float phase; // 대기 오르내림 박자
        private float lungeElapsed = -1f; // 공격 움직임 경과 시간 (음수 = 없음)
        private float recoilElapsed = -1f; // 피격 움직임 경과 시간 (음수 = 없음)
        private float defeatElapsed = -1f; // 쓰러짐 경과 시간 (음수 = 없음)
        private int appliedSortingOrder = int.MinValue; // 마지막으로 적용한 겹침 순서

        public bool IsDefeatPlaying => defeatElapsed >= 0f; // 쓰러지는 중인지

        public void Configure(RectTransform bodyRect, float forwardDirection, string runtimeId) // 움직일 대상 연결 (유닛을 다시 묶을 때마다 불러도 된다)
        {
            body = bodyRect; // 바디
            canvas = body == null ? null : body.GetComponentInParent<Canvas>(); // 유닛 캔버스
            basePosition = Vector2.zero; // 바디는 앵커로 배치되므로 원래 자리는 0
            forward = forwardDirection >= 0f ? 1f : -1f; // 전진 방향
            phase = BattleUnitMotionMath.GetPhase(runtimeId); // 유닛별 박자
            ResetPose(); // 자세 초기화 (부활 대응)
            ApplySortingOrder(); // 겹침 순서
        }

        public void PlayLunge() // 공격 : 앞으로 튀어나갔다 돌아옴
        {
            if (!IsDefeatPlaying) lungeElapsed = 0f; // 쓰러진 뒤에는 움직이지 않음
        }

        public void PlayRecoil() // 피격 : 뒤로 밀렸다 돌아옴
        {
            if (!IsDefeatPlaying) recoilElapsed = 0f; // 쓰러진 뒤에는 움직이지 않음
        }

        public void PlayDefeat() // 쓰러짐 : 뒤로 기울며 내려앉음
        {
            if (IsDefeatPlaying) return; // 이미 쓰러지는 중
            defeatElapsed = 0f; // 쓰러짐 시작
            lungeElapsed = -1f; // 공격 움직임 중단
            recoilElapsed = -1f; // 피격 움직임 중단
        }

        public void ResetPose() // 자세 초기화 (원래 자리 · 기울임 없음)
        {
            lungeElapsed = -1f; // 공격 움직임 없음
            recoilElapsed = -1f; // 피격 움직임 없음
            defeatElapsed = -1f; // 쓰러짐 없음
            if (body == null) return; // 바디 없음
            body.anchoredPosition = basePosition; // 원래 자리
            body.localRotation = Quaternion.identity; // 기울임 없음
        }

        private void Update() // 매 프레임 자세 계산 (전투가 멈추면 함께 멈추도록 게임 시간을 쓴다)
        {
            if (body == null) return; // 바디 없음
            float delta = Time.deltaTime; // 게임 시간
            float scale = BattleUnitMotionMath.GetMotionScale(GameSettings.ReduceShake); // 설정에 따른 배율
            float offsetX = 0f; // 가로 움직임
            float offsetY = 0f; // 세로 움직임
            float tilt = 0f; // 기울임

            if (IsDefeatPlaying) // 쓰러지는 중
            {
                defeatElapsed += delta; // 시간 경과
                float progress = BattleUnitMotionMath.GetDefeatProgress(defeatElapsed); // 진행률
                tilt = forward * BattleUnitMotionMath.DefeatTilt * progress; // 뒤로 기울어짐 (전진 방향의 반대로 넘어간다)
                offsetY = -BattleUnitMotionMath.DefeatDrop * progress; // 내려앉음
            }
            else // 서 있는 중
            {
                offsetY = BattleUnitMotionMath.Idle(Time.time, phase) * scale; // 대기 오르내림

                if (lungeElapsed >= 0f) // 공격 움직임
                {
                    offsetX += forward * BattleUnitMotionMath.LungeDistance * scale * BattleUnitMotionMath.Pulse(lungeElapsed, BattleUnitMotionMath.LungeOutSeconds, BattleUnitMotionMath.LungeBackSeconds); // 앞으로
                    lungeElapsed = BattleUnitMotionMath.IsPulseDone(lungeElapsed, BattleUnitMotionMath.LungeOutSeconds, BattleUnitMotionMath.LungeBackSeconds) ? -1f : lungeElapsed + delta; // 끝나면 정리
                }

                if (recoilElapsed >= 0f) // 피격 움직임
                {
                    offsetX -= forward * BattleUnitMotionMath.RecoilDistance * scale * BattleUnitMotionMath.Pulse(recoilElapsed, BattleUnitMotionMath.RecoilOutSeconds, BattleUnitMotionMath.RecoilBackSeconds); // 뒤로
                    recoilElapsed = BattleUnitMotionMath.IsPulseDone(recoilElapsed, BattleUnitMotionMath.RecoilOutSeconds, BattleUnitMotionMath.RecoilBackSeconds) ? -1f : recoilElapsed + delta; // 끝나면 정리
                }
            }

            body.anchoredPosition = basePosition + new Vector2(offsetX, offsetY); // 그림 위치 반영
            body.localRotation = Quaternion.Euler(0f, 0f, tilt); // 기울임 반영
            ApplySortingOrder(); // 겹침 순서 갱신 (이동하면 달라진다)
        }

        private void ApplySortingOrder() // 화면 아래쪽 유닛이 앞에 그려지게
        {
            if (canvas == null) return; // 캔버스 없음
            int order = BattleUnitMotionMath.GetSortingOrder(transform.position.y); // 세로 위치로 순서 계산
            if (order == appliedSortingOrder) return; // 변화 없음
            appliedSortingOrder = order; // 기록
            canvas.sortingOrder = order; // 적용
        }
    }
}
