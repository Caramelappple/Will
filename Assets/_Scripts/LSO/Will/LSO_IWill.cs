namespace _Scripts.LSO.Will
{
    public interface LSO_IWill
    {
        bool ShouldDeferDestruction { get; }
        void InvokeWill();
    }
}
