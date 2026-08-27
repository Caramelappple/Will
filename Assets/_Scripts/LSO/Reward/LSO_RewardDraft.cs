using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LSO.Reward
{
    public sealed class LSO_RewardDraft
    {
        //후보 목록
        private readonly List<LSO_RewardOption> _candidates = new();
        
        public List<LSO_RewardOption> Draw(
            LSO_RewardTableSO table,
            int chapter,
            int stage,
            Func<LSO_RewardOption, bool> isExcluded = null)
        {
            if (table == null)
            {
                Debug.LogError("[LSO_RewardDraft] 보상 테이블이 없습니다.");
                return new List<LSO_RewardOption>();
            }

            LSO_StageRewardData data = table.Find(chapter, stage);

            if (data == null)
            {
                Debug.LogWarning(
                    $"[LSO_RewardDraft] 챕터 {chapter} 스테이지 {stage} 항목이 테이블에 없습니다.");

                return new List<LSO_RewardOption>();
            }

            BuildCandidates(data);

            if (_candidates.Count == 0)
            {
                Debug.LogWarning(
                    $"[LSO_RewardDraft] 챕터 {chapter} 스테이지 {stage} 에 뽑을 수 있는 후보가 없습니다.");

                return new List<LSO_RewardOption>();
            }

            int count = Mathf.Max(1, data.rewardChoiceCount);

            return LSO_RewardRoller.PickMany(
                _candidates,
                count,
                option => option.weight,
                isExcluded ?? (_ => false));
        }
        
        private void BuildCandidates(LSO_StageRewardData data)
        {
            _candidates.Clear();

            if (data.possiblePieces != null)
            {
                foreach (LSO_RewardPoolEntry entry in data.possiblePieces)
                {
                    // 가중치 검사는 LSO_RewardRoller가 한다. 여기서는 알맹이가 있는지만 본다.
                    if (entry == null || entry.pieceSO == null) continue;

                    _candidates.Add(new LSO_RewardOption
                    {
                        type = LSO_RewardType.Piece,
                        piece = entry.pieceSO,
                        weight = entry.weight
                    });
                }
            }

            if (data.possibleWills == null) return;

            foreach (LSO_WillRewardPoolEntry entry in data.possibleWills)
            {
                if (entry == null || entry.willSO == null) continue;

                _candidates.Add(new LSO_RewardOption
                {
                    type = LSO_RewardType.Will,
                    will = entry.willSO,
                    weight = entry.weight
                });
            }
        }
    }
}
