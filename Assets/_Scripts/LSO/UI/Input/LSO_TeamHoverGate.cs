using _Scripts.LDY;
using UnityEngine;

namespace _Scripts.LSO.UI.Input
{
    /// <summary>
    /// 정해둔 팀의 기물만 호버를 받게 한다. 여닫는 것 외의 책임은 갖지 않는다.
    ///
    /// 적 기물이 커서를 따라 떠오르면 "고를 수 있는 것"으로 읽힌다.
    /// 그래서 호버 자체를 막는다 — 연출마다 팀을 확인하게 두면 그 검사가 흩어지고,
    /// 하나만 빠뜨려도 그것만 반응한다.
    ///
    /// 막는 대상은 LSO_ButtonHoverHandler다. 콜라이더는 그대로 두므로
    /// 클릭이나 다른 판정에는 영향이 없다. LSO_TurnClickGate와 같은 규칙이다.
    ///
    /// 씬 배선: 기물 프리팹에 LSO_ButtonHoverHandler 와 함께 붙일 것.
    /// </summary>
    [RequireComponent(typeof(LSO_ButtonHoverHandler))]
    [DisallowMultipleComponent]
    public class LSO_TeamHoverGate : MonoBehaviour
    {
        [Tooltip("이 팀일 때만 호버를 받는다.")]
        [SerializeField] private LDY_Team allowedTeam = LDY_Team.Player;

        [Tooltip("비우면 자신과 부모에서 찾는다.")]
        [SerializeField] private LDY_Animal animal;

        [Tooltip("켜면 매 프레임 팀을 다시 본다.\n" +
                 "\n" +
                 "LDY_Animal.team은 소환할 때 정해지는데 알려주는 신호가 없다.\n" +
                 "OnEnable에서 한 번만 보면 그 뒤에 정해진 팀을 놓친다.\n" +
                 "하는 일이 값 비교 하나라 켜두어도 부담이 없다.")]
        [SerializeField] private bool watchEveryFrame = true;

        private LSO_ButtonHoverHandler _handler;

        private void Awake()
        {
            _handler = GetComponent<LSO_ButtonHoverHandler>();

            if (animal == null) animal = GetComponentInParent<LDY_Animal>();

            if (animal == null)
                Debug.LogWarning($"{name}: LDY_Animal을 찾지 못해 팀을 볼 수 없습니다. 항상 열어둡니다.", this);
        }

        /// <summary>
        /// 볼 기물과 허용 팀을 코드로 정한다. 런타임에 붙일 때 쓴다.
        ///
        /// Awake의 GetComponentInParent로도 대개 찾아지지만, 부르는 쪽이 이미
        /// 기물을 손에 들고 있다면 그것을 그대로 주는 편이 확실하다.
        /// </summary>
        public void Configure(LDY_Animal owner, LDY_Team team)
        {
            if (owner != null) animal = owner;

            allowedTeam = team;

            Refresh();
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void Update()
        {
            if (!watchEveryFrame) return;

            Refresh();
        }

        /// <summary>
        /// 지금 팀을 보고 문을 여닫는다. 팀을 바꾼 쪽에서 직접 불러도 된다.
        ///
        /// 기물을 못 찾았으면 열어둔다. 없다는 이유로 막으면
        /// 배선을 빠뜨렸을 때 영영 반응이 없는 상태가 되어 원인을 찾기 어렵다.
        /// </summary>
        public void Refresh()
        {
            if (_handler == null) return;

            bool open = animal == null || animal.team == allowedTeam;

            // 값이 그대로면 건드리지 않는다. 매 프레임 껐다 켜면
            // 호버 핸들러의 OnDisable이 돌아 다른 것들이 함께 반응한다.
            if (_handler.enabled != open) _handler.enabled = open;
        }
    }
}
