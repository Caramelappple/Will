using System;
using _Scripts.LSO.CoreLib;
using _Scripts.LSO.UI.Input;
using TMPro;
using UnityEngine;

namespace _Scripts.LSO.Reward
{
    [RequireComponent(typeof(LSO_ButtonClickHandler))]
    public abstract class LSO_RewardCard : MonoBehaviour, LSO_IClickEffect, LSO_IPoolable
    {
        [Header("공통")]
        [SerializeField] private SpriteRenderer iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;

        private LSO_RewardOption _option;
        private Action<LSO_RewardCard> _onClick;

        /// <summary>이 카드가 들고 있는 보상. 상자가 지급할 때 읽는다.</summary>
        public LSO_RewardOption Option => _option;

        /// <summary>
        /// 보여줄 내용과 눌렸을 때 알릴 곳을 받는다.
        ///
        /// 콜백을 인스펙터가 아니라 인자로 받는 이유는, 풀에서 재사용되기 때문이다.
        /// 인스펙터에 걸어두면 지난번 상자에 계속 연결된 채로 돌아온다.
        /// </summary>
        public void Bind(LSO_RewardOption option, Action<LSO_RewardCard> onClick)
        {
            _option = option;
            _onClick = onClick;

            if (option == null)
            {
                Debug.LogWarning($"{name}: 보상이 비어 있어 빈 카드로 둡니다.", this);
                Clear();
                return;
            }

            Draw(option);
        }

        /// <summary>
        /// 눌렸을 때 알릴 곳만 정한다. 보상 없이 그려지는 카드가 쓴다.
        ///
        /// 유언 메모장이 그렇다 — 고르는 것이 아니라 보여주는 것이라 Option이 없는데,
        /// 눌러서 치울 수는 있어야 한다.
        /// </summary>
        protected void SetClickCallback(Action<LSO_RewardCard> onClick)
        {
            _onClick = onClick;
        }

        /// <summary>받은 보상을 화면에 옮긴다. 하위 클래스가 자기 수치를 채운다.</summary>
        protected abstract void Draw(LSO_RewardOption option);

        /// <summary>지난번 내용을 지운다. 풀에서 되살아날 때와 빈 보상이 왔을 때 불린다.</summary>
        protected abstract void Clear();

        public void OnClick()
        {
            // 이미 넘긴 뒤라면 아무것도 하지 않는다.
            // 한 번 클릭으로 확정되므로 두 번째 클릭이 들어올 틈이 짧게 있다.
            if (_onClick == null) return;

            Action<LSO_RewardCard> callback = _onClick;
            _onClick = null;

            callback(this);
        }

        protected void SetName(string value) => SetText(nameText, value);

        protected void SetDescription(string value) => SetText(descriptionText, value);

        protected static void SetText(TMP_Text label, string value)
        {
            if (label != null) label.text = value;
        }

        protected void SetIcon(Sprite sprite)
        {
            if (iconImage == null) return;

            iconImage.sprite = sprite;

            // 스프라이트가 없는데 켜두면 흰 사각형이 남는다.
            iconImage.enabled = sprite != null;
        }

        /// <summary>공통 칸을 비운다. 하위 클래스의 Clear가 먼저 불러 쓰면 된다.</summary>
        protected void ClearCommon()
        {
            SetName(string.Empty);
            SetDescription(string.Empty);
            SetIcon(null);
        }

        // 풀에서 되살아난 카드다. 지난번 보상이 남아 있으면 엉뚱한 것이 지급된다.
        public void OnSpawned()
        {
            _option = null;
            _onClick = null;
        }

        public void OnDespawned()
        {
            _option = null;
            _onClick = null;
        }
    }
}
