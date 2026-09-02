using System;
using System.Collections.Generic;
using UnityEngine;
using _Scripts.LSO.Ability;

[Serializable]
public struct KTH_AbilityExplanationEntry
{
    public LSO_AbilityType Type;
    [TextArea] public string Explanation;
}

// =========================================================
// SRP: 유언(어빌리티) 설명 텍스트 제공만 담당한다.
// OCP: 기존엔 switch (_ => type.ToString()) 하나뿐이라 새 어빌리티 설명을
// 추가하려면 코드를 고쳐야 했다. 지금은 인스펙터에 데이터(엔트리)만
// 추가하면 되고, 코드는 그대로 둔다 (확장에는 열려 있고 수정에는 닫혀 있다).
//
// 연산량 최적화: 문자열 비교 없이 Dictionary로 한 번에 조회한다 (O(1)).
// =========================================================
public interface IAbilityExplanationProvider
{
    string GetExplanation(LSO_AbilityType type);
}

public sealed class KTH_AbilityExplanationProvider : IAbilityExplanationProvider
{
    private readonly Dictionary<LSO_AbilityType, string> lookup;

    public KTH_AbilityExplanationProvider(IReadOnlyList<KTH_AbilityExplanationEntry> entries)
    {
        lookup = new Dictionary<LSO_AbilityType, string>(entries?.Count ?? 0);
        if (entries == null)
        {
            return;
        }
        foreach (var entry in entries)
        {
            lookup[entry.Type] = entry.Explanation;
        }
    }

    public string GetExplanation(LSO_AbilityType type)
    {
        // 기본 세팅: 데이터를 아직 안 채웠으면 enum 이름이라도 보여준다.
        return lookup.TryGetValue(type, out var explanation)
            ? explanation
            : type.ToString();
    }
}
