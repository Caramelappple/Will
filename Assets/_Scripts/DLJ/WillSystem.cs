using System;
using _Scripts.LSO;
using UnityEngine;
using UnityEngine.InputSystem;

public class WillSystem : MonoBehaviour, IWillActivation
{
    public LSO_AnimalSO AnimalSo;

    private static WillSystem successionSource;

    public void WillActivate()
    {
        if (AnimalSo == null)
        {
            Debug.LogError($"{name}: AnimalSo가 비어 있음", this);
            return;
        }

        if (successionSource != null)
        {
            successionSource.CompleteSuccession(this);
            successionSource = null;
            return;
        }

        /*switch (AnimalSo.willType)
        {
            case LSO_WillType.Curse:
                ActivateCurse();
                break;

            case LSO_WillType.Rage:
                ActivateRage();
                break;

            case LSO_WillType.Succession:
                BeginSuccession();
                break;
        }*/
    }

    private void ActivateCurse()
    {
        Debug.Log("Curse Activated");
    }

    private void ActivateRage()
    {
        Debug.Log("Rage Activated");
    }

    private void BeginSuccession()
    {
        successionSource = this;
        Debug.Log("Pick Target");
    }

    private void CompleteSuccession(WillSystem target)
    {
        if (target == this)
        {
            Debug.LogWarning("Failed");
            return;
        }

        if (target.AnimalSo == null)
        {
            Debug.LogError("No Target");
            return;
        }

        target.AnimalSo.maxHealth += AnimalSo.maxHealth;
        target.AnimalSo.damage += AnimalSo.damage;

        AnimalSo.maxHealth = 0;
        AnimalSo.damage = 0;

        Debug.Log("Succession Finished");
    }
}
