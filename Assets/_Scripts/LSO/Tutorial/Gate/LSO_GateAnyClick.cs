using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts.LSO.Tutorial.Gate
{
    /// <summary>
    /// 아무 데나 누르면 통과. 읽고 넘어가는 걸음의 기본값이다.
    ///
    /// ── 왜 이것이 기본인가 ───────────────────────────────────
    /// 시간으로만 넘기면 읽는 속도가 사람마다 달라 누군가는 늘 못 읽는다.
    /// 반대로 조작을 요구하면 그 조작이 되는 상태여야 한다.
    ///
    /// 클릭은 언제나 되고, 플레이어가 자기 속도로 넘긴다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// **바로 앞 걸음을 넘긴 클릭이 이 걸음까지 넘기지 않도록** 잠깐 안 받는 구간을 둔다.
    /// 그것이 없으면 한 번 누른 것으로 두세 걸음이 지나간다.
    /// </summary>
    [CreateAssetMenu(fileName = "Gate_AnyClick", menuName = "SO/Tutorial/Gate/아무 클릭")]
    public sealed class LSO_GateAnyClick : LSO_TutorialGateSO
    {
        [Tooltip("이 시간 동안은 클릭을 받지 않는다(초).\n" +
                 "\n" +
                 "앞 걸음을 넘긴 클릭이 이 걸음까지 넘기는 것을 막는다.\n" +
                 "0으로 두면 빠르게 누를 때 여러 걸음이 한 번에 지나간다.")]
        [SerializeField, Min(0f)] private float ignoreFor = 0.35f;

        [Tooltip("우클릭으로도 넘길지. 보드 조작과 겹치는 걸음에서는 꺼두는 편이 낫다.")]
        [SerializeField] private bool acceptRightClick;

        private Coroutine _routine;

        protected override void OnArm()
        {
            if (Runner == null)
            {
                // 몸을 못 빌렸으면 클릭을 볼 방법이 없다.
                // 여기서 멈춰 서면 튜토리얼이 영영 안 넘어간다.
                Debug.LogWarning($"{name}: Runner 가 없어 클릭을 못 받고 바로 통과합니다.");

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
            // Realtime 이다. 유언·보상 연출이 timeScale 을 쥐는 구간에서
            // 스케일 시간으로 재면 이 구간이 하염없이 길어진다.
            if (ignoreFor > 0f) yield return new WaitForSecondsRealtime(ignoreFor);

            while (!Clicked()) yield return null;

            _routine = null;

            Pass();
        }

        private bool Clicked()
        {
            Mouse mouse = Mouse.current;

            if (mouse == null) return false;

            if (mouse.leftButton.wasPressedThisFrame) return true;

            return acceptRightClick && mouse.rightButton.wasPressedThisFrame;
        }
    }
}
