using System;
using _Scripts.LSO;
using _Scripts.LSO.Animal;
using _Scripts.LSO.Board;
using UnityEngine;

public class LSO_ButtonManager : MonoBehaviour
{
    public static LSO_ButtonManager Instance;

    public static event Action<LSO_AnimalLoc, LSO_ButtonType, LSO_Animal> GetButtonData;

    // Summon: 어떤 동물(SO)을 어디에 소환할지.
    public static event Action<LSO_AnimalLoc, LSO_Animal> SummonButtonData;

    // Move/Attack: 대상 칸(loc)만 전달. 실제 이동 주체/공격자는 런타임 선택 상태에서 해석한다.
    // (구독자가 LSO_BoardManager.GetAnimal(loc) 등으로 해석 — 런타임 배선은 이후 단계)
    public static event Action<LSO_AnimalLoc, LSO_Animal> MoveButtonData;
    public static event Action<LSO_AnimalLoc> AttackButtonData;

    private void Awake()
    {
        Instance = this;
    }

    public void GiveButtonData(LSO_AnimalLoc loc, LSO_ButtonType type, LSO_Animal animal)
    {
        GetButtonData?.Invoke(loc, type,animal);
        switch (type)
        {
            case LSO_ButtonType.Attack:
                AttackButtonData?.Invoke(loc);
                break;
            case LSO_ButtonType.Move:
                MoveButtonData?.Invoke(loc,animal);
                break;
            case LSO_ButtonType.Summon:
                SummonButtonData?.Invoke(loc, animal);
                break;
            default:
                Debug.LogError($"{type} not implemented");
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

    private void Test(LSO_AnimalLoc loc, LSO_ButtonType type, LSO_Animal animal)
    {
        Debug.Log($"{LSO_BoardManager.Instance.Board2World(loc, gameObject.transform.position)} + {type}+ {animal}");
    }
}