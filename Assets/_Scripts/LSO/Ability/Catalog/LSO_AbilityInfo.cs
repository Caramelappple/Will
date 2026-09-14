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
    public class LSO_AbilityInfo
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

        [Tooltip("기물 자리에서 얼마나 옮길지. 월드 기준이다.\n" +
                 "\n" +
                 "Y 를 올리면 몸통 높이에서 나고, 0이면 발밑에서 난다.\n" +
                 "카메라가 비스듬히 내려다보므로 Z 도 조금 만져야 맞는 경우가 많다.")]
        public Vector3 effectOffset;

        [Tooltip("이펙트를 놓을 각도(도). **프리팹에 저장된 회전은 무시하고 이 값으로 놓는다.**\n" +
                 "\n" +
                 "0,0,0 이면 안 돌린 상태다. 프리팹이 눕혀서 저장돼 있어도 세워진다.\n" +
                 "\n" +
                 "더하는 방식으로 뒀더니 프리팹 값과 상쇄돼 0이 되는 일이 있었다.\n" +
                 "적은 값이 그대로 들어가는 편이 맞추기 쉽다.")]
        public Vector3 effectRotation;

        [Tooltip("이펙트 크기. **프리팹 크기를 무시하고 이 값으로 놓는다.**\n" +
                 "\n" +
                 "1,1,1 이 기본이다. 0.5,0.5,0.5 로 하면 절반이 된다.\n" +
                 "자식 파티클도 같이 줄어든다.\n" +
                 "\n" +
                 "격자 한 칸이 1이다.")]
        public Vector3 effectScale = Vector3.one;

        /// <summary>이름이 비어 있으면 enum 이름으로 대신한다.</summary>
        public string ResolvedName =>
            string.IsNullOrWhiteSpace(displayName) ? type.ToString() : displayName;

        /// <summary>표시할 만한 알맹이가 있는지. 자리만 잡아둔 빈 줄을 걸러낸다.</summary>
        public bool HasText =>
            !string.IsNullOrWhiteSpace(displayName) || !string.IsNullOrWhiteSpace(description);
    }
}
