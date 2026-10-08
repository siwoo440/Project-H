using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Image · Text 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class PortraitSlot // 이름 글자가 들어 있던 초상화 자리에 캐릭터 그림을 넣는 공용 기능 (Day81 신규 — 파티 화면 · 전투 결과창)
    {
        public const string HolderName = "PortraitArt"; // 글자 자리 위에 만드는 그림 틀 이름
        public const float BustAspect = 0.75f; // 상반신 그림의 가로 ÷ 세로 (세로로 긴 칸에 쓴다)

        public static Image Apply(Text placeholder, string characterId, bool bust) // 글자 자리에 그림을 채움 (그림이 없으면 숨기고 null — 호출 측이 이름 글자를 그대로 둔다)
        {
            if (placeholder == null || placeholder.transform.parent == null) return null; // 자리 없음
            Transform parent = placeholder.transform.parent; // 글자의 부모 칸
            Transform holder = parent.Find(HolderName); // 이전에 만든 그림 틀
            Sprite art = GetArt(characterId, bust); // 캐릭터 그림

            if (art == null) // 그림 없음
            {
                if (holder != null) holder.gameObject.SetActive(false); // 이전 그림 숨김
                return null; // 이름 글자 유지
            }

            Image image = holder == null ? Create(parent, placeholder.rectTransform) : holder.GetComponentInChildren<Image>(true); // 그림 이미지 (없으면 생성)
            image.transform.parent.gameObject.SetActive(true); // 그림 틀 표시
            image.sprite = art; // 그림 적용
            image.color = Color.white; // 원래 색
            AspectRatioFitter fitter = image.GetComponent<AspectRatioFitter>(); // 비율 맞춤
            fitter.aspectMode = art.rect.width < art.rect.height ? AspectRatioFitter.AspectMode.EnvelopeParent : AspectRatioFitter.AspectMode.FitInParent; // 상반신은 칸을 가득 채우고, 정사각 얼굴은 칸 안에 다 보이게
            fitter.aspectRatio = art.rect.width / art.rect.height; // 그림 비율
            return image; // 그림 반환
        }

        public static void Clear(Text placeholder) // 빈 자리 : 그림 숨김 (이름 글자는 호출 측이 정한다)
        {
            if (placeholder == null || placeholder.transform.parent == null) return; // 자리 없음
            Transform holder = placeholder.transform.parent.Find(HolderName); // 그림 틀
            if (holder != null) holder.gameObject.SetActive(false); // 숨김
        }

        private static Sprite GetArt(string characterId, bool bust) // 그림 고르기 : 상반신(요청했을 때) → 얼굴 → 없음
        {
            if (string.IsNullOrEmpty(characterId)) return null; // 빈 ID
            Sprite art = bust ? CharacterPortraitArt.GetBust(characterId, BustAspect) : null; // 상반신
            if (art != null) return art; // 상반신 사용
            art = CharacterPortraitArt.Get(characterId, out bool isPlaceholder); // 얼굴
            return isPlaceholder ? null : art; // 정식 그림만
        }

        private static Image Create(Transform parent, RectTransform slot) // 글자와 같은 자리에 그림 틀 생성 (자리 밖으로 나간 부분은 잘린다)
        {
            GameObject holderObject = new GameObject(HolderName, typeof(RectTransform), typeof(RectMask2D)); // 그림 틀
            holderObject.transform.SetParent(parent, false); // 글자의 부모 아래
            RectTransform holder = (RectTransform)holderObject.transform; // 틀 영역
            holder.anchorMin = slot.anchorMin; // 글자와 같은 자리
            holder.anchorMax = slot.anchorMax; // 글자와 같은 자리
            holder.pivot = slot.pivot; // 글자와 같은 기준점
            holder.anchoredPosition = slot.anchoredPosition; // 글자와 같은 위치
            holder.sizeDelta = slot.sizeDelta; // 글자와 같은 크기
            holder.SetSiblingIndex(slot.GetSiblingIndex()); // 글자 바로 앞 순서 (글자보다 뒤에 그려진다)
            Image image = RuntimeUiKit.CreateImage(holder, "Art", Color.white); // 그림
            image.raycastTarget = false; // 클릭은 칸이 받는다
            image.gameObject.AddComponent<AspectRatioFitter>(); // 비율 맞춤
            return image; // 그림 반환
        }
    }
}
