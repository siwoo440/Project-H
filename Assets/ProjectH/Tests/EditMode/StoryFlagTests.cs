using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.SaveSystem; // 저장 기능
using UnityEngine; // Unity JSON 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class StoryFlagTests // 스토리 플래그 테스트
    {
        [Test] // 테스트 표시
        public void StoryFlag_SetAndRemove_ChangesState() // 플래그 변경 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 새 저장 데이터 생성

            bool added = saveData.SetStoryFlag("STORY_TEST"); // 테스트 플래그 추가
            bool exists = saveData.HasStoryFlag("STORY_TEST"); // 플래그 존재 확인
            bool removed = saveData.RemoveStoryFlag("STORY_TEST"); // 테스트 플래그 제거

            Assert.That(added, Is.True); // 추가 결과 검증
            Assert.That(exists, Is.True); // 존재 결과 검증
            Assert.That(removed, Is.True); // 제거 결과 검증
            Assert.That(saveData.HasStoryFlag("STORY_TEST"), Is.False); // 제거 상태 검증
        }

        [Test] // 테스트 표시
        public void StoryFlag_JsonRoundTrip_PreservesFlags() // 플래그 저장 복원 테스트
        {
            SaveData source = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 원본 저장 데이터 생성
            source.SetStoryFlag("STORY_SERENA_JOINED"); // 스토리 플래그 설정
            string json = JsonUtility.ToJson(source, true); // JSON 직렬화
            SaveData loaded = JsonUtility.FromJson<SaveData>(json); // JSON 역직렬화
            loaded.EnsureDefaults(); // 누락 기본값 복원

            Assert.That(loaded.HasStoryFlag("STORY_SERENA_JOINED"), Is.True); // 플래그 복원 검증
        }
    }
}
