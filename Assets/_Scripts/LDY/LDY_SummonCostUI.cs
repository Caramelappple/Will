using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.LDY
{
    // 남은 소환 코스트를 화면에 표시한다. 표시 외의 일은 하지 않는다.
    // 씬 배선: CardPlacer와 표시할 UI Text를 연결할 것.
    public class LDY_SummonCostUI : MonoBehaviour
    {
        [SerializeField] private LDY_CardPlacer cardPlacer;
        [SerializeField] private Text label;
        [SerializeField] private Color normalColor = new Color(0.4f, 0.85f, 1f);
        [SerializeField] private Color emptyColor = new Color(0.6f, 0.6f, 0.6f);

        private void OnEnable()
        {
            if (cardPlacer != null)
                cardPlacer.OnCostChanged += HandleCostChanged;
        }

        private void OnDisable()
        {
            if (cardPlacer != null)
                cardPlacer.OnCostChanged -= HandleCostChanged;
        }

        private void Start()
        {
            if (cardPlacer != null)
                HandleCostChanged(cardPlacer.CurrentCost, cardPlacer.MaxCost);
        }

        private void HandleCostChanged(int current, int max)
        {
            if (label == null) return;

            label.text = $"Cost {current}/{max}";
            label.color = current > 0 ? normalColor : emptyColor;
        }
    }
}
