using System.Collections.Generic; // 목록 자료형
using UnityEngine; // Unity Transform 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public readonly struct BattleDeploymentEntry // 단일 아군 배치 항목
    {
        public int SlotIndex { get; } // 파티 슬롯 번호
        public BattleStats Stats { get; } // 전투 스탯
        public Transform Anchor { get; } // 전장 배치 앵커

        public BattleDeploymentEntry(int slotIndex, BattleStats stats, Transform anchor) // 배치 항목 생성
        {
            SlotIndex = slotIndex; // 파티 슬롯 번호 저장
            Stats = stats; // 전투 스탯 저장
            Anchor = anchor; // 배치 앵커 저장
        }
    }

    public sealed class BattleDeploymentPlan // 아군 전장 배치 계획
    {
        private readonly List<BattleDeploymentEntry> entries; // 배치 항목 목록
        public int Count => entries.Count; // 배치 인원 반환
        public IReadOnlyList<BattleDeploymentEntry> Entries => entries; // 배치 목록 반환
        public BattleDeploymentEntry this[int index] => entries[index]; // 배치 슬롯 반환

        private BattleDeploymentPlan(List<BattleDeploymentEntry> deploymentEntries) // 배치 계획 생성
        {
            entries = deploymentEntries; // 배치 항목 저장
        }

        public static bool TryCreate(BattlePartyRuntime party, BattleFormationAnchors anchors, out BattleDeploymentPlan plan, out string error) // 파티 기반 배치 계획 생성
        {
            plan = null; // 실패 기본 계획 설정
            error = string.Empty; // 오류 문구 초기화

            if (party == null) // 전투 파티 확인
            {
                error = "Battle party is missing."; // 파티 누락 오류 설정
                return false; // 배치 계획 생성 실패
            }

            if (anchors == null) // 배치 앵커 확인
            {
                error = "Battle formation anchor component is missing."; // 앵커 누락 오류 설정
                return false; // 배치 계획 생성 실패
            }

            if (party.Count > anchors.AllyCount) // 아군 앵커 개수 확인
            {
                error = $"Not enough ally anchor slots. Party={party.Count}, Anchors={anchors.AllyCount}."; // 앵커 부족 오류 설정
                return false; // 배치 계획 생성 실패
            }

            List<BattleDeploymentEntry> deploymentEntries = new List<BattleDeploymentEntry>(party.Count); // 배치 항목 목록 생성

            for (int index = 0; index < party.Count; index++) // 파티 슬롯 순회
            {
                Transform anchor = anchors.GetAllyAnchor(index); // 아군 슬롯 앵커 조회

                if (anchor == null) // 아군 앵커 존재 확인
                {
                    error = $"Ally anchor is missing. Slot={index}."; // 앵커 누락 오류 설정
                    return false; // 배치 계획 생성 실패
                }

                deploymentEntries.Add(new BattleDeploymentEntry(index, party[index], anchor)); // 파티 순서 기반 배치 항목 추가
            }

            plan = new BattleDeploymentPlan(deploymentEntries); // 배치 계획 생성
            return true; // 배치 계획 생성 성공
        }
    }
}
