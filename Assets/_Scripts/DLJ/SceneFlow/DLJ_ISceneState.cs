namespace _Scripts.DLJ.SceneFlow
{
    public interface DLJ_ISceneState
    {
        string SceneName { get; }

        void Enter();
        void Tick();
        void Exit();
    }
}
