using UnityEngine;

namespace _Scripts.LDY.Save
{
    /// <summary>
    /// 영구 기록(유언 사용, 조합 발견, 보스 처치)을 KTH_WillRecord와 세이브 사이에서 옮긴다.
    /// </summary>
    public sealed class LDY_MetaRecordGateway
    {
        /// <summary>
        /// 사용 횟수를 되돌릴 때 도는 루프의 상한.
        /// KTH_WillRecord에는 값을 직접 넣는 경로가 없고 AddWillUse()(+1)뿐이라 루프로 채워야 하는데,
        /// 세이브가 손상돼 터무니없는 값이 들어오면 게임이 멈춘다. 그걸 막는 안전장치다.
        /// </summary>
        private const int MaxRestoredWillUseCount = 1_000_000;

        public void Capture(LDY_MetaSaveData data)
        {
            data.foundComboWillIds.Clear();
            data.defeatedBossIds.Clear();

            // 아직 채울 공급자가 없는 항목들. 스키마 자리만 지킨다.
            data.willUsage.Clear();
            data.unlockedCardIds.Clear();

            KTH_WillRecord record = KTH_WillRecord.Instance;
            if (record == null)
            {
                Debug.LogWarning("[LDY_MetaRecordGateway] KTH_WillRecord가 없어 기록을 읽지 못했습니다.");
                return;
            }

            data.totalWillUseCount = record.GetWillUseCount();
            data.foundComboWillIds.AddRange(record.GetDiscoveredCombos());
            data.defeatedBossIds.AddRange(record.GetDefeatedBosses());
        }

        public void Restore(LDY_MetaSaveData data)
        {
            KTH_WillRecord record = KTH_WillRecord.Instance;
            if (record == null)
            {
                Debug.LogWarning("[LDY_MetaRecordGateway] KTH_WillRecord가 없어 기록을 되돌리지 못했습니다.");
                return;
            }

            record.ResetRecord();

            foreach (string comboId in data.foundComboWillIds)
                record.DiscoverCombo(comboId);

            foreach (string bossId in data.defeatedBossIds)
                record.RecordBossDefeat(bossId);

            RestoreWillUseCount(record, data.totalWillUseCount);
        }

        private static void RestoreWillUseCount(KTH_WillRecord record, int count)
        {
            if (count <= 0) return;

            if (count > MaxRestoredWillUseCount)
            {
                Debug.LogWarning(
                    $"[LDY_MetaRecordGateway] 유언 사용 횟수 {count} 가 상한을 넘어 {MaxRestoredWillUseCount} 까지만 되돌립니다.");
                count = MaxRestoredWillUseCount;
            }

            for (int i = 0; i < count; i++)
                record.AddWillUse();
        }
    }
}
