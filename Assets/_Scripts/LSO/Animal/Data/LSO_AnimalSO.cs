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
    //public Vector3Int pos;
    
    [Header("Stats")]
    public int maxHealth;
    public int damage;
    public int cost;
    
    [Header("Types")]
    public LDY_RangeType range;
    public LSO_AbilityType ability;

    [Tooltip("플레이어가 고르지 않고 소환될 때 쓸 유언.\n" +
             "적 기물과 스테이지 초기 배치가 이 값을 쓴다.\n" +
             "플레이어가 직접 소환할 때는 선택 UI 결과가 우선한다.")]
    public LSO_WillType defaultWill;

    [Header("Prefab")]
    [Tooltip("보드에 소환될 기물 프리팹. LDY_Animal 컴포넌트가 붙어 있어야 한다.")]
    public GameObject unitPrefab;
}
