using UnityEngine;
using UnityEngine.UI;

public class KTH_InfoPanlCancelButton : MonoBehaviour
{
    [SerializeField]private Button cancelButton;
  

    private void OnEnable()
    {
        cancelButton.onClick.AddListener(CancelClick);
    }
    private void OnDisable()
    {
        cancelButton.onClick.RemoveListener(CancelClick);
    }

    public void CancelClick()
    {
        KTH_InfoPanl.Instance.CancleInfoPanl();
    }
}
