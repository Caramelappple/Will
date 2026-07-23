using _Scripts.LSO;
using UnityEngine;
using UnityEngine.InputSystem;

public class DLJ_WillTest : MonoBehaviour
{
    private Camera mainCamera;
    
    private DLJ_WillSystem firstObj;
    private DLJ_WillSystem secondObj;

    private bool succession = false;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Click();
        }
    }
    
    private void Click()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f))
        {
            Renderer renderer = hit.collider.GetComponent<Renderer>();
            DLJ_IWillActivation dljIWill = hit.collider.GetComponent<DLJ_IWillActivation>();
            Debug.Log(1111111);
            if (dljIWill == null) return;
            if (renderer != null)
            {
                renderer.material.color = Color.gray;
                dljIWill.WillActivate();
            }
        }
    }
}
