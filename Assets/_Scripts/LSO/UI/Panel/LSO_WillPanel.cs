using System;
using System.Collections.Generic;
using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.Will;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.LSO.UI.Panel
{
    public class LSO_WillPanel : MonoBehaviour, LSO_IWillSelector
    {
        [Header("창")]
        [Tooltip("여닫을 대상. 비우면 자기 자신.\n" +
                 "LSO_FadePanel을 붙여두면 페이드로 나타난다.")]
        [SerializeField] private GameObject panelRoot;

        [Header("선택지")]
        [SerializeField] private LSO_WillOption optionPrefab;

        [Tooltip("선택지가 담길 부모. 보통 Layout Group을 붙여둔다.")]
        [SerializeField] private Transform optionContainer;

        [SerializeField] private string cardNameFormat = "{0}의 유언";

        [Header("취소")]
        [Tooltip("취소 버튼. 없어도 된다.\n" +
                 "기물은 이미 보드에 있으므로 취소해도 기본 유언으로 남는다.")]
        [SerializeField] private Button cancelButton;

        [Header("보드 입력 잠금 배경")]
        [SerializeField, Range(0f, 1f)] private float dimmedAlpha = 0.38f;
        [SerializeField, Min(0f)] private float dimFadeInDuration = 0.18f;
        [SerializeField, Min(0f)] private float dimFadeOutDuration = 0.14f;

        // 만들어둔 선택지를 재사용한다. 소환할 때마다 만들고 지우면 쓰레기가 쌓인다.
        private readonly List<LSO_WillOption> _options = new();

        private Action<LSO_WillType> _onSelected;
        private Action _onCancelled;
        private CanvasGroup _dimmer;

        public bool IsSelecting { get; private set; }

        private GameObject Root => panelRoot != null ? panelRoot : gameObject;

        private Transform Container => optionContainer != null ? optionContainer : transform;

        private void Awake()
        {
            if (optionPrefab == null)
                Debug.LogError($"{name}: 선택지 프리팹이 연결되지 않았습니다.", this);

            if (cancelButton != null)
                cancelButton.onClick.AddListener(Abort);

            // 등록을 OnEnable이 아니라 여기서 하는 이유:
            // Panel Root를 비워두면 Close()가 자기 자신을 끄는데,
            // Awake에서 꺼진 오브젝트는 OnEnable이 아예 실행되지 않아 등록이 누락된다.
            LSO_WillSelection.Register(this);
            CreateDimmer();
            LSO_WillSelection.BoardInteractionLockChanged += HandleBoardInteractionLockChanged;

            Close();
        }

        private void OnDestroy()
        {
            LSO_WillSelection.BoardInteractionLockChanged -= HandleBoardInteractionLockChanged;
            LSO_WillSelection.Unregister(this);
        }

        private void CreateDimmer()
        {
            Transform parent = transform.parent;
            if (parent == null) return;

            var dimmerObject = new GameObject("WillSelectionDimmer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            var rect = (RectTransform)dimmerObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetSiblingIndex(transform.GetSiblingIndex());

            Image image = dimmerObject.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;

            _dimmer = dimmerObject.GetComponent<CanvasGroup>();
            _dimmer.alpha = 0f;
            _dimmer.interactable = false;
            _dimmer.blocksRaycasts = false;
            dimmerObject.SetActive(false);
        }

        private void HandleBoardInteractionLockChanged(bool locked)
        {
            if (_dimmer == null) return;

            _dimmer.DOKill();
            if (locked)
            {
                _dimmer.gameObject.SetActive(true);
                _dimmer.DOFade(dimmedAlpha, dimFadeInDuration).SetUpdate(true);
                return;
            }

            _dimmer.DOFade(0f, dimFadeOutDuration)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    if (_dimmer != null)
                        _dimmer.gameObject.SetActive(false);
                });
        }

        private void OnDisable()
        {
            // 창이 꺼지는데 아직 답을 안 줬으면 호출부가 영영 기다린다.
            if (IsSelecting)
                Finish(cancelled: true, default);
        }

        public void Request(
            LSO_CardSO card,
            IReadOnlyList<LSO_WillType> options,
            Action<LSO_WillType> onSelected,
            Action onCancelled)
        {
            // 이전 요청이 남아 있으면 먼저 정리한다. 콜백 두 쌍이 살아 있으면 안 된다.
            if (IsSelecting)
                Finish(cancelled: true, default);

            _onSelected = onSelected;
            _onCancelled = onCancelled;
            IsSelecting = true;

            BuildOptions(options);

            LSO_PanelOps.Open(Root);
        }

        public void Abort()
        {
            if (!IsSelecting) return;

            Finish(cancelled: true, default);
        }

        /// <summary>
        /// 선택지를 필요한 만큼 만들고 각자에게 자기 유언을 알려준다.
        /// 남는 선택지는 지우지 않고 꺼두었다가 다음에 다시 쓴다.
        /// </summary>
        private void BuildOptions(IReadOnlyList<LSO_WillType> options)
        {
            int count = options?.Count ?? 0;

            for (int i = 0; i < count; i++)
            {
                LSO_WillOption option = GetOrCreate(i);
                if (option == null) break;

                option.Init(options[i], HandleOptionClicked);
                option.gameObject.SetActive(true);
            }

            for (int i = count; i < _options.Count; i++)
                _options[i].gameObject.SetActive(false);
        }

        private LSO_WillOption GetOrCreate(int index)
        {
            if (index < _options.Count) return _options[index];
            if (optionPrefab == null) return null;

            LSO_WillOption created = Instantiate(optionPrefab, Container);
            _options.Add(created);

            return created;
        }

        private void HandleOptionClicked(LSO_WillType willType)
        {
            if (!IsSelecting) return;

            Finish(cancelled: false, willType);
        }

        /// <summary>
        /// 콜백을 정확히 한 번만 부르고 창을 닫는다.
        /// 상태를 먼저 비우는 이유는, 콜백 안에서 또 소환이 일어나도 꼬이지 않게 하기 위해서다.
        /// </summary>
        private void Finish(bool cancelled, LSO_WillType willType)
        {
            Action<LSO_WillType> selected = _onSelected;
            Action gaveUp = _onCancelled;

            _onSelected = null;
            _onCancelled = null;
            IsSelecting = false;

            Close();

            if (cancelled) gaveUp?.Invoke();
            else selected?.Invoke(willType);
        }

        private void Close()
        {
            LSO_PanelOps.Close(Root);
        }
    }
}
