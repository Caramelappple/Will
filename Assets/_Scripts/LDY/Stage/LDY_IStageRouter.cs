namespace _Scripts.LDY.Stage
{
    /// <summary>
    /// 맵의 어떤 노드가 어떤 스테이지인지 결정한다.
    /// 고정 표로 정할지, 무작위 풀에서 뽑을지 같은 방식은 구현체가 정하며
    /// 맵 쪽(LDY_MapManager)은 이 인터페이스만 알면 된다.
    /// </summary>
    public interface LDY_IStageRouter
    {
        /// <summary>해당 노드에 배정된 스테이지. 정해진 것이 없으면 null.</summary>
        LDY_StageSO Resolve(int nodeIndex, LDY_NodeType nodeType);
    }
}
