using System;
using System.Collections.Generic;
using UnityEngine;
using _Scripts.LSO.Ability;
using _Scripts.LSO.Ability.Catalog;

[Serializable]
public struct KTH_AbilityExplanationEntry
{
    public LSO_AbilityType Type;

    [Tooltip("이 창에서만 다르게 보여주고 싶을 때 적는다.\n" +
             "보통은 비워둔다 — 비우면 특성 사전(LSO_AbilityCatalog)의 설명이 나온다.")]
    [TextArea] public string Explanation;
}

// =========================================================
// SRP: 유언(어빌리티) 설명 텍스트 제공만 담당한다.
// OCP: 기존엔 switch (_ => type.ToString()) 하나뿐이라 새 어빌리티 설명을
// 추가하려면 코드를 고쳐야 했다. 지금은 인스펙터에 데이터(엔트리)만
// 추가하면 되고, 코드는 그대로 둔다 (확장에는 열려 있고 수정에는 닫혀 있다).
//
// 연산량 최적화: 문자열 비교 없이 Dictionary로 한 번에 조회한다 (O(1)).
//
// ── 설명의 출처가 바뀌었다 ──────────────────────────────────
// 예전에는 이 창의 인스펙터 리스트가 설명을 들고 있었고, 이름은
// LSO_DisplayNames의 switch가 따로 들고 있었다. 특성 하나를 고치려면
// 두 곳을 맞춰야 했고, 어긋났을 때 어느 쪽이 맞는지 정할 방법이 없었다.
//
// 이제 기본 출처는 특성 사전(Assets/Resources/LSO_AbilityCatalog.asset) 하나다.
// 인스펙터 리스트는 "이 창에서만 다르게 보여주고 싶을 때" 쓰는 덮어쓰기로 남는다.
// 비워두면 사전 쪽이 나오므로, 이미 채워둔 값이 있으면 그대로 동작한다.
// =========================================================
public interface IAbilityExplanationProvider
{
    string GetExplanation(LSO_AbilityType type);
}

public sealed class KTH_AbilityExplanationProvider : IAbilityExplanationProvider
{
    private readonly Dictionary<LSO_AbilityType, string> overrides;

    public KTH_AbilityExplanationProvider(IReadOnlyList<KTH_AbilityExplanationEntry> entries)
    {
        overrides = new Dictionary<LSO_AbilityType, string>(entries?.Count ?? 0);
        if (entries == null)
        {
            return;
        }
        foreach (var entry in entries)
        {
            // 빈 줄은 담지 않는다. 담으면 "빈 문자열로 덮어쓰기"가 되어
            // 사전에 적어둔 설명이 화면에서 사라진다.
            if (string.IsNullOrWhiteSpace(entry.Explanation))
            {
                continue;
            }
            overrides[entry.Type] = entry.Explanation;
        }
    }

    public string GetExplanation(LSO_AbilityType type)
    {
        // 이 창만의 문구가 있으면 그것을, 없으면 특성 사전의 설명을 쓴다.
        return overrides.TryGetValue(type, out var explanation)
            ? explanation
            : LSO_AbilityText.DescriptionOf(type);
    }
}
