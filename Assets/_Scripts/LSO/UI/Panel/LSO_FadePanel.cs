using System;
using DG.Tweening;
using UnityEngine;

namespace _Scripts.LSO.UI.Panel
{
    /// <summary>
    /// 페이드 + 살짝 확대로 여닫는 창.
    /// LSO_Panel과 같은 계약(LSO_IPanel)이라 LSO_PanelSwitchButton은 그대로 쓰면 된다.
    ///
    /// 닫을 때는 연출이 끝난 뒤에야 SetActive(false)를 한다.
    /// 바로 꺼버리면 사라지는 모습이 안 보인다.
    ///
    /// 주의: 씬에 비활성 상태로 저장된 오브젝트는 Awake가 돌지 않는다.
    ///       "처음엔 닫힌 창"은 오브젝트를 켜둔 채로 두고 Open On Start를 꺼야 한다.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class LSO_FadePanel : MonoBehaviour, LSO_IPanel
    {
        [Tooltip("확대/축소할 대상. 비우면 자신.")]
        [SerializeField] private RectTransform scaleTarget;

        [Header("시간")]
        [SerializeField, Min(0f)] private float openDuration = 0.18f;
        [SerializeField, Min(0f)] private float closeDuration = 0.14f;

        [Header("크기")]
        [Tooltip("나타나기 시작할 때의 배율. 1이면 크기 변화 없이 페이드만 한다.")]
        [SerializeField, Range(0.5f, 1.5f)] private float startScale = 0.94f;

        [SerializeField] private Ease openEase = Ease.OutCubic;
        [SerializeField] private Ease closeEase = Ease.InQuad;

        [Header("동작")]
        [Tooltip("timescale 영향 여부. 일시정지 메뉴라면 켜둘 것.")]
        [SerializeField] private bool ignoreTimeScale = true;

        [Tooltip("Awake에서 아래 상태로 맞춘다. 끄면 씬에 저장된 상태를 그대로 쓴다.")]
        [SerializeField] private bool applyOnAwake = true;

        [SerializeField] private bool openOnStart;

        /// <summary>페이드가 끝난 뒤에 발생한다.</summary>
        public event Action<LSO_FadePanel> Opened;

        /// <summary>완전히 닫힌 뒤에 발생한다.</summary>
        public event Action<LSO_FadePanel> Closed;

        private CanvasGroup _canvasGroup;
        private RectTransform _scaleRect;
        private Vector3 _baseScale;
        private Sequence _sequence;
        private bool _isOpen;

        /// <summary>
        /// 열려 있는지. 닫히는 연출 중이면 이미 false다.
        /// activeSelf가 아니라 "의도"를 돌려줘야 연출 도중에 다시 눌러도 앞뒤가 맞는다.
        /// </summary>
        public bool IsOpen => _isOpen;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _scaleRect = scaleTarget != null ? scaleTarget : transform as RectTransform;
            _baseScale = _scaleRect != null ? _scaleRect.localScale : Vector3.one;

            if (applyOnAwake)
                ApplyInstant(openOnStart);
            else
                _isOpen = gameObject.activeSelf;
        }

        public void Open()
        {
            if (_isOpen) return;

            _isOpen = true;
            KillSequence();

            gameObject.SetActive(true);

            // 연출 중에는 못 누르게 막는다. 반쯤 투명한 버튼이 눌리면 이상하다.
            SetInteractable(false);

            if (openDuration <= 0f)
            {
                ApplyInstant(true);
                Opened?.Invoke(this);
                return;
            }

            _canvasGroup.alpha = 0f;
            if (_scaleRect != null)
                _scaleRect.localScale = _baseScale * startScale;

            _sequence = DOTween.Sequence()
                .Append(_canvasGroup.DOFade(1f, openDuration).SetEase(openEase));

            if (_scaleRect != null)
                _sequence.Join(_scaleRect.DOScale(_baseScale, openDuration).SetEase(openEase));

            _sequence
                .SetUpdate(ignoreTimeScale)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    _sequence = null;
                    SetInteractable(true);
                    Opened?.Invoke(this);
                });
        }

        public void Close()
        {
            if (!_isOpen)
            {
                // 이미 닫혀 있는데 또 닫으라고 하면 상태만 맞춰둔다.
                if (gameObject.activeSelf && _sequence == null)
                    gameObject.SetActive(false);
                return;
            }

            _isOpen = false;
            KillSequence();
            SetInteractable(false);

            if (closeDuration <= 0f || !gameObject.activeInHierarchy)
            {
                ApplyInstant(false);
                Closed?.Invoke(this);
                return;
            }

            _sequence = DOTween.Sequence()
                .Append(_canvasGroup.DOFade(0f, closeDuration).SetEase(closeEase));

            if (_scaleRect != null)
                _sequence.Join(_scaleRect.DOScale(_baseScale * startScale, closeDuration).SetEase(closeEase));

            _sequence
                .SetUpdate(ignoreTimeScale)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    _sequence = null;
                    // 연출이 끝난 다음에야 실제로 끈다.
                    gameObject.SetActive(false);
                    Closed?.Invoke(this);
                });
        }

        public void SetOpen(bool open)
        {
            if (open) Open();
            else Close();
        }

        public void Toggle()
        {
            SetOpen(!_isOpen);
        }

        /// <summary>연출 없이 상태만 맞춘다. 게임 시작이나 씬 초기화에 쓴다.</summary>
        private void ApplyInstant(bool open)
        {
            KillSequence();

            _isOpen = open;
            _canvasGroup.alpha = open ? 1f : 0f;

            if (_scaleRect != null)
                _scaleRect.localScale = open ? _baseScale : _baseScale * startScale;

            SetInteractable(open);
            gameObject.SetActive(open);
        }

        private void SetInteractable(bool value)
        {
            _canvasGroup.interactable = value;
            _canvasGroup.blocksRaycasts = value;
        }

        private void KillSequence()
        {
            if (_sequence == null) return;

            _sequence.Kill();
            _sequence = null;
        }

        private void OnDisable()
        {
            // 다른 코드가 강제로 꺼버린 경우. 반쯤 페이드된 채로 굳지 않게 정리한다.
            KillSequence();
            _isOpen = false;
        }
    }
}
