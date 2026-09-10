# 프로젝트 H : 39일차 개발 로그

## 오늘 목표

39일차의 목표는 **던전별 아이템 드롭 테이블을 데이터로 정의하고, 전투 결과의 `ResultId`를 기준으로 실제 드롭을 결정한 뒤 기존 1회 보상 커밋 흐름에 일반 아이템과 장비 지급을 연결하는 것**이었다.

핵심 흐름은 다음과 같다.

```text
던전별 드롭 아이템 / 확률 / 수량 설정
↓
승리 시 ResultId를 시드로 드롭 계산
↓
BattleResultData에 실제 드롭 결과 저장
↓
CommitOnce에서 중복 보상 여부 확인
↓
일반 아이템 → 기존 가방
장비 → 고유 장비 인스턴스
↓
SaveCurrent()로 영구 저장
```

## GitHub 기준

- 개발 일지 추가 전 기준 커밋: `b52aa03d014094551ba89807d313a27427e542b0`
- 기준 커밋 메시지: `39`
- Day38 기준 커밋: `b096c6318071b861d88f602f2a6b634ce85c5ed7`
- 브랜치: `main`
- Day38 → Day39 변경: **19개 파일 변경 / 삭제 없음**
- `Devlogs/Day39/README.md`: 기준 커밋에는 아직 없음

> 이 README를 기존 Day39 커밋에 `--amend`하면 최종 커밋 SHA는 변경된다.

## 1. 던전 드롭 데이터 구조 추가

던전마다 드롭 아이템, 확률, 최소 수량, 최대 수량을 설정할 수 있도록 `DungeonDropEntry`를 추가하고 `DungeonData`에 `dropTable`을 연결했다.

### 추가 파일

- `Assets/ProjectH/Scripts/Data/DungeonDropEntry.cs`
- `Assets/ProjectH/Scripts/Battle/DungeonDropResult.cs`
- `Assets/ProjectH/Scripts/Battle/DungeonDropRollService.cs`
- `Assets/ProjectH/Scripts/Battle/DungeonDropGrantService.cs`
- `Assets/ProjectH/Tests/EditMode/DungeonDropTests.cs`

각 C# 파일의 Unity `.meta` 파일도 함께 추가했다.

### 수정 파일

- `Assets/ProjectH/Scripts/Data/DungeonData.cs`
- `Assets/ProjectH/Scripts/Data/DataManager.cs`
- `Assets/ProjectH/Scripts/Battle/BattleResultData.cs`
- `Assets/ProjectH/Scripts/Battle/BattleResultCommitService.cs`

## 2. ResultId 기반 결정적 드롭 계산

`DungeonDropRollService`는 전투 결과의 `ResultId`와 던전 ID를 기반으로 고정 시드를 만든다.

같은 던전에서 같은 `ResultId`를 사용하면 동일한 드롭 결과가 계산되도록 구성했다. 각 드롭 엔트리는 설정된 확률을 판정한 뒤 최소/최대 수량 범위에서 수량을 결정한다.

같은 ItemId가 여러 엔트리에서 당첨되면 최종 결과에서는 수량을 합산한다.

## 3. BattleResultData에 실제 드롭 결과 저장

전투 종료 시 `BattleResultData.Create()`에서 승리 결과에 한해 던전 데이터를 조회하고 실제 드롭 결과를 계산한다.

계산된 결과는 `BattleResultData.Drops`에 저장되므로, 보상 커밋 시점에는 이미 확정된 드롭 결과를 사용한다.

패배 결과에는 던전 아이템 드롭을 생성하지 않는다.

## 4. CommitOnce와 중복 보상 방지 연결

기존 `BattleResultCommitService.CommitOnce()`의 `ResultId` 기반 중복 처리 흐름 안에 드롭 지급을 포함했다.

```text
같은 ResultId
↓
첫 CommitOnce
→ Gold 지급
→ EXP 지급
→ 던전 클리어 처리
→ 아이템 / 장비 드롭 지급
→ ResultId 기록

↓ 다시 호출

같은 ResultId 확인
→ 전체 지급 차단
```

따라서 동일 전투 결과가 다시 처리되어도 Gold, EXP, 던전 클리어, 아이템 드롭이 중복 지급되지 않는다.

## 5. 일반 아이템과 장비 지급 분리

`DungeonDropGrantService`는 드롭된 ItemData의 타입에 따라 지급 방식을 나눈다.

일반 아이템은 기존 `ItemInventoryService`를 사용하여 Day38에서 구축한 가방과 `MaxStack` 정책을 그대로 따른다.

장비는 일반 아이템 스택에 넣지 않고 `SaveData.TryCreateEquipmentInstance()`를 통해 고유 장비 인스턴스로 생성한다.

전투 화면의 기존 흐름은 `CommitOnce()` 성공 직후 `SaveCurrent()`를 호출하므로, 드롭 지급도 같은 저장 흐름을 통해 영구 저장된다.

