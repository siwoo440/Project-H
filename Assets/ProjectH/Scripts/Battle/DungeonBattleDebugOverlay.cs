using System; // 문자열 비교 기능
using ProjectH.Core; // 프로젝트 핵심 기능
using ProjectH.Data; // 던전 데이터 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // Unity 씬 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 디버그 오버레이 방지
    public sealed class DungeonBattleDebugOverlay : MonoBehaviour // 던전 전투 테스트 정보 오버레이
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // 첫 씬 로드 전 이벤트 구독 지정
        private static void RegisterSceneLoadedHandler() // 씬 로드 이벤트 구독
        {
            SceneRuntimePatch.Register(GameScenes.Battle, HandleSceneLoaded); // Battle 씬 로드 시 주입 등록 (최적화 — 공통 등록기 사용)
        }

        private static void HandleSceneLoaded(Scene scene) // 씬 로드 완료 처리
        {
            if (!DevelopmentFeatures.Enabled) // 출시 빌드 확인 (최적화)
            {
                return; // 출시 빌드에서는 테스트 정보 오버레이 생성 안 함
            }

            if (UnityEngine.Object.FindFirstObjectByType<DungeonBattleDebugOverlay>() != null) // 기존 오버레이 존재 확인
            {
                return; // 중복 생성 중단
            }

            new GameObject("DungeonBattleDebugOverlayRuntime", typeof(DungeonBattleDebugOverlay)); // 런타임 디버그 오버레이 생성
        }

        private void Start() // 디버그 오버레이 시작
        {
            BuildOverlay(); // 던전 전투 테스트 정보 생성
        }

        private void BuildOverlay() // 던전 전투 테스트 정보 UI 생성
        {
            GameObject canvasObject = new GameObject("DungeonBattleDebugCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler)); // 테스트 전용 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // 오버레이 하위 Canvas 배치
            Canvas canvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 Overlay 렌더링 설정
            canvas.sortingOrder = 950; // 기존 전투 UI 상단 표시 설정
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // CanvasScaler 참조 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 스케일 설정
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 해상도 설정
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응 방식 설정
            scaler.matchWidthOrHeight = 0.5f; // 가로 세로 중간 스케일 적용
            GameObject panelObject = new GameObject("DungeonBattleDebugPanel", typeof(RectTransform), typeof(Image)); // 테스트 정보 배경 생성
            panelObject.transform.SetParent(canvasObject.transform, false); // Canvas 하위 배경 배치
            RectTransform panelRect = panelObject.GetComponent<RectTransform>(); // 배경 RectTransform 조회
            panelRect.anchorMin = new Vector2(0f, 1f); // 좌측 상단 최소 앵커 설정
            panelRect.anchorMax = new Vector2(0f, 1f); // 좌측 상단 최대 앵커 설정
            panelRect.pivot = new Vector2(0f, 1f); // 좌측 상단 피벗 설정
            panelRect.anchoredPosition = new Vector2(18f, -112f); // 좌측 상단 위치 설정 (WAVE/시간/상태 HUD 줄 아래로 이동, 최적화 정리)
            panelRect.sizeDelta = new Vector2(980f, 86f); // 테스트 정보 패널 크기 설정
            Image panelImage = panelObject.GetComponent<Image>(); // 테스트 정보 배경 이미지 조회
            panelImage.color = new Color(0.02f, 0.03f, 0.05f, 0.86f); // 테스트 정보 배경 색상 적용
            GameObject textObject = new GameObject("DungeonBattleDebugText", typeof(RectTransform), typeof(Text)); // 테스트 정보 텍스트 생성
            textObject.transform.SetParent(panelObject.transform, false); // 배경 하위 텍스트 배치
            RectTransform textRect = textObject.GetComponent<RectTransform>(); // 텍스트 RectTransform 조회
            textRect.anchorMin = Vector2.zero; // 텍스트 최소 앵커 설정
            textRect.anchorMax = Vector2.one; // 텍스트 최대 앵커 설정
            textRect.offsetMin = new Vector2(14f, 8f); // 텍스트 최소 여백 설정
            textRect.offsetMax = new Vector2(-14f, -8f); // 텍스트 최대 여백 설정
            Text label = textObject.GetComponent<Text>(); // 테스트 정보 Text 조회
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 적용
            label.fontSize = 18; // 테스트 정보 글자 크기 설정
            label.fontStyle = FontStyle.Bold; // 테스트 정보 굵기 설정
            label.color = Color.white; // 테스트 정보 글자 색상 설정
            label.alignment = TextAnchor.MiddleLeft; // 테스트 정보 좌측 정렬
            label.horizontalOverflow = HorizontalWrapMode.Wrap; // 테스트 정보 가로 줄바꿈 적용
            label.verticalOverflow = VerticalWrapMode.Truncate; // 테스트 정보 세로 초과 제한
            label.raycastTarget = false; // 테스트 정보 입력 대상 제외
            label.text = BuildLabel(); // 전투 컨텍스트 테스트 정보 표시
        }

        private static string BuildLabel() // 전투 컨텍스트 테스트 정보 문구 생성
        {
            string dungeonId = BattleContextRuntimeState.CurrentDungeonId; // 현재 전투 컨텍스트 던전 ID 조회
            DungeonBattleTestProfile profile = DungeonBattleTestProfile.Get(dungeonId); // 현재 던전 테스트 프로필 조회
            DungeonBattleFormationProfile formation = DungeonBattleFormationProfile.Get(dungeonId); // 현재 던전 적 편성 조회
            DungeonData dungeon = null; // 선택 던전 데이터 초기화

            if (GameManager.Instance != null && GameManager.Instance.Data != null && GameManager.Instance.Data.IsInitialized) // 던전 데이터 조회 가능 상태 확인
            {
                dungeon = GameManager.Instance.Data.GetDungeon(dungeonId); // 표시용 던전 데이터 조회
            }

            string sourceLabel = BattleContextRuntimeState.UsedDirectFallback ? $"DIRECT→{dungeonId}" : dungeonId; // 직접 전투 실행 표시 결정
            string dungeonName = dungeon == null ? "던전 데이터 확인 필요" : dungeon.DisplayName; // 표시용 던전 이름 결정
            int rewardGold = dungeon == null ? BattleRewardCalculator.VictoryGold : dungeon.RewardGold; // 표시용 골드 보상 결정
            int rewardExp = dungeon == null ? BattleRewardCalculator.VictoryExperience : dungeon.RewardExp; // 표시용 경험치 보상 결정
            return $"{sourceLabel} · {dungeonName} | ENEMIES {formation.EnemyCount} | HP x{profile.HealthMultiplier:0.00} / ATK x{profile.AttackMultiplier:0.00} / DEF x{profile.DefenseMultiplier:0.00} / RES x{profile.ResistanceMultiplier:0.00} | WIN {rewardGold}G / {rewardExp}EXP"; // 테스트 정보 문구 반환
        }
    }
}
