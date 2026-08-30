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

#if UNITY_EDITOR

        #region 테스트용

        [Header("테스트용")]
        [Tooltip("컨텍스트 메뉴로 뽑아볼 챕터·스테이지. 빌드에는 들어가지 않는다.")]
        [SerializeField] private int testChapter = 1;

        [SerializeField] private int testStage = 1;

        [Tooltip("몇 번 뽑아 볼지. 많을수록 실제 확률에 가까워진다.")]
        [SerializeField, Min(1)] private int testRolls = 1000;

        /// <summary>
        /// 실제로 여러 번 뽑아 분포를 콘솔에 찍는다. 에셋 우클릭에서 부른다.
        ///
        /// 플레이 모드에 들어가지 않아도 된다. LSO_RewardDraft가 MonoBehaviour가 아니라
        /// 씬 없이도 돌기 때문이다. 가중치를 고칠 때마다 바로 확인할 수 있다.
        ///
        /// 나오는 값은 "한 번 뽑을 때 이 보상이 후보에 낄 확률"이다.
        /// 한 번에 rewardChoiceCount개를 뽑으므로 전부 더하면 100%를 넘는다.
        /// </summary>
        [ContextMenu("테스트: 확률 뽑아보기")]
        private void TestRoll()
        {
            LSO_StageRewardData data = Find(testChapter, testStage);

            if (data == null)
            {
                Debug.LogWarning($"{name}: 챕터 {testChapter} 스테이지 {testStage} 항목이 없습니다.", this);
                return;
            }

            var draft = new LSO_RewardDraft();
            var counts = new Dictionary<string, int>();

            for (int i = 0; i < testRolls; i++)
            {
                foreach (LSO_RewardOption option in draft.Draw(this, testChapter, testStage))
                {
                    string key = option.GetName();

                    counts.TryGetValue(key, out int n);
                    counts[key] = n + 1;
                }
            }

            var lines = new List<KeyValuePair<string, int>>(counts);
            lines.Sort((a, b) => b.Value.CompareTo(a.Value));

            var text = new System.Text.StringBuilder();

            text.AppendLine($"{name} — 챕터 {testChapter} 스테이지 {testStage}");
            text.AppendLine($"{testRolls}번 뽑음 / 한 번에 {Mathf.Max(1, data.rewardChoiceCount)}개");
            text.AppendLine();

            foreach (KeyValuePair<string, int> line in lines)
            {
                float percent = line.Value * 100f / testRolls;

                text.AppendLine($"  {line.Key,-24} {line.Value,6}회  {percent,6:F1}%");
            }

            if (lines.Count == 0)
                text.AppendLine("  (뽑힌 것이 없습니다. 후보나 가중치를 확인하세요.)");

            Debug.Log(text.ToString(), this);
        }

        #endregion

#endif
    }
}