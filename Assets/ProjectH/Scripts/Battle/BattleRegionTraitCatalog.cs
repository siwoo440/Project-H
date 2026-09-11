using System; // 문자열 비교 기능
using System.Collections.Generic; // 목록 자료형
using UnityEngine; // 수학 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public enum BattleRegionTraitKind // 지역 특징 종류 (Day65 신규)
    {
        None = 0, // 특징 없음
        ManaSurge = 1, // 노아르 : 마나 폭주
        SpiritBlessing = 2 // 실바란 : 정령의 가호
    }

    public static class BattleRegionTraitCatalog // 지역 특징 수치표 (Day65 신규 — 수치는 67일차 1차 밸런스에서 조정)
    {
        public const float ManaSurgeInterval = 10f; // 마나 폭주 간격 (초)
        public const float ManaSurgeStep = 0.08f; // 1중첩당 적 공격력 +8% · 방어력 -8%
        public const int ManaSurgeMaxStacks = 5; // 최대 5중첩 (공격 +40% · 방어 -40%)
        public const float SpiritBlessingInterval = 5f; // 정령의 가호 간격 (초)
        public const float SpiritBlessingHealRatio = 0.03f; // 적 최대 체력 3% 회복
        public const float SpiritBlessingDebuffedMultiplier = 0.5f; // 약화 상태인 적은 회복 절반
        public const string ManaAttackSourceKey = "REGION_MANA_ATK"; // 마나 폭주 공격 증가 출처
        public const string ManaDefenseSourceKey = "REGION_MANA_DEF"; // 마나 폭주 방어 감소 출처

        public static BattleRegionTraitKind GetKind(string dungeonId) // 던전의 지역 특징
        {
            if (string.Equals(dungeonId, "DG005", StringComparison.Ordinal)) return BattleRegionTraitKind.ManaSurge; // 노아르 금지된 지하 서고
            if (string.Equals(dungeonId, "DG006", StringComparison.Ordinal)) return BattleRegionTraitKind.SpiritBlessing; // 실바란 울부짖는 정령의 숲
            return BattleRegionTraitKind.None; // 그 외 던전
        }

        public static float GetInterval(BattleRegionTraitKind kind) => kind == BattleRegionTraitKind.ManaSurge ? ManaSurgeInterval : SpiritBlessingInterval; // 발동 간격

        public static string GetName(BattleRegionTraitKind kind) // 특징 이름
        {
            switch (kind) // 종류 분기
            {
                case BattleRegionTraitKind.ManaSurge: return "마나 폭주"; // 노아르
                case BattleRegionTraitKind.SpiritBlessing: return "정령의 가호"; // 실바란
                default: return string.Empty; // 없음
            }
        }

        public static string GetDescription(BattleRegionTraitKind kind) // 특징 설명 (지도 패널 표시)
        {
            switch (kind) // 종류 분기
            {
                case BattleRegionTraitKind.ManaSurge: return $"{ManaSurgeInterval:0}초마다 적 공격력 +{ManaSurgeStep * 100f:0}% · 방어력 -{ManaSurgeStep * 100f:0}% (최대 {ManaSurgeMaxStacks}중첩). 오래 끌수록 위험하니 빠르게 끝내세요."; // 노아르
                case BattleRegionTraitKind.SpiritBlessing: return $"{SpiritBlessingInterval:0}초마다 적이 최대 체력 {SpiritBlessingHealRatio * 100f:0}% 회복. 약화(방어·공격 감소, 독·화상 등)가 걸린 적은 절반만 회복해요."; // 실바란
                default: return string.Empty; // 없음
            }
        }
    }

    public sealed class BattleRegionTraitTicker // 전투 중 지역 특징 발동기 (Day65 신규 — BattleSkillRuntimeDriver가 전투마다 새로 가짐)
    {
        private readonly List<BattleStatusEffectSnapshot> statusBuffer = new List<BattleStatusEffectSnapshot>(); // 약화 확인용 버퍼
        private float elapsed; // 누적 시간
        private int manaStacks; // 마나 폭주 중첩

        public int ManaStacks => manaStacks; // 현재 중첩

        public int Tick(BattleCombatRegistry registry, BattleRegionTraitKind kind, float deltaTime) // 시간 진행 후 발동 횟수 반환
        {
            if (registry == null || kind == BattleRegionTraitKind.None || deltaTime <= 0f || registry.CountLiving(BattleTeam.Enemy) <= 0) return 0; // 적이 있을 때만
            elapsed += deltaTime; // 시간 누적
            float interval = BattleRegionTraitCatalog.GetInterval(kind); // 간격
            int pulses = 0; // 발동 수

            while (elapsed >= interval) // 간격마다
            {
                elapsed -= interval; // 누적 차감
                Pulse(registry, kind); // 발동
                pulses++; // 발동 수 증가
            }

            return pulses; // 발동 수 반환
        }

        private void Pulse(BattleCombatRegistry registry, BattleRegionTraitKind kind) // 특징 1회 발동
        {
            if (kind == BattleRegionTraitKind.ManaSurge) // 마나 폭주
            {
                manaStacks = Mathf.Min(BattleRegionTraitCatalog.ManaSurgeMaxStacks, manaStacks + 1); // 중첩 증가
                int count = ApplyManaSurge(registry, manaStacks); // 적용
                Debug.Log($"[Project H][REGION] 마나 폭주 {manaStacks}중첩 · 적 {count}명"); // 로그
                return; // 종료
            }

            int healed = ApplySpiritBlessing(registry, statusBuffer); // 정령의 가호
            Debug.Log($"[Project H][REGION] 정령의 가호 · 적 {healed}명 회복"); // 로그
        }

        public static int ApplyManaSurge(BattleCombatRegistry registry, int stacks) // 살아 있는 적 전원에 중첩 수치 적용 (같은 출처라 값이 갱신됨)
        {
            float value = BattleRegionTraitCatalog.ManaSurgeStep * Mathf.Clamp(stacks, 0, BattleRegionTraitCatalog.ManaSurgeMaxStacks); // 현재 수치
            int count = 0; // 적용 수
            if (registry == null || value <= 0f) return 0; // 입력 확인

            foreach (BattleActor actor in registry.Actors) // 전투 객체 순회
            {
                if (actor == null || actor.Team != BattleTeam.Enemy || !actor.IsCombatReady || !actor.Stats.IsAlive) continue; // 살아 있는 적만
                BattleSkillRuntimeState.AddModifier(actor.Stats.RuntimeId, BattleRuntimeModifierKind.AttackPercent, value, float.PositiveInfinity, BattleRegionTraitCatalog.ManaAttackSourceKey); // 공격 증가 (전투 내내)
                BattleSkillRuntimeState.AddModifier(actor.Stats.RuntimeId, BattleRuntimeModifierKind.DefenseReductionPercent, value, float.PositiveInfinity, BattleRegionTraitCatalog.ManaDefenseSourceKey); // 방어 감소 (전투 내내)
                count++; // 적용 수 증가
            }

            return count; // 적용 수 반환
        }

        public static int ApplySpiritBlessing(BattleCombatRegistry registry, List<BattleStatusEffectSnapshot> buffer = null) // 살아 있는 적 회복 (약화 상태면 절반)
        {
            int healed = 0; // 회복한 적 수
            if (registry == null) return 0; // 입력 확인
            buffer = buffer ?? new List<BattleStatusEffectSnapshot>(); // 버퍼 준비

            foreach (BattleActor actor in registry.Actors) // 전투 객체 순회
            {
                if (actor == null || actor.Team != BattleTeam.Enemy || !actor.IsCombatReady || !actor.Stats.IsAlive) continue; // 살아 있는 적만
                float ratio = BattleRegionTraitCatalog.SpiritBlessingHealRatio * (IsDebuffed(actor.Stats.RuntimeId, buffer) ? BattleRegionTraitCatalog.SpiritBlessingDebuffedMultiplier : 1f); // 회복 비율
                int amount = Mathf.RoundToInt(actor.Stats.MaxHp * ratio); // 회복량
                if (amount > 0 && actor.ApplyHealing(BattleHealingResolver.Resolve(actor.Stats, amount)) > 0) healed++; // 실제 회복
            }

            return healed; // 회복한 적 수 반환
        }

        public static bool IsDebuffed(string runtimeId, List<BattleStatusEffectSnapshot> buffer) // 약화 상태 여부
        {
            buffer.Clear(); // 버퍼 비움
            BattleSkillRuntimeState.CollectStatusEffects(runtimeId, buffer); // 활성 상태이상 수집

            foreach (BattleStatusEffectSnapshot snapshot in buffer) // 상태 순회
            {
                if (BattleStatusEffectCatalog.IsDebuff(snapshot.Id)) return true; // 약화 있음
            }

            return false; // 약화 없음
        }
    }
}
