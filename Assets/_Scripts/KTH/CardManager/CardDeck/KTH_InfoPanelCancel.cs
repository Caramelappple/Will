using UnityEngine;
using UnityEngine.InputSystem;

public class KTH_InfoPanelCancel : MonoBehaviour
{
    private void Update()
    {
        if (KTH_InfoPanel.Instance == null)
            return;

        if (Mouse.current == null)
            return;

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            KTH_InfoPanel.Instance.CancleInfoPanl();
        }
    }
}