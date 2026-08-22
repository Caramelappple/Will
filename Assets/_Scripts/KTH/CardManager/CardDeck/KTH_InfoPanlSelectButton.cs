using UnityEngine;
using UnityEngine.UI;

public class KTH_InfoPanlSelectButton : MonoBehaviour
{
    [SerializeField] private Button selectButton;

    private void OnEnable()
    {
        selectButton.onClick.AddListener(ClickSelectButton);
    }
    private void OnDisable()
    {
        selectButton.onClick.RemoveListener(ClickSelectButton);
    }

    public void ClickSelectButton()
    {
        KTH_InfoPanl.Instance.SelectInfoPanl();
    }
}
