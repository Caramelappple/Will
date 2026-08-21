using System;
using System.Collections.Generic;
using _Scripts.LDY.Save;
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

        [Header("3. [보스] 무작위 풀")]
        [Tooltip("보스 노드에 들어갈 때 이 목록에서 하나를 뽑는다. 뽑기는 런 시드로 정해지므로 " +
                 "같은 런에서 같은 챕터에 다시 들어가면(세이브를 거쳐도) 같은 보스가 나온다.\n" +
                 "비워두면 추첨하지 않고 위의 1·2번 표를 그대로 쓴다. 보스를 늘리려면 여기에 스테이지만 더 넣으면 된다.")]
        [SerializeField] private List<LDY_StageSO> bossStagePool = new List<LDY_StageSO>();

        // 추첨할 때마다 새로 만들지 않도록 재사용한다. 목록에 null이 섞여 있어도
        // 뽑기가 빈 칸에 걸리지 않게 걸러 담는 용도다.
        private readonly List<LDY_StageSO> _bossCandidates = new List<LDY_StageSO>();

        // 기존 인터페이스 원본 그대로 구현 (인자 2개)
        public LDY_StageSO Resolve(int nodeIndex, LDY_NodeType nodeType)
        {
            // MapManager 싱글톤에서 현재 챕터 번호를 가져옴 (없으면 기본값 1)
            int currentChapter = 1;
            if (LDY_MapManager.Instance != null)
            {
                currentChapter = LDY_MapManager.Instance.CurrentChapter;
            }

            // 0순위: 보스 노드이고 풀이 채워져 있으면 거기서 뽑는다.
            // 풀이 비어 있으면 null이 돌아오고 아래 기존 표로 그대로 내려간다.
            if (nodeType == LDY_NodeType.Boss)
            {
                LDY_StageSO drawn = DrawBossStage(currentChapter);
                if (drawn != null) return drawn;
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

        /// <summary>
        /// 보스 풀에서 하나를 뽑는다. 풀이 비어 있으면 null을 돌려주고, 부르는 쪽이 기존 표로 넘어간다.
        ///
        /// 뽑은 결과를 어디에도 남기지 않는 것이 핵심이다.
        /// 시드와 챕터만으로 매번 같은 답이 나오므로, 세이브/로드로 이 컴포넌트가 새로 만들어져도
        /// 보스가 바뀌지 않는다. "무엇을 뽑았는지"를 따로 저장할 필요도 없다.
        /// </summary>
        private LDY_StageSO DrawBossStage(int chapter)
        {
            if (bossStagePool == null || bossStagePool.Count == 0) return null;

            _bossCandidates.Clear();

            foreach (LDY_StageSO stage in bossStagePool)
            {
                if (stage != null) _bossCandidates.Add(stage);
            }

            if (_bossCandidates.Count == 0)
            {
                Debug.LogWarning("[LDY_StageRouter] 보스 풀에 빈 칸만 있어 추첨하지 못했습니다. 기존 표로 넘어갑니다.", this);
                return null;
            }

            int runSeed = LDY_RunSeed.EnsureAssigned();

            // 챕터를 그냥 XOR하면 1·2·3챕터의 시드가 하위 비트만 다르다.
            // System.Random은 가까운 시드끼리 첫 값이 얽혀서, 후보가 2개일 때
            // 챕터1과 챕터2가 같은 보스를 뽑을 확률이 50%가 아니라 9%까지 떨어진다
            // (20만 회 확인). 무작위가 아니라 "챕터마다 번갈아"에 가까워진다는 뜻이다.
            // 황금비 상수로 챕터를 흩뿌려 챕터끼리 독립이 되게 한다(같을 확률 50%).
            int seed;
            unchecked
            {
                seed = runSeed ^ (chapter * (int)0x9E3779B9);
            }

            int index = new System.Random(seed).Next(_bossCandidates.Count);
            LDY_StageSO chosen = _bossCandidates[index];

            Debug.Log(
                $"[LDY_StageRouter] {chapter}챕터 보스 추첨 " +
                $"(시드 {runSeed}, 후보 {_bossCandidates.Count}개) -> {chosen.name}");

            return chosen;
        }
    }
}