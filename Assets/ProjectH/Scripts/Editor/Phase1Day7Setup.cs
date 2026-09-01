using System; // 시스템 자료형
using System.Collections.Generic; // 목록 자료형
using ProjectH.Data; // 프로젝트 데이터 기능
using UnityEditor; // Unity 에디터 기능
using UnityEngine; // Unity 기본 기능

namespace ProjectH.EditorTools // 프로젝트 에디터 도구 영역
{
    public static class Phase1Day7Setup // 7일차 캐릭터 데이터 도구
    {
        private const string CharacterRoot = "Assets/ProjectH/Data/Characters"; // 캐릭터 데이터 경로
        private const string CatalogPath = "Assets/ProjectH/Data/Database/ProjectHDataCatalog.asset"; // 카탈로그 경로

        private sealed class CharacterSpec // 캐릭터 초기 데이터 명세
        {
            public string Id { get; } // 캐릭터 ID
            public string DisplayName { get; } // 캐릭터 이름
            public CharacterJob Job { get; } // 캐릭터 직군
            public BattlePosition Position { get; } // 캐릭터 포지션
            public int Hp { get; } // 캐릭터 체력
            public int Attack { get; } // 캐릭터 공격력
            public int Defense { get; } // 캐릭터 방어력
            public float AttackSpeed { get; } // 캐릭터 공격속도
            public float Accuracy { get; } // 캐릭터 명중률

            public CharacterSpec(string id, string displayName, CharacterJob job, BattlePosition position, int hp, int attack, int defense, float attackSpeed, float accuracy) // 명세 생성
            {
                Id = id; // 캐릭터 ID 저장
                DisplayName = displayName; // 캐릭터 이름 저장
                Job = job; // 캐릭터 직군 저장
                Position = position; // 캐릭터 포지션 저장
                Hp = hp; // 캐릭터 체력 저장
                Attack = attack; // 캐릭터 공격력 저장
                Defense = defense; // 캐릭터 방어력 저장
                AttackSpeed = attackSpeed; // 캐릭터 공격속도 저장
                Accuracy = accuracy; // 캐릭터 명중률 저장
            }
        }

        private static readonly CharacterSpec[] Specs = // 12인 초기 명세
        {
            new CharacterSpec("CH_SERENA", "세레나", CharacterJob.Cleric, BattlePosition.Healer, 2200, 180, 120, 0.90f, 0.98f), // 세레나 명세
            new CharacterSpec("CH_ELLEN", "엘렌", CharacterJob.Knight, BattlePosition.Tank, 3200, 230, 200, 0.85f, 0.96f), // 엘렌 명세
            new CharacterSpec("CH_LILIA", "릴리아", CharacterJob.Mage, BattlePosition.Dealer, 1700, 300, 100, 1.00f, 0.92f), // 릴리아 명세
            new CharacterSpec("CH_NATASHA", "나타샤", CharacterJob.Rogue, BattlePosition.Dealer, 1600, 310, 90, 1.30f, 0.95f), // 나타샤 명세
            new CharacterSpec("CH_EVE", "이브", CharacterJob.Archer, BattlePosition.Dealer, 1750, 270, 105, 1.25f, 0.94f), // 이브 명세
            new CharacterSpec("CH_CLAIRE", "클레어", CharacterJob.Alchemist, BattlePosition.Healer, 2400, 210, 130, 0.95f, 0.96f), // 클레어 명세
            new CharacterSpec("CH_LUCIA", "루시아", CharacterJob.Gunner, BattlePosition.Dealer, 2000, 285, 120, 1.15f, 0.97f), // 루시아 명세
            new CharacterSpec("CH_PYRA", "파이라", CharacterJob.Lancer, BattlePosition.Dealer, 2800, 320, 150, 1.05f, 0.93f), // 파이라 명세
            new CharacterSpec("CH_TYRIA", "티리아", CharacterJob.Guardian, BattlePosition.Tank, 3500, 200, 220, 0.80f, 0.95f), // 티리아 명세
            new CharacterSpec("CH_MERCIA", "메르시아", CharacterJob.Monk, BattlePosition.Dealer, 2100, 250, 130, 1.10f, 0.94f), // 메르시아 명세
            new CharacterSpec("CH_NOEL", "노엘", CharacterJob.Explorer, BattlePosition.Dealer, 1850, 240, 110, 1.15f, 0.93f), // 노엘 명세
            new CharacterSpec("CH_SEPHIRA", "세피라", CharacterJob.Pilgrim, BattlePosition.Healer, 2300, 260, 150, 0.95f, 0.97f) // 세피라 명세
        }; // 12인 초기 명세 종료

        [MenuItem("Tools/Project H/Phase 1/7일차 12인 캐릭터 데이터 재구성")] // 캐릭터 재구성 메뉴
        public static void RebuildCharacters() // 12인 캐릭터 데이터 재구성
        {
            List<CharacterData> characters = new List<CharacterData>(); // 카탈로그 캐릭터 목록 생성

            foreach (CharacterSpec spec in Specs) // 캐릭터 명세 순회
            {
                CharacterData character = CreateOrUpdateCharacter(spec); // 캐릭터 에셋 갱신
                characters.Add(character); // 카탈로그 목록 추가
            }

            UpdateCatalog(characters); // 데이터 카탈로그 갱신
            Validate(characters); // 캐릭터 데이터 검증
            AssetDatabase.SaveAssets(); // 에셋 변경 저장
            AssetDatabase.Refresh(); // 에셋 목록 갱신
            Debug.Log("[Project H][DATA] Phase 1 Day 7 character rebuild complete. Characters=12"); // 재구성 완료 로그
        }

