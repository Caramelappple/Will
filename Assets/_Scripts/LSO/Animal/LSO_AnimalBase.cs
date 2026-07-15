using _Scripts.LSO.Board;
using UnityEngine;

namespace _Scripts.LSO
{
    public abstract class LSO_AnimalBase : MonoBehaviour
    {
        public LSO_AnimalSO animal;
        
        protected BoardManager Board { get; private set; }
        public int Row { get; private set; }
        public int Col { get; private set; }

        public void Init(BoardManager board, int row, int col)
        {
            Board = board;
            Row = row;
            Col = col;
        }
        
        [ContextMenu("Attack")]
        public virtual void Attack()
        {
            Debug.Log($"{animal.name} is attacking with {animal.rangeType}");
        }
        
        [ContextMenu("Ability")]
        public virtual void ApplyAbility()
        {
            Debug.Log($"{animal.abilityType} applied");
        }
        
        [ContextMenu("Will")]
        public virtual void InvokeWill()
        {
            Debug.Log($"{animal.willType} Invoked");    
        }
    }
}