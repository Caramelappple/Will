using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.UI.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// 손패 카드 앞면을 채운다.
///
/// ── 보상 카드와 같은 것을 보여준다 ────────────────────────
/// 두 카드가 서로 다른 값을 보여주면, 상자에서 고를 때 본 것과 손에 들고 볼
/// 때 본 것이 달라진다. 같은 기물인데 판단 근거가 갈린다.
///
///   이름 · 그림 · 공격력 · 체력 · 코스트 · 점수 · 사거리 · 특성
///
/// 다른 점은 하나뿐이다 — 손패 카드에는 **유언 아이콘**이 있다.
/// 다만 그 아이콘은 여기서 안 건드린다. LSO_WillRevealEffect 가 제 것으로
/// 들고 있고, 붙는 순간의 연출까지 그쪽이 맡는다. 여기서 또 쥐면 같은
/// 스프라이트를 두 곳이 정하게 되고, 어긋났을 때 어느 쪽이 맞는지 알 수 없다.
/// ─────────────────────────────────────────────────────────
///
/// 칸을 하나 더 그리게 되면 LSO_RewardPieceCard 에도 같이 넣을 것.
/// </summary>
public class KTH_InitCardData : MonoBehaviour
{
    [Header("Card Visual")]
    [Tooltip("비워두면 그 칸은 건너뛴다.")]
    [SerializeField] private TextMeshPro cardName;

    [SerializeField] private SpriteRenderer cardImage;
    [SerializeField] private TextMeshPro atkText;
    [SerializeField] private TextMeshPro hpText;
    [SerializeField] private TextMeshPro cost;
    [SerializeField] private TextMeshPro pointText;
    [SerializeField] private TextMeshPro rangeText;

    [Tooltip("이 기물이 가진 특성 이름들. 쉼표로 이어 적는다.\n" +
             "\n" +
             "손패에만 있다. 들고 있는 동안 무엇을 할 기물인지 알아야\n" +
             "낼 자리를 정할 수 있다.")]
    [SerializeField] private TextMeshPro abillityText;

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
        SetText(cardName, string.IsNullOrEmpty(cardData.AnimalName)
            ? cardData.name
            : cardData.AnimalName);

        if (cardImage)
            cardImage.sprite = cardData.Image;

        SetText(atkText, $"{cardData.Damage}");
        SetText(hpText, $"{cardData.MaxHealth}");
        SetText(cost, $"{cardData.Cost}");
        SetText(pointText, $"{cardData.Point}");

        // 한글로 바꾸는 곳은 LSO_DisplayNames 하나다. enum 을 그대로 문자열로
        // 만들면 "Melee" 가 찍히고, 사거리 문구를 고쳐도 이 카드만 영문으로 남는다.
        SetText(rangeText, LSO_DisplayNames.Of(cardData.Range));

        SetText(abillityText, LSO_DisplayNames.Of(cardData.AbilityTypes));
    }

    private static void SetText(TextMeshPro label, string value)
    {
        if (label != null) label.text = value;
    }
}
