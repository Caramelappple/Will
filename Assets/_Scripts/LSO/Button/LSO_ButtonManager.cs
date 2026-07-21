using System;
using _Scripts.LSO;
using _Scripts.LSO.Animal;
using _Scripts.LSO.Board;
using UnityEngine;

public class LSO_ButtonManager : MonoBehaviour
{
    public static LSO_ButtonManager Instance;
    public static event Action<LSO_AnimalLoc, LSO_ButtonType> GetButtonData; 
    public static event Action<LSO_AnimalLoc,LSO_Animal> SummonButtonData; 
    public static event Action<LSO_AnimalLoc,LSO_Animal> MoveButtonData; 
    public static event Action<LSO_AnimalLoc,LSO_Animal,LSO_Animal> AttackButtonData; 
    
    
    
    private void Awake()
    {
        Instance = this;
    }
    
    public void GiveButtonData(LSO_AnimalLoc loc, LSO_ButtonType type, LSO_Animal targetAnimal, LSO_Animal animal)
    {
        GetButtonData?.Invoke(loc, type);
        switch (type)
        {
            case LSO_ButtonType.Attack :
                AttackButtonData?.Invoke(loc,targetAnimal,animal);
                break;
            case LSO_ButtonType.Move:
                MoveButtonData?.Invoke(loc,targetAnimal);
                break;
            case LSO_ButtonType.Summon:
                SummonButtonData?.Invoke(loc,targetAnimal);
                break;
            default:
                Debug.LogError($"{type.ToString()} not implemented");
                break;
        }
    }


    private void OnEnable()
    {
        GetButtonData += Test;
    }

    private void OnDisable()
    {
        GetButtonData -= Test;
    }

    private void Test(LSO_AnimalLoc loc, LSO_ButtonType type)
    {
        Debug.Log($"{BoardManager.Instance.Board2World(loc,gameObject.transform.position)} + {type.ToString()}");
    }
}
