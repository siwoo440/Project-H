using System.Collections.Generic; // 사전 자료형
using UnityEngine; // 오디오 기능
using UnityEngine.SceneManagement; // 씬 이벤트 기능

namespace ProjectH.Core // 프로젝트 핵심 영역
{
    [DisallowMultipleComponent] // 중복 방지
    public sealed class AudioService : MonoBehaviour // 배경음·효과음 재생기 (Day73 신규 — 음량은 72일차 설정을 따른다)
    {
        private const string ObjectName = "AudioService"; // 재생기 오브젝트 이름
        private const int SfxChannelCount = 6; // 동시에 겹쳐 낼 수 있는 효과음 채널 수 (Day88 — 전투에서 소리가 늘어 4 → 6)

        private static AudioService instance; // 단일 재생기
        private static readonly Dictionary<string, AudioClip> ClipCache = new Dictionary<string, AudioClip>(); // 불러온 소리 캐시

        private readonly BgmFader fader = new BgmFader(); // 배경음 전환 (Day88 — 뚝 끊기지 않고 줄였다가 바꾼다)
        private readonly SfxThrottle throttle = new SfxThrottle(); // 같은 효과음 겹침 방지 (Day88)
        private AudioSource bgmSource; // 배경음 채널
        private AudioSource[] sfxSources; // 효과음 채널
        private int sfxCursor; // 다음에 쓸 효과음 채널

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // 첫 씬 로드 후 시작
        private static void Create() // 재생기 만들기 (씬이 바뀌어도 유지)
        {
            if (instance != null) return; // 이미 있음
            GameObject root = new GameObject(ObjectName, typeof(AudioService)); // 오브젝트 생성
            DontDestroyOnLoad(root); // 유지
        }

        public static void PlayBgm(string key) // 배경음 바꾸기 (같은 곡이면 이어서 재생, 빈 값이면 끄기)
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

        private void Update() // 배경음 전환 진행 (전환 중이 아니면 할 일이 없다)
        {
            if (!fader.IsBusy) return; // 전환 중 아님
            if (fader.Tick(Time.unscaledDeltaTime)) SwapBgmClip(); // 다 줄였으면 곡 교체 (전투가 멈춰 있어도 진행되도록 실제 시간)
            ApplyVolume(); // 줄이고 키우는 음량 반영
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) // 씬이 바뀌면 배경음 교체
        {
            PlayBgmInternal(AudioCatalog.GetSceneBgm(scene.name)); // 씬 배경음
        }

        private void PlayBgmInternal(string key) // 배경음 바꾸기 요청 (같은 곡이면 그대로 둔다)
        {
            fader.Request(ResolveBgm(key)); // 파일이 있는 곡으로 요청 (실제 교체는 Update에서 줄인 뒤에)
        }

        private static string ResolveBgm(string key) // 파일이 있는 곡 찾기 (Day88 — 그 곡이 없으면 대신할 곡, 그것도 없으면 무음)
        {
            string current = key ?? string.Empty; // 찾을 곡

            for (int guard = 0; guard < 4 && current.Length > 0; guard++) // 대신할 곡을 따라감 (돌고 도는 연결 방지)
            {
                if (Load(AudioCatalog.BgmFolder + current) != null) return current; // 파일 있음
                current = AudioCatalog.GetBgmFallback(current); // 대신할 곡
            }

            return string.Empty; // 틀 곡 없음
        }

        private void SwapBgmClip() // 채널의 곡을 바꿔 끼움
        {
            AudioClip clip = string.IsNullOrEmpty(fader.Current) ? null : Load(AudioCatalog.BgmFolder + fader.Current); // 새 곡

            if (clip == null) // 끄기 · 파일 없음
            {
                bgmSource.Stop(); // 정지 (소리 없이 진행)
                bgmSource.clip = null; // 곡 비움
                return; // 종료
            }

            bgmSource.clip = clip; // 곡 지정
            bgmSource.Play(); // 재생 (음량은 호출 측이 이어서 반영)
        }

        private void PlaySfxInternal(string key) // 효과음 재생 (채널을 돌려 가며 겹쳐 낸다)
        {
            if (string.IsNullOrEmpty(key) || sfxSources == null) return; // 입력 확인
            if (!throttle.TryPass(key, Time.unscaledTime, AudioCatalog.GetSfxInterval(key))) return; // 방금 난 같은 소리 (Day88 — 겹쳐서 시끄러워지지 않게)
            AudioClip clip = Load(AudioCatalog.SfxFolder + key); // 소리 불러오기
            if (clip == null) return; // 파일 없음
            AudioSource source = sfxSources[sfxCursor]; // 채널 선택
            sfxCursor = (sfxCursor + 1) % sfxSources.Length; // 다음 채널
            source.volume = GameSettings.SfxVolume; // 설정 음량
            source.PlayOneShot(clip, AudioCatalog.GetSfxVolume(key)); // 재생 (소리별 음량 배율, Day88)
        }

        private void ApplyVolume() // 설정 음량을 채널에 반영
        {
            if (bgmSource != null) bgmSource.volume = GameSettings.BgmVolume * fader.Gain; // 배경음 (전환 중에는 줄어든 음량, Day88)
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
