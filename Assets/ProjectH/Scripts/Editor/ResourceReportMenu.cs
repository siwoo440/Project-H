using System.Collections.Generic; // 목록 자료형
using System.Text; // 문자열 조립 기능
using ProjectH.Core; // 소리 경로 기능
using ProjectH.Diary; // 12인 목록 기능
using ProjectH.Dialogue; // 대사 파일 기능
using ProjectH.UI; // 그림 경로 기능
using UnityEditor; // 에디터 메뉴·에셋 기능
using UnityEngine; // 로그 기능

namespace ProjectH.EditorTools // 프로젝트 에디터 도구 영역
{
    public static class ResourceReportMenu // 리소스 점검 표 (Day73 신규 — 무엇이 들어왔고 무엇이 비었는지 한눈에)
    {
        private const string ResourcesRoot = "Assets/ProjectH/Resources/"; // Resources 폴더 경로

        [MenuItem("Tools/Project H/Phase 2/73일차 리소스 점검 표 출력")] // 리소스 점검 메뉴 등록
        public static void PrintResourceReport() // 그림·소리·폰트가 들어왔는지 확인
        {
            StringBuilder builder = new StringBuilder("[Project H][RESOURCE] 리소스 점검\n"); // 표
            int have = 0; // 들어온 수
            int missing = 0; // 빈 수
            List<string> missingList = new List<string>(); // 빠진 목록

            builder.Append("\n■ 대화 배경 (Resources/Dialogues/Backgrounds)\n"); // 배경

            foreach (string key in CollectBackgroundKeys()) // 대사에서 쓰는 배경 키
            {
                CheckScreenBackground(key, builder, missingList, ref have, ref missing); // 확인 (Day78 — 전용 그림이 없으면 무엇으로 대신하는지 표시)
            }

            builder.Append("\n■ 화면 배경 (상점 · 대장간 · 마을 지도)\n"); // 화면 전용 배경 (Day76 추가)

            foreach (string key in new[] { "SHOP", "BLACKSMITH", "VILLAGE" }) // 대사가 아니라 화면이 직접 쓰는 배경 키
            {
                CheckScreenBackground(key, builder, missingList, ref have, ref missing); // 확인
            }

            builder.Append("\n■ 캐릭터 초상화 (전용 그림 Resources/Portraits · 없으면 스탠딩에서 얼굴을 잘라 사용)\n"); // 초상화 (Day77 — 전용 그림이 없어도 스탠딩이 있으면 채워진다)

            foreach (string characterId in DiaryCatalog.AllCharacters) // 12인
            {
                bool dedicated = Exists(CharacterPortraitArt.PortraitFolder + characterId, ".png"); // 전용 초상화
                bool cropped = !dedicated && StandingFaceCatalog.TryGet(characterId, out _)
                               && (Exists($"{DialogueArtFactory.StandingFolder}{characterId}_{ExpressionCatalog.Normal}", ".png") || Exists(DialogueArtFactory.StandingFolder + characterId, ".png")); // 스탠딩에서 자를 수 있음
                builder.Append(dedicated ? $"  [O] {characterId}\n" : cropped ? $"  [O] {characterId}  (스탠딩에서 잘라 사용)\n" : $"  [ ] {characterId}\n"); // 표시
                if (dedicated || cropped) have++; else { missing++; missingList.Add(CharacterPortraitArt.PortraitFolder + characterId); } // 집계
            }

            builder.Append("\n■ 캐릭터 스탠딩 · 표정 (Resources/Dialogues/Standing/{캐릭터ID}_{표정}.png)\n"); // 스탠딩 (Day76 — 표정 7종까지 표시)

            foreach (string characterId in DiaryCatalog.AllCharacters) // 12인
            {
                builder.Append($"  {characterId,-12}"); // 캐릭터 ID (줄 맞춤)

                foreach (string key in ExpressionCatalog.Keys) // 표정 7종
                {
                    bool exists = Exists($"{DialogueArtFactory.StandingFolder}{characterId}_{key}", ".png")
                                  || (key == ExpressionCatalog.Normal && Exists(DialogueArtFactory.StandingFolder + characterId, ".png")); // 표정 그림 (기본은 표정 없는 그림도 인정)
                    builder.Append(exists ? $" [O]{key}" : $" [ ]{key}"); // 표시
                    if (exists) have++; else { missing++; missingList.Add($"{DialogueArtFactory.StandingFolder}{characterId}_{key}"); } // 집계
                }

                builder.Append('\n'); // 줄 끝
            }

            builder.Append("\n■ 모험 지도 지역 아이콘 (Resources/Map/Regions/{지역ID}.png · 없으면 색 원)\n"); // 지역 아이콘 (Day78 추가)

            foreach (ProjectH.Dungeon.AdventureRegion region in ProjectH.Dungeon.AdventureRegionCatalog.All) // 지역 순회
            {
                Check(AdventureRegionArt.Folder + region.Id, ".png", $"{region.Id}  ({region.Name})", builder, missingList, ref have, ref missing); // 확인
            }

            builder.Append("\n■ 세계 지도 (Resources/Map/WORLD.png · 없으면 코드로 그린 임시 지도)\n"); // 세계 지도 (Day80 추가)
            Check("Map/WORLD", ".png", "WORLD", builder, missingList, ref have, ref missing); // 확인

            builder.Append("\n■ 아이템 · 장비 아이콘 (Resources/Icons/Items/{아이템ID}.png · 없으면 색 원과 글자)\n"); // 아이콘 (Day80 추가)

            foreach (string guid in AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/ProjectH/Data/Items" })) // 아이템 데이터 순회
            {
                ProjectH.Data.ItemData item = AssetDatabase.LoadAssetAtPath<ProjectH.Data.ItemData>(AssetDatabase.GUIDToAssetPath(guid)); // 아이템 원본
                if (item == null) continue; // 데이터 아님
                Check(ItemIconArt.Folder + item.Id, ".png", $"{item.Id}  ({item.DisplayName})", builder, missingList, ref have, ref missing); // 확인
            }

            builder.Append("\n■ UI 스킨 (Resources/UI/Skin/{이름}.png · 없으면 색 상자)\n"); // UI 스킨 (Day81 추가)

            foreach (UiSkinPart part in UiSkin.Parts) // 스킨 조각 순회
            {
                Check(UiSkin.Folder + part.Key, ".png", $"{part.Key}  ({part.Width}×{part.Height} · 테두리 {part.Border} · {part.Usage})", builder, missingList, ref have, ref missing); // 확인
            }

            builder.Append("\n■ UI 아이콘 (Resources/UI/Icons/{이름}.png · 없으면 글자 기호)\n"); // UI 아이콘 (Day81 추가)
            foreach (string key in UiIcon.MenuKeys) Check(UiIcon.Folder + key, ".png", key, builder, missingList, ref have, ref missing); // 메뉴 아이콘
            foreach (string key in UiIcon.NavKeys) Check(UiIcon.Folder + key, ".png", key, builder, missingList, ref have, ref missing); // 내비 아이콘
            foreach (string key in UiIcon.CurrencyKeys) Check(UiIcon.Folder + key, ".png", key, builder, missingList, ref have, ref missing); // 재화 아이콘

            builder.Append("\n■ 룬 · 상태이상 · 속성 아이콘 (Resources/Icons/Runes · Status · Elements · 없으면 색 원과 글자)\n"); // 전투 아이콘 (Day81 추가)

            foreach (ProjectH.SaveSystem.RuneKind kind in System.Enum.GetValues(typeof(ProjectH.SaveSystem.RuneKind))) // 룬 순회
            {
                Check(UiIcon.GetRunePath(kind), ".png", $"룬 {kind}", builder, missingList, ref have, ref missing); // 확인
            }

            foreach (ProjectH.Battle.BattleStatusEffectId id in System.Enum.GetValues(typeof(ProjectH.Battle.BattleStatusEffectId))) // 상태이상 순회
            {
                if (id != ProjectH.Battle.BattleStatusEffectId.None) Check(UiIcon.GetStatusPath(id), ".png", $"상태이상 {id}", builder, missingList, ref have, ref missing); // 확인
            }

            foreach (ProjectH.Battle.BattleElement element in System.Enum.GetValues(typeof(ProjectH.Battle.BattleElement))) // 속성 순회
            {
                if (element != ProjectH.Battle.BattleElement.None) Check(UiIcon.GetElementPath(element), ".png", $"속성 {element}", builder, missingList, ref have, ref missing); // 확인
            }

            builder.Append("\n■ NPC 스탠딩 (Resources/Dialogues/Standing/{NPC ID}_normal.png)\n"); // NPC (Day77 추가)

            foreach (NpcProfile npc in new[] { NpcLineCatalog.Shopkeeper, NpcLineCatalog.Blacksmith, NpcLineCatalog.Shadow, NpcLineCatalog.Archai }) // 상점 주인 · 대장장이 · 그림자 · 아르카이
            {
                Check($"{DialogueArtFactory.StandingFolder}{npc.Id}_{ExpressionCatalog.Normal}", ".png", $"{npc.Id}  ({npc.Title})", builder, missingList, ref have, ref missing); // 확인
            }

            builder.Append("\n■ 전투 SD 그림 · 아군 (Resources/BattleUnits/{캐릭터ID}.png · 없으면 스탠딩 사용)\n"); // 아군 SD (Day78 추가)

            foreach (string characterId in DiaryCatalog.AllCharacters) // 12인
            {
                Check(ProjectH.Battle.BattleUnitArt.Folder + characterId, ".png", characterId, builder, missingList, ref have, ref missing); // 확인
            }

            builder.Append("\n■ 전투 SD 그림 · 몬스터 (Resources/BattleUnits/{몬스터ID}.png · 없으면 색 상자)\n"); // 몬스터 SD (Day78 추가)

            foreach (string guid in AssetDatabase.FindAssets("t:MonsterData", new[] { "Assets/ProjectH/Data/Monsters" })) // 몬스터 데이터 순회
            {
                ProjectH.Data.MonsterData monster = AssetDatabase.LoadAssetAtPath<ProjectH.Data.MonsterData>(AssetDatabase.GUIDToAssetPath(guid)); // 몬스터 원본
                if (monster == null) continue; // 데이터 아님
                Check(ProjectH.Battle.BattleUnitArt.Folder + monster.Id, ".png", $"{monster.Id}  ({monster.DisplayName})", builder, missingList, ref have, ref missing); // 확인
            }

            builder.Append("\n■ 전투 배경 (Resources/Dialogues/Backgrounds/BATTLE_{지역}.png)\n"); // 전투 배경 (Day78 추가)

            foreach (string key in ProjectH.Battle.BattleBackgroundCatalog.Keys) // 지역별 전투 배경 키
            {
                CheckScreenBackground(key, builder, missingList, ref have, ref missing); // 확인 (없으면 무엇으로 대신하는지 표시)
            }

            builder.Append("\n■ 배경음 (Resources/Audio/Bgm)\n"); // 배경음

            foreach (string key in AudioCatalog.AllBgm) // 곡 (Day88 — 카탈로그의 전체 목록)
            {
                Check(AudioCatalog.BgmFolder + key, ".wav", key, builder, missingList, ref have, ref missing); // 확인
            }

            builder.Append("\n■ 효과음 (Resources/Audio/Sfx)\n"); // 효과음

            foreach (string key in AudioCatalog.AllSfx) // 소리 (Day88 — 카탈로그의 전체 목록)
            {
                Check(AudioCatalog.SfxFolder + key, ".wav", key, builder, missingList, ref have, ref missing); // 확인
            }

            builder.Append("\n■ 폰트 (Resources/Fonts)\n"); // 폰트
            bool hasFont = AssetDatabase.LoadAssetAtPath<Font>(ResourcesRoot + RuntimeUiKit.GameFontResourcePath + ".ttf") != null
                           || AssetDatabase.LoadAssetAtPath<Font>(ResourcesRoot + RuntimeUiKit.GameFontResourcePath + ".otf"); // 폰트 확인
            builder.Append(hasFont ? "  [O] GameFont\n" : "  [ ] GameFont  (Resources/Fonts/GameFont.ttf 를 넣으면 전체 글자가 바뀝니다)\n"); // 표시
            if (hasFont) have++; else { missing++; missingList.Add("Fonts/GameFont"); } // 집계

            builder.Append($"\n합계 : 들어옴 {have} · 비어 있음 {missing}\n"); // 합계
            if (missing > 0) builder.Append("비어 있는 항목은 임시 그림·무음으로 대체되어 게임은 정상 동작합니다.\n"); // 안내
            Debug.Log(builder.ToString()); // 출력
        }

        private static bool Exists(string resourcePath, string extension) // Resources 안에 파일이 있는지 (Day76 분리)
        {
            return AssetDatabase.LoadAssetAtPath<Object>(ResourcesRoot + resourcePath + extension) != null; // 존재 확인
        }

        private static void CheckScreenBackground(string key, StringBuilder builder, List<string> missingList, ref int have, ref int missing) // 화면 배경 하나 확인 (Day76 추가 — 전용 그림이 없으면 무엇으로 대신하는지 표시)
        {
            if (Exists($"Dialogues/Backgrounds/{key}", ".png")) // 전용 그림 확인
            {
                builder.Append($"  [O] {key}\n"); // 표시
                have++; // 집계
                return; // 확인 끝
            }

            string alias = DialogueArtFactory.GetBackgroundAlias(key); // 대신 쓸 배경 키
            bool aliasExists = !string.IsNullOrEmpty(alias) && Exists($"Dialogues/Backgrounds/{alias}", ".png"); // 대신 쓸 그림 존재
            builder.Append(aliasExists ? $"  [ ] {key}  (지금은 {alias} 그림을 대신 사용)\n" : $"  [ ] {key}  (코드로 그린 임시 그림 사용)\n"); // 표시
            missing++; // 집계
            missingList.Add($"Dialogues/Backgrounds/{key}"); // 빠진 목록
        }

        private static void Check(string resourcePath, string extension, string label, StringBuilder builder, List<string> missingList, ref int have, ref int missing) // 파일 하나 확인
        {
            bool exists = Exists(resourcePath, extension); // 존재 확인
            builder.Append(exists ? $"  [O] {label}\n" : $"  [ ] {label}\n"); // 표시
            if (exists) have++; else { missing++; missingList.Add(resourcePath); } // 집계
        }

        private static List<string> CollectBackgroundKeys() // 대사 229편이 실제로 쓰는 배경 키 모으기
        {
            List<string> keys = new List<string>(); // 결과
            HashSet<string> seen = new HashSet<string>(); // 중복 검사

            foreach (string guid in AssetDatabase.FindAssets("t:TextAsset", new[] { ResourcesRoot + "Dialogues" })) // 대사 파일 순회
            {
                TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(guid)); // 대사 원본
                DialogueScript script = asset == null ? null : DialogueLibrary.Parse(asset.text); // 해석
                if (script == null || string.IsNullOrEmpty(script.Background) || !seen.Add(script.Background)) continue; // 중복·빈 값 제외
                keys.Add(script.Background); // 추가
            }

            keys.Sort(); // 이름 순
            return keys; // 반환
        }
    }
}
