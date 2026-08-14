using UnityEngine;

namespace _Scripts.LSO.Will
{
    /// <summary>Standalone settings asset for one will type.</summary>
    [CreateAssetMenu(fileName = "DLJ_WillData", menuName = "DLJ/Will/Data")]
    public sealed class DLJ_WillDataSO : ScriptableObject
    {
        [Header("Type")]
        public LSO_WillType willType;

        [Header("System")]
        public int damage;
        public int range;
        public int duration;

        [Header("Tool Tip")]
        [TextArea(3, 10)]
        public string description;

        [Header("Icon")]
        public Sprite icon;

        [Header("Buff / Debuff")]
        public int buffAmount;
        public int debuffAmount;

        [Header("Effect")]
        public GameObject effectPrefab;
        public float expandTime = 0.25f;
        public float holdTime = 0.3f;
        public float effectHeight = 0.12f;
        public float moveDuration = 1f;
    }
}
