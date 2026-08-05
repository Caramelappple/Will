namespace _Scripts.LDY.Stage
{
    /// <summary>
    /// 스테이지를 시작할 때 실행되는 준비 단계 하나.
    /// 새로운 준비 절차가 필요하면 이 인터페이스를 구현한 컴포넌트를 추가하기만 하면 되고,
    /// LDY_StageDirector를 비롯한 기존 코드는 고치지 않는다.
    /// </summary>
    public interface LDY_IStageSetupStep
    {
        void Setup(LDY_StageSO stage);
    }
}
