using UnityEngine;

namespace _Scripts.LSO.Attributes
{
    /// <summary>
    /// [SerializeReference] 필드에 "어떤 구현을 넣을지" 고르는 드롭다운을 붙인다.
    ///
    /// [SerializeReference]는 직렬화만 해결한다. 구체 타입 이름을 같이 저장해주지만,
    /// 유니티 기본 인스펙터에는 그 타입을 고를 UI가 없어서 값이 null인 채로 남는다.
    /// 이 속성을 붙이면 LSO_SubclassPickerDrawer가 드롭다운을 그려준다.
    ///
    /// 목록에 붙이면 요소마다 적용된다.
    ///
    /// <code>
    /// [SerializeReference, LSO_SubclassPicker]
    /// private List&lt;LDY_IActionScorer&gt; scorers = new();
    /// </code>
    ///
    /// 드롭다운에 뜨려면 구현 클래스가 이래야 한다.
    ///   - [Serializable]이 붙어 있을 것
    ///   - abstract / 제네릭이 아닐 것
    ///   - 인자 없는 생성자가 있을 것
    ///   - UnityEngine.Object를 상속하지 않을 것 (SO·MonoBehaviour는 managed reference로 못 담는다)
    /// </summary>
    public class LSO_SubclassPickerAttribute : PropertyAttribute
    {
    }
}
