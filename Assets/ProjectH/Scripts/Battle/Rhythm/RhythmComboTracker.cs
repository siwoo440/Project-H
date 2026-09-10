using UnityEngine; // Unity 수학 기능

namespace ProjectH.Battle.Rhythm // 프로젝트 전투 리듬 영역 (Day50)
{
    public sealed class RhythmComboTracker // 리듬 챌린지 콤보 추적 기능 (Day50 신규, MonoBehaviour 비의존 순수 클래스)
    {
        public const int MidTierCombo = 5; // 중간 콤보 색상 전환 기준
        public const int HighTierCombo = 10; // 상위 콤보 색상 전환 기준
        private static readonly Color LowTierColor = new Color(1f, 1f, 1f, 0.95f); // 낮은 콤보 표시 색상
        private static readonly Color MidTierColor = new Color(0.55f, 0.85f, 1f, 1f); // 중간 콤보 표시 색상
        private static readonly Color HighTierColor = new Color(1f, 0.86f, 0.28f, 1f); // 상위 콤보 표시 색상

        public int CurrentCombo { get; private set; } // 현재 연속 성공 판정 수
        public int MaxCombo { get; private set; } // 이번 챌린지 최고 콤보 수

        public int Register(RhythmHitResult result) // 판정 결과 기반 콤보 갱신
        {
            if (RhythmCircleJudge.IsHit(result)) // Perfect 또는 Good 성공 판정 확인
            {
                CurrentCombo++; // 연속 성공 콤보 증가
                MaxCombo = Mathf.Max(MaxCombo, CurrentCombo); // 최고 콤보 기록 갱신
            }
            else // Miss 판정 처리
            {
                CurrentCombo = 0; // 연속 성공 콤보 초기화
            }

            return CurrentCombo; // 갱신된 현재 콤보 반환
        }

        public void Reset() // 콤보 추적 상태 초기화
        {
            CurrentCombo = 0; // 현재 콤보 초기화
            MaxCombo = 0; // 최고 콤보 초기화
        }

        public static Color GetTierColor(int combo) // 콤보 구간별 표시 색상 반환
        {
            if (combo >= HighTierCombo) // 상위 콤보 구간 확인
            {
                return HighTierColor; // 상위 콤보 금색 반환
            }

            if (combo >= MidTierCombo) // 중간 콤보 구간 확인
            {
                return MidTierColor; // 중간 콤보 하늘색 반환
            }

            return LowTierColor; // 낮은 콤보 흰색 반환
        }
    }
}
