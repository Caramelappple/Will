namespace _Scripts.LDY.AI
{
    /// <summary>
    /// 결정된 행동을 실제로 실행했을 때 관측된 결과.
    /// LDY_MoveSystem.MoveTo와 LDY_AttackSystem.Attack은 void라 자체 검증에 걸리면 조용히 아무것도 하지 않는다.
    /// "결정은 맞는데 실행이 안 된" 상황을 로그에서 구분하기 위한 값이다.
    /// </summary>
    public enum LDY_ActionOutcome
    {
        /// <summary>대기를 골라 실행할 것이 없었다.</summary>
        Waited,

        /// <summary>실행이 실제로 일어난 것을 확인했다.</summary>
        Executed,

        /// <summary>호출했지만 시스템이 조용히 무시했다.</summary>
        Rejected,

        /// <summary>확인할 수단이 없었다(ActionPointManager 미연결).</summary>
        Unverified
    }
}
