namespace _Scripts.LSO.DeathSystem
{
    public interface IOnBeforeDeath
    {
        public bool TryPreventDeath(LSO_AnimalSO self);
    }
}