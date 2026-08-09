using System;
using _Scripts.LDY;
using TMPro;
using UnityEngine;

namespace _Scripts.LSO.UI
{
    [RequireComponent(typeof (LDY_Animal))]
    public class StatBillBoardSpawner : MonoBehaviour
    {
        [SerializeField] public GameObject prefab;
        private GameObject _textObject;
        
        private LDY_Animal _animal;
        
        private void Awake()
        {
            _animal = GetComponent<LDY_Animal>();
        }

        private void OnEnable()
        {
            _textObject = Instantiate(prefab, transform);
            SendUpdate();
            
            
        }

        private void OnDisable()
        {
            
        }

        public void SendUpdate(int atk, int hp)
        {
            if (_textObject.TryGetComponent<LSO_StatBillboard>(out LSO_StatBillboard statBillboard))
            {
                statBillboard.SetText(atk, hp);
            }
            else
            {
                Debug.Log(_textObject.name  +"is null");
            }
        }
    }
}