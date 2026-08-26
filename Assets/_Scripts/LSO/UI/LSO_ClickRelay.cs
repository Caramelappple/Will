using UnityEngine;
using UnityEngine.Events;

namespace _Scripts.LSO.UI
{
    /// <summary>
    /// 클릭을 인스펙터에 걸어둔 곳으로 넘긴다. 그 외에는 아무것도 하지 않는다.
    ///
    /// LSO_ButtonClickHandler는 클릭을 감지해 LSO_IClickEffect들에게 알리는데,
    /// 그 인터페이스에는 UnityEvent로 빠져나갈 구멍이 없었다.
    /// 그래서 "클릭하면 저기를 부른다"를 하려면 매번 전용 스크립트를 하나씩 만들어야 했다.
    ///
    /// 이것 하나로 그 자리가 메워진다.
    /// 카메라 전환, 소리, 패널 열기 — 대상이 무엇이든 인스펙터에서 연결하면 된다.
    ///
    /// 상태를 갖지 않는다. 눌렸는지 여부조차 기억하지 않는다.
    /// 기억하기 시작하면 부르는 쪽이 아는 것과 어긋나기 시작한다.
    ///
    /// 씬 배선: Collider + LSO_ButtonClickHandler 와 함께 붙일 것.
    /// 3D 물건이면 씬에 EventSystem과 카메라의 Physics Raycaster가 있어야 한다.
    /// </summary>
    public class LSO_ClickRelay : MonoBehaviour, LSO_IClickEffect
    {
        [Tooltip("클릭했을 때 부를 것.\n" +
                 "예: LSO_CameraDirector.Play 를 고르고 샷 이름을 적는다.")]
        [SerializeField] private UnityEvent onClick;

        public void OnClick()
        {
            onClick?.Invoke();
        }
    }
}
