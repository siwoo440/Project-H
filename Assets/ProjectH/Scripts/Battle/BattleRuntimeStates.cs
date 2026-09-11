namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleRuntimeStates // 전투 정적 Runtime 상태 초기화 단일 창구 (최적화 통합 — 새 전투 저장소는 여기에만 추가)
    {
        // 초기화 규칙
        // · 전투 중 효과 저장소(스킬·상태이상, 궁극기 게이지)는 전투 시작 시 초기화한다.
        // · 등록형 저장소(속성, 흐트러짐)는 전투 시작 시 지우지 않는다.
        //   스탯 생성과 Start() 순서가 보장되지 않아 등록이 지워질 수 있기 때문이다 (Day52).
        //   대신 스탯 생성자가 같은 RuntimeId의 잔여 상태를 정리한다 (Day55).
        // · 선택 상태는 이벤트 구독을 유지해야 하므로 전투 중에는 Clear만 한다 (ResetAll은 구독까지 제거).

        public static void BeginBattle(BattleCombatRegistry registry) // 전투 시작 초기화 (스킬 실행기 설정 시점)
        {
            BattleSkillRuntimeState.SetRegistry(registry); // 스킬 Runtime 상태에 현재 Registry 연결
            BattleSkillRuntimeState.ResetAll(); // 스킬·상태이상 효과 초기화
            BattleUltimateGaugeRuntimeState.ResetAll(); // 궁극기 게이지 초기화
            BattleBondRuntimeState.ResetBattleEffects(); // 결속 전투 효과 초기화 (등록 단계는 유지, Day59 추가)
            BattleRuneRuntimeState.ResetBattleEffects(); // 룬 전투 효과 초기화 (등록 룬은 유지, Day60 추가)
        }

        public static void EndBattle() // 전투 종료 초기화 (Runtime Driver 제거 시점)
        {
            BattleUltimateGaugeRuntimeState.ResetAll(); // 궁극기 게이지 초기화
            BattleSkillRuntimeState.ResetAll(); // 스킬·상태이상 효과 초기화
            BattleDisarrayRuntimeState.ResetAll(); // 흐트러짐 등록 초기화
            BattleElementRuntimeState.ResetAll(); // 속성 등록 초기화
            BattlePassiveRuntimeState.ResetAll(); // 패시브 효과 초기화
            BattleBondRuntimeState.ResetAll(); // 결속 단계·효과 초기화 (Day59 추가)
            BattleRuneRuntimeState.ResetAll(); // 룬 등록·효과 초기화 (Day60 추가)
            BattleSelectionRuntimeState.Clear(); // 캐릭터 선택 해제 (구독 유지)
            BattleSkillRuntimeState.SetRegistry(null); // Registry 연결 해제
        }

        public static void ResetAll() // 전투 범위 정적 상태 전체 초기화 (테스트 SetUp·TearDown 권장 진입점)
        {
            EndBattle(); // 전투 종료와 동일한 전체 초기화
        }
    }
}
