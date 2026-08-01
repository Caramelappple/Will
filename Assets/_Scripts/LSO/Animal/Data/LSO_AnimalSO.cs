using _Scripts.LDY;
using _Scripts.LSO.Ability;
using UnityEngine;

[CreateAssetMenu(fileName = "NewAnimalSO", menuName = "SO/AnimalSO")]
public class LSO_AnimalSO : ScriptableObject
{
    [Header("Tool Tip")]
    public string animalName;
    [TextArea(3, 10)]
    public string description;
    //public Vector3Int pos;
    
    [Header("Stats")]
    public int maxHealth;
    public int damage;
    public int cost;
    
    [Header("Types")]
    public LDY_RangeType range;
    public LSO_AbilityType ability;
   // public LSO_WillType willType;
   //유언은 카드에서 랜덤으로 선택하므로 필요 X

    [Header("Prefab")]
    [Tooltip("보드에 소환될 기물 프리팹. LDY_Animal 컴포넌트가 붙어 있어야 한다.")]
    public GameObject unitPrefab;
}
