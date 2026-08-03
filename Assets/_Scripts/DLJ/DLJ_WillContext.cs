using _Scripts.LDY;
using UnityEngine;

public class DLJ_WillContext
{
    public GameObject owner;
    public LDY_Animal animal;
    public LDY_BoardManager board;
    public LDY_TurnManager turnManager;
    public LDY_AttackSystem attackSystem;

    public GameObject rageObject;
    public float rageExpandTime;
    public float rageHoldTime;
    public float effectHeight;

    public GameObject curseObject;
    public float curseExpandTime;
    public float curseEffectHeight;
    public GameObject successionObject;
}
