using System.Collections.Generic; // 목록 자료형
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 던전 클리어 기록 기능
using ProjectH.Data; // 몬스터·속성 데이터 기능
using ProjectH.Dialogue; // 대사 파일·개인 이벤트 완료 기능
using ProjectH.Diary; // 일기장 기능
using ProjectH.SaveSystem; // 저장 기능
using UnityEditor; // 에셋 로드 기능
using UnityEngine; // Unity 오브젝트·JSON 기능
using UnityEngine.UI; // 크게 보기 창 버튼·글자 기능 (Day63)

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class DiaryTests // Day63 일기장 : 본 이야기 · 만난 몬스터 · 약점 공개 · 목록 · 세계관 테스트
    {
        private GameObject dataObject; // 데이터 관리자 오브젝트
        private DataManager dataManager; // 실제 카탈로그 데이터 관리자

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 실제 카탈로그 준비
        {
            DialogueLibrary.ClearCache(); // 대화 캐시 비우기
            dataObject = new GameObject("DiaryTests"); // 오브젝트 생성
            dataManager = dataObject.AddComponent<DataManager>(); // 컴포넌트 추가
            dataManager.Configure(AssetDatabase.LoadAssetAtPath<ProjectHDataCatalog>("Assets/ProjectH/Data/Database/ProjectHDataCatalog.asset")); // 실제 카탈로그
            dataManager.Initialize(); // 초기화
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 정리
        {
            Object.DestroyImmediate(dataObject); // 오브젝트 제거
        }

        private static SaveData CreateSave() => SaveData.CreateNewGame(DiaryCatalog.Starters); // 초기 4인 새 게임

        [Test] // 끝까지 본 대화 기록 · 중복 무시 · 저장 왕복 · 이전 세이브 호환
        public void SeenDialogues_AreRecorded_AndSurviveSave() // 기록 테스트
        {
            SaveData saveData = CreateSave(); // 새 게임

            Assert.That(DiaryService.IsDialogueSeen(saveData, "VILLAGE_PLAZA_SERENA"), Is.False); // 처음엔 안 봄
            Assert.That(DiaryService.MarkDialogueSeen(saveData, "VILLAGE_PLAZA_SERENA"), Is.True); // 새로 기록
            Assert.That(DiaryService.MarkDialogueSeen(saveData, "VILLAGE_PLAZA_SERENA"), Is.False); // 중복은 무시
            SaveData loaded = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(saveData)); // 저장·불러오기
            loaded.EnsureDefaults(); // 보정
            Assert.That(DiaryService.IsDialogueSeen(loaded, "VILLAGE_PLAZA_SERENA"), Is.True); // 기록 유지
            SaveData old = JsonUtility.FromJson<SaveData>("{}"); // Day63 이전 세이브
            old.EnsureDefaults(); // 보정
            Assert.That(old.SeenDialogueIds, Is.Not.Null.And.Empty); // 빈 목록으로 시작
            Assert.That(old.SeenMonsterIds, Is.Not.Null.And.Empty); // 빈 목록으로 시작
        }

        [Test] // 이전 세이브의 완료 개인 이벤트 · 올린 결속 단계는 본 것으로 인정
        public void LegacyProgress_CountsAsSeen() // 자동 인정 테스트
        {
            SaveData saveData = CreateSave(); // 새 게임
            CharacterEventDefinition first = CharacterEventCatalog.All[0]; // 첫 개인 이벤트
            saveData.SetStoryFlag(DialogueService.BuildEventDoneFlag(first.Id)); // 58일차 방식 완료 기록
            saveData.FindCharacter("CH_SERENA").SetBondLevel(2); // 결속 2단계

            Assert.That(DiaryService.IsDialogueSeen(saveData, first.ScriptId), Is.True); // 개인 이벤트 인정
            Assert.That(DiaryService.IsDialogueSeen(saveData, BondCatalog.GetBondScriptId("CH_SERENA", 1)), Is.True); // 결속 1단계 대사 인정
            Assert.That(DiaryService.IsDialogueSeen(saveData, BondCatalog.GetBondScriptId("CH_SERENA", 2)), Is.True); // 결속 2단계 대사 인정
            Assert.That(DiaryService.IsDialogueSeen(saveData, BondCatalog.GetBondScriptId("CH_SERENA", 3)), Is.False); // 3단계는 아직
        }

        [Test] // 시나리오 44편 · CG 12장 모두 실제 대사 파일과 연결
        public void Catalog_ScenariosAndCgs_PointToRealScripts() // 목록 테스트
        {
            HashSet<string> ids = new HashSet<string>(); // 중복 검사

            foreach (DiaryScenarioEntry entry in DiaryCatalog.Scenarios) // 시나리오 순회
            {
                Assert.That(ids.Add(entry.ScriptId), Is.True, entry.ScriptId); // 중복 없음
                Assert.That(DialogueLibrary.Load(entry.ScriptId), Is.Not.Null, entry.ScriptId); // 대사 파일 존재
            }

            Assert.That(DiaryCatalog.Scenarios.Count, Is.EqualTo(CharacterEventCatalog.All.Count + 2 + 8 + 20 + 16 + 4)); // 개인 + 지역 2 (Day65) + 합류 8 (Day64) + 결속 20 + 마을 16 + 특별한 밤 4
            Assert.That(DiaryCatalog.AllCharacters.Count, Is.EqualTo(12)); // 궁극기 컷신 12인 (Day64)
            Assert.That(DiaryCatalog.Cgs.Count, Is.EqualTo(12)); // 캐릭터당 3장

            foreach (DiaryCgEntry cg in DiaryCatalog.Cgs) // CG 순회
            {
                Assert.That(ids.Contains(cg.ScriptId), Is.True, cg.Id); // 시나리오에 있는 이야기로 해금
            }
        }

        [Test] // 만난 몬스터 기록 · 약점은 등장 던전 클리어 후 공개
        public void Monsters_RecordedOnce_WeaknessAfterClear() // 몬스터 테스트
        {
            SaveData saveData = CreateSave(); // 새 게임

            Assert.That(DiaryService.MarkMonstersSeen(saveData, new[] { "MON_CORRUPTED_WOLF", "MON_CORRUPTED_WOLF", "MON_POLLUTED_PLANT" }), Is.EqualTo(2)); // 중복 제외 2마리
            Assert.That(DiaryService.MarkMonstersSeen(saveData, new[] { "MON_CORRUPTED_WOLF" }), Is.EqualTo(0)); // 이미 기록
            Assert.That(saveData.HasSeenMonster("MON_POLLUTED_PLANT"), Is.True); // 기록 확인
            Assert.That(DiaryService.GetAppearanceDungeonIds(dataManager, "MON_CORRUPTED_WOLF"), Has.Member("DG001")); // 숲에 등장
            Assert.That(DiaryService.IsWeaknessKnown(saveData, dataManager, "MON_CORRUPTED_WOLF"), Is.False); // 아직 비공개
            DungeonProgressSaveAdapter.MarkCleared(saveData, "DG001"); // 숲 클리어
            Assert.That(DiaryService.IsWeaknessKnown(saveData, dataManager, "MON_CORRUPTED_WOLF"), Is.True); // 공개
        }

        [Test] // 약점은 전투 상성표 그대로 · 몬스터 설명 존재
        public void Weaknesses_FollowBattleChart_AndMonstersHaveLore() // 상성 테스트
        {
            Assert.That(DiaryService.GetWeaknesses(ElementType.Grass), Has.Member(ElementType.Fire)); // 풀은 불에 약함
            Assert.That(DiaryService.GetWeaknesses(ElementType.Grass), Has.No.Member(ElementType.Water)); // 물은 약점 아님
            Assert.That(DiaryService.GetElementLabel(ElementType.Dark), Is.EqualTo("어둠")); // 한글 이름

            foreach (string id in new[] { "MON_CORRUPTED_WOLF", "MON_POLLUTED_PLANT", "MON_CORRUPTED_SOLDIER", "MON_BOSS_LETICIA" }) // 몬스터 순회
            {
                Assert.That(dataManager.GetMonster(id).Description, Is.Not.Empty, id); // 설명 존재
            }
        }

        [Test] // CG·컷신 : 가운데 질문만 (그림·안내·닫기 없음) · [아니요]는 바로 닫힘 · [네]는 재생
        public void Viewer_AskPlay_ShowsOnlyCenteredQuestion() // 재생 질문 테스트
        {
            GameObject canvasObject = new GameObject("ViewerTestCanvas", typeof(RectTransform), typeof(Canvas)); // 테스트 캔버스
            ProjectH.UI.DiaryImageViewer viewer = ProjectH.UI.DiaryImageViewer.Create(canvasObject.transform); // 창 생성
            Transform root = viewer.transform; // 창
            int played = 0; // 재생 횟수

            viewer.AskPlay("CG를 재생하겠습니까?", () => played++); // CG 누름
            Assert.That(viewer.IsOpen, Is.True); // 열림
            Assert.That(viewer.IsImageMode, Is.False); // 질문 모드
            Assert.That(root.Find("PlayPrompt").gameObject.activeSelf, Is.True); // 가운데 질문
            Assert.That(root.Find("PlayPrompt").GetComponentInChildren<Text>().text, Is.EqualTo("CG를 재생하겠습니까?")); // 질문 문구
            Assert.That(root.Find("Frame").gameObject.activeSelf, Is.False); // 그림 없음
            Assert.That(root.Find("Close").gameObject.activeSelf, Is.False); // 닫기 버튼 없음
            Assert.That(root.Find("ZoomText").gameObject.activeSelf, Is.False); // 왼쪽 위 글자 없음
            root.Find("PlayPrompt/No").GetComponent<Button>().onClick.Invoke(); // [아니요]
            Assert.That(viewer.IsOpen, Is.False); // 바로 닫힘
            Assert.That(played, Is.EqualTo(0)); // 재생 안 함
            viewer.AskPlay("CG를 재생하겠습니까?", () => played++); // 다시 누름
            root.Find("PlayPrompt/Yes").GetComponent<Button>().onClick.Invoke(); // [네]
            Assert.That(played, Is.EqualTo(1)); // 재생
            Assert.That(viewer.IsOpen, Is.False); // 창 닫힘
            Object.DestroyImmediate(canvasObject); // 정리
        }

        [Test] // 정지 컷신 : 전체 화면 그림 + 왼쪽 위 안내 + 닫기 버튼 · 1배로 시작 · 질문 없음
        public void Viewer_OpenImage_ShowsImageHintAndClose() // 크게 보기 테스트
        {
            GameObject canvasObject = new GameObject("ViewerTestCanvas", typeof(RectTransform), typeof(Canvas)); // 테스트 캔버스
            ProjectH.UI.DiaryImageViewer viewer = ProjectH.UI.DiaryImageViewer.Create(canvasObject.transform); // 창 생성
            Transform root = viewer.transform; // 창
            Sprite still = ProjectH.UI.RuntimeSpriteLoader.Load("Diary/Stills/STILL_TEST"); // 테스트 정지 컷신

            Assert.That(still, Is.Not.Null); // 그림 존재
            viewer.OpenImage(still, null, Color.white); // 정지 컷신 누름
            Assert.That(viewer.IsImageMode, Is.True); // 그림 모드
            Assert.That(viewer.Zoom, Is.EqualTo(ProjectH.UI.DiaryImageViewer.MinZoom)); // 화면에 맞춘 1배
            Assert.That(root.Find("Frame").gameObject.activeSelf, Is.True); // 그림 표시
            Assert.That(root.Find("ZoomText").gameObject.activeSelf, Is.True); // 왼쪽 위 안내
            Assert.That(root.Find("Close").gameObject.activeSelf, Is.True); // 닫기 버튼
            Assert.That(root.Find("PlayPrompt").gameObject.activeSelf, Is.False); // 질문 없음
            root.Find("Close").GetComponent<Button>().onClick.Invoke(); // 닫기
            Assert.That(viewer.IsOpen, Is.False); // 닫힘
            Object.DestroyImmediate(canvasObject); // 정리
        }

        [Test] // 테스트 CG : 기본 Texture PNG도 스프라이트로 불러옴 · 테스트 장면에 CG 켜기/끄기 · 사진/동영상 아이콘
        public void TestCg_LoadsAsSprite_SceneTogglesCg_BadgesExist() // 테스트 CG 테스트
        {
            Sprite cg = ProjectH.UI.RuntimeSpriteLoader.Load("Diary/CG/CG_TEST"); // 테스트 CG (Texture로 가져온 PNG)
            Assert.That(cg, Is.Not.Null); // 불러옴
            Assert.That(cg.rect.width, Is.EqualTo(1280f)); // 원본 크기 그대로
            Assert.That(ProjectH.UI.RuntimeSpriteLoader.Load("Diary/CG/NO_SUCH_CG"), Is.Null); // 없는 그림은 null

            DialogueScript script = DialogueLibrary.Load("DIARY_TEST_CG"); // 테스트 장면
            List<string> errors = new List<string>(); // 검사 오류
            Assert.That(script, Is.Not.Null); // 존재
            Assert.That(DialogueLibrary.Validate(script, errors), Is.True, string.Join("\n", errors)); // 구조 검사
            List<string> cgs = new List<string>(); // 대사별 CG 지정
            foreach (DialogueNode node in script.Nodes) if (!string.IsNullOrEmpty(node.Cg)) cgs.Add(node.Cg); // CG 지정 수집
            Assert.That(cgs, Is.EqualTo(new[] { "CG_TEST", "-" })); // CG 켜기 → 끄기

            Assert.That(ProjectH.UI.DiaryBadgeArt.Photo, Is.Not.Null); // 사진 아이콘
            Assert.That(ProjectH.UI.DiaryBadgeArt.Video, Is.Not.Null); // 동영상 아이콘
            Assert.That(ProjectH.UI.DiaryBadgeArt.Photo, Is.Not.SameAs(ProjectH.UI.DiaryBadgeArt.Video)); // 서로 다른 아이콘
        }

        [Test] // 세계관 단어 : 20개 이상 · 중복 없음 · 설명 있음 · 플래그 단어는 스토리 진행 후 열림
        public void Glossary_HasUniqueTerms_AndFlagLockedEntries() // 세계관 테스트
        {
            SaveData saveData = CreateSave(); // 새 게임
            HashSet<string> terms = new HashSet<string>(); // 중복 검사
            GlossaryEntry locked = null; // 잠긴 단어

            foreach (GlossaryEntry entry in WorldGlossaryCatalog.All) // 단어 순회
            {
                Assert.That(terms.Add(entry.Term), Is.True, entry.Term); // 중복 없음
                Assert.That(entry.Description, Is.Not.Empty, entry.Term); // 설명 있음
                if (!string.IsNullOrEmpty(entry.UnlockFlag)) locked = entry; // 잠긴 단어
            }

            Assert.That(terms.Count, Is.GreaterThanOrEqualTo(20)); // 20개 이상
            Assert.That(locked, Is.Not.Null); // 잠긴 단어 존재
            Assert.That(locked.IsUnlocked(saveData), Is.False); // 처음엔 잠김
            saveData.SetStoryFlag(locked.UnlockFlag); // 스토리 진행
            Assert.That(locked.IsUnlocked(saveData), Is.True); // 열림
        }
    }
}
