using System;
using UnityEngine;

namespace _Scripts.LSO.Will
{
    [Serializable]
    public class DLJ_WillData
    {
        [Header("Type")]
        public LSO_WillType willType;

        [Header("System")]
        public int damage;
        public int range;
        public int duration;

        [Header("Effect")]
        public GameObject effectPrefab;
        public float expandTime = 0.25f;
        public float holdTime = 0.3f;
        public float effectHeight = 0.12f;
        public float moveDuration = 1f;
    }
}
