using System.Collections.Generic; // 목록 자료형
using ProjectH.Core; // 게임 관리자 기능
using ProjectH.Data; // 캐릭터 및 스킬 데이터 기능
using UnityEngine; // Unity 기본 기능

namespace ProjectH.Battle.SkillBlock // 스킬 블록 전투 영역
{
    [DisallowMultipleComponent] // 중복 스킬 블록 컨트롤러 방지
    public sealed class BattleSkillBlockController : MonoBehaviour // 블록 생성·이동·강화·소비 전투 컨트롤러
    {
        [SerializeField] private BattleCombatRegistry registry; // 전투 객체 레지스트리
        [SerializeField] private BattleSkillBlockPanel panel; // 스킬 블록 UI 패널
        [SerializeField] private BattleSkillExecutor executor; // 스킬 사용 요청 실행기
        [SerializeField, Min(1)] private int maxBlockCount = 7; // 최대 보유 블록 수
        [SerializeField, Min(0.1f)] private float generationInterval = 1.5f; // 블록 생성 주기
        private BattleSkillBlockQueue queue; // 현재 전투 블록 Queue
        private float generationTimer; // 다음 블록 생성 시간 누적
        private float intervalMultiplier = 1f; // 생성 주기 배율 (Day60 스킬의 룬, 1 = 기본)
        private int nextRuntimeBlockId; // 다음 블록 런타임 순번
        private bool registryBound; // Registry 이벤트 연결 상태
        public BattleSkillBlockQueue Queue => queue; // 현재 블록 Queue 반환
        public bool CanInteract => IsBattleRunning() && Time.timeScale > 0f; // 현재 블록 조작 가능 상태 반환

        public void Configure(BattleCombatRegistry combatRegistry, BattleSkillBlockPanel blockPanel, BattleSkillExecutor skillExecutor, int maximumBlocks, float intervalSeconds) // 스킬 블록 시스템 참조 설정
        {
            UnbindRegistry(); // 기존 Registry 이벤트 연결 해제
            registry = combatRegistry; // 전투 Registry 연결
            panel = blockPanel; // 스킬 블록 패널 연결
            executor = skillExecutor; // 스킬 실행기 연결
            maxBlockCount = Mathf.Max(1, maximumBlocks); // 최대 블록 수 보정
            generationInterval = Mathf.Max(0.1f, intervalSeconds); // 블록 생성 주기 보정
            BindRegistry(); // 신규 Registry 이벤트 연결
        }

        public void SetIntervalReduction(float reduction) // 스킬의 룬 생성 주기 단축 설정 (Day60 추가, 0.2 = 20% 단축)
        {
            intervalMultiplier = Mathf.Clamp(1f - reduction, 0.4f, 1f); // 최대 60% 단축까지 허용
        }

        private void Start() // 스킬 블록 시스템 시작
        {
            queue = new BattleSkillBlockQueue(maxBlockCount); // 전투 블록 Queue 생성
            generationTimer = 0f; // 블록 생성 시간 초기화
            nextRuntimeBlockId = 0; // 블록 런타임 순번 초기화
            executor?.Configure(registry); // 스킬 실행기 Registry 연결
            panel?.BindController(this, maxBlockCount); // 블록 패널 Runtime 컨트롤러 연결
            panel?.Refresh(queue.Blocks); // 초기 빈 Queue 표시
            BindRegistry(); // Registry 이벤트 연결 보장
        }

        private void Update() // 주기적 스킬 블록 생성 갱신
        {
            if (queue == null || !IsBattleRunning()) // Queue 및 전투 진행 상태 확인
            {
                panel?.SetInteractable(false); // 비전투 상태 블록 입력 차단
                return; // 블록 생성 갱신 중단
            }

            panel?.SetInteractable(Time.timeScale > 0f); // Pause 상태 기반 블록 입력 갱신

            if (queue.Count >= queue.MaxCount) // 최대 보유 블록 도달 확인
            {
                generationTimer = Mathf.Min(generationTimer, generationInterval * intervalMultiplier); // 가득 찬 상태 생성 시간 과누적 방지
                return; // 추가 블록 생성 중단
            }

            generationTimer += Time.deltaTime; // 전투 시간 기반 생성 시간 누적

            while (generationTimer >= generationInterval * intervalMultiplier && queue.Count < queue.MaxCount) // 생성 주기 도달 및 빈 슬롯 확인 (Day60 스킬의 룬 배율)
            {
                generationTimer -= generationInterval * intervalMultiplier; // 블록 생성 주기 차감

                if (!TryGenerateBlock()) // 신규 스킬 블록 생성 시도
                {
                    generationTimer = 0f; // 생성 후보 없음 시 생성 시간 초기화
                    break; // 블록 생성 반복 중단
                }
            }
        }

