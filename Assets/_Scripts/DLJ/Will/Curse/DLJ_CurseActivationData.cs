using _Scripts.LDY;
using UnityEngine;

public class DLJ_CurseActivationData
{
    public int duration;
    public int damage;
    public int range;
    public Vector3Int center;
    public Vector3 centerWorld;
    public Vector3 areaSize;
    public float effectFadeOutTime;
    public LDY_Team sourceTeam;
    public LDY_TurnManager turnManager;
    public LDY_BoardManager board;
    public LDY_AttackSystem attackSystem;
}
