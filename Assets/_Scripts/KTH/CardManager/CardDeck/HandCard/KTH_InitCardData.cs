using System.Collections.Generic;
using _Scripts.LSO.Ability;
using _Scripts.LSO.Ability.Catalog;
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

    /// <summary>
    /// 카드 앞면을 채운다.
    ///
    /// ── 동물이 안 꽂힌 카드 ───────────────────────────────────
    /// 값은 전부 LSO_CardSO 를 거쳐 읽는다. 예전에는 cardData.Animal.damage 처럼
    /// 두 단계를 뚫고 들어갔는데, 동물이 안 꽂힌 카드가 손패에 오면 거기서 터졌다.
    ///
    /// 덱을 눌러 뽑던 때에는 안 터졌다. 사람이 그 카드를 뽑지 않았을 뿐이다.
    /// 판마다 자동으로 다섯 장을 받게 되면서 결국 걸렸다.
    ///
    /// 감싼 접근자는 IsValid 를 보고 0 과 빈 문자열을 돌려준다. 터지지는 않지만
    /// 0/0 짜리 카드가 조용히 손에 들어오는 것도 고장이므로 경고를 남긴다.
    /// ─────────────────────────────────────────────────────────
    /// </summary>
    public void InitCard(LSO_CardSO cardData)
    {
        // SettingUi 를 거치지 않고 바로 부를 수도 있어서 여기서도 막는다.
        if (cardData == null)
        {
            Debug.LogWarning($"[{name}] 카드 데이터 없이 InitCard 가 불렸습니다.", this);
            return;
        }

        if (!cardData.IsValid)
        {
            Debug.LogWarning(
                $"[{name}] 카드 '{cardData.name}' 에 동물 데이터가 꽂혀 있지 않습니다. " +
                "카드 SO 의 Animal 을 채워주세요. 그때까지 능력치는 0 으로 표시됩니다.",
                cardData);
        }

        // 이름은 동물 쪽 한글 이름을 쓴다. 비어 있으면 에셋 이름으로 버틴다 —
        // 아직 이름을 안 채운 기물이 있어서, 빈 칸보다는 무엇인지 보이는 편이 낫다.
        if (cardName)
            cardName.text = string.IsNullOrEmpty(cardData.AnimalName)
                ? cardData.name
                : cardData.AnimalName;

        if (cardImage)
            cardImage.sprite = cardData.Image;

        if (abillityText)
            abillityText.text = DescribeAbilities(cardData);

        if (atkText)
            atkText.text = $"{cardData.Damage}";

        if (hpText)
            hpText.text = $"{cardData.MaxHealth}";

        if (cost)
            cost.text = $"{cardData.Cost}";

        if (outlineImage)
            outlineImage.gameObject.SetActive(false);
    }

    /// <summary>
    /// 특성 칸에 적을 말.
    ///
    /// 예전에는 string.Join(",", cardData) 였다. 카드 SO 는 목록이 아니라서
    /// 이 호출은 에셋 파일 이름 하나를 그대로 내놓았다 — 카드에 "Wolf-card" 라고
    /// 적혀 있던 것이 그것이다.
    ///
    /// 이름을 정하는 곳은 LSO_AbilityText 하나다. 여기서 enum 이름을 그대로 쓰면
    /// 사전에서 한글 이름을 고쳐도 카드만 영문으로 남는다.
    /// </summary>
    private static string DescribeAbilities(LSO_CardSO cardData)
    {
        IReadOnlyList<LSO_AbilityType> types = cardData.AbilityTypes;

        if (types == null || types.Count == 0) return string.Empty;

        var names = new List<string>(types.Count);

        for (int i = 0; i < types.Count; i++)
        {
            if (types[i] == LSO_AbilityType.None) continue;

            names.Add(LSO_AbilityText.NameOf(types[i]));
        }

        return string.Join(", ", names);
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
