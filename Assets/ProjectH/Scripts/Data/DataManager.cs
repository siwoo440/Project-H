using System.Collections.Generic; // 목록 자료형
using UnityEngine; // Unity 기본 기능

namespace ProjectH.Data // 프로젝트 데이터 영역
{
    [DisallowMultipleComponent] // 중복 컴포넌트 방지
    public sealed class DataManager : MonoBehaviour // 전체 데이터 관리자
    {
        [SerializeField] private ProjectHDataCatalog catalog; // 데이터 카탈로그

        private readonly DataRegistry<CharacterData> characters = new DataRegistry<CharacterData>(); // 캐릭터 저장소
        private readonly DataRegistry<MonsterData> monsters = new DataRegistry<MonsterData>(); // 몬스터 저장소
        private readonly DataRegistry<DungeonData> dungeons = new DataRegistry<DungeonData>(); // 던전 저장소
        private readonly DataRegistry<ItemData> items = new DataRegistry<ItemData>(); // 아이템 저장소
        private readonly DataRegistry<EquipmentData> equipments = new DataRegistry<EquipmentData>(); // 장비 저장소
        private readonly List<string> validationErrors = new List<string>(); // 검증 오류 목록

        public bool IsInitialized { get; private set; } // 초기화 상태
        public IReadOnlyList<string> ValidationErrors => validationErrors; // 검증 오류 반환
        public int CharacterCount => characters.Count; // 캐릭터 개수 반환
        public int MonsterCount => monsters.Count; // 몬스터 개수 반환
        public int DungeonCount => dungeons.Count; // 던전 개수 반환
        public int ItemCount => items.Count; // 아이템 개수 반환
        public int EquipmentCount => equipments.Count; // 장비 개수 반환

        public void Configure(ProjectHDataCatalog dataCatalog) // 외부 카탈로그 설정
        {
            catalog = dataCatalog; // 카탈로그 교체
            IsInitialized = false; // 재초기화 허용
        }

        public void Initialize() // 전체 데이터 초기화
        {
            if (IsInitialized) // 초기화 여부 확인
            {
                return; // 중복 초기화 중단
            }

            validationErrors.Clear(); // 이전 오류 제거

            if (catalog == null) // 카탈로그 존재 확인
            {
                validationErrors.Add("[Catalog] 데이터 카탈로그가 지정되지 않았습니다."); // 카탈로그 누락 오류
                LogValidationErrors(); // 오류 로그 출력
                return; // 초기화 중단
            }

            characters.Build(catalog.Characters, validationErrors, "Character"); // 캐릭터 저장소 생성
            monsters.Build(catalog.Monsters, validationErrors, "Monster"); // 몬스터 저장소 생성
            dungeons.Build(catalog.Dungeons, validationErrors, "Dungeon"); // 던전 저장소 생성
            items.Build(catalog.Items, validationErrors, "Item"); // 아이템 저장소 생성
            equipments.Build(catalog.Equipments, validationErrors, "Equipment"); // 장비 저장소 생성
            ValidateItemDefinitions(); // 아이템 공통 정책 검증
            ValidateEquipmentLinks(); // 장비 연결 상태 검증
            ValidateDungeonDrops(); // 던전 드롭 테이블 검증

            if (validationErrors.Count > 0) // 검증 오류 확인
            {
                LogValidationErrors(); // 오류 로그 출력
                return; // 초기화 중단
            }

            IsInitialized = true; // 초기화 완료 기록
            Debug.Log($"[Project H] Data initialized. Characters={CharacterCount}, Monsters={MonsterCount}, Dungeons={DungeonCount}, Items={ItemCount}, Equipments={EquipmentCount}"); // 데이터 완료 로그
        }

        public CharacterData GetCharacter(string id) // 캐릭터 데이터 조회
        {
            return characters.GetOrDefault(id); // 캐릭터 조회 결과 반환
        }

        public MonsterData GetMonster(string id) // 몬스터 데이터 조회
        {
            return monsters.GetOrDefault(id); // 몬스터 조회 결과 반환
        }

        public DungeonData GetDungeon(string id) // 던전 데이터 조회
        {
            return dungeons.GetOrDefault(id); // 던전 조회 결과 반환
        }

        public ItemData GetItem(string id) // 아이템 데이터 조회
        {
            return items.GetOrDefault(id); // 아이템 조회 결과 반환
        }

        public EquipmentData GetEquipment(string id) // 장비 데이터 조회
        {
            return equipments.GetOrDefault(id); // 장비 조회 결과 반환
        }

        private void ValidateItemDefinitions() // 아이템 공통 정의 검증
        {
            if (catalog.Items == null) // 아이템 목록 존재 확인
            {
                return; // 공통 목록 오류만 유지
            }

            for (int index = 0; index < catalog.Items.Count; index++) // 아이템 목록 순회
            {
                ItemData item = catalog.Items[index]; // 현재 아이템 조회

                if (item == null) // 아이템 참조 확인
                {
                    continue; // 공통 null 오류만 유지
                }

                ItemDataPolicy.Validate(item, validationErrors); // 아이템 ID 및 수량 정책 검증
            }
        }

