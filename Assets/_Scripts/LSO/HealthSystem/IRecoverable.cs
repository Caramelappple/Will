using System;
using _Scripts.LSO.HealthSystem.Data;

namespace _Scripts.LSO.HealthSystem
{
    public interface IRecoverable
    {
        public event Action<RecoverResultData> OnRecover;
        public void Recover(RecoverData recoverValue);
    }
}
