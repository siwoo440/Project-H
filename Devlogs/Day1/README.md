# Project H — Phase 0 Day 1 개발 일지

- 날짜: 2026-08-31
- 단계: Phase 0 / Day 1
- Unity: 6000.3.21f1
- 기준 커밋: `e8478480c38ef653798a9281de932ebc97510edc`

---

## 목표

Unity 2D 프로젝트의 개발 기반을 구성하고, 이후 시스템 개발에서 공통으로 사용할 Core 구조와 프로젝트 관리 환경을 준비한다.

---

## 완료 작업

### 프로젝트 기반

- Unity 6000.3.21f1 프로젝트 생성
- Universal 2D 기반 프로젝트 설정
- Unity 기본 `Assets`, `Packages`, `ProjectSettings` 등록
- `.gitignore` 구성
- `.gitattributes` 및 Git LFS 대상 확장자 구성
- `.editorconfig` 구성

### 프로젝트 폴더 구조

`Assets/ProjectH` 아래에 프로젝트 전용 구조를 구성했다.

- `Art`
- `Audio`
- `Data`
- `Prefabs`
- `Scenes`
- `Scripts`
- `UI`
- `Scripts/Core`
- `Scripts/Editor`

### Core 시스템

`GameManager`를 추가했다.

- Singleton 인스턴스 관리
- 중복 GameManager 제거
- 씬 전환 이후 유지
- 공통 초기화 상태 관리
- `SceneLoader` 참조 제공

`SceneLoader`를 추가했다.

- 이름 기반 비동기 씬 전환
- 현재 씬 비동기 재로드
- 잘못된 씬 이름 기본 검증

### Bootstrap

`Assets/ProjectH/Scenes/Bootstrap.unity`를 생성했다.

- `[ProjectH] Bootstrap` 루트 오브젝트 구성
- `GameManager` 연결
- `SceneLoader` 연결
- Bootstrap 씬을 Build Settings 첫 번째 씬으로 등록

### 개발 자동화

`Phase0Day1Setup` 에디터 도구를 추가했다.

메뉴:

`Tools > Project H > Phase 0 > 1일차 설정 실행`

실행 시 다음 작업을 자동 처리한다.

- ProjectH 기본 폴더 생성
- Bootstrap 씬 생성 또는 갱신
- GameManager / SceneLoader 구성
- Build Settings Bootstrap 등록

---

## 검토 결과

최신 커밋 기준으로 Day 1 목표를 막는 명확한 구조 문제는 확인되지 않았다.

- `GameManager`의 중복 인스턴스 방지 및 `DontDestroyOnLoad` 처리 확인
- `SceneLoader`의 기본 비동기 씬 로드 처리 확인
- Bootstrap 씬 등록 확인
- 프로젝트 Unity 버전 `6000.3.21f1` 확인
- Universal 2D 관련 Renderer/설정 에셋 확인
- Git LFS 대상 규칙 확인

GitHub Actions 또는 별도 CI 검사는 현재 등록되어 있지 않아 실제 Unity Editor 컴파일/Play Mode 성공 여부는 이 커밋만으로 자동 검증되지 않는다.

---

## Day 1 완료 기준

- Unity 프로젝트가 정상적으로 열릴 것
- Bootstrap 씬이 Build Settings 첫 번째 씬일 것
- Bootstrap에 GameManager와 SceneLoader가 존재할 것
- Play 시 GameManager가 한 번만 초기화될 것
- 프로젝트 기본 폴더와 Git 관리 규칙이 준비되어 있을 것

Phase 0 Day 1 개발 기반 구성 완료.
