using System;
using UnityEngine;

namespace _Scripts.LSO.Will.Stamp
{
    /// <summary>
    /// 도장 하나의 겉모습. 유언에 맞는 모델을 켜는 것 외의 책임은 갖지 않는다.
    ///
    /// 보상 상자에서 나오는 도장과 선택창에 놓인 도장이 같은 모델을 쓴다.
    /// 그래서 모델을 고르는 일만 부품으로 떼어 두 곳이 함께 쓴다.
    ///
    /// 클릭도 자리도 모른다. 그건 이 부품을 들고 있는 쪽이 정한다.
    /// </summary>
    public class LSO_WillStampView : MonoBehaviour
    {
        [Serializable]
        private struct StampModel
        {
            [Tooltip("이 모델이 나타내는 유언.")]
            public LSO_WillType type;

            [Tooltip("켤 오브젝트. 도장 몸통째로 넣는다.")]
            public GameObject model;
        }

        [Tooltip("유언마다 하나씩. 고른 것만 켜지고 나머지는 꺼진다.\n" +
                 "목록에 없는 유언이 오면 전부 꺼지고 경고가 남는다.")]
        [SerializeField] private StampModel[] models = Array.Empty<StampModel>();

        /// <summary>지금 켜져 있는 도장. 아무것도 안 켜져 있으면 None.</summary>
        public LSO_WillType Current { get; private set; } = LSO_WillType.None;

        /// <summary>
        /// 맞는 모델 하나만 켠다. None을 주면 전부 끈다.
        ///
        /// 매번 전부 돌며 켜고 끄는 이유는, 이전 것만 끄는 방식이면
        /// 풀에서 재사용될 때 지난번 도장이 같이 켜진 채로 나올 수 있어서다.
        /// </summary>
        public void Show(LSO_WillType type)
        {
            Current = type;

            bool found = false;

            foreach (StampModel entry in models)
            {
                if (entry.model == null) continue;

                bool on = type != LSO_WillType.None && entry.type == type;

                entry.model.SetActive(on);

                found |= on;
            }

            if (type != LSO_WillType.None && !found)
                Debug.LogWarning($"{name}: {type} 도장 모델이 목록에 없습니다.", this);
        }

        /// <summary>전부 끈다.</summary>
        public void Hide()
        {
            Show(LSO_WillType.None);
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
