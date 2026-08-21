using UnityEngine;

namespace _Scripts.LDY.Save
{
    /// <summary>
    /// 이어할 런이 없으면 "이어하기" 버튼을 숨긴다.
    ///
    /// 버튼 자신이 아니라 별도 오브젝트에 붙여도 되게끔 대상을 필드로 받는다.
    /// 버튼에 직접 붙이고 자기 자신을 끄면 이 컴포넌트도 같이 멈춰버리기 때문이다.
    ///
    /// 씬 배선: 메인 메뉴의 아무 오브젝트(예: 버튼 부모)에 붙이고
    /// continueButton에 이어하기 버튼 오브젝트를 연결할 것.
    /// </summary>
    public class LDY_ContinueButtonVisibility : MonoBehaviour
    {
        [SerializeField] private GameObject continueButton;

        private void Start()
        {
            if (continueButton == null)
            {
                Debug.LogWarning("[LDY_ContinueButtonVisibility] continueButton이 비어 있습니다. 이어하기 버튼이 항상 보입니다.", this);
                return;
            }

            continueButton.SetActive(LDY_SaveService.Instance.HasRun);
        }
    }
}
