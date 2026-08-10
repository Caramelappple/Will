using System;

namespace _Scripts.DLJ.SceneFlow
{
    public sealed class DLJ_SceneStateMachine
    {
        private readonly Action<string> sceneLoadAction;
        private readonly Func<bool> sceneLoadingCheck;
        private bool isChangingState;

        public DLJ_ISceneState CurrentState { get; private set; }
        public bool IsChangingState => isChangingState;

        public event Action<DLJ_ISceneState, DLJ_ISceneState> StateChanged;

        public DLJ_SceneStateMachine(Action<string> sceneLoadAction, Func<bool> sceneLoadingCheck)
        {
            this.sceneLoadAction = sceneLoadAction ?? throw new ArgumentNullException(nameof(sceneLoadAction));
            this.sceneLoadingCheck = sceneLoadingCheck ?? throw new ArgumentNullException(nameof(sceneLoadingCheck));
        }

        public bool ChangeState(DLJ_ISceneState nextState)
        {
            if (nextState == null)
                throw new ArgumentNullException(nameof(nextState));

            if (isChangingState || sceneLoadingCheck() || ReferenceEquals(CurrentState, nextState))
                return false;

            isChangingState = true;
            DLJ_ISceneState previousState = CurrentState;

            try
            {
                previousState?.Exit();
                CurrentState = nextState;
                CurrentState.Enter();
                sceneLoadAction(CurrentState.SceneName);
                StateChanged?.Invoke(previousState, CurrentState);
                return true;
            }
            catch
            {
                CurrentState = previousState;
                throw;
            }
            finally
            {
                isChangingState = false;
            }
        }

        public void Tick()
        {
            if (!isChangingState && !sceneLoadingCheck())
                CurrentState?.Tick();
        }
    }
}
