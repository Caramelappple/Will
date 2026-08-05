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

        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
        LDY_Animal target = FindClosestAnimal(hits);

        if (target != null)
            DLJ_SuccessionSystem.TrySelectSuccessionTarget(target);
    }

    private static LDY_Animal FindClosestAnimal(RaycastHit[] hits)
    {
        LDY_Animal closestAnimal = null;
        float closestDistance = float.MaxValue;

        foreach (RaycastHit hit in hits)
        {
            LDY_Animal animal = hit.collider.GetComponentInParent<LDY_Animal>();

            if (animal == null || hit.distance >= closestDistance)
                continue;

            closestAnimal = animal;
            closestDistance = hit.distance;
        }

        return closestAnimal;
    }
}
