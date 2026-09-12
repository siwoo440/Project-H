using System; // 수학 기능
using UnityEngine; // PlayerPrefs·오디오 기능

namespace ProjectH.Core // 프로젝트 핵심 영역
{
    public static class GameSettings // 환경 설정 (Day72 신규 — 저장 파일과 분리되어 회차가 바뀌어도 유지된다)
    {
        public const float MinVolume = 0f; // 최소 음량
        public const float MaxVolume = 1f; // 최대 음량
        public const float VolumeStep = 0.1f; // 음량 조절 단위
        public const float MinDialogueSpeed = 0.5f; // 가장 느린 대사 속도
        public const float MaxDialogueSpeed = 2.5f; // 가장 빠른 대사 속도
        public const float DialogueSpeedStep = 0.25f; // 대사 속도 조절 단위
        public const float MinAutoAdvance = 1f; // 자동 진행 최소 대기 (초)
        public const float MaxAutoAdvance = 5f; // 자동 진행 최대 대기 (초)
        public const float AutoAdvanceStep = 0.5f; // 자동 진행 조절 단위
        public const float MinTextScale = 0.9f; // 가장 작은 글자
        public const float MaxTextScale = 1.5f; // 가장 큰 글자
        public const float TextScaleStep = 0.1f; // 글자 크기 조절 단위
        public const float EasyTimingBonus = 0.35f; // 판정 완화 시 판정 창 확대 비율 (Day72 — 리듬 전투 접근성)

        private const string MasterKey = "ProjectH.Settings.MasterVolume"; // 저장 키
        private const string BgmKey = "ProjectH.Settings.BgmVolume"; // 저장 키
        private const string SfxKey = "ProjectH.Settings.SfxVolume"; // 저장 키
        private const string DialogueSpeedKey = "ProjectH.Settings.DialogueSpeed"; // 저장 키
        private const string AutoAdvanceKey = "ProjectH.Settings.AutoAdvance"; // 저장 키
        private const string TextScaleKey = "ProjectH.Settings.TextScale"; // 저장 키
        private const string ReduceShakeKey = "ProjectH.Settings.ReduceShake"; // 저장 키
        private const string EasyTimingKey = "ProjectH.Settings.EasyTiming"; // 저장 키

        private static bool loaded; // 불러왔는지
        private static float masterVolume = 0.8f; // 전체 음량
        private static float bgmVolume = 0.7f; // 배경음 음량
        private static float sfxVolume = 0.8f; // 효과음 음량
        private static float dialogueSpeed = 1f; // 대사 속도 배수
        private static float autoAdvanceSeconds = 2.5f; // 자동 진행 대기 시간
        private static float textScale = 1f; // 글자 크기 배수
        private static bool reduceShake; // 화면 흔들림 줄이기
        private static bool easyTiming; // 리듬 판정 완화

        public static event Action Changed; // 설정이 바뀔 때 알림 (화면 갱신용)

        public static float MasterVolume => Get(ref masterVolume); // 전체 음량 반환
        public static float BgmVolume => Get(ref bgmVolume); // 배경음 음량 반환
        public static float SfxVolume => Get(ref sfxVolume); // 효과음 음량 반환
        public static float DialogueSpeed => Get(ref dialogueSpeed); // 대사 속도 반환
        public static float AutoAdvanceSeconds => Get(ref autoAdvanceSeconds); // 자동 진행 대기 반환
        public static float TextScale => Get(ref textScale); // 글자 크기 배수 반환
        public static bool ReduceShake { get { EnsureLoaded(); return reduceShake; } } // 흔들림 줄이기 반환
        public static bool EasyTiming { get { EnsureLoaded(); return easyTiming; } } // 판정 완화 반환

        public static float GetTimingWindowScale() => EasyTiming ? 1f + EasyTimingBonus : 1f; // 리듬 판정 창 배수 (Day72 — 전투에서 사용)

        public static float GetShakeScale() => ReduceShake ? 0f : 1f; // 화면 흔들림 배수

        public static int ScaleFontSize(int size) => Mathf.Max(1, Mathf.RoundToInt(size * TextScale)); // 글자 크기 적용

        public static void AddMasterVolume(float delta) => SetMasterVolume(MasterVolume + delta); // 전체 음량 조절
        public static void AddBgmVolume(float delta) => SetBgmVolume(BgmVolume + delta); // 배경음 조절
        public static void AddSfxVolume(float delta) => SetSfxVolume(SfxVolume + delta); // 효과음 조절
        public static void AddDialogueSpeed(float delta) => SetDialogueSpeed(DialogueSpeed + delta); // 대사 속도 조절
        public static void AddAutoAdvance(float delta) => SetAutoAdvance(AutoAdvanceSeconds + delta); // 자동 진행 조절
        public static void AddTextScale(float delta) => SetTextScale(TextScale + delta); // 글자 크기 조절

