using UnityEngine;

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

        /// <summary>
        /// 플레이 모드에 들어갈 때 값을 비운다.
        ///
        /// Enter Play Mode Options에서 Reload Domain을 끄면 static이 에디터 세션 내내 살아남는다.
        /// 그러면 지난번에 고르고 소비하지 않은 스테이지가 남아, 전투 씬을 단독 실행했을 때
        /// defaultStage 대신 그 스테이지가 시작된다. 빌드에서는 어차피 매번 초기화되므로 에디터 전용 문제다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Pending = null;
        }

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
