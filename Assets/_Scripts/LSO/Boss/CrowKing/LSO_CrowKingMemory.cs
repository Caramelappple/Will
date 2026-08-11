using System;
using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LSO.Ability;
using UnityEngine;

namespace _Scripts.LSO.Boss.CrowKing
{
    /// <summary>
    /// 이 스크립트에서 해주는 것
    /// 1.포식 (처치시 능력치 계승)
    /// </summary>
    [RequireComponent(typeof(LDY_Animal))]
    public class LSO_CrowKingMemory : MonoBehaviour
    {
        [SerializeField] private int maxSuccession;
        private int _addedSuccessionCount;
        //private 
        private readonly List<LSO_AnimalSO> _devoured = new();
        private LDY_Animal _owner;

        private void Awake()
        {
            _owner = GetComponent<LDY_Animal>();   
        }
        
        //등록 되어 있는가
        public bool HasDevoured(LSO_AnimalSO animal) => _devoured.Contains(animal);

        public bool TryAddDevour(LSO_AnimalSO animal)
        {
            if (_devoured.Contains(animal))
                return false;
            _devoured.Add(animal);
            return true;
        }

        public bool TryStoreAbility(LSO_AbilityType type)
        {
            if (_owner.AddAbility(type) != null && _addedSuccessionCount < maxSuccession)
            {
                _addedSuccessionCount++;
                return true;
            }
            else
            {
                return false;
            }
        }
        
        public int inheritedAtk;
    }
}