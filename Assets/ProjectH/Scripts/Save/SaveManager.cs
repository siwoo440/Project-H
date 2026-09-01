using System; // 예외 자료형
using System.Collections.Generic; // 집합 자료형
using System.IO; // 파일 입출력
using ProjectH.Data; // 데이터 기능
using UnityEngine; // Unity 기본 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    [DisallowMultipleComponent] // 중복 컴포넌트 방지
    public sealed class SaveManager : MonoBehaviour // 로컬 저장 관리자
    {
        private const string SaveFileName = "save_001.json"; // 저장 파일 이름

        private static readonly string[] InitialCharacterIds = // 초기 캐릭터 ID 목록
        {
            "CH_SERENA", // 세레나 ID
            "CH_ELLEN", // 엘렌 ID
            "CH_LILIA", // 릴리아 ID
            "CH_EVE" // 이브 ID
        }; // 초기 캐릭터 목록 종료

        private DataManager dataManager; // 데이터 관리자 참조

        public bool IsInitialized { get; private set; } // 초기화 상태
        public bool HasSaveData { get; private set; } // 저장 존재 여부
        public SaveData CurrentSave { get; private set; } // 현재 저장 데이터
        public string SavePath { get; private set; } // 저장 파일 경로

        public void Initialize(DataManager manager) // 저장 관리자 초기화
        {
            if (IsInitialized) // 초기화 여부 확인
            {
                return; // 중복 초기화 중단
            }

            if (manager == null) // 데이터 관리자 확인
            {
                Debug.LogError("[Project H] SaveManager requires DataManager."); // 데이터 관리자 누락 로그
                return; // 초기화 중단
            }

            dataManager = manager; // 데이터 관리자 저장
            SavePath = Path.Combine(Application.persistentDataPath, SaveFileName); // 저장 경로 생성
            HasSaveData = File.Exists(SavePath); // 기존 저장 확인
            IsInitialized = true; // 초기화 완료 기록
            Debug.Log($"[Project H] SaveManager initialized. HasSaveData={HasSaveData}"); // 저장 초기화 로그
        }

        public bool CreateNewGame() // 새 게임 생성
        {
            if (!EnsureInitialized()) // 초기화 상태 확인
            {
                return false; // 새 게임 생성 실패
            }

            if (!ValidateInitialCharacters()) // 초기 캐릭터 검증
            {
                return false; // 새 게임 생성 실패
            }

            CurrentSave = SaveData.CreateNewGame(InitialCharacterIds); // 초기 저장 데이터 생성
            Debug.Log("[Project H] New game save data created."); // 새 게임 생성 로그
            return SaveCurrent(); // 초기 데이터 저장
        }

        public bool SaveCurrent() // 현재 진행 저장
        {
            if (!EnsureInitialized()) // 초기화 상태 확인
            {
                return false; // 저장 실패
            }

            if (CurrentSave == null) // 현재 저장 데이터 확인
            {
                Debug.LogError("[Project H] Current save data is empty."); // 저장 데이터 누락 로그
                return false; // 저장 실패
            }

            try // 파일 저장 시도
            {
                string json = JsonUtility.ToJson(CurrentSave, true); // 저장 데이터 JSON 변환
                File.WriteAllText(SavePath, json); // JSON 파일 저장
                HasSaveData = true; // 저장 존재 상태 갱신
                Debug.Log($"[Project H] Save complete. Path={SavePath}"); // 저장 완료 로그
                return true; // 저장 성공
            }
            catch (Exception exception) // 저장 예외 처리
            {
                Debug.LogError($"[Project H] Save failed. {exception.Message}"); // 저장 실패 로그
                return false; // 저장 실패
            }
        }

        public bool LoadCurrent() // 기존 진행 불러오기
        {
            if (!EnsureInitialized()) // 초기화 상태 확인
            {
                return false; // 불러오기 실패
            }

            if (!File.Exists(SavePath)) // 저장 파일 확인
            {
                HasSaveData = false; // 저장 없음 상태 갱신
                Debug.LogWarning("[Project H] Save file does not exist."); // 저장 없음 로그
                return false; // 불러오기 실패
            }

            try // 파일 불러오기 시도
            {
                string json = File.ReadAllText(SavePath); // JSON 파일 읽기
                SaveData loadedSave = JsonUtility.FromJson<SaveData>(json); // 저장 데이터 역직렬화

                if (!ValidateLoadedSave(loadedSave)) // 불러온 데이터 검증
                {
                    return false; // 불러오기 실패
                }

                CurrentSave = loadedSave; // 현재 저장 데이터 교체
                HasSaveData = true; // 저장 존재 상태 갱신
                Debug.Log($"[Project H] Load complete. Day={CurrentSave.CurrentDay}, Characters={CurrentSave.Characters.Count}"); // 불러오기 완료 로그
                return true; // 불러오기 성공
            }
            catch (Exception exception) // 불러오기 예외 처리
            {
                Debug.LogError($"[Project H] Load failed. {exception.Message}"); // 불러오기 실패 로그
                return false; // 불러오기 실패
            }
        }

        public bool DeleteSave() // 저장 파일 삭제
        {
            if (!EnsureInitialized()) // 초기화 상태 확인
            {
                return false; // 삭제 실패
            }

            try // 파일 삭제 시도
            {
                if (File.Exists(SavePath)) // 저장 파일 확인
                {
                    File.Delete(SavePath); // 저장 파일 삭제
                }

                CurrentSave = null; // 현재 저장 데이터 제거
                HasSaveData = false; // 저장 없음 상태 갱신
                Debug.Log("[Project H] Save deleted."); // 삭제 완료 로그
                return true; // 삭제 성공
            }
            catch (Exception exception) // 삭제 예외 처리
            {
                Debug.LogError($"[Project H] Save delete failed. {exception.Message}"); // 삭제 실패 로그
                return false; // 삭제 실패
            }
        }

        public bool ApplyDebugProgress() // 테스트 진행도 적용
        {
            if (CurrentSave == null) // 현재 저장 데이터 확인
            {
                Debug.LogError("[Project H] Create or load save data first."); // 진행 데이터 누락 로그
                return false; // 변경 실패
            }

            CharacterSaveData serena = CurrentSave.FindCharacter("CH_SERENA"); // 세레나 진행 조회

            if (serena == null) // 세레나 저장 데이터 확인
            {
                Debug.LogError("[Project H] CH_SERENA save data is missing."); // 세레나 누락 로그
                return false; // 변경 실패
            }

            CurrentSave.SetCurrentDay(7); // 테스트 일차 적용
            serena.SetLevel(5); // 테스트 레벨 적용
            serena.SetExperience(350); // 테스트 경험치 적용
            Debug.Log("[Project H] Debug progress applied. Day=7, CH_SERENA Lv=5, Exp=350"); // 테스트 변경 로그
            return true; // 변경 성공
        }

        public void LogCurrentState() // 현재 진행 상태 출력
        {
            if (CurrentSave == null) // 현재 저장 데이터 확인
            {
                Debug.Log("[Project H] Current save data is empty."); // 저장 없음 로그
                return; // 출력 중단
            }

            CharacterSaveData serena = CurrentSave.FindCharacter("CH_SERENA"); // 세레나 진행 조회
            string serenaState = serena == null ? "Missing" : $"Lv={serena.Level}, Exp={serena.Experience}"; // 세레나 상태 생성
            Debug.Log($"[Project H] Save State: Day={CurrentSave.CurrentDay}, Time={CurrentSave.CurrentTime}, Serena={serenaState}"); // 현재 진행 로그
        }

        private bool EnsureInitialized() // 초기화 상태 검증
        {
            if (IsInitialized) // 초기화 완료 확인
            {
                return true; // 사용 가능 반환
            }

            Debug.LogError("[Project H] SaveManager is not initialized."); // 초기화 누락 로그
            return false; // 사용 불가 반환
        }

        private bool ValidateInitialCharacters() // 초기 캐릭터 데이터 검증
        {
            foreach (string characterId in InitialCharacterIds) // 초기 캐릭터 순회
            {
                if (dataManager.GetCharacter(characterId) == null) // 정적 데이터 존재 확인
                {
                    Debug.LogError($"[Project H] Initial character data is missing. ID={characterId}"); // 초기 캐릭터 누락 로그
                    return false; // 검증 실패
                }
            }

            return true; // 검증 성공
        }

        private bool ValidateLoadedSave(SaveData loadedSave) // 불러온 저장 데이터 검증
        {
            if (loadedSave == null) // 저장 데이터 존재 확인
            {
                Debug.LogError("[Project H] Loaded save data is null."); // null 저장 로그
                return false; // 검증 실패
            }

            if (loadedSave.SaveVersion != SaveData.CurrentVersion) // 저장 버전 확인
            {
                Debug.LogError($"[Project H] Unsupported save version. Version={loadedSave.SaveVersion}"); // 버전 오류 로그
                return false; // 검증 실패
            }

            HashSet<string> characterIds = new HashSet<string>(StringComparer.Ordinal); // 중복 검사 집합

            foreach (CharacterSaveData character in loadedSave.Characters) // 저장 캐릭터 순회
            {
                if (character == null || string.IsNullOrWhiteSpace(character.CharacterId)) // 캐릭터 ID 확인
                {
                    Debug.LogError("[Project H] Invalid character save data."); // 캐릭터 저장 오류 로그
                    return false; // 검증 실패
                }

                if (!characterIds.Add(character.CharacterId)) // 중복 캐릭터 확인
                {
                    Debug.LogError($"[Project H] Duplicate character save ID. ID={character.CharacterId}"); // 중복 캐릭터 로그
                    return false; // 검증 실패
                }

                if (dataManager.GetCharacter(character.CharacterId) == null) // 정적 캐릭터 데이터 확인
                {
                    Debug.LogError($"[Project H] Character data not found. ID={character.CharacterId}"); // 정적 데이터 누락 로그
                    return false; // 검증 실패
                }
            }

            return true; // 검증 성공
        }
    }
}
