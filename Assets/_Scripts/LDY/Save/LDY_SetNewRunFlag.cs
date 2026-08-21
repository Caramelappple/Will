using UnityEngine;

namespace _Scripts.LDY.Save
{
    /// <summary>
    /// "새 게임" 버튼이 누를 스위치. 맵에 들어가는 이유가 새 런임을 알린다.
    ///
    /// 씬 배선: 새 게임 버튼의 OnClick 목록에 이 컴포넌트의 SetFlag를 얹는다.
    /// 씬을 넘기는 쪽(LSO_MenuActions 등)은 그대로 두고, 그보다 먼저 놓기만 하면 된다.
    /// 플래그는 맵이 집어가면서 꺼지므로(<see cref="LDY_RunEntryState.Consume"/>)
    /// 여기서 되돌릴 일은 없다.
    /// </summary>
    public class LDY_SetNewRunFlag : MonoBehaviour
    {
        /// <summary>버튼 OnClick에서 부를 진입점. 매개변수가 없어야 인스펙터에 뜬다.</summary>
        public void SetFlag()
        {
            LDY_RunEntryState.IsStartingNewRun = true;

            // 새 런은 새 시드로 시작한다. 이어하기와 달리 되돌릴 시드가 없으므로,
            // 여기서 비워두지 않으면 방금 끝낸 런의 시드가 그대로 이어져 같은 보스가 또 나온다.
            LDY_RunSeed.Clear();
        }
    }
}
