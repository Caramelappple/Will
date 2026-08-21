using UnityEngine;

namespace _Scripts.LSO.Will
{
    /// <summary>Common metadata shared by every will data asset.</summary>
    public abstract class DLJ_WillDataSO : ScriptableObject
    {
        public abstract LSO_WillType WillType { get; }

        [Header("Tool Tip")]
        [TextArea(3, 10)]
        public string description;

        [Header("Icon")]
        public Sprite icon;
        
        [Header("Material")]
        public Material material;

        [Header("Effect")]
        public GameObject effectPrefab;

        public virtual int DisplayDamage => 0;
        public virtual int DisplayRange => 0;
        public virtual int DisplayDuration => 0;
        public virtual int DisplayBuffAmount => 0;
        public virtual int DisplayDebuffAmount => 0;
    }
}
