using _Scripts.LSO;
using UnityEngine;

[CreateAssetMenu(fileName = "NewAnimalSO", menuName = "SO/AnimalSO")]
public class LSO_AnimalSO : ScriptableObject
{
    [Header("Tool Tip")]
    public string animalName;
    [TextArea(3, 10)]
    public string description;
    
    
    [Header("Stats")]
    public int maxHealth;
    public int damage;
    
    [Header("Types")]
    public LSO_RangeType rangeType;
    public LSO_WillType willType;
    public LSO_AbilityType abilityType;
}
