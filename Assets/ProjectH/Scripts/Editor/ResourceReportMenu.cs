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
                Check($"Dialogues/Backgrounds/{key}", ".png", key, builder, missingList, ref have, ref missing); // 확인
            }

            builder.Append("\n■ 캐릭터 초상화 (Resources/Portraits)\n"); // 초상화

            foreach (string characterId in DiaryCatalog.AllCharacters) // 12인
            {
                Check(CharacterPortraitArt.PortraitFolder + characterId, ".png", characterId, builder, missingList, ref have, ref missing); // 확인
            }

            builder.Append("\n■ 캐릭터 스탠딩 (Resources/Dialogues/Standing)\n"); // 스탠딩

            foreach (string characterId in DiaryCatalog.AllCharacters) // 12인
            {
                Check($"Dialogues/Standing/{characterId}", ".png", characterId, builder, missingList, ref have, ref missing); // 확인
            }

            builder.Append("\n■ 배경음 (Resources/Audio/Bgm)\n"); // 배경음

            foreach (string key in new[] { AudioCatalog.BgmTitle, AudioCatalog.BgmVillage, AudioCatalog.BgmBattle }) // 곡
            {
                Check(AudioCatalog.BgmFolder + key, ".wav", key, builder, missingList, ref have, ref missing); // 확인
            }

            builder.Append("\n■ 효과음 (Resources/Audio/Sfx)\n"); // 효과음

            foreach (string key in new[] { AudioCatalog.SfxClick, AudioCatalog.SfxConfirm, AudioCatalog.SfxCancel, AudioCatalog.SfxPage, AudioCatalog.SfxGold, AudioCatalog.SfxItem, AudioCatalog.SfxHit, AudioCatalog.SfxPerfect, AudioCatalog.SfxLevelUp }) // 소리
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

        private static void Check(string resourcePath, string extension, string label, StringBuilder builder, List<string> missingList, ref int have, ref int missing) // 파일 하나 확인
        {
            bool exists = AssetDatabase.LoadAssetAtPath<Object>(ResourcesRoot + resourcePath + extension) != null; // 존재 확인
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
