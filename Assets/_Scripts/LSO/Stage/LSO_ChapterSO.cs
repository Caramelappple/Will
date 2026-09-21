using System;
using System.Collections.Generic;
using _Scripts.LDY.Save;
using _Scripts.LDY.Stage;
using UnityEngine;

namespace _Scripts.LSO.Stage
{
    /// <summary>한 스테이지 순번에서 무작위로 뽑을 모든 후보.</summary>
    [Serializable]
    public sealed class LSO_StageVariantGroup
    {
        [Tooltip("후보를 추가할 스테이지 순번. 0부터 센다. 화면의 1스테이지는 0이다.")]
        [Min(0)] public int stageIndex;

        [Tooltip("이 순번에서 무작위로 뽑을 모든 StageSO. A·B·C안을 모두 넣는다.")]
        public List<LDY_StageSO> alternatives = new List<LDY_StageSO>();
    }

    /// <summary>
    /// 챕터 하나. 스테이지를 순서대로 적어둔 목록이다.
    ///
    /// 별자리 맵의 노드·분기·해금을 이것으로 갈음한다.
    /// 고르는 것이 없어졌으므로 "다음은 무엇인가"만 답하면 되고, 그건 목록의 다음 칸이다.
    ///
    /// ── 번호가 아니라 순서다 ───────────────────────────────────
    /// 옛 세이브는 노드를 번호로 들고 있었다. 노드를 하나 끼워 넣으면
    /// 그 뒤 번호가 전부 밀려 저장된 진행도가 엉뚱한 곳을 가리켰다.
    ///
    /// 여기서도 인덱스를 쓰지만 목록이 한 줄이라 밀리는 범위가 눈에 보인다.
    /// 스테이지를 중간에 끼워 넣으면 진행 중인 세이브가 한 칸 뒤로 밀린다는 뜻이므로,
    /// 런이 도는 중에는 뒤에만 추가할 것.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 만들기: Project 창 우클릭 → Create → LSO → 챕터
    /// </summary>
    [CreateAssetMenu(fileName = "LSO_Chapter", menuName = "LSO/챕터", order = 1)]
    public class LSO_ChapterSO : ScriptableObject
    {
        [Header("표시")]
        [Tooltip("챕터 번호. 1부터.")]
        [Min(1)] public int chapter = 1;

        [Tooltip("챕터가 바뀔 때 화면 가운데 뜨는 이름. 예: 까마귀왕의 둥지")]
        public string regionName;

        // 기존 챕터 에셋과의 직렬화 호환을 위한 단일 스테이지 목록이다.
        // Variant Groups가 있는 순번에서는 사용하지 않으며 Inspector에서도 숨긴다.
        [SerializeField, HideInInspector]
        public List<LDY_StageSO> stages = new List<LDY_StageSO>();

        [Header("스테이지 (위에서부터 차례로)")]
        [Tooltip("각 순번의 Alternatives에 A·B·C안을 모두 넣는다.\n" +
                 "Stage Index는 0부터 세며, 후보가 하나뿐인 보스도 여기에 넣는다.")]
        public List<LSO_StageVariantGroup> variantGroups = new List<LSO_StageVariantGroup>();

        /// <summary>이 챕터의 스테이지 수.</summary>
        public int Count
        {
            get
            {
                int count = stages != null ? stages.Count : 0;
                if (variantGroups == null) return count;

                for (int i = 0; i < variantGroups.Count; i++)
                {
                    LSO_StageVariantGroup group = variantGroups[i];
                    if (group != null && group.stageIndex >= 0)
                        count = Math.Max(count, group.stageIndex + 1);
                }

                return count;
            }
        }

        /// <summary>
        /// 몇 번째 스테이지. 범위를 벗어나면 null.
        ///
        /// 0부터 센다. 화면에 보이는 "스테이지 3"은 여기서 인덱스 2다.
        /// </summary>
        public LDY_StageSO At(int index)
        {
            if (index < 0 || index >= Count) return null;

            LSO_StageVariantGroup group = FindVariantGroup(index);
            if (group != null && group.alternatives != null)
            {
                int candidateCount = 0;
                for (int i = 0; i < group.alternatives.Count; i++)
                    if (group.alternatives[i] != null) candidateCount++;

                if (candidateCount == 1) return FirstValid(group.alternatives);
                if (candidateCount > 1)
                {
                    int seed = LDY_RunSeed.EnsureAssigned();
                    int picked = new System.Random(CombineSeed(seed, chapter, index)).Next(candidateCount);

                    for (int i = 0; i < group.alternatives.Count; i++)
                    {
                        LDY_StageSO candidate = group.alternatives[i];
                        if (candidate == null) continue;
                        if (picked-- == 0) return candidate;
                    }
                }
            }

            return stages != null && index < stages.Count ? stages[index] : null;
        }

        private LSO_StageVariantGroup FindVariantGroup(int index)
        {
            if (variantGroups == null) return null;

            for (int i = 0; i < variantGroups.Count; i++)
            {
                LSO_StageVariantGroup group = variantGroups[i];
                if (group != null && group.stageIndex == index) return group;
            }

            return null;
        }

        private static LDY_StageSO FirstValid(List<LDY_StageSO> candidates)
        {
            for (int i = 0; i < candidates.Count; i++)
                if (candidates[i] != null) return candidates[i];

            return null;
        }

        private static int CombineSeed(int runSeed, int chapterNumber, int stageIndex)
        {
            unchecked
            {
                int hash = runSeed;
                hash = hash * 397 ^ chapterNumber;
                hash = hash * 397 ^ stageIndex;
                return hash;
            }
        }

        /// <summary>이 자리가 보스인지. 목록의 마지막 칸을 보스로 본다.</summary>
        public bool IsBossAt(int index)
        {
            return Count > 0 && index == Count - 1;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var usedIndices = new HashSet<int>();
            if (variantGroups != null)
            {
                for (int i = 0; i < variantGroups.Count; i++)
                {
                    LSO_StageVariantGroup group = variantGroups[i];
                    if (group == null) continue;

                    if (group.stageIndex < 0)
                        Debug.LogWarning($"{name}: 후보 그룹 {i}의 Stage Index가 음수입니다.", this);
                    else if (!usedIndices.Add(group.stageIndex))
                        Debug.LogWarning($"{name}: Stage Index {group.stageIndex}의 후보 그룹이 중복됐습니다.", this);

                    if (group.alternatives == null) continue;
                    for (int j = 0; j < group.alternatives.Count; j++)
                        if (group.alternatives[j] == null)
                            Debug.LogWarning($"{name}: Stage Index {group.stageIndex}의 후보 {j}가 비어 있습니다.", this);
                }
            }

            for (int i = 0; i < Count; i++)
            {
                LSO_StageVariantGroup group = FindVariantGroup(i);
                bool hasVariant = group != null && group.alternatives != null && FirstValid(group.alternatives) != null;
                bool hasLegacy = stages != null && i < stages.Count && stages[i] != null;
                if (!hasVariant && !hasLegacy)
                    Debug.LogWarning($"{name}: Stage Index {i}에 사용할 스테이지가 없습니다.", this);
            }
        }
#endif
    }
}
