using UnityEngine;

namespace _Scripts.LDY.Save
{
    /// <summary>
    /// 맵 씬에 들어온 이유가 "새 런 시작"인지 "이어하기"인지를 전달하기 위한 자리.
    ///
    /// 세이브 대상이 아니다. 씬 전환 한 번을 건너가는 신호일 뿐이라 파일에 남기지 않는다.
    /// 새 런으로 진입하는 쪽(메인 메뉴의 게임 시작, 덱빌드 완료)에서 IsStartingNewRun을 켜두면,
    /// 맵이 세이브를 되돌리지 않고 방금 만들어진 상태를 그대로 쓴다.
    ///
    /// 켜는 쪽이 아무도 없으면 항상 false이므로, 맵은 기존대로 세이브가 있을 때 이어하기만 한다.
    /// </summary>
    public static class LDY_RunEntryState
    {
        /// <summary>새 런으로 맵에 들어가는 중인지. 맵이 집어가면서 꺼진다.</summary>
        public static bool IsStartingNewRun { get; set; }

        /// <summary>
        /// 플레이 모드에 들어갈 때 값을 비운다.
        ///
        /// Reload Domain을 끈 에디터에서는 static이 살아남는다. 켜둔 채 플레이를 멈추면
        /// 그 플래그가 다음 플레이까지 넘어가, 이어하기로 들어갔는데도 맵이 세이브를 건너뛴다.
        /// 새 런으로 오인해 진행도를 날리는 쪽이라 반드시 꺼진 상태에서 시작해야 한다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsStartingNewRun = false;
        }

        /// <summary>
        /// 집어간 쪽이 소비했음을 알린다. 값을 읽고 곧바로 끄기 때문에
        /// 다음 번 맵 진입이 이전 신호를 물려받지 않는다.
        /// </summary>
        public static bool Consume()
        {
            bool startingNewRun = IsStartingNewRun;
            IsStartingNewRun = false;
            return startingNewRun;
        }
    }
}
