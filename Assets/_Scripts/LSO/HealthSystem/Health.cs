using System;
using _Scripts.LSO.HealthSystem.Data;
using UnityEngine;

namespace _Scripts.LSO.HealthSystem
{
    public class Health : DamageableResources, IRecoverable
    {
        public event Action<RecoverResultData> OnRecover;

        [Header("디버그")]
        [Tooltip("회복할 때마다 콘솔에 찍는다. 콘솔이 시끄러우면 끌 것.")]
        [SerializeField] private bool logRecover = true;

        public virtual void Recover(RecoverData data)
        {
            if (IsDestroyed) return;

            int lastValue = Value;
            Value += data.recoverValue;

            if (lastValue >= Value) return;

            // 요청한 양이 아니라 실제로 오른 양을 싣는다.
            // 최대치에 걸리면 둘이 달라지는데, 요청량을 쓰면 UI가 없는 회복을 보여준다.
            int recovered = Value - lastValue;

            OnRecover?.Invoke(RecoverResultData.Create(data.giver, recovered, Value));

            if (logRecover)
                Debug.Log($"<color=green>{name}가 {recovered}만큼 회복했습니다. 현재 체력 : {Value}</color>", this);
        }

        [ContextMenu("Recover")]
        public void Recover()
        {
            var data = RecoverData.Create(null, 1);
            Recover(data);
        }
    }
}