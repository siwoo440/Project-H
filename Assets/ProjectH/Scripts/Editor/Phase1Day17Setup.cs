using ProjectH.Battle; // 전투 공통 기능
using ProjectH.Battle.SkillBlock; // 스킬 블록 전투 기능
using ProjectH.Data; // 캐릭터 및 스킬 데이터 기능
using UnityEditor; // Unity 에디터 기능
using UnityEditor.SceneManagement; // 에디터 Scene 관리 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // Unity Scene 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.EditorTools // 프로젝트 에디터 도구 영역
{
    public static class Phase1Day17Setup // 17일차 스킬 블록 시스템 설정 도구
    {
        private const string BattleScenePath = "Assets/ProjectH/Scenes/Battle.unity"; // 전투 Scene 경로
        private const string CharacterDataFolder = "Assets/ProjectH/Data/Characters"; // 캐릭터 데이터 폴더
        private const string SkillDataFolder = "Assets/ProjectH/Data/Skills"; // 스킬 데이터 폴더
        private const int MaxBlockCount = 7; // 프로토타입 최대 스킬 블록 수
        private const float GenerationInterval = 1.5f; // 프로토타입 블록 생성 주기
        private static readonly Color PanelColor = new Color(0.04f, 0.06f, 0.10f, 0.90f); // 스킬 블록 패널 배경색
        private static readonly Color SlotColor = new Color(0.90f, 0.88f, 0.80f, 0.24f); // 빈 블록 슬롯 배경색
        private static readonly Color TextColor = new Color(0.96f, 0.96f, 0.92f, 1f); // 스킬 블록 UI 글자색

        [MenuItem("Tools/Project H/Phase 1/17일차 스킬 블록 시스템 설정 실행")] // 17일차 설정 메뉴 등록
        public static void Setup() // 스킬 데이터 및 Battle Scene 블록 UI 자동 설정
        {
            EnsureSkillDataFolder(); // 스킬 데이터 폴더 확보
            CreateAndAssignCharacterSkills(); // 전체 캐릭터 Skill 1·2·3 데이터 생성 및 연결
            Scene scene = EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Single); // 현재 Battle Scene 열기
            Transform controllerTransform = FindRequiredTransform(scene, "BattleController"); // BattleController 조회
            BattleCombatRegistry registry = EnsureComponent<BattleCombatRegistry>(controllerTransform.gameObject); // 기존 전투 Registry 확보
            Text battleStatus = FindRequiredComponent<Text>(scene, "BattleStatus"); // 기존 BattleStatus 텍스트 조회
            Canvas canvas = battleStatus.GetComponentInParent<Canvas>(); // Battle HUD Canvas 조회

            if (canvas == null) // Battle HUD Canvas 존재 확인
            {
                throw new System.InvalidOperationException("Battle HUD Canvas를 찾을 수 없습니다."); // Battle HUD Canvas 누락 예외 발생
            }

            BattleSkillBlockPanel panel = EnsureSkillBlockPanel(canvas.transform, out RectTransform blockLayer, out Text counterText, out Text hintText); // 스킬 블록 패널 UI 확보
            panel.Configure(blockLayer, counterText, hintText, MaxBlockCount); // 스킬 블록 패널 참조 연결
            RepositionProfileCards(scene); // 기존 4인 프로필 카드를 왼쪽 하단에 축소 배치
            HideLegacyCardSkillButtons(scene); // 16일차 카드별 SKILL LOCKED 버튼 숨김
            BattleSkillExecutor executor = EnsureComponent<BattleSkillExecutor>(controllerTransform.gameObject); // 스킬 사용 요청 실행기 확보
            executor.Configure(registry); // 스킬 실행기 Registry 연결
            BattleSkillBlockController blockController = EnsureComponent<BattleSkillBlockController>(controllerTransform.gameObject); // 스킬 블록 Runtime 컨트롤러 확보
            blockController.Configure(registry, panel, executor, MaxBlockCount, GenerationInterval); // 블록 생성·드래그·강화도·소비 설정 연결
            EditorUtility.SetDirty(panel); // 스킬 블록 패널 변경 표시
            EditorUtility.SetDirty(executor); // 스킬 실행기 변경 표시
            EditorUtility.SetDirty(blockController); // 스킬 블록 컨트롤러 변경 표시
            EditorSceneManager.MarkSceneDirty(scene); // Battle Scene 변경 표시
            EditorSceneManager.SaveScene(scene, BattleScenePath); // Battle Scene 변경 저장
            AssetDatabase.SaveAssets(); // 생성 SkillData 및 캐릭터 참조 저장
            AssetDatabase.Refresh(); // Unity 에셋 목록 갱신
            Debug.Log("[Project H][DAY17] Skill block system setup complete. Generate → Drag → Same SkillId → Enhancement 1~3 → Consume."); // 17일차 설정 완료 로그
        }

        private static void EnsureSkillDataFolder() // SkillData 저장 폴더 확보
        {
            if (AssetDatabase.IsValidFolder(SkillDataFolder)) // 기존 SkillData 폴더 존재 확인
            {
                return; // 기존 폴더 재생성 중단
            }

            AssetDatabase.CreateFolder("Assets/ProjectH/Data", "Skills"); // SkillData 폴더 생성
        }

        private static void CreateAndAssignCharacterSkills() // 전체 CharacterData에 Skill 1·2·3 생성 및 연결
        {
            string[] characterGuids = AssetDatabase.FindAssets("t:CharacterData", new[] { CharacterDataFolder }); // 전체 CharacterData GUID 검색

            for (int characterIndex = 0; characterIndex < characterGuids.Length; characterIndex++) // 전체 CharacterData 순회
            {
                string characterPath = AssetDatabase.GUIDToAssetPath(characterGuids[characterIndex]); // CharacterData 에셋 경로 변환
                CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>(characterPath); // CharacterData 에셋 로드

                if (character == null || string.IsNullOrWhiteSpace(character.Id)) // CharacterData 및 ID 확인
                {
                    continue; // 잘못된 캐릭터 데이터 제외
                }

                SkillData[] skills = new SkillData[3]; // 캐릭터 Skill 1·2·3 배열 생성

                for (int slot = 1; slot <= 3; slot++) // 캐릭터 스킬 슬롯 순회
                {
                    skills[slot - 1] = EnsureSkillAsset(character, slot); // 슬롯별 SkillData 생성 또는 조회
                }

                SerializedObject characterObject = new SerializedObject(character); // CharacterData 직렬화 객체 생성
                SerializedProperty skillsProperty = characterObject.FindProperty("skills"); // CharacterData 스킬 배열 속성 조회

                if (skillsProperty == null) // CharacterData 스킬 속성 존재 확인
                {
                    throw new System.InvalidOperationException($"{character.Id} CharacterData에서 skills 필드를 찾을 수 없습니다."); // CharacterData 스킬 필드 누락 예외 발생
                }

                skillsProperty.arraySize = 3; // CharacterData 스킬 배열 3칸 고정

                for (int skillIndex = 0; skillIndex < skills.Length; skillIndex++) // 생성 스킬 배열 순회
                {
                    skillsProperty.GetArrayElementAtIndex(skillIndex).objectReferenceValue = skills[skillIndex]; // CharacterData 슬롯별 SkillData 참조 적용
                }

                characterObject.ApplyModifiedPropertiesWithoutUndo(); // CharacterData 스킬 참조 변경 적용
                EditorUtility.SetDirty(character); // CharacterData 변경 표시
            }
        }

        private static SkillData EnsureSkillAsset(CharacterData character, int skillSlot) // 캐릭터 슬롯별 SkillData 생성 또는 조회
        {
            string shortCharacterId = character.Id.StartsWith("CH_") ? character.Id.Substring(3) : character.Id; // 캐릭터 ID 접두사 제거
            string skillId = $"SK_{shortCharacterId}_{skillSlot:00}"; // 고유 SkillId 생성
            string assetPath = $"{SkillDataFolder}/{skillId}.asset"; // SkillData 에셋 경로 생성
            SkillData skill = AssetDatabase.LoadAssetAtPath<SkillData>(assetPath); // 기존 SkillData 조회

            if (skill != null) // 기존 SkillData 존재 확인
            {
                return skill; // 기존 스킬 데이터 보존 반환
            }

            skill = ScriptableObject.CreateInstance<SkillData>(); // 신규 SkillData 인스턴스 생성
            AssetDatabase.CreateAsset(skill, assetPath); // 신규 SkillData 에셋 생성
            SerializedObject skillObject = new SerializedObject(skill); // 신규 SkillData 직렬화 객체 생성
            SetString(skillObject, "id", skillId); // SkillData 고유 ID 적용
            SetString(skillObject, "ownerCharacterId", character.Id); // SkillData 소유 캐릭터 ID 적용
            SetInt(skillObject, "skillSlot", skillSlot); // SkillData 스킬 슬롯 적용
            SetString(skillObject, "displayName", $"{character.DisplayName} 스킬 {skillSlot}"); // 18~19일차 전 임시 스킬 이름 적용
            SetString(skillObject, "description", "17일차 공통 스킬 블록용 임시 데이터. 실제 효과는 캐릭터 스킬 구현 일차에 연결."); // 임시 스킬 설명 적용
            SetInt(skillObject, "targetType", (int)SkillTargetType.NearestEnemy); // 임시 최근접 적 대상 적용
            SetInt(skillObject, "effectType", (int)SkillEffectType.None); // 17일차 실제 효과 미연결 상태 적용
            ConfigureDefaultEnhancements(skillObject); // 강화도 1·2·3 기본 데이터 적용
            skillObject.ApplyModifiedPropertiesWithoutUndo(); // 신규 SkillData 값 적용
            EditorUtility.SetDirty(skill); // 신규 SkillData 변경 표시
            return skill; // 신규 SkillData 반환
        }

        private static void ConfigureDefaultEnhancements(SerializedObject skillObject) // 신규 SkillData 강화도 1·2·3 기본값 설정
        {
            SerializedProperty enhancements = skillObject.FindProperty("enhancements"); // 강화도 배열 속성 조회

            if (enhancements == null) // 강화도 배열 속성 존재 확인
            {
                throw new System.InvalidOperationException("SkillData enhancements 필드를 찾을 수 없습니다."); // 강화도 필드 누락 예외 발생
            }

            enhancements.arraySize = 3; // 강화도 배열 3단계 고정
            float[] ratios = { 1f, 1.25f, 1.6f }; // 임시 강화도별 계수
            int[] gaugeGains = { 10, 20, 30 }; // 21일차 연결용 임시 게이지 획득량

            for (int index = 0; index < 3; index++) // 강화도 1~3 순회
            {
                SerializedProperty enhancement = enhancements.GetArrayElementAtIndex(index); // 현재 강화도 속성 조회
                enhancement.FindPropertyRelative("powerRatio").floatValue = ratios[index]; // 강화도별 임시 계수 적용
                enhancement.FindPropertyRelative("flatValue").intValue = 0; // 강화도별 임시 고정값 0 적용
                enhancement.FindPropertyRelative("ultimateGaugeGain").intValue = gaugeGains[index]; // 강화도별 임시 궁극기 게이지 획득량 적용
            }
        }

        private static BattleSkillBlockPanel EnsureSkillBlockPanel(Transform canvasRoot, out RectTransform blockLayer, out Text counterText, out Text hintText) // 스킬 블록 패널 UI 생성 또는 조회
        {
            Transform existing = FindOptionalChild(canvasRoot, "SkillBlockPanel"); // 기존 스킬 블록 패널 조회
            GameObject panelObject; // 스킬 블록 패널 객체 변수 선언

            if (existing != null) // 기존 스킬 블록 패널 존재 확인
            {
                panelObject = existing.gameObject; // 기존 패널 객체 사용
            }
            else // 신규 스킬 블록 패널 생성 처리
            {
                panelObject = new GameObject("SkillBlockPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(BattleSkillBlockPanel)); // 스킬 블록 패널 객체 생성
                panelObject.transform.SetParent(canvasRoot, false); // Battle HUD Canvas 연결
            }

            Image panelImage = EnsureComponent<Image>(panelObject); // 스킬 블록 패널 Image 확보
            panelImage.color = PanelColor; // 스킬 블록 패널 배경색 적용
            RectTransform panelRect = panelObject.GetComponent<RectTransform>(); // 스킬 블록 패널 RectTransform 조회
            BattleDay17HudLayout.ApplySkillPanel(panelRect); // 스킬 블록 패널을 하단 오른쪽 영역에 배치
            Text titleText = EnsureText(panelObject.transform, "TitleText", "SKILL BLOCKS", 16, FontStyle.Bold, TextColor); // 스킬 블록 패널 제목 확보
            SetRect(titleText.rectTransform, new Vector2(0.02f, 0.76f), new Vector2(0.22f, 0.98f)); // 스킬 블록 제목 왼쪽 상단 배치
            counterText = EnsureText(panelObject.transform, "CounterText", "0 / 7", 15, FontStyle.Bold, TextColor); // 블록 보유 수 텍스트 확보
            SetRect(counterText.rectTransform, new Vector2(0.82f, 0.76f), new Vector2(0.98f, 0.98f)); // 블록 보유 수 오른쪽 상단 배치
            RectTransform slotLayer = EnsureRectTransform(panelObject.transform, "SlotLayer"); // 빈 슬롯 표시 레이어 확보
            SetRect(slotLayer, new Vector2(0.02f, 0.18f), new Vector2(0.98f, 0.76f)); // 블록 슬롯 레이어 배치
            EnsureSlotBackgrounds(slotLayer); // 7개 블록 빈 슬롯 배경 확보
            blockLayer = EnsureRectTransform(panelObject.transform, "BlockLayer"); // Runtime 블록 표시 레이어 확보
            SetRect(blockLayer, new Vector2(0.02f, 0.18f), new Vector2(0.98f, 0.76f)); // Runtime 블록 레이어 슬롯과 동일 배치
            blockLayer.SetAsLastSibling(); // Runtime 블록을 빈 슬롯 위에 표시
            hintText = EnsureText(panelObject.transform, "HintText", "블록 생성 대기", 12, FontStyle.Normal, TextColor); // 블록 조작 안내 텍스트 확보
            SetRect(hintText.rectTransform, new Vector2(0.02f, 0.01f), new Vector2(0.98f, 0.17f)); // 블록 조작 안내 하단 배치
            BattleSkillBlockPanel panel = EnsureComponent<BattleSkillBlockPanel>(panelObject); // 스킬 블록 패널 컴포넌트 확보
            return panel; // 스킬 블록 패널 반환
        }

        private static void EnsureSlotBackgrounds(RectTransform slotLayer) // 최대 7개 블록 슬롯 배경 구성
        {
            for (int index = 0; index < MaxBlockCount; index++) // 최대 블록 슬롯 순회
            {
                string slotName = $"Slot_{index}"; // 슬롯 객체 이름 생성
                Transform existing = FindOptionalChild(slotLayer, slotName); // 기존 슬롯 객체 조회
                GameObject slotObject; // 슬롯 객체 변수 선언

                if (existing != null) // 기존 슬롯 존재 확인
                {
                    slotObject = existing.gameObject; // 기존 슬롯 객체 사용
                }
                else // 신규 슬롯 생성 처리
                {
                    slotObject = new GameObject(slotName, typeof(RectTransform), typeof(Image)); // 빈 스킬 블록 슬롯 생성
                    slotObject.transform.SetParent(slotLayer, false); // 슬롯 레이어 연결
                }

                Image image = EnsureComponent<Image>(slotObject); // 슬롯 Image 확보
                image.color = SlotColor; // 빈 슬롯 배경색 적용
                image.raycastTarget = false; // 빈 슬롯 Raycast 비활성화
                RectTransform rect = slotObject.GetComponent<RectTransform>(); // 슬롯 RectTransform 조회
                float slotWidth = 1f / MaxBlockCount; // 슬롯 정규화 너비 계산
                float minX = index * slotWidth; // 슬롯 최소 X 계산
                SetRect(rect, new Vector2(minX, 0.08f), new Vector2(minX + slotWidth, 0.92f)); // 슬롯 가로 7칸 배치
                rect.offsetMin = new Vector2(3f, 3f); // 슬롯 최소 내부 여백 적용
                rect.offsetMax = new Vector2(-3f, -3f); // 슬롯 최대 내부 여백 적용
            }
        }

        private static void RepositionProfileCards(Scene scene) // 기존 4인 프로필 카드 왼쪽 하단 축소 배치
        {
            for (int index = 0; index < BattleDay17HudLayout.ProfileCardCount; index++) // 4인 프로필 카드 순회
            {
                Transform card = FindOptionalTransform(scene, $"BattleHudCard_{index}"); // 현재 프로필 카드 조회

                if (card == null) // 프로필 카드 존재 확인
                {
                    continue; // 없는 프로필 카드 제외
                }

                RectTransform cardRect = card.GetComponent<RectTransform>(); // 프로필 카드 RectTransform 조회
                BattleDay17HudLayout.ApplyProfileCard(cardRect, index); // 프로필 카드를 왼쪽 하단 비중첩 위치에 배치
            }
        }

        private static void HideLegacyCardSkillButtons(Scene scene) // 기존 캐릭터 카드 SKILL LOCKED 버튼 숨김
        {
            for (int index = 0; index < 4; index++) // 기존 4인 HUD 카드 순회
            {
                Transform card = FindOptionalTransform(scene, $"BattleHudCard_{index}"); // HUD 카드 조회

                if (card == null) // HUD 카드 존재 확인
                {
                    continue; // 없는 HUD 카드 제외
                }

                Transform legacyButton = FindOptionalChild(card, "SkillButton"); // 기존 카드별 SkillButton 조회

                if (legacyButton != null) // 기존 SkillButton 존재 확인
                {
                    legacyButton.gameObject.SetActive(false); // 블록 시스템 전환으로 카드별 SkillButton 숨김
                }
            }
        }

        private static void SetString(SerializedObject target, string propertyName, string value) // 문자열 직렬화 속성 안전 설정
        {
            SerializedProperty property = target.FindProperty(propertyName); // 문자열 속성 조회

            if (property != null) // 문자열 속성 존재 확인
            {
                property.stringValue = value; // 문자열 속성 값 적용
            }
        }

        private static void SetInt(SerializedObject target, string propertyName, int value) // 정수 및 Enum 직렬화 속성 안전 설정
        {
            SerializedProperty property = target.FindProperty(propertyName); // 정수 속성 조회

            if (property != null) // 정수 속성 존재 확인
            {
                property.intValue = value; // 정수 속성 값 적용
            }
        }

        private static RectTransform EnsureRectTransform(Transform parent, string name) // RectTransform 객체 생성 또는 조회
        {
            Transform existing = FindOptionalChild(parent, name); // 기존 RectTransform 객체 조회

            if (existing != null) // 기존 객체 존재 확인
            {
                return existing as RectTransform; // 기존 RectTransform 반환
            }

            GameObject targetObject = new GameObject(name, typeof(RectTransform)); // 신규 RectTransform 객체 생성
            targetObject.transform.SetParent(parent, false); // 신규 객체 부모 연결
            return targetObject.GetComponent<RectTransform>(); // 신규 RectTransform 반환
        }

        private static Text EnsureText(Transform parent, string name, string value, int size, FontStyle style, Color color) // 공통 Text 생성 또는 조회
        {
            Transform existing = FindOptionalChild(parent, name); // 기존 Text 객체 조회
            Text text; // Text 변수 선언

            if (existing != null) // 기존 Text 객체 존재 확인
            {
                text = existing.GetComponent<Text>(); // 기존 Text 컴포넌트 조회

                if (text == null) // 기존 Text 컴포넌트 존재 확인
                {
                    text = existing.gameObject.AddComponent<Text>(); // 기존 객체에 Text 컴포넌트 추가
                }
            }
            else // 신규 Text 객체 생성 처리
            {
                GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // 신규 Text 객체 생성
                textObject.transform.SetParent(parent, false); // 신규 Text 부모 연결
                text = textObject.GetComponent<Text>(); // 신규 Text 컴포넌트 조회
            }

            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 적용
            text.text = value; // Text 초기 문구 적용
            text.fontSize = size; // Text 글자 크기 적용
            text.fontStyle = style; // Text 글자 스타일 적용
            text.color = color; // Text 글자색 적용
            text.alignment = TextAnchor.MiddleCenter; // Text 중앙 정렬
            text.alignByGeometry = true; // Text 글리프 기준 정렬
            text.resizeTextForBestFit = true; // Text 자동 크기 적용
            text.resizeTextMinSize = 9; // Text 최소 글자 크기 적용
            text.resizeTextMaxSize = size; // Text 최대 글자 크기 적용
            text.raycastTarget = false; // Text Raycast 비활성화
            return text; // Text 반환
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component // 컴포넌트 생성 또는 조회
        {
            T component = target.GetComponent<T>(); // 기존 컴포넌트 조회

            if (component == null) // 기존 컴포넌트 존재 확인
            {
                component = target.AddComponent<T>(); // 신규 컴포넌트 추가
            }

            return component; // 컴포넌트 반환
        }

        private static T FindRequiredComponent<T>(Scene scene, string objectName) where T : Component // Scene 필수 컴포넌트 조회
        {
            Transform target = FindRequiredTransform(scene, objectName); // 필수 Scene 객체 조회
            T component = target.GetComponent<T>(); // 필수 컴포넌트 조회

            if (component == null) // 필수 컴포넌트 존재 확인
            {
                throw new System.InvalidOperationException($"{objectName}에 {typeof(T).Name} 컴포넌트가 없습니다."); // 필수 컴포넌트 누락 예외 발생
            }

            return component; // 필수 컴포넌트 반환
        }

        private static Transform FindRequiredTransform(Scene scene, string objectName) // Scene 필수 Transform 조회
        {
            Transform found = FindOptionalTransform(scene, objectName); // Scene 객체 검색

            if (found == null) // Scene 객체 존재 확인
            {
                throw new System.InvalidOperationException($"Battle Scene object not found: {objectName}."); // Scene 객체 누락 예외 발생
            }

            return found; // 필수 Transform 반환
        }

        private static Transform FindOptionalTransform(Scene scene, string objectName) // Scene Transform 검색
        {
            foreach (GameObject root in scene.GetRootGameObjects()) // Scene 루트 객체 순회
            {
                Transform found = FindOptionalChild(root.transform, objectName); // 루트 하위 객체 검색

                if (found != null) // 검색 결과 존재 확인
                {
                    return found; // 검색 Transform 반환
                }
            }

            return null; // Scene 검색 실패 반환
        }

        private static Transform FindOptionalChild(Transform root, string objectName) // 하위 Transform 재귀 검색
        {
            if (root == null) // 검색 루트 확인
            {
                return null; // 빈 검색 루트 반환
            }

            if (root.name == objectName) // 현재 객체 이름 일치 확인
            {
                return root; // 현재 Transform 반환
            }

            for (int index = 0; index < root.childCount; index++) // 자식 객체 순회
            {
                Transform found = FindOptionalChild(root.GetChild(index), objectName); // 자식 객체 재귀 검색

                if (found != null) // 자식 검색 결과 확인
                {
                    return found; // 검색 Transform 반환
                }
            }

            return null; // 하위 객체 검색 실패 반환
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max) // RectTransform 앵커 배치
        {
            rect.anchorMin = min; // 최소 앵커 적용
            rect.anchorMax = max; // 최대 앵커 적용
            rect.offsetMin = Vector2.zero; // 최소 오프셋 초기화
            rect.offsetMax = Vector2.zero; // 최대 오프셋 초기화
        }
    }
}
