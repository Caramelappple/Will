using System;
using UnityEngine;

namespace _Scripts.LSO.Animal.Data
{
    [Serializable]
    public abstract class LSO_Will : MonoBehaviour
    {
        
        public LSO_HealthSystem healthSystem;
        public virtual void InvokeWill(LSO_Animal animal)
        {
            Debug.Log(animal.name + "'s Will Invoked");
        }

        private void OnEnable()
        {
            healthSystem.OnDeath += InvokeWill;
        }
    }
}