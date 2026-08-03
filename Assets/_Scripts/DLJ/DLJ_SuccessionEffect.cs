using System;
using DG.Tweening;
using UnityEngine;

public class DLJ_SuccessionEffect : MonoBehaviour
{
    private DLJ_SuccessionSystem successionSystem;
    private GameObject successionObject;

    public void Bind(DLJ_SuccessionSystem system, GameObject effectObject)
    {
        Unbind();

        successionSystem = system;
        successionObject = effectObject;

        if (successionObject != null)
            successionObject.SetActive(false);

        if (successionSystem == null)
            return;

        successionSystem.OnSelectionStarted += Show;
        successionSystem.OnTargetSelected += MoveToTarget;
        successionSystem.OnSuccessionFinished += Hide;
    }

    private void Show(Vector3 position)
    {
        if (successionObject == null)
            return;

        successionObject.transform.DOKill();
        successionObject.transform.position = position;
        successionObject.SetActive(true);
    }

    private void MoveToTarget(Vector3 position, Action onComplete)
    {
        if (successionObject == null)
        {
            onComplete?.Invoke();
            return;
        }

        successionObject.transform
            .DOMove(position, 1f)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    private void Hide()
    {
        if (successionObject == null)
            return;

        successionObject.transform.DOKill();
        successionObject.SetActive(false);
    }

    private void Unbind()
    {
        if (successionSystem != null)
        {
            successionSystem.OnSelectionStarted -= Show;
            successionSystem.OnTargetSelected -= MoveToTarget;
            successionSystem.OnSuccessionFinished -= Hide;
        }

        successionSystem = null;
    }

    private void OnDestroy()
    {
        Hide();
        Unbind();
    }
}
