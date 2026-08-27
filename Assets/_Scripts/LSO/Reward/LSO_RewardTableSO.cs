using System.Collections.Generic;
using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.Will;
using UnityEngine;
// ReSharper disable InconsistentNaming

namespace _Scripts.LSO.Reward
{
    public enum LSO_RewardType
    {
        Piece,
        Will
    }
 
    /// <summary>
    /// 플레이어에게 보여줄 보상 후보
    /// </summary>
    [System.Serializable]
    public class LSO_RewardOption
    {
        public LSO_RewardType type;

        public LSO_CardSO piece;
        public DLJ_WillDataSO will;

        /// <summary>
        /// 뽑기 가중치. 테이블의 항목에서 그대로 옮겨 담는다.
        ///
        /// 여기 담아두지 않으면 뽑을 때마다 원본 목록을 처음부터 훑어 짝을 다시 찾아야 한다.
        /// 에셋에 저장되는 값이 아니라 후보를 만들 때 채워지는 런타임 값이다.
        /// </summary>
        public float weight = 1f;

        public string GetName()
        {
            if (type == LSO_RewardType.Piece)
                return piece != null ? piece.name : "알 수 없는 기물";

            return will != null ? will.name : "알 수 없는 유언";
        }
    }


    /// <summary>
    /// 확률 뽑기용 카드 항목
    /// </summary>
    [System.Serializable]
    public class LSO_RewardPoolEntry
    {
        public LSO_CardSO pieceSO;

        [Tooltip("값이 클수록 뽑힐 확률이 높음")]
        public float weight = 1f;
    }


    /// <summary>
    /// 확률 뽑기용 유언 항목
    /// </summary>
    [System.Serializable]
    public class LSO_WillRewardPoolEntry
    {
        public DLJ_WillDataSO willSO;

        [Tooltip("값이 클수록 뽑힐 확률이 높음")]
        public float weight = 1f;
    }


    /// <summary>
    /// 스테이지별 보상 데이터
    /// </summary>
    [System.Serializable]
    public class LSO_StageRewardData
    {
        [Header("챕터")]
        public int chapter;

        [Header("스테이지")]
        public int stage;

        [Header("보상 후보 개수")]
        [Min(1)]
        public int rewardChoiceCount = 3;

        [Header("카드 후보")]
        public List<LSO_RewardPoolEntry> possiblePieces = new();

        [Header("유언 후보")]
        public List<LSO_WillRewardPoolEntry> possibleWills = new();
    }


    /// <summary>
    /// 스테이지별 보상 테이블
    /// </summary>
    [CreateAssetMenu(fileName = "LSO_RewardTable", menuName = "LSO/Reward Table")]
    public class LSO_RewardTableSO : ScriptableObject
    {
        [SerializeField]
        private List<LSO_StageRewardData> stages = new();

        public List<LSO_StageRewardData> Stages => stages;

        public LSO_StageRewardData Find(int chapter, int stage)
        {
            return stages.Find(
                x => x != null &&
                     x.chapter == chapter &&
                     x.stage == stage
            );
        }
    }
}