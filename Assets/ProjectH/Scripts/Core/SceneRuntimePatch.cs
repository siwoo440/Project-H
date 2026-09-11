using System; // 콜백 델리게이트 기능
using System.Collections.Generic; // 목록 자료형
using UnityEngine.SceneManagement; // 씬 로드 이벤트 기능

namespace ProjectH.Core // 프로젝트 핵심 영역
{
    public static class SceneRuntimePatch // 씬 로드 시 Runtime UI 주입 공통 등록기 (최적화 — 각 Runtime Patch의 구독·중복 방지·씬 이름 비교 통합)
    {
        private sealed class Entry // 단일 등록 항목
        {
            public string SceneName; // 대상 씬 이름
            public Action<Scene> Callback; // 씬 로드 콜백
        }

        private static readonly List<Entry> entries = new List<Entry>(); // 전체 등록 목록
        private static bool subscribed; // 씬 로드 이벤트 구독 여부

        public static void Register(string sceneName, Action<Scene> callback) // 지정 씬 로드 시 호출할 콜백 등록 (같은 콜백 중복 등록 무시)
        {
            if (string.IsNullOrWhiteSpace(sceneName) || callback == null) // 입력 확인
            {
                return; // 잘못된 등록 무시
            }

            for (int index = 0; index < entries.Count; index++) // 기존 등록 순회
            {
                if (entries[index].SceneName == sceneName && entries[index].Callback == callback) // 같은 씬·콜백 확인
                {
                    return; // 중복 등록 무시 (도메인 리로드 비활성 Play Mode 대비)
                }
            }

            entries.Add(new Entry { SceneName = sceneName, Callback = callback }); // 등록 추가

            if (!subscribed) // 구독 여부 확인
            {
                SceneManager.sceneLoaded -= Dispatch; // 중복 구독 방지
                SceneManager.sceneLoaded += Dispatch; // 씬 로드 이벤트 단일 구독
                subscribed = true; // 구독 기록
            }
        }

        public static int CountFor(string sceneName) // 지정 씬 등록 콜백 수 조회 (테스트·디버그용)
        {
            int count = 0; // 개수 초기화

            for (int index = 0; index < entries.Count; index++) // 등록 순회
            {
                if (entries[index].SceneName == sceneName) // 씬 일치 확인
                {
                    count++; // 개수 증가
                }
            }

            return count; // 개수 반환
        }

        private static void Dispatch(Scene scene, LoadSceneMode mode) // 로드된 씬의 등록 콜백 실행
        {
            for (int index = 0; index < entries.Count; index++) // 등록 순회 (등록 순서대로 실행)
            {
                Entry entry = entries[index]; // 항목 조회

                if (string.Equals(entry.SceneName, scene.name, StringComparison.Ordinal)) // 씬 이름 일치 확인
                {
                    entry.Callback(scene); // 콜백 실행
                }
            }
        }
    }
}
