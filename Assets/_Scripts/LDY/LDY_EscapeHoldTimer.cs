using UnityEngine;

namespace _Scripts.LDY
{
    /// <summary>
    /// 키를 누르고 있는 시간을 잰다. 입력도 UI도 모르고, 시간만 센다.
    ///
    /// MonoBehaviour가 아닌 이유: 씬에 놓을 것이 아니라 핸들러가 속으로 쓰는 도구다.
    /// 프로젝트에 "누른 시간"을 재는 기존 패턴이 없어서(New Input System의 Hold
    /// interaction을 쓰는 곳이 한 군데도 없다) 여기서 직접 누적한다.
    ///
    /// 넘기는 delta는 반드시 unscaled여야 한다 — 정지 중에도 롱프레스가 진행돼야 하는데
    /// Time.deltaTime은 timeScale이 0이면 0이라 게이지가 얼어붙는다.
    /// 그 판단은 부르는 쪽 몫이라 여기서는 강제하지 않는다.
    /// </summary>
    public sealed class LDY_EscapeHoldTimer
    {
        private float _elapsed;

        /// <summary>이 시간을 넘기면 롱프레스로 친다. 인스펙터 값이 그대로 흘러들어온다.</summary>
        public float Threshold { get; set; } = 1.5f;

        public float Elapsed => _elapsed;

        /// <summary>0~1. 게이지 fillAmount에 그대로 넣는다.</summary>
        public float Progress => Threshold > 0f ? Mathf.Clamp01(_elapsed / Threshold) : 1f;

        public bool IsComplete => Threshold > 0f ? _elapsed >= Threshold : _elapsed > 0f;

        /// <summary>
        /// 누르고 있는 동안 매 프레임 부른다.
        /// 이번 호출에서 <b>처음</b> 임계값을 넘겼으면 true — 그래서 완성 처리가 한 번만 돈다.
        /// 이미 넘긴 뒤에는 계속 눌러도 false다.
        /// </summary>
        public bool Advance(float deltaSeconds)
        {
            if (IsComplete) return false;

            _elapsed += Mathf.Max(0f, deltaSeconds);

            return IsComplete;
        }

        public void Reset() => _elapsed = 0f;
    }
}
