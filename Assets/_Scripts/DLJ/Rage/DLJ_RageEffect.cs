using DG.Tweening;
using UnityEngine;

public class DLJ_RageEffect : MonoBehaviour
{
    private DLJ_RageSystem rageSystem;
    private GameObject effectPrefab;
    private float expandTime;
    private float holdTime;
    private float effectHeight;

    public void Bind(
        DLJ_RageSystem system,
        GameObject prefab,
        float sourceExpandTime,
        float sourceHoldTime,
        float sourceEffectHeight)
    {
        Unbind();

        rageSystem = system;
        effectPrefab = prefab;
        expandTime = sourceExpandTime;
        holdTime = sourceHoldTime;
        effectHeight = sourceEffectHeight;

        if (rageSystem != null)
            rageSystem.OnRageActivated += Play;
    }

    private void Play(Vector3 center, Vector3 areaSize)
    {
        if (effectPrefab == null)
        {
            Debug.LogError($"{name}: Rage effect prefab is missing.", this);
            return;
        }

        Vector3 position = center + Vector3.up * (effectHeight * 0.5f);
        Vector3 targetScale =
            new Vector3(areaSize.x, effectHeight, areaSize.z);

        GameObject instance = Instantiate(effectPrefab, position, Quaternion.identity);
        instance.transform.localScale = Vector3.zero;
        instance.SetActive(true);

        DOTween.Sequence()
            .Append(instance.transform
                .DOScale(targetScale, expandTime)
                .SetEase(Ease.Linear))
            .AppendInterval(holdTime)
            .Append(instance.transform
                .DOScale(Vector3.zero, expandTime)
                .SetEase(Ease.Linear))
            .OnComplete(() => Destroy(instance));
    }

    private void Unbind()
    {
        if (rageSystem != null)
            rageSystem.OnRageActivated -= Play;

        rageSystem = null;
    }

    private void OnDestroy()
    {
        Unbind();
    }
}
