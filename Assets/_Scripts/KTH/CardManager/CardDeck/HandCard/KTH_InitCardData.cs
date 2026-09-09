using _Scripts.LSO.Deck.Data;
using GLTFast.Schema;
using TMPro;
using UnityEngine;

// KTH_HandCard에서 옮겨온 카드 비주얼(아웃라인/코스트 텍스트) 설정 담당.
public class KTH_InitCardData : MonoBehaviour
{
    [Header("Card Visual")]
    [SerializeField]private TextMeshPro cardName;

    [SerializeField] private SpriteRenderer cardImage;
    [SerializeField] private TextMeshPro atkText;
    [SerializeField]private TextMeshPro hpText;
    [SerializeField]private TextMeshPro abillityText;
    [SerializeField] private SpriteRenderer outlineImage;
    [SerializeField] private TextMeshPro cost;

    public void SettingUi(LSO_CardSO cardData)
    {
        if (cardData == null)
        {
            return;
        }

        InitCard(cardData);
    }

    public void InitCard(LSO_CardSO cardData)
    {
        if(cardName)
            cardName.text = cardData.name;

        if (cardImage)
            cardImage.sprite = cardData.Image;

        if (abillityText)
            abillityText.text = string.Join(",",cardData);

        if(atkText)
            atkText.text =  $"{cardData.Animal.damage}";

        if(hpText)
            hpText.text =  $"{cardData.Animal.maxHealth}";

        if(cost)
            cost.text = $"{cardData.Animal.cost}";

        if (outlineImage)
            outlineImage.gameObject.SetActive(false);
    }

    public void SetOutlineVisible(bool visible)
    {
        if (outlineImage != null)
        {
            outlineImage.gameObject.SetActive(visible);
        }
    }

    public void ResetForPool()
    {
        if (outlineImage != null)
        {
            outlineImage.gameObject.SetActive(false);
        }

        ResetRendererAlpha(outlineImage);
    }

    private static void ResetRendererAlpha(SpriteRenderer renderer)
    {
        if (renderer == null)
        {
            return;
        }

        Color color = renderer.color;
        color.a = 1f;
        renderer.color = color;
    }
}
