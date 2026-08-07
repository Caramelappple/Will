using DG.Tweening;
using UnityEngine;

[AddComponentMenu("")]
public sealed class DLJ_RageEffect : MonoBehaviour
{
    public static void Play(
        Vector3 center,
        Vector3 areaSize,
        GameObject effectPrefab,
        float expandTime,
        float holdTime,
        float effectHeight)
    {
        if (effectPrefab == null)
        {
            Debug.LogError("Rage effect prefab is missing.");
            return;
        }

        Vector3 position = center + Vector3.up * (effectHeight * 0.5f);
        Vector3 targetScale = new Vector3(areaSize.x, effectHeight, areaSize.z);
        GameObject instance = Object.Instantiate(effectPrefab, position, Quaternion.identity);
        instance.transform.localScale = Vector3.zero;
        instance.SetActive(true);

        DOTween.Sequence()
            .Append(instance.transform.DOScale(targetScale, expandTime).SetEase(Ease.Linear))
            .AppendInterval(holdTime)
            .Append(instance.transform.DOScale(Vector3.zero, expandTime).SetEase(Ease.Linear))
            .OnComplete(() => Object.Destroy(instance));
    }
}
