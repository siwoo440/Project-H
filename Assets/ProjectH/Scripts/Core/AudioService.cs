using System.Collections.Generic; // 사전 자료형
using UnityEngine; // 오디오 기능
using UnityEngine.SceneManagement; // 씬 이벤트 기능

namespace ProjectH.Core // 프로젝트 핵심 영역
{
    [DisallowMultipleComponent] // 중복 방지
    public sealed class AudioService : MonoBehaviour // 배경음·효과음 재생기 (Day73 신규 — 음량은 72일차 설정을 따른다)
    {
        private const string ObjectName = "AudioService"; // 재생기 오브젝트 이름
        private const int SfxChannelCount = 4; // 동시에 겹쳐 낼 수 있는 효과음 수

        private static AudioService instance; // 단일 재생기
        private static readonly Dictionary<string, AudioClip> ClipCache = new Dictionary<string, AudioClip>(); // 불러온 소리 캐시

        private AudioSource bgmSource; // 배경음 채널
        private AudioSource[] sfxSources; // 효과음 채널
        private int sfxCursor; // 다음에 쓸 효과음 채널
        private string currentBgm = string.Empty; // 재생 중인 배경음

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // 첫 씬 로드 후 시작
        private static void Create() // 재생기 만들기 (씬이 바뀌어도 유지)
        {
            if (instance != null) return; // 이미 있음
            GameObject root = new GameObject(ObjectName, typeof(AudioService)); // 오브젝트 생성
            DontDestroyOnLoad(root); // 유지
        }

        public static void PlayBgm(string key) // 배경음 바꾸기 (같은 곡이면 이어서 재생)
        {
            if (instance != null) instance.PlayBgmInternal(key); // 재생
        }

        public static void PlaySfx(string key) // 효과음 한 번 재생
        {
            if (instance != null) instance.PlaySfxInternal(key); // 재생
        }

        private void Awake() // 채널 준비
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; } // 중복 제거
            instance = this; // 단일 재생기 등록
            bgmSource = gameObject.AddComponent<AudioSource>(); // 배경음 채널
            bgmSource.loop = true; // 반복
            bgmSource.playOnAwake = false; // 자동 재생 안 함
            sfxSources = new AudioSource[SfxChannelCount]; // 효과음 채널

            for (int index = 0; index < SfxChannelCount; index++) // 채널 생성
            {
                sfxSources[index] = gameObject.AddComponent<AudioSource>(); // 채널 추가
                sfxSources[index].playOnAwake = false; // 자동 재생 안 함
            }

            ApplyVolume(); // 설정 음량 반영
            GameSettings.Changed += ApplyVolume; // 설정이 바뀌면 즉시 반영
            SceneManager.sceneLoaded += OnSceneLoaded; // 씬에 맞는 배경음
            PlayBgmInternal(AudioCatalog.GetSceneBgm(SceneManager.GetActiveScene().name)); // 현재 씬 배경음
        }

        private void OnDestroy() // 정리
        {
            GameSettings.Changed -= ApplyVolume; // 구독 해제
            SceneManager.sceneLoaded -= OnSceneLoaded; // 구독 해제
            if (instance == this) instance = null; // 등록 해제
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) // 씬이 바뀌면 배경음 교체
        {
            PlayBgmInternal(AudioCatalog.GetSceneBgm(scene.name)); // 씬 배경음
        }

        private void PlayBgmInternal(string key) // 배경음 재생 (같은 곡이면 그대로 둔다)
        {
            if (currentBgm == key && bgmSource.isPlaying) return; // 이미 재생 중
            currentBgm = key ?? string.Empty; // 곡 기록

            if (string.IsNullOrEmpty(currentBgm)) // 끄기
            {
                bgmSource.Stop(); // 정지
                return; // 종료
            }

            AudioClip clip = Load(AudioCatalog.BgmFolder + currentBgm); // 곡 불러오기

            if (clip == null) // 파일 없음
            {
                bgmSource.Stop(); // 정지 (소리 없이 진행)
                return; // 종료
            }

            bgmSource.clip = clip; // 곡 지정
            ApplyVolume(); // 음량 반영
            bgmSource.Play(); // 재생
        }

        private void PlaySfxInternal(string key) // 효과음 재생 (채널을 돌려 가며 겹쳐 낸다)
        {
            if (string.IsNullOrEmpty(key) || sfxSources == null) return; // 입력 확인
            AudioClip clip = Load(AudioCatalog.SfxFolder + key); // 소리 불러오기
            if (clip == null) return; // 파일 없음
            AudioSource source = sfxSources[sfxCursor]; // 채널 선택
            sfxCursor = (sfxCursor + 1) % sfxSources.Length; // 다음 채널
            source.volume = GameSettings.SfxVolume; // 설정 음량
            source.PlayOneShot(clip); // 재생
        }

        private void ApplyVolume() // 설정 음량을 채널에 반영
        {
            if (bgmSource != null) bgmSource.volume = GameSettings.BgmVolume; // 배경음
            if (sfxSources == null) return; // 채널 확인

            foreach (AudioSource source in sfxSources) // 채널 순회
            {
                if (source != null) source.volume = GameSettings.SfxVolume; // 효과음
            }
        }

        private static AudioClip Load(string path) // Resources에서 소리 불러오기 (캐시)
        {
            if (ClipCache.TryGetValue(path, out AudioClip cached) && cached != null) return cached; // 캐시
            AudioClip clip = Resources.Load<AudioClip>(path); // 파일
            ClipCache[path] = clip; // 캐시 저장 (없으면 null도 기억해 매번 찾지 않는다)
            return clip; // 반환
        }
    }
}
