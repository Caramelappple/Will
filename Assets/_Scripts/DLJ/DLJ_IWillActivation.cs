public interface DLJ_IWillActivation
{
    bool ShouldDeferDestruction { get; }
    void WillActivate();
}
