using System;

/// <summary>케이스 진입 연출과 코인 진입 연출 사이의 완료 신호 계약.</summary>
public interface IDLJ_CostCaseEntrance
{
    bool IsPlaying { get; }
    event Action Completed;
}
