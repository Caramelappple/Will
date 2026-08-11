using System;
using _Scripts.LDY;
using UnityEngine;

namespace _Scripts.LSO.Boss.CrowKing
{
    [RequireComponent(typeof(LDY_Animal))]
    public class LSO_PreyTracker : MonoBehaviour
    {
        private LDY_Animal _prey;
        
        public LDY_Animal Prey => _prey != null ? _prey : null;

        public event Action<LDY_Animal> PreyChanged;

        public void SetPrey(LDY_Animal animal)
        {
            if (_prey == animal) return;

            _prey = animal;
            
            PreyChanged?.Invoke(Prey);
        }
    }
}
