using _Scripts.LSO.Deck.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.LSO.UI
{
    /// <summary>
    /// 카드 한 장을 그리기만 한다.
    /// 인스펙터로 미리 지정할 수도 있고, 런타임에 Setup으로 갈아끼울 수도 있다.
    /// </summary>
    public class LSO_CardUI : MonoBehaviour
    {
        [Header("데이터")]
        [field: SerializeField] public LSO_CardSO CardSO { get; private set; }

        [Header("표시 설정")]
        [Tooltip("특성/사거리/유언의 표시명과 카드 색 매핑표. 비워두면 enum 이름을 그대로 쓴다.")]
        [SerializeField] private LSO_CardDisplaySO display;

        [Header("UI 요소")]
        [Tooltip("이름 텍스트")]
        [SerializeField] private TextMeshProUGUI nameTxt;
        [Tooltip("설명 텍스트")]
        [SerializeField] private TextMeshProUGUI descriptionTxt;
        [Tooltip("동물 이미지")]
        [SerializeField] private Image animalImage;

        [Tooltip("소환 코스트 텍스트")]
        [SerializeField] private TextMeshProUGUI costTxt;
        [Tooltip("공격력 텍스트")]
        [SerializeField] private TextMeshProUGUI damageTxt;
        [Tooltip("체력 텍스트")]
        [SerializeField] private TextMeshProUGUI healthTxt;

        [Tooltip("특성 텍스트")]
        [SerializeField] private TextMeshProUGUI abilityTxt;
        [Tooltip("범위 텍스트")]
        [SerializeField] private TextMeshProUGUI rangeTxt;
        [Tooltip("유언 텍스트")]
        [SerializeField] private TextMeshProUGUI willTxt;

        [Tooltip("배경 이미지")]
        [SerializeField] private Image cardBackground;

        private void OnEnable()
        {
            Refresh();
        }

        /// <summary>표시할 카드를 갈아끼운다. 손패를 다시 그릴 때 호출할 것.</summary>
        public void Setup(LSO_CardSO card)
        {
            if (card == null || !card.IsValid)
            {
                Debug.LogWarning($"{name}: 표시할 카드 데이터가 비어 있습니다.", this);
                return;
            }

            CardSO = card;
            Refresh();
        }

        /// <summary>현재 카드 값으로 다시 그린다. 카드가 없으면 아무것도 하지 않는다.</summary>
        public void Refresh()
        {
            if (CardSO == null || !CardSO.IsValid) return;

            SetText(nameTxt, CardSO.AnimalName);
            SetText(descriptionTxt, CardSO.Description);
            SetSprite(animalImage, CardSO.Image);

            SetText(costTxt, CardSO.Cost.ToString());
            SetText(damageTxt, CardSO.Damage.ToString());
            SetText(healthTxt, CardSO.MaxHealth.ToString());

            SetText(abilityTxt, display != null ? display.GetAbilityName(CardSO.Ability) : CardSO.Ability.ToString());
            SetText(rangeTxt, display != null ? display.GetRangeName(CardSO.Range) : CardSO.Range.ToString());
            SetText(willTxt, display != null ? display.GetWillName(CardSO.WillType) : CardSO.WillType.ToString());

            if (cardBackground != null && display != null)
                cardBackground.color = display.GetWillColor(CardSO.WillType);
        }

        private static void SetText(TextMeshProUGUI target, string value)
        {
            if (target != null)
                target.text = value;
        }

        private static void SetSprite(Image target, Sprite value)
        {
            if (target == null) return;

            target.sprite = value;
            target.enabled = value != null;
        }
    }
}
