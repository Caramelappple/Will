using System;
using System.Collections.Generic;
using _Scripts.LSO;
using UnityEngine;

namespace _Scripts.LDY.Save
{
    /// <summary>
    /// 해금 목록(기물/유언)을 KTH_Reward와 세이브 사이에서 옮긴다.
    ///
    /// 되돌릴 때는 KTH_UnlockState.Import를 쓴다. UnlockPiece를 루프로 도는 방식은
    /// 기존 해금 위에 덧씌워져서 세이브에 없는 해금이 남아버린다.
    /// </summary>
    public sealed class LDY_UnlockSaveGateway
    {
        public void Capture(LDY_RunSaveData data)
        {
            data.unlockedPieceNames.Clear();
            data.unlockedWillIds.Clear();

            KTH_Reward reward = KTH_Reward.Instance;
            if (reward == null)
            {
                Debug.LogWarning("[LDY_UnlockSaveGateway] KTH_Reward가 없어 해금 목록을 읽지 못했습니다.");
                return;
            }

            reward.Unlocks.Export(out string[] pieces, out LSO_WillType[] wills);

            data.unlockedPieceNames.AddRange(pieces);

            // 유언은 enum이지만 문자열로 적는다. 나중에 enum에 값을 끼워 넣어도
            // 기존 세이브가 엉뚱한 유언으로 읽히지 않는다.
            foreach (LSO_WillType will in wills)
                data.unlockedWillIds.Add(will.ToString());
        }

        public void Restore(LDY_RunSaveData data)
        {
            KTH_Reward reward = KTH_Reward.Instance;
            if (reward == null)
            {
                Debug.LogWarning("[LDY_UnlockSaveGateway] KTH_Reward가 없어 해금 목록을 되돌리지 못했습니다.");
                return;
            }

            var wills = new List<LSO_WillType>();

            foreach (string willId in data.unlockedWillIds)
            {
                if (Enum.TryParse(willId, out LSO_WillType will))
                {
                    wills.Add(will);
                    continue;
                }

                // 이름이 바뀌었거나 사라진 유언이다. 건너뛰되 조용히 넘기지는 않는다.
                Debug.LogWarning($"[LDY_UnlockSaveGateway] 알 수 없는 유언 id '{willId}' 입니다.");
            }

            reward.Unlocks.Import(data.unlockedPieceNames, wills);
        }
    }
}
