using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.SaveSystem; // 저장 및 지역 침식도 기능
using UnityEngine; // Unity JSON 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class RegionErosionServiceTests // Day44 지역 침식도 회귀 테스트
    {
        [Test] // 미등록 지역 기본 침식도 검증
        public void GetErosion_UnknownRegion_ReturnsZero() // 미등록 지역 기본값 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(System.Array.Empty<string>()); // 새 게임 데이터 생성

            Assert.That(RegionErosionService.GetErosion(saveData, "RG_FOREST"), Is.EqualTo(0)); // 기본 침식도 검증
        }

        [Test] // 침식도 증가 검증
        public void AddErosion_PositiveDelta_IncreasesErosion() // 침식도 증가 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(System.Array.Empty<string>()); // 새 게임 데이터 생성

            int result = RegionErosionService.AddErosion(saveData, "RG_FOREST", 45); // 침식도 45 증가 실행

            Assert.That(result, Is.EqualTo(45)); // 증가 결과 검증
        }

        [Test] // 침식도 최대값 보정 검증
        public void AddErosion_AboveMax_ClampsToHundred() // 최대값 초과 증가 보정 검증
        {
            SaveData saveData = SaveData.CreateNewGame(System.Array.Empty<string>()); // 새 게임 데이터 생성

            int result = RegionErosionService.AddErosion(saveData, "RG_FOREST", 999); // 최대값 초과 침식도 증가 실행

            Assert.That(result, Is.EqualTo(100)); // 최대값 보정 확인
        }

        [Test] // 침식도 최소값 보정 검증
        public void AddErosion_BelowMin_ClampsToZero() // 최소값 미만 감소 보정 검증
        {
            SaveData saveData = SaveData.CreateNewGame(System.Array.Empty<string>()); // 새 게임 데이터 생성
            RegionErosionService.AddErosion(saveData, "RG_FOREST", 30); // 침식도 30 적용

            int result = RegionErosionService.AddErosion(saveData, "RG_FOREST", -999); // 최소값 미만 침식도 감소 실행

            Assert.That(result, Is.EqualTo(0)); // 최소값 보정 확인
        }

        [Test] // 5단계 등급 경계값 검증
        public void ResolveTier_AtEachBoundary_ReturnsExpectedTier() // 등급 경계값 테스트
        {
            Assert.That(RegionErosionService.ResolveTier(0), Is.EqualTo(RegionErosionTier.Stable)); // 0 안정 검증
            Assert.That(RegionErosionService.ResolveTier(19), Is.EqualTo(RegionErosionTier.Stable)); // 19 안정 검증
            Assert.That(RegionErosionService.ResolveTier(20), Is.EqualTo(RegionErosionTier.Cracked)); // 20 균열 검증
            Assert.That(RegionErosionService.ResolveTier(39), Is.EqualTo(RegionErosionTier.Cracked)); // 39 균열 검증
            Assert.That(RegionErosionService.ResolveTier(40), Is.EqualTo(RegionErosionTier.Eroded)); // 40 침식 검증
            Assert.That(RegionErosionService.ResolveTier(59), Is.EqualTo(RegionErosionTier.Eroded)); // 59 침식 검증
            Assert.That(RegionErosionService.ResolveTier(60), Is.EqualTo(RegionErosionTier.Dangerous)); // 60 위험 검증
            Assert.That(RegionErosionService.ResolveTier(79), Is.EqualTo(RegionErosionTier.Dangerous)); // 79 위험 검증
            Assert.That(RegionErosionService.ResolveTier(80), Is.EqualTo(RegionErosionTier.Collapsed)); // 80 붕괴 검증
            Assert.That(RegionErosionService.ResolveTier(100), Is.EqualTo(RegionErosionTier.Collapsed)); // 100 붕괴 검증
        }

        [Test] // 등급이 높을수록 적 스탯 배율이 증가하는지 검증
        public void GetEnemyStatMultiplier_HigherTier_IsGreaterOrEqual() // 등급별 적 스탯 배율 상승 검증
        {
            float stable = RegionErosionService.GetEnemyStatMultiplier(RegionErosionTier.Stable); // 안정 등급 배율 조회
            float cracked = RegionErosionService.GetEnemyStatMultiplier(RegionErosionTier.Cracked); // 균열 등급 배율 조회
            float eroded = RegionErosionService.GetEnemyStatMultiplier(RegionErosionTier.Eroded); // 침식 등급 배율 조회
            float dangerous = RegionErosionService.GetEnemyStatMultiplier(RegionErosionTier.Dangerous); // 위험 등급 배율 조회
            float collapsed = RegionErosionService.GetEnemyStatMultiplier(RegionErosionTier.Collapsed); // 붕괴 등급 배율 조회

            Assert.That(cracked, Is.GreaterThan(stable)); // 균열이 안정보다 높은지 검증
            Assert.That(eroded, Is.GreaterThan(cracked)); // 침식이 균열보다 높은지 검증
            Assert.That(dangerous, Is.GreaterThan(eroded)); // 위험이 침식보다 높은지 검증
            Assert.That(collapsed, Is.GreaterThan(dangerous)); // 붕괴가 위험보다 높은지 검증
            Assert.That(stable, Is.EqualTo(1.00f)); // 안정 등급 기본 배율(가산 없음) 검증
        }

        [Test] // 서로 다른 지역의 침식도가 독립적으로 유지되는지 검증
        public void AddErosion_DifferentRegions_AreIndependent() // 지역별 독립성 검증
        {
            SaveData saveData = SaveData.CreateNewGame(System.Array.Empty<string>()); // 새 게임 데이터 생성
            RegionErosionService.AddErosion(saveData, "RG_FOREST", 70); // 첫 지역 침식도 적용
            RegionErosionService.AddErosion(saveData, "RG_SWAMP", 10); // 두 번째 지역 침식도 적용

            Assert.That(RegionErosionService.GetErosion(saveData, "RG_FOREST"), Is.EqualTo(70)); // 첫 지역 침식도 검증
            Assert.That(RegionErosionService.GetErosion(saveData, "RG_SWAMP"), Is.EqualTo(10)); // 두 번째 지역 침식도 검증
        }

        [Test] // 저장/로드 후 지역별 침식도 유지 검증
        public void JsonRoundTrip_WithRegionErosion_PreservesValue() // 침식도 JSON 왕복 저장 테스트
        {
            SaveData source = SaveData.CreateNewGame(System.Array.Empty<string>()); // 원본 저장 데이터 생성
            RegionErosionService.AddErosion(source, "RG_FOREST", 65); // 테스트 침식도 적용
            string json = JsonUtility.ToJson(source, true); // JSON 직렬화
            SaveData loaded = JsonUtility.FromJson<SaveData>(json); // JSON 역직렬화

            Assert.That(RegionErosionService.GetErosion(loaded, "RG_FOREST"), Is.EqualTo(65)); // 복원 침식도 검증
            Assert.That(RegionErosionService.GetErosionTier(loaded, "RG_FOREST"), Is.EqualTo(RegionErosionTier.Dangerous)); // 복원 등급 검증
        }

        [Test] // 미등록 지역 랜덤 초기화 범위 검증 (Day44 추가 작업)
        public void TryInitializeRandomErosion_UnknownRegion_AssignsValueInRange() // 랜덤 초기화 범위 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(System.Array.Empty<string>()); // 새 게임 데이터 생성

            bool initialized = RegionErosionService.TryInitializeRandomErosion(saveData, "RG_FOREST", out int erosion); // 랜덤 초기화 실행

            Assert.That(initialized, Is.True); // 신규 초기화 발생 검증
            Assert.That(erosion, Is.InRange(0, 100)); // 초기화 값 범위 검증
            Assert.That(saveData.HasRegionErosion("RG_FOREST"), Is.True); // 등록 여부 검증
        }

        [Test] // 이미 등록된 지역은 다시 초기화하지 않는지 검증 (Day44 추가 작업)
        public void TryInitializeRandomErosion_AlreadyRegistered_DoesNotReroll() // 재초기화 차단 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(System.Array.Empty<string>()); // 새 게임 데이터 생성
            RegionErosionService.AddErosion(saveData, "RG_FOREST", 40); // 지역 침식도 사전 등록

            bool initialized = RegionErosionService.TryInitializeRandomErosion(saveData, "RG_FOREST", out int erosion); // 재초기화 시도

            Assert.That(initialized, Is.False); // 재초기화 미발생 검증
            Assert.That(erosion, Is.EqualTo(40)); // 기존 값 유지 검증
        }

        [Test] // GetOrInitializeErosion 반복 호출 시 값이 유지되는지 검증 (Day44 추가 작업)
        public void GetOrInitializeErosion_CalledTwice_ReturnsSameValue() // 반복 호출 값 유지 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(System.Array.Empty<string>()); // 새 게임 데이터 생성

            int first = RegionErosionService.GetOrInitializeErosion(saveData, "RG_FOREST"); // 최초 조회 (랜덤 초기화 발생)
            int second = RegionErosionService.GetOrInitializeErosion(saveData, "RG_FOREST"); // 재조회

            Assert.That(second, Is.EqualTo(first)); // 값 유지 검증
        }

        [Test] // 등급이 높을수록 적 스탯 증가율(%)이 커지는지 검증 (Day44 추가 작업)
        public void GetEnemyStatBonusPercent_HigherErosion_IsGreaterPercent() // 침식도별 증가율 상승 검증
        {
            SaveData saveData = SaveData.CreateNewGame(System.Array.Empty<string>()); // 새 게임 데이터 생성
            RegionErosionService.AddErosion(saveData, "RG_STABLE", 0); // 안정 등급 침식도 적용
            RegionErosionService.AddErosion(saveData, "RG_COLLAPSED", 100); // 붕괴 등급 침식도 적용

            int stablePercent = RegionErosionService.GetEnemyStatBonusPercent(saveData, "RG_STABLE"); // 안정 등급 증가율 조회
            int collapsedPercent = RegionErosionService.GetEnemyStatBonusPercent(saveData, "RG_COLLAPSED"); // 붕괴 등급 증가율 조회

            Assert.That(stablePercent, Is.EqualTo(0)); // 안정 등급 증가율 0% 검증
            Assert.That(collapsedPercent, Is.EqualTo(50)); // 붕괴 등급 증가율 50% 검증
        }
    }
}
