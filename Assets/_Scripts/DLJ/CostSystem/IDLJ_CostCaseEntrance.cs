using System;

/// <summary>케이스 진입 연출과 코인 진입 연출 사이의 완료 신호 계약.</summary>
public interface IDLJ_CostCaseEntrance
{
    bool IsPlaying { get; }

    /// <summary>
    /// 케이스가 제자리에 없는지. 나가는 중이거나 이미 나가 있으면 참이다.
    ///
    /// IsPlaying 만으로는 모자란다. 그것은 "지금 들어오는 중"이라는 뜻이라,
    /// 케이스가 나가 있는 동안에는 거짓이다. 그 사이에 코인 연출이 돌면
    /// 케이스가 없는 자리로 금화가 쏟아진다.
    /// </summary>
    bool IsAway { get; }

    event Action Completed;
}
