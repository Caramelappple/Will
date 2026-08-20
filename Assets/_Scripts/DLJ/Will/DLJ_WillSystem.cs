using System.Collections;
using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LSO.Manager;
using _Scripts.LSO;
using _Scripts.LSO.Will;
using UnityEngine;

/// <summary>
/// Legacy component kept only so existing scenes do not gain a missing-script entry.
/// LDY_DeathHandler now invokes wills directly from LDY_Animal.WillType.
/// </summary>
[AddComponentMenu("")]
public sealed class DLJ_WillSystem : MonoBehaviour
{
}

/// <summary>Creates and invokes a will directly from an animal's WillType.</summary>
public static class DLJ_WillRuntime
{
    private const string DatabasePath = "DLJ/DLJ_WillDatabase";
    private static DLJ_WillDatabaseSO database;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        database = null;
    }

    public static LSO_IWill Invoke(
        LDY_Animal animal,
        LDY_BoardManager legacyBoard = null)
    {
        if (animal == null)
            return null;

        // Inherited stats last for the receiver's lifetime, but must not be
        // passed on again or remain on its deferred corpse.
        DLJ_SuccessionBonus.RemoveFrom(animal);

        if (animal.WillType == LSO_WillType.None)
            return null;

        DLJ_WillDatabaseSO willDatabase = GetDatabase();
        if (willDatabase == null)
            return null;

        DLJ_WillDataSO data = willDatabase.Get(animal.WillType);
        if (data == null)
            return null;

        if (!GameManager.HasInstance)
        {
            Debug.LogError("GameManager is missing. Will cannot be invoked.");
            return null;
        }

        GameManager gameManager = GameManager.Instance;
        LDY_TurnManager turnManager = gameManager.TurnManager;
        LDY_AttackSystem attackSystem =
            Object.FindFirstObjectByType<LDY_AttackSystem>();
        LDY_ActionPointManager actionPoints =
            Object.FindFirstObjectByType<LDY_ActionPointManager>();

        DLJ_WillContext context = new DLJ_WillContext
        {
            owner = animal.gameObject,
            animal = animal,
            board = gameManager.Board,
            turnManager = turnManager,
            attackSystem = attackSystem,
            actionPoints = actionPoints
        };

        LSO_IWill will = LSO_WillFactory.Create(animal.WillType, context, data);
        DLJ_DeathPreludeSO deathPrelude =
            (data as DLJ_ContractWillDataSO)?.deathPrelude;
        if (deathPrelude != null)
        {
            DLJ_WillPrelude.Begin(animal, deathPrelude);
            will?.InvokeWill();
        }
        else
        {
            will?.InvokeWill();
        }
        return will;
    }

    private static DLJ_WillDatabaseSO GetDatabase()
    {
        if (database == null)
            database = Resources.Load<DLJ_WillDatabaseSO>(DatabasePath);

        if (database == null)
            Debug.LogError($"Will database is missing at Resources/{DatabasePath}.");

        return database;
    }

}

