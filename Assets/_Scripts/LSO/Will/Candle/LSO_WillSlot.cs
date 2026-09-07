using System;
using _Scripts.LSO.UI.Input;
using UnityEngine;

namespace _Scripts.LSO.Will.Candle
{
    /// <summary>
    /// 촛대에 꽂힌 초 하나. 눌리면 알려주고, 골라졌는지를 보여준다.
    ///
    /// 자기가 골라졌는지 기억하지 않는다. 그 값은 LSO_WillRack 하나만 들고 있고,
    /// 여기는 촛대가 시키는 대로 보여주기만 한다.
    ///
    /// 자리도 정하지 않는다. 배치는 촛대가 계산한다.
    ///
    /// 씬 배선: Collider + LSO_ButtonClickHandler 와 함께 붙일 것.
    /// </summary>
    [RequireComponent(typeof(LSO_ButtonClickHandler))]
    public sealed class LSO_WillSlot : MonoBehaviour, LSO_IClickEffect
    {
        [Tooltip("초의 겉모습. 비워두면 자식에서 찾는다.")]
        [SerializeField] private LSO_WillCandleView view;

        private Action<LSO_WillSlot> _onClick;

        /// <summary>이 자리에 놓인 유언. 빈 초면 None.</summary>
        public LSO_WillType Will { get; private set; } = LSO_WillType.None;

        private void Awake()
        {
            if (view == null) view = GetComponentInChildren<LSO_WillCandleView>(true);

            if (view == null)
                Debug.LogError($"{name}: LSO_WillCandleView가 없어 초를 그릴 수 없습니다.", this);
        }

        /// <summary>
        /// 어떤 유언을 놓을지와 눌렸을 때 알릴 곳을 받는다.
        ///
        /// 콜백을 인스펙터가 아니라 인자로 받는 이유는 슬롯이 매번 새로 만들어지기 때문이다.
        /// 인스펙터에 걸어두면 지난 전투의 촛대에 연결된 채로 남는다.
        ///
        /// None을 넘기면 불이 꺼진 채로 선다. 그것이 "유언 없음" 초다.
        /// </summary>
        public void Bind(LSO_WillType will, Action<LSO_WillSlot> onClick)
        {
            Will = will;
            _onClick = onClick;

            if (view != null) view.Show(will);

            SetSelected(false);
        }

        /// <summary>골라진 상태를 보여준다. 값을 정하는 것은 촛대다.</summary>
        public void SetSelected(bool on)
        {
            if (view != null) view.SetHighlighted(on);
        }

        public void OnClick()
        {
            // 초는 여러 번 눌릴 수 있다. 같은 것을 다시 누르면 촛대가 선택을 푼다.
            // 그래서 보상 카드와 달리 콜백을 비우지 않는다.
            _onClick?.Invoke(this);
        }
    }
}
