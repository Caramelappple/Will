/// <summary>코스트 표시 값이 바뀌는 이유와 사용할 연출 정책.</summary>
public enum DLJ_CostVisualTransition
{
    /// <summary>턴 전환처럼 연출 없이 즉시 상태만 맞춘다.</summary>
    Immediate,

    /// <summary>플레이어가 사용한 코인은 소비 효과로 지우고, 늘어난 코인은 진입시킨다.</summary>
    Spend,

    /// <summary>플레이어 턴 충전처럼 늘어난 코인만 진입시킨다.</summary>
    Refill
}
