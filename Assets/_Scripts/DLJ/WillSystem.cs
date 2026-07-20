using System;
using _Scripts.LSO;
using UnityEngine;
using UnityEngine.InputSystem;

public class WillSystem : MonoBehaviour, IWillActivation
{
    [SerializeField] private LSO_AnimalSO AnimalSo;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();

            Ray ray = mainCamera.ScreenPointToRay(mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f))
            {
                Renderer renderer = hit.collider.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = Color.gray;
                    WillActivate();
                }
            }
        }
    }


    public void WillActivate()
    {
        switch (AnimalSo.willType)
        {
            case LSO_WillType.Curse:
                Debug.Log("Curse Activated");
                break;
            case LSO_WillType.Rage:
                Debug.Log("Rage Activated");
                break;
            case LSO_WillType.Succession:
                Debug.Log("Succession Activated");
                break;
        }
    }
}
