using DG.Tweening;
using UnityEngine;

[AddComponentMenu("")]
public sealed class DLJ_CurseEffect : MonoBehaviour
{
    public static void Play(
        DLJ_CurseActivationData data,
        GameObject effectPrefab,
        float expandTime,
        float effectHeight)
    {
        if (data == null)
            return;

        if (effectPrefab == null)
        {
            Debug.LogError("Curse effect prefab is missing.");
            return;
        }

        Vector3 position = data.centerWorld + Vector3.up * (effectHeight * 0.5f);
        Vector3 targetScale = new Vector3(data.areaSize.x, effectHeight, data.areaSize.z);
        GameObject instance = Object.Instantiate(effectPrefab, position, Quaternion.identity);
        instance.transform.localScale = Vector3.zero;
        instance.SetActive(true);

        DLJ_CurseZone zone = instance.GetComponent<DLJ_CurseZone>();
        if (zone == null)
            zone = instance.AddComponent<DLJ_CurseZone>();

        zone.Initialize(data);
        instance.transform.DOScale(targetScale, expandTime).SetEase(Ease.Linear);
    }
}
