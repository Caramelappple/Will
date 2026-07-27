using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KTH_InfoPanelController : MonoBehaviour
{
    public GameObject panelRoot;
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text descText;
    public TMP_Text costText;
    public Button placeButton;
    public Button closeButton;

    private void Awake()
    {
        if (closeButton) closeButton.onClick.AddListener(Hide);
        Hide();
    }

    /// <summary>
    /// 카드 정보를 UI에 표시.
    /// showPlaceButton = true : 손패 카드를 눌렀을 때 (배치 버튼 노출)
    /// showPlaceButton = false : 이미 배치된 기물을 눌렀을 때 (배치 버튼 숨김)
    /// </summary>
    public void Show(KTH_CardData data, bool showPlaceButton, Action onPlace)
    {
        panelRoot.SetActive(true);

        if (iconImage) iconImage.sprite = data.icon;
        if (nameText) nameText.text = data.cardName;
        if (descText) descText.text = data.description;
        if (costText) costText.text = $"Cost {data.cost}";

        if (placeButton)
        {
            placeButton.gameObject.SetActive(showPlaceButton);
            placeButton.onClick.RemoveAllListeners();
            if (showPlaceButton && onPlace != null)
                placeButton.onClick.AddListener(() => onPlace());
        }
    }

    public void Hide()
    {
        if (panelRoot) panelRoot.SetActive(false);
    }
}
