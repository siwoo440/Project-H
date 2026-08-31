using System; // 예외 자료형
using System.Collections.Generic; // 컬렉션 자료형

namespace ProjectH.Data // 프로젝트 데이터 영역
{
    public sealed class DataRegistry<T> where T : class, IDataRecord // ID 데이터 저장소
    {
        private readonly Dictionary<string, T> records = new Dictionary<string, T>(StringComparer.Ordinal); // ID 사전

        public int Count => records.Count; // 데이터 개수 반환

        public void Build(IEnumerable<T> source, IList<string> errors, string label) // 데이터 사전 생성
        {
            records.Clear(); // 기존 데이터 제거

            if (source == null) // 입력 목록 확인
            {
                errors.Add($"[{label}] 데이터 목록이 없습니다."); // 목록 누락 오류
                return; // 생성 중단
            }

            foreach (T record in source) // 데이터 순회
            {
                if (record == null) // 데이터 참조 확인
                {
                    errors.Add($"[{label}] null 데이터가 포함되어 있습니다."); // null 데이터 오류
                    continue; // 다음 데이터 이동
                }

                if (string.IsNullOrWhiteSpace(record.Id)) // ID 값 확인
                {
                    errors.Add($"[{label}] 비어 있는 ID가 있습니다."); // 빈 ID 오류
                    continue; // 다음 데이터 이동
                }

                if (!records.TryAdd(record.Id, record)) // 중복 ID 등록 확인
                {
                    errors.Add($"[{label}] 중복 ID: {record.Id}"); // 중복 ID 오류
                }
            }
        }

        public bool TryGet(string id, out T record) // ID 데이터 조회
        {
            if (string.IsNullOrWhiteSpace(id)) // 조회 ID 확인
            {
                record = null; // 빈 결과 지정
                return false; // 조회 실패 반환
            }

            return records.TryGetValue(id, out record); // 사전 조회 결과 반환
        }

        public T GetOrDefault(string id) // ID 데이터 단순 조회
        {
            return TryGet(id, out T record) ? record : null; // 조회 결과 반환
        }
    }
}
