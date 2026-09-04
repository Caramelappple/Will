using System;
using UnityEngine;

namespace _Scripts.LSO.Reward
{
    /// <summary>
    /// 카드가 몇 번째면 어디에 서고 얼마나 기우는지만 계산한다.
    ///
    /// 상태가 없다. 같은 index·total을 주면 언제나 같은 답이 나온다.
    /// 그래서 연출 도중이든 인스펙터를 만지는 중이든 마음대로 물어봐도 된다.
    ///
    /// 자리를 "정하는" 것은 여전히 LSO_RewardBox다. 이쪽은 계산만 한다 —
    /// 카드가 이 값을 직접 읽어 스스로 자리를 잡기 시작하면 주인이 둘이 된다.
    /// </summary>
    [Serializable]
    public class LSO_RewardLayout
    {
        [Tooltip("카드 한 칸의 간격과 방향. 기준 자리의 로컬 축이다.\n" +
                 "\n" +
                 "(0.6, 0, 0)      가로로 나란히\n" +
                 "(0.6, 0.1, 0)    가로로 가면서 조금씩 올라감 (계단)\n" +
                 "(0.5, 0, 0.2)    부채꼴처럼 앞뒤로도 벌어짐\n" +
                 "\n" +
                 "가운데를 기준으로 좌우 대칭이 된다. 세 장이면 -1, 0, +1 칸이다.")]
        public Vector3 spacing = new Vector3(0.6f, 0f, 0f);

        [Tooltip("상자 안에서 솟아오르는 높이. Spacing과 별개로 더해진다.")]
        public float riseHeight = 0.8f;

        [Tooltip("카드마다 더 기울일 각도(도). 가운데 카드는 0이고 바깥으로 갈수록 커진다.\n" +
                 "(0, 0, 5) 를 넣으면 부채꼴처럼 좌우로 기울어진다.")]
        public Vector3 tilt;

        /// <summary>
        /// 가운데를 기준으로 몇 칸 밀린 자리인지. 솟는 높이는 빼고 좌우 배치만이다.
        ///
        /// 상자 안에서 출발할 때는 이 값만 쓴다 — 한 점에서 겹쳐 나오면 뭉쳐 보인다.
        /// </summary>
        public Vector3 Spread(int index, int total)
        {
            return spacing * Step(index, total);
        }

        /// <summary>카드가 최종적으로 놓일 자리.</summary>
        public Vector3 Position(int index, int total)
        {
            return Spread(index, total) + Vector3.up * riseHeight;
        }

        /// <summary>가운데에서 멀수록 더 기울인다. Tilt가 0이면 전부 똑바로 선다.</summary>
        public Quaternion Tilt(int index, int total)
        {
            if (tilt == Vector3.zero) return Quaternion.identity;

            return Quaternion.Euler(tilt * Step(index, total));
        }

        /// <summary>
        /// 가운데를 0으로 놓았을 때 이 카드가 몇 칸째인지. 왼쪽은 음수다.
        ///
        /// 짝수여도 가운데가 비어 대칭이 유지된다. 네 장이면 -1.5, -0.5, +0.5, +1.5 다.
        /// </summary>
        private static float Step(int index, int total)
        {
            return index - (total - 1) * 0.5f;
        }
    }
}
