using _Scripts.LDY;

namespace _Scripts.LSO.Boss
{
    public interface LSO_IPhaseAware
    {
        void OnPhaseChanged(LDY_Animal self,int phase);
    }
}