        public static void SetMasterVolume(float value) // 전체 음량 저장
        {
            masterVolume = Round(Mathf.Clamp(value, MinVolume, MaxVolume)); // 범위 보정
            PlayerPrefs.SetFloat(MasterKey, masterVolume); // 기록
            ApplyAudio(); // 반영
            Commit(); // 저장·알림
        }

        public static void SetBgmVolume(float value) // 배경음 음량 저장
        {
            bgmVolume = Round(Mathf.Clamp(value, MinVolume, MaxVolume)); // 범위 보정
            PlayerPrefs.SetFloat(BgmKey, bgmVolume); // 기록
            Commit(); // 저장·알림
        }

        public static void SetSfxVolume(float value) // 효과음 음량 저장
        {
            sfxVolume = Round(Mathf.Clamp(value, MinVolume, MaxVolume)); // 범위 보정
            PlayerPrefs.SetFloat(SfxKey, sfxVolume); // 기록
            Commit(); // 저장·알림
        }

        public static void SetDialogueSpeed(float value) // 대사 속도 저장
        {
            dialogueSpeed = Round(Mathf.Clamp(value, MinDialogueSpeed, MaxDialogueSpeed)); // 범위 보정
            PlayerPrefs.SetFloat(DialogueSpeedKey, dialogueSpeed); // 기록
            Commit(); // 저장·알림
        }

        public static void SetAutoAdvance(float value) // 자동 진행 대기 저장
        {
            autoAdvanceSeconds = Round(Mathf.Clamp(value, MinAutoAdvance, MaxAutoAdvance)); // 범위 보정
            PlayerPrefs.SetFloat(AutoAdvanceKey, autoAdvanceSeconds); // 기록
            Commit(); // 저장·알림
        }

        public static void SetTextScale(float value) // 글자 크기 저장
        {
            textScale = Round(Mathf.Clamp(value, MinTextScale, MaxTextScale)); // 범위 보정
            PlayerPrefs.SetFloat(TextScaleKey, textScale); // 기록
            Commit(); // 저장·알림
        }

        public static void SetReduceShake(bool value) // 흔들림 줄이기 저장
        {
            EnsureLoaded(); // 불러오기 보장
            reduceShake = value; // 값 저장
            PlayerPrefs.SetInt(ReduceShakeKey, value ? 1 : 0); // 기록
            Commit(); // 저장·알림
        }

        public static void SetEasyTiming(bool value) // 판정 완화 저장
        {
            EnsureLoaded(); // 불러오기 보장
            easyTiming = value; // 값 저장
            PlayerPrefs.SetInt(EasyTimingKey, value ? 1 : 0); // 기록
            Commit(); // 저장·알림
        }

        public static void ResetToDefault() // 기본값으로 되돌리기
        {
            EnsureLoaded(); // 불러오기 보장
            SetMasterVolume(0.8f); // 전체 음량
            SetBgmVolume(0.7f); // 배경음
            SetSfxVolume(0.8f); // 효과음
            SetDialogueSpeed(1f); // 대사 속도
            SetAutoAdvance(2.5f); // 자동 진행
            SetTextScale(1f); // 글자 크기
            SetReduceShake(false); // 흔들림
            SetEasyTiming(false); // 판정 완화
        }

        private static float Get(ref float field) // 값 조회 (처음 부를 때 불러오기)
        {
            EnsureLoaded(); // 불러오기 보장
            return field; // 값 반환
        }

        private static void EnsureLoaded() // 저장된 설정 불러오기 (한 번만)
        {
            if (loaded) return; // 이미 불러옴
            loaded = true; // 표시 (아래 읽기에서 다시 들어오지 않도록 먼저)
            masterVolume = PlayerPrefs.GetFloat(MasterKey, masterVolume); // 전체 음량
            bgmVolume = PlayerPrefs.GetFloat(BgmKey, bgmVolume); // 배경음
            sfxVolume = PlayerPrefs.GetFloat(SfxKey, sfxVolume); // 효과음
            dialogueSpeed = PlayerPrefs.GetFloat(DialogueSpeedKey, dialogueSpeed); // 대사 속도
            autoAdvanceSeconds = PlayerPrefs.GetFloat(AutoAdvanceKey, autoAdvanceSeconds); // 자동 진행
            textScale = PlayerPrefs.GetFloat(TextScaleKey, textScale); // 글자 크기
            reduceShake = PlayerPrefs.GetInt(ReduceShakeKey, 0) != 0; // 흔들림
            easyTiming = PlayerPrefs.GetInt(EasyTimingKey, 0) != 0; // 판정 완화
            ApplyAudio(); // 음량 반영
        }

        private static void ApplyAudio() // 전체 음량을 실제 소리에 반영
        {
            AudioListener.volume = Mathf.Clamp(masterVolume, MinVolume, MaxVolume); // 전체 음량
        }

        private static void Commit() // 파일에 쓰고 변경 알림
        {
            PlayerPrefs.Save(); // 기록 저장
            Changed?.Invoke(); // 알림
        }

        private static float Round(float value) => Mathf.Round(value * 100f) / 100f; // 소수점 둘째 자리까지 (0.7000001 방지)
    }
}
