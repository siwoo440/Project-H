using System; // 이벤트 기능
using System.Collections.Generic; // 목록 자료형
using UnityEngine; // Unity 컴포넌트 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 전투 레지스트리 방지
    public sealed class BattleCombatRegistry : MonoBehaviour // 전투 객체 등록 관리자
    {
        private readonly List<BattleActor> actors = new List<BattleActor>(); // 전체 전투 객체 목록
        public IReadOnlyList<BattleActor> Actors => actors; // 전체 전투 객체 반환
        public event Action<BattleActor> ActorUnregistered; // 전투 객체 Registry 제외 이벤트

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

            bool removed = actors.Remove(actor); // 전투 객체 목록 제거 및 결과 저장

            if (removed) // 실제 Registry 제외 여부 확인
            {
                ActorUnregistered?.Invoke(actor); // 전투 객체 제외 이벤트 발생
            }
        }

        public bool Contains(BattleActor actor) // 전투 객체 등록 여부 확인
        {
            return actor != null && actors.Contains(actor); // 전투 객체 등록 여부 반환
        }

        public int CountLiving(BattleTeam team) // 팀별 생존 등록 객체 수 계산
        {
            int count = 0; // 생존 객체 수 초기화

            for (int index = 0; index < actors.Count; index++) // 전체 전투 객체 순회
            {
                BattleActor actor = actors[index]; // 현재 전투 객체 조회

                if (actor == null || actor.Team != team || !actor.IsCombatReady || !actor.Stats.IsAlive) // 팀 및 생존 상태 확인
                {
                    continue; // 생존 집계 대상 제외
                }

                count++; // 생존 객체 수 증가
            }

            return count; // 팀별 생존 객체 수 반환
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
