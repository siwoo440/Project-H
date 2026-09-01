# Project H — Phase 0 Day 3 개발 일지

- 날짜: 2026-09-01
- 단계: Phase 0 / Day 3
- 기준 커밋: `76ad30f84e61f4a784519d41785482d31761f861`
- 커밋 당시 메시지: `3`
- Unity: 6000.3.21f1

---

## 목표

플레이어 진행 상태를 로컬 파일로 저장하고 다시 불러올 수 있는 1슬롯 Save 시스템의 기반을 구축한다.

정적 게임 데이터는 기존 `DataManager`가 담당하고, 플레이 도중 변경되는 진행 데이터는 `SaveManager`와 `SaveData`가 별도로 관리하도록 역할을 분리한다.

---

## 완료 작업

### SaveData 구조

`SaveData`와 `CharacterSaveData`를 추가했다.

현재 저장 항목:

- `saveVersion`
- `currentDay`
- `currentTime`
- `currentChapter`
- `currentMainQuest`
- `partyCharacterIds`
- 캐릭터별 `characterId`
- 캐릭터별 `level`
- 캐릭터별 `experience`

현재 저장 버전은 `1`이다.

### 새 게임 초기화

새 게임 생성 시 다음 캐릭터를 레벨 1, 경험치 0으로 생성하고 초기 파티에 등록한다.

- `CH_SERENA`
- `CH_ELLEN`
- `CH_LILIA`
- `CH_EVE`

초기 일차는 1일차, 시간대는 Morning이다.

### SaveManager

`SaveManager`를 추가했다.

구현 기능:

- `Initialize()`
- `CreateNewGame()`
- `SaveCurrent()`
- `LoadCurrent()`
- `DeleteSave()`
- `HasSaveData`
- `CurrentSave`
- `SavePath`

저장 파일은 `Application.persistentDataPath/save_001.json`의 로컬 1슬롯 JSON 방식이다.

### 저장 검증

불러오기 과정에 다음 기본 검증을 추가했다.

- 저장 데이터 null 검사
- `saveVersion` 검사
- 캐릭터 저장 ID 누락 검사
- 캐릭터 저장 ID 중복 검사
- DataManager에 존재하지 않는 캐릭터 ID 검사

새 게임 생성 시 초기 캐릭터 ID가 DataManager에 실제 존재하는지도 확인한다.

### GameManager 연동

기존 `GameManager`에 `SaveManager` 의존성과 `GameManager.Save` 접근점을 추가했다.

초기화 순서:

1. DataManager 초기화
2. DataManager 성공 확인
3. SaveManager 초기화
4. SaveManager 성공 확인
5. GameManager 초기화 완료

### Bootstrap 연동

`Bootstrap.unity`의 `[ProjectH] Bootstrap` 오브젝트에 `SaveManager`를 추가했다.

현재 핵심 구성:

- SceneLoader
- DataManager
- SaveManager
- GameManager

### Editor Debug 도구

`Phase0Day3Setup`에 3일차 설정 메뉴와 Play Mode 저장 테스트 메뉴를 추가했다.

- 새 게임 생성
- 샘플 진행도 적용
- 현재 저장
- 불러오기
- 현재 상태 출력
- 저장 삭제

샘플 진행도는 Day 7, 세레나 Lv.5 / Exp 350 상태를 사용한다.

### EditMode 테스트

`SaveDataTests`를 추가했다.

- 새 게임 기본값 생성
- 초기 캐릭터 및 파티 생성
- JSON 직렬화/역직렬화 왕복
- Day / Level / Exp 복원
- 존재하지 않는 캐릭터 ID 조회

---

## 검토 결과

최신 커밋 기준 정적 검토에서 Phase 0 Day 3 진행을 막는 명확한 구조 문제는 확인되지 않았다.

확인 항목:

- Bootstrap의 SaveManager 컴포넌트 연결
- GameManager의 SaveManager 의존성 및 초기화 순서
- 로컬 `save_001.json` 저장/불러오기 구조
- 2일차 Character ID와 새 게임 저장 데이터 연결
- Save Version 1 검증
- 캐릭터 저장 ID 중복 및 정적 데이터 존재 검증
- JSON 왕복용 EditMode 테스트 코드

GitHub에는 이 커밋에 대한 CI 상태 검사가 등록되어 있지 않다. 따라서 Unity Editor 실제 컴파일과 EditMode Test Runner 성공 여부는 저장소 정적 검토만으로 확인할 수 없다.

---

## Day 3 완료 기준

- Bootstrap에서 DataManager와 SaveManager가 초기화될 것
- 새 게임 생성 시 초기 4캐릭터 진행 데이터가 만들어질 것
- `save_001.json`이 생성될 것
- 진행 상태 변경 후 저장할 수 있을 것
- 재실행 후 저장 파일을 불러올 수 있을 것
- Day / Level / Exp가 복원될 것
- `HasSaveData`로 저장 존재 여부를 확인할 수 있을 것
- 저장 파일을 삭제할 수 있을 것

Phase 0 Day 3 로컬 Save 시스템 기반 구축.
