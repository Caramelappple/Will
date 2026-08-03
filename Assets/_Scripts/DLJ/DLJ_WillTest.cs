using _Scripts.LDY;
using UnityEngine;
using UnityEngine.InputSystem;

public class DLJ_WillTest : MonoBehaviour
{
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!DLJ_SuccessionSystem.IsWaitingForSuccessionTarget)
            return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            Click();
    }

    private void Click()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 100f))
            return;

        LDY_Animal target = hit.collider.GetComponentInParent<LDY_Animal>();
        DLJ_SuccessionSystem.TrySelectSuccessionTarget(target);
    }
}
