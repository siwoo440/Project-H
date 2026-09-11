using System.Collections.Generic; // 목록 자료형
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Dialogue; // 대화 파일·진행 기능
using ProjectH.SaveSystem; // 저장·시간·활력·결속 기능
using ProjectH.Village; // 마을 구역·배치·행동 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class VillageTests // Day62 마을 : 무작위 배치 · 구역 행동 · 시간/일차 소비 테스트
    {
        private static readonly string[] Starters = { "CH_SERENA", "CH_ELLEN", "CH_LILIA", "CH_EVE" }; // 초기 4인
        private static readonly SaveTimeOfDay[] Phases = { SaveTimeOfDay.Morning, SaveTimeOfDay.Day, SaveTimeOfDay.Evening, SaveTimeOfDay.Night }; // 4시간대

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 캐시 비우기
        {
            DialogueLibrary.ClearCache(); // 대화 캐시 초기화
        }

        private static SaveData CreateSave() => SaveData.CreateNewGame(Starters); // 초기 4인 새 게임

        private static void MoveTo(SaveData saveData, string characterId, VillageZone zone, params SaveTimeOfDay[] phases) // 캐릭터가 구역에 있는 일차·시간대로 이동
        {
            for (int day = 1; day <= 400; day++) // 일차 탐색
            {
                foreach (SaveTimeOfDay phase in phases) // 허용 시간대
                {
                    if (VillagePresenceService.PickZone(characterId, day, phase) != zone) continue; // 다른 곳
                    saveData.SetCurrentDay(day); // 일차 이동
                    saveData.SetCurrentTime(phase); // 시간대 이동
                    return; // 찾음
                }
            }

            Assert.Fail($"{characterId}가 {zone}에 있는 시간을 찾지 못했습니다."); // 가중치 오류
        }

        private static DialogueRunner PlayToEnd(string scriptId) // 대화를 첫 선택지로 끝까지 진행
        {
            DialogueScript script = DialogueLibrary.Load(scriptId); // 대화 파일
            Assert.That(script, Is.Not.Null, scriptId); // 파일 존재
            DialogueRunner runner = new DialogueRunner(script); // 진행기

            for (int guard = 0; guard < 100 && !runner.IsFinished; guard++) // 끝까지
            {
                if (runner.IsWaitingForChoice) runner.Choose(0); // 첫 선택지
                else runner.Advance(); // 다음 대사
            }

            Assert.That(runner.IsFinished, Is.True, scriptId); // 종료 검증
            return runner; // 진행기 반환
        }

        [Test] // 같은 일차·시간대면 같은 배치, 보유하지 않은 캐릭터는 없음
        public void Presence_IsDeterministic_AndOnlyOwnedCharacters() // 재현성 테스트
        {
            SaveData saveData = CreateSave(); // 새 게임
            saveData.SetCurrentDay(7); // 7일차
            saveData.SetCurrentTime(SaveTimeOfDay.Evening); // 저녁

            Assert.That(VillagePresenceService.GetZone(saveData, "CH_SERENA"), Is.EqualTo(VillagePresenceService.PickZone("CH_SERENA", 7, SaveTimeOfDay.Evening))); // 저장 기준 = 일차·시간대 기준
            Assert.That(VillagePresenceService.PickZone("CH_SERENA", 7, SaveTimeOfDay.Evening), Is.EqualTo(VillagePresenceService.PickZone("CH_SERENA", 7, SaveTimeOfDay.Evening))); // 재현성
            Assert.That(VillagePresenceService.GetZone(saveData, "CH_NATASHA"), Is.Null); // 합류 전 캐릭터는 마을에 없음
        }

        [Test] // 한 캐릭터는 한 곳에만 · 시장은 밤에 비어 있음
        public void Presence_OneZonePerCharacter_AndMarketClosedAtNight() // 배치 규칙 테스트
        {
            SaveData saveData = CreateSave(); // 새 게임

            for (int day = 1; day <= 30; day++) // 30일
            {
                foreach (SaveTimeOfDay phase in Phases) // 4시간대
                {
                    saveData.SetCurrentDay(day); // 일차
                    saveData.SetCurrentTime(phase); // 시간대
                    HashSet<string> seen = new HashSet<string>(); // 이미 나온 캐릭터

                    foreach (VillageZoneInfo info in VillageZoneCatalog.All) // 구역 순회
                    {
                        foreach (string characterId in VillagePresenceService.GetCharactersIn(saveData, info.Zone)) // 구역의 캐릭터
                        {
                            Assert.That(seen.Add(characterId), Is.True, $"{characterId} {day}일 {phase} 중복"); // 한 곳에만
                        }
                    }

                    if (phase == SaveTimeOfDay.Night) Assert.That(VillagePresenceService.GetCharactersIn(saveData, VillageZone.Market), Is.Empty); // 밤 시장 비어 있음
                }
            }
        }

        [Test] // 선호 구역에 자주 나타나고, 여러 구역을 돌아다님
        public void Presence_FollowsPreferences_WithVariety() // 분포 테스트
        {
            Dictionary<VillageZone, int> ellen = new Dictionary<VillageZone, int>(); // 엘렌 구역별 횟수
            HashSet<VillageZone> serenaZones = new HashSet<VillageZone>(); // 세레나가 나타난 구역

            for (int day = 1; day <= 400; day++) // 400일
            {
                VillageZone? zone = VillagePresenceService.PickZone("CH_ELLEN", day, SaveTimeOfDay.Day); // 엘렌 낮 위치
                if (zone.HasValue) ellen[zone.Value] = ellen.TryGetValue(zone.Value, out int count) ? count + 1 : 1; // 횟수 누적
                VillageZone? serena = VillagePresenceService.PickZone("CH_SERENA", day, SaveTimeOfDay.Morning); // 세레나 아침 위치
                if (serena.HasValue) serenaZones.Add(serena.Value); // 구역 기록
            }

            VillageZone favorite = VillageZone.Plaza; // 가장 많은 구역

            foreach (KeyValuePair<VillageZone, int> pair in ellen) // 횟수 비교
            {
                if (!ellen.ContainsKey(favorite) || pair.Value > ellen[favorite]) favorite = pair.Key; // 최다 갱신
            }

            Assert.That(favorite, Is.EqualTo(VillageZone.Guild)); // 엘렌은 길드 선호
            Assert.That(serenaZones.Count, Is.GreaterThanOrEqualTo(4)); // 여러 곳을 돌아다님
            Assert.That(VillagePresenceService.GetWeights("CH_SEPHIRA").Length, Is.EqualTo(5)); // 64일차 합류 캐릭터도 등록
        }

        [Test] // 온천 : 활력 +30 · 시간 1칸, 밤이면 다음 날 아침
        public void Bathe_RestoresVitality_AndSpendsTime() // 온천 테스트
        {
            SaveData saveData = CreateSave(); // 새 게임
            saveData.SetCurrentDay(3); // 3일차
            saveData.SetCurrentTime(SaveTimeOfDay.Day); // 낮
            saveData.SetCurrentVitality(50); // 활력 50

            Assert.That(VillageActionService.TryBathe(saveData, out string message), Is.True, message); // 목욕
            Assert.That(saveData.CurrentVitality, Is.EqualTo(80)); // +30
            Assert.That(saveData.CurrentTime, Is.EqualTo(SaveTimeOfDay.Evening)); // 낮 → 저녁
            saveData.SetCurrentTime(SaveTimeOfDay.Night); // 밤
            VillageActionService.TryBathe(saveData, out _); // 밤 목욕
            Assert.That(saveData.CurrentDay, Is.EqualTo(4)); // 다음 날
            Assert.That(saveData.CurrentVitality, Is.EqualTo(SaveData.MaxVitality)); // 일차 변경 전체 회복
        }

        [Test] // 여관 잠자기 : 저녁·밤만 · 다음 날 아침 · 활력 전체 회복
        public void Sleep_OnlyEveningOrNight_AdvancesToNextMorning() // 잠자기 테스트
        {
            SaveData saveData = CreateSave(); // 새 게임
            saveData.SetCurrentDay(5); // 5일차
            saveData.SetCurrentTime(SaveTimeOfDay.Morning); // 아침
            saveData.SetCurrentVitality(10); // 활력 10

            Assert.That(VillageActionService.TrySleep(saveData, out string early), Is.False); // 아침에는 불가
            Assert.That(early, Does.Contain("저녁")); // 안내
            Assert.That(saveData.CurrentDay, Is.EqualTo(5)); // 그대로
            saveData.SetCurrentTime(SaveTimeOfDay.Evening); // 저녁
            Assert.That(VillageActionService.TrySleep(saveData, out string message), Is.True, message); // 잠자기
            Assert.That(saveData.CurrentDay, Is.EqualTo(6)); // 다음 날
            Assert.That(saveData.CurrentTime, Is.EqualTo(SaveTimeOfDay.Morning)); // 아침
            Assert.That(saveData.CurrentVitality, Is.EqualTo(SaveData.MaxVitality)); // 전체 회복
        }

        [Test] // 구역 이벤트 : 끝까지 보면 호감도 + 시간 1칸 · 하루 1회
        public void ZoneEvent_GivesAffinity_SpendsTime_OncePerDay() // 구역 이벤트 테스트
        {
            SaveData saveData = CreateSave(); // 새 게임
            MoveTo(saveData, "CH_SERENA", VillageZone.Plaza, SaveTimeOfDay.Morning, SaveTimeOfDay.Day, SaveTimeOfDay.Evening); // 광장에 있는 시간 (밤 제외)
            int day = saveData.CurrentDay; // 이벤트 일차
            SaveTimeOfDay phase = saveData.CurrentTime; // 이벤트 시간대
            string scriptId = VillageActionService.GetZoneEventScriptId("CH_SERENA", VillageZone.Plaza); // VILLAGE_PLAZA_SERENA

            Assert.That(VillageActionService.CanDoZoneEvent(saveData, "CH_SERENA", VillageZone.Plaza, out string reason), Is.True, reason); // 가능
            Assert.That(VillageActionService.CanDoZoneEvent(saveData, "CH_SERENA", VillageZone.Guild, out _), Is.False); // 없는 구역은 불가
            Assert.That(VillageActionService.CompleteZoneEvent(saveData, "CH_SERENA", VillageZone.Plaza, new DialogueRunner(DialogueLibrary.Load(scriptId))), Does.Contain("끝까지")); // 중간 종료는 미반영
            string message = VillageActionService.CompleteZoneEvent(saveData, "CH_SERENA", VillageZone.Plaza, PlayToEnd(scriptId)); // 끝까지 본 뒤 반영

            Assert.That(message, Does.Contain("호감도")); // 결과 안내
            Assert.That(AffinityService.GetAffinity(saveData, "CH_SERENA"), Is.GreaterThanOrEqualTo(VillageActionService.ZoneEventAffinity)); // 호감도 증가
            Assert.That(saveData.CurrentTime, Is.EqualTo(phase + 1)); // 시간 1칸
            Assert.That(saveData.FindCharacter("CH_SERENA").LastVillageEventDay, Is.EqualTo(day)); // 오늘 기록
            saveData.SetCurrentTime(phase); // 같은 날 같은 자리로 되돌려
            Assert.That(VillageActionService.CanDoZoneEvent(saveData, "CH_SERENA", VillageZone.Plaza, out string again), Is.False); // 하루 1회
            Assert.That(again, Does.Contain("오늘은")); // 안내
        }

        [Test] // 여관 특별한 밤 : 밤 · 결속 3단계 · 결속 자원 1 → 끝까지 보면 자원 소모 · 다음 날 아침
        public void InnEvent_RequiresNightBondAndResource_ThenNextMorning() // 특별한 밤 테스트
        {
            SaveData saveData = CreateSave(); // 새 게임
            MoveTo(saveData, "CH_SERENA", VillageZone.Inn, SaveTimeOfDay.Night); // 밤 여관
            int day = saveData.CurrentDay; // 이벤트 일차
            Assert.That(BondService.GetResource(saveData), Is.EqualTo(1)); // 첫 지급 결속 자원 1

            Assert.That(VillageActionService.CanStartInnEvent(saveData, "CH_SERENA", out string noBond), Is.False); // 결속 0단계
            Assert.That(noBond, Does.Contain("결속 3단계")); // 안내
            saveData.FindCharacter("CH_SERENA").SetBondLevel(3); // 결속 3단계
            Assert.That(VillageActionService.CanStartInnEvent(saveData, "CH_SERENA", out string reason), Is.True, reason); // 가능
            string message = VillageActionService.CompleteInnEvent(saveData, "CH_SERENA", PlayToEnd(VillageActionService.GetInnEventScriptId("CH_SERENA"))); // 끝까지 본 뒤 반영

            Assert.That(message, Does.Contain("결속 자원 -1")); // 결과 안내
            Assert.That(saveData.BondResource, Is.EqualTo(0)); // 자원 소모 (조회 전 저장값)
            Assert.That(saveData.CurrentDay, Is.EqualTo(day + 1)); // 다음 날
            Assert.That(saveData.CurrentTime, Is.EqualTo(SaveTimeOfDay.Morning)); // 아침
            Assert.That(saveData.FindCharacter("CH_SERENA").LastInnEventDay, Is.EqualTo(day)); // 기록
        }

        [Test] // 구역 이벤트 16편 + 특별한 밤 4편 대사 파일 검사
        public void VillageScripts_ExistAndValidateForStarters() // 대사 파일 테스트
        {
            foreach (string characterId in Starters) // 초기 4인
            {
                List<string> ids = new List<string> { VillageActionService.GetInnEventScriptId(characterId) }; // 특별한 밤

                foreach (VillageZoneInfo info in VillageZoneCatalog.All) // 구역 순회
                {
                    if (VillageActionService.HasZoneEvent(info.Zone)) ids.Add(VillageActionService.GetZoneEventScriptId(characterId, info.Zone)); // 이벤트 구역
                }

                foreach (string scriptId in ids) // 파일 순회
                {
                    DialogueScript script = DialogueLibrary.Load(scriptId); // 불러오기
                    List<string> errors = new List<string>(); // 오류 목록
                    Assert.That(script, Is.Not.Null, scriptId); // 존재
                    Assert.That(DialogueLibrary.Validate(script, errors), Is.True, string.Join("\n", errors)); // 구조 검사
                    Assert.That(script.CharacterId, Is.EqualTo(characterId), scriptId); // 캐릭터 일치
                    PlayToEnd(scriptId); // 끝까지 도달
                }
            }
        }
    }
}
