using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LDY_MapNode
{
    public Vector2 position;
    public LDY_NodeType type;
    public bool isCleared;
    public bool isUnlocked;

    // 이 노드를 클리어했을 때 열리는 다음 노드들 (2개 이상이면 분기). LDY_MapManager가 채워 넣음
    public List<int> nextIndices = new List<int>();

    public LDY_MapNode(Vector2 position, LDY_NodeType type)
    {
        this.position = position;
        this.type = type;
        isCleared = false;
        isUnlocked = false;
    }
}
