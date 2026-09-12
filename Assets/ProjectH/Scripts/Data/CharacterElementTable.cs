using System.Collections.Generic; // 사전 자료형

namespace ProjectH.Data // 프로젝트 데이터 영역
{
    public static class CharacterElementTable // 캐릭터 속성 표 (Day67 신규 — 데이터 관리자 없이 속성을 확인해야 하는 곳에서 사용, 에셋과 일치하는지는 테스트가 확인)
    {
        private static readonly Dictionary<string, ElementType> Elements = new Dictionary<string, ElementType> // 12인 속성
        {
            { "CH_SERENA", ElementType.Light }, // 세레나 : 빛
            { "CH_ELLEN", ElementType.Grass }, // 엘렌 : 풀
            { "CH_LILIA", ElementType.Fire }, // 릴리아 : 불
            { "CH_EVE", ElementType.Water }, // 이브 : 물
            { "CH_NATASHA", ElementType.Dark }, // 나타샤 : 어둠
            { "CH_CLAIRE", ElementType.Grass }, // 클레어 : 풀
            { "CH_LUCIA", ElementType.Fire }, // 루시아 : 불
            { "CH_PYRA", ElementType.Fire }, // 파이라 : 불
            { "CH_TYRIA", ElementType.None }, // 티리아 : 무속성
            { "CH_MERCIA", ElementType.Grass }, // 메르시아 : 풀
            { "CH_NOEL", ElementType.Water }, // 노엘 : 물
            { "CH_SEPHIRA", ElementType.Light } // 세피라 : 빛
        };

        public static ElementType Get(string characterId) => characterId != null && Elements.TryGetValue(characterId, out ElementType element) ? element : ElementType.None; // 캐릭터 속성 조회

        public static IReadOnlyDictionary<string, ElementType> All => Elements; // 전체 표
    }
}
