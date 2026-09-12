using System; // 날짜 자료형
using System.Collections.Generic; // 목록 자료형
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Core; // 환경 설정 기능
using ProjectH.SaveSystem; // 저장 슬롯 기능
using ProjectH.UI; // 대화 속도 연결 확인 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class SaveSlotAndSettingsTests // Day72 저장 슬롯(3×2 = 6칸) · 설정/접근성 테스트
    {
        [TearDown] // 테스트 정리 표시
        public void TearDown() // 설정을 기본값으로 되돌려 다른 테스트에 영향 주지 않기
        {
            GameSettings.ResetToDefault(); // 기본값 복원
        }

        [Test] // 한 페이지는 가로 3 × 세로 2 = 6칸이고, 페이지마다 칸 번호가 이어진다
        public void SlotCatalog_HasSixCellsPerPage() // 슬롯 구성 테스트
        {
            Assert.That(SaveSlotCatalog.Columns, Is.EqualTo(3)); // 가로 3
            Assert.That(SaveSlotCatalog.Rows, Is.EqualTo(2)); // 세로 2
            Assert.That(SaveSlotCatalog.SlotsPerPage, Is.EqualTo(6)); // 한 페이지 6칸
            Assert.That(SaveSlotCatalog.SlotCount, Is.EqualTo(SaveSlotCatalog.SlotsPerPage * SaveSlotCatalog.PageCount)); // 전체 칸 수
            Assert.That(SaveSlotCatalog.IsValidSlot(0), Is.True); // 첫 칸
            Assert.That(SaveSlotCatalog.IsValidSlot(SaveSlotCatalog.SlotCount - 1), Is.True); // 마지막 칸
            Assert.That(SaveSlotCatalog.IsValidSlot(-1), Is.False); // 범위 밖
            Assert.That(SaveSlotCatalog.IsValidSlot(SaveSlotCatalog.SlotCount), Is.False); // 범위 밖
            Assert.That(SaveSlotCatalog.IsValidPage(SaveSlotCatalog.PageCount), Is.False); // 페이지 범위 밖
        }

        [Test] // 칸 번호는 1부터, 파일 이름과 페이지·가로·세로 위치가 서로 맞는다
        public void SlotCatalog_MapsNumbersFilesAndGridPositions() // 칸 배치 테스트
        {
            HashSet<string> fileNames = new HashSet<string>(StringComparer.Ordinal); // 파일 이름 중복 검사

            for (int slot = 0; slot < SaveSlotCatalog.SlotCount; slot++) // 전체 칸 순회
            {
                Assert.That(SaveSlotCatalog.GetSlotNumber(slot), Is.EqualTo(slot + 1), $"slot {slot}"); // 화면 번호는 1부터
                Assert.That(fileNames.Add(SaveSlotCatalog.GetFileName(slot)), Is.True, $"slot {slot}"); // 파일 이름 중복 없음
                int page = SaveSlotCatalog.GetPage(slot); // 페이지
                Assert.That(page, Is.InRange(0, SaveSlotCatalog.PageCount - 1), $"slot {slot}"); // 페이지 범위
                Assert.That(SaveSlotCatalog.GetColumn(slot), Is.InRange(0, SaveSlotCatalog.Columns - 1), $"slot {slot}"); // 가로 범위
                Assert.That(SaveSlotCatalog.GetRow(slot), Is.InRange(0, SaveSlotCatalog.Rows - 1), $"slot {slot}"); // 세로 범위
                int rebuilt = SaveSlotCatalog.GetFirstSlotOfPage(page) + (SaveSlotCatalog.GetRow(slot) * SaveSlotCatalog.Columns) + SaveSlotCatalog.GetColumn(slot); // 위치로 다시 만든 번호
                Assert.That(rebuilt, Is.EqualTo(slot), $"slot {slot}"); // 원래 칸과 일치
            }

            Assert.That(SaveSlotCatalog.GetFileName(0), Is.EqualTo("save_slot_01.json")); // 첫 칸 파일 이름
            Assert.That(SaveSlotCatalog.GetFirstSlotOfPage(1), Is.EqualTo(SaveSlotCatalog.SlotsPerPage)); // 2페이지 첫 칸
        }

        [Test] // 빈 칸과 저장이 있는 칸의 표시 문구
        public void SlotInfo_ShowsDateAndSummary() // 표시 문구 테스트
        {
            SaveSlotInfo empty = new SaveSlotInfo(4); // 빈 칸
            Assert.That(empty.Exists, Is.False); // 비어 있음
            Assert.That(empty.GetSummaryText(), Is.EqualTo("비어 있음")); // 요약
            Assert.That(empty.GetDateText(), Is.EqualTo("기록 없음")); // 날짜

            DateTime savedAt = new DateTime(2026, 9, 12, 14, 3, 0); // 저장 시각
            SaveSlotInfo filled = new SaveSlotInfo(4, savedAt, 12, "저녁", "챕터 6 · 지워진 이름", "시우", 5, 14); // 저장이 있는 칸
            Assert.That(filled.Exists, Is.True); // 저장 있음
            Assert.That(filled.GetDateText(), Is.EqualTo("2026-09-12 14:03")); // 날짜·시간
            Assert.That(filled.GetSummaryText(), Does.Contain("시우")); // 주인공 이름
            Assert.That(filled.GetSummaryText(), Does.Contain("12일차 저녁")); // 일차·시간대
            Assert.That(filled.GetSummaryText(), Does.Contain("동료 5명")); // 동료 수
            Assert.That(filled.GetSummaryText(), Does.Contain("Lv.14")); // 최고 레벨
        }

        [Test] // 저장 시각이 기록되고, 새 저장은 기록이 비어 있다
        public void SaveData_RecordsSavedTime() // 저장 시각 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 새 게임
            Assert.That(saveData.SavedAt, Is.EqualTo(DateTime.MinValue)); // 아직 저장한 적 없음
            DateTime before = DateTime.Now.AddSeconds(-1); // 기준 시각
            saveData.MarkSavedNow(); // 저장 시각 기록
            Assert.That(saveData.SavedAt, Is.GreaterThan(before)); // 기록됨
        }

        [Test] // 설정 값은 범위를 벗어나지 않는다
        public void Settings_ClampValues() // 설정 범위 테스트
        {
            GameSettings.SetMasterVolume(5f); // 너무 큼
            Assert.That(GameSettings.MasterVolume, Is.EqualTo(GameSettings.MaxVolume).Within(0.001f)); // 상한
            GameSettings.SetMasterVolume(-5f); // 너무 작음
            Assert.That(GameSettings.MasterVolume, Is.EqualTo(GameSettings.MinVolume).Within(0.001f)); // 하한
            GameSettings.SetDialogueSpeed(99f); // 너무 빠름
            Assert.That(GameSettings.DialogueSpeed, Is.EqualTo(GameSettings.MaxDialogueSpeed).Within(0.001f)); // 상한
            GameSettings.SetAutoAdvance(0f); // 너무 짧음
            Assert.That(GameSettings.AutoAdvanceSeconds, Is.EqualTo(GameSettings.MinAutoAdvance).Within(0.001f)); // 하한
            GameSettings.SetTextScale(99f); // 너무 큼
            Assert.That(GameSettings.TextScale, Is.EqualTo(GameSettings.MaxTextScale).Within(0.001f)); // 상한
        }

        [Test] // 접근성 : 글자 크기·흔들림·판정 완화가 실제 값에 반영된다
        public void Settings_AccessibilityAffectsGameValues() // 접근성 테스트
        {
            GameSettings.SetTextScale(1f); // 기본 크기
            Assert.That(GameSettings.ScaleFontSize(20), Is.EqualTo(20)); // 그대로
            GameSettings.SetTextScale(GameSettings.MaxTextScale); // 가장 큰 글자
            Assert.That(GameSettings.ScaleFontSize(20), Is.GreaterThan(20)); // 커짐

            GameSettings.SetReduceShake(false); // 흔들림 켬
            Assert.That(GameSettings.GetShakeScale(), Is.EqualTo(1f).Within(0.001f)); // 그대로
            GameSettings.SetReduceShake(true); // 흔들림 끔
            Assert.That(GameSettings.GetShakeScale(), Is.EqualTo(0f).Within(0.001f)); // 흔들리지 않음

            GameSettings.SetEasyTiming(false); // 판정 기본
            Assert.That(GameSettings.GetTimingWindowScale(), Is.EqualTo(1f).Within(0.001f)); // 그대로
            GameSettings.SetEasyTiming(true); // 판정 완화
            Assert.That(GameSettings.GetTimingWindowScale(), Is.EqualTo(1f + GameSettings.EasyTimingBonus).Within(0.001f)); // 넓어짐
        }

        [Test] // 대화 화면이 통합 설정의 대사 속도·자동 넘김을 그대로 따른다
        public void DialogueSettings_FollowGameSettings() // 스토리 설정 연결 테스트
        {
            GameSettings.SetDialogueSpeed(1f); // 기본 속도
            Assert.That(DialogueSettings.GetCharsPerSecond(), Is.EqualTo(DialogueSettings.BaseCharsPerSecond).Within(0.01f)); // 기준 속도
            GameSettings.SetDialogueSpeed(2f); // 두 배
            Assert.That(DialogueSettings.GetCharsPerSecond(), Is.EqualTo(DialogueSettings.BaseCharsPerSecond * 2f).Within(0.01f)); // 두 배로 빨라짐

            GameSettings.SetDialogueSpeed(1f); // 속도 고정
            GameSettings.SetAutoAdvance(GameSettings.MinAutoAdvance); // 짧은 대기
            float shortDelay = DialogueSettings.GetAutoDelay(40); // 대기 시간
            GameSettings.SetAutoAdvance(GameSettings.MaxAutoAdvance); // 긴 대기
            float longDelay = DialogueSettings.GetAutoDelay(40); // 대기 시간
            Assert.That(longDelay, Is.GreaterThan(shortDelay)); // 설정대로 길어짐
            Assert.That(DialogueSettings.GetAutoDelay(0), Is.EqualTo(GameSettings.AutoAdvanceSeconds).Within(0.01f)); // 빈 대사는 기본 대기만

            float slowPerChar = DialogueSettings.GetAutoDelay(100) - DialogueSettings.GetAutoDelay(0); // 느릴 때 길이 보정
            GameSettings.SetDialogueSpeed(GameSettings.MaxDialogueSpeed); // 빠르게
            float fastPerChar = DialogueSettings.GetAutoDelay(100) - DialogueSettings.GetAutoDelay(0); // 빠를 때 길이 보정
            Assert.That(fastPerChar, Is.LessThan(slowPerChar)); // 빠를수록 글자당 대기도 짧아짐
            Assert.That(DialogueSettings.GetSpeedText(), Does.Contain("대사 속도")); // 표시 문구
        }

        [Test] // 기본값 되돌리기
        public void Settings_ResetToDefault() // 기본값 테스트
        {
            GameSettings.SetTextScale(GameSettings.MaxTextScale); // 바꿔 두기
            GameSettings.SetEasyTiming(true); // 바꿔 두기
            GameSettings.ResetToDefault(); // 되돌리기
            Assert.That(GameSettings.TextScale, Is.EqualTo(1f).Within(0.001f)); // 기본 글자 크기
            Assert.That(GameSettings.EasyTiming, Is.False); // 기본 판정
            Assert.That(GameSettings.ReduceShake, Is.False); // 기본 흔들림
        }
    }
}
