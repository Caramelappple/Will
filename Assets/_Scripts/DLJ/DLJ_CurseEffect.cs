using System;
using DG.Tweening;
using UnityEngine;

public class DLJ_CurseEffect : MonoBehaviour
{
    private DLJ_CurseSystem curseSystem;
    private GameObject effectPrefab;
    private float expandTime;
    private float effectHeight;

    public void Bind(
        DLJ_CurseSystem system,
        GameObject prefab,
        float sourceExpandTime,
        float sourceEffectHeight)
    {
        Unbind();

        curseSystem = system;
        effectPrefab = prefab;
        expandTime = sourceExpandTime;
        effectHeight = sourceEffectHeight;

        if (curseSystem != null)
            curseSystem.OnCurseActivated += Play;
    }

    private void Play(
        Vector3 center,
        Vector3 areaSize,
        Action<DLJ_CurseSystem> initializeSystem)
    {
        if (effectPrefab == null)
        {
            Debug.LogError($"{name}: Curse effect prefab is missing.", this);
            return;
        }

        Vector3 position = center + Vector3.up * (effectHeight * 0.5f);
        Vector3 targetScale =
            new Vector3(areaSize.x, effectHeight, areaSize.z);

        GameObject instance = Instantiate(effectPrefab, position, Quaternion.identity);
        instance.transform.localScale = Vector3.zero;
        instance.SetActive(true);

        DLJ_CurseSystem activeCurse = instance.GetComponent<DLJ_CurseSystem>();
        if (activeCurse == null)
        {
            Debug.LogError($"{instance.name}: CurseSystem is missing.", instance);
            Destroy(instance);
            return;
        }

        initializeSystem?.Invoke(activeCurse);

        instance.transform
            .DOScale(targetScale, expandTime)
            .SetEase(Ease.Linear);
    }

    private void Unbind()
    {
        if (curseSystem != null)
            curseSystem.OnCurseActivated -= Play;

        curseSystem = null;
    }

    private void OnDestroy()
    {
        Unbind();
    }
}
