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

            float yaw = YawFor(animal.team);

            if (Mathf.Approximately(yaw, 0f)) return;

            if (IsFlatModel(animal))
            {
                // 평면은 돌리면 뒷면이 보인다. 그대로 둔다.
                return;
            }

            // ── 덮어쓰지 않고 얹는다 ──────────────────────────────────
            // 프리팹이 자기 자세를 들고 있을 수 있다. 통째로 대입하면 그 자세가
            // 지워져 모델이 엉뚱하게 눕거나 선다.
            // ─────────────────────────────────────────────────────────
            animal.transform.localRotation =
                Quaternion.Euler(0f, yaw, 0f) * animal.transform.localRotation;
        }

        /// <summary>
        /// 모델이 납작한 판(빌보드)인지.
        ///
        /// ── 왜 이것을 가려내나 ───────────────────────────────────
        /// 상어왕처럼 그림 한 장을 세워둔 기물은 모델 자식이 X축으로 크게 누워 있다.
        /// 그 상태에서 루트를 180° 돌리면 판의 **뒷면**이 보인다. 한 면짜리 판이면
        /// 사라지고, 양면이어도 그림이 좌우로 뒤집혀 "이상하게 틀어진" 모양이 된다.
        ///
        /// 판은 애초에 어느 쪽에서 봐도 같은 그림을 보여주려고 만든 것이라,
        /// 돌릴 이유 자체가 없다.
        ///
        /// 기울기(X·Z)로 가른다. Y 만 돌아 있는 것은 방향을 정해둔 입체 모델이므로
        /// 그대로 얹는다.
        /// ─────────────────────────────────────────────────────────
        /// </summary>
        private static bool IsFlatModel(LDY_Animal animal)
        {
            // modelTransform 만 보면 놓친다. 상어왕은 그 값이 루트를 가리키고,
            // 실제로 누워 있는 것은 그 아래의 그림 자식이다.
            //
            // 그래서 **눈에 보이는 것**을 기준으로 본다 — 렌더러가 달린 자식 중
            // 하나라도 크게 기울어 있으면 판으로 친다.
            Renderer[] renderers = animal.GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < renderers.Length; i++)
            {
                Transform t = renderers[i] != null ? renderers[i].transform : null;

                if (t == null || t == animal.transform) continue;

                Vector3 euler = t.localEulerAngles;

                if (Tilted(euler.x) || Tilted(euler.z)) return true;
            }

            return false;
        }

        /// <summary>0°(=360°)에서 눈에 띄게 벗어났는지.</summary>
        private static bool Tilted(float angle)
        {
            float wrapped = Mathf.Abs(Mathf.DeltaAngle(0f, angle));

            return wrapped > 20f;
        }

        /// <summary>이 팀이 볼 방향. 모르는 팀은 아군 기준으로 둔다.</summary>
        public static float YawFor(LDY_Team team)
        {
            return team == LDY_Team.Enemy ? EnemyYaw : PlayerYaw;
        }
    }
}
