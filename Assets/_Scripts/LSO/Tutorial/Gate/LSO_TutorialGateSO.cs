using System;
using UnityEngine;

namespace _Scripts.LSO.Tutorial.Gate
{
    /// <summary>
    /// 다음 걸음으로 넘어가는 조건 하나.
    ///
    /// ── 왜 조건마다 클래스를 만드나 ───────────────────────────
    /// 감독 안에 if 로 늘어놓으면, 조건이 하나 늘 때마다 감독을 고쳐야 한다.
    /// 지금 필요한 것만 열세 가지다. 그 열세 개가 한 함수에 들어가면
    /// 그 함수는 아무도 못 읽는 것이 된다.
    ///
    /// **감독은 Arm 과 Disarm 만 안다.** 조건이 백 개가 되어도 감독은 안 고쳐진다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// SO 인 이유는 걸음(StepSO)이 인스펙터에서 관문을 고를 수 있어야 하기 때문이다.
    /// 에셋으로 하나 만들어 두고 여러 걸음이 같이 쓴다.
    /// </summary>
    public abstract class LSO_TutorialGateSO : ScriptableObject, LSO_ITutorialGate
    {
        /// <summary>
        /// 통과했을 때 부를 것. **직렬화되지 않는다.**
        ///
        /// SO 는 에셋이라 직렬화되는 필드에 런타임 값을 넣으면 플레이를 멈춰도 남는다.
        /// 여기 담기는 것은 델리게이트라 저장 대상이 아니다. 새 필드를 더할 때 주의할 것.
        /// </summary>
        private Action _onPassed;

        /// <summary>
        /// 코루틴이 필요한 관문이 빌려 쓰는 몸. 감독이 자기를 넘겨준다.
        ///
        /// SO 는 MonoBehaviour 가 아니라 코루틴을 못 돌린다. 관문마다 빈 오브젝트를
        /// 만들게 하느니, 이미 씬에 있는 감독을 빌려 쓰는 편이 배선이 줄어든다.
        /// </summary>
        protected MonoBehaviour Runner { get; private set; }

        /// <summary>지금 기다리는 중인지. 두 번 Arm 되는 것을 막는 데 쓴다.</summary>
        protected bool IsArmed => _onPassed != null;

        /// <summary>기다리기 시작한다.</summary>
        public void Arm(MonoBehaviour runner, Action onPassed)
        {
            if (IsArmed)
            {
                // 앞 걸음이 Disarm 을 안 하고 넘어갔다는 뜻이다. 조용히 덮으면
                // 앞 관문의 구독이 살아남아 엉뚱한 걸음에서 통과 신호가 날아온다.
                Debug.LogWarning(
                    $"{name}: 이미 기다리는 중인데 다시 Arm 되었습니다. 앞 것을 먼저 풉니다.");

                Disarm();
            }

            Runner = runner;
            _onPassed = onPassed;

            OnArm();
        }

        /// <summary>그만 기다린다. 통과했든 시간이 지났든 **반드시** 불린다.</summary>
        public void Disarm()
        {
            if (!IsArmed && Runner == null) return;

            OnDisarm();

            _onPassed = null;
            Runner = null;
        }

        /// <summary>
        /// 조건이 맞았다고 알린다. 구현이 이것을 부른다.
        ///
        /// 먼저 비우고 부르는 이유는, 콜백 안에서 다음 걸음이 시작되며
        /// 이 관문이 다시 Arm 될 수 있기 때문이다. 나중에 비우면 방금 담은 것을 지운다.
        /// </summary>
        protected void Pass()
        {
            Action callback = _onPassed;

            _onPassed = null;

            callback?.Invoke();
        }

        /// <summary>구독을 걸거나 코루틴을 시작한다.</summary>
        protected abstract void OnArm();

        /// <summary>건 것을 전부 푼다. 여기서 빠뜨리면 다음 걸음에서 유령 신호가 온다.</summary>
        protected abstract void OnDisarm();
    }
}
