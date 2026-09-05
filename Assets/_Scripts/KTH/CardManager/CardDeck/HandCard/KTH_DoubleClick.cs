using UnityEngine;

// DLJ_InfoPanel(Assets/_Scripts/DLJ/UI/InfoPanel)이 인포 패널의 데이터 채우기 +
// 올라오는/내려가는 애니메이션(DLJ_InfoPanelAnimation)을 이미 전부 갖고 있다.
// 여기서는 그 공개 API(Instance, Show, Hide)만 불러서 쓰고, DLJ 쪽 파일은 건드리지 않는다.
public class KTH_DoubleClick : MonoBehaviour
{
    private void OnEnable()
    {
        KTH_HandCardDoubleClickController.OnCardDoubleClicked += InfoPanelActivated;
        KTH_HandCardDoubleClickController.OnCardDoubleClickCancelled += InfoPanelDeactivated;
    }

    private void OnDisable()
    {
        KTH_HandCardDoubleClickController.OnCardDoubleClicked -= InfoPanelActivated;
        KTH_HandCardDoubleClickController.OnCardDoubleClickCancelled -= InfoPanelDeactivated;
    }

    private void InfoPanelActivated(KTH_HandCard card)
    {
        if (DLJ_InfoPanel.Instance == null)
        {
            Debug.LogWarning($"[{nameof(KTH_DoubleClick)}] 씬에서 DLJ_InfoPanel.Instance를 찾을 수 없습니다.", this);
            return;
        }

        if (card == null || card.CardData == null)
        {
            Debug.LogWarning($"[{nameof(KTH_DoubleClick)}] 더블클릭한 카드의 CardData가 없습니다.", this);
            return;
        }

        DLJ_InfoPanel.Instance.Show(card.CardData);
    }

    private void InfoPanelDeactivated(KTH_HandCard card)
    {
        DLJ_InfoPanel.Instance?.Hide();
    }
}
