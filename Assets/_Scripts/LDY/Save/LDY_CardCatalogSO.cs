using System.Collections.Generic;
using _Scripts.LSO.Deck.Data;
using UnityEngine;

namespace _Scripts.LDY.Save
{
    /// <summary>
    /// 세이브에 적힌 카드 id를 실제 카드 에셋으로 되돌리는 표.
    /// 여기 없는 카드는 불러오기에서 복원되지 않는다.
    ///
    /// 프로젝트 창에서 Create &gt; LDY &gt; Save &gt; Card Catalog 로 만들고,
    /// LDY_SaveService가 찾을 수 있도록 Resources 폴더 아래에 ResourcePath 이름으로 둘 것.
    /// </summary>
    [CreateAssetMenu(fileName = "LDY_CardCatalog", menuName = "LDY/Save/Card Catalog")]
    public class LDY_CardCatalogSO : ScriptableObject
    {
        /// <summary>Resources 폴더 기준 경로. 확장자는 붙이지 않는다.</summary>
        public const string ResourcePath = "LDY_CardCatalog";

        [Tooltip("게임에 존재하는 모든 카드. 세이브에 적힌 id를 이 목록에서 찾는다.")]
        [SerializeField] private LSO_CardSO[] cards = new LSO_CardSO[0];

        // 조회용 런타임 캐시다. 직렬화되지 않으므로 세이브 파일에는 나가지 않는다.
        private Dictionary<string, LSO_CardSO> _byId;

        public int Count
        {
            get
            {
                EnsureIndex();
                return _byId.Count;
            }
        }

        /// <summary>id에 해당하는 카드. 없으면 null.</summary>
        public LSO_CardSO Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            EnsureIndex();

            return _byId.TryGetValue(id, out LSO_CardSO card) ? card : null;
        }

        private void EnsureIndex()
        {
            if (_byId != null) return;

            _byId = new Dictionary<string, LSO_CardSO>();

            foreach (LSO_CardSO card in cards)
            {
                if (card == null) continue;

                // id가 겹치면 어느 쪽을 되돌려야 할지 알 수 없다.
                // 조용히 덮어쓰면 불러오기 결과가 엉뚱해지고 원인을 찾기 어려우므로 알린다.
                if (_byId.TryGetValue(card.Id, out LSO_CardSO existing))
                {
                    Debug.LogError(
                        $"[LDY_CardCatalogSO] id '{card.Id}' 가 중복입니다. ({existing.name} / {card.name})", this);
                    continue;
                }

                _byId.Add(card.Id, card);
            }
        }

        // 인스펙터에서 목록을 고치면 캐시를 버린다. 플레이 중 편집해도 결과가 어긋나지 않게 한다.
        private void OnValidate()
        {
            _byId = null;
        }
    }
}
