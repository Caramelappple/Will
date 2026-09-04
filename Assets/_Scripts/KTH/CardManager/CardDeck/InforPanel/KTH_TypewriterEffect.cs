using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

// =========================================================
// SRP: TMP 텍스트에 "한 글자씩 적히는" 연출만 담당한다.
//
// 연산량 최적화:
// 기존 방식(text.text = fullText.Substring(0, i))은 글자마다
//  1) 새 문자열을 매번 할당(GC)하고
//  2) TMP 메쉬/레이아웃을 매 글자마다 통째로 재생성한다.
// 3D 게임에서는 이 비용이 프레임마다 누적되어 부담이 크다.
//
// 대신 text는 처음 한 번만 대입하고 maxVisibleCharacters만 늘려가면
// 문자열 할당이 없고, TMP도 렌더링 범위만 조정하므로 훨씬 가볍다.
// =========================================================
public interface ITypewriterEffect : IDisposable
{
    /// <summary>
    /// 타이핑 트윈을 생성해서 반환한다. 트윈은 Pause 상태로 반환되므로
    /// 호출부에서 Sequence에 Join/Append 하는 등 재생 시점을 직접 제어한다.
    /// (여러 텍스트를 동시에 재생하려면 하나의 Sequence로 묶어야 하기 때문)
    /// </summary>
    Tween Play(TextMeshProUGUI target, string fullText, float secondsPerChar);
    void Stop();
}

public sealed class KTH_TypewriterEffect : ITypewriterEffect
{
    private Tween tween;

    public Tween Play(TextMeshProUGUI target, string fullText, float secondsPerChar)
    {
        Stop();
        if (target == null)
        {
            return null;
        }
        fullText ??= string.Empty;
        target.text = fullText;
        target.maxVisibleCharacters = 0;
        int totalChars = fullText.Length;
        if (totalChars == 0)
        {
            return null;
        }
        float duration = Mathf.Max(0.01f, totalChars * secondsPerChar);
        tween = DOVirtual
            .Int(0, totalChars, duration, count => target.maxVisibleCharacters = count)
            .SetEase(Ease.Linear)
            .Pause();
        return tween;
    }

    public void Stop()
    {
        tween?.Kill();
        tween = null;
    }

    public void Dispose()
    {
        Stop();
    }
}
