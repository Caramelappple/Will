using _Scripts.LDY;
using UnityEngine;

namespace _Scripts.LSO.Animal
{
    /// <summary>
    /// 갓 태어난 기물이 어느 쪽을 볼지 정한다.
    ///
    /// 적은 플레이어 쪽으로 돌려세운다. 프리팹은 전부 같은 방향을 보고 만들어져서,
    /// 그대로 놓으면 적도 아군과 같은 쪽을 본다 — 판 건너편에서 등을 보이고 서 있는 셈이다.
    ///
    /// ── 왜 프리팹마다 돌려두지 않나 ───────────────────────────
    /// 같은 프리팹이 아군으로도 적으로도 놓인다. 프리팹에 각도를 적어두면
    /// 한쪽이 반드시 틀어진다. 보는 방향은 **팀이 정해진 뒤에** 정해지는 값이다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 기물이 태어나는 입구는 LSO_AnimalFactory 하나뿐이라 여기 한 곳에서 챙긴다.
    /// LSO_PieceHoverInstaller 와 같은 자리, 같은 이유다.
    ///
    /// 로컬 회전을 쓴다. 기물은 판에 실려 같이 돌아가므로(LDY_BoardFlipDirector),
    /// 월드로 적으면 판이 뒤집힐 때마다 서로 다른 값이 된다.
    /// </summary>
    public static class LSO_PieceFacing
    {
        /// <summary>
        /// 아군이 볼 방향(Y축, 도).
        ///
        /// 0 은 "프리팹이 만들어진 그대로"다. 모델이 이미 플레이어 쪽을 보고
        /// 만들어져 있으므로 아군은 돌릴 것이 없다.
        /// </summary>
        public const float PlayerYaw = 0f;

        /// <summary>
        /// 적이 볼 방향(Y축, 도).
        ///
        /// **모델이 반대로 서 있으면 이 값 하나만 뒤집으면 된다.**
        /// 두 값을 서로 바꾸는 것이 아니라 여기만 0 과 180 사이로 옮긴다 —
        /// 아군 쪽은 프리팹이 만들어진 기준이라 건드릴 것이 없다.
        /// </summary>
        public const float EnemyYaw = 180f;

        /// <summary>
        /// 팀에 맞는 방향으로 돌려세운다.
        ///
        /// 반드시 팀이 정해진 뒤(LDY_Animal.Setup 다음)에 부를 것.
        /// 그 전에 부르면 전부 아군 방향으로 선다.
        /// </summary>
        public static void Apply(LDY_Animal animal)
        {
            if (animal == null) return;

            animal.transform.localRotation = Quaternion.Euler(0f, YawFor(animal.team), 0f);
        }

        /// <summary>이 팀이 볼 방향. 모르는 팀은 아군 기준으로 둔다.</summary>
        public static float YawFor(LDY_Team team)
        {
            return team == LDY_Team.Enemy ? EnemyYaw : PlayerYaw;
        }
    }
}
