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

        [Header("표시 우선도")]
        [Tooltip("0이면 우선도 없음 — 지금까지처럼 다른 특성과 나란히 표시된다.\n" +
                 "\n" +
                 "1 이상이면 **이 특성 하나만 표시된다.** 나머지는 감춘다.\n" +
                 "특성이 예닐곱 개씩 붙는 보스에서 무엇이 중요한지 안 보이는 것을 막는다.\n" +
                 "\n" +
                 "한 기물에 우선도 가진 특성이 여럿이면 **큰 값이 이긴다.**\n" +
                 "같으면 기물이 들고 있는 순서에서 앞선 것이 이긴다.")]
        [Min(0)] public int priority;

        [Header("발동 연출")]
        [Tooltip("발동했을 때 기물 자리에 띄울 프리팹. 비워두면 아무것도 안 뜬다.\n" +
                 "\n" +
                 "안에 있는 ParticleSystem 을 전부 재생하고, 제일 오래 사는 것에 맞춰\n" +
                 "알아서 치운다. 파티클을 더 넣어도 손볼 것이 없다.")]
        public GameObject effectPrefab;

        [Tooltip("기물 발밑에서 얼마나 띄울지. 가시처럼 몸통에서 나는 것은 올리고,\n" +
                 "바닥에 퍼지는 것은 0으로 둔다.")]
        public float effectHeight;

        /// <summary>이름이 비어 있으면 enum 이름으로 대신한다.</summary>
        public string ResolvedName =>
            string.IsNullOrWhiteSpace(displayName) ? type.ToString() : displayName;

        /// <summary>표시할 만한 알맹이가 있는지. 자리만 잡아둔 빈 줄을 걸러낸다.</summary>
        public bool HasText =>
            !string.IsNullOrWhiteSpace(displayName) || !string.IsNullOrWhiteSpace(description);
    }
}
