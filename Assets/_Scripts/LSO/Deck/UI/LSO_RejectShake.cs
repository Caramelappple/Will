using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.LSO.Deck.UI
{
    /// <summary>
    /// 거절됐을 때 잠깐 떨리는 UI. 안내 텍스트나 카드 어디에 붙여도 된다.
    ///
    /// 스스로 판정하지 않는다. 밖에서 Shake()를 불러줄 때만 움직인다.
    /// 연출을 판정에서 떼어낸 이유는, 연출을 바꾸거나 빼도 덱 로직을 건드릴 필요가 없게 하기 위해서다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class LSO_RejectShake : MonoBehaviour
    {
        [Header("흔들기")]
        [Tooltip("흔들리는 시간(초).")]
        [SerializeField, Min(0f)] private float duration = 0.3f;

        [Tooltip("좌우로 흔들리는 폭(픽셀).")]
        [SerializeField, Min(0f)] private float strength = 14f;

        [Tooltip("떠는 횟수. 높을수록 잘게 떨린다.")]
        [SerializeField, Min(1)] private int vibrato = 12;

        [Header("색")]
        [Tooltip("떨리는 동안 물들일 대상. 비워두면 색은 건드리지 않는다.\n" +
                 "TextMeshProUGUI도 Graphic이라 그대로 넣으면 된다.")]
        [SerializeField] private Graphic tintTarget;

        [SerializeField] private Color tintColor = new(1f, 0.4f, 0.4f);

        private RectTransform _rect;

        // 흔들기 전 자리와 색. 연출 도중에 또 불려도 원래대로 돌아오게 하려고 들고 있는다.
        private Vector2 _restPosition;
        private Color _restColor;
        private bool _hasRest;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
        }

        private void OnDisable()
        {
            // 꺼지는 순간 트윈이 멈추면 어긋난 자리에 굳는다. 되돌려두고 나간다.
            Stop();
        }

        private void OnDestroy()
        {
            _rect.DOKill();

            if (tintTarget != null)
                tintTarget.DOKill();
        }

        public void Shake()
        {
            if (_rect == null) return;

            CacheRest();
            Stop();

            _rect.DOShakeAnchorPos(duration, new Vector2(strength, 0f), vibrato, 90f, false, true)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(() => _rect.anchoredPosition = _restPosition);

            if (tintTarget == null) return;

            tintTarget.color = tintColor;
            tintTarget.DOColor(_restColor, duration)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        /// <summary>
        /// 처음 흔들 때의 자리를 기준으로 삼는다.
        ///
        /// Awake에서 재지 않는 이유는 그 시점에 레이아웃이 아직 안 잡혀 있어서다.
        /// LayoutGroup이 자리를 정하기 전이라 엉뚱한 값이 찍힌다.
        /// </summary>
        private void CacheRest()
        {
            if (_hasRest) return;

            _restPosition = _rect.anchoredPosition;
            _restColor = tintTarget != null ? tintTarget.color : Color.white;
            _hasRest = true;
        }

        private void Stop()
        {
            if (_rect != null)
            {
                _rect.DOKill();

                if (_hasRest)
                    _rect.anchoredPosition = _restPosition;
            }

            if (tintTarget == null) return;

            tintTarget.DOKill();

            if (_hasRest)
                tintTarget.color = _restColor;
        }
    }
}