        public bool MoveBlock(int fromIndex, int toIndex) // UI Drag 기반 블록 순서 변경
        {
            if (!CanInteract || queue == null) // 블록 조작 가능 상태 확인
            {
                return false; // 블록 이동 실패 반환
            }

            bool moved = queue.Move(fromIndex, toIndex); // Queue 블록 순서 이동

            if (moved) // 블록 이동 성공 확인
            {
                panel?.Refresh(queue.Blocks); // 이동 후 강화도 및 UI 재계산
            }

            return moved; // 블록 이동 결과 반환
        }

        public bool UseBlockAt(int index) // 클릭한 블록의 강화 그룹 사용
        {
            if (!CanInteract || queue == null || executor == null) // 블록 사용 가능 상태 확인
            {
                return false; // 스킬 블록 사용 실패 반환
            }

            BattleSkillChain chain = BattleSkillChainResolver.ResolveAtIndex(queue.Blocks, index); // 클릭 블록 소속 강화 그룹 판정

            if (!chain.IsValid) // 강화 그룹 유효성 확인
            {
                return false; // 잘못된 강화 그룹 사용 차단
            }

            BattleSkillBlock block = queue[chain.StartIndex]; // 강화 그룹 대표 스킬 블록 조회
            BattleSkillRequest request = BattleSkillRequest.FromChain(block, chain); // SkillId와 강화도 분리 사용 요청 생성

            if (!executor.TryExecute(request)) // 스킬 사용 요청 실행
            {
                return false; // 스킬 사용 요청 실패 시 블록 미소비
            }

            queue.Consume(chain.StartIndex, chain.Count); // 사용한 동일 SkillId 블록 묶음 소비
            panel?.Refresh(queue.Blocks); // 블록 소비 후 Queue UI 갱신
            return true; // 스킬 블록 사용 성공 반환
        }

        private bool TryGenerateBlock() // 현재 생존 파티에서 신규 스킬 블록 생성
        {
            List<SkillData> candidates = CollectEligibleSkills(); // 현재 생성 가능한 스킬 목록 수집

            if (candidates.Count == 0) // 생성 가능한 스킬 존재 확인
            {
                return false; // 신규 블록 생성 실패 반환
            }

            int selectedIndex = Random.Range(0, candidates.Count); // 생성 스킬 랜덤 인덱스 선택
            SkillData selectedSkill = candidates[selectedIndex]; // 생성 대상 SkillData 선택
            string runtimeId = $"SKILL_BLOCK_{nextRuntimeBlockId:0000}"; // 블록 런타임 ID 생성
            nextRuntimeBlockId++; // 다음 블록 런타임 순번 증가
            bool added = queue.TryAdd(new BattleSkillBlock(runtimeId, selectedSkill)); // 신규 스킬 블록 Queue 추가

            if (added) // 블록 Queue 추가 성공 확인
            {
                panel?.Refresh(queue.Blocks); // 신규 블록 UI 표시 및 강화도 갱신
            }

            return added; // 신규 블록 생성 결과 반환
        }

