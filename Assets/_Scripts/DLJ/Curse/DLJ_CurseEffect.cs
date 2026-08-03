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

    private void Play(DLJ_CurseActivationData data)
    {
        if (data == null)
            return;

        if (effectPrefab == null)
        {
            Debug.LogError($"{name}: Curse effect prefab is missing.", this);
            return;
        }

        Vector3 position =
            data.centerWorld + Vector3.up * (effectHeight * 0.5f);
        Vector3 targetScale =
            new Vector3(data.areaSize.x, effectHeight, data.areaSize.z);

        GameObject instance = Instantiate(effectPrefab, position, Quaternion.identity);
        instance.transform.localScale = Vector3.zero;
        instance.SetActive(true);

        DLJ_CurseZone zone = instance.GetComponent<DLJ_CurseZone>();
        if (zone == null)
            zone = instance.AddComponent<DLJ_CurseZone>();

        zone.Initialize(data);

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
