using System.Collections;
using UnityEngine;

namespace _Scripts.LSO.Tutorial.Gate
{
    /// <summary>
    /// 시간이 지나면 통과. 읽고 넘어가기만 하는 걸음에 쓴다.
    ///
    /// 코루틴이 필요한 관문의 본보기다 — Runner 를 빌려 쓰고 Disarm 에서 멈춘다.
    /// </summary>
    [CreateAssetMenu(fileName = "Gate_Delay", menuName = "SO/Tutorial/Gate/시간")]
    public sealed class LSO_GateDelay : LSO_TutorialGateSO
    {
        [Tooltip("기다릴 시간(초).")]
        [SerializeField, Min(0f)] private float seconds = 2f;

        private Coroutine _routine;

        protected override void OnArm()
        {
            if (Runner == null)
            {
                // 몸을 못 빌렸으면 기다릴 방법이 없다. 여기서 멈춰 서느니 그냥 통과한다.
                Debug.LogWarning($"{name}: Runner 가 없어 기다리지 못하고 바로 통과합니다.");

                Pass();
                return;
            }

            _routine = Runner.StartCoroutine(Co_Wait());
        }

        protected override void OnDisarm()
        {
            if (_routine == null || Runner == null) return;

            Runner.StopCoroutine(_routine);
            _routine = null;
        }

        private IEnumerator Co_Wait()
        {
            // Realtime 인 이유는 유언·보상 연출이 timeScale 을 쥐는 구간이 있기 때문이다.
            // 스케일 시간으로 재면 그 구간에서 안내문이 하염없이 늘어진다.
            yield return new WaitForSecondsRealtime(seconds);

            _routine = null;

            Pass();
        }
    }
}
