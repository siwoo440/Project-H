using System; // 예외 · 수학 기능
using System.IO; // 파일 입출력

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    public static class SaveFileStore // 저장 파일을 안전하게 쓰고 읽는 기능 (Day81 신규 — 쓰는 도중 꺼져도 원본이 남고, 원본이 깨지면 백업으로 되돌린다)
    {
        public const string TempSuffix = ".tmp"; // 쓰는 중인 임시 파일 꼬리표
        public const string BackupSuffix = ".bak"; // 직전 저장 백업 꼬리표
        private const int EdgeScanBytes = 16; // 파일 앞뒤에서 살펴보는 바이트 수

        public static string GetTempPath(string path) => path + TempSuffix; // 임시 파일 경로

        public static string GetBackupPath(string path) => path + BackupSuffix; // 백업 파일 경로

        public static bool Exists(string path) // 저장이 있는지 (원본 또는 백업)
        {
            return !string.IsNullOrEmpty(path) && (File.Exists(path) || File.Exists(GetBackupPath(path))); // 둘 중 하나라도 있으면 이어 할 수 있다
        }

        public static void Write(string path, string contents) // 안전 저장 : 임시 파일에 다 쓴 뒤 원본과 바꾸고, 멀쩡한 원본은 백업으로 남긴다
        {
            string temp = GetTempPath(path); // 임시 파일
            string backup = GetBackupPath(path); // 백업 파일
            File.WriteAllText(temp, contents); // 1) 임시 파일에 전부 쓴다 — 여기서 꺼져도 원본은 그대로다

            if (!File.Exists(path)) // 2) 첫 저장
            {
                File.Move(temp, path); // 임시 파일이 원본이 된다
                return; // 저장 끝
            }

            if (LooksComplete(path)) // 3) 멀쩡한 원본만 백업으로 넘긴다 (깨진 원본이 좋은 백업을 덮지 않게)
            {
                try // 한 번에 교체 시도
                {
                    File.Replace(temp, path, backup); // 원본 → 백업, 임시 → 원본 (운영체제가 한 번에 처리한다)
                    return; // 저장 끝
                }
                catch (IOException) // 다른 프로그램이 파일을 잠깐 잡고 있는 경우
                {
                    if (File.Exists(path)) File.Copy(path, backup, true); // 복사 방식으로 백업
                }
                catch (PlatformNotSupportedException) // 교체를 지원하지 않는 환경
                {
                    if (File.Exists(path)) File.Copy(path, backup, true); // 복사 방식으로 백업
                }
            }

            File.Copy(temp, path, true); // 4) 원본 덮어쓰기 (한 번에 교체하지 못했을 때만 온다)
            File.Delete(temp); // 임시 파일 정리
        }

        public static bool LooksComplete(string path) // 끝까지 쓰인 파일인지 가볍게 확인 (첫 글자 '{' · 끝 글자 '}' — 전체를 해석하지 않아 저장할 때마다 불러도 부담이 없다)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false; // 파일 없음

            try // 읽기 시도
            {
                using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) // 읽기 전용으로 열기
                {
                    int span = (int)Math.Min(EdgeScanBytes, stream.Length); // 살펴볼 길이
                    if (span < 2) return false; // 너무 짧음
                    byte[] head = new byte[span]; // 앞부분
                    int headCount = stream.Read(head, 0, span); // 앞부분 읽기
                    stream.Seek(-span, SeekOrigin.End); // 끝으로 이동
                    byte[] tail = new byte[span]; // 뒷부분
                    int tailCount = stream.Read(tail, 0, span); // 뒷부분 읽기
                    return FirstMeaningful(head, headCount) == '{' && LastMeaningful(tail, tailCount) == '}'; // 중괄호로 열리고 닫힘
                }
            }
            catch (IOException) // 읽기 실패
            {
                return false; // 온전하지 않은 것으로 본다
            }
            catch (UnauthorizedAccessException) // 권한 없음
            {
                return false; // 온전하지 않은 것으로 본다
            }
        }

        public static string PickReadable(string path) // 읽을 파일 고르기 : 멀쩡한 원본 → 멀쩡한 백업 → 없음(null)
        {
            if (string.IsNullOrEmpty(path)) return null; // 경로 없음
            if (LooksComplete(path)) return path; // 원본
            string backup = GetBackupPath(path); // 백업
            return LooksComplete(backup) ? backup : null; // 백업 또는 없음
        }

        public static bool RestoreFromBackup(string path) // 백업을 원본 자리로 되돌림 (원본이 깨졌을 때)
        {
            if (string.IsNullOrEmpty(path)) return false; // 경로 없음
            string backup = GetBackupPath(path); // 백업
            if (!File.Exists(backup)) return false; // 백업 없음
            File.Copy(backup, path, true); // 원본 자리에 복사
            return true; // 되돌림
        }

        public static void Delete(string path) // 원본 · 백업 · 임시 파일을 모두 지운다 (백업이 남으면 지운 저장이 되살아난다)
        {
            if (string.IsNullOrEmpty(path)) return; // 경로 없음
            DeleteIfExists(path); // 원본
            DeleteIfExists(GetBackupPath(path)); // 백업
            DeleteIfExists(GetTempPath(path)); // 임시 파일
        }

        private static void DeleteIfExists(string path) // 있으면 지움
        {
            if (File.Exists(path)) File.Delete(path); // 삭제
        }

        private static int FirstMeaningful(byte[] bytes, int count) // 앞에서 첫 글자 (BOM · 공백 제외, 없으면 -1)
        {
            for (int index = 0; index < count; index++) // 앞에서부터
            {
                byte value = bytes[index]; // 현재 바이트
                if (value == 0xEF || value == 0xBB || value == 0xBF || IsBlank(value)) continue; // BOM · 공백 건너뜀
                return value; // 첫 글자
            }

            return -1; // 없음
        }

        private static int LastMeaningful(byte[] bytes, int count) // 뒤에서 첫 글자 (공백 제외, 없으면 -1)
        {
            for (int index = count - 1; index >= 0; index--) // 뒤에서부터
            {
                if (!IsBlank(bytes[index])) return bytes[index]; // 마지막 글자
            }

            return -1; // 없음
        }

        private static bool IsBlank(byte value) => value == ' ' || value == '\n' || value == '\r' || value == '\t'; // 공백 문자인지
    }
}
