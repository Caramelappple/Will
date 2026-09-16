using _Scripts.LDY;
using UnityEngine;

namespace _Scripts.LSO.Ability
{
    /// <summary>
    /// 질주: 달리기 시작할 때 출발한 자리에 연출을 남긴다.
    ///
    /// ── 이 특성은 이동력을 주지 않는다 ────────────────────────
    /// 말이 두 칸을 가는 것은 동물 SO 의 moveRange 가 정한다. 이동 시스템도
    /// 거기만 본다. 여기서 또 정하면 값이 두 곳에 생기고, 어긋났을 때 어느 쪽이
    /// 맞는지 정할 방법이 없다.
    ///
    /// 그래서 이 특성이 하는 일은 둘뿐이다.
    ///   1. 달릴 때 연출을 알린다.
    ///   2. 정보창과 특성 사전에 "질주"라는 이름으로 드러난다.
    ///
    /// 두 번째가 없으면 말은 플레이어에게 아무 특성도 없는 기물로 보인다.
    /// 실제로는 남들보다 한 칸 더 가는데도 그렇다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 연출은 **출발**에 붙는다(OnMoveStarted). 도착에 붙이면 이미 멈춰 선 뒤에
    /// 먼지가 이는 모양이 된다.
    /// </summary>
    public sealed class LSO_Swift : LSO_IAbility, LDY_IOnMoved, LSO_IAbilityInitializable
    {
        public void Initialize(LSO_AbilityContext context)
        {
            LDY_Animal owner = context?.Owner;

            if (owner == null) return;

            // 이름은 질주인데 한 칸밖에 못 가면 배선이 반만 된 것이다.
            // 조용히 두면 "이펙트는 뜨는데 왜 안 빨라지지"로 한참 헤매게 된다.
            if (owner.MoveRange <= 1)
            {
                Debug.LogWarning(
                    $"{owner.name}: 질주 특성이 붙었지만 이동 칸 수가 {owner.MoveRange} 입니다. " +
                    "동물 SO 의 Move Range 를 2 이상으로 올려주세요. " +
                    "질주는 연출과 표시만 맡고 칸 수는 정하지 않습니다.",
                    owner);
            }
        }

        /// <summary>
        /// 달리기 시작할 때. 아직 연출이 시작되기 전이라 모델은 **떠나는 칸**에 서 있고,
        /// 신호도 그 자리를 잡아간다.
        ///
        /// 거리를 가리지 않는다. 한 칸을 가도 뜬다 — 두 칸일 때만 뜨게 하면
        /// 한 칸 움직였을 때 조용해져서 고장으로 보인다.
        /// </summary>
        public void OnMoveStarted(LDY_Animal self, Vector3Int from, Vector3Int to)
        {
            if (self == null) return;

            LSO_AbilitySignal.Raise(
                LSO_AbilityType.Swift, self,
                $"<color=#7fd8ff>{self.name}의 질주: {from.x},{from.z} → {to.x},{to.z}</color>");
        }

        /// <summary>도착할 때는 할 일이 없다. 질주는 떠나는 연출이다.</summary>
        public void OnMoved(LDY_Animal self, Vector3Int from, Vector3Int to) { }
    }
}
