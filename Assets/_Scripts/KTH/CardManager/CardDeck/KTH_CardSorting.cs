using UnityEngine;

public class KTH_CardSorting : MonoBehaviour
{
    private int originalSiblingIndex;
    private bool hasOriginalSiblingIndex;

    public void BringToFront()
    {
        if (!hasOriginalSiblingIndex)
        {
            originalSiblingIndex =
                transform.GetSiblingIndex();

            hasOriginalSiblingIndex = true;
        }

        transform.SetAsLastSibling();
    }

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

            int targetIndex =
                Mathf.Clamp(
                    originalSiblingIndex,
                    0,
                    maxSiblingIndex
                );

            transform.SetSiblingIndex(
                targetIndex
            );
        }

        hasOriginalSiblingIndex = false;
    }

    public bool IsFront =>
        transform.parent != null &&
        transform.GetSiblingIndex() ==
        transform.parent.childCount - 1;

    private void OnDisable()
    {
        RestoreSorting();
    }
}