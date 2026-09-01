# Project H — Phase 1 Day 7 개발 일지

- 날짜: 2026-09-01
- 단계: Phase 1 / Day 7
- 기준 커밋: `bd559c926866bb81267ff485f3d3715557b3b073`
- 커밋 당시 메시지: `7`
- 주제: 12인 캐릭터 데이터 기반 구축

---

## 목표

기존 Phase 1 계획의 초기 4인 캐릭터 데이터 구축 범위를 확장해, 프로젝트 H의 12인 전체 캐릭터 CharacterData 에셋을 먼저 생성하고 이후 전투 구현과 밸런스 과정에서 수치를 조정할 수 있는 기반을 만든다.

실제 플레이 시작 시 보유 캐릭터는 기존 4인을 유지하고, 전체 12명은 DataManager에서 조회 가능한 원본 데이터베이스로 준비한다.

---

## 포지션 구조 단순화

기존 전열 / 후열 구조를 제거하고 캐릭터의 전투 포지션을 다음 3종으로 단순화했다.

- `Tank`
- `Dealer`
- `Healer`

현재 배치:

### Tank

- 엘렌
- 티리아

### Dealer

- 릴리아
- 나타샤
- 이브
- 루시아
- 파이라
- 메르시아
- 노엘

### Healer

- 세레나
- 클레어
- 세피라

총 구성:

- Tank: 2
- Dealer: 7
- Healer: 3

`CharacterRole`은 기존 코드 호환을 위해 동일한 Tank / Dealer / Healer 값으로 유지한다.

---

## CharacterJob 확장

12명의 캐릭터를 데이터에서 구분할 수 있도록 CharacterJob을 확장했다.

현재 직군:

- Cleric
- Guardian
- Mage
- Ranger
- Knight
- Rogue
- Archer
- Alchemist
- Gunner
- Lancer
- Monk
- Explorer
- Pilgrim

기존 enum 값은 유지하면서 뒤쪽에 신규 직군을 추가했다.

---

## CharacterData 변경

CharacterData의 기본 전투 데이터 구조를 정리했다.

주요 필드:

- ID
- Display Name
- Character Job
- Battle Position
- Base HP
- Base Attack
- Base Defense
- Attack Speed
- Accuracy

새 필드:

- `accuracy`

기존 다음 필드는 이후 전투 공식과 밸런스 확장을 위해 호환용으로 유지했다.

- `baseMagic`
- `baseResistance`
- `criticalRate`

7일차에서는 위 3개 값을 확정 밸런스로 취급하지 않고 임시값으로 둔다.

---

## 12인 CharacterData

다음 12인의 CharacterData 에셋을 준비했다.

- `CH_SERENA`
- `CH_ELLEN`
- `CH_LILIA`
- `CH_NATASHA`
- `CH_EVE`
- `CH_CLAIRE`
- `CH_LUCIA`
- `CH_PYRA`
- `CH_TYRIA`
- `CH_MERCIA`
- `CH_NOEL`
- `CH_SEPHIRA`

기존 캐릭터:

- Serena
- Ellen
- Lilia
- Eve

의 기존 Unity `.meta` GUID는 유지했다.

신규 8인은 새로운 CharacterData 에셋과 `.meta`를 추가했다.

---

## 12인 초기 스탯

현재 캐릭터 기획표의 기본 수치를 CharacterData 초기값으로 입력했다.

| 캐릭터 | 포지션 | HP | 공격력 | 방어력 | 공격속도 | 명중률 |
|---|---|---:|---:|---:|---:|---:|
| 세레나 | Healer | 2200 | 180 | 120 | 0.90 | 0.98 |
| 엘렌 | Tank | 3200 | 230 | 200 | 0.85 | 0.96 |
| 릴리아 | Dealer | 1700 | 300 | 100 | 1.00 | 0.92 |
| 나타샤 | Dealer | 1600 | 310 | 90 | 1.30 | 0.95 |
| 이브 | Dealer | 1750 | 270 | 105 | 1.25 | 0.94 |
| 클레어 | Healer | 2400 | 210 | 130 | 0.95 | 0.96 |
| 루시아 | Dealer | 2000 | 285 | 120 | 1.15 | 0.97 |
| 파이라 | Dealer | 2800 | 320 | 150 | 1.05 | 0.93 |
| 티리아 | Tank | 3500 | 200 | 220 | 0.80 | 0.95 |
| 메르시아 | Dealer | 2100 | 250 | 130 | 1.10 | 0.94 |
| 노엘 | Dealer | 1850 | 240 | 110 | 1.15 | 0.93 |
| 세피라 | Healer | 2300 | 260 | 150 | 0.95 | 0.97 |

이 값들은 최종 밸런스가 아니라 이후 실제 전투 테스트를 위한 초기 기준값이다.

---

## ProjectHDataCatalog 확장

`ProjectHDataCatalog`의 Character 목록을 기존 4인에서 12인으로 확장했다.