        private List<SkillData> CollectEligibleSkills() // 생존 아군의 Skill 1·2·3 생성 후보 수집
        {
            List<SkillData> result = new List<SkillData>(); // 생성 가능한 스킬 목록 생성

            if (registry == null || GameManager.Instance == null || GameManager.Instance.Data == null) // Registry 및 데이터 관리자 확인
            {
                return result; // 빈 생성 후보 목록 반환
            }

            HashSet<string> addedSkillIds = new HashSet<string>(); // 중복 스킬 ID 방지 집합 생성

            for (int actorIndex = 0; actorIndex < registry.Actors.Count; actorIndex++) // 전체 전투 액터 순회
            {
                BattleActor actor = registry.Actors[actorIndex]; // 현재 전투 액터 조회

                if (actor == null || actor.Team != BattleTeam.Ally || !actor.IsCombatReady || !actor.Stats.IsAlive) // 살아있는 아군 여부 확인
                {
                    continue; // 블록 생성 캐릭터 후보 제외
                }

                BattleStats stats = actor.Stats as BattleStats; // 캐릭터 전투 스탯 변환

                if (stats == null) // 캐릭터 전투 스탯 확인
                {
                    continue; // 잘못된 아군 데이터 제외
                }

                CharacterData character = GameManager.Instance.Data.GetCharacter(stats.CharacterId); // 캐릭터 원본 데이터 조회

                if (character == null || character.Skills == null) // 캐릭터 및 스킬 목록 확인
                {
                    continue; // 스킬 미설정 캐릭터 제외
                }

                for (int skillIndex = 0; skillIndex < character.Skills.Count; skillIndex++) // 캐릭터 Skill 1·2·3 순회
                {
                    SkillData skill = character.Skills[skillIndex]; // 현재 SkillData 조회

                    if (skill == null || string.IsNullOrWhiteSpace(skill.Id) || !addedSkillIds.Add(skill.Id)) // 스킬 데이터 및 중복 ID 확인
                    {
                        continue; // 잘못된 또는 중복 스킬 제외
                    }

                    result.Add(skill); // 블록 생성 후보 스킬 추가
                }
            }

            return result; // 생존 파티 스킬 생성 후보 반환
        }

        private bool IsBattleRunning() // 블록 시스템 기준 전투 진행 상태 확인
        {
            if (registry == null) // 전투 Registry 확인
            {
                return false; // 전투 진행 아님 반환
            }

            return registry.CountLiving(BattleTeam.Ally) > 0 && registry.CountLiving(BattleTeam.Enemy) > 0; // 양 팀 생존 시 전투 진행 반환
        }

        private void HandleActorUnregistered(BattleActor actor) // 사망 또는 제외 전투 액터 처리
        {
            if (queue == null || actor == null) // Queue 및 제외 액터 확인
            {
                return; // 제외 액터 처리 중단
            }

            if (actor.Team == BattleTeam.Ally) // 아군 제외 여부 확인
            {
                BattleStats stats = actor.Stats as BattleStats; // 제외 아군 전투 스탯 변환

                if (stats != null) // 캐릭터 전투 스탯 확인
                {
                    queue.RemoveByCharacterId(stats.CharacterId); // 사망 캐릭터 보유 블록 일괄 제거
                    panel?.Refresh(queue.Blocks); // 사망 캐릭터 블록 제거 UI 갱신
                }
            }

            if (!IsBattleRunning()) // 전투 종료 상태 확인
            {
                panel?.SetInteractable(false); // 승패 결정 시 블록 조작 차단
            }
        }

        private void BindRegistry() // Registry 제외 이벤트 연결
        {
            if (registryBound || registry == null) // 기존 연결 및 Registry 존재 확인
            {
                return; // 중복 Registry 이벤트 연결 중단
            }

            registry.ActorUnregistered += HandleActorUnregistered; // 전투 객체 제외 이벤트 구독
            registryBound = true; // Registry 이벤트 연결 완료 기록
        }

        private void UnbindRegistry() // Registry 제외 이벤트 연결 해제
        {
            if (!registryBound || registry == null) // Registry 이벤트 연결 상태 확인
            {
                return; // Registry 이벤트 해제 중단
            }

            registry.ActorUnregistered -= HandleActorUnregistered; // 전투 객체 제외 이벤트 구독 해제
            registryBound = false; // Registry 이벤트 연결 상태 초기화
        }

        private void OnDestroy() // 스킬 블록 컨트롤러 제거 처리
        {
            UnbindRegistry(); // Registry 이벤트 연결 해제
        }
    }
}
