using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 열린 인포창 자체를 좌클릭 또는 우클릭하면 닫기 애니메이션을 실행한다.
/// EventSystem의 PhysicsRaycaster 없이 카메라에서 직접 레이캐스트한다.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public sealed class DLJ_InfoPanelClickClose : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("닫을 인포창. 비워두면 자식 또는 DLJ_InfoPanel.Instance에서 찾는다.")]
    [SerializeField] private DLJ_InfoPanel infoPanel;
    [Tooltip("클릭 판정에 사용할 카메라. 비워두면 Main Camera를 사용한다.")]
    [SerializeField] private Camera targetCamera;
    [Tooltip("인포창을 덮는 클릭 영역. 비워두면 이 오브젝트의 BoxCollider를 사용한다.")]
    [SerializeField] private BoxCollider clickArea;

    [Header("닫기 버튼")]
    [SerializeField] private bool closeWithLeftClick = true;
    [SerializeField] private bool closeWithRightClick = true;

    private void Awake()
    {
        if (clickArea == null)
            clickArea = GetComponent<BoxCollider>();

        if (infoPanel == null)
            infoPanel = GetComponentInChildren<DLJ_InfoPanel>(true);

        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void Update()
    {
        if (Mouse.current == null || clickArea == null)
            return;

        bool leftClicked = closeWithLeftClick && Mouse.current.leftButton.wasPressedThisFrame;
        bool rightClicked = closeWithRightClick && Mouse.current.rightButton.wasPressedThisFrame;
        if (!leftClicked && !rightClicked)
            return;

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return;

        Ray ray = targetCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!clickArea.Raycast(ray, out _, targetCamera.farClipPlane))
            return;

        DLJ_InfoPanel panel = infoPanel != null ? infoPanel : DLJ_InfoPanel.Instance;
        if (panel == null)
        {
            Debug.LogWarning("[DLJ_InfoPanelClickClose] 닫을 DLJ_InfoPanel을 찾을 수 없습니다.", this);
            return;
        }

        panel.Hide();
    }
}
