using System.Collections.Generic; // 목록 자료형
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Data; // 프로젝트 데이터 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class DataRegistryTests // 데이터 저장소 테스트
    {
        [Test] // 테스트 표시
        public void Build_WithUniqueIds_CanLookupRecords() // 정상 ID 조회 테스트
        {
            DataRegistry<FakeRecord> registry = new DataRegistry<FakeRecord>(); // 테스트 저장소 생성
            List<string> errors = new List<string>(); // 오류 목록 생성
            List<FakeRecord> records = new List<FakeRecord> // 테스트 데이터 생성
            {
                new FakeRecord("A"), // 첫 데이터 생성
                new FakeRecord("B") // 둘째 데이터 생성
            };

            registry.Build(records, errors, "Test"); // 저장소 생성 실행

            Assert.That(errors, Is.Empty); // 오류 없음 검증
            Assert.That(registry.Count, Is.EqualTo(2)); // 데이터 개수 검증
            Assert.That(registry.GetOrDefault("B"), Is.SameAs(records[1])); // ID 조회 검증
        }

        [Test] // 테스트 표시
        public void Build_WithDuplicateId_AddsValidationError() // 중복 ID 검증 테스트
        {
            DataRegistry<FakeRecord> registry = new DataRegistry<FakeRecord>(); // 테스트 저장소 생성
            List<string> errors = new List<string>(); // 오류 목록 생성
            List<FakeRecord> records = new List<FakeRecord> // 테스트 데이터 생성
            {
                new FakeRecord("A"), // 첫 데이터 생성
                new FakeRecord("A") // 중복 데이터 생성
            };

            registry.Build(records, errors, "Test"); // 저장소 생성 실행

            Assert.That(errors.Count, Is.EqualTo(1)); // 오류 개수 검증
            Assert.That(errors[0], Does.Contain("중복 ID")); // 중복 오류 문구 검증
            Assert.That(registry.Count, Is.EqualTo(1)); // 등록 개수 검증
        }

        [Test] // 테스트 표시
        public void Build_WithBlankId_AddsValidationError() // 빈 ID 검증 테스트
        {
            DataRegistry<FakeRecord> registry = new DataRegistry<FakeRecord>(); // 테스트 저장소 생성
            List<string> errors = new List<string>(); // 오류 목록 생성
            List<FakeRecord> records = new List<FakeRecord> // 테스트 데이터 생성
            {
                new FakeRecord(" ") // 빈 ID 데이터 생성
            };

            registry.Build(records, errors, "Test"); // 저장소 생성 실행

            Assert.That(errors.Count, Is.EqualTo(1)); // 오류 개수 검증
            Assert.That(errors[0], Does.Contain("비어 있는 ID")); // 빈 ID 오류 검증
            Assert.That(registry.Count, Is.EqualTo(0)); // 등록 없음 검증
        }

        private sealed class FakeRecord : IDataRecord // 테스트용 데이터
        {
            public FakeRecord(string id) // 테스트 데이터 생성자
            {
                Id = id; // 테스트 ID 저장
            }

            public string Id { get; } // 테스트 ID 반환
        }
    }
}
