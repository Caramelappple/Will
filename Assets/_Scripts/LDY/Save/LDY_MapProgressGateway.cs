using UnityEngine;

namespace _Scripts.LDY.Save
{
    /// <summary>
    /// 맵 진행도를 LDY_MapManager와 세이브 사이에서 옮긴다.
    /// </summary>
    public sealed class LDY_MapProgressGateway
    {
        public void Capture(LDY_RunSaveData data)
        {
            data.clearedNodeIndices.Clear();
            data.unlockedNodeIndices.Clear();

            LDY_MapManager map = LDY_MapManager.Instance;
            if (map == null)
            {
                Debug.LogWarning("[LDY_MapProgressGateway] LDY_MapManager가 없어 진행도를 읽지 못했습니다.");
                return;
            }

            data.chapter = map.CurrentChapter;
            data.stage = map.CurrentStage;
            data.currentNodeIndex = map.CurrentNodeIndex;
            data.battleEntryCount = map.BattleEntryCount;

            for (int i = 0; i < map.Nodes.Count; i++)
            {
                LDY_MapNode node = map.Nodes[i];
                if (node == null) continue;

                if (node.isCleared) data.clearedNodeIndices.Add(i);
                if (node.isUnlocked) data.unlockedNodeIndices.Add(i);
            }
        }

        public void Restore(LDY_RunSaveData data)
        {
            LDY_MapManager map = LDY_MapManager.Instance;
            if (map == null)
            {
                Debug.LogWarning("[LDY_MapProgressGateway] LDY_MapManager가 없어 진행도를 되돌리지 못했습니다.");
                return;
            }

            map.RestoreProgress(
                data.chapter,
                data.stage,
                data.clearedNodeIndices,
                data.unlockedNodeIndices,
                data.currentNodeIndex,
                data.battleEntryCount);
        }
    }
}
