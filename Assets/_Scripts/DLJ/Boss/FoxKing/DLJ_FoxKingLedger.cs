using _Scripts.LSO.HealthSystem;
using UnityEngine;

/// <summary>같은 씬에 생성되는 여우왕을 찾아 장부의 실제 보유량을 표시.</summary>
[DisallowMultipleComponent]
public sealed class DLJ_FoxKingLedger : MonoBehaviour
{
    [SerializeField] private DLJ_FoxKingBoss foxKing;
    [SerializeField] private bool findSpawnedBoss = true;
    [SerializeField] private DLJ_LedgerInkText resources;
    [SerializeField] private DLJ_LedgerInkText greed;
    [SerializeField] private DLJ_LedgerInkText[] milestoneRows = new DLJ_LedgerInkText[0];
    [SerializeField] private DLJ_LedgerStrike[] milestoneStrikes = new DLJ_LedgerStrike[0];
    [SerializeField, Min(0)] private float strokeInterval = .10f;
    private DLJ_FoxKingBoss boundBoss;
    private Health boundHealth;
    private float nextSearchTime;
    private float nextStrokeTime;
    public DLJ_FoxKingBoss BoundBoss => boundBoss;

    private void OnEnable()
    {
        ShowUnavailable();
        RefreshBoss();
    }

    private void OnDisable() => Unbind();

    private void Update()
    {
        if (boundBoss != null && boundBoss.isActiveAndEnabled &&
            (boundHealth == null || !boundHealth.IsDestroyed)) return;
        // Also handles Unity's destroyed-object null semantics without keeping an old value visible.
        Unbind();
        ShowUnavailable();
        if (Time.unscaledTime < nextSearchTime) return;
        nextSearchTime = Time.unscaledTime + .25f;
        RefreshBoss();
    }

    private bool IsAvailable(DLJ_FoxKingBoss candidate)
    {
        if (candidate == null || !candidate.isActiveAndEnabled || candidate.gameObject.scene != gameObject.scene)
            return false;
        var health = candidate.GetComponent<Health>();
        return health == null || !health.IsDestroyed;
    }

    private void RefreshBoss()
    {
        var next = IsAvailable(foxKing) ? foxKing : null;
        if (next == null && findSpawnedBoss)
            foreach (var candidate in FindObjectsByType<DLJ_FoxKingBoss>(FindObjectsSortMode.None))
                if (IsAvailable(candidate)) { next = candidate; break; }
        if (next == null) return;
        Unbind();
        boundBoss = next;
        boundHealth = next.GetComponent<Health>();
        next.OnStolenResourcesChanged += SetResources;
        next.OnGreedChanged += SetGreed;
        SetResources(next.StolenResources);
        SetGreed(next.Greed);
    }

    private void Unbind()
    {
        if (boundBoss != null)
        {
            boundBoss.OnStolenResourcesChanged -= SetResources;
            boundBoss.OnGreedChanged -= SetGreed;
        }
        boundBoss = null;
        boundHealth = null;
    }

    private void ShowUnavailable()
    {
        if (resources != null) resources.SetValue("-");
        if (greed != null) greed.SetValue("-");
        foreach (var row in milestoneRows)
            if (row != null) row.SetValue("");
        foreach (var strike in milestoneStrikes)
            if (strike != null) strike.SetAchieved(false);
        nextStrokeTime = 0;
    }

    private void SetResources(int value)
    {
        if (resources != null) resources.SetValue(value.ToString());
    }

    private void SetGreed(int value)
    {
        if (greed != null) greed.SetValue(value.ToString());
        if (boundBoss == null) return;
        int rowIndex = 0;
        foreach (var milestone in boundBoss.GreedMilestones)
        {
            if (milestone == null) continue;
            if (rowIndex >= milestoneRows.Length) break;
            var effect = milestone.effect == DLJ_GreedEffectType.Attack ? "공격" : "최대 체력";
            if (milestoneRows[rowIndex] != null)
                milestoneRows[rowIndex].SetValue($"{milestone.threshold}  {effect} +{milestone.amount}");
            if (rowIndex < milestoneStrikes.Length && milestoneStrikes[rowIndex] != null)
            {
                var strike = milestoneStrikes[rowIndex];
                bool achieved = value >= milestone.threshold;
                if (achieved && !strike.IsAchieved)
                {
                    float delay = Mathf.Max(0, nextStrokeTime - Time.unscaledTime);
                    strike.SetAchieved(true, delay);
                    nextStrokeTime = Time.unscaledTime + delay + strike.DrawDuration + strokeInterval;
                }
                else if (!achieved) strike.SetAchieved(false);
            }
            rowIndex++;
        }
        for (int i = rowIndex; i < milestoneRows.Length; i++)
            if (milestoneRows[i] != null) milestoneRows[i].SetValue("");
        for (int i = rowIndex; i < milestoneStrikes.Length; i++)
            if (milestoneStrikes[i] != null) milestoneStrikes[i].SetAchieved(false);
    }
}
