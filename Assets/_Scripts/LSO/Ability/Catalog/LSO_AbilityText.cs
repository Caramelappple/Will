using UnityEngine;

namespace _Scripts.LSO.Ability.Catalog
{
    /// <summary>
    /// 특성 문구를 물어보는 창구. 화면을 그리는 쪽은 전부 여기로 온다.
    ///
    /// 사전(LSO_AbilityCatalogSO)을 직접 들고 다니지 않아도 되게 만든 얇은 껍데기다.
    /// 정보창·카드창·툴팁이 저마다 에셋 참조를 인스펙터에 물고 있으면
    /// 하나를 빠뜨렸을 때 그 화면만 조용히 영문 이름을 뱉는다.
    ///
    /// 사전이 없어도 죽지 않는다. 이름은 enum 이름으로, 설명은 빈 문자열로 돈다.
    /// 대신 처음 한 번 경고를 남긴다 — 없는 것과 안 만든 것은 다르다.
    /// </summary>
    public static class LSO_AbilityText
    {
        private static LSO_AbilityCatalogSO _catalog;
        private static bool _searched;

        /// <summary>지금 쓰고 있는 사전. 없으면 null.</summary>
        public static LSO_AbilityCatalogSO Catalog
        {
            get
            {
                LoadIfNeeded();
                return _catalog;
            }
        }

        /// <summary>화면에 띄울 이름. 사전에 없으면 enum 이름을 그대로 돌려준다.</summary>
        public static string NameOf(LSO_AbilityType type)
        {
            if (type == LSO_AbilityType.None) return "없음";

            return TryGet(type, out LSO_AbilityInfo info) ? info.ResolvedName : type.ToString();
        }

        /// <summary>
        /// 특성 설명. 사전에 없거나 안 적어뒀으면 빈 문자열이다.
        ///
        /// 빈 문자열을 돌려주는 이유는, 설명 칸에 "설명 없음" 같은 자리채움이 뜨는 것보다
        /// 아무것도 안 뜨는 편이 화면이 덜 지저분하기 때문이다.
        /// 무엇이 비었는지는 사전 에셋의 "설명 빠진 것 짚기"로 확인한다.
        /// </summary>
        public static string DescriptionOf(LSO_AbilityType type)
        {
            if (type == LSO_AbilityType.None) return string.Empty;

            return TryGet(type, out LSO_AbilityInfo info) && info.description != null
                ? info.description
                : string.Empty;
        }

        /// <summary>특성 아이콘. 없으면 null. 쓰는 쪽이 null을 처리한다.</summary>
        public static Sprite IconOf(LSO_AbilityType type)
        {
            return TryGet(type, out LSO_AbilityInfo info) ? info.icon : null;
        }

        /// <summary>"옹골참 — 받는 피해가 1 줄어든다" 한 줄. 설명이 없으면 이름만 나온다.</summary>
        public static string LineOf(LSO_AbilityType type, string separator = " — ")
        {
            string name = NameOf(type);
            string description = DescriptionOf(type);

            return string.IsNullOrWhiteSpace(description) ? name : name + separator + description;
        }

        /// <summary>적어둔 것이 있는지 확인하고 꺼낸다.</summary>
        public static bool TryGet(LSO_AbilityType type, out LSO_AbilityInfo info)
        {
            LoadIfNeeded();

            if (_catalog != null) return _catalog.TryGet(type, out info);

            info = default;
            return false;
        }

        /// <summary>
        /// 사전을 갈아 끼운다. 테스트나 특수한 화면에서만 쓴다.
        /// null을 넣으면 다음 조회 때 Resources에서 다시 찾는다.
        /// </summary>
        public static void Override(LSO_AbilityCatalogSO catalog)
        {
            _catalog = catalog;
            _searched = catalog != null;
        }

        private static void LoadIfNeeded()
        {
            if (_searched) return;

            _searched = true;
            _catalog = Resources.Load<LSO_AbilityCatalogSO>(LSO_AbilityCatalogSO.ResourcePath);

            if (_catalog == null)
            {
                Debug.LogWarning(
                    "특성 사전을 찾지 못해 영문 이름으로 표시합니다.\n" +
                    $"Assets/Resources/{LSO_AbilityCatalogSO.ResourcePath}.asset 에 두세요.\n" +
                    "만들기: 프로젝트 창 우클릭 > Create > LSO > 특성 사전");
            }
        }

        /// <summary>
        /// 도메인 리로드를 끈 에디터에서는 static이 지난 플레이의 값을 그대로 들고 있다.
        /// 에셋을 지웠는데도 계속 찾아지는 일이 없도록 플레이할 때마다 비운다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            _catalog = null;
            _searched = false;
        }
    }
}
