using UnityEngine;

namespace _Scripts.LDY
{
    /// <summary>
    /// Time.timeScale을 건드리는 책임만 갖는다. 입력도 UI도 씬도 모른다.
    ///
    /// ── "저장해뒀다 복원"을 쓰지 않는 이유 ──────────────────────
    /// 프로젝트에는 이미 그 방식을 쓰는 시스템이 있다.
    ///   DLJ_SuccessionWill — timeScaleBeforeSuccession에 저장 후 복원
    /// 서로를 모르는 시스템이 추가로 끼어들면 "원래 값"을 덮어쓸 수 있다.
    /// 우리가 0으로 만든 뒤 연출이 시작되면 그 연출은 0을 원래 값으로 저장하고,
    /// 연출이 끝나며 0으로 되돌려 게임이 영영 멈춘다.
    ///
    /// 그래서 값을 저장하지 않고 값 자체를 조건으로 본다.
    ///   - 걸 때: timeScale이 1이 아니면 남이 쓰는 중이므로 손대지 않는다.
    ///   - 풀 때: 우리가 건 정지만 1f로 되돌린다.
    /// 이 규칙 덕에 연출 중에 정지가 끼어들 수 없고, 우리 정지 중에 연출이 시작되면
    /// 그건 이미 IsBlocked로 막히는 상황이라 실제로 겹치지 않는다.
    /// ─────────────────────────────────────────────────────────
    /// </summary>
    public sealed class LDY_GameplayPause
    {
        /// <summary>"우리가" 걸어둔 정지인지. 남이 만든 timeScale 0은 여기 잡히지 않는다.</summary>
        public bool IsPaused { get; private set; }

        /// <summary>정지를 걸었으면 true. 남이 이미 시간을 쓰는 중이면 아무것도 하지 않고 false.</summary>
        public bool TryPause()
        {
            if (IsPaused) return false;
            if (!Mathf.Approximately(Time.timeScale, 1f)) return false;

            Time.timeScale = 0f;
            IsPaused = true;

            return true;
        }

        /// <summary>우리가 건 정지를 풀었으면 true.</summary>
        public bool Resume()
        {
            if (!IsPaused) return false;

            Time.timeScale = 1f;
            IsPaused = false;

            return true;
        }

        /// <summary>
        /// 씬을 떠나기 직전에 부른다.
        ///
        /// timeScale은 씬을 넘어가도 유지되므로, 멈춘 채 나가면 다음 씬이 멈춘 채로 뜬다.
        /// 떠나는 씬의 연출은 어차피 함께 사라지니 누가 걸었든 조건 없이 1로 되돌린다.
        /// </summary>
        public void ReleaseForSceneChange()
        {
            Time.timeScale = 1f;
            IsPaused = false;
        }
    }
}
