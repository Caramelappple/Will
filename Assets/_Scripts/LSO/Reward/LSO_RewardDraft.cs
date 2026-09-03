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

            // 장수는 테이블 전체가 하나를 쓴다. 스테이지마다 다르지 않다.
            return LSO_RewardRoller.PickMany(
                _candidates,
                table.CardCount,
                option => option.weight,
                isExcluded ?? (_ => false));
        }
        
        /// <summary>
        /// 후보를 만든다. 나오는 것은 전부 기물 카드다.
        ///
        /// 유언은 뽑지 않는다. 스테이지가 정해둔 하나를 모든 후보에 똑같이 붙인다.
        /// 그래서 어느 카드를 골라도 같은 유언이 들어온다.
        ///
        /// 후보마다 붙여두는 이유는 고른 뒤에 테이블을 다시 뒤지지 않기 위해서다.
        /// 고른 것만 들고 다니면 되는 편이 지급하는 쪽이 단순하다.
        /// </summary>
        private void BuildCandidates(LSO_StageRewardData data)
        {
            _candidates.Clear();

            if (data.possiblePieces == null) return;

            foreach (LSO_RewardPoolEntry entry in data.possiblePieces)
            {
                // 가중치 검사는 LSO_RewardRoller가 한다. 여기서는 알맹이가 있는지만 본다.
                if (entry == null || entry.pieceSO == null) continue;

                _candidates.Add(new LSO_RewardOption
                {
                    type = LSO_RewardType.Piece,
                    piece = entry.pieceSO,
                    will = data.stageWill,
                    weight = entry.weight
                });
            }
        }

    }
}
