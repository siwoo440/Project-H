using System; // 고유 ID 기능
using System.IO; // 파일 입출력
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.SaveSystem; // 안전 저장 기능
using UnityEngine; // 임시 폴더 · JSON 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class SaveSafetyTests // Day81 저장 안전장치 테스트 (쓰는 도중 꺼져도 원본이 남고, 원본이 깨지면 백업으로 되돌린다)
    {
        private const string First = "{\n  \"day\": 1\n}"; // 첫 저장 내용
        private const string Second = "{\n  \"day\": 2\n}"; // 두 번째 저장 내용
        private const string Third = "{\n  \"day\": 3\n}"; // 세 번째 저장 내용
        private const string Broken = "{\n  \"day\": "; // 쓰다 만 내용

        private string folder; // 테스트 폴더
        private string path; // 저장 파일 경로

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 테스트마다 빈 폴더 준비
        {
            folder = Path.Combine(Application.temporaryCachePath, "ProjectH_SaveSafety_" + Guid.NewGuid().ToString("N")); // 고유 폴더
            Directory.CreateDirectory(folder); // 폴더 생성
            path = Path.Combine(folder, "save_test.json"); // 저장 파일
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 테스트 폴더 삭제
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true); // 폴더와 내용 삭제
        }

        [Test] // 첫 저장은 저장 파일 하나만 남긴다 (임시 파일 · 백업 없음)
        public void FirstWrite_CreatesOnlyTheSaveFile() // 첫 저장 테스트
        {
            SaveFileStore.Write(path, First); // 첫 저장
            Assert.That(File.ReadAllText(path), Is.EqualTo(First)); // 내용
            Assert.That(File.Exists(SaveFileStore.GetTempPath(path)), Is.False); // 임시 파일 없음
            Assert.That(File.Exists(SaveFileStore.GetBackupPath(path)), Is.False); // 백업 없음
            Assert.That(Directory.GetFiles(folder).Length, Is.EqualTo(1)); // 파일 하나
        }

        [Test] // 다시 저장하면 직전 저장이 백업으로 남고, 임시 파일은 남지 않는다
        public void SecondWrite_KeepsPreviousSaveAsBackup() // 백업 테스트
        {
            SaveFileStore.Write(path, First); // 첫 저장
            SaveFileStore.Write(path, Second); // 두 번째 저장
            Assert.That(File.ReadAllText(path), Is.EqualTo(Second)); // 원본은 새 내용
            Assert.That(File.ReadAllText(SaveFileStore.GetBackupPath(path)), Is.EqualTo(First)); // 백업은 직전 내용
            Assert.That(File.Exists(SaveFileStore.GetTempPath(path)), Is.False); // 임시 파일 없음

            SaveFileStore.Write(path, Third); // 세 번째 저장
            Assert.That(File.ReadAllText(path), Is.EqualTo(Third)); // 원본
            Assert.That(File.ReadAllText(SaveFileStore.GetBackupPath(path)), Is.EqualTo(Second)); // 백업은 한 단계씩 따라온다
        }

        [Test] // 원본이 깨져 있으면 백업으로 넘기지 않는다 (좋은 백업을 깨진 파일로 덮지 않는다)
        public void BrokenOriginal_DoesNotReplaceGoodBackup() // 깨진 원본 테스트
        {
            SaveFileStore.Write(path, First); // 첫 저장
            SaveFileStore.Write(path, Second); // 두 번째 저장 (백업 = 첫 저장)
            File.WriteAllText(path, Broken); // 원본이 쓰다 만 상태가 됨
            SaveFileStore.Write(path, Third); // 그 위에 다시 저장
            Assert.That(File.ReadAllText(path), Is.EqualTo(Third)); // 원본은 새 내용
            Assert.That(File.ReadAllText(SaveFileStore.GetBackupPath(path)), Is.EqualTo(First)); // 백업은 멀쩡한 첫 저장 그대로
        }

        [Test] // 끝까지 쓰인 파일만 온전하다고 본다
        public void LooksComplete_ChecksBothEnds() // 온전함 확인 테스트
        {
            Assert.That(SaveFileStore.LooksComplete(path), Is.False); // 파일 없음
            File.WriteAllText(path, First); // 온전한 파일
            Assert.That(SaveFileStore.LooksComplete(path), Is.True); // 온전함
            File.WriteAllText(path, "﻿  " + First + "\r\n\r\n"); // 앞에 BOM · 공백, 뒤에 줄바꿈
            Assert.That(SaveFileStore.LooksComplete(path), Is.True); // 앞뒤 공백은 무시
            File.WriteAllText(path, Broken); // 쓰다 만 파일
            Assert.That(SaveFileStore.LooksComplete(path), Is.False); // 온전하지 않음
            File.WriteAllText(path, string.Empty); // 빈 파일
            Assert.That(SaveFileStore.LooksComplete(path), Is.False); // 온전하지 않음
            Assert.That(SaveFileStore.LooksComplete(null), Is.False); // null 안전
        }

        [Test] // 읽을 파일 : 멀쩡한 원본 → 멀쩡한 백업 → 없음
        public void PickReadable_FallsBackToBackup() // 읽을 파일 고르기 테스트
        {
            Assert.That(SaveFileStore.PickReadable(path), Is.Null); // 아무것도 없음
            SaveFileStore.Write(path, First); // 첫 저장
            SaveFileStore.Write(path, Second); // 두 번째 저장
            Assert.That(SaveFileStore.PickReadable(path), Is.EqualTo(path)); // 원본
            File.WriteAllText(path, Broken); // 원본이 깨짐
            Assert.That(SaveFileStore.PickReadable(path), Is.EqualTo(SaveFileStore.GetBackupPath(path))); // 백업
            File.WriteAllText(SaveFileStore.GetBackupPath(path), Broken); // 백업도 깨짐
            Assert.That(SaveFileStore.PickReadable(path), Is.Null); // 읽을 것이 없음
            Assert.That(SaveFileStore.PickReadable(null), Is.Null); // null 안전
        }

        [Test] // 원본이 사라져도 백업이 있으면 저장이 있는 것이고, 지울 때는 백업까지 지운다
        public void Exists_Restore_Delete_CoverTheBackup() // 존재 · 되돌리기 · 삭제 테스트
        {
            Assert.That(SaveFileStore.Exists(path), Is.False); // 없음
            Assert.That(SaveFileStore.RestoreFromBackup(path), Is.False); // 백업 없음
            SaveFileStore.Write(path, First); // 첫 저장
            SaveFileStore.Write(path, Second); // 두 번째 저장
            File.Delete(path); // 원본만 사라짐
            Assert.That(SaveFileStore.Exists(path), Is.True); // 백업이 있으므로 저장 있음
            Assert.That(SaveFileStore.RestoreFromBackup(path), Is.True); // 되돌림
            Assert.That(File.ReadAllText(path), Is.EqualTo(First)); // 직전 저장으로 복구

            SaveFileStore.Delete(path); // 저장 삭제
            Assert.That(SaveFileStore.Exists(path), Is.False); // 없음
            Assert.That(Directory.GetFiles(folder), Is.Empty); // 원본 · 백업 · 임시 파일 모두 없음 (백업이 남으면 지운 저장이 되살아난다)
            Assert.That(() => SaveFileStore.Delete(path), Throws.Nothing); // 없는 것을 지워도 안전
        }

        [Test] // 실제 저장 데이터 : 원본이 중간에서 잘려도 백업에서 직전 진행을 읽을 수 있다
        public void RealSave_SurvivesTruncatedFile() // 실제 저장 복구 테스트
        {
            SaveData save = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 새 게임
            SaveFileStore.Write(path, JsonUtility.ToJson(save, true)); // 1일차 저장
            save.SetCurrentDay(5); // 진행
            string later = JsonUtility.ToJson(save, true); // 5일차 내용
            SaveFileStore.Write(path, later); // 5일차 저장 (백업 = 1일차)
            File.WriteAllText(path, later.Substring(0, later.Length / 2)); // 쓰는 도중 꺼진 상황 (절반만 쓰임)

            Assert.That(SaveFileStore.LooksComplete(path), Is.False); // 원본은 온전하지 않음
            string readable = SaveFileStore.PickReadable(path); // 읽을 파일
            Assert.That(readable, Is.EqualTo(SaveFileStore.GetBackupPath(path))); // 백업
            SaveData restored = JsonUtility.FromJson<SaveData>(File.ReadAllText(readable)); // 백업 읽기
            Assert.That(restored, Is.Not.Null); // 읽힘
            Assert.That(restored.CurrentDay, Is.EqualTo(1)); // 직전 진행 (1일차)
        }
    }
}
