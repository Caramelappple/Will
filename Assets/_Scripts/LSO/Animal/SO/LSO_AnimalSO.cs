using _Scripts.LDY;
using _Scripts.LSO;
using _Scripts.LSO.Ability;
using UnityEngine;

[CreateAssetMenu(fileName = "NewAnimalSO", menuName = "SO/AnimalSO")]
public class LSO_AnimalSO : ScriptableObject
{
    [Header("Tool Tip")]
    public string animalName;
    [TextArea(3, 10)]
    public string description;
    public Vector3Int pos;
    
    [Header("Stats")]
    public int maxHealth;
    public int damage;
    public int cost;
    
    [Header("Types")]
    public LDY_RangeType range;
    public LSO_AbilityType ability;
    public LSO_WillType willType;
}
