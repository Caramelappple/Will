using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LDY.Stage
{
    /// <summary>
    /// 인스펙터에 적어둔 표를 보고 노드에 배정된 스테이지를 찾아준다.
    /// 특정 노드를 콕 집어 지정한 것이 우선이고, 없으면 노드 타입별 기본 스테이지를 쓴다.
    /// 씬 배선: 맵 씬의 LDY_MapManager와 같은 오브젝트(또는 아무 오브젝트)에 붙이고,
    /// LDY_MapManager의 Stage Router Source에 연결할 것.
    /// </summary>
    public class LDY_StageRouter : MonoBehaviour, LDY_IStageRouter
    {
        [Serializable]
        public class NodeEntry
        {
            [Tooltip("맵 노드 번호(LDY_MapManager의 Node Positions 순서, 0부터).")]
            public int nodeIndex = -1;
            public LDY_StageSO stage;
        }

        [Serializable]
        public class TypeEntry
        {
            public LDY_NodeType nodeType = LDY_NodeType.Battle;
            public LDY_StageSO stage;
        }

        [Header("노드 하나하나 지정 (여기 있으면 타입별 기본값보다 우선)")]
        [SerializeField] private List<NodeEntry> nodeStages = new List<NodeEntry>();

        [Header("타입별 기본 스테이지 (위에서 못 찾았을 때 사용)")]
        [SerializeField] private List<TypeEntry> typeStages = new List<TypeEntry>();

        public LDY_StageSO Resolve(int nodeIndex, LDY_NodeType nodeType)
        {
            foreach (NodeEntry entry in nodeStages)
            {
                if (entry != null && entry.nodeIndex == nodeIndex && entry.stage != null)
                    return entry.stage;
            }

            foreach (TypeEntry entry in typeStages)
            {
                if (entry != null && entry.nodeType == nodeType && entry.stage != null)
                    return entry.stage;
            }

            return null;
        }
    }
}
