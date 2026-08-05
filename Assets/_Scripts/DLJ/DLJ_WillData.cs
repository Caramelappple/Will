using UnityEngine;

namespace _Scripts.LSO.Will
{
    /// <summary>Base data contract consumed by the will factory.</summary>
    public abstract class DLJ_WillData : ScriptableObject
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
