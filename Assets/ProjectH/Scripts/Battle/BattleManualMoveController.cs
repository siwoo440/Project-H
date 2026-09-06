using UnityEngine; // Unity 기본 기능
using UnityEngine.EventSystems; // UI 포인터 판정 기능
using UnityEngine.InputSystem; // 신규 입력 시스템 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 수동 이동 컨트롤러 방지
    public sealed class BattleManualMoveController : MonoBehaviour // 숫자키 선택 및 우클릭 수동 이동 관리자
    {
        private const int MaxPartySlots = 4; // 지원 파티 슬롯 수
        private const float ArrivalDistance = 0.03f; // 목적지 도착 판정 거리
        private const float MarkerStemHeight = 0.55f; // 목적지 화살표 몸통 높이
        private const float MarkerWingHeight = 0.22f; // 목적지 화살표 날개 높이
        private const float MarkerWingWidth = 0.22f; // 목적지 화살표 날개 너비
        private const float MarkerLineWidth = 0.06f; // 목적지 화살표 선 굵기
        private readonly BattleActor[] allySlots = new BattleActor[MaxPartySlots]; // 파티 슬롯별 아군 액터
        private readonly BattleBasicAttackController[] attackControllers = new BattleBasicAttackController[MaxPartySlots]; // 파티 슬롯별 자동 공격 컨트롤러
        private readonly Vector3[] destinations = new Vector3[MaxPartySlots]; // 파티 슬롯별 수동 이동 목적지
        private readonly bool[] movingSlots = new bool[MaxPartySlots]; // 파티 슬롯별 수동 이동 활성 상태
        private readonly LineRenderer[] destinationMarkers = new LineRenderer[MaxPartySlots]; // 파티 슬롯별 목적지 화살표 Renderer
        private BattleCombatRegistry registry; // 현재 전투 Registry
        private Camera worldCamera; // 전장 화면 좌표 변환 카메라
        private Material destinationMarkerMaterial; // 목적지 화살표 공용 Material
        private int selectedSlotIndex = -1; // 현재 선택된 파티 슬롯 인덱스
        public int SelectedSlotNumber => selectedSlotIndex >= 0 ? selectedSlotIndex + 1 : 0; // 현재 선택 슬롯 번호 반환
        public BattleActor SelectedActor => IsSlotIndexValid(selectedSlotIndex) ? allySlots[selectedSlotIndex] : null; // 현재 선택 아군 반환

        public void Configure(BattleCombatRegistry combatRegistry, Camera targetCamera) // 수동 이동 전투 참조 설정
        {
            registry = combatRegistry; // 전투 Registry 연결
            worldCamera = targetCamera; // 월드 카메라 연결
        }

        public int RegisterAlly(BattleActor actor, BattleBasicAttackController attackController) // 파티 순서 기반 아군 슬롯 등록
        {
            if (actor == null || actor.Team != BattleTeam.Ally) // 아군 등록 대상 확인
            {
                return 0; // 잘못된 아군 등록 실패 반환
            }

            PruneStaleSlotsForRegistration(); // 신규 전투 잔여 슬롯 정리

            for (int index = 0; index < allySlots.Length; index++) // 기존 슬롯 순회
            {
                if (allySlots[index] != actor) // 현재 액터 기존 등록 여부 확인
                {
                    continue; // 다른 슬롯 확인
                }

                attackControllers[index] = attackController; // 기존 슬롯 공격 컨트롤러 갱신
                return index + 1; // 기존 슬롯 번호 반환
            }

            for (int index = 0; index < allySlots.Length; index++) // 빈 슬롯 순회
            {
                if (allySlots[index] != null) // 사용 중 슬롯 확인
                {
                    continue; // 다음 빈 슬롯 확인
                }

                allySlots[index] = actor; // 파티 슬롯 액터 저장
                attackControllers[index] = attackController; // 파티 슬롯 공격 컨트롤러 저장
                destinations[index] = actor.transform.position; // 초기 목적지 현재 위치 저장
                movingSlots[index] = false; // 초기 수동 이동 상태 해제
                HideDestinationMarker(index); // 재사용 슬롯 목적지 화살표 숨김
                return index + 1; // 신규 슬롯 번호 반환
            }

            return 0; // 파티 슬롯 초과 등록 실패 반환
        }

        public bool TrySelectSlot(int slotNumber) // 숫자키 파티 슬롯 선택 시도
        {
            int index = slotNumber - 1; // 1 기반 슬롯 번호를 배열 인덱스로 변환

            if (!IsSlotIndexValid(index)) // 슬롯 범위 확인
            {
                return false; // 잘못된 슬롯 선택 실패 반환
            }

            BattleActor actor = allySlots[index]; // 선택 대상 아군 조회

            if (!IsControllable(actor)) // 생존 및 전투 가능 아군 확인
            {
                if (selectedSlotIndex == index) // 현재 선택 슬롯과 동일 여부 확인
                {
                    ClearSelection(); // 전투 불가 선택 및 윤곽 해제
                }

                return false; // 전투 불가 아군 선택 실패 반환
            }

            selectedSlotIndex = index; // 현재 선택 슬롯 저장
            BattleSelectionRuntimeState.SetSelected(actor.Stats.RuntimeId); // 선택 캐릭터 Runtime 상태 및 윤곽 갱신
            Debug.Log($"[Project H][MANUAL MOVE] SELECT {slotNumber} · {actor.Stats.RuntimeId}"); // 파티 선택 로그 출력
            return true; // 파티 슬롯 선택 성공 반환
        }

        public bool TryIssueMove(Vector3 worldPosition) // 현재 선택 아군 수동 이동 명령 시도
        {
            if (!IsSlotIndexValid(selectedSlotIndex)) // 현재 선택 슬롯 확인
            {
                return false; // 선택 없음 이동 실패 반환
            }

            BattleActor actor = allySlots[selectedSlotIndex]; // 현재 선택 아군 조회

            if (!IsControllable(actor)) // 현재 선택 아군 생존 상태 확인
            {
                ClearSelection(); // 전투 불가 선택 및 윤곽 해제
                return false; // 전투 불가 이동 실패 반환
            }

            if (BattleSkillRuntimeState.IsStunned(actor.Stats.RuntimeId)) // 현재 선택 아군 기절 상태 확인
            {
                return false; // 기절 상태 이동 명령 차단
            }

            worldPosition.z = actor.transform.position.z; // 현재 전장 Z축 유지
            destinations[selectedSlotIndex] = worldPosition; // 현재 슬롯 목적지 갱신
            movingSlots[selectedSlotIndex] = true; // 현재 슬롯 수동 이동 활성화
            attackControllers[selectedSlotIndex]?.BeginManualMove(); // 현재 슬롯 자동 전투 일시 중지
            ShowDestinationMarker(selectedSlotIndex, worldPosition); // 우클릭 목적지 화살표 표시
            Debug.Log($"[Project H][MANUAL MOVE] MOVE {selectedSlotIndex + 1} · {actor.Stats.RuntimeId} → ({worldPosition.x:0.00}, {worldPosition.y:0.00})"); // 이동 명령 로그 출력
            return true; // 수동 이동 명령 성공 반환
        }

        public bool IsMoving(BattleActor actor) // 특정 아군 수동 이동 상태 조회
        {
            if (actor == null) // 조회 아군 확인
            {
                return false; // 아군 없음 이동 상태 반환
            }

            for (int index = 0; index < allySlots.Length; index++) // 파티 슬롯 순회
            {
                if (allySlots[index] == actor) // 조회 아군 슬롯 확인
                {
                    return movingSlots[index]; // 현재 수동 이동 상태 반환
                }
            }

            return false; // 미등록 아군 이동 상태 반환
        }

        public bool IsDestinationMarkerVisible(BattleActor actor) // 특정 아군 목적지 화살표 표시 상태 조회
        {
            if (actor == null) // 조회 아군 확인
            {
                return false; // 아군 없음 화살표 상태 반환
            }

            for (int index = 0; index < allySlots.Length; index++) // 파티 슬롯 순회
            {
                if (allySlots[index] != actor) // 조회 아군 슬롯 여부 확인
                {
                    continue; // 다른 슬롯 확인
                }

                LineRenderer marker = destinationMarkers[index]; // 현재 슬롯 목적지 화살표 조회
                return marker != null && marker.enabled; // 화살표 생성 및 표시 상태 반환
            }

            return false; // 미등록 아군 화살표 상태 반환
        }

        public void TickManualMoves(float deltaTime) // 수동 이동 상태 프레임 갱신
        {
            for (int index = 0; index < allySlots.Length; index++) // 전체 파티 슬롯 순회
            {
                if (!movingSlots[index]) // 현재 수동 이동 활성 여부 확인
                {
                    continue; // 비활성 슬롯 이동 처리 제외
                }

                BattleActor actor = allySlots[index]; // 현재 이동 아군 조회

                if (!IsControllable(actor)) // 사망 또는 전투 불가 여부 확인
                {
                    CancelMovement(index); // 전투 불가 수동 이동 취소
                    continue; // 다음 슬롯 처리
                }

                if (BattleSkillRuntimeState.IsStunned(actor.Stats.RuntimeId)) // 이동 중 기절 상태 확인
                {
                    continue; // 기절 종료까지 현재 위치 유지
                }

                if (deltaTime <= 0f) // 진행 시간 확인
                {
                    continue; // 정지 상태 이동 처리 제외
                }

                Vector3 currentPosition = actor.transform.position; // 현재 월드 위치 조회
                Vector3 targetPosition = destinations[index]; // 현재 수동 목적지 조회
                float maxStep = Mathf.Max(0.01f, actor.Stats.MoveSpeed) * deltaTime; // 캐릭터 이동속도 기반 프레임 이동량 계산
                Vector2 nextPlanar = Vector2.MoveTowards(new Vector2(currentPosition.x, currentPosition.y), new Vector2(targetPosition.x, targetPosition.y), maxStep); // X/Y 평면 다음 위치 계산
                currentPosition.x = nextPlanar.x; // 다음 X 위치 적용
                currentPosition.y = nextPlanar.y; // 다음 Y 위치 적용
                actor.transform.position = currentPosition; // 수동 이동 월드 위치 저장

                if (Vector2.Distance(nextPlanar, new Vector2(targetPosition.x, targetPosition.y)) <= ArrivalDistance) // 목적지 도착 여부 확인
                {
                    CompleteMovement(index); // 수동 이동 완료 및 자동 전투 복귀
                }
            }
        }

        private void Update() // 숫자키 선택과 우클릭 이동 입력 갱신
        {
            if (registry == null || Time.timeScale <= 0f) // 전투 Registry 및 일시정지 상태 확인
            {
                return; // 전투 입력 처리 중단
            }

            RefreshSelectionValidity(); // 현재 선택 아군 생존 상태 갱신
            HandleNumberSelection(); // 숫자키 파티 선택 입력 처리
            HandleRightClick(); // 우클릭 수동 이동 입력 처리
            TickManualMoves(Time.deltaTime); // 활성 수동 이동 프레임 갱신
        }

        private void HandleNumberSelection() // 숫자키 1~4 선택 입력 처리
        {
            Keyboard keyboard = Keyboard.current; // 현재 키보드 입력 장치 조회

            if (keyboard == null) // 키보드 장치 존재 확인
            {
                return; // 키보드 입력 처리 중단
            }

            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) // 1번 슬롯 키 입력 확인
            {
                TrySelectSlot(1); // 1번 파티 캐릭터 선택
            }
            else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) // 2번 슬롯 키 입력 확인
            {
                TrySelectSlot(2); // 2번 파티 캐릭터 선택
            }
            else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) // 3번 슬롯 키 입력 확인
            {
                TrySelectSlot(3); // 3번 파티 캐릭터 선택
            }
            else if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) // 4번 슬롯 키 입력 확인
            {
                TrySelectSlot(4); // 4번 파티 캐릭터 선택
            }
        }

        private void HandleRightClick() // 마우스 우클릭 이동 입력 처리
        {
            Mouse mouse = Mouse.current; // 현재 마우스 입력 장치 조회

            if (mouse == null || !mouse.rightButton.wasPressedThisFrame) // 우클릭 신규 입력 확인
            {
                return; // 우클릭 이동 처리 중단
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) // UI 위 포인터 여부 확인
            {
                return; // HUD 및 버튼 위 우클릭 이동 차단
            }

            if (!TryGetPointerWorldPosition(mouse.position.ReadValue(), out Vector3 worldPosition)) // 포인터 월드 좌표 변환 시도
            {
                return; // 월드 좌표 변환 실패 이동 차단
            }

            TryIssueMove(worldPosition); // 현재 선택 캐릭터 수동 이동 명령
        }

        private bool TryGetPointerWorldPosition(Vector2 screenPosition, out Vector3 worldPosition) // 화면 좌표를 전장 X/Y 위치로 변환
        {
            worldPosition = Vector3.zero; // 월드 좌표 출력 초기화
            BattleActor actor = SelectedActor; // 현재 선택 아군 조회

            if (!IsControllable(actor)) // 현재 선택 아군 확인
            {
                return false; // 선택 아군 없음 좌표 변환 실패 반환
            }

            if (worldCamera == null) // 연결 월드 카메라 확인
            {
                worldCamera = Camera.main; // 현재 메인 카메라 재조회
            }

            if (worldCamera == null) // 메인 카메라 존재 확인
            {
                return false; // 카메라 없음 좌표 변환 실패 반환
            }

            Ray ray = worldCamera.ScreenPointToRay(screenPosition); // 화면 포인터 기준 월드 Ray 생성
            Plane battlePlane = new Plane(Vector3.forward, new Vector3(0f, 0f, actor.transform.position.z)); // 현재 캐릭터 Z축 전투 평면 생성

            if (!battlePlane.Raycast(ray, out float enter)) // 전투 평면 교차 여부 확인
            {
                return false; // 전투 평면 미교차 실패 반환
            }

            worldPosition = ray.GetPoint(enter); // 전투 평면 교차 월드 위치 계산
            worldPosition.z = actor.transform.position.z; // 캐릭터 Z축 고정
            return true; // 화면 좌표 변환 성공 반환
        }

        private void ShowDestinationMarker(int index, Vector3 destination) // 수동 이동 목적지 화살표 표시
        {
            if (!IsSlotIndexValid(index)) // 화살표 슬롯 범위 확인
            {
                return; // 잘못된 화살표 표시 중단
            }

            LineRenderer marker = GetOrCreateDestinationMarker(index); // 현재 슬롯 목적지 화살표 준비

            if (marker == null) // 화살표 Renderer 생성 여부 확인
            {
                return; // 화살표 표시 실패 중단
            }

            float markerZ = destination.z - 0.05f; // 전장 오브젝트 앞쪽 화살표 Z 위치 계산
            Vector3 tip = new Vector3(destination.x, destination.y, markerZ); // 화살표 끝점 위치 계산
            Vector3 stem = new Vector3(destination.x, destination.y + MarkerStemHeight, markerZ); // 화살표 몸통 상단 위치 계산
            Vector3 leftWing = new Vector3(destination.x - MarkerWingWidth, destination.y + MarkerWingHeight, markerZ); // 화살표 왼쪽 날개 위치 계산
            Vector3 rightWing = new Vector3(destination.x + MarkerWingWidth, destination.y + MarkerWingHeight, markerZ); // 화살표 오른쪽 날개 위치 계산
            marker.SetPosition(0, stem); // 화살표 몸통 상단 점 적용
            marker.SetPosition(1, tip); // 화살표 끝점 적용
            marker.SetPosition(2, leftWing); // 화살표 왼쪽 날개 점 적용
            marker.SetPosition(3, tip); // 화살표 날개 연결 끝점 적용
            marker.SetPosition(4, rightWing); // 화살표 오른쪽 날개 점 적용
            marker.enabled = true; // 목적지 화살표 표시 활성화
        }

        private LineRenderer GetOrCreateDestinationMarker(int index) // 슬롯별 목적지 화살표 조회 또는 생성
        {
            if (!IsSlotIndexValid(index)) // 화살표 슬롯 범위 확인
            {
                return null; // 잘못된 슬롯 화살표 반환 실패
            }

            if (destinationMarkers[index] != null) // 기존 목적지 화살표 존재 확인
            {
                return destinationMarkers[index]; // 기존 화살표 반환
            }

            GameObject markerObject = new GameObject($"ManualMoveDestinationArrow_{index + 1}"); // 목적지 화살표 GameObject 생성
            markerObject.transform.SetParent(transform, false); // 수동 이동 컨트롤러 하위 연결
            LineRenderer marker = markerObject.AddComponent<LineRenderer>(); // 화살표 LineRenderer 추가
            marker.useWorldSpace = true; // 월드 좌표 기반 화살표 사용
            marker.positionCount = 5; // 화살표 몸통 및 양쪽 날개 점 개수 설정
            marker.loop = false; // 화살표 선 루프 비활성화
            marker.startWidth = MarkerLineWidth; // 화살표 시작 선 굵기 설정
            marker.endWidth = MarkerLineWidth; // 화살표 끝 선 굵기 설정
            marker.numCapVertices = 2; // 화살표 선 끝 둥근 처리
            marker.sortingOrder = 500; // 전장 캐릭터보다 앞쪽 표시 순서 설정
            marker.startColor = Color.yellow; // 화살표 시작 색상 설정
            marker.endColor = Color.yellow; // 화살표 끝 색상 설정
            Material material = GetOrCreateMarkerMaterial(); // 화살표 공용 Material 준비

            if (material != null) // 화살표 Material 생성 여부 확인
            {
                marker.sharedMaterial = material; // 목적지 화살표 공용 Material 적용
            }

            marker.enabled = false; // 생성 직후 화살표 숨김
            destinationMarkers[index] = marker; // 슬롯별 화살표 참조 저장
            return marker; // 생성 화살표 반환
        }

        private Material GetOrCreateMarkerMaterial() // 목적지 화살표 공용 Material 조회 또는 생성
        {
            if (destinationMarkerMaterial != null) // 기존 화살표 Material 존재 확인
            {
                return destinationMarkerMaterial; // 기존 Material 반환
            }

            Shader markerShader = Shader.Find("Sprites/Default"); // 기본 Sprite 화살표 Shader 조회

            if (markerShader == null) // 기본 Sprite Shader 누락 여부 확인
            {
                return null; // 기본 LineRenderer Material 사용 처리
            }

            destinationMarkerMaterial = new Material(markerShader); // 목적지 화살표 공용 Material 생성
            destinationMarkerMaterial.name = "ProjectH.ManualMoveDestinationArrow"; // Runtime 화살표 Material 이름 설정
            return destinationMarkerMaterial; // 생성 Material 반환
        }

        private void HideDestinationMarker(int index) // 슬롯별 목적지 화살표 숨김
        {
            if (!IsSlotIndexValid(index)) // 화살표 슬롯 범위 확인
            {
                return; // 잘못된 화살표 숨김 중단
            }

            LineRenderer marker = destinationMarkers[index]; // 현재 슬롯 목적지 화살표 조회

            if (marker != null) // 기존 화살표 존재 확인
            {
                marker.enabled = false; // 목적지 화살표 표시 비활성화
            }
        }

        private void CompleteMovement(int index) // 수동 이동 완료 처리
        {
            if (!IsSlotIndexValid(index)) // 완료 슬롯 범위 확인
            {
                return; // 잘못된 완료 처리 중단
            }

            movingSlots[index] = false; // 수동 이동 활성 상태 해제
            HideDestinationMarker(index); // 도착 완료 목적지 화살표 숨김
            attackControllers[index]?.EndManualMove(); // 기존 자동 타겟 탐색 재개
        }

        private void CancelMovement(int index) // 수동 이동 취소 처리
        {
            if (!IsSlotIndexValid(index)) // 취소 슬롯 범위 확인
            {
                return; // 잘못된 취소 처리 중단
            }

            movingSlots[index] = false; // 수동 이동 활성 상태 해제
            HideDestinationMarker(index); // 취소된 목적지 화살표 숨김
            attackControllers[index]?.EndManualMove(); // 자동 전투 상태 안전 복구
        }

        private void RefreshSelectionValidity() // 현재 선택 슬롯 유효성 갱신
        {
            if (!IsSlotIndexValid(selectedSlotIndex)) // 선택 슬롯 범위 확인
            {
                return; // 선택 없음 갱신 중단
            }

            if (!IsControllable(allySlots[selectedSlotIndex])) // 현재 선택 아군 전투 가능 여부 확인
            {
                ClearSelection(); // 전투 불가 선택 슬롯 및 윤곽 해제
            }
        }

        private void PruneStaleSlotsForRegistration() // 신규 전투 등록 전 이전 슬롯 정리
        {
            if (registry == null) // 전투 Registry 확인
            {
                return; // Registry 없음 정리 중단
            }

            for (int index = 0; index < allySlots.Length; index++) // 기존 파티 슬롯 순회
            {
                BattleActor actor = allySlots[index]; // 기존 슬롯 아군 조회

                if (actor == null || registry.Contains(actor)) // 빈 슬롯 또는 현재 전투 등록 여부 확인
                {
                    continue; // 유지 대상 슬롯 건너뜀
                }

                attackControllers[index]?.EndManualMove(); // 이전 자동 전투 잠금 해제
                HideDestinationMarker(index); // 이전 전투 목적지 화살표 숨김
                allySlots[index] = null; // 이전 전투 슬롯 액터 제거
                attackControllers[index] = null; // 이전 공격 컨트롤러 제거
                destinations[index] = Vector3.zero; // 이전 목적지 제거
                movingSlots[index] = false; // 이전 수동 이동 상태 제거

                if (selectedSlotIndex == index) // 제거 슬롯 현재 선택 여부 확인
                {
                    ClearSelection(); // 이전 선택 상태 및 윤곽 제거
                }
            }
        }


        private void ClearSelection() // 현재 캐릭터 선택 및 윤곽 해제
        {
            selectedSlotIndex = -1; // 현재 선택 슬롯 초기화
            BattleSelectionRuntimeState.Clear(); // 선택 Runtime 상태 및 연결 윤곽 해제
        }

        private static bool IsControllable(BattleActor actor) // 수동 조작 가능 아군 판정
        {
            return actor != null && actor.Team == BattleTeam.Ally && actor.IsCombatReady && actor.Stats.IsAlive && actor.gameObject.activeInHierarchy; // 생존 활성 아군 여부 반환
        }

        private static bool IsSlotIndexValid(int index) // 파티 슬롯 배열 범위 판정
        {
            return index >= 0 && index < MaxPartySlots; // 유효 슬롯 범위 반환
        }

        private void OnDestroy() // 수동 이동 컨트롤러 제거 처리
        {
            ClearSelection(); // 수동 이동 시스템 제거 시 선택 및 윤곽 해제

            for (int index = 0; index < attackControllers.Length; index++) // 등록 자동 공격 컨트롤러 순회
            {
                if (movingSlots[index]) // 현재 수동 이동 활성 여부 확인
                {
                    attackControllers[index]?.EndManualMove(); // 제거 전 자동 전투 잠금 해제
                }
            }

            if (destinationMarkerMaterial != null) // Runtime 목적지 화살표 Material 존재 확인
            {
                if (Application.isPlaying) // Play Mode 제거 방식 확인
                {
                    Destroy(destinationMarkerMaterial); // Play Mode 공용 화살표 Material 제거
                }
                else // EditMode 제거 방식 처리
                {
                    DestroyImmediate(destinationMarkerMaterial); // EditMode 공용 화살표 Material 즉시 제거
                }

                destinationMarkerMaterial = null; // 화살표 Material 참조 초기화
            }
        }
    }
}