## 6. 드롭 데이터 검증 추가

`DataManager` 초기화 과정에서 던전 드롭 테이블을 검사하도록 추가했다.

검증 대상은 다음과 같다.

- 비어 있거나 잘못된 ItemId
- Item Registry에 존재하지 않는 ItemId
- 0 미만 또는 1 초과 드롭 확률
- 1 미만의 최소 수량
- 최대 수량이 최소 수량보다 작은 경우

기존 Equipment Registry와 기반 ItemData 연결 검증 구조는 그대로 유지했다.

## 7. 임시 던전 드롭 밸런스

현재 수치는 Day39 시스템 동작 확인용 임시값이다.

| 던전 | 임시 드롭 테이블 |
| --- | --- |
| `DG001` | 재료 1~2 확정, 소형 물약 35% |
| `DG002` | 재료 2~3 확정, 소형 물약 45% |
| `DG003` | 재료 3~4 확정, 소형 물약 55%, 철제 무기 8% |
| `DG004` | 재료 4~6 확정, 소형 물약 70% 1~2개, 가드 방어구 8% |
| `DG_LETICIA_FOREST` | 재료 1~2 확정, 소형 물약 35% |

사용한 주요 ItemId는 `IT_MATERIAL_001`, `IT_POTION_SMALL`, `EQ_WEAPON_IRON`, `EQ_ARMOR_GUARD`이며 최신 프로젝트 데이터에 실제 등록되어 있는 것을 확인했다.

## 8. EditMode 테스트 추가

`DungeonDropTests.cs`에는 드롭 계산의 핵심 규칙을 검증하는 테스트를 추가했다.

- 드롭 확률 0이면 미지급
- 확정 드롭의 최소/최대 수량 범위
- 동일 `ResultId`의 동일 결과
- 동일 ItemId 중복 당첨 시 수량 합산

현재 테스트는 드롭 계산 서비스 중심이다. 실제 아이템 지급과 `CommitOnce()`까지 포함한 통합 회귀 테스트는 후속 보강 대상이다.

## 9. 결과창 UI 범위

이번 Day39에서는 `BattleResultOverlay`를 수정하지 않았다.

따라서 전투 결과창은 기존 Gold / EXP 표시 구조를 유지하며, 드롭 아이템 이름과 수량을 별도로 나열하는 UI는 아직 연결하지 않았다.

실제 확인 기준은 다음 흐름이다.

```text
던전 승리
→ 보상 커밋
→ SaveCurrent()
→ 로비 / 가방 이동
→ 재료·물약 수량 증가 확인

장비 당첨
→ EquipmentInventory에 새 고유 장비 인스턴스 생성 확인
```

## 10. 검증 결과

GitHub의 최신 `main` 기준으로 Day38 커밋에서 Day39 커밋까지의 변경 내용을 다시 검토했다.

정적 검토 기준으로 Day39 핵심 흐름을 막는 오류는 확인되지 않았다.

확인된 사항:

- 최신 Day39 커밋은 Day38 기준 커밋 바로 다음 1개 커밋
- Day39 구현 파일과 Unity `.meta` 파일 포함
- 5개 던전의 드롭 테이블 변경 포함
- 일반 아이템 / 장비 지급 경로 분리 확인
- `ResultId` 중복 보상 차단 흐름 안에 드롭 지급 포함
- 성공 커밋 후 `SaveCurrent()` 호출 흐름 유지
- Day39 파일 삭제 없음
- `Devlogs/Day39/README.md`는 아직 원격 커밋에 없음

다만 GitHub Actions 상태 체크와 워크플로 실행 기록이 없어 **Unity 컴파일 및 EditMode Test Runner 통과 여부는 이 검토만으로 확인할 수 없다.**

## 11. Day39 완료 정리

Day39에서는 기존 Gold / EXP 중심의 던전 보상 흐름을 실제 아이템 보상까지 확장했다.

핵심적으로 던전 데이터 → 결정적 드롭 계산 → 전투 결과 저장 → 1회 커밋 → 일반 아이템/고유 장비 지급 → 세이브 저장의 흐름이 하나로 연결되었다.

Day38에서 만든 아이템 가방과 장비 인스턴스 구조를 새 시스템으로 교체하지 않고 그대로 활용했기 때문에, 기존 인벤토리 구조와도 연결성을 유지했다.

## 다음 작업

- `BattleResultOverlay`에 드롭 아이템 이름과 수량 표시
- `DungeonDropGrantService` 지급 회귀 테스트 보강
- `BattleResultCommitService` 중복 드롭 지급 방지 통합 테스트 보강
- Unity EditMode Test Runner에서 Day39 테스트 실행
- 실제 플레이 결과를 기준으로 임시 드롭 확률과 수량 조정