/// <summary>
/// 사망과 유언 발동 사이의 짧은 정적과 문양 등장을 담당한다.
/// 문양 Sprite가 비어 있어도 동일한 위치와 크기의 빈 렌더러를 만들어,
/// 이미지가 준비된 뒤 데이터 에셋에 연결하는 것만으로 연출이 보이게 한다.
/// </summary>
public static class DLJ_WillPrelude
{
    private static DLJ_WillPreludeRunner runner;
    private static readonly List<System.Action> pendingRevealCallbacks = new();
    private static int activePreludeCount;
    private static float timeScaleBeforePrelude = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        runner = null;
        pendingRevealCallbacks.Clear();
        activePreludeCount = 0;
        timeScaleBeforePrelude = 1f;
    }

    /// <summary>
    /// 유언 로직은 원래 호출 흐름을 유지하되, 같은 프레임에 전장을 정지시킨다.
    /// 기존 유언 이펙트는 scaled time을 사용하므로 문양이 드러난 뒤 정지가 풀릴 때부터 움직인다.
    /// </summary>
    public static void Begin(
        LDY_Animal animal,
        DLJ_DeathPreludeSO settings,
        System.Action onRevealed = null)
    {
        if (animal == null || settings == null)
        {
            onRevealed?.Invoke();
            return;
        }

        EnsureRunner();
        GameObject sigil = CreateSigilPlaceholder(animal, settings);
        AcquirePause();
        runner.StartCoroutine(Animate(sigil, settings, onRevealed));
    }

    private static IEnumerator Animate(
        GameObject sigil,
        DLJ_DeathPreludeSO settings,
        System.Action onRevealed)
    {
        try
        {
            yield return WaitRealtime(settings.silenceDuration);

            if (sigil == null)
                yield break;

            Transform sigilTransform = sigil.transform;
            Vector3 targetScale = Vector3.one;
            sigilTransform.localScale = Vector3.zero;

            float duration = Mathf.Max(
                0f,
                settings.revealDuration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (sigil == null)
                    yield break;

                elapsed += Time.unscaledDeltaTime;
                float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
                sigilTransform.localScale = Vector3.LerpUnclamped(
                    Vector3.zero,
                    targetScale,
                    1f - Mathf.Pow(1f - t, 3f));
                yield return null;
            }

            if (sigil != null)
                sigilTransform.localScale = targetScale;
        }
        finally
        {
            ReleasePause(onRevealed);
        }

        if (sigil == null)
            yield break;

        yield return WaitRealtime(settings.holdDuration);

        SpriteRenderer renderer = sigil.GetComponent<SpriteRenderer>();
        Color startColor = renderer != null ? renderer.color : Color.clear;
        float fadeDuration = Mathf.Max(
            0f,
            settings.fadeDuration);
        float fadeElapsed = 0f;

        while (sigil != null && fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.unscaledDeltaTime;
            float t = fadeDuration > 0f
                ? Mathf.Clamp01(fadeElapsed / fadeDuration)
                : 1f;
            if (renderer != null)
            {
                Color color = startColor;
                color.a = Mathf.Lerp(startColor.a, 0f, t);
                renderer.color = color;
            }
            yield return null;
        }

        if (sigil != null)
            UnityEngine.Object.Destroy(sigil);
    }

    private static GameObject CreateSigilPlaceholder(
        LDY_Animal animal,
        DLJ_DeathPreludeSO settings)
    {
        GameObject sigil = new GameObject($"Will Sigil - {animal.WillType}");
        sigil.transform.position = animal.transform.position + Vector3.up * 0.01f;
        sigil.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        sigil.transform.localScale = Vector3.zero;

        SpriteRenderer renderer = sigil.AddComponent<SpriteRenderer>();
        renderer.sprite = settings.sigilSprite;
        renderer.color = settings.sigilColor;
        renderer.sortingOrder = 1;
        return sigil;
    }

    private static void EnsureRunner()
    {
        if (runner != null)
            return;

        GameObject runnerObject = new GameObject("DLJ Will Prelude Runner");
        UnityEngine.Object.DontDestroyOnLoad(runnerObject);
        runner = runnerObject.AddComponent<DLJ_WillPreludeRunner>();
    }

    private static void AcquirePause()
    {
        if (activePreludeCount == 0)
            timeScaleBeforePrelude = Time.timeScale;

        activePreludeCount++;
        Time.timeScale = 0f;
    }

    private static void ReleasePause(System.Action onRevealed)
    {
        if (onRevealed != null)
            pendingRevealCallbacks.Add(onRevealed);

        activePreludeCount = Mathf.Max(0, activePreludeCount - 1);
        if (activePreludeCount > 0)
            return;

        Time.timeScale = timeScaleBeforePrelude;

        if (pendingRevealCallbacks.Count == 0)
            return;

        System.Action[] callbacks = pendingRevealCallbacks.ToArray();
        pendingRevealCallbacks.Clear();
        foreach (System.Action callback in callbacks)
            callback?.Invoke();
    }

    private static IEnumerator WaitRealtime(float duration)
    {
        float remaining = Mathf.Max(0f, duration);
        while (remaining > 0f)
        {
            remaining -= Time.unscaledDeltaTime;
            yield return null;
        }
    }

    internal static void CancelAll()
    {
        if (activePreludeCount == 0)
            return;

        Time.timeScale = timeScaleBeforePrelude;
        activePreludeCount = 0;
        pendingRevealCallbacks.Clear();
    }
}

[AddComponentMenu("")]
public sealed class DLJ_WillPreludeRunner : MonoBehaviour
{
    private void OnDestroy()
    {
        DLJ_WillPrelude.CancelAll();
    }
}
