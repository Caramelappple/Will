using UnityEngine;

namespace _Scripts.LSO.UI
{
    /// <summary>
    /// 클릭을 LSO_TurnLever에 넘긴다. 그 외에는 아무것도 하지 않는다.
    ///
    /// 상태를 하나도 갖지 않는 것이 이 클래스의 존재 이유다.
    /// 눌린 자리·선택 여부를 여기서도 들고 있으면 레버가 아는 것과 어긋나기 시작한다.
    ///
    /// LSO_ButtonClickHandler가 이것을 LSO_IClickEffect로 보고 불러준다.
    /// 덕분에 "상호작용 불가일 때 무시", "좌클릭만" 같은 규칙을 다시 만들 필요가 없다.
    ///
    /// 씬 배선: 플레이어 쪽에만 Collider + LSO_ButtonClickHandler 와 함께 붙인다.
    /// 적 턴은 스스로 끝나므로 적 쪽에는 필요 없다.
    /// 3D 물건이면 씬에 EventSystem과 카메라의 Physics Raycaster가 있어야 한다.
    /// </summary>
    public class LSO_TurnLeverSide : MonoBehaviour, LSO_IClickEffect
    {
        [Tooltip("비워두면 부모에서 찾는다.")]
        [SerializeField] private LSO_TurnLever lever;

        private void Awake()
        {
            if (lever == null)
                lever = GetComponentInParent<LSO_TurnLever>();

            if (lever == null)
                Debug.LogError($"{name}: LSO_TurnLever를 찾지 못했습니다. 레버의 자식으로 두거나 직접 연결하세요.", this);
        }

        public void OnClick()
        {
            if (lever == null) return;

            lever.RequestEndTurn();
        }
    }
}
