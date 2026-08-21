using System;
using UnityEngine;

namespace _Scripts.LDY.Save
{
    /// <summary>
    /// 지금 진행 중인 런의 시드.
    ///
    /// 런 하나 동안 값이 바뀌지 않아야 하고, 세이브를 거쳐 돌아와도 같은 값이어야 한다.
    /// 그래서 무작위를 쓰는 쪽(예: 보스 추첨)이 시드를 직접 들고 있지 않고 여기서 받아 쓴다.
    /// "같은 런이면 같은 결과"를 만드는 것이 이 자리의 목적이다.
    ///
    /// 파일에 남기고 되돌리는 일은 <see cref="LDY_RunSeedGateway"/>가 맡는다.
    /// 여기서는 값만 들고 있으며 저장소를 알지 못한다.
    /// </summary>
    public static class LDY_RunSeed
    {
        /// <summary>아직 시드가 정해지지 않았음을 뜻하는 값.</summary>
        public const int Unassigned = 0;

        /// <summary>현재 런의 시드. 아직 정해지지 않았으면 <see cref="Unassigned"/>다.</summary>
        public static int Value { get; private set; } = Unassigned;

        public static bool IsAssigned => Value != Unassigned;

        /// <summary>
        /// 플레이 모드에 들어갈 때 값을 비운다.
        ///
        /// Reload Domain을 끈 에디터에서는 static이 세션 내내 살아남는다.
        /// 지난 플레이의 시드가 남아 있으면 새 런인데도 같은 보스가 계속 나와서,
        /// 무작위가 동작하지 않는 것처럼 보인다. LDY_StageSelection과 같은 이유다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Value = Unassigned;
        }

        /// <summary>
        /// 시드가 없으면 새로 뽑고, 이미 있으면 그대로 돌려준다.
        ///
        /// 시드를 쓰는 자리와 저장하는 자리 중 어느 쪽이 먼저 오는지가 상황마다 다르다.
        /// (새 런은 추첨이 먼저, 이어하기는 복원이 먼저다.)
        /// 양쪽 모두 이 메서드를 거치게 해서 한 런에서 시드가 한 번만 정해지도록 한다.
        /// </summary>
        public static int EnsureAssigned()
        {
            if (IsAssigned) return Value;

            // TickCount만 쓰면 같은 프레임에 두 번 부를 때 같은 값이 나온다.
            // Guid를 섞어 그런 경우에도 갈리게 한다.
            int seed = Environment.TickCount ^ Guid.NewGuid().GetHashCode();

            // 0은 "아직 없음"이라는 뜻으로 이미 쓰이고 있어 시드 값으로 쓸 수 없다.
            Value = seed != Unassigned ? seed : 1;

            Debug.Log($"[LDY_RunSeed] 새 런 시드를 뽑았습니다: {Value}");
            return Value;
        }

        /// <summary>
        /// 세이브에서 읽은 시드를 그대로 쓴다.
        /// 0이면 시드가 생기기 전에 만들어진 세이브이므로 무시하고, 필요할 때 새로 뽑게 둔다.
        /// </summary>
        public static void Restore(int seed)
        {
            if (seed == Unassigned) return;

            Value = seed;
        }

        /// <summary>런이 끝났거나 새 런을 시작한다. 다음 런이 이전 시드를 물려받지 않게 비운다.</summary>
        public static void Clear()
        {
            Value = Unassigned;
        }
    }
}
