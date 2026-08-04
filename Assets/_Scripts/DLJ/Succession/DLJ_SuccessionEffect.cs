using System;
using DG.Tweening;
using UnityEngine;

public class DLJ_SuccessionEffect : MonoBehaviour
{
    private DLJ_SuccessionSystem successionSystem;
    private GameObject effectPrefab;
    private GameObject effectInstance;
    private float moveDuration;

    public void Bind(
        DLJ_SuccessionSystem system,
        GameObject prefab,
        float sourceMoveDuration)
    {
        Unbind();

        successionSystem = system;
        effectPrefab = prefab;
        moveDuration = sourceMoveDuration;

        if (successionSystem == null)
            return;

        successionSystem.OnSelectionStarted += Show;
        successionSystem.OnTargetSelected += MoveToTarget;
        successionSystem.OnSuccessionFinished += Hide;
    }

    private void Show(Vector3 position)
    {
        if (effectPrefab == null)
        {
            Debug.LogError($"{name}: Succession effect prefab is missing.", this);
            return;
        }

        if (effectInstance != null)
            Destroy(effectInstance);

        effectInstance = Instantiate(effectPrefab, position, Quaternion.identity);
        effectInstance.SetActive(true);
    }

    private void MoveToTarget(Vector3 position, Action onComplete)
    {
        if (effectInstance == null)
        {
            onComplete?.Invoke();
            return;
        }

        effectInstance.transform
            .DOMove(position, moveDuration)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    private void Hide()
    {
        if (effectInstance == null)
            return;

        effectInstance.transform.DOKill();
        Destroy(effectInstance);
        effectInstance = null;
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
