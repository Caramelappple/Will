using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace _Scripts.LSO.Reward
{
    /// <summary>
    /// 보상 카드를 어떻게 움직이는지. **언제 움직이는지는 모른다.**
    ///
    /// 상자에서 솟고, 고른 것이 밀려나고, 덱으로 날아가고, 나머지가 도로 내려간다.
    /// 그 네 가지 움직임과 거기 딸린 시간·이징 값이 여기 모여 있다.
    ///
    /// ── 왜 떼어냈나 ───────────────────────────────────────────
    /// LSO_RewardBox는 "뽑고 → 보여주고 → 기다리고 → 지급한다"는 순서를 아는 곳이다.
    /// 그런데 카드가 몇 초에 걸쳐 어떤 이징으로 움직이는지까지 같이 들고 있었다.
    /// 연출을 손볼 때마다 순서를 아는 파일을 열게 되고, 그 파일이 계속 자란다.
    ///
    /// 이제 상자는 "지금 카드를 내려라"만 말하고, 어떻게 내려가는지는 여기가 정한다.
    /// LSO_RewardLayout(어디에 서는가) · LSO_RewardCardPool(어디서 빌리는가) 와 같은 결이다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 카드의 생사는 건드리지 않는다. 빌리고 돌려주는 것은 상자와 풀의 일이다.
    /// 여기서 풀에 돌려보내기 시작하면 상자가 든 목록과 어긋난다.
    /// </summary>
    [Serializable]
    public sealed class LSO_RewardCardMotion
    {
        [Header("솟아오르기")]
        [Tooltip("카드 한 장이 솟는 데 걸리는 시간.")]
        [SerializeField, Min(0f)] private float riseDuration = 0.35f;

        [Tooltip("다음 카드가 나오기까지의 간격.")]
        [SerializeField, Min(0f)] private float dealInterval = 0.12f;

        [SerializeField] private Ease riseEase = Ease.OutBack;

        [Header("고른 카드 반응")]
        [Tooltip("카드를 고른 순간 그 자리에서 더 밀려나는 양. 기준 자리의 로컬 축이다.\n" +
                 "\n" +
                 "누른 즉시 이만큼 움직여 '골랐다'를 알린다.\n" +
                 "(0,0,0) 으로 두면 누른 뒤 Pick Hold 동안 아무 변화가 없어 멈춘 것처럼 보인다.")]
        [SerializeField] private Vector3 pickLift = new Vector3(0f, 0.25f, -0.2f);

        [Tooltip("고른 카드가 밀려나는 데 걸리는 시간. 짧아야 누른 즉시 반응한 느낌이 난다.")]
        [SerializeField, Min(0f)] private float pickLiftDuration = 0.12f;

        [SerializeField] private Ease pickLiftEase = Ease.OutCubic;

        [Header("덱으로 보내기")]
        [Tooltip("고른 카드가 날아갈 자리. 손패나 덱 더미를 꽂는다.\n" +
                 "\n" +
                 "비워두면 그냥 상자 안으로 들어간다. 덱에 추가되는 것은 마찬가지지만,\n" +
                 "어디로 갔는지 화면에 보이지 않는다.")]
        [SerializeField] private Transform deckAnchor;

        [Tooltip("덱까지 날아가는 데 걸리는 시간.")]
        [SerializeField, Min(0f)] private float toDeckDuration = 0.5f;

        [Tooltip("덱으로 들어갈 때의 크기 배율. 1보다 작으면 멀어지듯 작아진다.")]
        [SerializeField, Min(0.01f)] private float toDeckScale = 0.5f;

        [SerializeField] private Ease toDeckEase = Ease.InCubic;

        private LSO_RewardCardPool _pool;
        private LSO_RewardLayout _layout;
        private Transform _anchor;
        private MonoBehaviour _owner;

        /// <summary>다음 카드가 나오기까지의 간격. 순서를 쥔 쪽이 이 값만큼 쉰다.</summary>
        public float DealInterval => dealInterval;

        /// <summary>
        /// 쓰기 전에 한 번 불러 연결한다. 상자의 Awake에서 부른다.
        /// </summary>
        public void Bind(
            LSO_RewardCardPool pool,
            LSO_RewardLayout layout,
            Transform anchor,
            MonoBehaviour owner)
        {
            _pool = pool;
            _layout = layout;
            _anchor = anchor;
            _owner = owner;
        }

        /// <summary>
        /// 카드 한 장을 상자 안에서 띄운다.
        ///
        /// 상자 안에서 시작한다. 켜기 전에 자리를 잡아야 한 프레임 튀지 않는다.
        /// 좌우로는 미리 벌려둔다 — 세 장이 한 점에서 겹쳐 나오면 뭉쳐 보인다.
        ///
        /// 만들지 못하면 null을 돌려준다. 부르는 쪽이 목록에 넣을지 말지 정한다.
        /// </summary>
        public LSO_RewardCard Rise(
            LSO_RewardOption option,
            int index,
            int total,
            Action<LSO_RewardCard> onClick)
        {
            if (!IsReady()) return null;

            LSO_RewardCard card = _pool.TakePiece();

            if (card == null)
            {
                Debug.LogError(
                    $"{Name}: Piece Card Prefab이 없어 기물 카드를 만들지 못했습니다.", _owner);
                return null;
            }

            card.transform.SetParent(_anchor, false);

            card.transform.localPosition = _pool.CardInsideLocal + _layout.Spread(index, total);
            card.transform.localRotation = _layout.Tilt(index, total);

            card.Bind(option, onClick);

            card.transform
                .DOLocalMove(_layout.Position(index, total), riseDuration)
                .SetEase(riseEase)
                .SetLink(card.gameObject);

            return card;
        }

        /// <summary>
        /// 고른 카드를 제자리에서 한 번 밀어낸다. 클릭에 대한 즉시 반응이다.
        ///
        /// 트윈이 끝날 때까지 기다린다. 기다리지 않고 넘어가면 밀려나는 도중에
        /// 덱으로 가는 트윈이 시작돼 두 움직임이 겹친다.
        /// </summary>
        public IEnumerator Lift(LSO_RewardCard chosen)
        {
            if (chosen == null) yield break;
            if (pickLift == Vector3.zero || pickLiftDuration <= 0f) yield break;

            Transform card = chosen.transform;

            card.DOKill();

            yield return card
                .DOLocalMove(card.localPosition + pickLift, pickLiftDuration)
                .SetEase(pickLiftEase)
                .SetLink(chosen.gameObject)
                .WaitForCompletion();
        }

        /// <summary>
        /// 고른 카드를 덱 쪽으로 날려 보낸다.
        ///
        /// 덱에 실제로 넣는 것은 이 연출이 아니다. 지급은 이미 끝났고(LSO_RewardClaim →
        /// LSO_ItemLibraryManager), 여기서는 "어디로 갔는지" 만 보여준다.
        /// 둘을 묶으면 연출이 끊겼을 때 카드가 사라지거나 두 번 들어간다.
        ///
        /// 덱 자리를 안 꽂았으면 상자 안으로 들어간다. 받은 것은 마찬가지지만
        /// 어디로 갔는지 보이지 않으므로 한 번 짚어준다.
        /// </summary>
        public IEnumerator SendToDeck(LSO_RewardCard chosen)
        {
            if (chosen == null || !IsReady()) yield break;

            Transform card = chosen.transform;

            card.DOKill();

            if (deckAnchor == null)
            {
                Debug.LogWarning(
                    $"{Name}: Deck Anchor가 비어 있어 고른 카드가 상자 안으로 들어갑니다. " +
                    "덱으로 가는 것을 보여주려면 손패나 덱 더미를 꽂으세요.", _owner);

                yield return card
                    .DOLocalMove(_pool.CardInsideLocal, toDeckDuration)
                    .SetEase(toDeckEase)
                    .SetLink(chosen.gameObject)
                    .WaitForCompletion();

                yield break;
            }

            // 월드 좌표로 움직인다. 덱은 상자의 자식이 아니라 화면 아래 다른 곳에 있다.
            Sequence flight = DOTween.Sequence()
                .Append(card.DOMove(deckAnchor.position, toDeckDuration).SetEase(toDeckEase))
                .Join(card.DOScale(card.localScale * toDeckScale, toDeckDuration).SetEase(toDeckEase))
                .SetLink(chosen.gameObject);

            yield return flight.WaitForCompletion();
        }

        /// <summary>
        /// 건네준 카드들을 상자 안으로 도로 집어넣는다.
        ///
        /// 제자리에서 내려가는 것이 아니라 상자 입구(기준 자리의 원점)로 모인다.
        /// 벌어져 있던 자리로만 내리면 상자 옆 허공으로 가라앉는 것처럼 보인다.
        ///
        /// 풀에 돌려보내지 않는다. 그것은 상자의 일이다 —
        /// 여기서 돌려보내면 상자가 든 목록과 어긋난다.
        /// </summary>
        public IEnumerator Lower(IReadOnlyList<LSO_RewardCard> cards)
        {
            if (cards == null || cards.Count == 0 || !IsReady()) yield break;

            int moving = 0;

            foreach (LSO_RewardCard card in cards)
            {
                if (card == null) continue;

                card.transform.DOKill();

                card.transform
                    .DOLocalMove(_pool.CardInsideLocal, riseDuration)
                    .SetEase(riseEase)
                    .SetLink(card.gameObject);

                moving++;
            }

            if (moving == 0) yield break;

            yield return new WaitForSeconds(riseDuration);
        }

        /// <summary>
        /// 꺼내둔 카드를 지금 설정대로 다시 늘어놓는다.
        ///
        /// 간격을 인스펙터에서 만지는 동안 결과를 바로 보기 위한 것이다.
        /// 트윈 없이 즉시 옮긴다 — 값을 조금씩 바꿔볼 때 연출이 끼면 오히려 보기 어렵다.
        /// </summary>
        public void Relayout(IReadOnlyList<LSO_RewardCard> cards)
        {
            if (cards == null || _layout == null) return;

            for (int i = 0; i < cards.Count; i++)
            {
                LSO_RewardCard card = cards[i];
                if (card == null) continue;

                card.transform.DOKill();

                card.transform.localPosition = _layout.Position(i, cards.Count);
                card.transform.localRotation = _layout.Tilt(i, cards.Count);
            }
        }

        /// <summary>
        /// Bind를 안 불렀으면 조용히 아무 일도 안 하는 대신 짚어준다.
        /// 연출이 통째로 안 도는데 로그가 없으면 원인을 찾기 어렵다.
        /// </summary>
        private bool IsReady()
        {
            if (_pool != null && _layout != null && _anchor != null) return true;

            Debug.LogError(
                $"{Name}: 카드 움직임이 연결되지 않았습니다. Bind를 먼저 불러야 합니다.", _owner);

            return false;
        }

        private string Name => _owner != null ? _owner.name : nameof(LSO_RewardCardMotion);
    }
}
