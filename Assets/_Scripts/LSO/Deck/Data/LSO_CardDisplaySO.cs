using System;
using _Scripts.LDY;
using _Scripts.LSO.Ability;
using UnityEngine;

namespace _Scripts.LSO.Deck.Data
{
    /// <summary>
    /// enum을 화면에 보여줄 때 쓰는 표시명·설명·색 매핑표.
    /// UI가 enum.ToString()을 직접 쓰지 않게 해서, 기획 문구가 바뀌어도 코드를 고치지 않는다.
    /// 표에 없는 값은 enum 이름을 그대로 돌려주므로 비워둬도 동작은 한다.
    /// </summary>
    [CreateAssetMenu(fileName = "LSO_CardDisplaySO", menuName = "LSO/Deck/CardDisplaySO")]
    public class LSO_CardDisplaySO : ScriptableObject
    {
        [Serializable]
        public struct AbilityDisplay
        {
            public LSO_AbilityType type;
            public string displayName;
            [TextArea(2, 4)] public string description;
        }

        [Serializable]
        public struct RangeDisplay
        {
            public LDY_RangeType type;
            public string displayName;
        }

        [Serializable]
        public struct WillDisplay
        {
            public LSO_WillType type;
            public string displayName;
            public Color cardColor;
        }

        [SerializeField] private AbilityDisplay[] abilities = Array.Empty<AbilityDisplay>();
        [SerializeField] private RangeDisplay[] ranges = Array.Empty<RangeDisplay>();
        [SerializeField] private WillDisplay[] wills = Array.Empty<WillDisplay>();

        [Tooltip("유언 표에 없는 값일 때 쓸 카드 배경색.")]
        [SerializeField] private Color defaultCardColor = Color.white;

        public string GetAbilityName(LSO_AbilityType type)
        {
            return TryFindAbility(type, out AbilityDisplay entry) && !string.IsNullOrEmpty(entry.displayName)
                ? entry.displayName
                : type.ToString();
        }

        public string GetAbilityDescription(LSO_AbilityType type)
        {
            return TryFindAbility(type, out AbilityDisplay entry)
                ? entry.description
                : string.Empty;
        }

        public string GetRangeName(LDY_RangeType type)
        {
            foreach (RangeDisplay entry in ranges)
                if (entry.type == type && !string.IsNullOrEmpty(entry.displayName))
                    return entry.displayName;

            return type.ToString();
        }

        public string GetWillName(LSO_WillType type)
        {
            return TryFindWill(type, out WillDisplay entry) && !string.IsNullOrEmpty(entry.displayName)
                ? entry.displayName
                : type.ToString();
        }

        public Color GetWillColor(LSO_WillType type)
        {
            return TryFindWill(type, out WillDisplay entry)
                ? entry.cardColor
                : defaultCardColor;
        }

        private bool TryFindAbility(LSO_AbilityType type, out AbilityDisplay result)
        {
            foreach (AbilityDisplay entry in abilities)
            {
                if (entry.type != type) continue;

                result = entry;
                return true;
            }

            result = default;
            return false;
        }

        private bool TryFindWill(LSO_WillType type, out WillDisplay result)
        {
            foreach (WillDisplay entry in wills)
            {
                if (entry.type != type) continue;

                result = entry;
                return true;
            }

            result = default;
            return false;
        }
    }
}
