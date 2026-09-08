using _Scripts.LSO.UI.Input;
using UnityEngine;

namespace _Scripts.LSO.Will.Candle
{
    /// <summary>
    /// 양초를 누르면 지금 고른 카드로 가서 불을 댄다.
    ///
    /// ── 어떻게 쓰나 ───────────────────────────────────────────
    ///   1. 숫자키로 불꽃 색을 고른다        (LSO_WillCandle)
    ///   2. 손패에서 카드를 고른다
    ///   3. 양초를 누른다                    → 양초가 그 카드로 가서 불을 댄다
    ///
    /// 고른 카드가 없으면 아무 일도 하지 않는다. 어디에 붙일지 모르는 채로
    /// 양초만 움직이면 무엇이 일어났는지 알 수 없다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 양초(LSO_WillCandle)는 "지금 무슨 색인가"까지만 알고,
    /// 카드(LSO_CardWill)는 "나에게 무엇이 붙었나"만 안다.
    /// 둘을 이어주는 것이 여기다. 어느 쪽도 상대를 모른다.
    ///
    /// 씬 배선: 양초 오브젝트에 LSO_WillCandle 과 함께 붙일 것.
    /// Collider 도 있어야 클릭이 온다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LSO_ButtonClickHandler))]
    public sealed class LSO_WillPainter : MonoBehaviour, LSO_IClickEffect
    {
        [Header("연결")]
        [Tooltip("불을 고르는 양초. 비워두면 같은 오브젝트에서 찾는다.")]
        [SerializeField] private LSO_WillCandle candle;

        [Tooltip("카드로 다가가는 움직임. 비워두면 같은 오브젝트에서 찾는다.\n" +
                 "없어도 된다 — 그때는 움직임 없이 그 자리에서 붙는다.")]
        [SerializeField] private LSO_WillCandleMotion motion;

        [Header("동작")]
        [Tooltip("이미 붙어 있는 카드에 다시 붙일 수 있을지.\n" +
                 "\n" +
                 "켜면 덮어쓴다. 색을 바꾸고 다시 누르면 고쳐진다.\n" +
                 "끄면 한 번 붙인 카드는 놓기 전까지 바꿀 수 없다.")]
        [SerializeField] private bool allowOverwrite = true;

        [Header("반응")]
        [Tooltip("유언을 붙였을 때. 소리를 여기 걸면 된다.")]
        [SerializeField] private LSO_WillTypeEvent onPainted;

        [Tooltip("고른 카드가 없어서 아무 일도 못 했을 때.\n" +
                 "'카드를 먼저 고르세요' 같은 안내를 여기 걸면 된다.")]
        [SerializeField] private LSO_WillTypeEvent onNoTarget;

        /// <summary>
        /// 붙일 카드를 밖에서 직접 지정한다.
        ///
        /// 비워두면 손패에서 고른 카드를 찾는다. 손패 구현이 바뀌어 못 찾게 되면
        /// 이 값을 넣어주는 쪽을 만들면 된다 — 찾는 코드를 고칠 필요가 없다.
        /// </summary>
        public GameObject Target { get; set; }

        /// <summary>지금 양초가 든 유언. 양초가 없으면 None.</summary>
        public LSO_WillType Held => candle != null ? candle.Current : LSO_WillType.None;

        private void Awake()
        {
            if (candle == null) candle = GetComponent<LSO_WillCandle>();
            if (motion == null) motion = GetComponent<LSO_WillCandleMotion>();

            if (candle == null)
                Debug.LogError($"{name}: LSO_WillCandle이 없어 무슨 색인지 알 수 없습니다.", this);
        }

        /// <summary>양초를 눌렀다. 고른 카드로 가서 불을 댄다.</summary>
        public void OnClick()
        {
            Paint();
        }

        /// <summary>
        /// 고른 카드에 지금 든 유언을 붙인다.
        /// </summary>
        /// <returns>붙였으면 참. 고를 카드가 없거나 배선이 빠졌으면 거짓.</returns>
        public bool Paint()
        {
            if (candle == null) return false;

            GameObject card = Target != null ? Target : FindSelectedCard();

            if (card == null)
            {
                // 경고가 아니라 이벤트로 알린다. 카드를 안 고르고 양초를 누르는 것은
                // 실수이지 버그가 아니다. 화면에서 알려주는 편이 낫다.
                onNoTarget?.Invoke(candle.Current);
                return false;
            }

            LSO_CardWill target = card.GetComponentInChildren<LSO_CardWill>(true);

            if (target == null)
            {
                Debug.LogWarning(
                    $"{name}: '{card.name}'에 LSO_CardWill이 없어 유언을 붙이지 못했습니다. " +
                    "손패 카드 프리팹에 붙여 주세요.", card);
                return false;
            }

            if (target.HasWill && !allowOverwrite) return false;

            LSO_WillType will = candle.Current;

            if (motion == null)
            {
                // 움직임이 없으면 그 자리에서 바로 붙이고 드러낸다. 결과는 같다.
                target.Apply(will, transform.position);

                onPainted?.Invoke(will);
                return true;
            }

            // 값은 지금 붙인다. 양초가 다가가는 동안에도 카드는 이미 그 유언을 들고 있어야
            // 연출이 끝나기 전에 놓아도 유언이 빠지지 않는다.
            target.Apply(will, revealNow: false);

            // 아이콘은 가장 가까이 닿은 순간에 드러난다.
            Transform self = transform;

            motion.Reach(card.transform.position, () => target.Reveal(self.position));

            onPainted?.Invoke(will);

            return true;
        }

        /// <summary>
        /// 손패에서 지금 고른 카드를 찾는다.
        ///
        /// ── 여기만 손패 구현을 안다 ───────────────────────────
        /// KTH 쪽에 "지금 고른 카드"를 한 번에 알려주는 길이 없어서, 카드마다 물어본다.
        /// 유언이 붙을 수 있는 카드(LSO_CardWill)만 훑으므로 손패 몇 장이 전부다.
        ///
        /// 손패 구현이 바뀌면 **이 메서드 하나만** 고치면 된다.
        /// 아니면 밖에서 Target 을 넣어주면 이쪽은 아예 안 돈다.
        /// ─────────────────────────────────────────────────────
        /// </summary>
        private static GameObject FindSelectedCard()
        {
            LSO_CardWill[] cards = FindObjectsByType<LSO_CardWill>(FindObjectsSortMode.None);

            foreach (LSO_CardWill card in cards)
            {
                var hand = card.GetComponentInParent<KTH_HandCard>();

                if (hand != null && hand.IsSelected) return hand.gameObject;
            }

            return null;
        }
    }
}
