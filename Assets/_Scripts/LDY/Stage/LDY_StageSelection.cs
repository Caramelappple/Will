namespace _Scripts.LDY.Stage
{
    /// <summary>
    /// 씬을 넘어가며 "다음에 시작할 스테이지"를 전달하기 위한 자리.
    /// 맵에서 노드를 고를 때 Pending에 넣어두면, 전투 씬의 LDY_StageDirector가 그것을 집어 시작한다.
    /// 여기서 스테이지를 실행하지는 않는다 — 값을 들고 있는 것이 전부다.
    /// </summary>
    public static class LDY_StageSelection
    {
        public static LDY_StageSO Pending { get; private set; }

        public static void Select(LDY_StageSO stage)
        {
            Pending = stage;
        }

        /// <summary>집어간 쪽이 소비했음을 알린다. 다음 전투가 이전 선택을 물려받지 않게 한다.</summary>
        public static LDY_StageSO Consume()
        {
            LDY_StageSO stage = Pending;
            Pending = null;
            return stage;
        }
    }
}
