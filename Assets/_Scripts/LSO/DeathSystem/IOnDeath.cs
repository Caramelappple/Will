namespace _Scripts.LSO.DeathSystem
{
    public interface IOnDeath
    {
        public void OnDeath(LSO_AnimalSO self, LSO_AnimalSO killer);
    }
}