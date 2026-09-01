using System.Collections.Generic; // 목록 자료형
using UnityEngine; // Unity 컴포넌트 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 전투 레지스트리 방지
    public sealed class BattleCombatRegistry : MonoBehaviour // 전투 객체 등록 관리자
    {
        private readonly List<BattleActor> actors = new List<BattleActor>(); // 전체 전투 객체 목록
        public IReadOnlyList<BattleActor> Actors => actors; // 전체 전투 객체 반환

        public void Register(BattleActor actor) // 전투 객체 등록
        {
            if (actor == null || actors.Contains(actor)) // 전투 객체 유효성 및 중복 확인
            {
                return; // 중복 등록 중단
            }

            actors.Add(actor); // 전투 객체 목록 추가
        }

        public void Unregister(BattleActor actor) // 전투 객체 등록 해제
        {
            if (actor == null) // 전투 객체 확인
            {
                return; // 등록 해제 중단
            }

            actors.Remove(actor); // 전투 객체 목록 제거
        }

        public void Clear() // 전투 객체 등록 전체 초기화
        {
            actors.Clear(); // 전투 객체 목록 초기화
        }

        public BattleActor FindNearestOpponent(BattleActor source) // 가장 가까운 생존 상대 조회
        {
            return BattleTargetSelector.SelectNearest(source, actors); // 공통 타겟 선택 결과 반환
        }
    }
}
