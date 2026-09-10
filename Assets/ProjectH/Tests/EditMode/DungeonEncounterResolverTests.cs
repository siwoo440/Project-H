using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 던전 인카운터 웨이브 해석 기능
using ProjectH.Data; // 던전 데이터 기능
using UnityEngine; // Unity ScriptableObject 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class DungeonEncounterResolverTests // Day45 던전 인카운터 웨이브 해석 회귀 테스트
    {
        [Test] // 인카운터 데이터가 있으면 해당 웨이브 목록을 그대로 사용하는지 검증
        public void ResolveWaves_WithEncounterData_ReturnsConfiguredWaves() // 인카운터 데이터 우선 사용 테스트
        {
            DungeonData dungeon = ScriptableObject.CreateInstance<DungeonData>(); // 테스트용 던전 데이터 생성
            SetEncounterWaves(dungeon, new[]
            {
                new DungeonEncounterWave(new[] { "MON_A" }), // 1웨이브 구성
                new DungeonEncounterWave(new[] { "MON_B", "MON_C" }) // 2웨이브 구성
            });

            System.Collections.Generic.List<string[]> waves = DungeonEncounterResolver.ResolveWaves(dungeon, new[] { "MON_LEGACY" }); // 웨이브 해석 실행

            Assert.That(waves.Count, Is.EqualTo(2)); // 웨이브 개수 검증
            Assert.That(waves[0], Is.EqualTo(new[] { "MON_A" })); // 1웨이브 구성 검증
            Assert.That(waves[1], Is.EqualTo(new[] { "MON_B", "MON_C" })); // 2웨이브 구성 검증
        }

        [Test] // 인카운터 데이터가 없으면 레거시 단일 편성으로 대체되는지 검증
        public void ResolveWaves_WithoutEncounterData_FallsBackToLegacySingleWave() // 레거시 폴백 테스트
        {
            DungeonData dungeon = ScriptableObject.CreateInstance<DungeonData>(); // 인카운터 데이터 없는 던전 생성

            System.Collections.Generic.List<string[]> waves = DungeonEncounterResolver.ResolveWaves(dungeon, new[] { "MON_LEGACY" }); // 웨이브 해석 실행

            Assert.That(waves.Count, Is.EqualTo(1)); // 단일 웨이브 검증
            Assert.That(waves[0], Is.EqualTo(new[] { "MON_LEGACY" })); // 레거시 편성 유지 검증
        }

        [Test] // 던전 데이터 및 레거시 편성 모두 없으면 빈 목록을 반환하는지 검증
        public void ResolveWaves_WithNoDataAtAll_ReturnsEmptyList() // 완전 누락 처리 테스트
        {
            System.Collections.Generic.List<string[]> waves = DungeonEncounterResolver.ResolveWaves(null, null); // 웨이브 해석 실행

            Assert.That(waves.Count, Is.EqualTo(0)); // 빈 웨이브 목록 검증
        }

        [Test] // 빈 웨이브 항목은 결과에서 제외되는지 검증
        public void ResolveWaves_WithEmptyWaveEntries_ExcludesThem() // 빈 웨이브 제외 테스트
        {
            DungeonData dungeon = ScriptableObject.CreateInstance<DungeonData>(); // 테스트용 던전 데이터 생성
            SetEncounterWaves(dungeon, new[]
            {
                new DungeonEncounterWave(new string[0]), // 빈 웨이브 구성
                new DungeonEncounterWave(new[] { "MON_A" }) // 유효 웨이브 구성
            });

            System.Collections.Generic.List<string[]> waves = DungeonEncounterResolver.ResolveWaves(dungeon, null); // 웨이브 해석 실행

            Assert.That(waves.Count, Is.EqualTo(1)); // 빈 웨이브 제외 후 개수 검증
            Assert.That(waves[0], Is.EqualTo(new[] { "MON_A" })); // 유효 웨이브 유지 검증
        }

        private static void SetEncounterWaves(DungeonData dungeon, DungeonEncounterWave[] waves) // 테스트용 리플렉션 웨이브 주입
        {
            System.Reflection.FieldInfo field = typeof(DungeonData).GetField("encounterWaves", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); // 비공개 웨이브 필드 조회
            field.SetValue(dungeon, new System.Collections.Generic.List<DungeonEncounterWave>(waves)); // 테스트용 웨이브 목록 주입
        }
    }
}
