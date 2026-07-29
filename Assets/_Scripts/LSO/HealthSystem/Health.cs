using System;
using _Scripts.HealthSystem;
using UnityEngine;

namespace _Scripts.LSO.HealthSystem
{
    public class Health : DamageableResources, IRecoverable
    {
        public event Action<RecoverResultData> OnRecover;

        public virtual void Recover(RecoverData data)
        {
            if (IsDestroyed) return;

            int lastValue = Value;
            Value += data.recoverValue;

            if (lastValue < Value)
            {
                Debug.Log($"{data.recoverValue}만큼 회복했습니다. 현재 체력 : {Value}");
                var resultData = RecoverResultData.Create(data.giver, data.recoverValue, Value);
                OnRecover?.Invoke(resultData);
            }
        }

        [ContextMenu("Recover")]
        public void Recover()
        {
            var data = RecoverData.Create(null, 1);
            Recover(data);
        }
    }
}