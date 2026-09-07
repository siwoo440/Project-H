# Day 33 - 장비 인벤토리·고유 인스턴스 및 저장 영속화

## 개발 목표

Day32에서 구축한 장비 데이터/슬롯 구조를 실제 플레이 진행 데이터와 연결하기 위한 장비 인벤토리 저장 기반을 구축한다.

핵심 목표는 같은 장비 데이터를 여러 개 보유하더라도 각각을 별도의 장비 인스턴스로 식별하고, 획득·조회·수량 확인·제거 상태가 `SaveData`의 JSON 저장/불러오기 과정에서 유지되도록 만드는 것이다.

## 기준 커밋

- 확인 커밋: `dedeac1d5a8dd88cc28f376943ff6a103e2751e7`
- 기존 커밋 메시지: `33`
- 이전 기준: Day32 장비 데이터·슬롯 구조 및 카탈로그 검증 체계
- Day33 개발 코드가 이미 최신 커밋에 포함되어 있으므로 개발 일지는 해당 커밋에 amend하는 흐름을 기준으로 한다.

## 구현 내용

### 1. 장비 인스턴스 저장 데이터 추가

`EquipmentInstanceSaveData`를 추가하여 플레이어가 실제로 보유한 장비 한 개를 별도의 저장 객체로 표현한다.

저장 정보:

- `InstanceId`: 장비 한 개의 고유 인스턴스 ID
- `EquipmentId`: Day32에서 정의한 원본 장비 데이터 ID

동일한 `EquipmentId`를 가진 장비를 여러 개 획득해도 서로 다른 `InstanceId`를 사용하므로 이후 장착·해제·교체 시스템에서 개별 장비를 안정적으로 식별할 수 있다.

### 2. SaveData 장비 인벤토리 확장

`SaveData`에 `equipmentInventory` 목록을 추가하고 읽기 전용 접근자인 `EquipmentInventory`를 노출했다.

기존 저장 데이터에 장비 인벤토리 필드가 존재하지 않는 경우에도 `EnsureDefaults()`에서 빈 목록으로 복구하도록 구성했다.

이번 변경에서는 `SaveData.CurrentVersion`을 `1`로 유지했다.

### 3. 장비 획득 및 고유 ID 생성

`TryCreateEquipmentInstance()`를 추가하여 원본 장비 ID만 전달하면 신규 장비 인스턴스를 생성하고 인벤토리에 등록할 수 있도록 했다.

고유 인스턴스 ID는 `EQI_` 접두사와 GUID를 조합하여 생성한다.

예시:

```text
EQI_1A2B...
EQI_9F8E...
```

같은 `EQ_SWORD_001`을 두 개 획득해도 각 장비의 인스턴스 ID는 서로 다르게 유지된다.

### 4. 장비 인벤토리 기본 API 추가

다음 기능을 `SaveData`에 추가했다.

- `FindEquipmentInstance()` : 특정 인스턴스 ID 장비 조회
- `GetEquipmentCount()` : 원본 장비 ID 기준 보유 수량 조회
- `TryCreateEquipmentInstance()` : 자동 고유 ID 기반 신규 장비 획득
- `TryAddEquipmentInstance()` : 지정 인스턴스 ID 기반 장비 추가
- `TryRemoveEquipmentInstance()` : 특정 인스턴스 장비 제거

중복 `InstanceId`, 빈 인스턴스 ID, 빈 장비 ID는 등록 단계에서 거부하도록 처리했다.

### 5. 저장/불러오기 회귀 검증 코드 추가

`EquipmentInventorySaveTests`를 추가하여 장비 인벤토리 저장 구조의 핵심 시나리오를 검증하도록 구성했다.

포함된 테스트 시나리오:

- 새 게임 장비 인벤토리가 빈 상태로 생성되는지 확인
- 같은 원본 장비를 여러 번 획득해도 인스턴스 ID가 고유한지 확인
- 동일 인스턴스 ID 중복 등록이 거부되는지 확인
- 특정 인스턴스 조회 및 제거가 가능한지 확인
- `JsonUtility` 직렬화/역직렬화 후 장비 목록과 수량이 유지되는지 확인
- 구 저장 데이터처럼 장비 인벤토리가 null인 경우 `EnsureDefaults()`가 빈 목록으로 복구하는지 확인

## 변경 파일

### 생성

- `Assets/ProjectH/Scripts/Save/EquipmentInstanceSaveData.cs`
- `Assets/ProjectH/Scripts/Save/EquipmentInstanceSaveData.cs.meta`
- `Assets/ProjectH/Tests/EditMode/EquipmentInventorySaveTests.cs`
- `Assets/ProjectH/Tests/EditMode/EquipmentInventorySaveTests.cs.meta`

### 수정

- `Assets/ProjectH/Scripts/Save/SaveData.cs`

### 삭제

- 없음

## 구조 요약

```text
EquipmentData
    ↓ 원본 데이터 ID
EquipmentInstanceSaveData
    ├─ InstanceId
    └─ EquipmentId
        ↓
SaveData.EquipmentInventory
        ↓
JSON 저장 / 불러오기
```

이 구조를 통해 Day34에서 캐릭터의 장비 슬롯에 `InstanceId`를 연결하여 특정 장비 한 개를 장착·해제·교체하는 흐름으로 확장할 수 있다.

## 검증 상태

최신 GitHub 커밋의 변경 파일과 구현 내용을 확인했으며, Day33 목표와 충돌하는 명확한 소스 구조 문제는 확인되지 않았다.

GitHub 커밋에는 CI 상태 체크 및 GitHub Actions 실행 기록이 없으므로 Unity 컴파일 성공이나 EditMode 테스트 통과를 이 개발 일지에서 확정하지 않는다.

실제 Unity Editor에서 프로젝트를 열었을 때 발생하는 컴파일 오류나 테스트 오류는 이후 회귀 수정 대상으로 처리한다.

## 다음 개발 연결

Day34에서는 이번에 추가한 장비 `InstanceId`를 캐릭터별 장비 슬롯 저장 데이터와 연결하고 장착·해제·교체 및 저장 흐름을 구현하는 것이 핵심이다.