        private void ValidateEquipmentLinks() // 장비 원본 연결 검증
        {
            if (catalog.Equipments == null) // 장비 목록 존재 확인
            {
                return; // 공통 목록 오류만 유지
            }

            for (int index = 0; index < catalog.Equipments.Count; index++) // 장비 목록 순회
            {
                EquipmentData equipment = catalog.Equipments[index]; // 현재 장비 조회

                if (equipment == null) // 장비 참조 확인
                {
                    continue; // 공통 null 오류만 유지
                }

                ItemData backingItem = equipment.Item; // 기반 아이템 조회

                if (backingItem == null) // 기반 아이템 존재 확인
                {
                    validationErrors.Add($"[Equipment] ItemData 누락: index={index}"); // 기반 아이템 누락 오류
                    continue; // 다음 장비 이동
                }

                if (backingItem.Type != ItemType.Equipment) // 장비 아이템 유형 확인
                {
                    validationErrors.Add($"[Equipment] 장비가 아닌 ItemData 연결: {equipment.Id}"); // 아이템 유형 오류
                    continue; // 다음 장비 이동
                }

                if (!items.TryGet(equipment.Id, out ItemData registeredItem)) // 기반 아이템 등록 확인
                {
                    validationErrors.Add($"[Equipment] Items 미등록 ItemData 연결: {equipment.Id}"); // 아이템 미등록 오류
                    continue; // 다음 장비 이동
                }

                if (!object.ReferenceEquals(backingItem, registeredItem)) // 등록 인스턴스 일치 확인
                {
                    validationErrors.Add($"[Equipment] 동일 ID의 다른 ItemData 인스턴스 연결: {equipment.Id}"); // 아이템 인스턴스 오류
                }
            }
        }

        private void ValidateDungeonDrops() // 던전 드롭 테이블 검증
        {
            if (catalog.Dungeons == null) // 던전 목록 존재 확인
            {
                return; // 공통 목록 오류만 유지
            }

            for (int dungeonIndex = 0; dungeonIndex < catalog.Dungeons.Count; dungeonIndex++) // 던전 목록 순회
            {
                DungeonData dungeon = catalog.Dungeons[dungeonIndex]; // 현재 던전 조회

                if (dungeon == null || dungeon.DropTable == null) // 던전 및 드롭 테이블 확인
                {
                    continue; // 빈 던전 또는 드롭 테이블 제외
                }

                for (int dropIndex = 0; dropIndex < dungeon.DropTable.Count; dropIndex++) // 현재 던전 드롭 항목 순회
                {
                    DungeonDropEntry entry = dungeon.DropTable[dropIndex]; // 현재 드롭 항목 조회

                    if (entry == null) // 드롭 항목 존재 확인
                    {
                        validationErrors.Add($"[DungeonDrop] null 항목: Dungeon={dungeon.Id}, index={dropIndex}"); // null 드롭 항목 오류
                        continue; // 다음 드롭 항목 이동
                    }

                    if (string.IsNullOrWhiteSpace(entry.ItemId)) // 드롭 아이템 ID 확인
                    {
                        validationErrors.Add($"[DungeonDrop] 빈 ItemId: Dungeon={dungeon.Id}, index={dropIndex}"); // 빈 아이템 ID 오류
                        continue; // 다음 드롭 항목 이동
                    }

                    if (!items.TryGet(entry.ItemId, out _)) // 드롭 아이템 등록 확인
                    {
                        validationErrors.Add($"[DungeonDrop] Items 미등록 ID: Dungeon={dungeon.Id}, Item={entry.ItemId}"); // 미등록 아이템 오류
                    }

                    if (entry.DropChance < 0f || entry.DropChance > 1f) // 드롭 확률 범위 확인
                    {
                        validationErrors.Add($"[DungeonDrop] 잘못된 확률: Dungeon={dungeon.Id}, Item={entry.ItemId}, Chance={entry.DropChance}"); // 드롭 확률 오류
                    }

                    if (entry.MinQuantity < 1 || entry.MaxQuantity < entry.MinQuantity) // 드롭 수량 범위 확인
                    {
                        validationErrors.Add($"[DungeonDrop] 잘못된 수량 범위: Dungeon={dungeon.Id}, Item={entry.ItemId}, Min={entry.MinQuantity}, Max={entry.MaxQuantity}"); // 드롭 수량 오류
                    }
                }
            }
        }

        private void LogValidationErrors() // 검증 오류 출력
        {
            foreach (string error in validationErrors) // 오류 목록 순회
            {
                Debug.LogError($"[Project H] {error}"); // 개별 오류 로그
            }
        }
    }
}
