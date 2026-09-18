using System.Collections.Generic;
using System.Text;
using _Scripts.LSO.HealthSystem;
using TMPro;
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
    [Header("Investment History")]
    [SerializeField] private DLJ_LedgerInkText investmentHistory;
    [SerializeField, Min(.1f)] private float investmentFadeDuration = .85f;
    private DLJ_FoxKingBoss boundBoss;
    private Health boundHealth;
    private float nextSearchTime;
    private float nextStrokeTime;
    private readonly List<DisplayedInvestment> displayedInvestments = new();
    public DLJ_FoxKingBoss BoundBoss => boundBoss;

    private void OnEnable()
    {
        EnsureInvestmentHistory();
        ShowUnavailable();
        RefreshBoss();
    }

    private void OnDisable() => Unbind();

    private void Update()
    {
        UpdateInvestmentFade();

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
        next.OnInvestmentMade += AddInvestment;
        next.OnInvestmentTurnAdvanced += FadeFinishedTurnInvestments;
        next.OnAttackInvestmentConsumed += RemoveAttackInvestments;
        SetResources(next.StolenResources);
        SetGreed(next.Greed);
        SyncInvestments(next.ActiveInvestments);
    }

    private void Unbind()
    {
        if (boundBoss != null)
        {
            boundBoss.OnStolenResourcesChanged -= SetResources;
            boundBoss.OnGreedChanged -= SetGreed;
            boundBoss.OnInvestmentMade -= AddInvestment;
            boundBoss.OnInvestmentTurnAdvanced -= FadeFinishedTurnInvestments;
            boundBoss.OnAttackInvestmentConsumed -= RemoveAttackInvestments;
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
        displayedInvestments.Clear();
        if (investmentHistory != null) investmentHistory.SetValue("");
        nextStrokeTime = 0;
    }

    private void EnsureInvestmentHistory()
    {
        if (investmentHistory != null || !Application.isPlaying || milestoneRows.Length == 0 ||
            milestoneRows[0] == null)
            return;

        // 두 장부 프리팹이 같은 글꼴/잉크 설정을 유지하도록 기존 행을 런타임에 복제한다.
        // 원본 프리팹 구조를 각각 중복 수정하지 않아도 왼쪽 페이지에 같은 필체로 표시된다.
        investmentHistory = Instantiate(milestoneRows[0], transform);
        investmentHistory.name = "InvestmentHistory";

        if (investmentHistory.transform is RectTransform rect)
        {
            rect.anchoredPosition = new Vector2(-1.015f, .26f);
            rect.sizeDelta = new Vector2(1.55f, 1.85f);
        }

        TextMeshPro text = investmentHistory.GetComponent<TextMeshPro>();
        if (text != null)
        {
            text.text = "";
            text.alignment = TextAlignmentOptions.TopLeft;
            text.enableAutoSizing = true;
            text.fontSizeMax = 1.7f;
            text.fontSizeMin = 1.05f;
            text.lineSpacing = 9f;
            text.richText = true;
        }
    }

    private void SyncInvestments(IReadOnlyList<DLJ_FoxKingInvestmentEntry> entries)
    {
        displayedInvestments.Clear();
        foreach (DLJ_FoxKingInvestmentEntry entry in entries)
            AddInvestment(entry, false);
        RefreshInvestmentText();
    }

    private void AddInvestment(DLJ_FoxKingInvestmentEntry entry)
    {
        AddInvestment(entry, true);
    }

    private void AddInvestment(DLJ_FoxKingInvestmentEntry entry, bool refresh)
    {
        DisplayedInvestment matching = displayedInvestments.Find(candidate =>
            !candidate.IsFading && candidate.Effect == entry.Effect);

        if (matching == null)
        {
            displayedInvestments.Add(new DisplayedInvestment(entry));
        }
        else
        {
            matching.Amount += entry.Amount;
        }

        if (refresh)
            RefreshInvestmentText();
    }

    private void FadeFinishedTurnInvestments()
    {
        foreach (DisplayedInvestment entry in displayedInvestments)
            if (entry.Effect == DLJ_InvestmentEffectType.Heal)
                entry.IsFading = true;
    }

    private void RemoveAttackInvestments()
    {
        displayedInvestments.RemoveAll(entry => entry.Effect == DLJ_InvestmentEffectType.Attack);
        RefreshInvestmentText();
    }

    private void UpdateInvestmentFade()
    {
        bool changed = false;
        float fadeStep = Time.unscaledDeltaTime / Mathf.Max(.1f, investmentFadeDuration);
        foreach (DisplayedInvestment entry in displayedInvestments)
        {
            if (!entry.IsFading)
                continue;

            entry.Alpha = Mathf.Max(0f, entry.Alpha - fadeStep);
            changed = true;
        }

        if (!changed)
            return;

        displayedInvestments.RemoveAll(entry => entry.Alpha <= 0f);
        RefreshInvestmentText();
    }

    private void RefreshInvestmentText()
    {
        if (investmentHistory == null)
            return;

        if (displayedInvestments.Count == 0)
        {
            investmentHistory.SetValue("");
            return;
        }

        float titleAlpha = 0f;
        foreach (DisplayedInvestment entry in displayedInvestments)
            titleAlpha = Mathf.Max(titleAlpha, entry.Alpha);

        var builder = new StringBuilder();
        // TMP의 alpha 태그에는 닫는 태그가 없으므로 각 행에서 알파를 직접 지정한다.
        builder.Append("<alpha=#").Append(ToAlphaHex(titleAlpha))
            .Append("><align=center>투자 내역</align>");

        foreach (DisplayedInvestment entry in displayedInvestments)
        {
            string effect = entry.Effect == DLJ_InvestmentEffectType.Heal
                ? $"회복 +{entry.Amount}"
                : $"다음 공격 +{entry.Amount}";
            builder.Append("\n<alpha=#").Append(ToAlphaHex(entry.Alpha)).Append(">")
                .Append(effect);
        }

        builder.Append("<alpha=#FF>");
        investmentHistory.SetValue(builder.ToString());
    }

    private static string ToAlphaHex(float alpha)
    {
        return Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f).ToString("X2");
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

    private sealed class DisplayedInvestment
    {
        public readonly DLJ_InvestmentEffectType Effect;
        public int Amount;
        public float Alpha = 1f;
        public bool IsFading;

        public DisplayedInvestment(DLJ_FoxKingInvestmentEntry entry)
        {
            Effect = entry.Effect;
            Amount = entry.Amount;
        }
    }
}