현재 Catalog 구조:

- Characters: 12
- Monsters: 기존 3종 유지
- Dungeons: 기존 1종 유지
- Items: 기존 2종 유지

Monster / Dungeon / Item 참조는 기존 값을 유지한다.

---

## 초기 보유 캐릭터 유지

CharacterData 에셋이 12개 존재하는 것과 새 게임에서 플레이어가 실제로 보유하는 캐릭터는 분리했다.

새 게임 초기 보유 캐릭터는 기존과 동일하다.

- `CH_SERENA`
- `CH_ELLEN`
- `CH_LILIA`
- `CH_EVE`

따라서:

`전체 CharacterData 12명 ≠ 새 게임 초기 보유 12명`

구조를 유지한다.

나머지 캐릭터는 이후 합류 / 해금 시스템에서 SaveData에 추가하는 방식으로 확장한다.

---

## 7일차 Editor 도구

다음 메뉴를 추가했다.

`Tools > Project H > Phase 1 > 7일차 12인 캐릭터 데이터 재구성`

실행 기능:

- 12인 CharacterData 생성 또는 갱신
- 기본 스탯 입력
- Tank / Dealer / Healer 포지션 입력
- ProjectHDataCatalog Character 목록 재구성
- ID 중복 검사
- 기본 스탯 유효성 검사
- Accuracy 범위 검사

정상 데이터 기준으로 다음 구성을 확인한다.

- Tank 2
- Dealer 7
- Healer 3

---

## Phase 0 Day 2 호환 수정

7일차에서 BattlePosition과 CharacterData 구조가 변경되면서 기존 `Phase0Day2Setup`에 남아 있던 구형 참조로 컴파일 오류가 발생했다.

문제가 된 구형 구조:

- `BattlePosition.Front`
- `BattlePosition.Back`
- `CharacterRole.MagicDealer`
- `CharacterRole.RangedDealer`
- CharacterData의 `role` SerializedProperty

이를 현재 7일차 데이터 구조에 맞게 수정했다.

수정 후 Phase0Day2Setup은:

- Tank / Dealer / Healer 사용
- Accuracy 필드 사용
- 기존 7일차 CharacterData가 있으면 수치를 덮어쓰지 않음
- 현재 Characters 폴더의 전체 CharacterData를 읽어 Catalog에 등록
- 2일차 Setup 재실행 시 12인 Catalog가 4인으로 축소되지 않도록 처리

하도록 변경했다.

---

## EditMode 테스트

`Phase1Day7CharacterDataTests`를 추가했다.

검증 항목:

### BattlePosition

- BattlePosition 값이 정확히 3개인지 확인
- Tank 존재
- Dealer 존재
- Healer 존재

### Character Catalog

- ProjectHDataCatalog 존재
- Character 개수 12
- 12개 Character ID 고유성
- CharacterData null 여부
- HP > 0
- Attack >= 0
- Defense >= 0
- AttackSpeed > 0
- Accuracy 0~1
- CharacterJob이 None이 아닌지 확인

### Character Position

12명의 포지션이 현재 설계와 일치하는지 확인한다.

---

## 검토 결과

최신 커밋 기준 저장소 구조를 다시 확인했다.

확인 사항:

- BattlePosition은 Tank / Dealer / Healer 3종으로 구성
- CharacterData에 Accuracy 존재
- CharacterData의 Position과 이전 Role 호환 속성 존재
- ProjectHDataCatalog에 Character 참조 12개 등록
- Phase1Day7CharacterDataTests 존재
- Phase1Day7Setup에서 12인 데이터 재구성 지원
- 기존 Phase0Day2Setup의 Front / Back 및 세부 Dealer Role 참조 제거
- Phase0Day2Setup 재실행 시 전체 CharacterData를 Catalog에 다시 등록하도록 수정

저장소의 정적 구조 검토 기준으로 7일차 진행을 막는 추가적인 명확한 문제는 확인하지 못했다.

GitHub Commit Status에는 별도의 CI 상태 검사가 등록되어 있지 않다.

따라서 Unity Editor 실제 컴파일 성공, EditMode Test Runner 통과, DataManager 런타임 초기화 결과까지 GitHub 상태만으로 자동 검증된 것은 아니다.

---

## Day 7 완료 기준

- CharacterData 에셋 12개 존재
- Data Catalog에 12명 등록
- Tank / Dealer / Healer 3개 포지션만 사용
- 초기 4인 Save 구조 유지
- Accuracy 데이터 사용 가능
- CharacterJob 12인 대응
- 기존 2일차 Setup과 현재 CharacterData 구조의 컴파일 참조 충돌 제거
- 2일차 Setup 재실행 시 12인 Character Catalog 보존
- 12인 CharacterData EditMode 검증 코드 준비

Phase 1 Day 7 — 12인 캐릭터 데이터 기반 구축.
