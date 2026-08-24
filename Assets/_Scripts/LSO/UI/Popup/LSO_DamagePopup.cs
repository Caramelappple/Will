using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace _Scripts.LSO.UI.Popup
{
    /// <summary>
    /// 팝업 한 장의 연출만 담당한다.
    /// 언제 어디에 뜰지는 모르며, 풀에 돌려보내는 일도 콜백으로 넘긴다.
    ///
    /// 프리팹 구성: RectTransform + CanvasGroup + 자식에 TMP_Text
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public class LSO_DamagePopup : MonoBehaviour
    {
        [Tooltip("비워두면 자식에서 찾는다.")]
        [SerializeField] private TMP_Text label;

        [Header("이동")]
        [Tooltip("위로 떠오르는 거리(px).")]
        [SerializeField, Min(0f)] private float riseDistance = 70f;

        [Tooltip("좌우로 흩어지는 폭(px). 같은 자리에 여러 개가 겹쳐 뜨는 걸 막는다.")]
        [SerializeField, Min(0f)] private float horizontalSpread = 28f;

        [Header("시간")]
        [SerializeField, Min(0.05f)] private float duration = 0.85f;

        [Tooltip("전체 시간 중 사라지기 시작하는 지점(0~1).")]
        [SerializeField, Range(0f, 1f)] private float fadeStart = 0.55f;

        [Header("크기")]
        [Tooltip("튀어나올 때 잠깐 커지는 배율.")]
        [SerializeField, Min(1f)] private float punchScale = 1.35f;

        [SerializeField, Min(0.01f)] private float punchDuration = 0.15f;

        [SerializeField] private Ease riseEase = Ease.OutCubic;

        [Tooltip("timescale 영향 여부. 일시정지 중에도 보이게 하려면 켠다.")]
        [SerializeField] private bool ignoreTimeScale = true;

        private RectTransform _rect;
        private CanvasGroup _canvasGroup;
        private Vector3 _baseScale;
        private Sequence _sequence;
        private Action<LSO_DamagePopup> _onFinished;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _canvasGroup = GetComponent<CanvasGroup>();
            _baseScale = _rect.localScale;

            if (label == null)
                label = GetComponentInChildren<TMP_Text>(true);

            if (label == null)
                Debug.LogWarning($"{name}: TMP_Text를 찾지 못했습니다.", this);
        }

        /// <param name="onFinished">연출이 끝났을 때. 풀에 돌려보내는 용도.</param>
        public void Play(Vector2 anchoredPosition, string text, Color color,
                         Action<LSO_DamagePopup> onFinished)
        {
            _onFinished = onFinished;

            KillSequence();

            if (label != null)
            {
                label.text = text;
                label.color = color;
            }

            // 좌우로 조금씩 흩뿌려서 연타 시 숫자가 완전히 겹치지 않게 한다.
            float offsetX = horizontalSpread > 0f
                ? UnityEngine.Random.Range(-horizontalSpread, horizontalSpread)
                : 0f;

            _rect.anchoredPosition = anchoredPosition + new Vector2(offsetX, 0f);
            _rect.localScale = _baseScale;
            _canvasGroup.alpha = 1f;

            float fadeDelay = duration * fadeStart;
            float fadeDuration = Mathf.Max(0.01f, duration - fadeDelay);

            _sequence = DOTween.Sequence()
                .Append(_rect.DOScale(_baseScale * punchScale, punchDuration).SetEase(Ease.OutBack))
                .Append(_rect.DOScale(_baseScale, punchDuration).SetEase(Ease.InQuad))
                // Join은 시퀀스 시작 기준이라 위 두 개와 동시에 진행된다.
                .Join(_rect.DOAnchorPosY(_rect.anchoredPosition.y + riseDistance, duration)
                    .SetEase(riseEase))
                .Insert(fadeDelay, _canvasGroup.DOFade(0f, fadeDuration).SetEase(Ease.InQuad))
                .SetUpdate(ignoreTimeScale)
                .SetLink(gameObject)
                .OnComplete(Finish);
        }

        private void Finish()
        {
            _sequence = null;

            Action<LSO_DamagePopup> callback = _onFinished;
            _onFinished = null;
            callback?.Invoke(this);
        }

        private void KillSequence()
        {
            if (_sequence == null) return;

            _sequence.Kill();
            _sequence = null;
        }

        private void OnDisable()
        {
            // 풀로 돌아갈 때 트윈이 남아 있으면 다음 재생과 섞인다.
            KillSequence();
            _onFinished = null;
        }
    }
}
