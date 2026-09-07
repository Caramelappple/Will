using System.Collections.Generic;
using _Scripts.LDY.Stage;
using UnityEngine;

namespace _Scripts.LSO.Stage
{
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

        [Header("스테이지 (위에서부터 차례로)")]
        [Tooltip("클리어할 때마다 아래로 한 칸씩 내려간다.\n" +
                 "\n" +
                 "마지막 칸은 보통 보스다. 보스인지 아닌지는 스테이지가 정하는 것이 아니라\n" +
                 "이 목록의 마지막인지로 판단한다 — 두 곳에 적어두면 어긋난다.")]
        public List<LDY_StageSO> stages = new List<LDY_StageSO>();

        /// <summary>이 챕터의 스테이지 수.</summary>
        public int Count => stages != null ? stages.Count : 0;

        /// <summary>
        /// 몇 번째 스테이지. 범위를 벗어나면 null.
        ///
        /// 0부터 센다. 화면에 보이는 "스테이지 3"은 여기서 인덱스 2다.
        /// </summary>
        public LDY_StageSO At(int index)
        {
            if (stages == null) return null;
            if (index < 0 || index >= stages.Count) return null;

            return stages[index];
        }

        /// <summary>이 자리가 보스인지. 목록의 마지막 칸을 보스로 본다.</summary>
        public bool IsBossAt(int index)
        {
            return Count > 0 && index == Count - 1;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (stages == null) return;

            for (int i = 0; i < stages.Count; i++)
            {
                // 빈 칸이 있으면 그 자리에서 진행이 멈춘다. 눈으로는 잘 안 보인다.
                if (stages[i] == null)
                    Debug.LogWarning($"{name}: {i}번 칸이 비어 있습니다.", this);
            }
        }
#endif
    }
}
