using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LDY.Stage
{
    public class LDY_StageRouter : MonoBehaviour, LDY_IStageRouter
    {
        [Serializable]
        public class ChapterNodeEntry
        {
            [Tooltip("챕터 번호 (1부터 시작)")]
            public int chapter = 1;

            [Tooltip("맵 노드 번호 (0부터 시작)")]
            public int nodeIndex = -1;

            public LDY_StageSO stage;
        }

        [Serializable]
        public class ChapterTypeEntry
        {
            [Tooltip("챕터 번호 (1부터 시작)")]
            public int chapter = 1;

            public LDY_NodeType nodeType = LDY_NodeType.Battle;
            public LDY_StageSO stage;
        }

        [Header("1. [챕터 + 노드 번호] 개별 지정")]
        [SerializeField] private List<ChapterNodeEntry> nodeStages = new List<ChapterNodeEntry>();

        [Header("2. [챕터 + 노드 타입] 기본 지정")]
        [SerializeField] private List<ChapterTypeEntry> typeStages = new List<ChapterTypeEntry>();

        // 기존 인터페이스 원본 그대로 구현 (인자 2개)
        public LDY_StageSO Resolve(int nodeIndex, LDY_NodeType nodeType)
        {
            // MapManager 싱글톤에서 현재 챕터 번호를 가져옴 (없으면 기본값 1)
            int currentChapter = 1;
            if (LDY_MapManager.Instance != null)
            {
                currentChapter = LDY_MapManager.Instance.CurrentChapter;
            }

            // 1순위: [현재 챕터 + 특정 노드 번호] 매칭
            foreach (ChapterNodeEntry entry in nodeStages)
            {
                if (entry != null && entry.chapter == currentChapter && entry.nodeIndex == nodeIndex && entry.stage != null)
                    return entry.stage;
            }

            // 2순위: [현재 챕터 + 노드 타입] 매칭
            foreach (ChapterTypeEntry entry in typeStages)
            {
                if (entry != null && entry.chapter == currentChapter && entry.nodeType == nodeType && entry.stage != null)
                    return entry.stage;
            }

            Debug.LogWarning($"[LDY_StageRouter] {currentChapter}챕터 {nodeIndex}번 노드({nodeType})에 해당하는 스테이지를 찾지 못했습니다.");
            return null;
        }
    }
}