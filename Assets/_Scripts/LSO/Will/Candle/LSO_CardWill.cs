using System;
using UnityEngine;

namespace _Scripts.LSO.Will.Candle
{
    /// <summary>
    /// 손패 카드 한 장이 들고 있는 유언. 놓기 전에 촛대에서 붙여둔다.
    ///
    /// ── 왜 카드 SO 가 아니라 여기인가 ─────────────────────────
    /// LSO_CardSO 는 에셋이다. 거기에 유언을 쓰면 **같은 카드 여러 장이 전부 같이 바뀐다.**
    /// 늑대 카드 세 장 중 하나에 저주를 붙였는데 셋 다 저주가 되는 식이다.
    /// 에디터에서 플레이를 멈춰도 그 값이 에셋에 남는다.
    ///
    /// 그래서 유언은 손패에 놓인 **그 한 장**에 붙어야 한다.
    /// 이 컴포넌트가 그 자리다. 카드 오브젝트에 붙여두면 된다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 값을 두 곳에 두지 않는다. 양초(LSO_WillCandle)는 "지금 무슨 색인가"를 알고,
    /// 이쪽은 "이 카드에 무엇이 붙었나"를 안다. 서로 묻지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LSO_CardWill : MonoBehaviour
    {
        [Tooltip("붙는 순간 아이콘이 드러나는 연출. 비워두면 자식에서 찾는다.\n" +
                 "없어도 값은 정상적으로 붙는다 — 화면에만 안 보인다.")]
        [SerializeField] private LSO_WillRevealEffect reveal;

        /// <summary>
        /// 붙어 있는 유언. <b>HasWill 이 참일 때만 뜻이 있다.</b>
        ///
        /// 아직 안 붙였을 때도 None 이고, "유언 없음"을 붙였을 때도 None 이다.
        /// 빈 초를 고른 것과 아예 안 고른 것은 다른 상태다.
        /// </summary>
        public LSO_WillType Will { get; private set; } = LSO_WillType.None;

        /// <summary>유언이 붙어 있는지. 빈 초(없음)를 붙였어도 참이다.</summary>
        public bool HasWill { get; private set; }

        /// <summary>붙거나 지워졌을 때. 인자는 Will 과 HasWill.</summary>
        public event Action<LSO_WillType, bool> Changed;

        private void Awake()
        {
            if (reveal == null) reveal = GetComponentInChildren<LSO_WillRevealEffect>(true);
        }

        /// <summary>붙어 있는 유언을 꺼낸다. 안 붙었으면 거짓.</summary>
        public bool TryGetWill(out LSO_WillType will)
        {
            will = Will;
            return HasWill;
        }

        /// <summary>
        /// 유언을 붙인다. 아이콘이 드러나는 연출이 함께 돈다.
        /// </summary>
        /// <param name="will">붙일 유언. None 이면 "유언 없음"으로 확정된다.</param>
        /// <param name="from">불이 있던 월드 좌표. 그쪽에서부터 번진다.</param>
        public void Apply(LSO_WillType will, Vector3? from = null)
        {
            Will = will;
            HasWill = true;

            if (reveal != null) reveal.Play(will, from);

            Changed?.Invoke(Will, HasWill);
        }

        /// <summary>
        /// 붙은 것을 지운다. 카드를 다시 쓸 때(손패에서 빠졌다 돌아올 때) 부른다.
        ///
        /// 카드 오브젝트를 풀에서 돌려쓰면 지난 판의 유언이 남는다.
        /// 손패에 놓을 때 한 번 불러줄 것.
        /// </summary>
        public void Clear()
        {
            if (!HasWill && Will == LSO_WillType.None) return;

            Will = LSO_WillType.None;
            HasWill = false;

            if (reveal != null) reveal.Clear();

            Changed?.Invoke(Will, HasWill);
        }
    }
}
