using _Scripts.LDY;
using _Scripts.LSO.Ability;
using _Scripts.LSO.Will;

namespace _Scripts.LSO.UI.Text
{
    /// <summary>
    /// enum을 화면에 띄울 이름으로 바꾼다.
    ///
    /// enum 이름을 그대로 쓰면 MeleeOrthogonal이나 WillEnhancement 같은 긴 영문이 칸을 넘는다.
    /// 기물 정보창과 카드창이 같은 표를 보게 해서 두 곳의 표기가 갈리지 않게 한다.
    ///
    /// 표에 없는 값은 enum 이름을 그대로 돌려준다.
    /// 새 항목을 빠뜨려도 화면이 비지 않고, 영문이 보이면 여기에 추가하라는 신호가 된다.
    /// </summary>
    public static class LSO_DisplayNames
    {
        public static string Of(LDY_RangeType value)
        {
            return value switch
            {
                LDY_RangeType.Melee => "근접",
                LDY_RangeType.MeleeOrthogonal => "직선 근접",
                LDY_RangeType.Ranged => "원거리",
                LDY_RangeType.Jump => "도약",
                LDY_RangeType.None => "없음",
                _ => value.ToString()
            };
        }

        public static string Of(LSO_WillType value)
        {
            return value switch
            {
                LSO_WillType.Curse => "저주",
                LSO_WillType.Rage => "분노",
                LSO_WillType.Succession => "계승",
                LSO_WillType.Contract => "계약",
                LSO_WillType.Sacrifice => "희생",
                LSO_WillType.None => "없음",
                _ => value.ToString()
            };
        }

        /// <summary>
        /// 폐기된 값(Immune · Double · Test)은 일부러 넣지 않았다.
        /// 여기 적으면 [Obsolete] 경고가 나고, 어차피 붙지 않는 특성이라 화면에 나올 일도 없다.
        /// </summary>
        public static string Of(LSO_AbilityType value)
        {
            return value switch
            {
                LSO_AbilityType.None => "없음",

                LSO_AbilityType.Sturdy => "옹골참",
                LSO_AbilityType.Dodge => "날따름",
                LSO_AbilityType.Bloodlust => "피의 갈증",
                LSO_AbilityType.PackTactics => "무리 사냥",
                LSO_AbilityType.Thorns => "가시",
                LSO_AbilityType.Vengeance => "복수",
                LSO_AbilityType.Frail => "허약",
                LSO_AbilityType.CurseImmunity => "저주 면역",

                LSO_AbilityType.Evolve => "진화",
                LSO_AbilityType.AllHeal => "전체 치유",
                LSO_AbilityType.LifeSteal => "흡혈",
                LSO_AbilityType.CostRefund => "코스트 환급",
                LSO_AbilityType.WillEnhancement => "유언 강화",

                LSO_AbilityType.Predation => "포식",
                LSO_AbilityType.MemoryFrenzy => "기억 폭주",
                LSO_AbilityType.PreyMarking => "사냥감 물색",

                _ => value.ToString()
            };
        }
    }
}
