using _Scripts.LSO.Deck.Data;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class KTH_SelectCardUi :
    MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler
{
    [SerializeField] private Image cardImage;

    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;

    private LSO_CardSO cardData;

    private Transform originalParent;
    private bool droppedSuccessfully;
    private bool isPendingDestroy;

    public bool IsInInventory { get; private set; } = false;
    public LSO_CardSO CardData => cardData;
    public int OriginalPageIndex { get; private set; }

    private void Awake()
    {
        rectTransform = transform as RectTransform;

        canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas = canvas.rootCanvas;
        }

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void Setup(LSO_CardSO card, int pageIndex = 0, float spawnDelay = 0f, bool playAnimation = false)
    {
        cardData = card;
        OriginalPageIndex = pageIndex;

        Debug.Log($"[Setup] card={card?.name}, image={card?.Image}, cardImageRef={cardImage}");

        if (cardImage != null && cardData != null)
        {
            cardImage.sprite = cardData.Image;
        }
        originalParent = transform.parent;
        IsInInventory = false;
        isPendingDestroy = false;
        droppedSuccessfully = false;

        if (playAnimation)
        {
            PlayFlipInAnimation(spawnDelay);
        }
        else
        {
            transform.DOKill();
            transform.localRotation = Quaternion.identity;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        droppedSuccessfully = false;
        isPendingDestroy = false;

        if (canvas != null)
        {
            transform.SetParent(canvas.transform);
        }

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
        }

        transform.DOKill();
        transform.localRotation = Quaternion.identity;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rectTransform != null)
        {
            rectTransform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        if (isPendingDestroy)
        {
            Destroy(gameObject);
            return;
        }

        if (!droppedSuccessfully)
        {
            ReturnToOriginalPosition();
        }
    }

    public void MarkDroppedSuccess()
    {
        droppedSuccessfully = true;
    }

    public void MarkForDestruction()
    {
        droppedSuccessfully = true;
        isPendingDestroy = true;
        transform.DOKill();
        gameObject.SetActive(false);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (transform.parent != null)
        {
            ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.dropHandler);
        }
    }

    public void MoveToInventory(Transform inventoryParent)
    {
        droppedSuccessfully = true;
        IsInInventory = true;
        transform.SetParent(inventoryParent, false);
        originalParent = inventoryParent;

        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.one;
            rectTransform.anchoredPosition = Vector2.zero;
        }
        transform.localRotation = Quaternion.identity;
    }

    public void ReturnToOriginalPosition()
    {
        droppedSuccessfully = false;

        if (originalParent != null)
        {
            transform.SetParent(originalParent, false);

            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one;
                rectTransform.anchoredPosition = Vector2.zero;
            }
            transform.localRotation = Quaternion.identity;
        }
    }

    public void PlayFlipInAnimation(float delay = 0f)
    {
        transform.DOKill();
        transform.localRotation = Quaternion.Euler(0, 90f, 0);
        transform.DORotate(Vector3.zero, 0.25f)
            .SetDelay(delay)
            .SetEase(Ease.OutBack);
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }
}