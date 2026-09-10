using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.SaveSystem; // 저장 및 호감도 진행 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class AffinityServiceTests // Day43 호감도 회귀 테스트
    {
        [Test] // 새 캐릭터 기본 호감도 검증
        public void NewCharacter_DefaultAffinity_IsZero() // 새 캐릭터 기본 호감도 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 새 게임 데이터 생성

            Assert.That(AffinityService.GetAffinity(saveData, "CH_SERENA"), Is.EqualTo(0)); // 기본 호감도 검증
        }

        [Test] // 호감도 증가 검증
        public void AddAffinity_PositiveDelta_IncreasesAffinity() // 호감도 증가 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 새 게임 데이터 생성

            int result = AffinityService.AddAffinity(saveData, "CH_SERENA", 30); // 호감도 30 증가 실행

            Assert.That(result, Is.EqualTo(30)); // 증가 결과 검증
        }

        [Test] // 호감도 최대값 보정 검증
        public void AddAffinity_AboveMax_ClampsToHundred() // 최대값 초과 증가 보정 검증
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 새 게임 데이터 생성

            int result = AffinityService.AddAffinity(saveData, "CH_SERENA", 999); // 최대값 초과 호감도 증가 실행

            Assert.That(result, Is.EqualTo(100)); // 최대값 보정 확인
        }

        [Test] // 호감도 최소값 보정 검증
        public void AddAffinity_BelowMin_ClampsToZero() // 최소값 미만 감소 보정 검증
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 새 게임 데이터 생성

            int result = AffinityService.AddAffinity(saveData, "CH_SERENA", -999); // 최소값 미만 호감도 감소 실행

            Assert.That(result, Is.EqualTo(0)); // 최소값 보정 확인
        }

        [Test] // 미보유 캐릭터 호감도 변경 무시 검증
        public void AddAffinity_UnknownCharacter_ReturnsZeroAndNoOp() // 미보유 캐릭터 처리 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 새 게임 데이터 생성

            int result = AffinityService.AddAffinity(saveData, "CH_UNKNOWN", 50); // 미보유 캐릭터 호감도 증가 시도

            Assert.That(result, Is.EqualTo(0)); // 변경 없음 결과 검증
        }

        [Test] // 5단계 등급 경계값 검증
        public void ResolveTier_AtEachBoundary_ReturnsExpectedTier() // 등급 경계값 테스트
        {
            Assert.That(AffinityService.ResolveTier(0), Is.EqualTo(AffinityTier.Stranger)); // 0 낯섦 검증
            Assert.That(AffinityService.ResolveTier(19), Is.EqualTo(AffinityTier.Stranger)); // 19 낯섦 검증
            Assert.That(AffinityService.ResolveTier(20), Is.EqualTo(AffinityTier.Acquaintance)); // 20 안면 검증
            Assert.That(AffinityService.ResolveTier(39), Is.EqualTo(AffinityTier.Acquaintance)); // 39 안면 검증
            Assert.That(AffinityService.ResolveTier(40), Is.EqualTo(AffinityTier.Friendly)); // 40 호감 검증
            Assert.That(AffinityService.ResolveTier(59), Is.EqualTo(AffinityTier.Friendly)); // 59 호감 검증
            Assert.That(AffinityService.ResolveTier(60), Is.EqualTo(AffinityTier.Trusted)); // 60 신뢰 검증
            Assert.That(AffinityService.ResolveTier(79), Is.EqualTo(AffinityTier.Trusted)); // 79 신뢰 검증
            Assert.That(AffinityService.ResolveTier(80), Is.EqualTo(AffinityTier.Bonded)); // 80 유대 검증
            Assert.That(AffinityService.ResolveTier(100), Is.EqualTo(AffinityTier.Bonded)); // 100 유대 검증
        }

        [Test] // 저장/로드 후 캐릭터별 호감도 유지 검증
        public void JsonRoundTrip_WithAffinity_PreservesValue() // 호감도 JSON 왕복 저장 테스트
        {
            SaveData source = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 원본 저장 데이터 생성
            AffinityService.AddAffinity(source, "CH_SERENA", 65); // 테스트 호감도 적용
            string json = UnityEngine.JsonUtility.ToJson(source, true); // JSON 직렬화
            SaveData loaded = UnityEngine.JsonUtility.FromJson<SaveData>(json); // JSON 역직렬화

            Assert.That(AffinityService.GetAffinity(loaded, "CH_SERENA"), Is.EqualTo(65)); // 복원 호감도 검증
            Assert.That(AffinityService.GetAffinityTier(loaded, "CH_SERENA"), Is.EqualTo(AffinityTier.Trusted)); // 복원 등급 검증
        }
    }
}
