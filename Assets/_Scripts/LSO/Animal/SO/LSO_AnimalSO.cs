using _Scripts.LSO.Animal.Data;
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
    public int cost;
    
    [Header("Types")]
    public LSO_Range range;
    public LSO_Ability ability;
    public LSO_Will will;
    public bool isAlly;
}
