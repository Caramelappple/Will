using _Scripts.LSO.Animal;
using UnityEngine;

[CreateAssetMenu(fileName = "New ButtonSO",menuName = "SO/ButtonSO")]
public class LSO_ButtonSO : ScriptableObject
{
  public Vector2Int pos;
  public LSO_ButtonType buttonType;
  public LSO_Animal targetAnimal;
  public LSO_Animal animal;
}
