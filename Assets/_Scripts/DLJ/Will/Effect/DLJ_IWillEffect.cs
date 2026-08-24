using System;
using UnityEngine;

/// <summary>일반 C# 이펙트 클래스가 구현하는 공통 재생 규약.</summary>
public interface DLJ_IWillEffect
{
    void Play(
        GameObject effectObject,
        DLJ_WillEffectContext context,
        Action onComplete = null);
}
