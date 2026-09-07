using UnityEngine;

namespace _Scripts.LSO.Will.Candle
{
    /// <summary>
    /// 양초가 든 불을 카드에 붙이는 다리.
    ///
    /// 양초(LSO_WillCandle)는 "지금 무슨 색인가"까지만 알고,
    /// 카드(LSO_CardWill)는 "나에게 무엇이 붙었나"만 안다.
    /// 둘을 이어주는 것이 여기다. 어느 쪽도 상대를 모른다.
    ///
    /// ── 부르는 쪽 ─────────────────────────────────────────────
    /// 손패 카드를 골랐을 때 이걸 부른다.
    ///
    ///     painter.Paint(card.gameObject);
    ///
    /// **배치를 막지 않는다.** 양초는 늘 무언가를 들고 있으므로
    /// "붙이는 클릭"과 "놓는 클릭"을 나눌 필요가 없다.
    /// 카드를 고르면 그때 색이 붙고, 그대로 놓으면 그 유언으로 소환된다.
    ///
    /// 다른 유언으로 바꾸고 싶으면 숫자키로 색을 바꾸고 그 카드를 다시 고르면 된다.
    /// ─────────────────────────────────────────────────────────
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LSO_WillPainter : MonoBehaviour
    {
        [Header("연결")]
        [Tooltip("불을 고르는 양초. 비워두면 씬에서 찾는다.")]
        [SerializeField] private LSO_WillCandle candle;

        [Header("동작")]
        [Tooltip("이미 붙어 있는 카드에 다시 붙일 수 있을지.\n" +
                 "\n" +
                 "켜면 덮어쓴다. 잘못 붙였을 때 색을 바꾸고 다시 고르면 고쳐진다.\n" +
                 "끄면 한 번 붙인 카드는 놓기 전까지 바꿀 수 없다.")]
        [SerializeField] private bool allowOverwrite = true;

        [Header("반응")]
        [Tooltip("유언을 붙였을 때. 소리를 여기 걸면 된다.")]
        [SerializeField] private LSO_WillTypeEvent onPainted;

        private LSO_WillCandle Candle
        {
            get
            {
                if (candle == null) candle = FindAnyObjectByType<LSO_WillCandle>();
                return candle;
            }
        }

        /// <summary>지금 양초가 든 유언. 양초가 없으면 None.</summary>
        public LSO_WillType Held => Candle != null ? Candle.Current : LSO_WillType.None;

        /// <summary>
        /// 카드에 지금 든 유언을 붙인다.
        /// </summary>
        /// <param name="card">손패 카드의 오브젝트. LSO_CardWill 이 붙어 있어야 한다.</param>
        /// <returns>
        /// 붙였으면 참. 거짓이면 양초나 LSO_CardWill 이 없는 것이다.
        ///
        /// <b>거짓이어도 배치는 그대로 진행할 것.</b> 유언이 안 붙었을 뿐이고,
        /// 그때는 LDY_CardPlacer 가 예전처럼 고르는 창을 띄운다.
        /// </returns>
        public bool Paint(GameObject card)
        {
            if (card == null) return false;

            LSO_WillCandle current = Candle;

            if (current == null)
            {
                Debug.LogWarning($"{name}: 씬에 LSO_WillCandle이 없어 유언을 붙이지 못했습니다.", this);
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

            // 불이 있던 자리를 넘겨준다. 아이콘이 그쪽에서부터 번진다.
            LSO_WillType will = current.Current;

            target.Apply(will, current.transform.position);

            onPainted?.Invoke(will);

            return true;
        }
    }
}
