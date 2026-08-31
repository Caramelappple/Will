using System;
using _Scripts.LSO.Will;
using UnityEngine;

namespace _Scripts.LSO.Reward
{
    public class LSO_WillStamp : LSO_RewardCard
    {
        [Serializable]
        private struct StampModel
        {
            [Tooltip("이 모델이 나타내는 유언.")]
            public LSO_WillType type;

            [Tooltip("켤 오브젝트. 도장 몸통째로 넣는다.")]
            public GameObject model;
        }

        [Header("도장 모델")]
        [Tooltip("유언마다 하나씩. 고른 것만 켜지고 나머지는 꺼진다.\n" +
                 "목록에 없는 유언이 오면 전부 꺼지고 경고가 남는다.")]
        [SerializeField] private StampModel[] models = Array.Empty<StampModel>();

        /// <summary>
        /// 보상 없이 유언만 그린다. 고른 뒤 보여줄 때 부른다.
        ///
        /// 이쪽으로 그리면 클릭 콜백이 붙지 않는다. 눌러도 아무 일이 없는 것이 맞다 —
        /// 이미 고른 뒤이므로 다시 고를 것이 없다.
        /// </summary>
        public void Bind(DLJ_WillDataSO will)
        {
            DrawWill(will);
        }

        protected override void Draw(LSO_RewardOption option)
        {
            if (option.type != LSO_RewardType.Will)
            {
                Debug.LogWarning($"{name}: 유언 도장인데 {option.type} 보상이 들어왔습니다.", this);
                Clear();
                return;
            }

            DrawWill(option.will);
        }

        private void DrawWill(DLJ_WillDataSO will)
        {
            if (will == null)
            {
                Debug.LogWarning($"{name}: 유언 데이터가 없어 도장을 고르지 못했습니다.", this);
                Clear();
                return;
            }

            ShowModel(will.WillType);
        }

        /// <summary>
        /// 맞는 모델 하나만 켠다.
        ///
        /// 매번 전부 돌며 켜고 끄는 이유는, 이전 것만 끄는 방식이면
        /// 풀에서 재사용될 때 지난번 도장이 같이 켜진 채로 나올 수 있어서다.
        /// </summary>
        private void ShowModel(LSO_WillType type)
        {
            bool found = false;

            foreach (StampModel entry in models)
            {
                if (entry.model == null) continue;

                bool on = entry.type == type;

                entry.model.SetActive(on);

                found |= on;
            }

            if (!found)
                Debug.LogWarning($"{name}: {type} 도장 모델이 목록에 없습니다.", this);
        }

        protected override void Clear()
        {
            ClearCommon();

            foreach (StampModel entry in models)
            {
                if (entry.model != null) entry.model.SetActive(false);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 같은 유언이 두 번 들어가면 나중 것만 켜져서, 앞의 모델은 영영 안 보인다.
            for (int i = 0; i < models.Length; i++)
            {
                for (int j = i + 1; j < models.Length; j++)
                {
                    if (models[i].type != models[j].type) continue;

                    Debug.LogWarning($"{name}: {models[i].type} 이 목록에 두 번 들어 있습니다.", this);
                    return;
                }
            }
        }
#endif
    }
}
