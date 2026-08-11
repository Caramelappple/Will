using System;
using _Scripts.LSO.HealthSystem.Data;

public interface IRecoverable
{
    public event Action<RecoverResultData> OnRecover;
    public void Recover(RecoverData recoverValue);
}