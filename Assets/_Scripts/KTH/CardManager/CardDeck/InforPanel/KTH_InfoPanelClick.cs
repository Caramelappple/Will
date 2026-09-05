using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class KTH_InfoPanelClick : MonoBehaviour,
    IPointerClickHandler
{
    [SerializeField]private DLJ_InfoPanel  infoPanel;

    public void OnPointerClick(PointerEventData eventData)
    {
        DLJ_InfoPanel panel = infoPanel != null ? infoPanel : DLJ_InfoPanel.Instance;


        panel?.Hide();

        KTH_HandCard.DeselectCurrent();
    }
}
