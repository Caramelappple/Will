using _Scripts.LSO;
using UnityEngine;
using UnityEngine.InputSystem;

public class WillTest : MonoBehaviour
{
    private Camera mainCamera;
    
    private WillSystem firstObj;
    private WillSystem secondObj;

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
            IWillActivation will = hit.collider.GetComponent<IWillActivation>();
            Debug.Log(1111111);
            if (renderer != null)
            {
                renderer.material.color = Color.gray;
                will.WillActivate();
            }
        }
    }
}
