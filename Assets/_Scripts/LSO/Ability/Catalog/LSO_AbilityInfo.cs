using System;
using UnityEngine;

namespace _Scripts.LSO.Ability.Catalog
{
    /// <summary>
    /// 특성 하나를 화면에 어떻게 보여줄지. 순수 데이터다.
    ///
    /// 효과는 여기 없다. 그것은 LSO_ISpecialAbility 구현이 안다.
    /// 이쪽은 "플레이어에게 뭐라고 적어줄 것인가"만 담는다.
    /// </summary>
    [Serializable]
    public struct LSO_AbilityInfo
    {
        [Tooltip("어떤 특성에 대한 설명인지.")]
        public LSO_AbilityType type;

        [Tooltip("화면에 띄울 이름. 비우면 enum 이름이 그대로 나온다.\n" +
                 "예: 옹골참, 피의 갈증")]
        public string displayName;

        [Tooltip("무슨 일이 일어나는지 한두 문장으로.\n" +
                 "\n" +
                 "숫자를 적을 때는 코드의 실제 값과 맞출 것. 여기는 표시용이라\n" +
                 "적힌 값과 실제 효과가 어긋나도 아무도 경고해주지 않는다.")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("특성 아이콘. 없으면 비워둔다. 쓰는 쪽이 null을 처리한다.")]
        public Sprite icon;

        /// <summary>이름이 비어 있으면 enum 이름으로 대신한다.</summary>
        public string ResolvedName =>
            string.IsNullOrWhiteSpace(displayName) ? type.ToString() : displayName;

        /// <summary>표시할 만한 알맹이가 있는지. 자리만 잡아둔 빈 줄을 걸러낸다.</summary>
        public bool HasText =>
            !string.IsNullOrWhiteSpace(displayName) || !string.IsNullOrWhiteSpace(description);
    }
}