        private static CharacterData CreateOrUpdateCharacter(CharacterSpec spec) // 캐릭터 에셋 생성 또는 갱신
        {
            string path = $"{CharacterRoot}/{spec.Id}.asset"; // 캐릭터 에셋 경로 생성
            CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>(path); // 기존 캐릭터 로드

            if (character == null) // 기존 캐릭터 확인
            {
                character = ScriptableObject.CreateInstance<CharacterData>(); // 캐릭터 인스턴스 생성
                AssetDatabase.CreateAsset(character, path); // 캐릭터 에셋 생성
            }

            SerializedObject serialized = new SerializedObject(character); // 캐릭터 직렬화 객체 생성
            serialized.FindProperty("id").stringValue = spec.Id; // 캐릭터 ID 설정
            serialized.FindProperty("displayName").stringValue = spec.DisplayName; // 캐릭터 이름 설정
            serialized.FindProperty("job").enumValueIndex = (int)spec.Job; // 캐릭터 직군 설정
            serialized.FindProperty("position").enumValueIndex = (int)spec.Position; // 캐릭터 포지션 설정
            serialized.FindProperty("baseHp").intValue = spec.Hp; // 캐릭터 체력 설정
            serialized.FindProperty("baseAttack").intValue = spec.Attack; // 캐릭터 공격력 설정
            serialized.FindProperty("baseDefense").intValue = spec.Defense; // 캐릭터 방어력 설정
            serialized.FindProperty("attackSpeed").floatValue = spec.AttackSpeed; // 캐릭터 공격속도 설정
            serialized.FindProperty("accuracy").floatValue = spec.Accuracy; // 캐릭터 명중률 설정
            serialized.FindProperty("baseMagic").intValue = 0; // 임시 마력 초기화
            serialized.FindProperty("baseResistance").intValue = 0; // 임시 저항력 초기화
            serialized.FindProperty("criticalRate").floatValue = 0.05f; // 임시 치명타율 설정
            serialized.ApplyModifiedPropertiesWithoutUndo(); // 캐릭터 값 적용
            character.name = spec.Id; // 에셋 이름 동기화
            EditorUtility.SetDirty(character); // 캐릭터 변경 표시
            return character; // 캐릭터 에셋 반환
        }

        private static void UpdateCatalog(List<CharacterData> characters) // 데이터 카탈로그 갱신
        {
            ProjectHDataCatalog catalog = AssetDatabase.LoadAssetAtPath<ProjectHDataCatalog>(CatalogPath); // 데이터 카탈로그 로드

            if (catalog == null) // 카탈로그 존재 확인
            {
                Debug.LogError("[Project H][DATA] ProjectHDataCatalog is missing."); // 카탈로그 누락 로그
                return; // 카탈로그 갱신 중단
            }

            SerializedObject serialized = new SerializedObject(catalog); // 카탈로그 직렬화 객체 생성
            SerializedProperty characterProperty = serialized.FindProperty("characters"); // 캐릭터 배열 조회
            characterProperty.arraySize = characters.Count; // 캐릭터 배열 크기 설정

            for (int index = 0; index < characters.Count; index++) // 캐릭터 목록 순회
            {
                characterProperty.GetArrayElementAtIndex(index).objectReferenceValue = characters[index]; // 캐릭터 참조 등록
            }

            serialized.ApplyModifiedPropertiesWithoutUndo(); // 카탈로그 값 적용
            EditorUtility.SetDirty(catalog); // 카탈로그 변경 표시
        }

        private static void Validate(List<CharacterData> characters) // 12인 캐릭터 데이터 검증
        {
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal); // ID 검증 집합
            int errorCount = 0; // 오류 개수 초기화

            if (characters.Count != 12) // 캐릭터 수 확인
            {
                Debug.LogError($"[Project H][DATA] Expected 12 characters, actual={characters.Count}."); // 캐릭터 수 오류 로그
                errorCount++; // 오류 개수 증가
            }

            foreach (CharacterData character in characters) // 캐릭터 목록 순회
            {
                if (character == null) // 캐릭터 존재 확인
                {
                    Debug.LogError("[Project H][DATA] Character asset is null."); // 캐릭터 null 로그
                    errorCount++; // 오류 개수 증가
                    continue; // 다음 캐릭터 이동
                }

                if (!ids.Add(character.Id)) // 캐릭터 ID 고유성 확인
                {
                    Debug.LogError($"[Project H][DATA] Duplicate character ID: {character.Id}"); // 중복 ID 로그
                    errorCount++; // 오류 개수 증가
                }

                if (character.BaseHp <= 0 || character.AttackSpeed <= 0f) // 기본 수치 확인
                {
                    Debug.LogError($"[Project H][DATA] Invalid base stats: {character.Id}"); // 기본 수치 오류 로그
                    errorCount++; // 오류 개수 증가
                }

                if (character.Accuracy < 0f || character.Accuracy > 1f) // 명중률 범위 확인
                {
                    Debug.LogError($"[Project H][DATA] Invalid accuracy: {character.Id}"); // 명중률 오류 로그
                    errorCount++; // 오류 개수 증가
                }
            }

            if (errorCount == 0) // 오류 개수 확인
            {
                Debug.Log("[Project H][DATA] Character validation passed. Tank=2, Dealer=7, Healer=3."); // 검증 성공 로그
            }
        }
    }
}
