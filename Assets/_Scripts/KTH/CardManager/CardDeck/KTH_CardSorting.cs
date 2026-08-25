using UnityEngine;

public class KTH_CardSorting : MonoBehaviour
{
    private int originalSiblingIndex;
    private bool hasOriginalSiblingIndex;

    /// <summary>
    /// 이 카드를 다른 형제 UI보다 가장 위에 표시한다.
    /// </summary>
    public void BringToFront()
    {
        if (!hasOriginalSiblingIndex)
        {
            originalSiblingIndex = transform.GetSiblingIndex();
            hasOriginalSiblingIndex = true;
        }

        transform.SetAsLastSibling();
    }

    /// <summary>
    /// 선택이 끝나면 원래 UI 순서로 되돌린다.
    /// </summary>
    public void RestoreSorting()
    {
        if (!hasOriginalSiblingIndex)
        {
            return;
        }

        if (transform.parent != null)
        {
            int maxSiblingIndex =
                transform.parent.childCount - 1;

            int targetIndex = Mathf.Clamp(
                originalSiblingIndex,
                0,
                maxSiblingIndex
            );

            transform.SetSiblingIndex(targetIndex);
        }

        hasOriginalSiblingIndex = false;
    }

    /// <summary>
    /// 현재 카드가 맨 위에 있는지 확인한다.
    /// </summary>
    public bool IsFront =>
        transform.parent != null &&
        transform.GetSiblingIndex() ==
        transform.parent.childCount - 1;

    private void OnDisable()
    {
        RestoreSorting();
    }
}