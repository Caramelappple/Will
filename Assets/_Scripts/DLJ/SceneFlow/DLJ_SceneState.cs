using System;

namespace _Scripts.DLJ.SceneFlow
{
    /// <summary>
    /// 씬 하나를 게임 흐름의 상태로 표현하는 기본 상태.
    /// 상태별 동작이 필요하면 상속해서 Enter, Tick, Exit를 재정의한다.
    /// </summary>
    public class DLJ_SceneState : DLJ_ISceneState
    {
        public string SceneName { get; }

        public DLJ_SceneState(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                throw new ArgumentException("씬 이름은 비어 있을 수 없습니다.", nameof(sceneName));

            SceneName = sceneName;
        }

        public virtual void Enter()
        {
        }

        public virtual void Tick()
        {
        }

        public virtual void Exit()
        {
        }
    }
}
